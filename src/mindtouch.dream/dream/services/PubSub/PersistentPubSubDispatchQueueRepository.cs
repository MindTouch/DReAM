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
using log4net;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.Services.PubSub {
    public class PersistentPubSubDispatchQueueRepository : IPubSubDispatchQueueRepository {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly string _queueRootPath;
        private readonly TaskTimerFactory _taskTimerFactory;
        private readonly TimeSpan _retryTime;
        private readonly Dictionary<string, PersistentPubSubDispatchQueue> _repository = new Dictionary<string, PersistentPubSubDispatchQueue>();
        private readonly List<PubSubSubscriptionSet> _uninitializedSets = new List<PubSubSubscriptionSet>();
        private Func<DispatchItem, Result<bool>> _handler;

        //--- Constructors ---
        public PersistentPubSubDispatchQueueRepository(string queueRootPath, TaskTimerFactory taskTimerFactory, TimeSpan retryTime) {
            _queueRootPath = queueRootPath;
            _taskTimerFactory = taskTimerFactory;
            _retryTime = retryTime;

            if(!Directory.Exists(_queueRootPath)) {
                Directory.CreateDirectory(_queueRootPath);
            }
            foreach(var setFile in Directory.GetFiles(_queueRootPath, "*.xml")) {
                PubSubSubscriptionSet set;
                try {
                    var setDoc = XDocFactory.LoadFrom(setFile, MimeType.TEXT_XML);
                    var location = setDoc["@location"].AsText;
                    var accessKey = setDoc["@accesskey"].AsText;
                    set = new PubSubSubscriptionSet(setDoc, location, accessKey);
                } catch(Exception e) {
                    _log.Warn(string.Format("unable to retrieve and re-register subscription for location", Path.GetFileNameWithoutExtension(setFile)), e);
                    continue;
                }
                _uninitializedSets.Add(set);
            }
        }

        //--- Methods ---
        public IEnumerable<PubSubSubscriptionSet> GetUninitializedSets() {
            lock(_repository) {
                return _uninitializedSets.ToArray();
            }
        }

        public void InitializeRepository(Func<DispatchItem, Result<bool>> dequeueHandler) {
            if(dequeueHandler == null) {
                throw new ArgumentException("cannot set the handler to a null value");
            }
            if(_handler != null) {
                throw new InvalidOperationException("cannot initialize and already initialized repository");
            }
            _handler = dequeueHandler;
            lock(_repository) {
                foreach(var set in _uninitializedSets) {
                    RegisterOrUpdate(set);
                }
                _uninitializedSets.Clear();
            }
        }

        public void RegisterOrUpdate(PubSubSubscriptionSet set) {
            lock(_repository) {
                var subscriptionDocument = set.AsDocument();
                subscriptionDocument.Attr("location", set.Location).Attr("accesskey", set.AccessKey);
                subscriptionDocument.Save(Path.Combine(_queueRootPath, set.Location + ".xml"));
                if(_repository.ContainsKey(set.Location)) {
                    return;
                }

                var queue = new PersistentPubSubDispatchQueue(Path.Combine(_queueRootPath, XUri.EncodeSegment(set.Location)), _taskTimerFactory, _retryTime, _handler);
                _repository[set.Location] = queue;
            }
        }

        public void Delete(PubSubSubscriptionSet set) {
            lock(_repository) {
                PersistentPubSubDispatchQueue queue;
                if(!_repository.TryGetValue(set.Location, out queue)) {
                    return;
                }
                _repository.Remove(set.Location);
                queue.DeleteAndDispose();
                var subscriptionDocPath = Path.Combine(_queueRootPath, set.Location + ".xml");
                try {
                    File.Delete(subscriptionDocPath);
                } catch(Exception e) {
                    _log.Warn(string.Format("unable to delete subscription doc at '{0}'", subscriptionDocPath), e);
                }
            }
        }

        public IPubSubDispatchQueue this[PubSubSubscriptionSet set] {
            get {
                PersistentPubSubDispatchQueue queue;
                return _repository.TryGetValue(set.Location, out queue) ? queue : null;
            }
        }

        public void Dispose() {
            lock(_repository) {
                foreach(var tuplet in _repository.Values) {
                    tuplet.Dispose();
                }
            }
        }
    }
}