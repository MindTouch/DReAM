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

namespace MindTouch.Collections {

    /// <summary>
    /// Provides a node container class for data in a singly linked list
    /// </summary>
    /// <typeparam name="T">Type of data contained by the node.</typeparam>
    public class SingleLinkNode<T> {

        //--- Fields ---

        /// <summary>
        /// Pointer to the next node in list.
        /// </summary>
        public SingleLinkNode<T> Next;

        /// <summary>
        /// The data contained by the node.
        /// </summary>
        public T Item;

        //--- Constructors ---

        /// <summary>
        /// Create a new node instance.
        /// </summary>
        public SingleLinkNode() { }

        /// <summary>
        /// Create a new node instance.
        /// </summary>
        /// <param name="item">Initial data value.</param>
        public SingleLinkNode(T item) {
            this.Item = item;
        }
    }
}
