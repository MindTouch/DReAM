/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
using System.Threading;

namespace MindTouch.Dream.Test.Mock {

    /// <summary>
    /// Provides a utiltiy class for defining how often an event is expected to happen.
    /// </summary>
    public class Times {

        //--- Types ---

        /// <summary>
        /// Verification result.
        /// </summary>
        public enum Result {

            /// <summary>
            /// <see cref="Times.Verify(int)"/> was called with too few occurences.
            /// </summary>
            TooFew,
            /// <summary>
            /// <see cref="Times.Verify(int)"/> was called with too many occurences.
            /// </summary>
            TooMany,
            /// <summary>
            /// <see cref="Times.Verify(int)"/> was called with the expected number of occurences.
            /// </summary>
            Ok
        }
        
        private enum Type {
            AtLeast,
            AtMost,
            Exactly
        }

        //--- Class Methods ---

        /// <summary>
        /// Create an instance that expects at least a specified number of occurences.
        /// </summary>
        /// <param name="count">Occurence count.</param>
        /// <returns>New instance.</returns>
        public static Times AtLeast(int count) {
            return new Times(count, Type.AtLeast);
        }

        /// <summary>
        /// Create an instance that expects at least one occurence.
        /// </summary>
        /// <returns>New instance.</returns>
        public static Times AtLeastOnce() {
            return new Times(1, Type.AtLeast);
        }

        /// <summary>
        /// Create an instance that expects at most a specified number of occurences.
        /// </summary>
        /// <param name="count">Occurence count.</param>
        /// <returns>New instance.</returns>
        public static Times AtMost(int count) {
            return new Times(count, Type.AtMost);
        }

        /// <summary>
        /// Create an instance that expects at most one occurence.
        /// </summary>
        /// <returns>New instance.</returns>
        public static Times AtMostOnce() {
            return new Times(1, Type.AtMost);
        }

        /// <summary>
        /// Create an instance that expects no occurences.
        /// </summary>
        /// <returns>New instance.</returns>
        public static Times Never() {
            return new Times(0, Type.Exactly);
        }

        /// <summary>
        /// Create an instance that expects exactly one occurence.
        /// </summary>
        /// <returns>New instance.</returns>
        public static Times Once() {
            return new Times(1, Type.Exactly);
        }

        /// <summary>
        /// Create an instance that expects a specific number of occurences.
        /// </summary>
        /// <param name="count">Occurence count.</param>
        /// <returns>New instance.</returns>
        public static Times Exactly(int count) {
            return new Times(count, Type.Exactly);
        }

        //--- Fields ---
        private readonly int _times;
        private readonly Type _type;

        //--- Constructors ---
        private Times(int times, Type type) {
            _times = times;
            _type = type;
        }


        //--- Methods ---

        /// <summary>
        /// Verify the specified number of occurences against expectations.
        /// </summary>
        /// <param name="count">Occurence count.</param>
        /// <returns>Verification result</returns>
        public Result Verify(int count) {
            return Verify(count, TimeSpan.Zero);
        }

        /// <summary>
        /// Verify the specified number of occurences against expectations.
        /// </summary>
        /// <param name="count">Occurence count.</param>
        /// <param name="timeout">Time to wait if expectations have not yet been met.</param>
        /// <returns>Verification result</returns>
        public Result Verify(int count, TimeSpan timeout) {
            while(true) {
                Result? result = null;
                switch(_type) {
                case Type.Exactly:
                    if(count > _times) {
                        result = Result.TooMany;
                        break;
                    }
                    if(count < _times) {
                        result = Result.TooFew;
                    }
                    break;
                case Type.AtLeast:
                    if(count < _times) {
                        result = Result.TooFew;
                    }
                    break;
                case Type.AtMost:
                    if(count > _times) {
                        result = Result.TooMany;
                    }
                    break;
                }
                if(!result.HasValue) {
                    result = Result.Ok;
                }
                if(timeout.TotalMilliseconds <= 0) {
                    return result.Value;
                }
                if(result != Result.TooFew && (count != 0 || result != Result.Ok)) {
                    return result.Value;
                }
                Thread.Sleep(Math.Min(100, (int)timeout.TotalMilliseconds));
                timeout = timeout.Subtract(TimeSpan.FromMilliseconds(100));
            }
        }
    }
}