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
    /// Provides a queue that allows items to pushed and popped by a single thread while 
    /// other threads may attempt steal from it.  The implementation is based on the work done by 
    /// Danny Hendler, Yossi Lev, Mark Moir, and Nir Shavit: "A dynamic-sized nonblocking work stealing deque", 
    /// Distributed Computing, Volume 18, Issue 3 (February 2006), pp189-207, ISSN:0178-2770 
    /// </summary>
    /// <typeparam name="T">Collection item type.</typeparam>
    public sealed class WorkStealingDeque<T> {

        //--- Constants ---
        private const int DEFAULT_CAPACITY = 32;

        //--- Types ---
        internal class BottomData {

            //--- Fields ---
            internal readonly DequeNode Node;
            internal readonly int Index;

            //--- Constructors ---
            internal BottomData(DequeNode node, int index) {
                this.Node = node;
                this.Index = index;
            }
        }

        internal class TopData {

            //--- Fields ---
            internal readonly int Tag;
            internal readonly DequeNode Node;
            internal readonly int Index;

            //--- Constructors ---
            internal TopData(int tag, DequeNode node, int index) {
                this.Tag = tag;
                this.Node = node;
                this.Index = index;
            }
        }

        internal class DequeNode {

            //--- Fields ---
            internal readonly T[] Data;
            internal DequeNode Next;
            internal DequeNode Prev;

            //--- Constructors ---
            internal DequeNode(int capacity, DequeNode next) {
                Data = new T[capacity];
                if(next != null) {
                    this.Next = next;
                    next.Prev = this;
                }
            }
        }

        //--- Class Methods ---
        private static bool IsEmpty(BottomData bottom, TopData top, int capacity) {
            if(ReferenceEquals(bottom.Node, top.Node) && ((bottom.Index == top.Index) || (bottom.Index == (top.Index + 1)))) {
                return true;
            } else if(ReferenceEquals(bottom.Node, top.Node.Next) && (bottom.Index == 0) && (top.Index == (capacity - 1))) {
                return true;
            }
            return false;
        }

        //--- Fields ---
        private readonly int _capacity;
        private BottomData _bottom;
        private TopData _top;

        //--- Constructors ---
        /// <summary>
        /// Create a new instance.
        /// </summary>
        public WorkStealingDeque() : this(DEFAULT_CAPACITY) { }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="capacity">Maximum number of items in the queue.</param>
        public WorkStealingDeque(int capacity) {
            _capacity = capacity;
            DequeNode nodeB = new DequeNode(_capacity, null);
            DequeNode nodeA = new DequeNode(_capacity, nodeB);
            _bottom = new BottomData(nodeA, _capacity - 1);
            _top = new TopData(0, nodeA, _capacity - 1);
        }

        //--- Properties ---

        /// <summary>
        /// Total number of items in queue.
        /// </summary>
        public int Count {
            get {
                BottomData curBottom = _bottom;
                TopData curTop = _top;
                int count;

                // check if top and bottom share the same node
                if(ReferenceEquals(curBottom.Node, curTop.Node)) {
                    count = Math.Max(0, curTop.Index - curBottom.Index);
                } else if(ReferenceEquals(curBottom.Node, curTop.Node.Next) && (curBottom.Index == 0) && (curTop.Index == (_capacity - 1))) {
                    count = 0;
                } else {
                    count = _capacity - (curBottom.Index + 1);
                    for(var node = curBottom.Node.Next; (node != curTop.Node) && (node != null); node = node.Next) {
                        count += _capacity;
                    }
                    count += curTop.Index + 1;
                }
                return count;
            }
        }

        //--- Methods ---

        /// <summary>
        /// Push an item onto the tail of the queue.
        /// </summary>
        /// <remarks>
        /// NOTE: Push() and TryPop() <strong>MUST</strong> be called from the same thread.
        /// </remarks>
        /// <param name="data">Item to push onto the tail of the queue.</param>
        public void Push(T data) {

            // read bottom data
            BottomData curBottom = _bottom;

            // write data in current bottom cell
            curBottom.Node.Data[curBottom.Index] = data;
            BottomData newBottom;
            if(curBottom.Index != 0) {
                newBottom = new BottomData(curBottom.Node, curBottom.Index - 1);
            } else {

                // allocate and link a new node
                DequeNode newNode = new DequeNode(_capacity, curBottom.Node);
                newBottom = new BottomData(newNode, _capacity - 1);
            }

            // update bottom
            _bottom = newBottom;
        }

        /// <summary>
        /// Pop an item from the tail of the queue.
        /// </summary>
        /// <remarks>
        /// NOTE: Push() and TryPop() <strong>MUST</strong> be called from the same thread.
        /// </remarks>
        /// <param name="item">Tail item of the queue when operation is successful.</param>
        /// <returns><see langword="True"/> if operation was successful.</returns>
        public bool TryPop(out T item) {
            item = default(T);

            // read bottom data
            BottomData curBottom = _bottom;
            BottomData newBottom;
            if(curBottom.Index != (_capacity - 1)) {
                newBottom = new BottomData(curBottom.Node, curBottom.Index + 1);
            } else {
                newBottom = new BottomData(curBottom.Node.Next, 0);
            }

            // update bottom
            _bottom = newBottom;

            // read top
            TopData curTop = _top;

            // read data to be popped
            T retVal = newBottom.Node.Data[newBottom.Index];

            // case 1: if _top has crossed _bottom
            if(ReferenceEquals(curBottom.Node, curTop.Node) && (curBottom.Index == curTop.Index)) {

                // return bottom to its old position
                _bottom = curBottom;
                return false;
            }

            // case 2: when popping the last entry in the deque (i.e. deque is empty after the update of bottom)
            if(ReferenceEquals(newBottom.Node, curTop.Node) && (newBottom.Index == curTop.Index)) {

                // try to update _top's tag so no concurrent Steal operation will also pop the same entry
                TopData newTopVal = new TopData(curTop.Tag + 1, curTop.Node, curTop.Index);
                if(SysUtil.CAS(ref _top, curTop, newTopVal)) {

                    // TODO (steveb): clear out the entry we read, so the GC can reclaim it

                    // free old node if needed
                    if(!ReferenceEquals(curBottom.Node, newBottom.Node)) {
                        newBottom.Node.Prev = null;
                    }
                    item = retVal;
                    return true;
                } else {

                    // if CAS failed (i.e. a concurrent Steal operation alrady popped that last entry)

                    // return bottom to its old position
                    _bottom = curBottom;
                    return false;
                }
            }

            // case 3: regular case (i.e. there was a least one entry in the deque _after_ bottom's update)
            // free old node if needed
            if(!ReferenceEquals(curBottom.Node, newBottom.Node)) {
                newBottom.Node.Prev = null;
            }
            item = retVal;
            return true;
        }

        /// <summary>
        /// Pop an item from the head of the queue.
        /// </summary>
        /// <remarks>
        /// NOTE: TrySteal() can be invoked from any thread.
        /// </remarks>
        /// <param name="item">Head item of the queue when operation is successful.</param>
        /// <returns><see langword="True"/> if operation was successful.</returns>
        public bool TrySteal(out T item) {

            // read top
            TopData curTop = _top;

            // read bottom
            BottomData curBottom = _bottom;
            if(IsEmpty(curBottom, curTop, _capacity)) {
                item = default(T);
                if(ReferenceEquals(curTop, _top)) {
                    return false;
                } else {

                    // NOTE (steveb): this is contentious access case; we currently return 'false' but may want to differentiate in the future
                    return false;
                }
            }

            // if deque isn't empty, calcuate next top pointer
            TopData newTop;
            if(curTop.Index != 0) {

                // stay at current node
                newTop = new TopData(curTop.Tag, curTop.Node, curTop.Index - 1);
            } else {
                
                // move to next node and update tag
                newTop = new TopData(curTop.Tag + 1, curTop.Node.Prev, _capacity - 1);
            }

            // read value
            T retVal = curTop.Node.Data[curTop.Index];

            // try updating _top using CAS
            if(SysUtil.CAS(ref _top, curTop, newTop)) {

                // TODO (steveb): clear out the entry we read, so the GC can reclaim it

                // free old node
                curTop.Node.Next = null;
                item = retVal;
                return true;
            } else {
                item = default(T);

                // NOTE (steveb): this is contentious access case; we currently return 'false' but may want to differentiate in the future
                return false;
            }
        }
    }
}
