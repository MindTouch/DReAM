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
using MindTouch.Dream;
using MindTouch.Threading;
using System.Linq;

namespace MindTouch.Tasking {

    /// <summary>
    /// Environment propagated along with a task as it transitions between threads.
    /// </summary>
    public class TaskEnv : Dictionary<object, object> {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        [ThreadStatic]
        private static TaskEnv _currentEnv;

        //--- Class Properties ---

        /// <summary>
        /// Get the instantaneous execution environment.
        /// </summary>
        public static TaskEnv Instantaneous { get { return new TaskEnv(null, CurrentTimerFactoryOrNull); } }

        /// <summary>
        /// Get the environment-less marker value.
        /// </summary>
        public static TaskEnv None { get { return new TaskEnv(Async.GlobalDispatchQueue, CurrentTimerFactoryOrNull); } }

        /// <summary>
        /// Get the current task environment. Returns <see langword="null"/> if there is no current environment.
        /// </summary>
        public static TaskEnv CurrentOrNull { get { return _currentEnv; } }

        /// <summary>
        /// Get the current task environment. Will create a new task environment if none exists.
        /// </summary>
        public static TaskEnv Current {
            get {
                if(_currentEnv == null) {
                    _currentEnv = new TaskEnv(Async.CurrentDispatchQueue, null);
                }
                return _currentEnv;
            }
        }

        private static TaskTimerFactory CurrentTimerFactoryOrNull { get { return _currentEnv == null ? null : _currentEnv._taskTimerFactory; } }

        //--- Class Methods ---

        /// <summary>
        /// Create a new environment.
        /// </summary>
        /// <param name="dispatchQueue">Dispatch queue to use for the environment.</param>
        /// <returns>A new <see cref="TaskEnv"/> instance.</returns>
        public static TaskEnv New(IDispatchQueue dispatchQueue) {
            if(dispatchQueue == null) {
                throw new ArgumentNullException("dispatchQueue");
            }
            return new TaskEnv(dispatchQueue, CurrentTimerFactoryOrNull);
        }

        /// <summary>
        /// Clone the current task environment with a provided dispatch queue.
        /// </summary>
        /// <param name="dispatchQueue">Dispatch queue to use for the environment.</param>
        /// <returns>A new <see cref="TaskEnv"/> instance.</returns>
        public static TaskEnv Clone(IDispatchQueue dispatchQueue) {
            if(dispatchQueue == null) {
                throw new ArgumentNullException("dispatchQueue");
            }
            return new TaskEnv(_currentEnv, dispatchQueue, CurrentTimerFactoryOrNull);
        }

        /// <summary>
        /// Create a new environment.
        /// </summary>
        /// <param name="dispatchQueue">Dispatch queue to use for the environment.</param>
        /// <param name="taskTimerFactory"><see cref="TaskTimer"/> factory to use for the environment.</param>
        /// <returns>A new <see cref="TaskEnv"/> instance.</returns>
        public static TaskEnv New(IDispatchQueue dispatchQueue, TaskTimerFactory taskTimerFactory) {
            if(dispatchQueue == null) {
                throw new ArgumentNullException("dispatchQueue");
            }
            return new TaskEnv(dispatchQueue, taskTimerFactory);
        }

        /// <summary>
        ///  Clone the current task environment with a provided dispatch queue and task timer factory.
        /// </summary>
        /// <param name="dispatchQueue">Dispatch queue to use for the environment.</param>
        /// <param name="taskTimerFactory"><see cref="TaskTimer"/> factory to use for the environment.</param>
        /// <returns>A new <see cref="TaskEnv"/> instance.</returns>
        public static TaskEnv Clone(IDispatchQueue dispatchQueue, TaskTimerFactory taskTimerFactory) {
            if(dispatchQueue == null) {
                throw new ArgumentNullException("dispatchQueue");
            }
            return new TaskEnv(_currentEnv, dispatchQueue, taskTimerFactory);
        }

        /// <summary>
        /// Create a new environment.
        /// </summary>
        /// <param name="taskTimerFactory"><see cref="TaskTimer"/> factory to use for the environment.</param>
        /// <returns>A new <see cref="TaskEnv"/> instance.</returns>
        public static TaskEnv New(TaskTimerFactory taskTimerFactory) {
            return new TaskEnv(Async.GlobalDispatchQueue, taskTimerFactory);
        }

        /// <summary>
        ///  Clone the current task environment with a task timer factory.
        /// </summary>
        /// <param name="taskTimerFactory"><see cref="TaskTimer"/> factory to use for the environment.</param>
        /// <returns>A new <see cref="TaskEnv"/> instance.</returns>
        public static TaskEnv Clone(TaskTimerFactory taskTimerFactory) {
            return new TaskEnv(_currentEnv, Async.GlobalDispatchQueue, taskTimerFactory);
        }

