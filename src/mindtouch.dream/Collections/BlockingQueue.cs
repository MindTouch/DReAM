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
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace MindTouch.Collections {

    /// <summary>
    /// Exception thrown by <see cref="BlockingQueue{T}.Enqueue"/> and <see cref="BlockingQueue{T}.Dequeue"/> when the underlying queue has already been closed.
    /// </summary>
    public class QueueClosedException : Exception {

        /// <summary>
        /// Create a new exception instance.
        /// </summary>
        public QueueClosedException() : base("BlockingQueue has already been closed") { }
    }

    /// <summary>
    /// Provides a thread-safe queue that blocks on queue and dequeue operations under lock contention or when no items are available.
    /// </summary>
    /// <typeparam name="T">Type of the data items in the queue</typeparam>
    public interface IBlockingQueue<T> : IEnumerable<T> {

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> when the queue has been closed and can no longer accept new items, <see langword="False"/> otherwise.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Total number of items currently in the queue.
        /// </summary>
        int Count { get; }

        //--- Methods ---

        /// <summary>
        /// Attempt to dequeue an item from the queue.
        /// </summary>
        /// <remarks>Dequeue timeout can occur either because a lock could not be acquired or because no item was available.</remarks>
        /// <param name="timeout">Time to wait for an item to become available.</param>
        /// <param name="item">The location for a dequeue item.</param>
        /// <returns><see langword="True"/> if an item was dequeued, <see langword="False"/> if the operation timed out instead.</returns>
        bool TryDequeue(TimeSpan timeout, out T item);

        /// <summary>
        /// Blocking dequeue operation. Will not return until an item is available.
        /// </summary>
        /// <returns>A data item.</returns>
        /// <exception cref="QueueClosedException">Thrown when the queue is closed and has no more items.</exception>
        T Dequeue();

        /// <summary>
        /// Enqueue a new item into the queue.
        /// </summary>
        /// <param name="data">A data item.</param>
        /// <exception cref="QueueClosedException">Thrown when the queue is closed and does not accept new items.</exception>
        void Enqueue(T data);

        /// <summary>
        /// Close the queue and stop it from accepting more items.
        /// </summary>
        /// <remarks>Pending items can still be dequeued.</remarks>
        void Close();
    }

    /// <summary>
    /// Provides a thread-safe queue that blocks on queue and dequeue operations under lock contention or when no items are available.
    /// </summary>
    /// <typeparam name="T">Type of the data items in the queue</typeparam>
    public class BlockingQueue<T> : IBlockingQueue<T> {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly Queue<T> _queue = new Queue<T>();
        private bool _closed;

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> when the queue has been closed and can no longer accept new items, <see langword="False"/> otherwise.
        /// </summary>
        public bool IsClosed { get { return _closed; } }

        /// <summary>
        /// Total number of items currently in the queue.
        /// </summary>
        public int Count { get { lock(_queue) return _queue.Count; } }

        //--- Methods ----

        /// <summary>
        /// Blocking dequeue operation. Will not return until an item is available.
        /// </summary>
        /// <returns>A data item.</returns>
        /// <exception cref="QueueClosedException">Thrown when the queue is closed and has no more items.</exception>
        public T Dequeue() {
            T returnValue;
            if(!TryDequeue(TimeSpan.FromMilliseconds(-1), out returnValue)) {
                throw new QueueClosedException();
            }
            return returnValue;
        }

        /// <summary>
        /// Attempt to dequeue an item from the queue.
        /// </summary>
        /// <remarks>Dequeue timeout can occur either because a lock could not be acquired or because no item was available.</remarks>
        /// <param name="timeout">Time to wait for an item to become available.</param>
        /// <param name="item">The location for a dequeue item.</param>
        /// <returns><see langword="True"/> if an item was dequeued, <see langword="False"/> if the operation timed out instead.</returns>
        public bool TryDequeue(TimeSpan timeout, out T item) {
            item = default(T);
            if(IsClosed && _queue.Count == 0) {
                _log.Debug("dropping out of dequeue, queue is empty and closed (1)");
                return false;
            }
            lock(_queue) {
                while(_queue.Count == 0 && !IsClosed) {
                    if(!Monitor.Wait(_queue, timeout, false)) {
                        _log.Debug("dropping out of dequeue, timed out");
                        return false;
                    }
                }
                if(_queue.Count == 0 && IsClosed) {
                    _log.Debug("dropping out of dequeue, queue is empty and closed (2)");
                    return false;
                }
                item = _queue.Dequeue();
                return true;
            }
        }

        /// <summary>
        /// Enqueue a new item into the queue.
        /// </summary>
        /// <param name="data">A data item.</param>
        /// <exception cref="QueueClosedException">Thrown when the queue is closed and does not accept new items.</exception>
        public void Enqueue(T data) {
            if(_closed) {
                throw new InvalidOperationException("cannot enqueue new items on a closed queue");
            }
            lock(_queue) {
                _queue.Enqueue(data);
                Monitor.PulseAll(_queue);
            }
        }

        /// <summary>
        /// Close the queue and stop it from accepting more items.
        /// </summary>
        /// <remarks>Pending items can still be dequeued.</remarks>
        public void Close() {
            _log.Debug("closing queue");
            _closed = true;
            lock(_queue) {
                Monitor.PulseAll(_queue);
            }
        }

        //--- IEnumerable<T> Members ---
        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            while(true) {
                T returnValue;
                if(!TryDequeue(TimeSpan.FromMilliseconds(-1), out returnValue)) {
                    yield break;
                }
                yield return returnValue;
            }
        }

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}

