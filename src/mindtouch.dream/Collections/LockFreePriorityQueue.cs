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

namespace MindTouch.Collections {

    /// <summary>
    /// Provides an implementation of <see cref="IThreadsafePriorityQueue{T}"/> that does not incur locking overhead to provide thread-safe access to its members.
    /// </summary>
    /// <typeparam name="T">Type of item the queue can contain.</typeparam>
    public class LockFreePriorityQueue<T> : IThreadsafePriorityQueue<T> {

        //--- Fields ---
        private readonly LockFreeQueue<T>[] _queues;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance of the queue.
        /// </summary>
        /// <param name="maxPriority">Maximum priority for <see cref="TryEnqueue"/>.</param>
        public LockFreePriorityQueue(int maxPriority) {
            _queues = new LockFreeQueue<T>[maxPriority + 1];
            for(int i = 0; i < _queues.Length; i++) {
                _queues[i] = new LockFreeQueue<T>();
            }
        }

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if the queue is empty.
        /// </summary>
        public bool IsEmpty {
            get {
                foreach(LockFreeQueue<T> queue in _queues) {
                    if(!queue.IsEmpty) {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Total number of items in queue.
        /// </summary>
        public int Count {
            get {
                int count = 0;
                foreach(LockFreeQueue<T> queue in _queues) {
                    count += queue.Count;
                }
                return count;
            }
        }

        /// <summary>
        /// Maximum allowed priority for an enqueued item.
        /// </summary>
        public int MaxPriority { get { return _queues.Length - 1; } }

        //--- Methods ---

        /// <summary>
        /// Try to add an item to the queue.
        /// </summary>
        /// <param name="priority">Priority of the added item.</param>
        /// <param name="item">Item to add to queue.</param>
        /// <returns>Always returns <see langword="True"/>.</returns>
        public bool TryEnqueue(int priority, T item) {
            if((priority < 0) || (priority >= _queues.Length)) {
                throw new ArgumentException("out of range", "priority");
            }
            return _queues[priority].TryEnqueue(item);
        }

        /// <summary>
        /// Try to get an item from the queue.
        /// </summary>
        /// <param name="priority">Storage location for priority of the removed item.</param>
        /// <param name="item">Storage location for the item to be removed.</param>
        /// <returns><see langword="True"/> if the dequeue succeeded.</returns>
        public bool TryDequeue(out int priority, out T item) {
            for(int i = 0; i < _queues.Length; ++i) {
                LockFreeQueue<T> queue = _queues[i];
                if(queue.TryDequeue(out item)) {
                    priority = i;
                    return true;
                }
            }
            priority = -1;
            item = default(T);
            return false;
        }
    }
}
