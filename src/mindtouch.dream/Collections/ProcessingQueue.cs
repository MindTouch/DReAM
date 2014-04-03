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
using System.Threading;
using MindTouch.Tasking;
using MindTouch.Threading;

namespace MindTouch.Collections {
    
    /// <summary>
    /// Provides a mechanism for dispatching work items against an <see cref="IDispatchQueue"/>.
    /// </summary>
    /// <typeparam name="T">Type of work item that can be dispatched.</typeparam>
    public class ProcessingQueue<T> : IThreadsafeQueue<T> {

        //--- Constants ---
        public const int MAX_PARALLELISM = 10000;

        //--- Class Methods ---
        private static Action<T, Action> MakeHandlerWithCompletion(Action<T> handler) {
            return (item, completion) => {
                try {
                    handler(item);
                } finally {
                    completion();
                }
            };
        }

        //--- Fields ---
        private readonly Action<T, Action> _handler;
        private readonly IDispatchQueue _dispatchQueue;
        private readonly LockFreeItemConsumerQueue<T> _pending = new LockFreeItemConsumerQueue<T>();
        private int _count;

        //--- Constructors ---

        /// <summary>
        /// Create an instance of the work queue.
        /// </summary>
        /// <param name="handler">Dispatch action for work item Type and with completion callback.</param>
        /// <param name="maxParallelism">Maximum number of items being dispatch simultaneously against the dispatch queue.</param>
        /// <param name="dispatchQueue">Dispatch queue for work items.</param>
        public ProcessingQueue(Action<T, Action> handler, int maxParallelism, IDispatchQueue dispatchQueue) {
            if(dispatchQueue == null) {
                throw new ArgumentNullException("dispatchQueue");
            }
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            if((maxParallelism <= 0) || (maxParallelism > MAX_PARALLELISM)) {
                throw new ArgumentException(string.Format("maxParallelism must be between 1 and {0:#,##0}", MAX_PARALLELISM), "maxParallelism");
            }
            _handler = handler;
            _dispatchQueue = dispatchQueue;

            // prime the pending queue with dispatchers
            for(var i = 0; i < maxParallelism; ++i) {
                if(!_pending.TryEnqueue(StartWorkItem)) {
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// Create an instance of the work queue.
        /// </summary>
        /// <param name="handler">Dispatch action for work item Type and with completion callback.</param>
        /// <param name="maxParallelism">Maximum number of items being dispatch simultaneously against the dispatch queue.</param>
        public ProcessingQueue(Action<T, Action> handler, int maxParallelism) : this(handler, maxParallelism, AsyncUtil.GlobalDispatchQueue) { }

        /// <summary>
        /// Create an instance of the work queue.
        /// </summary>
        /// <param name="handler">Dispatch action for work item Type.</param>
        /// <param name="maxParallelism">Maximum number of items being dispatch simultaneously against the dispatch queue.</param>
        /// <param name="dispatchQueue">Dispatch queue for work items.</param>
        public ProcessingQueue(Action<T> handler, int maxParallelism, IDispatchQueue dispatchQueue) : this(MakeHandlerWithCompletion(handler), maxParallelism, dispatchQueue) { }

        /// <summary>
        /// Create an instance of the work queue.
        /// </summary>
        /// <param name="handler">Dispatch action for work item Type and with completion callback.</param>
        /// <param name="maxParallelism">Maximum number of items being dispatch simultaneously against the dispatch queue.</param>
        public ProcessingQueue(Action<T> handler, int maxParallelism) : this(handler, maxParallelism, AsyncUtil.GlobalDispatchQueue) { }

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if the queue is empty.
        /// </summary>
        public bool IsEmpty { get { return Count == 0; } }

        /// <summary>
        /// Total number of items in queue.
        /// </summary>
        public int Count { get { return Thread.VolatileRead(ref _count); } }

        //--- Methods ---

        /// <summary>
        /// Try to queue a work item for dispatch.
        /// </summary>
        /// <param name="item">Item to add to queue.</param>
        /// <returns><see langword="True"/> if the enqueue succeeded.</returns>
        public bool TryEnqueue(T item) {

            // NOTE (steveb): we optimistically increase the _count variable and then decrease it if the enqueue were to fail;
            //                this avoids a race condition where an item gets enqueued and immediately processed before _count 
            //                is increased, which could lead to an observable negative _count value.

            Interlocked.Increment(ref _count);
            if(!_pending.TryEnqueue(item)) {
                Interlocked.Decrement(ref _count);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Try to get an item from the queue.
        /// </summary>
        /// <param name="item">Storage location for the item to be removed.</param>
        /// <returns><see langword="True"/> if the dequeue succeeded.</returns>
        public bool TryDequeue(out T item) {
            if(_pending.TryDequeue(out item)) {
                Interlocked.Decrement(ref _count);
                return true;
            }
            return false;
        }

        private void StartWorkItem(T item) {
            if(!_dispatchQueue.TryQueueWorkItem(() => _handler(item, EndWorkItem))) {
                throw new NotSupportedException();
            }
        }

        private void EndWorkItem() {
            Interlocked.Decrement(ref _count);
            if(!_pending.TryEnqueue(StartWorkItem)) {
                throw new NotSupportedException();
            }
        }
    }
}
