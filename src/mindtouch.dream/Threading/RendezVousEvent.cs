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
using System.Threading;
using MindTouch.Tasking;

namespace MindTouch.Threading {

    /// <summary>
    /// RendezVousEvent is a lightweight synchronization primitive.  It used to align exactly one signal source to a signal receiver (i.e. a rendez-vous).
    /// The order in which the signal is set and waited on are not important.  The RendezVousEvent will remember that it was signaled and immediately
    /// trigger the receiver. <br/>
    /// The receiver must be a continuation.  The RendezVousEvent does not allow for blocking wait.
    /// </summary>
    public sealed class RendezVousEvent {

        // NOTE (steveb): 'RendezVousEvent' is as a tuple-space implementation; it is succesful only when
        //      both Signal() and Wait() have been invoked once and exactly once; the invocation order is irrelevant.

        //--- Constants ---
        private readonly static object TOKEN = new object();
        private readonly static object USED = new object();

        //--- Class Fields ---
        
        // TODO (steveb): there is a race condition where pending events get added, but not removed; the reason is that we don't have a hint that the event
        //                was already removed and hence should not be added; note that since '_pendingCounter' uses atomic operation, it is not affected.

        /// <summary>
        /// Capture the state of the task (set to <see langword="False"/> by default.)
        /// </summary>
        public static bool CaptureTaskState = false;

        /// <summary>
        /// Dictionary of pending events.
        /// </summary>
        public static readonly Dictionary<object, KeyValuePair<TaskEnv, System.Diagnostics.StackTrace>> Pending = new Dictionary<object, KeyValuePair<TaskEnv, System.Diagnostics.StackTrace>>();

        private static log4net.ILog _log = MindTouch.LogUtils.CreateLog();
        private static int _pendingCounter = 0;

        //--- Class Properties ---

        /// <summary>
        /// Returns the number of pending RendezVousEvent instances.  A pending RendezVousEvent instance has a continuation, but has not been signaled yet.
        /// </summary>
        public static int PendingCounter { get { return _pendingCounter; } }

        //--- Fields ---
        private object _placeholder;
        private IDispatchQueue _dispatchQueue;
        private System.Diagnostics.StackTrace _stacktrace = DebugUtil.GetStackTrace();
        private bool _captured;

        //--- Properties ---

        /// <summary>
        /// Returns true if the RendezVousEvent instance has been signaled and the receiver continuation has been triggered.
        /// </summary>
        public bool HasCompleted { get { return ReferenceEquals(_placeholder, USED); } }

        //--- Methods ---

        /// <summary>
        /// Ensure the receiver continuation is executed on the current thread.
        /// </summary>
        /// <exception cref="InvalidOperationException">The RendezVousEvent is already pinned to an IDispatchQueue.</exception>
        [Obsolete("PinToThread is obsolete.  Use the continuation to dispatch to the desired thread.")]
        public void PinToThread() {

            // TODO 2.0 (steveb): remove implementation

            PinTo(SynchronizationContext.Current);
        }

        /// <summary>
        /// Ensure the receiver continuation is executed on the given synchronization context.
        /// </summary>
        /// <param name="context">Synchronization context for the receiver continuation.</param>
        /// <exception cref="InvalidOperationException">The RendezVousEvent is already pinned to an IDispatchQueue.</exception>
        [Obsolete("PinTo is obsolete.  Use the continuation to dispatch to the desired thread.")]
        public void PinTo(SynchronizationContext context) {

            // TODO 2.0 (steveb): remove implementation

            PinTo(new SynchronizationDispatchQueue(context));
        }

        /// <summary>
        /// Ensure the receiver continuation is executed on the given IDispatchQueue.
        /// </summary>
        /// <param name="dispatchQueue">IDispatchQueue for the receiver continuation.</param>
        /// <exception cref="InvalidOperationException">The RendezVousEvent is already pinned to an IDispatchQueue.</exception>
        [Obsolete("PinTo is obsolete.  Use the continuation to dispatch to the desired thread.")]
        public void PinTo(IDispatchQueue dispatchQueue) {

            // TODO 2.0 (steveb): remove implementation

            if(_dispatchQueue != null) {
                throw new InvalidOperationException("RendezVousEvent is already pinned to an IDispatchQueue");
            }
            _dispatchQueue = dispatchQueue;
        }

