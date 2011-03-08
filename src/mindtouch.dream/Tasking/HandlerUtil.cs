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

namespace MindTouch.Tasking {

    /// <summary>
    /// Extension methods to wrap a <see cref="Func{TResult}"/> or <see cref="Action"/> with an <see cref="TaskEnv"/> for use in invocation.
    /// </summary>
    public static class HandlerUtil {

        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        
        /// <summary>
        /// Wrap a <see cref="Func{TResult}"/> with an execution environment.
        /// </summary>
        /// <remarks>
        /// This method is deprecated. Use <see cref="TaskEnv.MakeAction{T}(Func{T})"/> instead.
        /// </remarks>
        /// <typeparam name="T">Return value of func.</typeparam>
        /// <param name="handler">The func to wrap.</param>
        /// <param name="env">The environment to wrap with.</param>
        /// <returns>An action that when invoked will execute the func in the given environment.</returns>
        [Obsolete("This method is deprecated. Use TaskEnv.Wrap() instead.")]
        public static Action WithEnv<T>(this Func<T> handler, TaskEnv env) {
            return handler.WithEnv(env, null); 
        }

        /// <summary>
        /// Wrap a <see cref="Func{TResult}"/> with an execution environment.
        /// </summary>
        /// <remarks>
        /// This method is deprecated. Use <see cref="TaskEnv.MakeAction{T}(System.Func{T},MindTouch.Tasking.Result{T})"/> instead.
        /// </remarks>
        /// <typeparam name="T">Return value of func.</typeparam>
        /// <param name="handler">The func to wrap.</param>
        /// <param name="env">The environment to wrap with.</param>
        /// <param name="result">Synchronization handle for the returned action.</param>
        /// <returns>An action that when invoked will execute the func in the given environment.</returns>
        [Obsolete("This method is deprecated. Use TaskEnv.Wrap() instead.")]
        public static Action WithEnv<T>(this Func<T> handler, TaskEnv env, Result<T> result) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            System.Diagnostics.StackTrace stacktrace = DebugUtil.GetStackTrace();
            env.Acquire();
            return delegate() {
                try {
                    T response = default(T);
                    Exception exception = env.InvokeNow(delegate {
                        response = handler();
                    });
                    env.Release();

                    // check if a result object was provided
                    if(result != null) {
                        if(exception != null) {
                            result.Throw(exception);
                        } else {
                            result.Return(response);
                        }
                    }
                } catch(Exception e) {
                    _log.ErrorExceptionMethodCall(e, "Execution failed for state wrapped handler", stacktrace, handler.Method.Name);
                }
            };
        }

        /// <summary>
        /// Wrap a <see cref="Action"/> with an execution environment.
        /// </summary>
        /// <remarks>
        /// This method is deprecated. Use <see cref="TaskEnv.MakeAction(Action)"/> instead.
        /// </remarks>
        /// <param name="handler">The action to wrap.</param>
        /// <param name="env">The environment to wrap with.</param>
        /// <returns>An action that when invoked will execute the action in the given environment.</returns>
        [Obsolete("This method is deprecated. Use TaskEnv.Wrap() instead.")]
        public static Action WithEnv(this Action handler, TaskEnv env) {
            return handler.WithEnv(env, null);
        }

        /// <summary>
        /// Wrap a <see cref="Action"/> with an execution environment.
        /// </summary>
        /// <remarks>
        /// This method is deprecated. Use <see cref="TaskEnv.MakeAction(System.Action,MindTouch.Tasking.Result)"/> instead.
        /// </remarks>
        /// <param name="handler">The action to wrap.</param>
        /// <param name="env">The environment to wrap with.</param>
        /// <param name="result">Synchronization handle for the returned action.</param>
        /// <returns>An action that when invoked will execute the action in the given environment.</returns>
        [Obsolete("This method is deprecated. Use TaskEnv.Wrap() instead.")]
        public static Action WithEnv(this Action handler, TaskEnv env, Result result) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            System.Diagnostics.StackTrace stacktrace = DebugUtil.GetStackTrace();
            env.Acquire();
            return delegate() {
                try {
                    Exception exception = env.InvokeNow(handler);
                    env.Release();

                    // check if a result object was provided
                    if(result != null) {
                        if(exception != null) {
                            result.Throw(exception);
                        } else {
                            result.Return();
                        }
                    }
                } catch(Exception e) {
                    _log.ErrorExceptionMethodCall(e, "Execution failed for state wrapped handler", stacktrace, handler.Method.Name);
                }
            };
        }

    }
}
