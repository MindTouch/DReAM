﻿/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
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

namespace MindTouch.Data.Db {
    class UpdateDb {

        /// <summary>
        /// Class to store database connection information
        /// </summary>
        internal class DBConnection {
            public string dbServer;
            public string dbName;
            public string dbUsername;
            public string dbPassword;

            /// <summary>
            /// Parse a config string of format dbname[;dbserver;dbuser;dbpassword]
            /// </summary>
            /// <param name="configString"></param>
            /// Config String of the proper format
            /// <param name="defaultServer"></param>
            /// Default server
            /// <param name="defaultUser"></param>
            /// Default user
            /// <param name="defaultPassword"></param>
            /// Default password
            /// <returns></returns>
            public static DBConnection Parse(string configString, string defaultServer, string defaultUser, string defaultPassword) {
                if(string.IsNullOrEmpty(configString)) {
                    return null;
                }
                var items = configString.Split(';');
                var con = new DBConnection();
                con.dbName = items[0];
                if(items.Length < 4) {
                    con.dbServer = defaultServer;
                    con.dbUsername = defaultUser;
                    con.dbPassword = defaultPassword;
                } else if(items.Length == 4) {
                    con.dbServer = items[1];
                    con.dbUsername = items[2];
                    con.dbPassword = items[3];
                }
                return con;
            }   
        }

        //--- Class Methods ---
        static int Main(string[] args) {
            string dbusername = "root", dbname = "", dbserver = "localhost", dbpassword = null, updateDLL = null, 
                   targetVersion = null, sourceVersion = null, customMethod = null;
            string[] param = null;
            int dbport = 3306, exitCode = 0, errors = 0;
            uint timeout = UInt32.MaxValue;
            bool showHelp = false, dryrun = false, verbose = false, listDatabases = false, checkDb = false;
            var errorStrings = new List<string>();

            // set command line options
            var options = new Options() {
                { "p=|dbpassword=", "Database password", p => dbpassword = p },
                { "v=|version=", "Target Version", v => targetVersion = v },
                { "b=|sversion=", "Source Version", b => sourceVersion = b },
                { "u=|dbusername=", "Database user name (default: root)", u => dbusername = u },
                { "d=|dbname=", "Database name (default: wikidb)", p => dbname = p },
                { "s=|dbserver=", "Database server (default: localhost)", s => dbserver = s },
                { "n=|port=", "Database port (default: 3306)", n => {dbport = Int32.Parse(n);}},
                { "o=|timeout=", "Sets the database's Default Command Timeout", o => { timeout = UInt32.Parse(o); }},
                { "c=|custom", "Custom method to invoke", c => { customMethod = c; }},
                { "i|info", "Display verbose information (default: false)", i => { verbose = true; }},
                { "noop|dryrun", "Just display the methods that will be called, do not execute any of them. (default: false)", f => { dryrun = verbose = true; }},
                { "l|listdb" , "List of databases separated by EOF", l => { listDatabases = true; }},
                { "t|checkdb", "Run database tests and nothing else", t => { checkDb = true; }},
                { "h|?|help", "show help text", h => { showHelp = true; }},
            };
            if(args == null || args.Length == 0) {
                showHelp = true;
            } else {
                try {
                    var trailingOptions = options.Parse(args).ToArray();

                    // if there are more trailing arguments display help
                    if(trailingOptions.Length < 1) {
                        showHelp = true;
                    } else {
                        updateDLL = Path.GetFullPath(trailingOptions.First());
                        param = trailingOptions.SubArray(1);
                    }
                } catch(InvalidOperationException) {
                    exitCode = -3;
                    Console.Error.WriteLine("Invalid arguments");
                    showHelp = true;
                }
            }
            if(showHelp) {
                ShowHelp(options);
                return exitCode;
            }

            // Check Arguments
            CheckArg(updateDLL, "No DLL file was specified");
            if(!listDatabases) {
                CheckArg(dbpassword, string.Format("No Database password specified for database {0}", dbname));
            }

            // If there are no custom methods specified we need a version number
            if(customMethod == null) {
                CheckArg(dbname, "No Database was specified");
            }

            // Begin Parsing DLL
            var dllAssembly = Assembly.LoadFile(updateDLL);
            MysqlDataUpdater mysqlSchemaUpdater = null;
            if(listDatabases) {

                // Read list of databases if listDatabases is true
                var databaseList = new List<DBConnection>();

                // Read the db names from input
                // format: dbname[;dbserver;dbuser;dbpassword]
                string line = null;
                while(!string.IsNullOrEmpty(line = Console.ReadLine())) {
                    var connection = DBConnection.Parse(line, dbserver, dbusername, dbpassword);
                    if(connection != null) {
                        databaseList.Add(connection);
                    }
                }
                foreach(var db in databaseList) {
                    try {
                        mysqlSchemaUpdater = new MysqlDataUpdater(db.dbServer, dbport, db.dbName, db.dbUsername, db.dbPassword, targetVersion, timeout);
                        if(sourceVersion != null) {
                            mysqlSchemaUpdater.SourceVersion = sourceVersion;
                        }
                    } catch(VersionInfoException) {
                        PrintErrorAndExit("You entered an incorrect version numbner.");
                    } catch(Exception) {

                        // If there is any problem creating the connection we will just keep going
                        errors++;
                        errorStrings.Add(string.Format("There was an error connecting to database {0} on {1}", db.dbName, db.dbServer));
                        continue;
                    }
                    if(verbose) {
                        Console.WriteLine("\n--- Updating database {0} on server {1}", db.dbName, db.dbServer);
                    }

                    // Run methods on database
                    RunUpdate(mysqlSchemaUpdater, dllAssembly, customMethod, param, verbose, dryrun, checkDb);
                }
            } else {
                try {
                    mysqlSchemaUpdater = new MysqlDataUpdater(dbserver, dbport, dbname, dbusername, dbpassword, targetVersion, timeout);
                    if(sourceVersion != null) {
                        mysqlSchemaUpdater.SourceVersion = sourceVersion;
                    }
                } catch(VersionInfoException) {
                    PrintErrorAndExit("You entered an incorrect version numner.");
                }

                // Run update
                RunUpdate(mysqlSchemaUpdater, dllAssembly, customMethod, param, verbose, dryrun, checkDb);
            }
            if(errors > 0) {
                Console.WriteLine("\nThere were {0} errors:", errors);
                foreach(var error in errorStrings) {
                    Console.WriteLine("---" + error);
                }
            }
            return exitCode;
        }

