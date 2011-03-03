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

namespace MindTouch.Collections {

    /// <summary>
    /// Provides a thread-safe queue.
    /// </summary>
    /// <typeparam name="T">Type of item in the queue.</typeparam>
    public interface IThreadsafeQueue<T> {

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if the queue is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Total number of items in queue.
        /// </summary>
        int Count { get; }

        //--- Methods ---

        /// <summary>
        /// Try to add an item to the queue.
        /// </summary>
        /// <param name="item">Item to add to queue.</param>
        /// <returns><see langword="True"/> if the enqueue succeeded.</returns>
        bool TryEnqueue(T item);

        /// <summary>
        /// Try to get an item from the queue.
        /// </summary>
        /// <param name="item">Storage location for the item to be removed.</param>
        /// <returns><see langword="True"/> if the dequeue succeeded.</returns>
        bool TryDequeue(out T item);
    }
}
