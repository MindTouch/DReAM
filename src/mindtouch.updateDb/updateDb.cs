/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2011 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MindTouch.Data.UpdateDb {

    internal class UpdateMethod : IComparable<UpdateMethod> {
        //--- Fields ---
        private readonly MethodInfo _methodInfo;
        private readonly VersionInfo _effectiveVersion;

        //--- Constructors ---
        public UpdateMethod(MethodInfo methodInfo, VersionInfo effectiveVersion) {
            _methodInfo = methodInfo;
            _effectiveVersion = effectiveVersion;
        }

        //--- Methods ---
        public MethodInfo GetMethodInfo {
            get { return _methodInfo; }
        }

        public VersionInfo GetVersionInfo {
            get { return _effectiveVersion; }
        }

        // Compares by version then by name
        public int CompareTo(UpdateMethod other) {
            var otherVersion = other.GetVersionInfo;
            var change = _effectiveVersion.CompareTo(otherVersion).Change;
            switch(change) {
            case VersionChange.None:
                return _methodInfo.Name.CompareTo(other._methodInfo.Name);
            case VersionChange.Upgrade:
                return 1;
            default:
                return -1;
            }
        }
    }

    class UpdateDb {

        //--- Class Methods ---
        static int Main(string[] args) {
            string updateDLL = null;
            string targetVersion = null;
            var dbusername = "root";
            var dbname = "wikidb";
            string dbpassword = null;
            var dbserver = "localhost";
            int dbport = 3306;
            var showHelp = false;
            var exit = 0;
            var verbose = false;
            var dryrun = false;
            VersionInfo targetVersionInfo = null;

            // set command line options
            var options = new Options() {
                { "p=|dbpassword=", "Database password", p => dbpassword = p},
                { "v=|version=", "Target Version (default: newest)", v => targetVersion = v},
                { "u=|dbusername=", "Database user name (default: root)", u => dbusername = u},
                { "d=|dbname=", "Database name (default: wikidb)", p => dbname = p},
                { "s=|dbserver=", "Database server (default: localhost)", s => dbserver = s},
                { "n=|port=", "Database port (default: 3306)", n => {dbport = Int32.Parse(n);}},
                { "i|info", "Display verbose information (default: false)", i => {verbose = true;}},
                { "f|dryrun", "Just display the methods that will be called, do not execute any of them. (default: false)", f => { dryrun = verbose = true;} },
                { "h|?|help", "show help text", h => { verbose = true; }},
            };
            if(args == null || args.Length == 0) {
                showHelp = true;
            } else {
                try {
                    var trailingOptions = options.Parse(args).ToArray();

                    // if there are more trailing arguments display help
                    if(trailingOptions.Length != 1) {
                        showHelp = true;
                    } else {
                        updateDLL = Path.GetFullPath(trailingOptions.First());
                    }
                } catch(InvalidOperationException) {
                    exit = -3;
                    Console.Error.WriteLine("Invalid arguments");
                    showHelp = true;
                }
            }
            if(!showHelp) {
                try {
                    // Check Arguments
                    CheckArg(updateDLL, "No DLL file was specified");
                    CheckArg(dbpassword, "No Database password specified");

                    // parse version
                    if(targetVersion != null) {
                        targetVersionInfo = new VersionInfo(targetVersion);
                        if(!targetVersionInfo.IsValid) {
                            PrintErrorAndExit("The version number you entered is not in a valid format", 4);
                        }
                    }

                    // Begin Parsing DLL
                    var dllAssembly = Assembly.LoadFile(updateDLL);

                    // get all the members
                    var types = dllAssembly.GetTypes();

                    // Find the class with attribute "DbUpgrade"
                    Type DbUpgradeClass = null;
                    var classTypes = from type in types where type.IsClass select type;
                    foreach(var type in from type in classTypes from attribute in (from a in System.Attribute.GetCustomAttributes(type) where a is DbUpgradeAttribute select a) select type) {
                        if(DbUpgradeClass != null) {
                            PrintErrorAndExit(String.Format("File {0} contains multiple classes with Attribute DbUpgrade. This is not allowed", updateDLL), 2);
                        }
                        DbUpgradeClass = type;
                    }

                    // if no class was found exit 
                    if(DbUpgradeClass == null) {
                        PrintErrorAndExit(String.Format("No Class with attribute DbUpgrade was found", updateDLL), 3);
                    }
                    if(verbose) {
                        Console.WriteLine(string.Format("Found class {0} marked with [DbUpgrade]", DbUpgradeClass.Name));
                    }


                    // search the class for methods labeled with Attribute "EffectiveVersion("version")"
                    var methods = DbUpgradeClass.GetMethods();
                    var methodList = new List<UpdateMethod>();
                    foreach(var methodInfo in methods) {
                        foreach(var attr in (from m in methodInfo.GetCustomAttributes(false) where m is EffectiveVersionAttribute select m)) { 
                            var version = new VersionInfo(((EffectiveVersionAttribute)attr).VersionString);
                            if(targetVersionInfo == null || version.CompareTo(targetVersionInfo).Change != VersionChange.Upgrade) {
                                methodList.Add(new UpdateMethod(methodInfo, version));
                            }
                        }
                    }

                    // Sort Methods by version then by name
                    methodList.Sort();
                    if(verbose) {
                        Console.WriteLine("The following methods will be executed:\n");
                        foreach(var methodInfo in methodList) {
                            Console.WriteLine(methodInfo.GetMethodInfo.Name);
                        }
                    }

                    // Open mysql connection
                    var dataFactory = new DataFactory("MySql.Data", "?");
                    var connectionString = BuildConnectionString(dbserver, dbport, dbname, dbusername, dbpassword);
                    var dataCatalog = new DataCatalog(dataFactory, connectionString);

                    // test the database connection
                    if(verbose) {
                        Console.WriteLine(string.Format("\nConnecting to database using connection string: {0}\n", connectionString));
                    }
                    try {
                        dataCatalog.TestConnection();
                    } catch(Exception) {
                        PrintErrorAndExit(string.Format("Unable to connect to mysql database with connectionString: {0}", connectionString), 4);
                    }

                    // if this is not a fake run execute the methods
                    if(!dryrun) {
                        // Create instance of the DbUpgrade Class
                        var updateClass = Activator.CreateInstance(DbUpgradeClass, dataCatalog);
                        if(updateClass == null) {
                            PrintErrorAndExit(string.Format("Could not instantiate class {0}", DbUpgradeClass.Name) ,5);
                        }

                        // Call methods
                        foreach(var method in methodList) {
                            if(verbose) {
                                Console.WriteLine(string.Format("Invoking method: {0}", method.GetMethodInfo.Name));
                            }
                            DbUpgradeClass.InvokeMember(method.GetMethodInfo.Name, BindingFlags.Default | BindingFlags.InvokeMethod, null, updateClass, null);
                        }                     
                    }
                } catch(CliArgException e) {
                    exit = 1;
                    Console.Error.WriteLine("Argument Error: {0}", e.Message);
                    showHelp = true;
                } catch (FileNotFoundException ex) {
                    exit = 1;
                    Console.Error.WriteLine("File {0} was not found.", updateDLL);
                    showHelp = true;
                } catch (FileLoadException ex) {
                    exit = 1;
                    Console.Error.WriteLine("Could not load file {0}", updateDLL);
                    showHelp = true;
                }
            }

            if(showHelp) {
                ShowHelp(options);
            }
            return exit;
        }

        private static void ShowHelp(Options p) {
            var sw = new StringWriter();
            sw.WriteLine("Usage: updateDb.exe -p password mindtouch.deki.dll");
            p.WriteOptionDescriptions(sw);
            Console.WriteLine(sw.ToString());
        }

        private static void CheckArg(string arg, string message) {
            if(string.IsNullOrEmpty(arg)) {
                throw new CliArgException(message);
            }
        }

        private static void PrintErrorAndExit(string message, int errorCode) {
            Console.Error.WriteLine("ERROR: " + message);
            Environment.Exit(errorCode);
        }

        private static string BuildConnectionString(string server, int port, string dbname, string dbuser, string dbpassword) {
            StringBuilder connectionString = new StringBuilder();
            connectionString.AppendFormat("Server={0};", server);
            connectionString.AppendFormat("Port={0};", port);
            connectionString.AppendFormat("Database={0};", dbname);
            connectionString.AppendFormat("User Id={0};", dbuser);
            connectionString.AppendFormat("Password={0};", dbpassword);
            return connectionString.ToString();
        }

        private static void OutputException(Exception exception) {
            if(exception.InnerException != null) {
                OutputException(exception.InnerException);
            }
            Console.Error.WriteLine("----------------------------------");
            Console.Error.WriteLine(exception);
        }
        
    }
}
