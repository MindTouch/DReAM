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
    /// A specialized queue that uses a two-phase dequeue to retrieve items
    /// </summary>
    /// <remarks>
    /// Items dequeued are invisible to other users of the queue, but are not permanently removed until the dequeue is either committed,
    /// or rolled back, the latter action making the item availabe for dequeuing again
    /// </remarks>
    /// <seealso cref="TransactionalQueue{T}"/>
    /// <typeparam name="T">Type of entries in the queue</typeparam>
    public interface ITransactionalQueue<T> : IDisposable {

        //--- Properties ---

        /// <summary>
        /// The default timeout used for a dequeued item before the item is considered abandoned and automatically rolled back
        /// </summary>
        TimeSpan DefaultCommitTimeout { get; set; }

        /// <summary>
        /// The current count of items available for <see cref="Dequeue()"/>
        /// </summary>
        /// <remarks>
        /// Count only reflects items available for dequeue, not items pending commit or rollback
        /// </remarks>
        int Count { get; }

        //--- Methods ---

        /// <summary>
        /// Clear out the queue and drop all items.
        /// </summary>
        void Clear();

        /// <summary>
        /// Put an item into the queue
        /// </summary>
        /// <param name="item">An instance of type T</param>
        void Enqueue(T item);

        /// <summary>
        /// Get the next available item from the queue. Must call <see cref="CommitDequeue"/> to fully take possession or the item. Uses <see cref="DefaultCommitTimeout"/>.
        /// </summary>
        /// <remarks>
        /// Phase 1 of the two-phase dequeue. <see cref="CommitDequeue"/> within <see cref="DefaultCommitTimeout"/> completes the dequeue, while waiting for the timeout to
        /// expire or calling <see cref="RollbackDequeue"/> aborts the dequeue.
        /// </remarks>
        /// <returns>
        /// An instance of <see cref="ITransactionalQueueEntry{T}"/> wrapping the dequeued value and item id for use 
        /// with <see cref="CommitDequeue"/> or <see cref="RollbackDequeue"/>. <see langword="null"/> if the queue is empty.
        /// </returns>
        ITransactionalQueueEntry<T> Dequeue();

        /// <summary>
        /// Get the next available item from the queue. Must call <see cref="CommitDequeue"/> to fully take possession or the item.
        /// </summary>
        /// <remarks>
        /// Phase 1 of the two-phase dequeue. <see cref="CommitDequeue"/> within <b>commitTimeout</b> completes the dequeue, while waiting for the timeout to
        /// expire or calling <see cref="RollbackDequeue"/> aborts the dequeue.
        /// </remarks>
        /// <param name="commitTimeout">Time before an uncommitted dequeue is considered abandoned and automatically rolled back</param>
        /// <returns>
        /// An instance of <see cref="ITransactionalQueueEntry{T}"/> wrapping the dequeued value and item id for use 
        /// with <see cref="CommitDequeue"/> or <see cref="RollbackDequeue"/>. <see langword="null"/> if the queue is empty.
        /// </returns>
        ITransactionalQueueEntry<T> Dequeue(TimeSpan commitTimeout);

        /// <summary>
        /// Completes the two-phase <see cref="Dequeue()"/>.
        /// </summary>
        /// <param name="id"><see cref="ITransactionalQueueEntry{T}.Id"/> identifier of the dequeued item</param>
        /// <returns><see langword="true"/> if the commit succeeded, <see langword="false"/> if the item was already dequeued or has been rolled back</returns>
        bool CommitDequeue(long id);

        /// <summary>
        /// Undo <see cref="Dequeue()"/> and return item back to the queue.
        /// </summary>
        /// <param name="id"><see cref="ITransactionalQueueEntry{T}.Id"/> identifier of the dequeued item</param>
        void RollbackDequeue(long id);
    }

    /// <summary>
    /// A entry returned from <see cref="ITransactionalQueue{T}.Dequeue()"/> containing both the dequeued value and an entry Id to be
    /// used with <see cref="ITransactionalQueue{T}.CommitDequeue"/> or <see cref="ITransactionalQueue{T}.RollbackDequeue"/>.
    /// </summary>
    /// <typeparam name="T">Type of entries in the originating <see cref="ITransactionalQueue{T}"/></typeparam>
    public interface ITransactionalQueueEntry<T> {

        //--- Properties ---

        /// <summary>
        /// The dequeued value
        /// </summary>
        T Value { get; }

        /// <summary>
        /// The entry Id for use with <see cref="ITransactionalQueue{T}.CommitDequeue"/> or <see cref="ITransactionalQueue{T}.RollbackDequeue"/>.
        /// </summary>
        long Id { get; }

        /// <summary>
        /// The time at which this entry will be considered abananded and automatically rolled back.
        /// </summary>
        DateTime Expiration { get; }
    }
}