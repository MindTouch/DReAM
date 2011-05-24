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
    /// Provides an implementation of <see cref="IThreadsafeQueue{T}"/> that does not incur locking overhead to provide thread-safe access to its members.
    /// </summary>
    /// <typeparam name="T">Type of item the queue can contain.</typeparam>
    public class LockFreeItemConsumerQueue<T> : IThreadsafeQueue<T>, IThreadsafeQueue<Action<T>> {

        //--- Fields ---
        private SingleLinkNode<object> _head;
        private SingleLinkNode<object> _tail;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance of the queue.
        /// </summary>
        public LockFreeItemConsumerQueue() {
            _head = new SingleLinkNode<object>();
            _tail = _head;
        }

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if there are items in the queue.
        /// </summary>
        public bool ItemIsEmpty {
            get {
                SingleLinkNode<object> curHead = _head;
                SingleLinkNode<object> curTail = _tail;
                SingleLinkNode<object> curHeadNext = curHead.Next;
                return (ReferenceEquals(curHead, curTail) && (curHeadNext == null)) || (curTail.Item is Action<T>);
            }
        }

        /// <summary>
        /// Total number of items in the queue.
        /// </summary>
        public int ItemCount {
            get {
                int count = 0;
                SingleLinkNode<object> curHead = _head;
                SingleLinkNode<object> next = curHead.Next;
                if((next != null) && !(next.Item is Action<T>)) {
                    do {
                        ++count;
                        next = next.Next;
                    } while(next != null);
                }
                return count;
            }
        }

        /// <summary>
        /// <see langword="True"/> if there are consumers in the queue.
        /// </summary>
        public bool ConsumerIsEmpty {
            get {
                SingleLinkNode<object> curHead = _head;
                SingleLinkNode<object> curTail = _tail;
                SingleLinkNode<object> curHeadNext = curHead.Next;
                return (ReferenceEquals(curHead, curTail) && (curHeadNext == null)) || !(curTail.Item is Action<T>);
            }
        }

        /// <summary>
        /// Total number of consumers in the queue.
        /// </summary>
        public int ConsumerCount {
            get {
                int count = 0;
                SingleLinkNode<object> curHead = _head;
                SingleLinkNode<object> next = curHead.Next;
                if((next != null) && (next.Item is Action<T>)) {
                    do {
                        ++count;
                        next = next.Next;
                    } while(next != null);
                }
                return count;
            }
        }

        //--- Methods ---

        /// <summary>
        /// Try to add an item to the queue.
        /// </summary>
        /// <param name="item">Item to add to queue.</param>
        /// <returns>Always returns <see langword="True"/>.</returns>
        public bool TryEnqueue(T item) {
            SingleLinkNode<object> newTail = null;
            while(true) {

                // check if we have a consumer ready
                Action<T> consumer;
                if(TryDequeueConsumer(out consumer)) {
                    consumer(item);
                    return true;
                }

                // try to enqueue the item
                newTail = newTail ?? new SingleLinkNode<object>(item);
                if(TryEnqueueItem(newTail)) {
                    return true;
                }
            }
        }

        /// <summary>
        /// Try to get an item from the queue.
        /// </summary>
        /// <param name="item">Storage location for the item to be removed.</param>
        /// <returns><see langword="True"/> if the dequeue succeeded.</returns>
        public bool TryDequeue(out T item) {
            return TryDequeueItem(out item);
        }

        /// <summary>
        /// Try to add a consumer to the queue.
        /// </summary>
        /// <param name="callback">Consumer to add to queue.</param>
        /// <returns>Always returns <see langword="True"/>.</returns>
        public bool TryEnqueue(Action<T> callback) {
            if(callback == null) {
                throw new ArgumentNullException("callback");
            }
            SingleLinkNode<object> newTail = null;
            while(true) {

                // check if we have an item ready
                T item;
                if(TryDequeueItem(out item)) {
                    callback(item);
                    return true;
                }

                // try to enqueue the callback
                newTail = newTail ?? new SingleLinkNode<object>(callback);
                if(TryEnqueueConsumer(newTail)) {
                    return true;
                }
            }
        }

        /// <summary>
        /// Try to get a consumer from the queue.
        /// </summary>
        /// <param name="callback">Storage location for the consumer to be removed.</param>
        /// <returns><see langword="True"/> if the dequeue succeeded.</returns>
        public bool TryDequeue(out Action<T> callback) {
            return TryDequeueConsumer(out callback);
        }

        private bool TryEnqueueItem(SingleLinkNode<object> newTail) {

            // loop until we successful enqueue the new tail node
            while(true) {

                // capture the current tail reference and its current Next reference
                SingleLinkNode<object> curHead = _head;
                SingleLinkNode<object> curTail = _tail;
                SingleLinkNode<object> curTailNext = curTail.Next;

                // check if the current tail is indeed the last node
                if(curTailNext == null) {

                    // ensure if the queue is not empty, that it contains items of the expected type
                    if(!ReferenceEquals(curHead, curTail) && (curTail.Item is Action<T>)) {
                        return false;
                    }

                    // update the tail's Next reference to point to the new entry
                    if(SysUtil.CAS(ref _tail.Next, null, newTail)) {

                        // NOTE (steveb): there is a race-condition here where we may update the tail to point a non-terminal node; that's ok

                        // update the tail reference to the new entry (may fail)
                        SysUtil.CAS(ref _tail, curTail, newTail);
                        return true;
                    }
                } else {

                    // tail reference was not properly updated in an earlier attempt, update it now (see note above) 
                    SysUtil.CAS(ref _tail, curTail, curTailNext);
                }
            }
        }

        private bool TryEnqueueConsumer(SingleLinkNode<object> newTail) {

            // loop until we successful enqueue the new tail node
            while(true) {

                // capture the current tail reference and its current Next reference
                SingleLinkNode<object> curHead = _head;
                SingleLinkNode<object> curTail = _tail;
                SingleLinkNode<object> curTailNext = curTail.Next;

                // check if the current tail is indeed the last node
                if(curTailNext == null) {

                    // ensure if the queue is not empty, that it contains items of the expected type
                    if(!ReferenceEquals(curHead, curTail) && !(curTail.Item is Action<T>)) {
                        return false;
                    }

                    // update the tail's Next reference to point to the new entry
                    if(SysUtil.CAS(ref _tail.Next, null, newTail)) {

                        // NOTE (steveb): there is a race-condition here where we may update the tail to point a non-terminal node; that's ok

                        // update the tail reference to the new entry (may fail)
                        SysUtil.CAS(ref _tail, curTail, newTail);
                        return true;
                    }
                } else {

                    // tail reference was not properly updated in an earlier attempt, update it now (see note above) 
                    SysUtil.CAS(ref _tail, curTail, curTailNext);
                }
            }
        }

        private bool TryDequeueItem(out T item) {

            // TODO (arnec): should convert return to enum to indicate contention vs. empty queue

            // loop until we successfully dequeue a node or the queue is empty
            while(true) {

                // capture the current state of the queue
                SingleLinkNode<object> curHead = _head;
                SingleLinkNode<object> curHeadNext = curHead.Next;
                SingleLinkNode<object> curTail = _tail;

                // check if the current head and tail are equal
                if(ReferenceEquals(curHead, curTail)) {

                    // check if the current head has a non-empty Next reference
                    if(curHeadNext == null) {

                        // unable to find an item in the queue
                        item = default(T);
                        return false;
                    }

                    // tail reference was not properly updated in an earlier attempt, update it now (see note above) 
                    SysUtil.CAS(ref _tail, curTail, curHeadNext);
                } else if(curHeadNext == null) {

                    // head and tail differ, but we have no next, i.e. contention changed the queue before we
                    // captured its state
                    item = default(T);
                    return false;
                } else if(!(curHeadNext.Item is Action<T>)) {

                    // try to replace the current head with the current head's Next reference
                    if(SysUtil.CAS(ref _head, curHead, curHeadNext)) {

                        // we have successfully retrieved the head of the queue
                        item = (T)curHeadNext.Item;

                        // clear out the Item field so the GC can reclaim the memory
                        curHeadNext.Item = default(T);
                        return true;
                    }
                } else {

                    // head contains a consumer instead of an item
                    item = default(T);
                    return false;
                }
            }
        }

        private bool TryDequeueConsumer(out Action<T> callback) {

            // TODO (arnec): should convert return to enum to indicate contention vs. empty queue

            // loop until we successfully dequeue a node or the queue is empty
            while(true) {

                // capture the current state of the queue
                SingleLinkNode<object> curHead = _head;
                SingleLinkNode<object> curHeadNext = curHead.Next;
                SingleLinkNode<object> curTail = _tail;

                // check if the current head and tail are equal
                if(ReferenceEquals(curHead, curTail)) {

                    // check if the current head has a non-empty Next reference
                    if(curHeadNext == null) {

                        // unable to find an item in the queue
                        callback = null;
                        return false;
                    }

                    // tail reference was not properly updated in an earlier attempt, update it now (see note above) 
                    SysUtil.CAS(ref _tail, curTail, curHeadNext);
                } else if(curHeadNext == null) {

                    // head and tail differ, but we have no next, i.e. contention changed the queue before we
                    // captured its state
                    callback = null;
                    return false;
                } else if(curHeadNext.Item is Action<T>) {

                    // try to replace the current head with the current head's Next reference
                    if(SysUtil.CAS(ref _head, curHead, curHeadNext)) {

                        // we have successfully retrieved the head of the queue
                        callback = (Action<T>)curHeadNext.Item;

                        // clear out the Item field so the GC can reclaim the memory
                        curHeadNext.Item = default(T);
                        return true;
                    }
                } else {

                    // head contains an item instead of a callback
                    callback = null;
                    return false;
                }
            }
        }

        #region --- IThreadsafeQueue<T> Members ---
        bool IThreadsafeQueue<T>.IsEmpty { get { return ItemIsEmpty; } }
        int IThreadsafeQueue<T>.Count { get { return ItemCount; } }
        #endregion

        #region --- IThreadsafeQueue<Action<T>> Members ---
        bool IThreadsafeQueue<Action<T>>.IsEmpty { get { return ConsumerIsEmpty; } }
        int IThreadsafeQueue<Action<T>>.Count { get { return ConsumerCount; } }
        #endregion
    }
}
