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
using System.Text;

namespace MindTouch {
    /// <summary>
    /// Interface for a generic ordered list of items.
    /// </summary>
    public interface ITuplet {

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
    public abstract class ATuplet : ITuplet, IEnumerable, IComparable {

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
    /// Empty Tuple
    /// </summary>
    public class Tuplet : ATuplet {

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

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 0; }
        }

        /// <summary>
        /// Accessor to tuple elements by position.
        /// </summary>
        /// <param name="index">Index of item.</param>
        /// <returns>Value of item.</returns>
        public override object this[int index] {
            get {
                throw new IndexOutOfRangeException();
            }
            set {
                throw new IndexOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// A Tuple of 1 item.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    public class Tuplet<T1> : ATuplet {

        //--- Fields ---

        /// <summary>
        /// The first item.
        /// </summary>
        public T1 Item1;

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
        public Tuplet(T1 t1) {
            this.Item1 = t1;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 1; }
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
                default:
                    throw new IndexOutOfRangeException();
                }
            }
            set {
                switch(index) {
                case 0:
                    Item1 = (T1)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 2 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    public class Tuplet<T1, T2> : ATuplet {

        //--- Class Operators ---

        /// <summary>
        /// Implict cast operator to convert this Tuple into a <see cref="KeyValuePair{TKey,TValue}"/>.
        /// </summary>
        /// <param name="tuplet">The Tuple to convert.</param>
        /// <returns>A new instance of <see cref="KeyValuePair{TKey,TValue}"/>.</returns>
        public static implicit operator KeyValuePair<T1, T2>(Tuplet<T1, T2> tuplet) {
            return new KeyValuePair<T1, T2>(tuplet.Item1, tuplet.Item2);
        }

        //--- Fields ---

        /// <summary>
        /// The first item.
        /// </summary>
        public T1 Item1;

        /// <summary>
        /// The second item.
        /// </summary>
        public T2 Item2;

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
        public Tuplet(T1 t1, T2 t2) {
            this.Item1 = t1;
            this.Item2 = t2;
        }

        /// <summary>
        /// Create a new instance from a key/value pair.
        /// </summary>
        /// <param name="pair">Key/Value pair.</param>
        public Tuplet(KeyValuePair<T1, T2> pair) {
            this.Item1 = pair.Key;
            this.Item2 = pair.Value;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 2; }
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
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 3 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    public class Tuplet<T1, T2, T3> : ATuplet {

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
        public Tuplet(T1 t1, T2 t2, T3 t3) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
        }        

        //--- Properties ---
        
        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 3; }
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
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 4 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    public class Tuplet<T1, T2, T3, T4> : ATuplet {

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

    /// <summary>
    /// A Tuple of 5 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    /// <typeparam name="T5">The type of the fifth item.</typeparam>
    public class Tuplet<T1, T2, T3, T4, T5> : ATuplet {

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

        /// <summary>
        /// The fifth item.
        /// </summary>
        public T5 Item5;

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
        /// <param name="t5">The fifth item.</param>
        public Tuplet(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
            this.Item4 = t4;
            this.Item5 = t5;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 5; }
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
                case 4:
                    return Item5;
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
                case 4:
                    Item5 = (T5)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 6 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    /// <typeparam name="T5">The type of the fifth item.</typeparam>
    /// <typeparam name="T6">The type of the sixth item.</typeparam>
    public class Tuplet<T1, T2, T3, T4, T5, T6> : ATuplet {

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

        /// <summary>
        /// The fifth item.
        /// </summary>
        public T5 Item5;

        /// <summary>
        /// The sixth item.
        /// </summary>
        public T6 Item6;

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
        /// <param name="t5">The fifth item.</param>
        /// <param name="t6">The sixth item.</param>
        public Tuplet(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
            this.Item4 = t4;
            this.Item5 = t5;
            this.Item6 = t6;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 6; }
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
                case 4:
                    return Item5;
                case 5:
                    return Item6;
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
                case 4:
                    Item5 = (T5)value;
                    break;
                case 5:
                    Item6 = (T6)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 7 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    /// <typeparam name="T5">The type of the fifth item.</typeparam>
    /// <typeparam name="T6">The type of the sixth item.</typeparam>
    /// <typeparam name="T7">The type of the seventh item.</typeparam>
    public class Tuplet<T1, T2, T3, T4, T5, T6, T7> : ATuplet {

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

        /// <summary>
        /// The fifth item.
        /// </summary>
        public T5 Item5;

        /// <summary>
        /// The sixth item.
        /// </summary>
        public T6 Item6;

        /// <summary>
        /// The seventh item.
        /// </summary>
        public T7 Item7;

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
        /// <param name="t5">The fifth item.</param>
        /// <param name="t6">The sixth item.</param>
        /// <param name="t7">The seventh item.</param>
        public Tuplet(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
            this.Item4 = t4;
            this.Item5 = t5;
            this.Item6 = t6;
            this.Item7 = t7;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 7; }
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
                case 4:
                    return Item5;
                case 5:
                    return Item6;
                case 6:
                    return Item7;
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
                case 4:
                    Item5 = (T5)value;
                    break;
                case 5:
                    Item6 = (T6)value;
                    break;
                case 6:
                    Item7 = (T7)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 8 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    /// <typeparam name="T5">The type of the fifth item.</typeparam>
    /// <typeparam name="T6">The type of the sixth item.</typeparam>
    /// <typeparam name="T7">The type of the seventh item.</typeparam>
    /// <typeparam name="T8">The type of the eighth item.</typeparam>
    public class Tuplet<T1, T2, T3, T4, T5, T6, T7, T8> : ATuplet {

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

        /// <summary>
        /// The fifth item.
        /// </summary>
        public T5 Item5;

        /// <summary>
        /// The sixth item.
        /// </summary>
        public T6 Item6;

        /// <summary>
        /// The seventh item.
        /// </summary>
        public T7 Item7;

        /// <summary>
        /// The eighth item.
        /// </summary>
        public T8 Item8;

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
        /// <param name="t5">The fifth item.</param>
        /// <param name="t6">The sixth item.</param>
        /// <param name="t7">The seventh item.</param>
        /// <param name="t8">The eighth item.</param>
        public Tuplet(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
            this.Item4 = t4;
            this.Item5 = t5;
            this.Item6 = t6;
            this.Item7 = t7;
            this.Item8 = t8;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 8; }
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
                case 4:
                    return Item5;
                case 5:
                    return Item6;
                case 6:
                    return Item7;
                case 7:
                    return Item8;
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
                case 4:
                    Item5 = (T5)value;
                    break;
                case 5:
                    Item6 = (T6)value;
                    break;
                case 6:
                    Item7 = (T7)value;
                    break;
                case 7:
                    Item8 = (T8)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 9 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    /// <typeparam name="T5">The type of the fifth item.</typeparam>
    /// <typeparam name="T6">The type of the sixth item.</typeparam>
    /// <typeparam name="T7">The type of the seventh item.</typeparam>
    /// <typeparam name="T8">The type of the eighth item.</typeparam>
    /// <typeparam name="T9">The type of the nineth item.</typeparam>
    public class Tuplet<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ATuplet {

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

        /// <summary>
        /// The fifth item.
        /// </summary>
        public T5 Item5;

        /// <summary>
        /// The sixth item.
        /// </summary>
        public T6 Item6;

        /// <summary>
        /// The seventh item.
        /// </summary>
        public T7 Item7;

        /// <summary>
        /// The eighth item.
        /// </summary>
        public T8 Item8;

        /// <summary>
        /// The nineth item.
        /// </summary>
        public T9 Item9;

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
        /// <param name="t5">The fifth item.</param>
        /// <param name="t6">The sixth item.</param>
        /// <param name="t7">The seventh item.</param>
        /// <param name="t8">The eighth item.</param>
        /// <param name="t9">The nineth item.</param>
        public Tuplet(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
            this.Item4 = t4;
            this.Item5 = t5;
            this.Item6 = t6;
            this.Item7 = t7;
            this.Item8 = t8;
            this.Item9 = t9;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 9; }
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
                case 4:
                    return Item5;
                case 5:
                    return Item6;
                case 6:
                    return Item7;
                case 7:
                    return Item8;
                case 8:
                    return Item9;
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
                case 4:
                    Item5 = (T5)value;
                    break;
                case 5:
                    Item6 = (T6)value;
                    break;
                case 6:
                    Item7 = (T7)value;
                    break;
                case 7:
                    Item8 = (T8)value;
                    break;
                case 8:
                    Item9 = (T9)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 10 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    /// <typeparam name="T5">The type of the fifth item.</typeparam>
    /// <typeparam name="T6">The type of the sixth item.</typeparam>
    /// <typeparam name="T7">The type of the seventh item.</typeparam>
    /// <typeparam name="T8">The type of the eighth item.</typeparam>
    /// <typeparam name="T9">The type of the nineth item.</typeparam>
    /// <typeparam name="T10">The type of the tenth item.</typeparam>
    public class Tuplet<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ATuplet {

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

        /// <summary>
        /// The fifth item.
        /// </summary>
        public T5 Item5;

        /// <summary>
        /// The sixth item.
        /// </summary>
        public T6 Item6;

        /// <summary>
        /// The seventh item.
        /// </summary>
        public T7 Item7;

        /// <summary>
        /// The eighth item.
        /// </summary>
        public T8 Item8;

        /// <summary>
        /// The nineth item.
        /// </summary>
        public T9 Item9;

        /// <summary>
        /// The tenth item.
        /// </summary>
        public T10 Item10;

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
        /// <param name="t5">The fifth item.</param>
        /// <param name="t6">The sixth item.</param>
        /// <param name="t7">The seventh item.</param>
        /// <param name="t8">The eighth item.</param>
        /// <param name="t9">The nineth item.</param>
        /// <param name="t10">The tenth item.</param>
        public Tuplet(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
            this.Item4 = t4;
            this.Item5 = t5;
            this.Item6 = t6;
            this.Item7 = t7;
            this.Item8 = t8;
            this.Item9 = t9;
            this.Item10 = t10;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 10; }
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
                case 4:
                    return Item5;
                case 5:
                    return Item6;
                case 6:
                    return Item7;
                case 7:
                    return Item8;
                case 8:
                    return Item9;
                case 9:
                    return Item10;
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
                case 4:
                    Item5 = (T5)value;
                    break;
                case 5:
                    Item6 = (T6)value;
                    break;
                case 6:
                    Item7 = (T7)value;
                    break;
                case 7:
                    Item8 = (T8)value;
                    break;
                case 8:
                    Item9 = (T9)value;
                    break;
                case 9:
                    Item10 = (T10)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 11 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    /// <typeparam name="T5">The type of the fifth item.</typeparam>
    /// <typeparam name="T6">The type of the sixth item.</typeparam>
    /// <typeparam name="T7">The type of the seventh item.</typeparam>
    /// <typeparam name="T8">The type of the eighth item.</typeparam>
    /// <typeparam name="T9">The type of the nineth item.</typeparam>
    /// <typeparam name="T10">The type of the tenth item.</typeparam>
    /// <typeparam name="T11">The type of the eleventh item.</typeparam>
    public class Tuplet<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : ATuplet {

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

        /// <summary>
        /// The fifth item.
        /// </summary>
        public T5 Item5;

        /// <summary>
        /// The sixth item.
        /// </summary>
        public T6 Item6;

        /// <summary>
        /// The seventh item.
        /// </summary>
        public T7 Item7;

        /// <summary>
        /// The eighth item.
        /// </summary>
        public T8 Item8;

        /// <summary>
        /// The nineth item.
        /// </summary>
        public T9 Item9;

        /// <summary>
        /// The tenth item.
        /// </summary>
        public T10 Item10;

        /// <summary>
        /// The eleventh item.
        /// </summary>
        public T11 Item11;

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
        /// <param name="t5">The fifth item.</param>
        /// <param name="t6">The sixth item.</param>
        /// <param name="t7">The seventh item.</param>
        /// <param name="t8">The eighth item.</param>
        /// <param name="t9">The nineth item.</param>
        /// <param name="t10">The tenth item.</param>
        /// <param name="t11">The eleventh item.</param>
        public Tuplet(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
            this.Item4 = t4;
            this.Item5 = t5;
            this.Item6 = t6;
            this.Item7 = t7;
            this.Item8 = t8;
            this.Item9 = t9;
            this.Item10 = t10;
            this.Item11 = t11;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 11; }
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
                case 4:
                    return Item5;
                case 5:
                    return Item6;
                case 6:
                    return Item7;
                case 7:
                    return Item8;
                case 8:
                    return Item9;
                case 9:
                    return Item10;
                case 10:
                    return Item11;
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
                case 4:
                    Item5 = (T5)value;
                    break;
                case 5:
                    Item6 = (T6)value;
                    break;
                case 6:
                    Item7 = (T7)value;
                    break;
                case 7:
                    Item8 = (T8)value;
                    break;
                case 8:
                    Item9 = (T9)value;
                    break;
                case 9:
                    Item10 = (T10)value;
                    break;
                case 10:
                    Item11 = (T11)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 12 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    /// <typeparam name="T5">The type of the fifth item.</typeparam>
    /// <typeparam name="T6">The type of the sixth item.</typeparam>
    /// <typeparam name="T7">The type of the seventh item.</typeparam>
    /// <typeparam name="T8">The type of the eighth item.</typeparam>
    /// <typeparam name="T9">The type of the nineth item.</typeparam>
    /// <typeparam name="T10">The type of the tenth item.</typeparam>
    /// <typeparam name="T11">The type of the eleventh item.</typeparam>
    /// <typeparam name="T12">The type of the twelveth item.</typeparam>
    public class Tuplet<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : ATuplet {

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

        /// <summary>
        /// The fifth item.
        /// </summary>
        public T5 Item5;

        /// <summary>
        /// The sixth item.
        /// </summary>
        public T6 Item6;

        /// <summary>
        /// The seventh item.
        /// </summary>
        public T7 Item7;

        /// <summary>
        /// The eighth item.
        /// </summary>
        public T8 Item8;

        /// <summary>
        /// The nineth item.
        /// </summary>
        public T9 Item9;

        /// <summary>
        /// The tenth item.
        /// </summary>
        public T10 Item10;

        /// <summary>
        /// The eleventh item.
        /// </summary>
        public T11 Item11;

        /// <summary>
        /// The twelveth item.
        /// </summary>
        public T12 Item12;

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
        /// <param name="t5">The fifth item.</param>
        /// <param name="t6">The sixth item.</param>
        /// <param name="t7">The seventh item.</param>
        /// <param name="t8">The eighth item.</param>
        /// <param name="t9">The nineth item.</param>
        /// <param name="t10">The tenth item.</param>
        /// <param name="t11">The eleventh item.</param>
        /// <param name="t12">The twelveth item.</param>
        public Tuplet(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
            this.Item4 = t4;
            this.Item5 = t5;
            this.Item6 = t6;
            this.Item7 = t7;
            this.Item8 = t8;
            this.Item9 = t9;
            this.Item10 = t10;
            this.Item11 = t11;
            this.Item12 = t12;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 12; }
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
                case 4:
                    return Item5;
                case 5:
                    return Item6;
                case 6:
                    return Item7;
                case 7:
                    return Item8;
                case 8:
                    return Item9;
                case 9:
                    return Item10;
                case 10:
                    return Item11;
                case 11:
                    return Item12;
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
                case 4:
                    Item5 = (T5)value;
                    break;
                case 5:
                    Item6 = (T6)value;
                    break;
                case 6:
                    Item7 = (T7)value;
                    break;
                case 7:
                    Item8 = (T8)value;
                    break;
                case 8:
                    Item9 = (T9)value;
                    break;
                case 9:
                    Item10 = (T10)value;
                    break;
                case 10:
                    Item11 = (T11)value;
                    break;
                case 11:
                    Item12 = (T12)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 14 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    /// <typeparam name="T5">The type of the fifth item.</typeparam>
    /// <typeparam name="T6">The type of the sixth item.</typeparam>
    /// <typeparam name="T7">The type of the seventh item.</typeparam>
    /// <typeparam name="T8">The type of the eighth item.</typeparam>
    /// <typeparam name="T9">The type of the nineth item.</typeparam>
    /// <typeparam name="T10">The type of the tenth item.</typeparam>
    /// <typeparam name="T11">The type of the eleventh item.</typeparam>
    /// <typeparam name="T12">The type of the twelveth item.</typeparam>
    /// <typeparam name="T13">The type of the thirteenth item.</typeparam>
    public class Tuplet<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : ATuplet {

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

        /// <summary>
        /// The fifth item.
        /// </summary>
        public T5 Item5;

        /// <summary>
        /// The sixth item.
        /// </summary>
        public T6 Item6;

        /// <summary>
        /// The seventh item.
        /// </summary>
        public T7 Item7;

        /// <summary>
        /// The eighth item.
        /// </summary>
        public T8 Item8;

        /// <summary>
        /// The nineth item.
        /// </summary>
        public T9 Item9;

        /// <summary>
        /// The tenth item.
        /// </summary>
        public T10 Item10;

        /// <summary>
        /// The eleventh item.
        /// </summary>
        public T11 Item11;

        /// <summary>
        /// The twelveth item.
        /// </summary>
        public T12 Item12;

        /// <summary>
        /// The thirteenth item.
        /// </summary>
        public T13 Item13;

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
        /// <param name="t5">The fifth item.</param>
        /// <param name="t6">The sixth item.</param>
        /// <param name="t7">The seventh item.</param>
        /// <param name="t8">The eighth item.</param>
        /// <param name="t9">The nineth item.</param>
        /// <param name="t10">The tenth item.</param>
        /// <param name="t11">The eleventh item.</param>
        /// <param name="t12">The twelveth item.</param>
        /// <param name="t13">The thirteenth item.</param>
        public Tuplet(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
            this.Item4 = t4;
            this.Item5 = t5;
            this.Item6 = t6;
            this.Item7 = t7;
            this.Item8 = t8;
            this.Item9 = t9;
            this.Item10 = t10;
            this.Item11 = t11;
            this.Item12 = t12;
            this.Item13 = t13;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 13; }
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
                case 4:
                    return Item5;
                case 5:
                    return Item6;
                case 6:
                    return Item7;
                case 7:
                    return Item8;
                case 8:
                    return Item9;
                case 9:
                    return Item10;
                case 10:
                    return Item11;
                case 11:
                    return Item12;
                case 12:
                    return Item13;
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
                case 4:
                    Item5 = (T5)value;
                    break;
                case 5:
                    Item6 = (T6)value;
                    break;
                case 6:
                    Item7 = (T7)value;
                    break;
                case 7:
                    Item8 = (T8)value;
                    break;
                case 8:
                    Item9 = (T9)value;
                    break;
                case 9:
                    Item10 = (T10)value;
                    break;
                case 10:
                    Item11 = (T11)value;
                    break;
                case 11:
                    Item12 = (T12)value;
                    break;
                case 12:
                    Item13 = (T13)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 14 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    /// <typeparam name="T5">The type of the fifth item.</typeparam>
    /// <typeparam name="T6">The type of the sixth item.</typeparam>
    /// <typeparam name="T7">The type of the seventh item.</typeparam>
    /// <typeparam name="T8">The type of the eighth item.</typeparam>
    /// <typeparam name="T9">The type of the nineth item.</typeparam>
    /// <typeparam name="T10">The type of the tenth item.</typeparam>
    /// <typeparam name="T11">The type of the eleventh item.</typeparam>
    /// <typeparam name="T12">The type of the twelveth item.</typeparam>
    /// <typeparam name="T13">The type of the thirteenth item.</typeparam>
    /// <typeparam name="T14">The type of the fourteenth item.</typeparam>
    public class Tuplet<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : ATuplet {

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

        /// <summary>
        /// The fifth item.
        /// </summary>
        public T5 Item5;

        /// <summary>
        /// The sixth item.
        /// </summary>
        public T6 Item6;

        /// <summary>
        /// The seventh item.
        /// </summary>
        public T7 Item7;

        /// <summary>
        /// The eighth item.
        /// </summary>
        public T8 Item8;

        /// <summary>
        /// The nineth item.
        /// </summary>
        public T9 Item9;

        /// <summary>
        /// The tenth item.
        /// </summary>
        public T10 Item10;

        /// <summary>
        /// The eleventh item.
        /// </summary>
        public T11 Item11;

        /// <summary>
        /// The twelveth item.
        /// </summary>
        public T12 Item12;

        /// <summary>
        /// The thirteenth item.
        /// </summary>
        public T13 Item13;

        /// <summary>
        /// The fourteenth item.
        /// </summary>
        public T14 Item14;

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
        /// <param name="t5">The fifth item.</param>
        /// <param name="t6">The sixth item.</param>
        /// <param name="t7">The seventh item.</param>
        /// <param name="t8">The eighth item.</param>
        /// <param name="t9">The nineth item.</param>
        /// <param name="t10">The tenth item.</param>
        /// <param name="t11">The eleventh item.</param>
        /// <param name="t12">The twelveth item.</param>
        /// <param name="t13">The thirteenth item.</param>
        /// <param name="t14">The fourteenth item.</param>
        public Tuplet(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
            this.Item4 = t4;
            this.Item5 = t5;
            this.Item6 = t6;
            this.Item7 = t7;
            this.Item8 = t8;
            this.Item9 = t9;
            this.Item10 = t10;
            this.Item11 = t11;
            this.Item12 = t12;
            this.Item13 = t13;
            this.Item14 = t14;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 14; }
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
                case 4:
                    return Item5;
                case 5:
                    return Item6;
                case 6:
                    return Item7;
                case 7:
                    return Item8;
                case 8:
                    return Item9;
                case 9:
                    return Item10;
                case 10:
                    return Item11;
                case 11:
                    return Item12;
                case 12:
                    return Item13;
                case 13:
                    return Item14;
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
                case 4:
                    Item5 = (T5)value;
                    break;
                case 5:
                    Item6 = (T6)value;
                    break;
                case 6:
                    Item7 = (T7)value;
                    break;
                case 7:
                    Item8 = (T8)value;
                    break;
                case 8:
                    Item9 = (T9)value;
                    break;
                case 9:
                    Item10 = (T10)value;
                    break;
                case 10:
                    Item11 = (T11)value;
                    break;
                case 11:
                    Item12 = (T12)value;
                    break;
                case 12:
                    Item13 = (T13)value;
                    break;
                case 13:
                    Item14 = (T14)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 15 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    /// <typeparam name="T5">The type of the fifth item.</typeparam>
    /// <typeparam name="T6">The type of the sixth item.</typeparam>
    /// <typeparam name="T7">The type of the seventh item.</typeparam>
    /// <typeparam name="T8">The type of the eighth item.</typeparam>
    /// <typeparam name="T9">The type of the nineth item.</typeparam>
    /// <typeparam name="T10">The type of the tenth item.</typeparam>
    /// <typeparam name="T11">The type of the eleventh item.</typeparam>
    /// <typeparam name="T12">The type of the twelveth item.</typeparam>
    /// <typeparam name="T13">The type of the thirteenth item.</typeparam>
    /// <typeparam name="T14">The type of the fourteenth item.</typeparam>
    /// <typeparam name="T15">The type of the fifteenth item.</typeparam>
    public class Tuplet<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : ATuplet {

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

        /// <summary>
        /// The fifth item.
        /// </summary>
        public T5 Item5;

        /// <summary>
        /// The sixth item.
        /// </summary>
        public T6 Item6;

        /// <summary>
        /// The seventh item.
        /// </summary>
        public T7 Item7;

        /// <summary>
        /// The eighth item.
        /// </summary>
        public T8 Item8;

        /// <summary>
        /// The nineth item.
        /// </summary>
        public T9 Item9;

        /// <summary>
        /// The tenth item.
        /// </summary>
        public T10 Item10;

        /// <summary>
        /// The eleventh item.
        /// </summary>
        public T11 Item11;

        /// <summary>
        /// The twelveth item.
        /// </summary>
        public T12 Item12;

        /// <summary>
        /// The thirteenth item.
        /// </summary>
        public T13 Item13;

        /// <summary>
        /// The fourteenth item.
        /// </summary>
        public T14 Item14;

        /// <summary>
        /// The fifteenth item.
        /// </summary>
        public T15 Item15;

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
        /// <param name="t5">The fifth item.</param>
        /// <param name="t6">The sixth item.</param>
        /// <param name="t7">The seventh item.</param>
        /// <param name="t8">The eighth item.</param>
        /// <param name="t9">The nineth item.</param>
        /// <param name="t10">The tenth item.</param>
        /// <param name="t11">The eleventh item.</param>
        /// <param name="t12">The twelveth item.</param>
        /// <param name="t13">The thirteenth item.</param>
        /// <param name="t14">The fourteenth item.</param>
        /// <param name="t15">The fifteenth item.</param>
        public Tuplet(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
            this.Item4 = t4;
            this.Item5 = t5;
            this.Item6 = t6;
            this.Item7 = t7;
            this.Item8 = t8;
            this.Item9 = t9;
            this.Item10 = t10;
            this.Item11 = t11;
            this.Item12 = t12;
            this.Item13 = t13;
            this.Item14 = t14;
            this.Item15 = t15;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 15; }
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
                case 4:
                    return Item5;
                case 5:
                    return Item6;
                case 6:
                    return Item7;
                case 7:
                    return Item8;
                case 8:
                    return Item9;
                case 9:
                    return Item10;
                case 10:
                    return Item11;
                case 11:
                    return Item12;
                case 12:
                    return Item13;
                case 13:
                    return Item14;
                case 14:
                    return Item15;
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
                case 4:
                    Item5 = (T5)value;
                    break;
                case 5:
                    Item6 = (T6)value;
                    break;
                case 6:
                    Item7 = (T7)value;
                    break;
                case 7:
                    Item8 = (T8)value;
                    break;
                case 8:
                    Item9 = (T9)value;
                    break;
                case 9:
                    Item10 = (T10)value;
                    break;
                case 10:
                    Item11 = (T11)value;
                    break;
                case 11:
                    Item12 = (T12)value;
                    break;
                case 12:
                    Item13 = (T13)value;
                    break;
                case 13:
                    Item14 = (T14)value;
                    break;
                case 14:
                    Item15 = (T15)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// A Tuple of 16 items.
    /// </summary>
    /// <typeparam name="T1">The type of the first item.</typeparam>
    /// <typeparam name="T2">The type of the second item.</typeparam>
    /// <typeparam name="T3">The type of the third item.</typeparam>
    /// <typeparam name="T4">The type of the fourth item.</typeparam>
    /// <typeparam name="T5">The type of the fifth item.</typeparam>
    /// <typeparam name="T6">The type of the sixth item.</typeparam>
    /// <typeparam name="T7">The type of the seventh item.</typeparam>
    /// <typeparam name="T8">The type of the eighth item.</typeparam>
    /// <typeparam name="T9">The type of the nineth item.</typeparam>
    /// <typeparam name="T10">The type of the tenth item.</typeparam>
    /// <typeparam name="T11">The type of the eleventh item.</typeparam>
    /// <typeparam name="T12">The type of the twelveth item.</typeparam>
    /// <typeparam name="T13">The type of the thirteenth item.</typeparam>
    /// <typeparam name="T14">The type of the fourteenth item.</typeparam>
    /// <typeparam name="T15">The type of the fifteenth item.</typeparam>
    /// <typeparam name="T16">The type of the sixteenth item.</typeparam>
    public class Tuplet<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : ATuplet {

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

        /// <summary>
        /// The fifth item.
        /// </summary>
        public T5 Item5;

        /// <summary>
        /// The sixth item.
        /// </summary>
        public T6 Item6;

        /// <summary>
        /// The seventh item.
        /// </summary>
        public T7 Item7;

        /// <summary>
        /// The eighth item.
        /// </summary>
        public T8 Item8;

        /// <summary>
        /// The nineth item.
        /// </summary>
        public T9 Item9;

        /// <summary>
        /// The tenth item.
        /// </summary>
        public T10 Item10;

        /// <summary>
        /// The eleventh item.
        /// </summary>
        public T11 Item11;

        /// <summary>
        /// The twelveth item.
        /// </summary>
        public T12 Item12;

        /// <summary>
        /// The thirteenth item.
        /// </summary>
        public T13 Item13;

        /// <summary>
        /// The fourteenth item.
        /// </summary>
        public T14 Item14;

        /// <summary>
        /// The fifteenth item.
        /// </summary>
        public T15 Item15;

        /// <summary>
        /// The sixteenth item.
        /// </summary>
        public T16 Item16;

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
        /// <param name="t5">The fifth item.</param>
        /// <param name="t6">The sixth item.</param>
        /// <param name="t7">The seventh item.</param>
        /// <param name="t8">The eighth item.</param>
        /// <param name="t9">The nineth item.</param>
        /// <param name="t10">The tenth item.</param>
        /// <param name="t11">The eleventh item.</param>
        /// <param name="t12">The twelveth item.</param>
        /// <param name="t13">The thirteenth item.</param>
        /// <param name="t14">The fourteenth item.</param>
        /// <param name="t15">The fifteenth item.</param>
        /// <param name="t16">The sixteenth item.</param>
        public Tuplet(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16) {
            this.Item1 = t1;
            this.Item2 = t2;
            this.Item3 = t3;
            this.Item4 = t4;
            this.Item5 = t5;
            this.Item6 = t6;
            this.Item7 = t7;
            this.Item8 = t8;
            this.Item9 = t9;
            this.Item10 = t10;
            this.Item11 = t11;
            this.Item12 = t12;
            this.Item13 = t13;
            this.Item14 = t14;
            this.Item15 = t15;
            this.Item16 = t16;
        }

        //--- Properties ---

        /// <summary>
        /// Number of elements in the tuple
        /// </summary>
        public override int Count {
            get { return 16; }
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
                case 4:
                    return Item5;
                case 5:
                    return Item6;
                case 6:
                    return Item7;
                case 7:
                    return Item8;
                case 8:
                    return Item9;
                case 9:
                    return Item10;
                case 10:
                    return Item11;
                case 11:
                    return Item12;
                case 12:
                    return Item13;
                case 13:
                    return Item14;
                case 14:
                    return Item15;
                case 15:
                    return Item16;
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
                case 4:
                    Item5 = (T5)value;
                    break;
                case 5:
                    Item6 = (T6)value;
                    break;
                case 6:
                    Item7 = (T7)value;
                    break;
                case 7:
                    Item8 = (T8)value;
                    break;
                case 8:
                    Item9 = (T9)value;
                    break;
                case 9:
                    Item10 = (T10)value;
                    break;
                case 10:
                    Item11 = (T11)value;
                    break;
                case 11:
                    Item12 = (T12)value;
                    break;
                case 12:
                    Item13 = (T13)value;
                    break;
                case 13:
                    Item14 = (T14)value;
                    break;
                case 14:
                    Item15 = (T15)value;
                    break;
                case 15:
                    Item16 = (T16)value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }
}