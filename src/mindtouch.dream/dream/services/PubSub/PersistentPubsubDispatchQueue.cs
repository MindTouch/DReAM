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
using log4net;
using MindTouch.Collections;
using MindTouch.IO;
using MindTouch.Tasking;

namespace MindTouch.Dream.Services.PubSub {
    public class PersistentPubSubDispatchQueue : IPubSubDispatchQueue {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private Func<DispatchItem, Result<bool>> _dequeueHandler;
        private readonly ITransactionalQueue<DispatchItem> _queue;
        private readonly TaskTimer _queueTimer;
        private readonly TimeSpan _retryTime;
        private ITransactionalQueueEntry<DispatchItem> _currentItem;
        private DateTime _failureWindowStart = DateTime.MinValue;

        //--- Construtors
        public PersistentPubSubDispatchQueue(string queuePath, TaskTimerFactory taskTimerFactory, TimeSpan retryTime) {
            _retryTime = retryTime;
            _queueTimer = taskTimerFactory.New(RetryDequeue, null);
            _queue = new TransactionalQueue<DispatchItem>(new MultiFileQueueStream(queuePath), new DispatchItemSerializer()) {
                DefaultCommitTimeout = TimeSpan.MaxValue
            };
        }

        //--- Fields ---
        public TimeSpan FailureWindow {
            get {
                var failureWindowStart = _failureWindowStart;
                return failureWindowStart == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - failureWindowStart;
            }
        }

        //--- Methods ---
        public void Enqueue(DispatchItem item) {
            _queue.Enqueue(item);
            Kick();
        }

        public void SetDequeueHandler(Func<DispatchItem, Result<bool>> dequeueHandler) {
            if(dequeueHandler == null) {
                throw new ArgumentException("cannot set the handler to a null value");
            }
            _dequeueHandler = dequeueHandler;
            Kick();
        }

        public void Dispose() {
            lock(_queue) {
                _queue.Dispose();
                _dequeueHandler = null;
            }
        }

        private void Kick() {
            if(_currentItem != null || _dequeueHandler == null) {
                return;
            }
            lock(_queue) {
                if(_currentItem != null) {
                    return;
                }
                _currentItem = _queue.Dequeue();
                if(_currentItem == null) {
                    return;
                }
                Async.Fork(TryDequeue);
            }
        }

        private void RetryDequeue(TaskTimer obj) {
            TryDequeue();
        }

        private void TryDequeue() {
            _dequeueHandler(_currentItem.Value).WhenDone(r => {
                lock(_queue) {
                    if(r.HasException || !r.Value) {
                        _failureWindowStart = DateTime.UtcNow;
                        _queueTimer.Change(_retryTime, TaskEnv.None);
                        return;
                    }
                    _failureWindowStart = DateTime.MinValue;
                    _queue.CommitDequeue(_currentItem.Id);
                    _currentItem = _queue.Dequeue();
                }

                // TODO (arnec): race condition?
                if(_currentItem != null) {
                    TryDequeue();
                }
            });
        }
    }

}

