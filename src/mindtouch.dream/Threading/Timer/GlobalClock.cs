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

//#define LOCKFREEE

using System;
using System.Collections.Generic;
using System.Threading;
using MindTouch.Collections;
using MindTouch.Dream;

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

        private static SingleLinkNode<NamedClockCallback> _head;

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
        public static void AddCallback(string name, ClockCallback callback) {
            if(string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException("name", "name cannot be null or empty");
            }
            if(callback == null) {
                throw new ArgumentNullException("callback");
            }

            // add callback
#if LOCKFREEE
            var newNode = new SingleLinkNode<NamedClockCallback>(new NamedClockCallback(name, callback));
            do {
                newNode.Next = _head;
            } while(!SysUtil.CAS(ref _head, newNode.Next, newNode));
#else
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
#endif
        }

        /// <summary>
        /// Remove a callback by reference.
        /// </summary>
        /// <param name="callback">Callback to remove.</param>
        public static void RemoveCallback(ClockCallback callback) {

            // remove callback
#if LOCKFREEE

            // NOTE (steveb): this code does NOT guarantee that the removed callback won't be invoked after this method call!
            var current = _head;
            while(current != null) {
                if(current.Item.Value == callback) {
                    current.Item = new NamedClockCallback(current.Item.Key, null);
                    return;
                }
                current = current.Next;
            }
#else
            lock(_syncRoot) {
                for(int i = 0; i < _callbacks.Length; ++i) {
                    if(_callbacks[i].Value == callback) {
                        _callbacks[i] = new NamedClockCallback(null, null);
                    }
                }
            }
#endif
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
#if LOCKFREEE
                SingleLinkNode<NamedClockCallback> previous = null;
                var current = _head;
                while(current != null) {
                    var key = current.Item.Key;
                    var callback = current.Item.Value;
                    if(callback == null) {

                        // remove linked node
                        if(previous == null) {

                            // there might be contention on the head item of the callback list;
                            // hence, we need to do it in a threadsafe fashion
                            SingleLinkNode<NamedClockCallback> head;
                            SingleLinkNode<NamedClockCallback> next;
                            do {
                                head = _head;
                                next = head.Next;
                            } while(!SysUtil.CAS(ref _head, head, next));
                        } else {
                            
                            // other threads don't operate on non-head items of the callback list
                            previous.Next = current.Next;
                        }

                        // clear out the item entirely to indicate we've removed it
                        current.Item = new NamedClockCallback(null, null);
                    } else {
                        try {
                            callback(now, elapsed);
                        } catch(Exception e) {
                            _log.ErrorExceptionMethodCall(e, "GlobalClock callback failed", key);
                        }
                    }
                    previous = current;
                    current = current.Next;
                }
#else
                lock(_syncRoot) {
                    var callbacks = _callbacks;
                    foreach(NamedClockCallback callback in callbacks) {
                        if(callback.Value != null) {
                            try {
                                callback.Value(now, elapsed);
                            } catch(Exception e) {
                                _log.ErrorExceptionMethodCall(e, "GlobalClock callback failed", callback.Key);
                            }
                        }
                    }
                }
#endif
            }

            // indicate that this thread has exited
            _stopped.Set();
        }
    }
}
