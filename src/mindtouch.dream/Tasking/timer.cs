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

// warning CS0420: 'MindTouch.Dream.TaskTimer._status': a reference to a volatile field will not be treated as volatile
#pragma warning disable 420

using System;
using System.Threading;
using MindTouch.Tasking;

// TODO (steveb): change namespace to MindTouch.Tasking
namespace MindTouch.Dream {

    /// <summary>
    /// Possible <see cref="TaskTimer"/> states
    /// </summary>
    public enum TaskTimerStatus {

        /// <summary>
        /// The timer has completed.
        /// </summary>
        Done,

        /// <summary>
        /// The timer is pending execution.
        /// </summary>
        Pending,

        /// <summary>
        /// The timer is queued for later execution.
        /// </summary>
        Queued,

        /// <summary>
        /// The timer is locked.
        /// </summary>
        Locked
    }

    /// <summary>
    /// Provides a mechanism for invoking an action at future time. 
    /// </summary>
    public class TaskTimer {

        //--- Constants ---
        internal const double QUEUE_CUTOFF = 30;
        internal const double QUEUE_RESCAN = 25;

        //--- Class Fields ---
        private static int _retries;

        //--- Class Properties ---

        /// <summary>
        /// Number of times the timer has retried a state change.
        /// </summary>
        public static int Retries { get { return _retries; } }

        //--- Class Methods ---

        /// <summary>
        /// This method is obsolete. Use <see cref="TaskTimerFactory.New(System.DateTime,System.Action{MindTouch.Dream.TaskTimer},object,MindTouch.Tasking.TaskEnv)"/> instead.
        /// </summary>
        /// <param name="when"></param>
        /// <param name="handler"></param>
        /// <param name="state"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        [Obsolete("New(DateTime, Action<TaskTimer>, object, TaskEnv) is obsolete.  Use TaskTimerFactory instead.")]
        public static TaskTimer New(DateTime when, Action<TaskTimer> handler, object state, TaskEnv env) {
            return TaskTimerFactory.Current.New(when, handler, state, env);
        }

        /// <summary>
        /// This methods is obsolete. Use <see cref="TaskTimerFactory.New(System.TimeSpan,System.Action{MindTouch.Dream.TaskTimer},object,MindTouch.Tasking.TaskEnv)"/> instead.
        /// </summary>
        /// <param name="when"></param>
        /// <param name="handler"></param>
        /// <param name="state"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        [Obsolete("New(TimeSpan, Action<TaskTimer>, object, TaskEnv) is obsolete.  Use TaskTimerFactory instead.")]
        public static TaskTimer New(TimeSpan when, Action<TaskTimer> handler, object state, TaskEnv env) {
            return TaskTimerFactory.Current.New(when, handler, state, env);
        }

        /// <summary>
        /// This method is obsolete. Use <see cref="TaskTimerFactory.Shutdown()"/>  instead.
        /// </summary>
        [Obsolete("Shutdown() is obsolete.  Use TaskTimerFactory instead.")]
        public static void Shutdown() {
            TaskTimerFactory.Current.Shutdown();
        }

        //--- Fields ---

        /// <summary>
        /// State object.
        /// </summary>
        public readonly object State;

        internal TaskEnv Env = null;
        private readonly ITaskTimerOwner _owner;
        private volatile int _status = (int)TaskTimerStatus.Done;
        private readonly Action<TaskTimer> _handler;
        private DateTime _when;

        //--- Constructors ---
        internal TaskTimer(ITaskTimerOwner owner, Action<TaskTimer> handler, object state) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            _owner = owner;
            this.State = state;
            _handler = handler;
        }

        /// <summary>
        /// This constructor is obsolete. Use <see cref="TaskTimerFactory.New(System.Action{MindTouch.Dream.TaskTimer},object)"/> instead.
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="state"></param>
        [Obsolete("TaskTimer(Action<TaskTimer>, object) is obsolete.  Use TaskTimerFactory instead.")]
        public TaskTimer(Action<TaskTimer> handler, object state) : this(TaskTimerFactory.Current, handler, state) { }

        //--- Properties ---

        /// <summary>
        /// The time when the timer is scheduled to fire.
        /// </summary>
        public DateTime When { get { return _when; } }

        /// <summary>
        /// Timer status.
        /// </summary>
        public TaskTimerStatus Status { get { return (TaskTimerStatus)_status; } }

        /// <summary>
        /// The action that will be invoked at fire time.
        /// </summary>
        public Action<TaskTimer> Handler { get { return _handler; } }

        //--- Methods ---

        /// <summary>
        /// Change when the timer will execute.
        /// </summary>
        /// <param name="timespan">The relative time.</param>
        /// <param name="env">The environment to use for invocation.</param>
        public void Change(TimeSpan timespan, TaskEnv env) {
            if(timespan != TimeSpan.MaxValue) {
                Change(DateTime.UtcNow.Add(timespan), env);
            } else {
                Change(DateTime.MaxValue, env);
            }
        }

