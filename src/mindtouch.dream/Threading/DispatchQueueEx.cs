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
using MindTouch.Tasking;

namespace MindTouch.Threading {

    /// <summary>
    /// Provides extension methods to <see cref="IDispatchQueue"/> to simplify enqueuing of work to be invoked in a specific <see cref="TaskEnv"/>.
    /// </summary>
    public static class DispatchQueueEx {

        /// <summary>
        /// Enqueue a callback as a work item to be invoked with the current <see cref="TaskEnv"/>.
        /// </summary>
        /// <param name="dispatchQueue">DispatchQueue to enqueue work into.</param>
        /// <param name="callback">Work item callback.</param>
        public static void QueueWorkItemWithCurrentEnv(this IDispatchQueue dispatchQueue, Action callback) {
            dispatchQueue.QueueWorkItemWithCurrentEnv(callback, null);
        }

        /// <summary>
        /// Enqueue a callback as a work item to be invoked with a clone of the current <see cref="TaskEnv"/>.
        /// </summary>
        /// <param name="dispatchQueue">DispatchQueue to enqueue work into.</param>
        /// <param name="callback">Work item callback.</param>
        public static void QueueWorkItemWithClonedEnv(this IDispatchQueue dispatchQueue, Action callback) {
            dispatchQueue.QueueWorkItemWithEnv(callback, TaskEnv.Clone(), null);
        }

        /// <summary>
        /// Enqueue a callback as a work item to be invoked with a provided <see cref="TaskEnv"/>.
        /// </summary>
        /// <param name="dispatchQueue">DispatchQueue to enqueue work into.</param>
        /// <param name="callback">Work item callback.</param>
        /// <param name="env">Environment for work item invocation.</param>
        public static void QueueWorkItemWithEnv(this IDispatchQueue dispatchQueue, Action callback, TaskEnv env) {
            dispatchQueue.QueueWorkItemWithEnv(callback, env, null);
        }

        /// <summary>
        /// Enqueue a callback as a work item to be invoked with the current <see cref="TaskEnv"/>.
        /// </summary>
        /// <param name="dispatchQueue">DispatchQueue to enqueue work into.</param>
        /// <param name="callback">Work item callback.</param>
        /// <param name="result">Synchronization handle for work item.</param>
        /// <returns>The synchronization handle provided to the method.</returns>
        public static Result QueueWorkItemWithCurrentEnv(this IDispatchQueue dispatchQueue, Action callback, Result result) {
            var current = TaskEnv.CurrentOrNull;
            if(current != null) {
                return dispatchQueue.QueueWorkItemWithEnv(callback, current, result);
            }
            if(result != null) {
                dispatchQueue.QueueWorkItem(delegate() {
                    try {
                        callback();
                        result.Return();
                    } catch(Exception e) {
                        result.Throw(e);
                    }
                });
                return result;
            }
            dispatchQueue.QueueWorkItem(callback);
            return null;
        }

        /// <summary>
        /// Enqueue a callback as a work item to be invoked with a clone of the current <see cref="TaskEnv"/>.
        /// </summary>
        /// <param name="dispatchQueue">DispatchQueue to enqueue work into.</param>
        /// <param name="callback">Work item callback.</param>
        /// <param name="result">Synchronization handle for work item.</param>
        /// <returns>The synchronization handle provided to the method.</returns>
        public static Result QueueWorkItemWithClonedEnv(this IDispatchQueue dispatchQueue, Action callback, Result result) {
            return dispatchQueue.QueueWorkItemWithEnv(callback, TaskEnv.Clone(), result);
        }

        /// <summary>
        /// Enqueue a callback as a work item to be invoked with a provided <see cref="TaskEnv"/>.
        /// </summary>
        /// <param name="dispatchQueue">DispatchQueue to enqueue work into.</param>
        /// <param name="callback">Work item callback.</param>
        /// <param name="env">Environment for work item invocation.</param>
        /// <param name="result">Synchronization handle for work item.</param>
        /// <returns>The synchronization handle provided to the method.</returns>
        public static Result QueueWorkItemWithEnv(this IDispatchQueue dispatchQueue, Action callback, TaskEnv env, Result result) {
            if(env == null) {
                throw new ArgumentException("env");
            }
            dispatchQueue.QueueWorkItem(env.MakeAction(callback, result));
            return result;
        }