        /// <summary>
        /// Create a new environment.
        /// </summary>
        /// <returns>A new <see cref="TaskEnv"/> instance.</returns>
        public static TaskEnv New() {
            return new TaskEnv(Async.GlobalDispatchQueue, CurrentTimerFactoryOrNull);
        }
        /// <summary>
        /// Clone the current task environment.
        /// </summary>
        /// <returns>A new <see cref="TaskEnv"/> instance.</returns>
        public static TaskEnv Clone() {
            return new TaskEnv(_currentEnv, Async.GlobalDispatchQueue, CurrentTimerFactoryOrNull);
        }

        /// <summary>
        /// Execute an action in a new environment.
        /// </summary>
        /// <param name="handler">Action to execute.</param>
        /// <returns><see langword="null"/> if the handler was executed sucessfully, the captured exception otherwise.</returns>
        public static Exception ExecuteNew(Action handler) {

            // TODO (arnec): Investigate if callers really still need New env
            return ExecuteNew(handler, null);
        }

        /// <summary>
        /// Execute an action in a new environment.
        /// </summary>
        /// <param name="handler">Action to execute.</param>
        /// <param name="timerFactory">The <see cref="TaskTimer"/> factory to use in the execution environment.</param>
        /// <returns><see langword="null"/> if the handler was executed sucessfully, the captured exception otherwise.</returns>
        public static Exception ExecuteNew(Action handler, TaskTimerFactory timerFactory) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            var env = New(timerFactory);
            env.Acquire();
            var exception = env.InvokeNow(handler);
            env.Release();
            return exception;
        }

        //--- Fields ---
        private object _syncRoot = new object();
        private int _referenceCount;
        private readonly IDispatchQueue _dispatchQueue;
        private TaskTimerFactory _taskTimerFactory;

        //--- Constructors ---
        private TaskEnv(IDispatchQueue dispatchQueue, TaskTimerFactory taskTimerFactory) {
            _taskTimerFactory = taskTimerFactory;
            _dispatchQueue = (dispatchQueue is ImmediateDispatchQueue) ? null : dispatchQueue;
        }

        private TaskEnv(TaskEnv env, IDispatchQueue dispatchQueue, TaskTimerFactory taskTimerFactory) {
            if(env != null) {
                foreach(var entry in env) {
                    var cloneable = entry.Value as ITaskLifespan;
                    Add(entry.Key, cloneable == null ? entry.Value : cloneable.Clone());
                }
            }
            _taskTimerFactory = taskTimerFactory;
            _dispatchQueue = (dispatchQueue is ImmediateDispatchQueue) ? null : dispatchQueue;
        }

        //--- Properties ---

        /// <summary>
        /// Dispatch queue of the environment.
        /// </summary>
        public IDispatchQueue DispatchQueue { get { return _dispatchQueue; } }

        /// <summary>
        /// <see cref="TaskTimer"/> factory used by the environment.
        /// </summary>
        public TaskTimerFactory TimerFactory {
            get {
                if(_taskTimerFactory == null) {
                    _taskTimerFactory = TaskTimerFactory.Default;
                }
                return _taskTimerFactory;
            }
        }

        //--- Methods ---

        /// <summary>
        /// Retrieve a typed state variable.
        /// </summary>
        /// <typeparam name="T">Type of the state variable.</typeparam>
        /// <returns>Value for the type, or type default if the variable is not set.</returns>
        public T GetState<T>() {
            return GetState<T>(typeof(T));
        }

        /// <summary>
        /// Set a typed state variable.
        /// </summary>
        /// <typeparam name="T">Type of the state variable.</typeparam>
        /// <param name="value">Value to set.</param>
        public void SetState<T>(T value) {
            this[typeof(T)] = value;
        }

        /// <summary>
        /// Remove a typed state variable
        /// </summary>
        /// <remarks>Throws <see cref="InvalidOperationException"/> if the given value is different from the one stored for the given type.</remarks>
        /// <typeparam name="T">Type of the state variable.</typeparam>
        /// <param name="value">Value to remove.</param>
        /// <returns><see langword="False"/> if the value was not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the given value is different from the one stored for the given type.</exception>
        public bool RemoveState<T>(T value) {
            object current;
            var t = typeof(T);
            if(!TryGetValue(t, out current)) {
                return false;
            }
            if(current != (object)value) {
                throw new InvalidOperationException("The instance provided was not the instance currently stored as state");
            }
            return Remove(typeof(T));
        }

