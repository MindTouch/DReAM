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
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

using MindTouch.Xml;

namespace MindTouch.Dream {
    public partial class WindowsServiceHost : ServiceBase {

        //--- Constants ---
        private const string DREAM_SOURCE = "MindTouch Dream";

        //--- Fields ---
        private DreamHost _host;

        //--- Constructors ---
        public WindowsServiceHost() {
            InitializeComponent();
            if(!EventLog.SourceExists(DREAM_SOURCE)) {
                EventLog.CreateEventSource(DREAM_SOURCE, "Application");
            }
            _sysEventLog.Source = DREAM_SOURCE;
            _sysEventLog.Log = "Application";
        }

        //--- Methods ---
        protected override void OnStart(string[] args) {
            TimeSpan time;
            _sysEventLog.WriteEntry("host service starting", EventLogEntryType.Information);

            // TODO (steveb): make settings file name & location configurable (use app settings)
            string baseFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);
            string startupFile = Path.Combine(baseFolder, "mindtouch.dream.startup.xml");
            XDoc settings = XDoc.Empty;
            settings = XDocFactory.LoadFrom(startupFile, MimeType.XML);
            if(settings == null) {
                throw new Exception("invalid settings file");
            }

            // create environment
            time = DebugUtil.Stopwatch(() => {
                _host = new DreamHost(settings);
            });
            _sysEventLog.WriteEntry(string.Format("ApiKey: {0}", _host.Self.Uri.GetParam("apikey")), EventLogEntryType.Information);
            _sysEventLog.WriteEntry(string.Format("initialized {0} secs", time.TotalSeconds), EventLogEntryType.Information);

            // execute all scripts
            time = DebugUtil.Stopwatch(() => {
                _host.RunScripts(settings, null);
            });
            _sysEventLog.WriteEntry(string.Format("ready {0} secs", time.TotalSeconds), EventLogEntryType.Information);
        }

        protected override void OnStop() {
            _sysEventLog.WriteEntry("host service stopping", EventLogEntryType.Information);
            try {
                _host.Dispose();
            } finally {
                _host = null;
            }
        }
    }
}
