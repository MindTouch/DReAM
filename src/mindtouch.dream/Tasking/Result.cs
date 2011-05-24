/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2011 MindTouch Inc.
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
using System.Diagnostics;
using System.Threading;
using MindTouch.Collections;

using MindTouch.Dream;
using MindTouch.Threading;

namespace MindTouch.Tasking {
    /* NOTE (steveb): the following table describes all valid state transitions for Result/Result<T>
     * 
     * Methods by role:
     *     Caller: exactly one of WhenDone() or IYield.Resume()
     *     Callee: exactly one of Return(), Throw(), or ConfirmCancel(); ConfirmCancel() requires Cancel() to occur first
     *     Anyone: zero to many Cancel()
     * 
     * --- Common Cases ---
     * Return(v), WhenDone(callback)                        --> callback(v)
     * WhenDone(callback), Return(v)                        --> callback(v)
     * Throw(e), WhenDone(callback)                         --> callback(e)
     * WhenDone(callback), Throw(e)                         --> callback(e)
     * 
     * --- Timeout Cases ---
     * WhenDone(callback), ...                              --> callback(timeout)
     * WhenDone(callback), ..., Return(v)                   --> callback(timeout), cleanup(v)
     * WhenDone(callback), ..., Throw(e)                    --> callback(timeout), cleanup(e)
     * WhenDone(callback), ..., ConfirmCancel()             --> callback(timeout), cleanup(null)
     * WhenDone(callback), ..., Cancel()                    --> callback(timeout)
     * WhenDone(callback), ..., Cancel(), Return(v)         --> callback(timeout), cleanup(v)
     * WhenDone(callback), ..., Cancel(), Throw(e)          --> callback(timeout), cleanup(e)
     * WhenDone(callback), ..., Cancel(), ConfirmCancel()   --> callback(timeout), cleanup(null)
     * 
     * --- Cancel Cases ---
     * Cancel(), Return(v), WhenDone(callback)              --> callback(v)
     * WhenDone(callback), Cancel(), Return(v)              --> callback(canceled), cleanup(v)
     * Cancel(), Throw(e), WhenDone(callback)               --> callback(e)
     * WhenDone(callback), Cancel(), Throw(e)               --> callback(canceled), cleanup(e)
     * Return(v), Cancel(), WhenDone(callback)              --> callback(v)
     * WhenDone(callback), Return(v), Cancel()              --> callback(v)
     * Throw(e), Cancel(), WhenDone(callback)               --> callback(e)
     * WhenDone(callback), Throw(e), Cancel()               --> callback(e)
     * 
     * --- Observed Cancel Cases ---
     * Cancel(), HasFinished(), Return(v), WhenDone(callback) --> callback(canceled), cleanup(v)
     * Cancel(), HasFinished(), Throw(e), WhenDone(callback)  --> callback(canceled), cleanup(e)
     * Cancel(), HasFinished(), ConfirmCancel(), WhenDone(callback) --> callback(canceled), cleanup(null)
     * 
     * --- ConfirmCancel Cases ---
     * Cancel(), ConfirmCancel(), WhenDone(callback)        --> callback(canceled), cleanup(null)
     * WhenDone(callback), Cancel(), ConfirmCancel()        --> callback(canceled), cleanup(null)
     * 
     */

    /// <summary>
    /// Provides a base class for a synchronization handle for asychronous processing.
    /// </summary>
    public abstract class AResult : IYield {

        //--- Constants ---

        /// <summary>
        /// The default result timeout is 10 minutes.
        /// </summary>
        public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(10 * 60);

        //--- Types ---

        /// <summary>
        /// Possible states a result can be in.
        /// </summary>
        protected enum ResultState {

            /// <summary>
            /// New result.
            /// </summary>
            New,

            /// <summary>
            /// Result has a value (i.e. completed successfully).
            /// </summary>
            Value,

            /// <summary>
            /// Result finished with an error.
            /// </summary>
            Error,

            /// <summary>
            /// Result has been asked to be canceled.
            /// </summary>
            Cancel,

            /// <summary>
            /// Executing Result handle holder has confirmed that the cancel was accepted.
            /// </summary>
            ConfirmedCancel,

            /// <summary>
            /// A canceled Result has been observed as being canceled.
            /// </summary>
            ObservedCancel
        }

        //--- Class Fields ---

        /// <summary>
        /// Total number of pending results in process.
        /// </summary>
        protected static int _pendingCounter;
        private static LockFreeStack<AutoResetEvent> _resetEventStack = new LockFreeStack<AutoResetEvent>();


        //--- Class Properties ---

        /// <summary>
        /// Returns the number of pending AResult instances in the process.
        /// A pending AResult instance has a continuation, but has not been signaled yet.
        /// </summary>
        public static int PendingCounter { get { return _pendingCounter; } }

        //--- Fields ---

        /// <summary>
        /// The current state of the Result.
        /// </summary>
        protected ResultState _state = ResultState.New;

        /// <summary>
        /// The task environment to execute the callbacks in.
        /// </summary>
        protected TaskEnv _env;


        private TimeSpan _timeout;
        private Exception _exception;
        private Action _completion;
        private TaskTimer _timer;
        private StackTrace _stackTrace = DebugUtil.GetStackTrace();

        //--- Constructors ---

