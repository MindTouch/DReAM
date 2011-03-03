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

namespace MindTouch.Tasking {

    // TODO (steveb): need to figure out a way that ensures a lock is released when the receving tasks exits; similar to how an exception is triggered
    //  on the outer result object in coroutines in case of failure.
    //  also, consider using Capture(Result<Lock<T>> result) instead

    /// <summary>
    /// Provides a generic mechanism for synchronizing access to a value.
    /// </summary>
    /// <typeparam name="T">Type of value the lock can synchronize access for.</typeparam>
    public class Lock<T> {

        //--- Fields ---
        private T _value;
        private bool _captured;
        private Queue<Result> _queue = new Queue<Result>();

        //--- Constructors ---

        /// <summary>
        /// Create a new lock for a value.
        /// </summary>
        /// <param name="value">Value to lock.</param>
        public Lock(T value) {
            _value = value;
        }

        //--- Properties ---

        /// <summary>
        /// Retrieve the value if the lock has been captured. Throws <see cref="InvalidOperationException"/> if the lock has not yet been captured.
        /// </summary>
        public T Value {
            get {
                Check();
                return _value;
            }
            set {
                Check();
                _value = value;
            }
        }

        //---- Methods ---

        /// <summary>
        /// Capture the lock.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle for capturing the lock.</returns>
        public Result Capture(Result result) {
            bool success = false;
            lock(_queue) {
                if(!_captured && (_queue.Count == 0)) {
                    success = true;
                    _captured = true;
                } else {
                    _queue.Enqueue(result);
                }
            }
            if(success) {
                result.Return();
            }
            return result;
        }

        /// <summary>
        /// Release the captured lock.
        /// </summary>
        public void Release() {
            Check();
            Result next = null;
            lock(_queue) {
                _captured = (_queue.Count > 0);
                if(_captured) {
                    next = _queue.Dequeue();
                }
            }
            if(next != null) {
                next.Return();
            }
        }

        private void Check() {
            if(!_captured) {
                throw new InvalidOperationException("lock is not captured");
            }
        }
    }
}
