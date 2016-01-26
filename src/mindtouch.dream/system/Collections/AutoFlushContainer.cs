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

using MindTouch;
using MindTouch.Dream;
using MindTouch.Tasking;
using log4net;

namespace System.Collections {
    public class AutoFlushContainer<T> : IDisposable {

        //--- Types ---
        public delegate void FlushDelegate(T state, bool disposing);

        //--- Class Fields ---
        private readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly object _syncRoot = new object();
        private readonly T _state;
        private readonly FlushDelegate _flush;
        private readonly int _maxUpdates;
        private readonly TimeSpan _autoFlushDelay;
        private readonly TaskTimer _autoFlushTimer;
        private int _pendingUpdates;
        private bool _disposed;

        //--- Constructors ---
        public AutoFlushContainer(T initialState, FlushDelegate flush, int maxUpdates, TimeSpan autoFlushDelay, TaskTimerFactory timerFactory) {
            if(flush == null) {
                throw new ArgumentNullException("flush");
            }
            if(maxUpdates <= 0) {
                throw new ArgumentException("maxItems must be positive", "maxUpdates");
            }
            if(autoFlushDelay <= TimeSpan.Zero) {
                throw new ArgumentException("maxDelay must be positive", "autoFlushDelay");
            }
            _state = initialState;
            _flush = flush;
            _maxUpdates = maxUpdates;
            _autoFlushDelay = autoFlushDelay;
            _autoFlushTimer = timerFactory.New(DateTime.MaxValue, AutoFlushCallback, null, TaskEnv.None);
        }

        //--- Methods ---
        public void Do(Action<T> callback) {
            lock(_syncRoot) {
                if(_disposed) {
                    throw new ObjectDisposedException("instance has been disposed");
                }
                callback(_state);
                var updatesCounter = ++_pendingUpdates;
                if((updatesCounter == 1) && (_maxUpdates > 1)) {
                    _autoFlushTimer.Change(_autoFlushDelay, TaskEnv.None);
                } else if(updatesCounter == _maxUpdates) {
                    Flush();
                }
            }
        }

        public V Get<V>(Func<T, V> callback) {
            lock(_syncRoot) {
                if(_disposed) {
                    throw new ObjectDisposedException("instance has been disposed");
                }
                return callback(_state);
            }
        }

        public void Flush() {
            lock (_syncRoot) {
                if(_pendingUpdates > 0) {
                    _autoFlushTimer.Cancel();
                    _pendingUpdates = 0;
                    try {
                        _flush(_state, _disposed);
                    } catch(Exception e) {
                        _log.Error("flush handler failed", e);
                    }
                }
            }
        }

        public void Dispose() {
            lock(_syncRoot) {
                if(_disposed) {
                    return;
                }
                _disposed = true;
                Flush();
            }
        }

        private void AutoFlushCallback(TaskTimer timer) {
            lock(_syncRoot) {
                if(_disposed) {
                    return;
                }
                Flush();
            }
        }
    }
}
