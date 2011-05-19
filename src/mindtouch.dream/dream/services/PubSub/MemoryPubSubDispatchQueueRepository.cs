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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using log4net;
using MindTouch.Tasking;

namespace MindTouch.Dream.Services.PubSub {
    public class MemoryPubSubDispatchQueueRepository : IPubSubDispatchQueueRepository {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly TaskTimerFactory _taskTimerFactory;
        private readonly TimeSpan _retryTime;
        private readonly Dictionary<string, MemoryPubSubDispatchQueue> _repository = new Dictionary<string, MemoryPubSubDispatchQueue>();
        private Func<DispatchItem, Result<bool>> _handler;

        //--- Constructors ---
        public MemoryPubSubDispatchQueueRepository(TaskTimerFactory taskTimerFactory, TimeSpan retryTime) {
            //CLEANUP: check args
            _taskTimerFactory = taskTimerFactory;
            _retryTime = retryTime;
        }

        //--- Methods ---
        public IEnumerable<PubSubSubscriptionSet> Initialize(Func<DispatchItem, Result<bool>> handler) {
            if(handler == null) {
                throw new ArgumentException("cannot set the handler to a null value");
            }
            if(_handler != null) {
                throw new InvalidOperationException("cannot initialize and already initialized repository");
            }
            _handler = handler;
            return new PubSubSubscriptionSet[0];
        }

        public void RegisterOrUpdate(PubSubSubscriptionSet set) {
            lock(_repository) {
                if(_repository.ContainsKey(set.Location)) {
                    return;
                }
                var queue = new MemoryPubSubDispatchQueue(set.Location, _taskTimerFactory, _retryTime);
                //CLEANUP: Handler can be in ctor
                queue.SetDequeueHandler(_handler);
                _repository[set.Location] = queue;
            }
        }

        public void Delete(PubSubSubscriptionSet set) {
            lock(_repository) {
                MemoryPubSubDispatchQueue queue;
                if(!_repository.TryGetValue(set.Location, out queue)) {
                    return;
                }
                _repository.Remove(set.Location);
                queue.Dispose();
            }
        }

        public IPubSubDispatchQueue this[PubSubSubscriptionSet set] {
            get {
                MemoryPubSubDispatchQueue queue;
                lock(_repository) {
                    return _repository.TryGetValue(set.Location, out queue) ? queue : null;
                }
            }
        }

        public IEnumerator<IPubSubDispatchQueue> GetEnumerator() {
            lock(_repository) {
                IEnumerable<IPubSubDispatchQueue> enumerable = _repository.Values.ToArray();
                return enumerable.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void Dispose() {
            lock(_repository) {
                foreach(var queue in _repository.Values) {
                    queue.Dispose();
                }
            }
        }
    }
}