        /// <summary>
        /// Retrieve a keyed state variable.
        /// </summary>
        /// <typeparam name="T">Type of the state variable.</typeparam>
        /// <param name="key">Key the variable is indexed under.</param>
        /// <returns>Value for the type, or type default if the variable is not set.</returns>
        public T GetState<T>(string key) {
            return GetState<T>((object)key);
        }

        /// <summary>
        /// Set a keyed state variable.
        /// </summary>
        /// <typeparam name="T">Type of the state variable.</typeparam>
        /// <param name="key">Key the variable is indexed under.</param>
        /// <param name="value">Value to set.</param>
        public void SetState<T>(string key, T value) {
            this[key] = value;
        }

        /// <summary>
        /// Retrieve a keyed state variable.
        /// </summary>
        /// <typeparam name="T">Type of the state variable.</typeparam>
        /// <param name="key">Key the variable is indexed under.</param>
        /// <returns>Value for the type, or type default if the variable is not set.</returns>
        private T GetState<T>(object key) {
            object value;
            return TryGetValue(key, out value) ? (T)value : default(T);
        }

        /// <summary>
        /// Rmove a keyed state variable.
        /// </summary>
        /// <param name="key">Key the variable is indexed under.</param>
        /// <returns><see langword="False"/> if the key was not found.</returns>
        public bool RemoveState(object key) {
            return Remove(key);
        }

        /// <summary>
        /// Acquire the environment, prevent it from being disposed.
        /// </summary>
        public void Acquire() {
            int current;
            lock(_syncRoot) {
                current = ++_referenceCount;
            }
            if(current < 1) {
                _log.WarnFormat("Illegal TaskEnv reference count after acquisition: {0}", current);
                throw new InvalidOperationException(string.Format("Illegal TaskEnv reference count after acquisition: {0}", current));
            }
        }

        /// <summary>
        /// Release the environment, possibly making it eligble for disposal.
        /// </summary>
        public void Release() {
            int current;
            IEnumerable<object> values;
            lock(_syncRoot) {
                current = --_referenceCount;
                if(current > 0) {
                    return;
                }
                values = BeginReset();
            }
            EndReset(values);
            if(current < 0) {
                _log.WarnFormat("Illegal TaskEnv reference count after release: {0}", current);
            }
        }

        /// <summary>
        /// Reset all aquisitions and dispose the environment.
        /// </summary>
        public void Reset() {
            EndReset(BeginReset());
        }

        private IEnumerable<object> BeginReset() {
            IEnumerable<object> values = null;
            lock(_syncRoot) {
                if(_referenceCount > 0) {
                    _log.WarnFormat("resetting task environment with a ref count of {0}", _referenceCount);
                }
                _referenceCount = 0;
                if(Count == 0) {
                    return null;
                }
                values = Values.ToList();
                Clear();
            }
            return values;
        }

        private void EndReset(IEnumerable<object> values) {
            if(values == null) {
                return;
            }
            foreach(var value in values) {
                var disposable = value as ITaskLifespan;
                if(disposable == null) {
                    continue;
                }
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Invoke an action in place.
        /// </summary>
        /// <param name="handler">Action to invoke.</param>
        /// <returns><see langword="null"/> if the handler was executed sucessfully, the captured exception otherwise.</returns>
        public Exception InvokeNow(Action handler) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            if(_referenceCount < 1) {
                throw new InvalidOperationException("Cannot call invoke an unaquired TaskEnv");
            }

            // store current task environment
            TaskEnv previousEnv = _currentEnv;
            try {

                // set task environment
                _currentEnv = this;

                // execute handler
                handler();
            } catch(Exception e) {
                return e;
            } finally {

                // restore current task environment
                _currentEnv = previousEnv;
            }
            return null;
        }

        /// <summary>
        /// Invoke a zero arg action.
        /// </summary>
        /// <param name="handler">Action to invoke.</param>
        public void Invoke(Action handler) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            if(_referenceCount < 1) {
                throw new InvalidOperationException("Cannot call invoke an unaquired TaskEnv");
            }
            System.Diagnostics.StackTrace stacktrace = DebugUtil.GetStackTrace();

            // check if handler can be invoked in-place or needs to queued up
            if(_dispatchQueue != null) {
                _dispatchQueue.QueueWorkItem(delegate {

                    // store current thread-specific settings
                    TaskEnv previousEnv = _currentEnv;
                    try {

                        // set thread-specific settings
                        _currentEnv = this;

                        // execute handler
                        handler();
                    } catch(Exception e) {
                        _log.ErrorExceptionMethodCall(e, "Invoke: unhandled exception in handler");
                    } finally {
                        Release();

                        // restore thread-specific settings
                        _currentEnv = previousEnv;
                    }
                });
            } else {

                // store current thread-specific settings
                TaskEnv previousEnv = _currentEnv;
                try {

                    // set thread-specific settings
                    _currentEnv = this;

                    // execute handler
                    handler();
                } catch(Exception e) {
                    _log.WarnExceptionMethodCall(e, "Invoke: unhandled exception in handler");
                } finally {
                    Release();

                    // restore thread-specific settings
                    _currentEnv = previousEnv;
                }
            }
        }