        /// <summary>
        /// Base class constructor.
        /// </summary>
        /// <param name="timeout">Result timeout.</param>
        /// <param name="env">Result environment.</param>
        protected AResult(TimeSpan timeout, TaskEnv env) {
            _timeout = timeout;
            _env = env;
        }

        //--- Abstract Properties ---

        /// <summary>
        /// Type of value that the result encapsulates (if any).
        /// </summary>
        public abstract Type ValueType { get; }

        /// <summary>
        /// Accessor to encapsulated value as an object, if set.
        /// </summary>
        public abstract object UntypedValue { get; }

        /// <summary>
        /// <see langword="True"/> if the result has a clean-up handler.
        /// </summary>
        protected abstract bool HasCleanup { get; }

        //--- Properties ---

        /// <summary>
        /// The result has an exception set on it.
        /// </summary>
        public bool HasException { get { return _exception != null; } }

        /// <summary>
        /// The result has a value set on it.
        /// </summary>
        public bool HasValue { get { return _state == ResultState.Value; } }

        /// <summary>
        /// The result has failed to complete because it timed out.
        /// </summary>
        public bool HasTimedOut { get { return IsCanceled && (_exception is TimeoutException); } }

        /// <summary>
        /// The result will not complete because it was canceled.
        /// </summary>
        public bool IsCanceled { get { return (_state == ResultState.Cancel) || (_state == ResultState.ConfirmedCancel) || (_state == ResultState.ObservedCancel); } }

        /// <summary>
        /// The action using the result as a synchronization handle has completed.
        /// </summary>
        public bool HasFinished {
            get {

                // check if Cancel has been set
                if(_state == ResultState.Cancel) {

                    // NOTE (steveb): there is a race-condition with Cancel where HasFinished informs an 
                    //                outside observer that the result has finished, but it then still 
                    //                changes its value.  Since reading the Error or Value state is not 
                    //                threadsafe for performance reasons, we have to ensure that the
                    //                outcome doesn't change once HasFinished has been called.

                    lock(this) {
                        if(_state == ResultState.Cancel) {

                            // block transitions from Cancel to Value or Error states
                            _state = ResultState.ObservedCancel;
                        }
                    }
                }
                return _state != ResultState.New;
            }
        }

        /// <summary>
        /// Amount of time this result will wait before it has to be signaled to succeed.
        /// </summary>
        public TimeSpan Timeout {
            get { return _timeout; }
            set {
                if(HasCompletion) {
                    throw new InvalidOperationException("Timeout can no longer be changed");
                }
                _timeout = value;
            }
        }

        /// <summary>
        /// The environment in which this result is being used.
        /// </summary>
        public TaskEnv Env {
            get {
                InitTaskEnv(true);
                return _env;
            }
            set {
                if(HasCompletion) {
                    throw new InvalidOperationException("Env can no longer be changed");
                }
                if(value == null) {
                    throw new ArgumentNullException("value");
                }
                _env = value;
            }
        }

        /// <summary>
        /// The exception set on the result if <see cref="HasException"/> is <see langword="True"/>.
        /// </summary>
        public Exception Exception {
            get {
                EnsureFinished();
                return _exception;
            }
            protected set {
                _exception = value;
            }
        }

        /// <summary>
        /// <see langword="True"/> if the Result has a completion callback.
        /// </summary>
        protected bool HasCompletion { get { return _completion != null; } }

        //--- Abstract Methods ---

        /// <summary>
        /// Unimplemented hook for clean-up being canceled.
        /// </summary>
        protected abstract void CallCleanupCanceled();

        /// <summary>
        /// Unimplemented hook for performing clean-up on error.
        /// </summary>
        /// <param name="exception">Exception causing the clean-up event.</param>
        protected abstract void CallCleanupError(Exception exception);

        //--- Methods ---
        #region --- Core Methods ---

        /// <summary>
        /// Confirm that the result does not have an exception. If it has a an exception, it will be rethrown by this call.
        /// </summary>
        public void Confirm() {

            // check if we have an exception to propagate
            var exception = Exception;
            if(exception != null) {
                throw exception.Rethrow();
            }
        }

        /// <summary>
        /// Set an exception on the result.
        /// </summary>
        /// <param name="exception">The exception instance to set on the result.</param>
        public void Throw(Exception exception) {
            if(exception == null) {
                throw new ArgumentNullException("exception");
            }
            exception.SetCoroutineInfo();
            SetStateError(exception);
        }

        /// <summary>
        /// Mark the handle as canceled, to notify the pending action that execution should be aborted.
        /// </summary>
        public void Cancel() {
            SetStateCancel(new CanceledException("result was canceled"));
        }

        /// <summary>
        /// Signal that the executing end has acknowledged the cancel action of the invokee.
        /// </summary>
        public void ConfirmCancel() {
            SetStateConfirmCancel();
        }

        /// <summary>
        /// Behaves as a no-op if the result has finished, otherwise throws <see cref="InvalidProgramException"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="HasFinished"/> is <see langword="True"/>.</exception>
        protected void EnsureFinished() {
            if(!HasFinished) {
                throw new InvalidOperationException("async operation has not finished");
            }
        }

