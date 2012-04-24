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
    [Obsolete("The PubSub subsystem has been deprecated and will be removed in v3.0")]
    public class MemoryPubSubDispatchQueue : IPubSubDispatchQueue {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private Func<DispatchItem, Result<bool>> _dequeueHandler;
        private readonly Queue<DispatchItem> _queue = new Queue<DispatchItem>();
        private readonly TaskTimer _queueTimer;
        private readonly string _location;
        private readonly TimeSpan _retryTime;
        private DispatchItem _currentItem;
        private DateTime _failureWindowStart = DateTime.MinValue;
        private bool _isDisposed;

        //--- Construtors
        public MemoryPubSubDispatchQueue(string location, TaskTimerFactory taskTimerFactory, TimeSpan retryTime, Func<DispatchItem, Result<bool>> handler) {
            if(string.IsNullOrEmpty(location)) {
                throw new ArgumentNullException("location");
            }
            if(taskTimerFactory == null) {
                throw new ArgumentNullException("taskTimerFactory");
            }
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            _location = location;
            _retryTime = retryTime;
            _queueTimer = taskTimerFactory.New(RetryDequeue, null);
            _dequeueHandler = handler;
        }

        //--- Fields ---
        public TimeSpan FailureWindow {
            get {
                lock(_queue) {
                    return _failureWindowStart == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - _failureWindowStart;
                }
            }
        }

        //--- Methods ---
        public void Enqueue(DispatchItem item) {
            EnsureNotDisposed();
            _queue.Enqueue(item);
            Kick();
        }

        public void Dispose() {
            if(_isDisposed) {
                return;
            }
            lock(_queue) {
                _isDisposed = true;
                _queue.Clear();
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
                if(_queue.Count == 0) {
                    return;
                }
                _currentItem = _queue.Peek();
                AsyncUtil.Fork(TryDequeue);
            }
        }

        private void EnsureNotDisposed() {
            if(_isDisposed) {
                throw new ObjectDisposedException("MemoryPubSubDispatchQueue", string.Format("Queue for subscription location '{0}' is already disposed", _location));
            }
        }

        private void RetryDequeue(TaskTimer obj) {
            TryDequeue();
        }

        private void TryDequeue() {
            _dequeueHandler(_currentItem).WhenDone(r => {
                lock(_queue) {
                    if(_isDisposed) {
                        return;
                    }
                    if(r.HasException || !r.Value) {
                        if(_failureWindowStart == DateTime.MinValue) {
                            _failureWindowStart = DateTime.UtcNow;
                        }
                        _queueTimer.Change(_retryTime, TaskEnv.None);
                        return;
                    }
                    _failureWindowStart = DateTime.MinValue;
                    _queue.Dequeue();
                    _currentItem = null;
                    if(_queue.Count == 0) {
                        return;
                    }
                    _currentItem = _queue.Peek();
                }
                TryDequeue();
            });
        }
    }

}