        /// <summary>
        /// Enqueue a callback as a work item to be invoked with the current <see cref="TaskEnv"/>.
        /// </summary>
        /// <typeparam name="T">Result value type of callback.</typeparam>
        /// <param name="dispatchQueue">DispatchQueue to enqueue work into.</param>
        /// <param name="callback">Work item callback.</param>
        public static void QueueWorkItemWithCurrentEnv<T>(this IDispatchQueue dispatchQueue, Func<T> callback) {
            dispatchQueue.QueueWorkItemWithCurrentEnv(callback, null);
        }

        /// <summary>
        /// Enqueue a callback as a work item to be invoked with a clone of the current <see cref="TaskEnv"/>.
        /// </summary>
        /// <typeparam name="T">Result value type of callback.</typeparam>
        /// <param name="dispatchQueue">DispatchQueue to enqueue work into.</param>
        /// <param name="callback">Work item callback.</param>
        public static void QueueWorkItemWithClonedEnv<T>(this IDispatchQueue dispatchQueue, Func<T> callback) {
            dispatchQueue.QueueWorkItemWithEnv(callback, TaskEnv.Clone(), null);
        }

        /// <summary>
        /// Enqueue a callback as a work item to be invoked with a provided <see cref="TaskEnv"/>.
        /// </summary>
        /// <typeparam name="T">Result value type of callback.</typeparam>
        /// <param name="dispatchQueue">DispatchQueue to enqueue work into.</param>
        /// <param name="callback">Work item callback.</param>
        /// <param name="env">Environment for work item invocation.</param>
        public static void QueueWorkItemWithEnv<T>(this IDispatchQueue dispatchQueue, Func<T> callback, TaskEnv env) {
            dispatchQueue.QueueWorkItemWithEnv(callback, env, null);
        }

        /// <summary>
        /// Enqueue a callback as a work item to be invoked with the current <see cref="TaskEnv"/>.
        /// </summary>
        /// <typeparam name="T">Result value type of callback.</typeparam>
        /// <param name="dispatchQueue">DispatchQueue to enqueue work into.</param>
        /// <param name="callback">Work item callback.</param>
        /// <param name="result">Synchronization handle for work item.</param>
        /// <returns>The synchronization handle provided to the method.</returns>
        public static Result<T> QueueWorkItemWithCurrentEnv<T>(this IDispatchQueue dispatchQueue, Func<T> callback, Result<T> result) {
            var current = TaskEnv.CurrentOrNull;
            if(current != null) {
                return dispatchQueue.QueueWorkItemWithEnv(callback, current, result);
            }
            if(result != null) {
                dispatchQueue.QueueWorkItem(delegate() {
                    try {
                        result.Return(callback());
                    } catch(Exception e) {
                        result.Throw(e);
                    }
                });
                return result;
            }
            dispatchQueue.QueueWorkItem(() => callback());
            return null;
        }

        /// <summary>
        /// Enqueue a callback as a work item to be invoked with a clone of the current <see cref="TaskEnv"/>.
        /// </summary>
        /// <typeparam name="T">Result value type of callback.</typeparam>
        /// <param name="dispatchQueue">DispatchQueue to enqueue work into.</param>
        /// <param name="callback">Work item callback.</param>
        /// <param name="result">Synchronization handle for work item.</param>
        /// <returns>The synchronization handle provided to the method.</returns>
        public static Result<T> QueueWorkItemWithClonedEnv<T>(this IDispatchQueue dispatchQueue, Func<T> callback, Result<T> result) {
            return dispatchQueue.QueueWorkItemWithEnv(callback, TaskEnv.Clone(), result);
        }

        /// <summary>
        /// Enqueue a callback as a work item to be invoked with a provided <see cref="TaskEnv"/>.
        /// </summary>
        /// <typeparam name="T">Result value type of callback.</typeparam>
        /// <param name="dispatchQueue">DispatchQueue to enqueue work into.</param>
        /// <param name="callback">Work item callback.</param>
        /// <param name="env">Environment for work item invocation.</param>
        /// <param name="result">Synchronization handle for work item.</param>
        /// <returns>The synchronization handle provided to the method.</returns>
        public static Result<T> QueueWorkItemWithEnv<T>(this IDispatchQueue dispatchQueue, Func<T> callback, TaskEnv env, Result<T> result) {
            if(env == null) {
                throw new ArgumentException("env");
            }
            dispatchQueue.QueueWorkItem(env.MakeAction(callback, result));
            return result;
        }
    }
}