        /// <summary>
        /// Try to set the state to <see cref="ResultState.Error"/>.
        /// </summary>
        /// <param name="exception">The exception to set as the error.</param>
        /// <exception cref="InvalidOperationException">Thrown if the result is already in a completed state.</exception>
        protected void SetStateError(Exception exception) {
            lock(this) {

                // check if we have an unconfirmed canceled state; if so, we may allow the current state to be replaced, 
                // or we need to call the cleanup callback with the real outcome
                switch(_state) {
                case ResultState.New:

                    // replace current status with one that has a state
                    _exception = exception;
                    _state = ResultState.Error;

                    // check if a completion callback has beens set
                    if(HasCompletion) {

                        // invoke completion callback since we have a state now
                        Interlocked.Decrement(ref _pendingCounter);
                        CallCompletion();
                    }
                    return;
                case ResultState.Cancel:
                case ResultState.ObservedCancel:

                    // check if we can replace the current outcome since completion can't have been triggered yet
                    if(!HasCompletion && (_state != ResultState.ObservedCancel)) {

                        // replace current status with real outcome since there isn't a callback yet to observe the previous one
                        _exception = exception;
                        _state = ResultState.Error;

                        // nothing further to do since we don't have a callback yet
                        return;
                    }

                    // call cleanup callback
                    CallCleanupError(exception);
                    return;
                case ResultState.ConfirmedCancel:
                case ResultState.Error:
                case ResultState.Value:

                    // can't finish twice; something is amiss!
                    throw new InvalidOperationException("async operation already finished");
                }

                // should never happen
                throw new InvalidOperationException(string.Format("async operation invalid transition from {0} to {1}", _state, ResultState.Error));
            }
        }

        /// <summary>
        /// Try to set the state to <see cref="ResultState.Cancel"/>.
        /// </summary>
        /// <param name="exception">The cancelation exception.</param>
        /// <returns><see langword="True"/> if state was set to cancel, <see langword="False"/> if the current state didn't allow cancellation or if the state was already <see cref="ResultState.Cancel"/>.</returns>
        protected bool SetStateCancel(Exception exception) {
            lock(this) {
                switch(_state) {
                case ResultState.New:
                    _exception = exception;
                    _state = ResultState.Cancel;

                    // check if a completion callback has been set
                    if(HasCompletion) {

                        // invoke completion callback since we have a state now
                        Interlocked.Decrement(ref _pendingCounter);
                        CallCompletion();
                    }
                    return true;
                case ResultState.Cancel:
                case ResultState.ConfirmedCancel:
                case ResultState.ObservedCancel:
                case ResultState.Error:
                case ResultState.Value:

                    // ignore second cancel attempt
                    return false;
                }

                // should never happen
                throw new InvalidOperationException(string.Format("async operation invalid transition from {0} to {1}", _state, ResultState.Cancel));
            }
        }

        /// <summary>
        /// Try to set the state to <see cref="ResultState.ConfirmedCancel"/>. Will throw <see cref="InvalidOperationException"/> if the state transition is invalid.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if state transition is invalid.</exception>
        protected void SetStateConfirmCancel() {
            lock(this) {
                switch(_state) {
                case ResultState.New:

                    // NOTE (steveb): we always throw, because conditional throwing is only used for TryReturn()

                    Exception e = new InvalidOperationException("async operation is not canceled (case 1)");
                    Throw(e);
                    throw e;
                case ResultState.Cancel:
                case ResultState.ObservedCancel:

                    // replace current status with real outcome since there isn't a callback yet to observe the previous one
                    _state = ResultState.ConfirmedCancel;

                    // check if completion has been triggered yet
                    if(!HasCompletion) {

                        // nothing further to do since we don't have a callback yet
                        return;
                    }

                    // call cleanup callback
                    CallCleanupCanceled();
                    return;
                case ResultState.ConfirmedCancel:
                case ResultState.Error:
                case ResultState.Value:

                    // can't finish twice; something is amiss!
                    throw new InvalidOperationException("async operation already finished");
                }

                // should never happen
                throw new InvalidOperationException(string.Format("async operation invalid transition from {0} to {1}", _state, ResultState.ConfirmedCancel));
            }
        }

        private bool SetCallback(TaskEnv environmentToAcquire, Action completion, bool callIfReady) {

            // NOTE (steveb): _env must be initialized before SetCallback() is called; unless _cleanup is null

            lock(this) {

                // check if a completion has already been set
                if(HasCompletion) {
                    throw new InvalidOperationException("async operation already has completion callback");
                }

                // check if no status has been set so far
                switch(_state) {
                case ResultState.New:

                    // update the current status
                    if(environmentToAcquire != null) {
                        environmentToAcquire.Acquire();
                    }

                    // replace current state with one that has a callback
                    _completion = completion;
                    _timer = (_timeout != TimeSpan.MaxValue) ? TaskTimerFactory.Current.New(_timeout, OnTimeout, null, TaskEnv.None) : null;

                    // nothing further to do since we don't have a state yet
                    Interlocked.Increment(ref _pendingCounter);
                    return false;
                case ResultState.Cancel:
                case ResultState.ConfirmedCancel:
                case ResultState.ObservedCancel:
                case ResultState.Error:
                case ResultState.Value:

                    // replace current status with one that has a completion callback and inherits the previous state
                    _completion = completion;

                    // check if we should call the callback immediately
                    if(callIfReady) {

                        // update the current status
                        if(environmentToAcquire != null) {
                            environmentToAcquire.Acquire();
                        }

                        // invoke completion callback since we already have a state
                        CallCompletion();
                    }

                    // check if state is a cancelation confirmation; if so, we still need to call the cleanup callback
                    if(_state == ResultState.ConfirmedCancel) {
                        CallCleanupCanceled();
                    }
                    return true;
                }

                // should never happen
                throw new InvalidOperationException(string.Format("async callback operation invalid for {0}", _state));
            }
        }

