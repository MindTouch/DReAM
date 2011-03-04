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
using System.Threading;
using MindTouch.Tasking;
using MindTouch.Threading;

namespace MindTouch.Collections {
    
    /// <summary>
    /// Provides a mechanism for dispatching work items against an <see cref="IDispatchQueue"/>.
    /// </summary>
    /// <typeparam name="T">Type of work item that can be dispatched.</typeparam>
    public class ProcessingQueue<T> : IThreadsafeQueue<T>, IDisposable {

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
        private readonly LockFreeItemConsumerQueue<T> _inbox;
        private bool _disposed;
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
            _capacity = maxParallelism;
            _dispatchQueue = dispatchQueue;

            // check if we need an item holding queue
            if(maxParallelism < int.MaxValue) {
                _inbox = new LockFreeItemConsumerQueue<T>();
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
        public ProcessingQueue(Action<T, Action> handler, int maxParallelism) : this(handler, maxParallelism, Async.GlobalDispatchQueue) { }

        /// <summary>
        /// Create an instance of the work queue.
        /// </summary>
        /// <param name="handler">Dispatch action for work item Type and with completion callback.</param>
        public ProcessingQueue(Action<T, Action> handler) : this(handler, int.MaxValue, Async.GlobalDispatchQueue) { }

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
        public ProcessingQueue(Action<T> handler, int maxParallelism) : this(handler, maxParallelism, Async.GlobalDispatchQueue) { }

        /// <summary>
        /// Create an instance of the work queue.
        /// </summary>
        /// <param name="handler">Dispatch action for work item Type and with completion callback.</param>
        public ProcessingQueue(Action<T> handler) : this(handler, int.MaxValue, Async.GlobalDispatchQueue) { }

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if the queue is empty.
        /// </summary>
        public bool IsEmpty { get { return (_inbox != null) ? _inbox.ItemIsEmpty : true; } }

        /// <summary>
        /// Total number of items in queue.
        /// </summary>
        public int Count { get { return (_inbox != null) ? _inbox.ItemCount : 0; } }

        //--- Methods ---

        /// <summary>
        /// Try to queue a work item for dispatch.
        /// </summary>
        /// <param name="item">Item to add to queue.</param>
        /// <returns><see langword="True"/> if the enqueue succeeded.</returns>
        public bool TryEnqueue(T item) {
            if((_inbox != null) && (Interlocked.Decrement(ref _capacity) < 0)) {
                return _inbox.TryEnqueue(item);
            }
            return TryStartWorkItem(item);
        }

        /// <summary>
        /// This method is not supported and throws <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"/>
        public bool TryDequeue(out T item) {
            if(_inbox != null) {
                return _inbox.TryDequeue(out item);
            }
            item = default(T);
            return false;
        }

        /// <summary>
        /// Release the resources reserved by the work queue from the global thread pool.
        /// </summary>
        public void Dispose() {
            if(!_disposed) {
                _disposed = true;

                // check if the dispatch queue needs to be disposed
                IDisposable disposable = _dispatchQueue as IDisposable;
                if(disposable != null) {
                    disposable.Dispose();
                }
            }
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
            if((_inbox != null) && (Interlocked.Increment(ref _capacity) <= 0)) {
                if(!_inbox.TryEnqueue(StartWorkItem)) {
                    throw new NotSupportedException("TryEnqueue failed");
                }
            }
        }
    }
}
