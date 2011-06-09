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
using System.IO;
using System.Linq;
using System.Reflection;

namespace MindTouch.Data.Db {
    class UpdateDb {

        //--- Class Methods ---
        static int Main(string[] args) {
            string dbusername = "root", dbname = "wikidb", dbserver = "localhost", dbpassword = null, updateDLL = null, targetVersion = null;
            int dbport = 3306, exit = 0;
            bool showHelp = false, dryrun = false, verbose = false;

            // set command line options
            var options = new Options() {
                { "p=|dbpassword=", "Database password", p => dbpassword = p},
                { "v=|version=", "Target Version", v => targetVersion = v},
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

                // Check Arguments
                CheckArg(updateDLL, "No DLL file was specified");
                CheckArg(dbpassword, "No Database password specified");
                CheckArg(targetVersion, "No version specified");

                // Begin Parsing DLL
                var dllAssembly = Assembly.LoadFile(updateDLL);
               
                // Instatiate Mysql Upgrade class
                MysqlDataUpdater mysqlSchemaUpdater = null;
                try {
                    mysqlSchemaUpdater = new MysqlDataUpdater(dbserver, dbport, dbname, dbusername, dbpassword, targetVersion);
                } catch(VersionInfoException) {
                    PrintErrorAndExit("You entered an incorrect version numner.");
                }
                mysqlSchemaUpdater.LoadMethods(dllAssembly);
                var methods = mysqlSchemaUpdater.GetMethods();

                // Execute each method
                foreach(var method in methods) {
                    if(verbose) {
                        Console.WriteLine(String.Format("Executing Method: {0}", method));
                    }
                    if(!dryrun) {
                        try { mysqlSchemaUpdater.TestConnection(); }
                        catch (Exception) {
                            System.Threading.Thread.Sleep(5000);
                            mysqlSchemaUpdater.TestConnection();
                        }
                        mysqlSchemaUpdater.ExecuteMethod(method);
                    }
                }   
            }
            else {
                ShowHelp(options);
            } 
            return exit;
        }

        private static void ShowHelp(Options p) {
            var sw = new StringWriter();
            sw.WriteLine("Usage: updateDb.exe -p password -v version mindtouch.deki.dll");
            p.WriteOptionDescriptions(sw);
            Console.WriteLine(sw.ToString());
        }

        private static void CheckArg(string arg, string message) {
            if(string.IsNullOrEmpty(arg)) {
                throw new CliArgException(message);
            }
        }

        private static void PrintErrorAndExit(string message) {
            Console.Error.WriteLine("ERROR: " + message);
            Environment.Exit(-1);
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