        private void OnTimeout(TaskTimer timer) {
            SetStateCancel(new TimeoutException("async operation timed out"));
        }

        private void InitTaskEnv(bool alwaysInitialize) {

            // check if environment is not yet initialized; unless requested, we only initialize _env if we also have a _cleanup callback
            if(_env == null && (alwaysInitialize || HasCleanup)) {
                _env = TaskEnv.Clone();
            }
        }

        /// <summary>
        /// Cancels any internal timer and calls the completion.
        /// </summary>
        protected void CallCompletion() {
            if(_timer != null) {
                _timer.Cancel();
            }
            _completion();
        }
        #endregion

        #region --- Synchronous Methods ---
        /// <summary>
        /// Block on the current thread until the result completes either successfully or with an exception.
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking. Please avoid using it if possible.
        /// </remarks>
        /// <returns>The current result instance.</returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking. Please avoid using it if possible.")]
#endif
        public AResult Block() {

            // block until result is available
            if(!HasFinished) {
                var monitor = new MonitorSemaphore();

                // NOTE (steveb): the continuation MUST be dispatched in-place immediately, otherwise a deadlock occurs

                // set signal when async operation has executed
                InitTaskEnv(false);
                SetCallback(null, monitor.Signal, true);

                // block until async operation has executed
                Async.WaitFor(monitor, TimeSpan.MaxValue);
            }
            return this;
        }

        /// <summary>
        /// Block on the current thread until the result completes either successfully or with an exception.
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking. Please avoid using it if possible.
        /// </remarks>
        /// <param name="signal">The reset event to use for the blocking operation.</param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking. Please avoid using it if possible.")]
#endif
        public AResult Block(AutoResetEvent signal) {

            // block until result is available
            if(!HasFinished) {
                var localSignal = signal;

                // check if a signal object needs to be created && try to get it from our stack
                if(localSignal == null) {
                    if(!_resetEventStack.TryPop(out localSignal)) {
                        localSignal = new AutoResetEvent(false);
                    }
                }

                // NOTE (steveb): the continuation MUST be dispatched in-place immediately, otherwise a deadlock occurs

                // set signal when async operation has executed
                InitTaskEnv(false);
                SetCallback(null, () => localSignal.Set(), true);

                // block until async operation has executed
                Async.WaitFor(localSignal, TimeSpan.MaxValue);
                if(signal == null) {

                    // signal was locally created, try to put it back on the stack
                    _resetEventStack.TryPush(localSignal);
                }
            }
            return this;
        }

        /// <summary>
        /// Block on the current thread until the result completes. Will throw if an exception triggers completion.
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking. Please avoid using it if possible.
        /// </remarks>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking. Please avoid using it if possible.")]
#endif
        public void Wait() {
            Block();
            Confirm();
        }

        /// <summary>
        /// Block on the current thread until the result completes. Will throw if an exception triggers completion.
        /// </summary>
        /// <param name="signal">The reset event to use for the blocking operation.</param>
        /// <remarks>
        /// WARNING: This method is thread-blocking. Please avoid using it if possible.
        /// </remarks>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking. Please avoid using it if possible.")]
#endif
        public void Wait(AutoResetEvent signal) {
            Block(signal);
            Confirm();
        }
        #endregion

        #region --- WhenDone Methods ---

        /// <summary>
        /// Register a callback handler that is invoked when the result completes.
        /// </summary>
        /// <param name="handler">Completion callback.</param>
        /// <returns>The current result instance.</returns>
        public AResult WhenDone(Action<AResult> handler) {
            return WhenDone<AResult>(handler);
        }

        /// <summary>
        /// Register a callback for result completion.
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="completion">Completion callback.</param>
        /// <returns>The result instance this method was called on.</returns>
        protected TResult WhenDone<TResult>(Action<TResult> completion) where TResult : AResult {
            InitTaskEnv(true);
            SetCallback(_env, () => _env.Invoke(completion, (TResult)this), true);
            return (TResult)this;
        }
        #endregion

        #region --- IYield Members ---
        bool IYield.CanContinueImmediately(IContinuation continuation) {

            // we use TaskEnv.Current since coroutine yielding cannot continue on the current TaskEnv anyway
            TaskEnv env = TaskEnv.Current;
            InitTaskEnv(false);
            bool result = SetCallback(env, () => env.Invoke(continuation.Continue), false);
            return result;
        }
        #endregion
    }

    /// <summary>
    /// A value-less implemenation of <see cref="AResult"/>. Use <see cref="Result{T}"/> if the invoked action needs to return a value.
    /// </summary>
    public sealed class Result : AResult {

        //--- Class Methods ---
        private static void CleanupCalled(Result result) { }

        //--- Fields ---
        private Action<Result> _cleanupCallback;
        private Result _cleanupValue;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance with the default timeout.
        /// </summary>
        public Result() : base(DEFAULT_TIMEOUT, null) { }

