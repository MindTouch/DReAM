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

namespace MindTouch.Threading {

    /// <summary>
    /// IDispatchQueue interface provides methods for submitting work-items to execute on a thread.
    /// </summary>
    public interface IDispatchQueue {

        //--- Methods ---

        /// <summary>
        /// Adds a work-item to the thread pool.  Depending on the implementation, this method may block the invoker
        /// or throw an exception if the thread pool cannot accept more items.
        /// </summary>
        /// <param name="callback">Item to add to the thread pool.</param>
        void QueueWorkItem(Action callback);

        /// <summary>
        /// Attempts to add a work-item to the thread pool.
        /// </summary>
        /// <param name="callback">Item to add to the thread pool.</param>
        /// <returns>Returns true if the item was successfully added to the thread pool.</returns>
        bool TryQueueWorkItem(Action callback);
    }
}