        /// <summary>
        /// Change when the timer will execute.
        /// </summary>
        /// <param name="when">The absolute time.</param>
        /// <param name="env">The environment to use for invocation.</param>
        public void Change(DateTime when, TaskEnv env) {
            DateTime now = DateTime.UtcNow;

            // determine new status
            int next;
            if(when <= now.AddSeconds(QUEUE_CUTOFF)) {
                next = (int)TaskTimerStatus.Queued;
            } else if(when < DateTime.MaxValue) {
                next = (int)TaskTimerStatus.Pending;
            } else {
                next = (int)TaskTimerStatus.Done;
            }

            // ensure we have a behavior if we need one and we don't if we do not
            if(next != (int)TaskTimerStatus.Done) {
                if(env == null) {
                    throw new ArgumentNullException("env");
                }
            } else {
                env = null;
            }

            // attempt to change current status
        retry:
            int current;
            switch(_status) {
            case (int)TaskTimerStatus.Done:

                // nothing to do
                break;
            case (int)TaskTimerStatus.Pending:

                // attempt to remove timer from pending list
                current = Interlocked.CompareExchange(ref _status, (int)TaskTimerStatus.Done, (int)TaskTimerStatus.Pending);
                switch(current) {
                case (int)TaskTimerStatus.Done:

                    // nothing to do
                    break;
                case (int)TaskTimerStatus.Pending:

                    // remove timer from pending list
                    _owner.RemoveFromPending(this);
                    break;
                case (int)TaskTimerStatus.Queued:

                    // we changed states; retry
                    Interlocked.Increment(ref _retries);
                    goto retry;
                case (int)TaskTimerStatus.Locked:

                    // somebody else is already changing the timer; no need to compete
                    return;
                }
                break;
            case (int)TaskTimerStatus.Queued:

                // attempto remove timer from queue
                current = Interlocked.CompareExchange(ref _status, (int)TaskTimerStatus.Done, (int)TaskTimerStatus.Queued);
                switch(current) {
                case (int)TaskTimerStatus.Done:

                    // nothing to do
                    break;
                case (int)TaskTimerStatus.Pending:

                    // we changed states; retry
                    Interlocked.Increment(ref _retries);
                    goto retry;
                case (int)TaskTimerStatus.Queued:

                    // remove timer from queue
                    _owner.RemoveFromQueue(this);
                    break;
                case (int)TaskTimerStatus.Locked:

                    // somebody else is already changing the timer; no need to compete
                    return;
                }
                break;
            case (int)TaskTimerStatus.Locked:

                // somebody else is already changing the timer; no need to compete
                return;
            }

            // register timer according to new status
            if(Interlocked.CompareExchange(ref _status, (int)TaskTimerStatus.Locked, (int)TaskTimerStatus.Done) == (int)TaskTimerStatus.Done) {
                _when = when;
                switch(next) {
                case (int)TaskTimerStatus.Done:

                    // release Task Environment
                    if(Env != null) {
                        Env.Release();
                    }
                    Env = null;
                    Interlocked.Exchange(ref _status, next);
                    return;
                case (int)TaskTimerStatus.Pending:

                    // add timer to pending list
                    _owner.AddToPending(this, env, (TaskTimerStatus)next);
                    break;
                case (int)TaskTimerStatus.Queued:

                    // add timer to active queue
                    _owner.AddToQueue(this, env, (TaskTimerStatus)next);
                    break;
                case (int)TaskTimerStatus.Locked:
                    Interlocked.Exchange(ref _status, (int)TaskTimerStatus.Done);
                    throw new InvalidOperationException("should never happen");
                }
            }
        }

        /// <summary>
        /// Cancel the scheduled invocation.
        /// </summary>
        public void Cancel() {
            Change(DateTime.MaxValue, (TaskEnv)null);
        }

        internal bool TryLockPending() {
            return (Interlocked.CompareExchange(ref _status, (int)TaskTimerStatus.Locked, (int)TaskTimerStatus.Pending) == (int)TaskTimerStatus.Pending);
        }

        internal bool TryLockQueued() {
            return (Interlocked.CompareExchange(ref _status, (int)TaskTimerStatus.Locked, (int)TaskTimerStatus.Queued) == (int)TaskTimerStatus.Queued);
        }

        internal bool TryQueuePending() {
            return (Interlocked.CompareExchange(ref _status, (int)TaskTimerStatus.Queued, (int)TaskTimerStatus.Pending) == (int)TaskTimerStatus.Pending);
        }

        internal void Execute(TaskEnv env) {
            env.Invoke(_handler, this);
        }

        internal void SetStatus(TaskTimerStatus status) {
            Interlocked.Exchange(ref _status, (int)status);
        }
    }
}