        /// <summary>
        /// Create a new instance with a timeout.
        /// </summary>
        /// <param name="timeout">Time to wait for completion before setting completion as a <see cref="TimeoutException"/>.</param>
        public Result(TimeSpan timeout) : base(timeout, null) { }

        /// <summary>
        /// Create a new instance with a given environment.
        /// </summary>
        /// <param name="env">Environment to use for the result invocation.</param>
        public Result(TaskEnv env) : base(DEFAULT_TIMEOUT, env) { }

        /// <summary>
        /// Create a new instance with a given environment and timeout
        /// </summary>
        /// <param name="timeout">Time to wait for completion before setting completion as a <see cref="TimeoutException"/>.</param>
        /// <param name="env">Environment to use for the result invocation.</param>
        public Result(TimeSpan timeout, TaskEnv env) : base(timeout, env) { }

        //--- Properties ---

        /// <summary>
        /// Type of value that the result encapsulates (if any).
        /// </summary>
        public override Type ValueType { get { return typeof(void); } }

        /// <summary>
        /// This method will throw <see cref="InvalidOperationException"/>, because this implementation of <see cref="AResult"/> does not have a value
        /// </summary>
        public override object UntypedValue { get { throw new InvalidOperationException("Result has 'void' as value type."); } }

        /// <summary>
        /// <see langword="True"/> if the instance has a clean-up callback.
        /// </summary>
        protected override bool HasCleanup { get { return (_cleanupCallback != null); } }

        //--- Methods ---
        #region --- Core Methods ---

        /// <summary>
        /// Register a clean-up callback to allow disposal of <see cref="IDisposable"/> resources used in invocation.
        /// </summary>
        /// <remarks>
        /// Doing clean-up in <see cref="WhenDone(System.Action{MindTouch.Tasking.Result})"/> is not safe, since the completion
        /// may have been triggered by a cancel, while the the invokee has not acknowledged the cancel and is still using the 
        /// resource.
        /// </remarks>
        /// <param name="callback">Callback action on completion of the invokee.</param>
        /// <returns>The current result index.</returns>
        public Result WithCleanup(Action<Result> callback) {
            if(callback == null) {
                throw new ArgumentNullException("callback");
            }
            lock(this) {
                if(HasCompletion) {
                    throw new InvalidOperationException("Cleanup callback can no longer be changed");
                }
                if(_cleanupCallback != null) {
                    throw new InvalidOperationException("async operation already has a cleanup callback");
                }

                // TODO (arnec): this needs to go against the environment of the cleanup (see CallCleanup)
                _env.Acquire();
                _cleanupCallback = callback;

                // check if we have a clean-up value already
                if(_cleanupValue != null) {
                    _env.Invoke(_cleanupCallback, _cleanupValue.IsCanceled ? null : _cleanupValue);
                    _cleanupCallback = CleanupCalled;
                }
            }
            return this;
        }

        /// <summary>
        /// Try to call the clean-up callback on result cancelation.
        /// </summary>
        protected override void CallCleanupCanceled() {

            // check if this is the second attempt to create a canceled result
            if(_cleanupValue != null) {
                throw new InvalidOperationException("async operation already has a value to clean up");
            }

            // check if cleanup was already called
            if(_cleanupCallback == CleanupCalled) {
                throw new InvalidOperationException("async operation was already cleaned up");
            }

            // TODO (arnec): Cleanup shouldn't really be on the Result env
            // when this is changed, also change the _env.Acquire in WithCleanup()
            _cleanupValue = new Result(TimeSpan.MaxValue, TaskEnv.None);
            _cleanupValue.SetStateCancel(null);

            // check if we have a cleanup callback to invoke
            if(_cleanupCallback != null) {
                _env.Invoke(_cleanupCallback, null);
                _cleanupCallback = CleanupCalled;
            }
        }

        /// <summary>
        /// Try to call the clean-up callback on result error.
        /// </summary>
        /// <param name="exception">Exception instance that caused the error state transition.</param>
        protected override void CallCleanupError(Exception exception) {

            // check if this is the second attempt to create a canceled result
            if(_cleanupValue != null) {
                throw new InvalidOperationException("async operation already has a value to clean up");
            }

            // check if cleanup was already called
            if(_cleanupCallback == CleanupCalled) {
                throw new InvalidOperationException("async operation was already cleaned up");
            }

            // TODO (arnec): Cleanup shouldn't really be on the Result env
            // when this is changed, also change the _env.Acquire in WithCleanup()
            _cleanupValue = new Result(TimeSpan.MaxValue, TaskEnv.None);
            _cleanupValue.SetStateError(exception);

            // check if we have a cleanup callback to invoke
            if(_cleanupCallback != null) {
                _env.Invoke(_cleanupCallback, _cleanupValue.IsCanceled ? null : _cleanupValue);
                _cleanupCallback = CleanupCalled;
            }
        }

