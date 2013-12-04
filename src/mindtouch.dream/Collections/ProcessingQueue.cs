/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2013 MindTouch, Inc.
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
using MindTouch.Tasking;
using MindTouch.Threading;

namespace MindTouch.Collections {
    
    /// <summary>
    /// Provides a mechanism for dispatching work items against an <see cref="IDispatchQueue"/>.
    /// </summary>
    /// <typeparam name="T">Type of work item that can be dispatched.</typeparam>
    public class ProcessingQueue<T> : IThreadsafeQueue<T> {

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
        private readonly LockFreeItemConsumerQueue<T> _pending;
        private readonly int _maxParallelism;
        private int _capacity;

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
            if(maxParallelism <= 0) {
                throw new ArgumentException("maxParallelism must be greater than 0", "maxParallelism");
            }
            _handler = handler;
            _maxParallelism = maxParallelism;
            _capacity = maxParallelism;
            _dispatchQueue = dispatchQueue;

            // check if we need an item holding queue
            if(maxParallelism < int.MaxValue) {
                _pending = new LockFreeItemConsumerQueue<T>();
            }
        }

        /// <summary>
        /// Create an instance of the work queue.
        /// </summary>
        /// <param name="handler">Dispatch action for work item Type and with completion callback.</param>
        /// <param name="dispatchQueue">Dispatch queue for work items.</param>
        public ProcessingQueue(Action<T, Action> handler, IDispatchQueue dispatchQueue) : this(handler, int.MaxValue, dispatchQueue) { }

        /// <summary>
        /// Create an instance of the work queue.
        /// </summary>
        /// <param name="handler">Dispatch action for work item Type and with completion callback.</param>
        /// <param name="maxParallelism">Maximum number of items being dispatch simultaneously against the dispatch queue.</param>
        public ProcessingQueue(Action<T, Action> handler, int maxParallelism) : this(handler, maxParallelism, AsyncUtil.GlobalDispatchQueue) { }

        /// <summary>
        /// Create an instance of the work queue.
        /// </summary>
        /// <param name="handler">Dispatch action for work item Type and with completion callback.</param>
        public ProcessingQueue(Action<T, Action> handler) : this(handler, int.MaxValue, AsyncUtil.GlobalDispatchQueue) { }

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
        /// <param name="handler">Dispatch action for work item Type.</param>
        /// <param name="dispatchQueue">Dispatch queue for work items.</param>
        public ProcessingQueue(Action<T> handler, IDispatchQueue dispatchQueue) : this(handler, int.MaxValue, dispatchQueue) { }

        /// <summary>
        /// Create an instance of the work queue.
        /// </summary>
        /// <param name="handler">Dispatch action for work item Type and with completion callback.</param>
        /// <param name="maxParallelism">Maximum number of items being dispatch simultaneously against the dispatch queue.</param>
        public ProcessingQueue(Action<T> handler, int maxParallelism) : this(handler, maxParallelism, AsyncUtil.GlobalDispatchQueue) { }

        /// <summary>
        /// Create an instance of the work queue.
        /// </summary>
        /// <param name="handler">Dispatch action for work item Type and with completion callback.</param>
        public ProcessingQueue(Action<T> handler) : this(handler, int.MaxValue, AsyncUtil.GlobalDispatchQueue) { }

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if the queue is empty.
        /// </summary>
        public bool IsEmpty { get { return Count == 0; } }

        /// <summary>
        /// Total number of items in queue.
        /// </summary>
        public int Count { get { return _maxParallelism - Thread.VolatileRead(ref _capacity); } }

        //--- Methods ---

        /// <summary>
        /// Try to queue a work item for dispatch.
        /// </summary>
        /// <param name="item">Item to add to queue.</param>
        /// <returns><see langword="True"/> if the enqueue succeeded.</returns>
        public bool TryEnqueue(T item) {

            // NOTE (steveb): when '_capacity' drops below 0, we have more items to process than we have 
            //                concurrent capacity for; in this case, we need to enqueue the item for later.

            var overCapacity = (Interlocked.Decrement(ref _capacity) < 0);
            if((_pending != null) && overCapacity) {
                return _pending.TryEnqueue(item);
            }
            return TryStartWorkItem(item);
        }

        /// <summary>
        /// Try to get an item from the queue.
        /// </summary>
        /// <param name="item">Storage location for the item to be removed.</param>
        /// <returns><see langword="True"/> if the dequeue succeeded.</returns>
        public bool TryDequeue(out T item) {

            // WRONG

            if((_pending != null) && _pending.TryDequeue(out item)) {
                Interlocked.Increment(ref _capacity);
                return true;
            }
            item = default(T);
            return false;
        }

        private bool TryStartWorkItem(T item) {
            return _dispatchQueue.TryQueueWorkItem(() => _handler(item, EndWorkItem));
        }

        private void StartWorkItem(T item) {
            if(!TryStartWorkItem(item)) {
                throw new NotSupportedException("TryStartWorkItem failed");
            }
        }

        private void EndWorkItem() {

            // NOTE (steveb): if after increasing '_capacity', it is still not positive, then we know
            //                there were pending items that need to be run; otherwise, all remaining
            //                items are inflight or done.

            var overCapacity = (Interlocked.Increment(ref _capacity) <= 0);
            if((_pending != null) && overCapacity) {
                if(!_pending.TryEnqueue(StartWorkItem)) {
                    throw new NotSupportedException("TryEnqueue failed");
                }
            }
        }
    }
}
