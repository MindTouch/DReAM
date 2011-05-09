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

namespace MindTouch.Dream.Services.PubSub {
    public class MemoryPubSubDispatchQueue : IPubSubDispatchQueue {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private Func<DispatchItem, Result<bool>> _dequeueHandler;
        private readonly Queue<DispatchItem> _queue = new Queue<DispatchItem>();
        private readonly TaskTimer _queueTimer;
        private readonly TimeSpan _retryTime;
        private DispatchItem _currentItem;

        public MemoryPubSubDispatchQueue(TaskTimerFactory taskTimerFactory, TimeSpan retryTime) {
            _retryTime = retryTime;
            _queueTimer = taskTimerFactory.New(RetryDequeue, null);
        }

        public void Enqueue(DispatchItem item) {
            lock(_queue) {
                _queue.Enqueue(item);
                Kick();
            }
        }

        public void SetDequeueHandler(Func<DispatchItem, Result<bool>> dequeueHandler) {
            if(dequeueHandler == null) {
                throw new ArgumentException("cannot set the handler to a null value");
            }
            _dequeueHandler = dequeueHandler;
            Kick();
        }

        private void Kick() {
            if(_currentItem != null || _dequeueHandler == null) {
                return;
            }
            lock(_queue) {
                if(_currentItem != null || _queue.Count == 0) {
                    return;
                }
                _currentItem = _queue.Peek();
                Async.Fork(TryDequeue);
            }
        }

        private void RetryDequeue(TaskTimer obj) {
            TryDequeue();
        }

        private void TryDequeue() {
            _dequeueHandler(_currentItem).WhenDone(r => {
                lock(_queue) {
                    if(r.HasException || !r.Value) {
                        _queueTimer.Change(_retryTime, TaskEnv.None);
                        return;
                    }
                    _currentItem = null;
                    _queue.Dequeue();
                    if(_queue.Count > 0) {
                        _currentItem = _queue.Peek();
                    }
                }
                if(_currentItem != null) {
                    TryDequeue();
                }
            });
        }
    }

}