        private bool CallCleanupValue(bool throwOnInvalidState) {

            // check if this is the second attempt to create a canceled result
            if(_cleanupValue != null) {
                if(!throwOnInvalidState) {
                    return false;
                }
                throw new InvalidOperationException("async operation already has a value to clean up");
            }

            // check if cleanup was already called
            if(_cleanupCallback == CleanupCalled) {
                if(!throwOnInvalidState) {
                    return false;
                }
                throw new InvalidOperationException("async operation was already cleaned up");
            }

            // TODO (arnec): Cleanup shouldn't really be on the Result env
            // when this is changed, also change the _env.Acquire in WithCleanup()
            _cleanupValue = new Result(TimeSpan.MaxValue, TaskEnv.None);
            _cleanupValue.SetStateValue(true);

            // check if we have a cleanup callback to invoke
            if(_cleanupCallback != null) {
                _env.Invoke(_cleanupCallback, _cleanupValue.IsCanceled ? null : _cleanupValue);
                _cleanupCallback = CleanupCalled;
            }
            return true;
        }

        private bool SetStateValue(bool throwOnInvalidState) {
            lock(this) {
                switch(_state) {
                case ResultState.New:
                    _state = ResultState.Value;

                    // check if a completion callback has been set
                    if(HasCompletion) {

                        // invoke completion callback since we have a state now
                        Interlocked.Decrement(ref _pendingCounter);
                        CallCompletion();
                    }
                    return true;
                case ResultState.Cancel:
                case ResultState.ObservedCancel:

                    // check if we can replace the current outcome since completion can't have been triggered yet
                    if(!HasCompletion && (_state != ResultState.ObservedCancel)) {

                        // replace current status with real outcome since there isn't a callback yet to observe the previous one
                        Exception = null;
                        _state = ResultState.Value;

                        // nothing further to do since we don't have a callback yet
                        return true;
                    }

                    // call cleanup callback
                    return CallCleanupValue(throwOnInvalidState);
                case ResultState.ConfirmedCancel:
                case ResultState.Error:
                case ResultState.Value:
                    if(throwOnInvalidState) {

                        // can't finish twice; something is amiss!
                        throw new InvalidOperationException("async operation already finished");
                    }
                    return false;
                }

                // should never happen
                throw new InvalidOperationException(string.Format("async operation invalid transition from {0} to {1}", _state, ResultState.Value));
            }
        }
        #endregion

        #region --- Synchronous Methods ---

        /// <summary>
        /// Block on the current thread until the result completes either successfully or with an exception.
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking. Please avoid using it if possible.
        /// </remarks>
        /// <returns>The current result instance.</returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking. Please avoid using it if possible.")]
#endif
        public new Result Block() {
            base.Block();
            return this;
        }

        /// <summary>
        /// Block on the current thread until the result completes either successfully or with an exception.
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking. Please avoid using it if possible.
        /// </remarks>
        /// <param name="signal">The reset event to use for the blocking operation.</param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking. Please avoid using it if possible.")]
#endif
        public new Result Block(AutoResetEvent signal) {
            base.Block(signal);
            return this;
        }
        #endregion

        #region -- WhenDone Methods ---

        /// <summary>
        /// Register a callback handler that is invoked when the result completes.
        /// </summary>
        /// <param name="handler">Completion callback.</param>
        /// <returns>The current result instance.</returns>
        public Result WhenDone(Action<Result> handler) {
            return base.WhenDone(handler);
        }

        /// <summary>
        /// Register a success and an error callback handler for invocation depending on result completion.
        /// </summary>
        /// <param name="success">Callback to be called on successful completion.</param>
        /// <param name="error">Callback to be called if the result completes due to an exception.</param>
        /// <returns></returns>
        public Result WhenDone(Action success, Action<Exception> error) {
            if(success == null) {
                throw new ArgumentNullException("success");
            }
            if(error == null) {
                throw new ArgumentNullException("error");
            }
            return WhenDone<Result>(
                delegate {
                    if(HasException) {
                        error(Exception);
                    } else {
                        Confirm();
                        success();
                    }
                }
            );
        }
        #endregion

        #region --- Return Methods ---

        /// <summary>
        /// Set successful completion on the result. Will throw <see cref="InvalidOperationException"/> if the result can not transition into
        /// the successful completion state.
        /// </summary>
        public void Return() {
            SetStateValue(true);
        }

        /// <summary>
        /// Try to set successful completion on the result. Unlike <see cref="Return()"/> and <see cref="Return(MindTouch.Tasking.Result)"/>
        /// does not throw.
        /// </summary>
        /// <returns><see langword="False"/> if the state transition failed.</returns>
        public bool TryReturn() {
            return SetStateValue(false);
        }

        /// <summary>
        /// Use another <see cref="Result"/>'s completion to trigger completion of this instance.
        /// </summary>
        /// <param name="result">The result instance to slave to.</param>
        public void Return(Result result) {
            result.EnsureFinished();
            if(result.HasException) {
                Throw(result.Exception);
            } else {
                Return();
            }
        }
        #endregion
    }

    /// <summary>
    /// An implemenation of <see cref="AResult"/> that has a value on successful completion.
    /// Use <see cref="Result"/> if no return value is desired.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Result<T> : AResult {

        //--- Class Methods ---
        private static void CleanupCalled(Result<T> result) { }

        //--- Fields ---
        private T _value;
        private Action<Result<T>> _cleanupCallback;
        private Result<T> _cleanupValue;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance with the default timeout.
        /// </summary>
        public Result() : base(DEFAULT_TIMEOUT, null) { }

        /// <summary>
        /// Create a new instance with a timeout.
        /// </summary>
        /// <param name="timeout">Time to wait for completion before setting completion as a <see cref="TimeoutException"/>.</param>
        public Result(TimeSpan timeout) : base(timeout, null) { }

