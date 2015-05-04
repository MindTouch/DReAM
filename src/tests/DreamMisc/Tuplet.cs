/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2015 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
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
using System.Text;

namespace MindTouch {
    /// <summary>
    /// Interface for a generic ordered list of items.
    /// </summary>
    [Obsolete("Use System.Tuple")]
    internal interface ITuplet {

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Accessor to tuple elements by position.
        /// </summary>
        /// <param name="index">Index of item.</param>
        /// <returns>Value of item.</returns>
        object this[int index] {
            get;
            set;
        }

        //--- Methods ---

        /// <summary>
        /// Convert tuple into an array of elements.
        /// </summary>
        /// <returns>Array of objects.</returns>
        object[] ToArray();
    }

    /// <summary>
    /// Abstract base class for creating concretely sized Tuple classes.
    /// </summary>
    [Obsolete("Use System.Tuple")]
    internal abstract class ATuplet : ITuplet, IEnumerable, IComparable {

        //--- Constructors ---
        /// <summary>
        /// Create a new instance without initial values.
        /// </summary>
        protected ATuplet() { }

        /// <summary>
        /// Create a new instances with initial values.
        /// </summary>
        /// <param name="items">Array of initial values.</param>
        protected ATuplet(object[] items) {
            if((items == null) || (items.Length != Count)) {
                throw new ArgumentException("items");
            }
            for(int i = 0; i < items.Length; ++i) {
                this[i] = items[i];
            }
        }

        //--- Abstract Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        /// <remarks>This Property is abstract and must be implemented by the implementing class</remarks>
        public abstract int Count { get; }

        /// <summary>
        /// Accessor to tuple elements by position.
        /// </summary>
        /// <param name="index">Index of item.</param>
        /// <returns>Value of item.</returns>
        /// <remarks>This Property is abstract and must be implemented by the implementing class</remarks>
        public abstract object this[int index] {
            get;
            set;
        }

        //--- Methods ---
        /// <summary>
        /// Convert tuple into an array of elements.
        /// </summary>
        /// <returns>Array of objects.</returns>
        public object[] ToArray() {
            object[] result = new object[Count];
            for(int i = 0; i < result.Length; ++i) {
                result[i] = this[i];
            }
            return result;
        }

        int IComparable.CompareTo(object other) {

            // check if other element is a tuple
            ITuplet tuplet = other as ITuplet;
            if(tuplet == null) {
                throw new ArgumentNullException("other");
            }

            // check if both tuple have the same length
            int delta = (Count - tuplet.Count);
            if(delta != 0) {
                return delta;
            }

            // compare each element in the tuple
            for(int i = 0; i < Count; ++i) {
                IComparable a = this[i] as IComparable;
                IComparable b = this[i] as IComparable;
                if((a != null) && (b != null)) {

                    // compare elements
                    delta = a.CompareTo(b);
                    if(delta != 0) {
                        return delta;
                    }
                } else if(a != null) {

                    // (null) preceeds non-null values; our element is null
                    return 1;
                } else if(b != null) {

                    // (null) preceeds non-null values; other element is null
                    return -1;
                }
            }
            return 0;
        }

        /// <summary>
        /// Create a string representation of the Tuple
        /// </summary>
        /// <returns>A string.</returns>
        public override string ToString() {
            StringBuilder result = new StringBuilder();
            bool first = true;
            result.Append("(");
            foreach(object item in this) {
                if(!first) {
                    result.AppendFormat(", {0}", item);
                } else {
                    result.AppendFormat("{0}", item);
                }
                first = false;
            }
            result.Append(")");
            return result.ToString();
        }

        //--- Inteface Methods ---
        IEnumerator IEnumerable.GetEnumerator() {
            return ToArray().GetEnumerator();
        }
    }

    /// <summary>
    /// A Tuple of 4 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    [Obsolete("Use System.Tuple")]
    internal class Tuplet<T1, T2, T3, T4> : ATuplet {

        //--- Fields ---

        /// <summary>
        /// The first item.
        /// </summary>
        public T1 Item1;

        /// <summary>
        /// The second item.
        /// </summary>
        public T2 Item2;

        /// <summary>
        /// The third item.
        /// </summary>
        public T3 Item3;

        /// <summary>
        /// The fourth item.
        /// </summary>
        public T4 Item4;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance.
        /// </summary>
        public Tuplet() { }

        /// <summary>
        /// Create a new instance from a list of items.
        /// </summary>
        /// <param name="items"></param>
        public Tuplet(object[] items) : base(items) { }

        /// <summary>
        /// Create a new instance with initial values for all items.
        /// </summary>
        /// <param name="t1">The first item.</param>
        /// <param name="t2">The second item.</param>
        /// <param name="t3">The third item.</param>
        /// <param name="t4">The fourth item.</param>
        public Tuplet(T1 t1, T2 t2, T3 t3, T4 t4) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
            this.Item4 = t4;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 4; }
        }

        /// <summary>
        /// Accessor to tuple elements by position.
        /// </summary>
        /// <param name="index">Index of item.</param>
        /// <returns>Value of item.</returns>
        public override object this[int index] {
            get {
                switch(index) {
                case 0:
                    return Item1;
                case 1:
                    return Item2;
                case 2:
                    return Item3;
                case 3:
                    return Item4;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
            set {
                switch(index) {
                case 0:
                    Item1 = (T1)value;
                    break;
                case 1:
                    Item2 = (T2)value;
                    break;
                case 2:
                    Item3 = (T3)value;
                    break;
                case 3:
                    Item4 = (T4)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }
}