        /// <summary>
        /// Invoke a one argument action.
        /// </summary>
        /// <typeparam name="T1">Type of first argument.</typeparam>
        /// <param name="handler">Action to invoke.</param>
        /// <param name="arg1">First argument.</param>
        public void Invoke<T1>(Action<T1> handler, T1 arg1) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            if(_referenceCount < 1) {
                throw new InvalidOperationException("Cannot call invoke an unaquired TaskEnv");
            }
            System.Diagnostics.StackTrace stacktrace = DebugUtil.GetStackTrace();

            // check if handler can be invoked in-place or needs to queued up
            if(_dispatchQueue != null) {
                _dispatchQueue.QueueWorkItem(delegate {

                    // store current thread-specific settings
                    TaskEnv previousEnv = _currentEnv;
                    try {

                        // set thread-specific settings
                        _currentEnv = this;

                        // execute handler
                        handler(arg1);
                    } catch(Exception e) {
                        _log.WarnExceptionMethodCall(e, "Invoke: unhandled exception in handler");
                    } finally {
                        Release();

                        // restore thread-specific settings
                        _currentEnv = previousEnv;
                    }
                });
            } else {

                // store current thread-specific settings
                TaskEnv previousEnv = _currentEnv;
                try {

                    // set thread-specific settings
                    _currentEnv = this;

                    // execute handler
                    handler(arg1);
                } catch(Exception e) {
                    _log.WarnExceptionMethodCall(e, "Invoke: unhandled exception in handler");
                } finally {
                    Release();

                    // restore thread-specific settings
                    _currentEnv = previousEnv;
                }
            }
        }

        /// <summary>
        /// Invoke a two argument action.
        /// </summary>
        /// <typeparam name="T1">Type of first argument.</typeparam>
        /// <typeparam name="T2">Type of second argument.</typeparam>
        /// <param name="handler">Action to invoke.</param>
        /// <param name="arg1">First argument.</param>
        /// <param name="arg2">Second argument.</param>
        public void Invoke<T1, T2>(Action<T1, T2> handler, T1 arg1, T2 arg2) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            if(_referenceCount < 1) {
                throw new InvalidOperationException("Cannot call invoke an unaquired TaskEnv");
            }
            System.Diagnostics.StackTrace stacktrace = DebugUtil.GetStackTrace();

            // check if handler can be invoked in-place or needs to queued up
            if(_dispatchQueue != null) {
                _dispatchQueue.QueueWorkItem(delegate {

                    // store current thread-specific settings
                    TaskEnv previousEnv = _currentEnv;
                    try {

                        // set thread-specific settings
                        _currentEnv = this;

                        // execute handler
                        handler(arg1, arg2);
                    } catch(Exception e) {
                        _log.WarnExceptionMethodCall(e, "Invoke: unhandled exception in handler");
                    } finally {
                        Release();

                        // restore thread-specific settings
                        _currentEnv = previousEnv;
                    }
                });
            } else {

                // store current thread-specific settings
                TaskEnv previousEnv = _currentEnv;
                try {

                    // set thread-specific settings
                    _currentEnv = this;

                    // execute handler
                    handler(arg1, arg2);
                } catch(Exception e) {
                    _log.WarnExceptionMethodCall(e, "Invoke: unhandled exception in handler");
                } finally {
                    Release();

                    // restore thread-specific settings
                    _currentEnv = previousEnv;
                }
            }
        }

        /// <summary>
        /// Invoke a three argument action.
        /// </summary>
        /// <typeparam name="T1">Type of first argument.</typeparam>
        /// <typeparam name="T2">Type of second argument.</typeparam>
        /// <typeparam name="T3">Type of third argument.</typeparam>
        /// <param name="handler">Action to invoke.</param>
        /// <param name="arg1">First argument.</param>
        /// <param name="arg2">Second argument.</param>
        /// <param name="arg3">Third argument.</param>
        public void Invoke<T1, T2, T3>(Action<T1, T2, T3> handler, T1 arg1, T2 arg2, T3 arg3) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            if(_referenceCount < 1) {
                throw new InvalidOperationException("Cannot call invoke an unaquired TaskEnv");
            }
            System.Diagnostics.StackTrace stacktrace = DebugUtil.GetStackTrace();

            // check if handler can be invoked in-place or needs to queued up
            if(_dispatchQueue != null) {
                _dispatchQueue.QueueWorkItem(delegate {

                    // store current thread-specific settings
                    TaskEnv previousEnv = _currentEnv;
                    try {

                        // set thread-specific settings
                        _currentEnv = this;

                        // execute handler
                        handler(arg1, arg2, arg3);
                    } catch(Exception e) {
                        _log.WarnExceptionMethodCall(e, "Invoke: unhandled exception in handler");
                    } finally {
                        Release();

                        // restore thread-specific settings
                        _currentEnv = previousEnv;
                    }
                });
            } else {

                // store current thread-specific settings
                TaskEnv previousEnv = _currentEnv;
                try {

                    // set thread-specific settings
                    _currentEnv = this;

                    // execute handler
                    handler(arg1, arg2, arg3);
                } catch(Exception e) {
                    _log.WarnExceptionMethodCall(e, "Invoke: unhandled exception in handler");
                } finally {
                    Release();

                    // restore thread-specific settings
                    _currentEnv = previousEnv;
                }
            }
        }

        /// <summary>
        /// Wrap a method call, delegate or lambda in an environment for later invocation.
        /// </summary>
        /// <param name="action">Call to wrap.</param>
        /// <returns>Handler for invocation in the environment.</returns>
        public Action MakeAction(Action action) {
            return MakeAction(action, null);
        }

        /// <summary>
        /// Wrap a method call, delegate or lambda in an environment for later invocation.
        /// </summary>
        /// <param name="action">Call to wrap.</param>
        /// <param name="result">Synchronization handle for the returned handler.</param>
        /// <returns>Handler for invocation in the environment.</returns>
        public Action MakeAction(Action action, Result result) {
            if(action == null) {
                throw new ArgumentNullException("action");
            }
            System.Diagnostics.StackTrace stacktrace = DebugUtil.GetStackTrace();
            Acquire();
            int executionCount = 0;
            return delegate() {
                try {
                    var execution = Interlocked.Increment(ref executionCount);
                    var exception = InvokeNow(action);
                    if(execution == 1) {
                        Release();
                    } else {
                        _log.WarnFormat("The action was unexpectedly called more than once ({0} times), later executions did not try to release the environment", execution);
                    }


                    // check if a result object was provided
                    if(result != null) {
                        if(exception != null) {
                            result.Throw(exception);
                        } else {
                            result.Return();
                        }
                    }
                } catch(Exception e) {
                    _log.ErrorExceptionMethodCall(e, "Execution failed for state wrapped action", stacktrace, action.Method.Name);
                }
            };
        }

        /// <summary>
        /// Wrap a method call, delegate or lambda in an environment for later invocation.
        /// </summary>
        /// <typeparam name="T">Return type of wrapped call.</typeparam>
        /// <param name="func">Call to wrap.</param>
        /// <returns>Handler for invocation in the environment.</returns>
        public Action MakeAction<T>(Func<T> func) {
            return MakeAction(func, null);
        }

        /// <summary>
        /// Wrap a method call, delegate or lambda in an environment for later invocation.
        /// </summary>
        /// <typeparam name="T">Return type of wrapped call.</typeparam>
        /// <param name="func">Call to wrap.</param>
        /// <param name="result">Synchronization handle for the returned handler.</param>
        /// <returns>Handler for invocation in the environment.</returns>
        public Action MakeAction<T>(Func<T> func, Result<T> result) {
            if(func == null) {
                throw new ArgumentNullException("func");
            }
            System.Diagnostics.StackTrace stacktrace = DebugUtil.GetStackTrace();
            Acquire();
            int executionCount = 0;
            return delegate() {
                var execution = Interlocked.Increment(ref executionCount);
                try {
                    var response = default(T);
                    var exception = InvokeNow(delegate {
                        response = func();
                    });
                    if(execution == 1) {
                        Release();
                    } else {
                        _log.WarnFormat("The action<{0}> was unexpectedly called more than once ({1} times), later executions did not try to release the environment", typeof(T), execution);
                    }

                    // check if a result object was provided
                    if(result != null) {
                        if(exception != null) {
                            result.Throw(exception);
                        } else {
                            result.Return(response);
                        }
                    }
                } catch(Exception e) {
                    _log.ErrorExceptionMethodCall(e, "Execution failed for state wrapped func", stacktrace, func.Method.Name);
                }
            };
        }
    }
}
