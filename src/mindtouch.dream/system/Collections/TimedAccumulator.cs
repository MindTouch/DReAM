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

using System.Collections.Generic;
using MindTouch;
using MindTouch.Dream;
using MindTouch.Tasking;
using log4net;

namespace System.Collections {
    public class TimedAccumulator<T> : IDisposable {

        //--- Class Fields ---
        private readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        private static List<T> ExtractItems(List<T> items, int count) {
            if(count > 0) {
                var result = items.GetRange(0, count);
                items.RemoveRange(0, count);
                return result;
            }
            return null;
        }

        //--- Fields ---
        private readonly List<T> _items = new List<T>();
        private readonly Action<IEnumerable<T>> _handler;
        private readonly int _maxItems;
        private readonly TimeSpan _autoFlushDelay;
        private readonly TaskTimer _autoFlushTimer;
        private volatile bool _disposed;

        //--- Constructors ---
        public TimedAccumulator(Action<IEnumerable<T>> handler, int maxItems, TimeSpan autoFlushDelay, TaskTimerFactory timerFactory) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            if(maxItems <= 0) {
                throw new ArgumentException("maxItems must be positive", "maxItems");
            }
            if(autoFlushDelay <= TimeSpan.Zero) {
                throw new ArgumentException("maxDelay must be positive", "autoFlushDelay");
            }
            _handler = handler;
            _maxItems = maxItems;
            _autoFlushDelay = autoFlushDelay;

            // kick off timer
            _autoFlushTimer = timerFactory.New(DateTime.MaxValue, AutoFlushCallback, null, TaskEnv.None);
        }

        //--- Methods ---
        public void Enqueue(T item) {
            if(_disposed) {
                throw new ObjectDisposedException("The TimedAccumulator has been disposed");
            }

            // add item to queue
            List<T> dispatch = null;
            lock(_items) {
                if((_items.Count == 0) && (_maxItems > 1)) {
                    _autoFlushTimer.Change(_autoFlushDelay, TaskEnv.None);
                }
                _items.Add(item);

                // check if we have enough items to dispatch some
                if(_items.Count >= _maxItems) {
                    dispatch = ExtractItems(_items, _maxItems);
                }
            }
            if(dispatch != null) {
                CallHandler(dispatch);
            }
        }

        public void Dispose() {
            if(_disposed) {
                return;
            }
            _disposed = true;

            // first we cancel the timer
            _autoFlushTimer.Cancel();

            // extract all items from the queue and dispatch them
            List<T> dispatch;
            lock(_items) {
                dispatch = ExtractItems(_items, _items.Count);
            }
            CallHandler(dispatch);
        }

        private void CallHandler(IEnumerable<T> items) {
            try {
                _handler(items);
            } catch(Exception e) {
                _log.Error("TimedAccumulator handler failed", e);
            }
        }

        private void AutoFlushCallback(TaskTimer timer) {
            if(_disposed) {
                return;
            }

            // check if we have enough items to dispatch some
            List<T> dispatch;
            lock(_items) {
                dispatch = ExtractItems(_items, _items.Count);
            }
            if(dispatch != null) {
                CallHandler(dispatch);
            }
        }
    }
}
