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
using MindTouch.Threading;

namespace MindTouch.Tasking {

    using Yield = IEnumerator<IYield>;

    /// <summary>
    /// A static utility class containing helpers and extension methods for working with <see cref="AResult"/> based objects.
    /// </summary>
    public static class AResultEx {
        
        //--- Extension Methods ---

        /// <summary>
        /// Get a result object for joining the completion of a sequence of <see cref="Result"/> instances.
        /// </summary>
        /// <remarks>
        /// This extension is useful for executing some action once all Results complete, but does not provide the Results waited on.
        /// If access to the finished is needed, they need to be manually captured in the synchronization handler.
        /// </remarks>
        /// <typeparam name="TResult">Type of the result enumerable to operate on.</typeparam>
        /// <param name="enumerable">Enumerable of <see cref="AResult"/> instances to wait on.</param>
        /// <param name="result">The <see cref="Result"/> instance this method will return.</param>
        /// <returns>The passed in <see cref="Result"/> object to be used as a synchronization handle.</returns>
        public static Result Join<TResult>(this IEnumerable<TResult> enumerable, Result result) where TResult : AResult {
            Join_Helper(enumerable.GetEnumerator(), result);
            return result;
        }

        private static void Join_Helper<TResult>(this IEnumerator<TResult> enumerator, Result result) where TResult : AResult {
            while(enumerator.MoveNext()) {
                var current = enumerator.Current;
                if(!current.HasFinished) {
                    current.WhenDone(_ => Join_Helper(enumerator, result));
                    return;
                }
            }
            enumerator.Dispose();
            result.Return();
        }

        /// <summary>
        /// Get a result object to capture the first completion in an sequence of alternative <see cref="Result{T}"/> objects. 
        /// </summary>
        /// <typeparam name="T">Type of the result value</typeparam>
        /// <param name="alternatives">Array of alternative <see cref="Result{T}"/> objects.</param>
        /// <param name="result">The <see cref="Result{T}"/> instance this method will return.</param>
        /// <param name="discard">Callback for alternatives that complete after the first completion, and did not get cancelled.</param>
        /// <returns>The passed in <see cref="Result"/> object to be used as a synchronization handle.</returns>
        public static Result<T> Alt<T>(this Result<T>[] alternatives, Result<T> result, Action<T> discard) {
            if((alternatives == null) || (alternatives.Length == 0)) {
                throw new ArgumentNullException("alternatives");
            }
            if(result == null) {
                throw new ArgumentNullException("result");
            }

            // create delegate to forward the first result
            var counter = new LockFreeCounter(alternatives.Length);
            Action<Result<T>> handler = res => {
                if(res.HasValue) {

                    // let outer context know about the successful outcome
                    if(!result.TryReturn(res.Value) && (discard != null)) {
                        discard(res.Value);
                    }

                    // cancel all incomplete alternatives
                    foreach(var alt in alternatives) {
                        alt.Cancel();
                    }
                } else if(counter.Decrement() == 0) {

                    // we have exhausted all alternatives
                    result.Throw(new AsyncAllAlternatesFailed());
                }
            };

            // wait for outcomes on all alternatives
            for(int i = 0; i < alternatives.Length; ++i) {
                alternatives[i].WhenDone(handler);
            }
            return result;
        }

        /// <summary>
        /// Get a result object to capture the first completion in an sequence of alternative <see cref="Result"/> objects. 
        /// </summary>
        /// <param name="alternatives">Array of alternative <see cref="Result"/> objects.</param>
        /// <param name="result">The <see cref="Result"/> instance this method will return.</param>
        /// <returns>The passed in <see cref="Result"/> object to be used as a synchronization handle.</returns>
        public static Result Alt(this Result[] alternatives, Result result) {
            if((alternatives == null) || (alternatives.Length == 0)) {
                throw new ArgumentNullException("alternatives");
            }
            if(result == null) {
                throw new ArgumentNullException("result");
            }

            // create delegate to forward the first result
            var counter = new LockFreeCounter(alternatives.Length);
            Action<Result> handler = res => {
                if(res.HasValue) {

                    // let outer context know about the successful outcome
                    result.TryReturn();

                    // cancel all incomplete alternatives
                    foreach(var alt in alternatives) {
                        alt.Cancel();
                    }
                } else if(counter.Decrement() == 0) {

                    // we have exhausted all alternatives
                    result.Throw(new AsyncAllAlternatesFailed());
                }
            };

            // wait for outcomes on all alternatives
            for(int i = 0; i < alternatives.Length; ++i) {
                alternatives[i].WhenDone(handler);
            }
            return result;
        }
    }
}