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

using System;
using System.Collections.Generic;
using System.Threading;
using MindTouch.Dream;
using MindTouch.Tasking;

namespace MindTouch.Threading.Timer {
    using ClockCallback = Action<DateTime, TimeSpan>;
    using NamedClockCallback = KeyValuePair<string, Action<DateTime, TimeSpan>>;

    /// <summary>
    /// Provides a global timing mechanism that accepts registration of callback to be invoked by the clock. In most cases, a
    /// <see cref="TaskTimer"/> should be used rather than registering a callback directly with the global clock.
    /// </summary>
    public static class GlobalClock {


        //--- Constants ---
        private const int INITIAL_CALLBACK_CAPACITY = 8;

        //--- Class Fields ---
        private static readonly object _syncRoot = new object();
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static volatile bool _running = true;
        private static readonly ManualResetEvent _stopped = new ManualResetEvent(false);
        private static NamedClockCallback[] _callbacks = new NamedClockCallback[INITIAL_CALLBACK_CAPACITY];
        private static readonly int _intervalMilliseconds;
        private static int _timeOffset;

        //--- Class Constructor ---
        static GlobalClock() {

            // TODO (steveb): make global clock interval configurable via app settings
            _intervalMilliseconds = 100;

            // start-up the tick thread for all global timed callbacks
            var thread = AsyncUtil.CreateThread(MasterTickThread);
            thread.Priority = ThreadPriority.Highest;
            thread.Name = "GlobalClock";
            thread.Start();
        }

        //--- Class Properties ---
        public static DateTime UtcNow { get { return DateTime.UtcNow + TimeSpan.FromMilliseconds(_timeOffset); } }

        //--- Class Methods ---

        /// <summary>
        /// Add a named callback to the clock.
        /// </summary>
        /// <param name="name">Unique key for the callback.</param>
        /// <param name="callback">Callback action.</param>
        public static void AddCallback(string name, ClockCallback callback) {
            if(string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException("name", "name cannot be null or empty");
            }
            if(callback == null) {
                throw new ArgumentNullException("callback");
            }

            // add callback
            lock(_syncRoot) {
                int index;

                // check if there is an empty slot in the callbacks array
                for(index = 0; index < _callbacks.Length; ++index) {
                    if(_callbacks[index].Value == null) {
                        _callbacks[index] = new NamedClockCallback(name, callback);
                        return;
                    }
                }

                // make room to add a new thread by doubling the array size and copying over the existing entries
                var newArray = new NamedClockCallback[2 * _callbacks.Length];
                Array.Copy(_callbacks, newArray, _callbacks.Length);

                // assign new thread
                newArray[index] = new NamedClockCallback(name, callback);

                // update instance field
                _callbacks = newArray;
            }
        }

        /// <summary>
        /// Remove a callback by reference.
        /// </summary>
        /// <param name="callback">Callback to remove.</param>
        public static void RemoveCallback(ClockCallback callback) {

            // remove callback
            lock(_syncRoot) {
                for(var i = 0; i < _callbacks.Length; ++i) {
                    if(_callbacks[i].Value == callback) {
                        _callbacks[i] = new NamedClockCallback(null, null);
                    }
                }
            }
        }

        /// <summary>
        /// Fast-forward time for the global clock.
        /// </summary>
        /// <param name="time"></param>
        public static void FastForward(TimeSpan time) {
            if(time < TimeSpan.Zero) {
                throw new ArgumentException("time cannot be negative");
            }
            Interlocked.Add(ref _timeOffset, (int)time.TotalMilliseconds);
        }

        internal static bool Shutdown(TimeSpan timeout) {

            // stop the thread timer
            _running = false;
            if((timeout != TimeSpan.MaxValue && !_stopped.WaitOne((int)timeout.TotalMilliseconds, true)) || (timeout == TimeSpan.MaxValue && !_stopped.WaitOne())
            ) {
                _log.ErrorExceptionMethodCall(new TimeoutException("GlobalClock thread shutdown timed out"), "Shutdown");
                return false;
            }
            return true;
        }

        private static void MasterTickThread() {
            var last = UtcNow;
            while(_running) {

                // wait until next iteration
                Thread.Sleep(_intervalMilliseconds);

                // get current time and calculate delta
                var now = UtcNow;
                var elapsed = now - last;
                last = now;

                // execute all callbacks
                lock(_syncRoot) {
                    var callbacks = _callbacks;
                    foreach(var callback in callbacks) {
                        if(callback.Value != null) {
                            try {
                                callback.Value(now, elapsed);
                            } catch(Exception e) {
                                _log.ErrorExceptionMethodCall(e, "GlobalClock callback failed", callback.Key);
                            }
                        }
                    }
                }
            }

            // indicate that this thread has exited
            _stopped.Set();
        }
    }
}
