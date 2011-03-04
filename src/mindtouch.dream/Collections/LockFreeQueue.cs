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
    public class LockFreeQueue<T> : IThreadsafeQueue<T>, IThreadsafeCollection<T> {

        //--- Fields ---
        private SingleLinkNode<T> _head;
        private SingleLinkNode<T> _tail;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance of the queue.
        /// </summary>
        public LockFreeQueue() {
            _head = new SingleLinkNode<T>();
            _tail = _head;
        }

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if the queue is empty.
        /// </summary>
        public bool IsEmpty {
            get {

                // capture the current state of the queue
                SingleLinkNode<T> oldHead = _head;
                SingleLinkNode<T> oldTail = _tail;
                SingleLinkNode<T> oldHeadNext = oldHead.Next;
                return ReferenceEquals(oldHead, oldTail) && (oldHeadNext == null);
            }
        }

        /// <summary>
        /// Total number of items in queue.
        /// </summary>
        public int Count {
            get {
                SingleLinkNode<T> curHead = _head;
                SingleLinkNode<T> curTail = _tail;
                int count = 0;
                for(SingleLinkNode<T> node = curHead; node != curTail; node = node.Next) {
                    ++count;
                }
                return count;
            }
        }

        //--- Methods ---

        /// <summary>
        /// Try to add an item to the queue.
        /// </summary>
        /// <param name="item">Item to add to queue.</param>
        /// <returns><see langword="True"/> if the enqueue succeeded.</returns>
        /// <returns>Always returns <see langword="True"/>.</returns>
        public bool TryEnqueue(T item) {

            // create new entry to add
            SingleLinkNode<T> newTail = new SingleLinkNode<T>(item);

            // loop until we successful enqueue the new tail node
            while(true) {

                // capture the current tail reference and its current Next reference
                SingleLinkNode<T> curTail = _tail;
                SingleLinkNode<T> curTailNext = curTail.Next;

                // check if the current tail is indeed the last node
                if(curTailNext == null) {

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

        /// <summary>
        /// Try to get an item from the queue.
        /// </summary>
        /// <param name="item">Storage location for the item to be removed.</param>
        /// <returns><see langword="True"/> if the dequeue succeeded.</returns>
        public bool TryDequeue(out T item) {

            // TODO (arnec): should convert return to enum to indicate contention vs. empty queue

            // loop until we successfully dequeue a node or the queue is empty
            while(true) {

                // capture the current state of the queue
                SingleLinkNode<T> curHead = _head;
                SingleLinkNode<T> curHeadNext = curHead.Next;
                SingleLinkNode<T> curTail = _tail;

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
                } else {

                    // try to replace the current head with the current head's Next reference
                    if(SysUtil.CAS(ref _head, curHead, curHeadNext)) {

                        // we have successfully retrieved the head of the queue
                        item = curHeadNext.Item;

                        // clear out the Item field so the GC can reclaim the memory
                        curHeadNext.Item = default(T);
                        return true;
                    }
                }
            }
        }

        //--- IThreadsafeCollection<T> Members ---
        bool IThreadsafeCollection<T>.TryAdd(T item) {
            return TryEnqueue(item);
        }

        bool IThreadsafeCollection<T>.TryRemove(out T item) {
            return TryDequeue(out item);
        }
    }
}