        /// <summary>
        /// Create a new instance with a given environment.
        /// </summary>
        /// <param name="env">Environment to use for the result invocation.</param>
        public Result(TaskEnv env) : base(DEFAULT_TIMEOUT, env) { }

        /// <summary>
        /// Create a new instance with a given environment and timeout
        /// </summary>
        /// <param name="timeout">Time to wait for completion before setting completion as a <see cref="TimeoutException"/>.</param>
        /// <param name="env">Environment to use for the result invocation.</param>
        public Result(TimeSpan timeout, TaskEnv env) : base(timeout, env) { }

        //--- Properties ---

        /// <summary>
        /// Type of value that the result encapsulates (if any).
        /// </summary>
        public override Type ValueType { get { return typeof(T); } }

        /// <summary>
        /// Accessor to encapsulated value as an object, if set.
        /// </summary>
        public override object UntypedValue { get { return Value; } }

        /// <summary>
        /// <see langword="True"/> if the instance has a clean-up callback.
        /// </summary>
        protected override bool HasCleanup { get { return (_cleanupCallback != null); } }

        /// <summary>
        /// The value set on the result on successful completion.
        /// </summary>
        public T Value {
            get {
                Confirm();
                return _value;
            }
        }

        //--- Methods ---
        #region --- Core Methods ---

        /// <summary>
        /// Register a clean-up callback to allow disposal of <see cref="IDisposable"/> resources used in invocation.
        /// </summary>
        /// <remarks>
        /// Doing clean-up in <see cref="WhenDone(System.Action{MindTouch.Tasking.Result{T}})"/> is not safe, since the completion
        /// may have been triggered by a cancel, while the the invokee has not acknowledged the cancel and is still using the 
        /// resource.
        /// </remarks>
        /// <param name="callback">Callback action on completion of the invokee.</param>
        /// <returns>The current result index.</returns>
        public Result<T> WithCleanup(Action<Result<T>> callback) {
            if(callback == null) {
                throw new ArgumentNullException("callback");
            }
            lock(this) {
                if(HasCompletion) {
                    throw new InvalidOperationException("Cleanup callback can no longer be changed");
                }
                if(_cleanupCallback != null) {
                    throw new InvalidOperationException("async operation already has a cleanup callback");
                }

                // TODO (arnec): this needs to go against the environment of the cleanup (see CallCleanup)
                _env.Acquire();
                _cleanupCallback = callback;

                // check if we have a clean-up value already
                if(_cleanupValue != null) {
                    _env.Invoke(_cleanupCallback, _cleanupValue.IsCanceled ? null : _cleanupValue);
                    _cleanupCallback = CleanupCalled;
                }
            }
            return this;
        }

        /// <summary>
        /// Try to call the clean-up callback on result cancelation.
        /// </summary>
        protected override void CallCleanupCanceled() {

            // check if this is the second attempt to create a canceled result
            if(_cleanupValue != null) {
                throw new InvalidOperationException("async operation already has a value to clean up");
            }

            // check if cleanup was already called
            if(_cleanupCallback == CleanupCalled) {
                throw new InvalidOperationException("async operation was already cleaned up");
            }

            // TODO (arnec): Cleanup shouldn't really be on the Result env
            // when this is changed, also change the _env.Acquire in WithCleanup()
            _cleanupValue = new Result<T>(TimeSpan.MaxValue, TaskEnv.None);
            _cleanupValue.SetStateCancel(null);

            // check if we have a cleanup callback to invoke
            if(_cleanupCallback != null) {
                _env.Invoke(_cleanupCallback, null);
                _cleanupCallback = CleanupCalled;
            }
        }

        /// <summary>
        /// Try to call the clean-up callback on result error.
        /// </summary>
        /// <param name="exception">Exception instance that caused the error state transition.</param>
        protected override void CallCleanupError(Exception exception) {

            // check if this is the second attempt to create a canceled result
            if(_cleanupValue != null) {
                throw new InvalidOperationException("async operation already has a value to clean up");
            }

            // check if cleanup was already called
            if(_cleanupCallback == CleanupCalled) {
                throw new InvalidOperationException("async operation was already cleaned up");
            }

            // TODO (arnec): Cleanup shouldn't really be on the Result env
            // when this is changed, also change the _env.Acquire in WithCleanup()
            _cleanupValue = new Result<T>(TimeSpan.MaxValue, TaskEnv.None);
            _cleanupValue.SetStateError(exception);

            // check if we have a cleanup callback to invoke
            if(_cleanupCallback != null) {
                _env.Invoke(_cleanupCallback, _cleanupValue.IsCanceled ? null : _cleanupValue);
                _cleanupCallback = CleanupCalled;
            }
        }

