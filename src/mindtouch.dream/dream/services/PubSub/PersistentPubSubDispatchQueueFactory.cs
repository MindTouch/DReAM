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
using MindTouch.Tasking;

namespace MindTouch.Dream.Services.PubSub {
    public class PersistentPubSubDispatchQueueFactory : IPersistentPubSubDispatchQueueRepository {
        private readonly string _queueRootPath;
        private readonly TaskTimerFactory _taskTimerFactory;
        private readonly TimeSpan _retryTime;
        private readonly Dictionary<string, IPubSubDispatchQueue> _repository = new Dictionary<string, IPubSubDispatchQueue>();

        public PersistentPubSubDispatchQueueFactory(string queueRootPath, TaskTimerFactory taskTimerFactory, TimeSpan retryTime) {
            _queueRootPath = queueRootPath;
            _taskTimerFactory = taskTimerFactory;
            _retryTime = retryTime;
        }

        public void Register(PubSubSubscriptionSet set, Func<DispatchItem, Result<bool>> handler) {
            var queue =new PersistentPubSubDispatchQueue(Path.Combine(_queueRootPath, XUri.EncodeSegment(set.Location)), _taskTimerFactory, _retryTime);
            queue.SetDequeueHandler(handler);
            _repository[set.Location] = queue;
        }

        public void Delete(PubSubSubscriptionSet set) {
            throw new NotImplementedException();
        }

        public IPubSubDispatchQueue this[PubSubSubscriptionSet set] {
            get { throw new NotImplementedException(); }
        }
    }
}