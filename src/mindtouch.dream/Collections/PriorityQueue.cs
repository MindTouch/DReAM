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

namespace System.Collections.Generic {

    /// <summary>
    /// Delegate used by <see cref="PriorityQueue{T}"/> to determine priority order of items.
    /// </summary>
    /// <typeparam name="T">Type of item in queue.</typeparam>
    /// <param name="first">First item.</param>
    /// <param name="second">Second item.</param>
    /// <returns>Negative if the second item should come before the first, otherwise the order remains the same.</returns>
    public delegate int PriorityQueueComparer<T>(T first, T second);

    /// <summary>
    /// Provides a queue that determines the dequeue order of items by priority.
    /// </summary>
    /// <typeparam name="T">Type of item in the queue.</typeparam>
    public class PriorityQueue<T> {

        //--- Fields ---

        /// <summary>
        /// Internal storage of queue items.
        /// </summary>
        protected readonly List<T> _list;

        /// <summary>
        /// Comparison delegate for doing queue priority ordering.
        /// </summary>
        protected readonly PriorityQueueComparer<T> _comparer;

        //--- Constructors ---

        /// <summary>
        /// Create a new queue instance.
        /// </summary>
        /// <param name="comparer">Comparison delegate for ordering queue items by priority.</param>
        public PriorityQueue(PriorityQueueComparer<T> comparer) : this(32, comparer) { }

        /// <summary>
        /// Create a new queue instance.
        /// </summary>
        /// <param name="capacity">Initial capacity of queue.</param>
        /// <param name="comparer">Comparison delegate for ordering queue items by priority.</param>
        public PriorityQueue(int capacity, PriorityQueueComparer<T> comparer) {
            _comparer = comparer;
            _list = new List<T>(capacity);
        }

        //--- Properties ---

        /// <summary>
        /// Number of items in the queue.
        /// </summary>
        public int Count { get { return _list.Count; } }

        /// <summary>
        /// <see langword="True"/> if there are no items in the queue.
        /// </summary>
        public bool IsEmpty { get { return Count == 0; } }
        
        //--- Methods ---

        /// <summary>
        /// Add an item to queue.
        /// </summary>
        /// <param name="item">Item to add.</param>
        public void Enqueue(T item) {

            // add item to the end of the list
            int current = _list.Count;
            _list.Add(item);
            RebalanceBinaryArrayStartingAt(current);
        }

        /// <summary>
        /// Get the next item from the queue.
        /// </summary>
        /// <returns>The next item from the queue.</returns>
        public T Dequeue() {

            // remove first item (always the smallest one)
            T result = _list[0];

            // move last item into first position and decrease size of list
            _list[0] = _list[_list.Count - 1];
            _list.RemoveAt(_list.Count - 1);
            RebalanceBinaryArrayStartingAt(0);
            return result;
        }

        /// <summary>
        /// Get the next item in the queue without removing it.
        /// </summary>
        /// <returns></returns>
        public T Peek() {
            return _list[0];
        }

        /// <summary>
        /// Remove an item from the queue.
        /// </summary>
        /// <param name="item">The item to remove from the queue.</param>
        /// <returns><see langword="True"/> if the remove succeeded.</returns>
        public bool Remove(T item) {

            // TODO (steveb): we should use the comparer to locate the item in O(log(n)) time instead of O(n)

            int index = _list.IndexOf(item);
            if(index < 0) {
                return false;
            }

            // check if we're removing the last item, then there is nothing to do
            if(index != (_list.Count - 1)) {

                // put the last item into the removed item's position and rebalance the array
                _list[index] = _list[_list.Count - 1];
                _list.RemoveAt(_list.Count - 1);
                RebalanceBinaryArrayStartingAt(index);
            } else {
                _list.RemoveAt(_list.Count - 1);
            }
            return true;
        }

        /// <summary>
        /// Clear all items from the queue.
        /// </summary>
        public void Clear() {
            _list.Clear();
        }

        /// <summary>
        /// Switch the position of two index positions in the queue.
        /// </summary>
        /// <param name="i">First index to switch into second.</param>
        /// <param name="j">Second index to switch into first.</param>
        protected void SwitchElements(int i, int j) {
            T tmp = _list[i];
            _list[i] = _list[j];
            _list[j] = tmp;
        }
        
        /// <summary>
        /// Rebalance the queue starting at a specific index into <see cref="_list"/>.
        /// </summary>
        /// <param name="i"></param>
        protected void RebalanceBinaryArrayStartingAt(int i) {
            int current = i;

            // determine proper position for the current item by progressively promoting it in the binary list
            while(current != 0) {
                int parent = (current - 1) / 2;
                if(_comparer(_list[current], _list[parent]) < 0) {
                    SwitchElements(current, parent);
                    current = parent;
                } else {
                    break;
                }
            }

            // we can stop if current item was promoted
            if(current < i) {
                return;
            }

            // determine proper position for current item by progressively demoting it into the binary list
            while(true) {
                int position = current;
                int left = 2 * current + 1;
                int right = 2 * current + 2;

                // check if left branch is smaller than item
                if((left < _list.Count) && (_comparer(_list[left], _list[current]) < 0)) {
                    current = left;
                }

                // check if right branch is even smaller
                if((right < _list.Count) && (_comparer(_list[right], _list[current]) < 0)) {
                    current = right;
                }

                // check if any progress was made
                if(current == position) {
                    break;
                }
                SwitchElements(current, position);
            }
        }
    }
}