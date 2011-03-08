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
using log4net;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Dream Directory", "Copyright (c) 2006-2011 MindTouch, Inc.", 
        Info = "http://developer.mindtouch.com/Dream/Reference/Services/Directory",
        SID = new string[] { 
            "sid://mindtouch.com/2007/03/dream/directory",
            "http://services.mindtouch.com/dream/stable/2007/03/directory" 
        }
    )]
    [DreamServiceConfig("parent", "uri?", "Uri to parent directory service for hierarchical look-up.")]
    [DreamServiceConfig("filestorage-path", "string?", "Parent directory on filesystem for storing directory records.")]
    internal class DirectoryService : DreamService {

        //--- Constants ---
        public const string TIME_TO_LIVE = "ttl";

        //--- Types ---
        public class DirectoryRecord {

            //--- Fields ---
            public string Name;
            public DateTime Expiration = DateTime.MaxValue;
            public XDoc Value;

            //--- Properties ---
            public bool HasExpiration { get { return Expiration != DateTime.MaxValue; } }

            //--- Methods ---
            public XDoc ToXDoc() {
                XDoc result = new XDoc("record");
                result.Elem("name", Name);
                if(Expiration != DateTime.MaxValue) {
                    result.Elem("expiration", Expiration);
                }
                result.Start("value").Add(Value).End();
                return result;
            }
        }

        // --- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private Plug _parent = null;
        private Dictionary<string, DirectoryRecord> _directory = new Dictionary<string, DirectoryRecord>(StringComparer.OrdinalIgnoreCase);
        private Plug _events;

        //--- Features ---
        [DreamFeature("GET:records", "Get list of all records.")]
        public Yield GetAllRecords(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc result = new XDoc("list");
            lock(_directory) {
                foreach(KeyValuePair<string, DirectoryRecord> entry in _directory) {
                    result.Add(entry.Value.ToXDoc());
                }
            }
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:records/{name}", "Get record from directory.")]
        public Yield GetRecord(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc doc = null;
            string name = context.GetSuffix(0, UriPathFormat.Normalized);
            lock(_directory) {
                DirectoryRecord record;
                if(_directory.TryGetValue(name, out record)) {
                    doc = record.Value;
                }
            }

            // check if we should look into a parent directory
            if(doc == null) {
                if(_parent != null) {
                    Result<DreamMessage> result;
                    yield return result = _parent.At("records", context.GetSuffix(0, UriPathFormat.Normalized)).Get(new Result<DreamMessage>(TimeSpan.MaxValue));
                    if(!result.Value.IsSuccessful) {

                        // respond with the error code we received
                        response.Return(result.Value);
                        yield break;
                    }
                    doc = result.Value.ToDocument();
                } else {
                    response.Return(DreamMessage.NotFound("record not found"));
                    yield break;
                }
            }
            response.Return(DreamMessage.Ok(doc));
            yield break;
        }

        [DreamFeature("PUT:records/{name}", "Add record to directory.")]
        [DreamFeatureParam("ttl", "int", "time-to-live in seconds for the added record")]
        public Yield PutRecord(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            DirectoryRecord record = new DirectoryRecord();
            record.Name = context.GetSuffix(0, UriPathFormat.Normalized);
            record.Value = request.ToDocument();
            int ttl = context.GetParam<int>(TIME_TO_LIVE, -1);
            if(ttl >= 0) {
                record.Expiration = DateTime.UtcNow.AddSeconds(ttl);
            }

            // add value to directory
            InsertRecord(record);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("DELETE:records/{name}", "Delete record from directory.")]
        public Yield DeleteRecord(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string name = context.GetSuffix(0, UriPathFormat.Normalized);
            DeleteRecord(name);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("POST:subscribe", "Subscribe to directory change notifications.")]
        public Yield PostSubscribeEvents(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            yield return context.Relay(_events.At("subscribe"), request, response);
        }

        [DreamFeature("POST:unsubscribe", "Unsubscribe from directory change notifications.")]
        public Yield PostUnsubscribeEvents(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            yield return context.Relay(_events.At("unsubscribe"), request, response);
        }

        [DreamFeature("POST:update", "Process a directory change notification")]
        public Yield Update(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc update = request.ToDocument();
            switch(update.Name.ToLowerInvariant()) {
            case "insert":
                DirectoryRecord record = new DirectoryRecord();
                record.Name = update["@name"].Contents;
                record.Expiration = update["@expire"].AsDate ?? DateTime.MaxValue;
                record.Value = update[0];
                InsertRecord(record);
                break;
            case "delete":
                string name = update["@name"].Contents;
                DeleteRecord(name);
                break;
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            _parent = Plug.New(Config["parent"].AsUri);
            yield return CreateService("events", "sid://mindtouch.com/2007/03/dream/events", null, new Result<Plug>()).Set(v => _events = v);
            LoadRecordsFromFileSystem();
            result.Return();
        }

        protected override Yield Stop(Result result) {
            if(_events != null) {
                yield return _events.Delete(new Result<DreamMessage>(TimeSpan.MaxValue)).CatchAndLog(_log);
                _events = null;
            }
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        private void InsertRecord(DirectoryRecord record) {

            // add value to directory
            lock(_directory) {
                _directory[record.Name] = record;
            }

            // check if value has an expiration time
            if(record.HasExpiration) {
                TimerFactory.New(record.Expiration, OnExpire, record.Name, TaskEnv.New());
            }

            SaveToFileSystem(record.Name, record.Value);

            // notify event channel
            XDoc notify = new XDoc("insert").Attr("name", record.Name);
            if(record.HasExpiration) {
                notify.Attr("expire", record.Expiration);
            }
            notify.Add(record.Value);
            _events.Post(notify, new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        private bool DeleteRecord(string name) {
            bool result;
            lock(_directory) {
                result = _directory.Remove(name);
            }

            DeleteFromFileSystem(name);

            // notify event channel
            _events.Post(new XDoc("delete").Attr("name", name), new Result<DreamMessage>(TimeSpan.MaxValue));
            return result;
        }

        private void OnExpire(TaskTimer timer) {
            string name = (string)timer.State;
            lock(_directory) {

                // check if the record still exists
                DirectoryRecord record;
                if(_directory.TryGetValue(name, out record)) {

                    // verify if the record should still be deleted
                    if(record.Expiration <= timer.When) {
                        _directory.Remove(record.Name);
                    } else {
                        timer.Change(record.Expiration, TaskEnv.Clone());
                    }
                }
            }
        }

        private void LoadRecordsFromFileSystem() {
            string storagePath = Config["filestorage-path"].AsText;

            if (string.IsNullOrEmpty(storagePath))
                return;

            if (!System.IO.Directory.Exists(storagePath))
                return;

            string[] files =
                System.IO.Directory.GetFiles(storagePath, "*.xml", System.IO.SearchOption.TopDirectoryOnly);

            if (files != null) {
                foreach (string file in files) {
                    try {
                        DirectoryRecord record = new DirectoryRecord();
                        record.Name = System.IO.Path.GetFileNameWithoutExtension(file);
                        record.Value = XDocFactory.LoadFrom(file, MimeType.XML);
                        _directory[record.Name] = record;
                    }
                    catch (Exception) {
                        System.IO.File.Delete(file);
                    }
                }
            }
        }

        private XDoc RetrieveFromFileSystem(string key) {

            string fullPath = BuildSavePath(key);
            
            if (fullPath != null && System.IO.File.Exists(fullPath))
                return XDocFactory.LoadFrom(fullPath, MimeType.XML);
            else
                return null;
        }

        private void SaveToFileSystem(string key, XDoc doc) {
            string fullPath = BuildSavePath(key);

            if (fullPath != null && doc != null)
                doc.Save(fullPath);
        }

        private void DeleteFromFileSystem(string key) {
            string fullPath = BuildSavePath(key);

            if (fullPath != null && System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        private string BuildSavePath(string key) {

            if (string.IsNullOrEmpty(key))
                return null;

            string storagePath = Config["filestorage-path"].AsText;

            if (string.IsNullOrEmpty(storagePath))
                return null;

            if (!System.IO.Directory.Exists(storagePath))
                return null;

            return System.IO.Path.Combine(storagePath, key + ".xml");
        }    
    }
}