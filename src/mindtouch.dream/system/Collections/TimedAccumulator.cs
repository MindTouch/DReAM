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
using System.Linq;
using MindTouch.Tasking;

namespace System.Collections {
    public class TimedAccumulator<T> : IDisposable {

        //--- Fields ---
        private readonly AutoFlushContainer<List<T>> _accumulator;

        //--- Constructors ---
        public TimedAccumulator(Action<IEnumerable<T>> handler, int maxItems, TimeSpan autoFlushDelay, TaskTimerFactory timerFactory) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            _accumulator = new AutoFlushContainer<List<T>>(
                initialState: new List<T>(),
                flush: (list, disposing) => {
                    if(list.Any()) {
                        var items = list.GetRange(0, Math.Min(list.Count, maxItems));
                        list.RemoveRange(0, items.Count);
                        handler(items);
                    }
                },
                maxUpdates: maxItems,
                autoFlushDelay: autoFlushDelay,
                timerFactory: timerFactory
            );
        }

        //--- Properties ---
        public int Count { get { return _accumulator.Get(list => list.Count); } }

        //--- Methods ---
        public void Enqueue(T item) { _accumulator.Do(list => list.Add(item)); }
        public void Dispose() { _accumulator.Dispose(); }
    }
}
