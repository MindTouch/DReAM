/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
using System.Threading;
using MindTouch.Dream;

namespace MindTouch.Threading.Timer {

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
        private static KeyValuePair<string, Action<DateTime,TimeSpan>>[] _callbacks = new KeyValuePair<string, Action<DateTime,TimeSpan>>[INITIAL_CALLBACK_CAPACITY];
        private static readonly int _intervalMilliseconds;

        //--- Class Constructor ---
        static GlobalClock() {

            // TODO (steveb): make global clock interval configurable via app settings
            _intervalMilliseconds = 100;

            // start-up the tick thread for all global timed callbacks
            new Thread(MasterTickThread) { Priority = ThreadPriority.Highest, IsBackground = true, Name = "GlobalClock" }.Start();
        }

        //--- Class Methods ---

        /// <summary>
        /// Add a named callback to the clock.
        /// </summary>
        /// <param name="name">Unique key for the callback.</param>
        /// <param name="callback">Callback action.</param>
        public static void AddCallback(string name, Action<DateTime,TimeSpan> callback) {
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
                        _callbacks[index] = new KeyValuePair<string, Action<DateTime,TimeSpan>>(name, callback);
                        return;
                    }
                }

                // make room to add a new thread by doubling the array size and copying over the existing entries
                var newArray = new KeyValuePair<string, Action<DateTime,TimeSpan>>[2 * _callbacks.Length];
                Array.Copy(_callbacks, newArray, _callbacks.Length);

                // assign new thread
                newArray[index] = new KeyValuePair<string, Action<DateTime,TimeSpan>>(name, callback);

                // update instance field
                _callbacks = newArray;
            }
        }

        /// <summary>
        /// Remove a callback by reference.
        /// </summary>
        /// <param name="callback">Callback to remove.</param>
        public static void RemoveCallback(Action<DateTime,TimeSpan> callback) {

            // remove callback
            lock(_syncRoot) {
                for(int i = 0; i < _callbacks.Length; ++i) {
                    if(_callbacks[i].Value == callback) {
                        _callbacks[i] = new KeyValuePair<string, Action<DateTime, TimeSpan>>(null, null);
                    }
                }
            }
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

        private static void MasterTickThread(object _unused) {
            DateTime last = DateTime.UtcNow;
            while(_running) {

                // wait until next iteration
                Thread.Sleep(_intervalMilliseconds);

                // get current time and calculate delta
                DateTime now = DateTime.UtcNow;
                TimeSpan elapsed = now - last;
                last = now;

                // execute all callbacks
                lock(_syncRoot) {
                    var callbacks = _callbacks;
                    foreach(KeyValuePair<string, Action<DateTime, TimeSpan>> callback in callbacks) {
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