        /// <summary>
        /// Signal the RendezVousEvent.  If a receiver continuation is present, trigger it.  
        /// Otherwise, store the signal until a continuation is registered.
        /// </summary>
        /// <exception cref="InvalidOperationException">The RendezVousEvent instance has already been signaled.</exception>
        public void Signal() {
            if(HasCompleted) {
                throw new InvalidOperationException("event has already been used");
            }
            object value = Interlocked.Exchange(ref _placeholder, TOKEN);
            if(value != null) {
                if(!(value is Action)) {
                    throw new InvalidOperationException("event has already been signaled");
                }
                Action handler = (Action)value;
                Interlocked.Decrement(ref _pendingCounter);
                if(_captured) {
                    lock(Pending) {
                        Pending.Remove(this);
                    }
                }
                _placeholder = USED;
                if(_dispatchQueue != null) {
                    _dispatchQueue.QueueWorkItem(handler);
                } else {
                    try {
                        handler();
                    } catch(Exception e) {

                        // log exception, but ignore it; outer task is immune to it
                        _log.WarnExceptionMethodCall(e, "Signal: unhandled exception in continuation");
                    }
                }
            }
        }

        /// <summary>
        /// Register the receiver continuation to activate when the RendezVousEvent instance is signaled.
        /// </summary>
        /// <param name="handler">Receiver continuation to invoke when RendezVousEvent instance is signaled.</param>
        /// <exception cref="InvalidOperationException">The RendezVousEvent instance has already a continuation.</exception>
        public void Wait(Action handler) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            if(HasCompleted) {
                throw new InvalidOperationException("event has already been used");
            }
            object token = Interlocked.Exchange(ref _placeholder, handler);
            if(token != null) {
                if(!ReferenceEquals(token, TOKEN)) {
                    throw new InvalidOperationException("event has already a continuation");
                }
                _placeholder = USED;
                if(_dispatchQueue != null) {
                    _dispatchQueue.QueueWorkItem(handler);
                } else {
                    try {
                        handler();
                    } catch(Exception e) {

                        // log exception, but ignore it; outer task is immune to it
                        _log.WarnExceptionMethodCall(e, "Wait: unhandled exception in continuation");
                    }
                }
            } else {
                Interlocked.Increment(ref _pendingCounter);
                if(CaptureTaskState) {
                    lock(Pending) {
                        if(!HasCompleted) {
                            Pending.Add(this, new KeyValuePair<TaskEnv, System.Diagnostics.StackTrace>(TaskEnv.CurrentOrNull, DebugUtil.GetStackTrace()));
                            _captured = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reset RendezVousEvent instance to its initial state without a signal and a continuation.
        /// </summary>
        public void Abandon() {
            object value = Interlocked.Exchange(ref _placeholder, null);
            if(ReferenceEquals(value, USED)) {

                // rendez-vous already happened; nothing to do
            } else if(ReferenceEquals(value, TOKEN)) {

                // only signal was set; nothing to do
            } else if(!ReferenceEquals(value, null)) {

                // decrease counter and remove stack trace if one exists
                Interlocked.Decrement(ref _pendingCounter);
                if(_captured) {
                    lock(Pending) {
                        Pending.Remove(this);
                    }
                }
            }
        }

        /// <summary>
        /// Atomically check if RendezVousEvent is already signaled.  If so, return true and mark the RendezVousEvent instance as having 
        /// completed its synchronization operation.  Otherwise, register the receiver continuation to activate when the RendezVousEvent 
        /// instance is signaled.
        /// </summary>
        /// <param name="handler">Receiver continuation to invoke when RendezVousEvent instance is signaled.</param>
        /// <returns>Returns true if RendezVousEvent instance is already signaled.</returns>
        public bool IsReadyOrWait(Action handler) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            if(HasCompleted) {
                throw new InvalidOperationException("event has already been used");
            }
            object token = Interlocked.Exchange(ref _placeholder, handler);
            if(token != null) {
                if(!ReferenceEquals(token, TOKEN)) {
                    throw new InvalidOperationException("event has already a continuation");
                }
                _placeholder = USED;
                return true;
            } else {
                Interlocked.Increment(ref _pendingCounter);
                if(CaptureTaskState) {
                    lock(Pending) {
                        if(!HasCompleted) {
                            Pending.Add(this, new KeyValuePair<TaskEnv, System.Diagnostics.StackTrace>(TaskEnv.CurrentOrNull, DebugUtil.GetStackTrace()));
                            _captured = true;
                        }
                    }
                }
            }
            return false;
        }
    }
}