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
using System.Linq;
using MindTouch.Dream;
using MindTouch.Threading.Timer;

namespace MindTouch.Tasking {

    internal interface ITaskTimerOwner {

        //--- Methods ---
        void RemoveFromPending(TaskTimer timer);
        void RemoveFromQueue(TaskTimer timer);
        void AddToPending(TaskTimer timer, TaskEnv env, TaskTimerStatus next);
        void AddToQueue(TaskTimer timer, TaskEnv env, TaskTimerStatus next);
    }

    /// <summary>
    /// Provides a factory for creating new <see cref="TaskTimer"/> instances whose lifetime is goverened by the creating factory lifetime.
    /// </summary>
    public class TaskTimerFactory : ITaskTimerOwner, IDisposable {

        //--- Types ---

        /// <summary>
        /// Statistical information about all <see cref="TaskTimerFactory"/>.
        /// </summary>
        public class TaskTimerStatistics {

            /// <summary>
            /// Total number of <see cref="TaskTimer"/> instances pending.
            /// </summary>
            public int PendingTimers;

            /// <summary>
            /// Total number of <see cref="TaskTimer"/> instances queued.
            /// </summary>
            public int QueuedTimers;

            /// <summary>
            /// Total number of retries that have occured when trying to change <see cref="TaskTimer"/> states.
            /// </summary>
            public int Retries;

            /// <summary>
            /// Last factory tick.
            /// </summary>
            public DateTime Last;

            /// <summary>
            /// Total factory ticks.
            /// </summary>
            public int Counter;
        }

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static readonly HashSet<TaskTimerFactory> _factories = new HashSet<TaskTimerFactory>();
        private static readonly TaskTimerFactory _defaultFactory = new TaskTimerFactory();

        //--- Class Constructor ---
        static TaskTimerFactory() {
            _factories.Add(_defaultFactory);
        }

        //--- Class Properties ---

        /// <summary>
        /// The global default factory.
        /// </summary>
        public static TaskTimerFactory Default { get { return _defaultFactory; } }

        /// <summary>
        /// The currently active factory.
        /// </summary>
        public static TaskTimerFactory Current {
            get {
                var env = TaskEnv.CurrentOrNull;
                return env == null ? _defaultFactory : env.TimerFactory;
            }
        }

        /// <summary>
        /// All active factories.
        /// </summary>
        public static TaskTimerFactory[] Factories {
            get {
                lock(_factories) {
                    return _factories.ToArray();
                }
            }
        }

        //--- Class Methods ---

        /// <summary>
        /// Create a new factory.
        /// </summary>
        /// <param name="owner">
        /// The entity that this factory belongs to. Owner is used for usage tracking and is stored as a weak reference, which means
        /// that the factory will not prevent the owner from being garbage collected.
        /// </param>
        /// <returns>New factory instance.</returns>
        public static TaskTimerFactory Create(object owner) {
            if(owner == null) {
                throw new ArgumentNullException("owner", "Cannot create TaskTimerFactory without an owner");
            }
            lock(_factories) {
                var factory = new TaskTimerFactory(owner);
                _factories.Add(factory);
                return factory;
            }
        }

        /// <summary>
        /// Get statistical information about all active factories.
        /// </summary>
        /// <returns>A statics object.</returns>
        public static TaskTimerStatistics GetStatistics() {
            var statistics = new TaskTimerStatistics {
                Retries = TaskTimer.Retries,
                Last = DateTime.MinValue,
            };
            foreach(var factory in Factories) {
                statistics.PendingTimers += factory._pending.Count;
                statistics.QueuedTimers += factory._queue.Count;
                statistics.Last = factory._last > statistics.Last ? factory._last : statistics.Last;
                statistics.Counter += factory._counter;
            }
            return statistics;
        }

        /// <summary>
        /// Globally shut down all task timers and factories.
        /// </summary>
        public static void ShutdownAll() {
            foreach(var factory in Factories) {
                factory.Shutdown();
            }
        }

        private static int Compare(TaskTimer left, TaskTimer right) {
            return left.When.CompareTo(right.When);
        }

        //--- Fields ----
        private readonly WeakReference _owner;
        private readonly Type _ownerType;
        private readonly PriorityQueue<TaskTimer> _queue = new PriorityQueue<TaskTimer>(Compare);
        private readonly Dictionary<TaskTimer, object> _pending = new Dictionary<TaskTimer, object>();
        private DateTime _maintenance = DateTime.UtcNow;
        private volatile bool _running = true;
        private int _counter;
        private DateTime _last = DateTime.UtcNow;
        private volatile bool _shutdown;

