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
using log4net;
using MindTouch.IO;

namespace MindTouch.Collections {

    /// <summary>
    /// A specialized queue that uses a two-phase dequeue to retrieve items
    /// </summary>
    /// <remarks>
    /// Items dequeued are invisible to other users of the queue, but are not permanently removed until the dequeue is either committed,
    /// or rolled back, the latter action making the item availabe for dequeuing again
    /// </remarks>
    /// <typeparam name="T">Type of entries in the queue</typeparam>
    public class TransactionalQueue<T> : ITransactionalQueue<T> {

        //--- Types ---
        private class Item : ITransactionalQueueEntry<T> {

            //--- Fields ---
            public readonly IQueueStreamHandle Handle;
            private readonly T _data;

            //--- Constructors ---
            public Item(T data, IQueueStreamHandle handle) {
                _data = data;
                Handle = handle;
            }

            //--- Properties ---
            public T Value { get { return _data; } }
            public long Id { get; set; }
            public DateTime Expiration { get; set; }

            //--- Methods ---
            public Item Clone() {
                return new Item(_data, Handle);
            }
        }

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly IQueueStream _stream;
        private readonly IQueueItemSerializer<T> _serializer;
        private readonly Dictionary<long, Item> _pending = new Dictionary<long, Item>();
        private readonly Queue<Item> _available = new Queue<Item>();
        private bool _isDisposed;
        private long _currentId = new Random().Next(int.MaxValue);
        private DateTime _nextCollect = DateTime.MinValue;

        //--- Constructors ---

        /// <summary>
        /// Create a new Queue given an <see cref="IQueueStream"/> storage provider and an <see cref="IQueueItemSerializer{T}"/> serializer for type T.
        /// </summary>
        /// <remarks>
        /// This class assumes ownership of the provided <see cref="IQueueStream"/> resource and will dispose it upon instance disposal
        /// </remarks>
        /// <param name="stream">An implementation of <see cref="IQueueStream"/> <seealso cref="SingleFileQueueStream"/><seealso cref="MultiFileQueueStream"/></param>
        /// <param name="serializer">A serializer implementation of <see cref="IQueueItemSerializer{T}"/> for type T</param>
        public TransactionalQueue(IQueueStream stream, IQueueItemSerializer<T> serializer) {
            _stream = stream;
            _serializer = serializer;
            DefaultCommitTimeout = TimeSpan.FromSeconds(30);
        }

        //--- Properties ---

        /// <summary>
        /// The default timeout used for a dequeued item before the item is considered abandoned and automatically rolled back
        /// </summary>
        public TimeSpan DefaultCommitTimeout { get; set; }

        /// <summary>
        /// The current count of items available for <see cref="Dequeue()"/>
        /// </summary>
        /// <remarks>
        /// Count only reflects items available for dequeue, not items pending commit or rollback
        /// </remarks>
        public int Count {
            get {
                EnsureInstanceNotDisposed();
                ProcessExpiredItems();
                return _stream.UnreadCount + _available.Count;
            }
        }

        //--- Methods ---

        /// <summary>
        /// Clear out the queue and drop all items, including pending commits.
        /// </summary>
        public void Clear() {
            EnsureInstanceNotDisposed();
            _log.DebugFormat("Clearing queue");
            lock(_available) {
                _stream.Truncate();
                _available.Clear();
                _pending.Clear();
            }
        }

        /// <summary>
        /// Put an item into the queue
        /// </summary>
        /// <param name="item">An instance of type T</param>
        public void Enqueue(T item) {
            EnsureInstanceNotDisposed();
            var data = _serializer.ToStream(item);
            lock(_available) {
                _stream.AppendRecord(data, data.Length);
            }
        }

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
        public ITransactionalQueueEntry<T> Dequeue() {
            return Dequeue(DefaultCommitTimeout);
        }

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
        public ITransactionalQueueEntry<T> Dequeue(TimeSpan commitTimeout) {
            EnsureInstanceNotDisposed();
            lock(_available) {
                Item pending = null;
                ProcessExpiredItems();
                if(_available.Count > 0) {
                    pending = _available.Dequeue().Clone();
                } else {
                    while(pending == null) {
                        var data = _stream.ReadNextRecord();
                        if(data == QueueStreamRecord.Empty) {
                            return null;
                        }
                        try {
                            pending = new Item(_serializer.FromStream(data.Stream), data.Handle);
                        } catch(Exception e) {
                            _log.Warn("message failed to deserialize", e);
                        }
                    }
                }
                pending.Id = _currentId++;
                if( commitTimeout == TimeSpan.MinValue) {
                    pending.Expiration = DateTime.MinValue;
                } else if(commitTimeout == TimeSpan.MaxValue) {
                    pending.Expiration = DateTime.MaxValue;
                } else {
                    pending.Expiration = DateTime.UtcNow.Add(commitTimeout);
                }
                if(_nextCollect > pending.Expiration) {
                    _nextCollect = pending.Expiration;
                }
                _pending.Add(pending.Id, pending);
                return pending;
            }
        }

        /// <summary>
        /// Completes the two-phase <see cref="Dequeue()"/>.
        /// </summary>
        /// <param name="id"><see cref="ITransactionalQueueEntry{T}.Id"/> identifier of the dequeued item</param>
        /// <returns><see langword="true"/> if the commit succeeded, <see langword="false"/> if the item was already dequeued or has been rolled back</returns>
        public bool CommitDequeue(long id) {
            EnsureInstanceNotDisposed();
            Item item;
            lock(_available) {
                ProcessExpiredItems();
                if(!_pending.TryGetValue(id, out item)) {
                    return false;
                }
                _pending.Remove(id);
                _stream.DeleteRecord(item.Handle);
            }
            return true;
        }

        /// <summary>
        /// Undo <see cref="Dequeue()"/> and return item back to the queue.
        /// </summary>
        /// <param name="id"><see cref="ITransactionalQueueEntry{T}.Id"/> identifier of the dequeued item</param>
        public void RollbackDequeue(long id) {
            EnsureInstanceNotDisposed();
            lock(_available) {
                ProcessExpiredItems();
                Item item;
                if(!_pending.TryGetValue(id, out item)) {
                    return;
                }
                _pending.Remove(id);
                _available.Enqueue(item);
            }
        }

        /// <summary>
        /// Clean up the resources used by the Queue per the <see cref="IDisposable"/> pattern
        /// </summary>
        public void Dispose() {
            if(!_isDisposed) {
                lock(_available) {
                    _isDisposed = true;
                    _stream.Dispose();
                }
            }
        }

        private void ProcessExpiredItems() {
            lock(_available) {
                var now = DateTime.UtcNow;
                if(_pending.Count == 0 || now < _nextCollect) {
                    return;
                }
                var nextCollect = DateTime.MaxValue;
                var released = new List<long>();
                foreach(var item in _pending.Values) {
                    if(item.Expiration <= now) {
                        released.Add(item.Id);
                        _available.Enqueue(item);
                    } else if(item.Expiration < nextCollect) {
                        nextCollect = item.Expiration;
                    }
                }
                _nextCollect = nextCollect;
                foreach(var id in released) {
                    _pending.Remove(id);
                }
            }
        }

        private void EnsureInstanceNotDisposed() {
            if(_isDisposed) {
                throw new ObjectDisposedException("the queue has been disposed");
            }
        }
    }
}