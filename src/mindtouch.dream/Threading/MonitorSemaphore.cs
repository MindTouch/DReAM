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
using System.Threading;

namespace MindTouch.Threading {

    /// <summary>
    /// Provides a syncrhonization class based on <see cref="Monitor"/>.
    /// </summary>
    public class MonitorSemaphore {

        //--- Fields ---
        private int _counter;

        //--- Constructors ---

        /// <summary>
        /// Create a new Monitor semaphore.
        /// </summary>
        public MonitorSemaphore() : this(0) { }

        /// <summary>
        /// Create a new monitor semaphore with a set number of watchers.
        /// </summary>
        /// <param name="initial"></param>
        public MonitorSemaphore(int initial) {
            if(initial < 0) {
                throw new ArgumentException("initial value must be non-negative", "initial");
            }
            _counter = initial;
        }

        //--- Methods ---

        /// <summary>
        /// Signals a waiting thread and unlocks it.
        /// </summary>
        public void Signal() {
            lock(this) {
                if(++_counter <= 0) {
                    Monitor.Pulse(this);
                }
            }
        }

        /// <summary>
        /// Blocks current thread until signaled.
        /// </summary>
        /// <param name="timeout"></param>
        public bool Wait(TimeSpan timeout) {
            lock(this) {
                if(--_counter < 0) {
                    return Monitor.Wait(this, (timeout == TimeSpan.MaxValue) ? Timeout.Infinite : (int)timeout.TotalMilliseconds);
                }
                return true;
            }
        }
    }
}