        //--- Constructors ---
        private TaskTimerFactory() {
            _owner = new WeakReference(this);
            _ownerType = GetType();
            GlobalClock.AddCallback("TaskTimerFactory", Tick);
        }

        private TaskTimerFactory(object owner) {
            _owner = new WeakReference(owner);
            _ownerType = owner.GetType();
            GlobalClock.AddCallback("TaskTimerFactory", Tick);
        }

        //--- Properties ---

        /// <summary>
        /// Weak-reference accessor to factory owner (i.e. the factory will not stop the owner from being garbage collected.)
        /// </summary>
        public object Owner { get { return _owner.Target; } }

        /// <summary>
        /// Type of owner object. This value will remain populated even if the actual owner is garbage collected.
        /// </summary>
        public Type OwnerType { get { return _ownerType; } }

        /// <summary>
        /// Determine whether the owner was garbage collected. In proper usage, the owner should always have shut down the
        /// factory before it went away.
        /// </summary>
        public bool IsAbandoned { get { return !_owner.IsAlive; } }

        /// <summary>
        /// Time until the next <see cref="TaskTimer"/> maintenance.
        /// </summary>
        public TimeSpan NextMaintenance { get { return _maintenance - DateTime.UtcNow; } }

        /// <summary>
        /// A collection of all timers currently pending.
        /// </summary>
        public IEnumerable<TaskTimer> Pending {
            get {
                lock(_pending) {
                    return _pending.Keys.ToArray();
                }
            }
        }

        /// <summary>
        /// The timer that will fire next.
        /// </summary>
        public TaskTimer Next {
            get {
                lock(_queue) {
                    return _queue.Count > 0 ? _queue.Peek() : null;
                }
            }
        }

        //--- Methods ---

        /// <summary>
        /// Create a new timer.
        /// </summary>
        /// <param name="handler">The action to invoke when the timer fires.</param>
        /// <param name="state">A state object to associate with the timer.</param>
        /// <returns>New timer instance.</returns>
        public TaskTimer New(Action<TaskTimer> handler, object state) {
            return new TaskTimer(this, handler, state);
        }

        /// <summary>
        /// Create a new timer and set its fire time.
        /// </summary>
        /// <param name="when">Absolute time when the timer should fire.</param>
        /// <param name="handler">The action to invoke when the timer fires.</param>
        /// <param name="state">A state object to associate with the timer.</param>
        /// <param name="env">The environment in which the timer should fire.</param>
        /// <returns>New timer instance.</returns>
        public TaskTimer New(DateTime when, Action<TaskTimer> handler, object state, TaskEnv env) {
            var result = new TaskTimer(this, handler, state);
            result.Change(when, env);
            return result;
        }

        /// <summary>
        /// Create a new timer and set its fire time.
        /// </summary>
        /// <param name="when">Relateive time from now until when the timer should fire.</param>
        /// <param name="handler">The action to invoke when the timer fires.</param>
        /// <param name="state">A state object to associate with the timer.</param>
        /// <param name="env">The environment in which the timer should fire.</param>
        /// <returns>New timer instance.</returns>
        public TaskTimer New(TimeSpan when, Action<TaskTimer> handler, object state, TaskEnv env) {
            var result = new TaskTimer(this, handler, state);
            result.Change(when, env);
            return result;
        }

        /// <summary>
        /// Shut down all timers related to this factory.
        /// </summary>
        /// <remarks>
        /// Warning: this call is thread-blocking. It will try to execute all pending timers immediately, but
        /// will wait for each timer to complete.
        /// </remarks>
        public void Shutdown() {
            lock(_factories) {
                _factories.Remove(this);
            }
            List<KeyValuePair<TaskTimer, TaskEnv>> timers = null;

            // stop the thread timer
            _shutdown = true;
            GlobalClock.RemoveCallback(Tick);
            _owner.Target = null;

            // schedule all queued items for immediate execution
            // Note (arnec): should run the below in a helper so we can respect the timeout on this part as well
            //               (or maybe this should just be part of the thread clean-up)
            lock(_queue) {
                while(_queue.Count > 0) {
                    TaskTimer timer = _queue.Dequeue();
                    if(timer.TryLockPending()) {

                        // retrieve the associated behavior and reset the timer
                        TaskEnv env = timer.Env;
                        timer.Env = null;
                        timer.SetStatus(TaskTimerStatus.Done);

                        // add timer
                        timers = timers ?? new List<KeyValuePair<TaskTimer, TaskEnv>>();
                        timers.Add(new KeyValuePair<TaskTimer, TaskEnv>(timer, env));
                    }
                }
            }

            // BUGBUGBUG (arnec): we don't actually do anything with timeout, but let every timer take
            // an indefinite time.
            // check if any timers were gathered for immediate execution
            if(timers != null) {
                foreach(KeyValuePair<TaskTimer, TaskEnv> entry in timers) {
                    entry.Key.Execute(entry.Value);
                }
            }

            _running = false;
        }

