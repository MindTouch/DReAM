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
        private readonly TimeSpan _checkTime;
        private readonly TimeSpan _retryTime;

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private Func<DispatchItem, Result<bool>> _dequeueHandler;
        private readonly Queue<DispatchItem> _queue = new Queue<DispatchItem>();
        private readonly TaskTimer _queueTimer;
        private DispatchItem _currentItem;

        public MemoryPubSubDispatchQueue(TaskTimerFactory taskTimerFactory, TimeSpan checkTime, TimeSpan retryTime) {
            _checkTime = checkTime;
            _retryTime = retryTime;
            _queueTimer = taskTimerFactory.New(CheckQueue, null);
        }

        private void CheckQueue(TaskTimer obj) {
            ProcessNextItem();
        }

        private void ProcessNextItem() {
            lock(_queue) {
                if(_currentItem != null) {
                    return;
                }
                if(_queue.Count == 0) {
                    _queueTimer.Change(_checkTime, TaskEnv.None);
                    return;
                }
                _currentItem = _queue.Peek();
            }
            _dequeueHandler(_currentItem).WhenDone(r => {
                lock(_queue) {
                    _currentItem = null;
                    if(r.HasException || !r.Value) {
                        _queueTimer.Change(_retryTime, TaskEnv.None);
                        return;
                    }
                    _queue.Dequeue();
                }
                ProcessNextItem();
            });
        }

        public void Enqueue(DispatchItem item) {
            lock(_queue) {
                _queue.Enqueue(item);
            }
        }

        public void SetDequeueHandler(Func<DispatchItem, Result<bool>> dequeueHandler) {
            _dequeueHandler = dequeueHandler;
            _queueTimer.Change(_checkTime, TaskEnv.None);
        }
    }

}

