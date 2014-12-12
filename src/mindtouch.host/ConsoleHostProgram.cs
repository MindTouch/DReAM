/*
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
using System.IO;
using System.Threading;
using MindTouch.Tasking;
using MindTouch.Xml;
using Mono.Unix;
using Mono.Unix.Native;

namespace MindTouch.Dream {
    internal class DreamConsoleHost {

        //--- Constants ---
        private const int DEFAULT_PORT = 8081;

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog();
        private static DreamHost _host;

        //--- Class Methods ---
        private static int Main(string[] args) {
            bool useTty = true;
            TimeSpan time;

            // process command line arguments
            XDoc config = new XDoc("config");
            for(int i = 0; i < args.Length; i += 2) {
                string key = args[i].ToLowerInvariant();
                string value = ((i + 1) < args.Length) ? args[i + 1] : string.Empty;
                switch(key) {
                case "help":
                    PrintUsage();
                    return 0;
                case "notty":
                    --i;
                    useTty = false;
                    break;
                case "capture-stack-trace":
                    --i;
                    DebugUtil.CaptureStackTrace = true;
                    break;
                case "nolog":
                    --i;

                    // NOTE (steveb): this option used to disable logging, but was removed in favor of using the automatic re-reading of app.config by log4net

                    break;
                case "settings":
                case "config":
                    if(!File.Exists(value)) {
                        WriteError(key, "config file not found");
                        return 1;
                    }
                    config = XDocFactory.LoadFrom(value, MimeType.XML);
                    break;
                case "script":
                    config.Start("script").Attr("src", value).End();
                    break;
                case "ip":
                case "host":
                    config.Elem("host", value);
                    break;
                case "http-port":
                case "path-prefix":
                case "server-path":
                case "server-name":
                case "storage-dir":
                case "connect-limit":
                case "apikey":
                case "guid":
                    config.Elem(key, value);
                    break;
                case "public-uri":
                case "root-uri":
                    config.Elem("uri.public", value);
                    break;
                case "service-dir":
                    config.Elem("storage-dir", value);
                    break;
                case "collect-interval":
                    int interval;
                    if(!int.TryParse(value, out interval)) {
                        WriteError(key, "invalid collection interval (must be an integer representing seconds)");
                        return 1;
                    }
                    if(interval > 0) {
                        DebugUtil.SetCollectionInterval(TimeSpan.FromSeconds(interval));
                    }
                    break;
                case "auth":
                    config.Elem("authentication-shemes", value);
                    break;
                default:
                    WriteError(key, "unknown setting");
                    return 1;
                }
            }
            try {

                // initialize environment
                if(config["apikey"].IsEmpty) {
                    string apikey = StringUtil.CreateAlphaNumericKey(32);
                    config.Elem("apikey", apikey);
                    Console.WriteLine("Dream Host APIKEY: {0}", apikey);
                }
                Console.WriteLine("-------------------- initializing");
                time = DebugUtil.Stopwatch(() => {
                    _host = new DreamHost(config);
                });
                Console.WriteLine("-------------------- initialized {0} secs", time.TotalSeconds);

                // execute scripts
                time = DebugUtil.Stopwatch(() => {
                    _host.RunScripts(config, null);
                });
                Console.WriteLine("-------------------- ready {0} secs", time.TotalSeconds);

                // for UNIX systems, let's also listen to SIGTERM
                if(SysUtil.IsUnix) {
                    new Thread(SigTermHandler) { IsBackground = true }.Start();
                }

                // check if we can use the console
                if(useTty) {
                    int debuglevel = 0;

                    // wait for user input then exit
                    while(_host.IsRunning) {
                        Thread.Sleep(250);
                        #region Interactive Key Handler
                        if(Console.KeyAvailable) {
                            ConsoleKeyInfo key = Console.ReadKey(true);
                            switch(key.Key) {
                            case ConsoleKey.Q:
                            case ConsoleKey.Escape:
                            case ConsoleKey.Spacebar:
                                Console.WriteLine("Shutting down");
                                return 0;
                            case ConsoleKey.G:
                                Console.WriteLine("Full garbage collection pass");
                                System.GC.Collect();
                                break;
                            case ConsoleKey.C:
                                Console.Clear();
                                break;
                            case ConsoleKey.D:
                                switch(++debuglevel) {
                                default:
                                    debuglevel = 0;
                                    Threading.RendezVousEvent.CaptureTaskState = false;
                                    DebugUtil.CaptureStackTrace = false;
                                    Console.WriteLine("Debug capture: none");
                                    break;
                                case 1:
                                    Threading.RendezVousEvent.CaptureTaskState = true;
                                    DebugUtil.CaptureStackTrace = false;
                                    Console.WriteLine("Debug capture: task-state only");
                                    break;
                                case 2:
                                    Threading.RendezVousEvent.CaptureTaskState = true;
                                    DebugUtil.CaptureStackTrace = true;
                                    Console.WriteLine("Debug capture: task-state and stack-trace");
                                    break;
                                }
                                break;
                            case ConsoleKey.I: {
                                    Console.WriteLine("--- System Information ---");

                                    // show memory
                                    Console.WriteLine("Allocated memory: {0}", GC.GetTotalMemory(false));

                                    // show threads
                                    int workerThreads;
                                    int completionThreads;
                                    int dispatcherThreads;
                                    AsyncUtil.GetAvailableThreads(out workerThreads, out completionThreads, out dispatcherThreads);
                                    int maxWorkerThreads;
                                    int maxCompletionThreads;
                                    int maxDispatcherThreads;
                                    AsyncUtil.GetMaxThreads(out maxWorkerThreads, out maxCompletionThreads, out maxDispatcherThreads);
                                    Console.WriteLine("Thread-pool worker threads available: {0} (max: {1})", workerThreads, maxWorkerThreads);
                                    Console.WriteLine("Thread-pool completion threads available: {0} (max: {1})", completionThreads, maxCompletionThreads);
                                    Console.WriteLine("Dispatcher threads available: {0} (max: {1})", dispatcherThreads, maxDispatcherThreads);

                                    // show pending/waiting timers
                                    var taskTimerStats = Tasking.TaskTimerFactory.GetStatistics();
                                    Console.WriteLine("Pending timer objects: {0}", taskTimerStats.PendingTimers);
                                    Console.WriteLine("Queued timer objects: {0}", taskTimerStats.QueuedTimers);
                                    Console.WriteLine("Timer retries: {0}", taskTimerStats.Retries);

                                    // show activities
                                    var activities = _host.ActivityMessages;
                                    Console.WriteLine("Host activities: {0}", activities.Length);
                                    foreach(var activity in activities) {
                                        Console.WriteLine("* {0}: {1}", activity.Created.ToString(XDoc.RFC_DATETIME_FORMAT), activity.Description);
                                    }

                                    // show pending tasks
                                    Console.WriteLine("Pending rendez-vous events: {0}", Threading.RendezVousEvent.PendingCounter);
                                    Console.WriteLine("Pending results: {0}", AResult.PendingCounter);
                                    lock(Threading.RendezVousEvent.Pending) {
                                        int count = 0;
                                        foreach(var entry in Threading.RendezVousEvent.Pending.Values) {
                                            ++count;
                                            if(entry.Key != null) {
                                                var context = entry.Key.GetState<DreamContext>();
                                                if(context != null) {
                                                    Console.WriteLine("--- DreamContext for pending rendez-vous event #{0} ---", count);
                                                    Console.WriteLine(context.Uri.ToString(false));
                                                }
                                            }
                                            Console.WriteLine();
                                            if(entry.Value != null) {
                                                Console.WriteLine("--- Stack trace for pending rendez-vous event #{0} ---", count);
                                                Console.WriteLine(entry.Value.ToString());
                                            }
                                        }
                                    }
                                    Console.WriteLine("--------------------------");
                                }
                                break;
                            case ConsoleKey.H:
                                Console.WriteLine("Help:");
                                Console.WriteLine("   Q     - quit application");
                                Console.WriteLine("   ESC   - quit application");
                                Console.WriteLine("   SPACE - quit application");
                                Console.WriteLine("   G     - full garbage collection");
                                Console.WriteLine("   C     - clear screen");
                                Console.WriteLine("   D     - set debug capture level");
                                Console.WriteLine("   I     - show system information (memory, threads, pending tasks)");
                                Console.WriteLine("   H     - this help text");
                                break;
                            }
                        }
                        #endregion
                    }
                } else {
                    _host.WaitUntilShutdown();
                }
            } finally {
                Console.WriteLine("-------------------- shutting down");
                TaskTimerFactory.ShutdownAll();
                if(_host != null) {
                    _host.Dispose();
                }
            }
            return 0;
        }

        private static void SigTermHandler() {
            Console.WriteLine("(initializing SIGTERM handler)");
            UnixSignal.WaitAny(new[] { new UnixSignal(Signum.SIGTERM) });
            TaskTimerFactory.ShutdownAll();
            _host.Dispose();
        }

        private static void WriteError(string setting, string error) {
            Console.Error.WriteLine("ERROR argument {0}: {1}", setting, error);
            Console.Error.WriteLine();
            PrintUsage();
        }

        private static void PrintUsage() {
            Console.WriteLine("MindTouch Console Host, Copyright (c) 2006-2014 MindTouch, Inc.");
            Console.WriteLine("USAGE: mindtouch.host.exe [arg1] ... [argN]");
            Console.WriteLine("    config <filename>       host configuration xml file (default: built from command line)");
            Console.WriteLine("    public-uri <uri>        public uri for server for non local:// uris (default: http://localhost:8081)");
            Console.WriteLine("    server-name <uri>       server name to use for local:// uris (default: http://localhost)");
            Console.WriteLine("    server-path <path>      path to prepend to uri for each requst received through http-listener (default: nothing)");
            Console.WriteLine("    http-port <int>         port on which Dream is listening for requests (default: 8081)");
            Console.WriteLine("    storage-dir <path>      folder for storing service state (default: host directory)");
            Console.WriteLine("    script <filename>       XML startup script");
            Console.WriteLine("    notty                   runs application as daemon, and will have to be shutdown with a REST call");
            Console.WriteLine("    host <host[:port]>      listen only on this network address and port (use 'localhost' to only allow local access)");
            Console.WriteLine("    apikey <key>            acces key to the Host service (must be non-empty)");
            Console.WriteLine("    capture-stack-trace     capture stack trace for asynchronous calls");
            Console.WriteLine("    nolog                   disable logging");
            Console.WriteLine("    connect-limit <int>     maximum # of simultaneous connections (negative: set to max threads - #, default: 0)");
            Console.WriteLine("    collect-interval <int>  interval in seconds for forcing garbage collection (off by default)");
            Console.WriteLine("    guid <guid>             set GUID for host");
            Console.WriteLine("    auth <auth-schemes>     Comma separated list of authentication mechanisms used to access this service (None by default)");
            Console.WriteLine("                            See System.Net.AuthenticationSchemes for available values.");
        }
    }
}
