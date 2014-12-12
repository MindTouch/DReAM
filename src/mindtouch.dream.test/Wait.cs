/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
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
using MindTouch.Extensions.Time;
using MindTouch.Tasking;

namespace MindTouch.Dream.Test {

    /// <summary>
    /// A helper for waiting for certain conditions to occur during tests with asynchronous control flow. Should generally be used as an argument
    /// for an NUnit Assert.
    /// </summary>
    public static class Wait {

        /// <summary>
        /// Value handle for the wait condition used by <see cref="Wait.For{T}"/>. Used to allow the result to emit both a value and a success indicator.
        /// </summary>
        /// <typeparam name="T">Type of the result <see cref="Value"/>.</typeparam>
        public class WaitResult<T> {

            /// <summary>
            /// Value returned by <see cref="Wait.For"/> condition function.
            /// </summary>
            public T Value { get; set; }

            /// <summary>
            /// Did <see cref="Wait.For{T}"/> succeed in the alloted time?
            /// </summary>
            public bool Success;
        }

        /// <summary>
        /// Repeatedly test a <b>condition</b> until the call times out or the condition indicates success.
        /// </summary>
        /// <typeparam name="T">The type of the result <see cref="WaitResult{T}"/> returned on success.</typeparam>
        /// <param name="condition">Func to check whether wait condition has been met.</param>
        /// <param name="timeout">The maximum time to keep testing the condition.</param>
        /// <returns>The value produced by successful execution of the condition.</returns>
        public static T For<T>(Func<WaitResult<T>> condition, TimeSpan timeout) {
            var timer = Stopwatch.StartNew();
            while(timeout > timer.Elapsed) {
                var waitresult = condition();
                if(waitresult.Success) {
                    return waitresult.Value;
                }
                AsyncUtil.Sleep(50.Milliseconds());
            }
            throw new TimeoutException("gave up waiting for value");
        }

        /// <summary>
        /// Repeatedly test a <b>condition</b> until the call times out or the condition indicates success.
        /// </summary>
        /// <param name="condition">Func to check whether wait condition has been met.</param>
        /// <param name="timeout">The maximum time to keep testing the condition.</param>
        /// <returns></returns>
        public static bool For(Func<bool> condition, TimeSpan timeout) {
            var timer = Stopwatch.StartNew();
            while(timeout > timer.Elapsed) {
                if(condition()) {
                    return true;
                }
                AsyncUtil.Sleep(50.Milliseconds());
            }
            return false;
        }
    }
}