        private void Tick(DateTime now, TimeSpan elapsed) {

            // ignore ticks that come in after we've initialized a shutdown
            if(_shutdown) {
                return;
            }

            // check if some timers are ready
            List<KeyValuePair<TaskTimer, TaskEnv>> timers = null;
            System.Threading.Interlocked.Increment(ref _counter);
            _last = now;
            lock(_queue) {

                // dequeue all timers that are ready to go
                while((_queue.Count > 0) && (_queue.Peek().When <= now)) {
                    TaskTimer timer = _queue.Dequeue();

                    // check if timer can be transitioned
                    if(timer.TryLockQueued()) {

                        // retrieve the associated behavior and reset the timer
                        TaskEnv env = timer.Env;
                        timer.Env = null;
                        timer.SetStatus(TaskTimerStatus.Done);

                        // add timer
                        timers = timers ?? new List<KeyValuePair<TaskTimer, TaskEnv>>();
                        timers.Add(new KeyValuePair<TaskTimer, TaskEnv>(timer, env));
                    }
                }

                // check if a maintance run is due
                if(_maintenance <= now) {
                    _maintenance = now.AddSeconds(TaskTimer.QUEUE_RESCAN);
                    DateTime horizon = now.AddSeconds(TaskTimer.QUEUE_CUTOFF);
                    lock(_pending) {
                        List<TaskTimer> activate = new List<TaskTimer>();
                        foreach(TaskTimer timer in _pending.Keys) {
                            if(timer.When <= horizon) {
                                activate.Add(timer);
                            }
                        }
                        foreach(TaskTimer timer in activate) {
                            _pending.Remove(timer);
                            if(timer.TryQueuePending()) {
                                _queue.Enqueue(timer);
                            }
                        }
                    }
                }
            }

            // run schedule on its own thread to avoid re-entrancy issues
            if(timers != null) {
                foreach(KeyValuePair<TaskTimer, TaskEnv> entry in timers) {
                    entry.Key.Execute(entry.Value);
                }
            }
        }

        #region --- ITaskTimerOwner Members ---
        void ITaskTimerOwner.RemoveFromPending(TaskTimer timer) {
            lock(_pending) {
                _pending.Remove(timer);
            }
        }

        void ITaskTimerOwner.RemoveFromQueue(TaskTimer timer) {
            lock(_queue) {
                _queue.Remove(timer);
            }
        }

        void ITaskTimerOwner.AddToPending(TaskTimer timer, TaskEnv env, TaskTimerStatus next) {
            env.Acquire();
            if(timer.Env != null) {
                timer.Env.Release();
            }
            if(_running) {
                lock(_pending) {
                    timer.Env = env;
                    timer.SetStatus(next);
                    _pending[timer] = null;
                }
            } else {
                env.Release();
                timer.Env = null;
                timer.SetStatus(TaskTimerStatus.Done);
            }
        }

        void ITaskTimerOwner.AddToQueue(TaskTimer timer, TaskEnv env, TaskTimerStatus next) {
            env.Acquire();
            if(timer.Env != null) {
                timer.Env.Release();
            }
            if(_running) {
                lock(_queue) {
                    timer.Env = env;
                    timer.SetStatus(next);
                    _queue.Enqueue(timer);
                }
            } else {
                env.Release();
                timer.Env = null;
                timer.SetStatus(TaskTimerStatus.Done);
            }
        }

        #endregion

        #region --- IDisposable Members ---
        /// <summary>
        /// Shutdown the factory.
        /// </summary>
        public void Dispose() {
            Shutdown();
        }
        #endregion
    }

}