        private bool CallCleanupValue(T value, bool throwOnInvalidState) {

            // check if this is the second attempt to create a canceled result
            if(_cleanupValue != null) {
                if(!throwOnInvalidState) {
                    return false;
                }
                throw new InvalidOperationException("async operation already has a value to clean up");
            }

            // check if cleanup was already called
            if(_cleanupCallback == CleanupCalled) {
                if(!throwOnInvalidState) {
                    return false;
                }
                throw new InvalidOperationException("async operation was already cleaned up");
            }

            // TODO (arnec): Cleanup shouldn't really be on the Result env
            // when this is changed, also change the _env.Acquire in WithCleanup()
            _cleanupValue = new Result<T>(TimeSpan.MaxValue, TaskEnv.None);
            _cleanupValue.SetStateValue(value, true);

            // check if we have a cleanup callback to invoke
            if(_cleanupCallback != null) {
                _env.Invoke(_cleanupCallback, _cleanupValue.IsCanceled ? null : _cleanupValue);
                _cleanupCallback = CleanupCalled;
            }
            return true;
        }

        private bool SetStateValue(T value, bool throwOnInvalidState) {
            lock(this) {
                switch(_state) {
                case ResultState.New:
                    _value = value;
                    _state = ResultState.Value;

                    // check if a completion callback has been set
                    if(HasCompletion) {

                        // invoke completion callback since we have a state now
                        Interlocked.Decrement(ref _pendingCounter);
                        CallCompletion();
                    }
                    return true;
                case ResultState.Cancel:
                case ResultState.ObservedCancel:

                    // check if we can replace the current outcome since completion can't have been triggered yet
                    if(!HasCompletion && (_state != ResultState.ObservedCancel)) {

                        // replace current status with real outcome since there isn't a callback yet to observe the previous one
                        Exception = null;
                        _value = value;
                        _state = ResultState.Value;

                        // nothing further to do since we don't have a callback yet
                        return true;
                    }

                    // call cleanup callback
                    return CallCleanupValue(value, throwOnInvalidState);
                case ResultState.ConfirmedCancel:
                case ResultState.Error:
                case ResultState.Value:
                    if(throwOnInvalidState) {

                        // can't finish twice; something is amiss!
                        throw new InvalidOperationException("async operation already finished");
                    }
                    return false;
                }

                // should never happen
                throw new InvalidOperationException(string.Format("async operation invalid transition from {0} to {1}", _state, ResultState.Value));
            }
        }
        #endregion

        #region --- Synchronous Methods ---

        /// <summary>
        /// Block on the current thread until the result completes either successfully or with an exception.
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking. Please avoid using it if possible.
        /// </remarks>
        /// <returns>The current result instance.</returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking. Please avoid using it if possible.")]
#endif
        public new Result<T> Block() {
            base.Block();
            return this;
        }

        /// <summary>
        /// Block on the current thread until the result completes either successfully or with an exception.
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking. Please avoid using it if possible.
        /// </remarks>
        /// <param name="signal">The reset event to use for the blocking operation.</param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking. Please avoid using it if possible.")]
#endif
        public new Result<T> Block(AutoResetEvent signal) {
            base.Block(signal);
            return this;
        }

#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking. Please avoid using it if possible.")]
#endif
        /// <summary>
        /// WARNING: This method is thread-blocking. Please avoid using it if possible.
        /// </summary>
        public new T Wait() {
            base.Wait();
            return Value;
        }

#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking. Please avoid using it if possible.")]
#endif
        /// <summary>
        /// WARNING: This method is thread-blocking. Please avoid using it if possible.
        /// </summary>
        public new T Wait(AutoResetEvent signal) {
            base.Wait(signal);
            return Value;
        }
        #endregion

        #region --- WhenDone Methods ---
        /// <summary>
        /// Register a callback handler that is invoked when the result completes.
        /// </summary>
        /// <param name="handler">Completion callback.</param>
        /// <returns>The current result instance.</returns>
        public Result<T> WhenDone(Action<Result<T>> handler) {
            return base.WhenDone(handler);
        }

        /// <summary>
        /// Register a success and an error callback handler for invocation depending on result completion.
        /// </summary>
        /// <param name="success">Callback to be called on successful completion.</param>
        /// <param name="error">Callback to be called if the result completes due to an exception.</param>
        /// <returns></returns>
        public Result<T> WhenDone(Action<T> success, Action<Exception> error) {
            if(success == null) {
                throw new ArgumentNullException("success");
            }
            if(error == null) {
                throw new ArgumentNullException("fail");
            }
            return WhenDone<Result<T>>(
                delegate {
                    if(HasException) {
                        error(Exception);
                    } else {
                        success(Value);
                    }
                }
            );
        }
        #endregion

        #region --- Return Methods ---
        /// <summary>
        /// Set successful completion on the result. Will throw <see cref="InvalidOperationException"/> if the result can not transition into
        /// the successful completion state.
        /// </summary>
        /// <param name="value">Value to return.</param>
        public void Return(T value) {
            SetStateValue(value, true);
        }

        /// <summary>
        /// Try to set successful completion on the result. Unlike <see cref="Return(T)"/> and <see cref="Return(MindTouch.Tasking.Result{T})"/>
        /// does not throw.
        /// </summary>
        /// <param name="value">Value to return.</param>
        /// <returns><see langword="False"/> if the state transition failed.</returns>
        public bool TryReturn(T value) {
            return SetStateValue(value, false);
        }

        /// <summary>
        /// Use another <see cref="Result"/>'s completion to trigger completion of this instance.
        /// </summary>
        /// <param name="result">The result instance to slave to.</param>
        public void Return(Result<T> result) {
            result.EnsureFinished();
            if(result.HasException) {
                Throw(result.Exception);
            } else {
                Return(result.Value);
            }
        }
        #endregion
    }
}
