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
    /// Provides and implementation of <see cref="IThreadsafeStack{T}"/> that does not incur locking overhead to provide thread-safe access to its members.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LockFreeStack<T> : IThreadsafeStack<T>, IThreadsafeCollection<T> {

        //--- Fields ---
        private SingleLinkNode<T> _head;

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if the stack is empty.
        /// </summary>
        public bool IsEmpty {
            get {
                return _head == null;
            }
        }

        /// <summary>
        /// Total number of items on stack.
        /// </summary>
        public int Count {
            get {
                int count = 0;
                for(SingleLinkNode<T> node = _head; node != null; node = node.Next) {
                    ++count;
                }
                return count;
            }
        }

        //--- Methods ---

        /// <summary>
        /// Try to push a new item on top of the stack.
        /// </summary>
        /// <param name="item">Item to add.</param>
        /// <returns><see langword="True"/> if the push succeeded.</returns>
        public bool TryPush(T item) {
            SingleLinkNode<T> newNode = new SingleLinkNode<T>(item);
            do {
                newNode.Next = _head;
            } while(!SysUtil.CAS(ref _head, newNode.Next, newNode));
            return true;
        }

        /// <summary>
        /// Try to pop an item from the top of the stack
        /// </summary>
        /// <param name="item">Storage location for the item to be removed.</param>
        /// <returns><see langword="True"/> if the pop succeeded.</returns>
        public bool TryPop(out T item) {
            SingleLinkNode<T> node;
            do {
                node = _head;
                if(node == null) {
                    item = default(T);
                    return false;
                }
            } while(!SysUtil.CAS(ref _head, node, node.Next));
            item = node.Item;
            return true;
        }

        //--- IThreadsafeCollection<T> Members ---
        bool IThreadsafeCollection<T>.TryAdd(T item) {
            return TryPush(item);
        }

        bool IThreadsafeCollection<T>.TryRemove(out T item) {
            return TryPop(out item);
        }
    }
}
