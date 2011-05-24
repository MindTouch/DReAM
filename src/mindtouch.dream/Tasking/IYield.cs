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
    /// Provides the iterator interface used by <see cref="Coroutine"/> to execute methods as coroutines.
    /// </summary>
    public interface IYield {

        //--- Properties ---

        /// <summary>
        /// Current iterator has an exception.
        /// </summary>
        Exception Exception { get; }

        //--- Methods ---

        /// <summary>
        /// <see langword="True"/> If the co-routine can continue immediately, rather than be enqueued in a worker pool.
        /// </summary>
        /// <param name="continuation">The continuation to use to continue to execute the iterator.</param>
        /// <returns></returns>
        bool CanContinueImmediately(IContinuation continuation);
    }

    /// <summary>
    /// Provides the interface for an iterator value that can continue coroutine execution.
    /// </summary>
    public interface IContinuation {
        
        //--- Methods ---

        /// <summary>
        /// Continue to execute the current coroutine.
        /// </summary>
        void Continue();
    }
}