        private static void RunUpdate(MysqlDataUpdater site, Assembly dllAssembly, string customMethod, string[] param, bool verbose, bool dryrun, bool checkdb) {
            
            // Execute custom methods
            if(customMethod != null) {
                if(verbose) {
                    Console.WriteLine("Executing custom method: {0}", customMethod);
                }
                if(!dryrun) {
                    site.ExecuteCustomMethod(customMethod.Trim(), dllAssembly, param);
                }
                return;
            }

            // Execute update methods
            site.LoadMethods(dllAssembly);
            var methods = checkdb ? site.GetDataIntegrityMethods() : site.GetMethods();

            // Execute each method
            foreach(var method in methods) {
                if(verbose) {
                    Console.WriteLine("Executing method: {0}", method);
                }
                if(!dryrun) {
                    try { site.TestConnection(); } catch(Exception) {
                        System.Threading.Thread.Sleep(5000);
                        site.TestConnection();
                    }
                    try {
                        site.ExecuteMethod(method);
                    } catch(Exception ex) {
                        Console.WriteLine("\n --- Error occured in method {0}: \n\n{1}", method, ex.StackTrace);
                        if(!checkdb) {
                            break;
                        }
                    }
                }
            }
        }

        private static void ShowHelp(Options p) {
            var sw = new StringWriter();
            sw.WriteLine("Usage: mindtouch.db.exe -p password -v version mindtouch.deki.db.dll");
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
    }
}
