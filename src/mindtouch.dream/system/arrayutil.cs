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

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using MindTouch;
using MindTouch.Collections;

namespace System {

    /// <summary>
    /// Type of difference result comparing arrays
    /// </summary>
    public enum ArrayDiffKind {

        /// <summary>
        /// Items in arrays that are the same.
        /// </summary>
        Same,

        /// <summary>
        /// Items removed from the first array.
        /// </summary>
        Removed,

        /// <summary>
        /// Items added to the first array.
        /// </summary>
        Added,

        // ambiguous entries

        /// <summary>
        /// Left Item was removed.
        /// </summary>
        /// <remarks>
        /// This is an ambigious result that can be avoided by usint <see cref="ArrayMergeDiffPriority"/>
        /// </remarks>
        RemovedLeft,

        /// <summary>
        /// Right Item was removed.
        /// </summary>
        /// <remarks>
        /// This is an ambigious result that can be avoided by usint <see cref="ArrayMergeDiffPriority"/>
        /// </remarks>
        RemovedRight,

        /// <summary>
        /// Left Item was added.
        /// </summary>
        /// <remarks>
        /// This is an ambigious result that can be avoided by usint <see cref="ArrayMergeDiffPriority"/>
        /// </remarks>
        AddedLeft,

        /// <summary>
        /// Right Item was added.
        /// </summary>
        /// <remarks>
        /// This is an ambigious result that can be avoided by usint <see cref="ArrayMergeDiffPriority"/>
        /// </remarks>
        AddedRight
    }

    /// <summary>
    /// Merge priority for diff operations.
    /// </summary>
    /// <remarks>
    /// This removes ambigious results
    /// </remarks>
    public enum ArrayMergeDiffPriority {

        /// <summary>
        /// No merge priority.
        /// </summary>
        None,

        /// <summary>
        /// Prefer left most items in diff result merge.
        /// </summary>
        Left,

        /// <summary>
        /// Prefer right most items in diff result merge.
        /// </summary>
        Right
    }

    /// <summary>
    /// A static utility class containing helper and extension methods for working with <see cref="Array"/> instances.
    /// </summary>
    public static class ArrayUtil {

        //--- Extension Methods ---

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey,TValue}"/> from an <see cref="IEnumerable{TValue}"/> according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements of source.</typeparam>
        /// <typeparam name="TValue">The type of the key returned by keySelector.</typeparam>
        /// <param name="collection">An <see cref="IEnumerable{TValue}"/> to create a <see cref="Dictionary{TKey,TValue}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="overwriteDuplicates">
        /// If <see langword="True"/>, duplicate key values are overwritten as encountered, otherwise this method will throw 
        /// <see cref="ArgumentException"/> on key collision.
        /// </param>
        /// <returns>A <see cref="Dictionary{TKey,TValue}"/> that contains keys and values.</returns>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<TValue> collection, Func<TValue, TKey> keySelector, bool overwriteDuplicates) {
            if(overwriteDuplicates) {
                var result = new Dictionary<TKey, TValue>();
                if(collection != null) {
                    foreach(var entry in collection) {
                        result[keySelector(entry)] = entry;
                    }
                }
                return result;
            }
            return collection.ToDictionary(keySelector);
        }

        /// <summary>
        /// Get a value from a dictionary or a default value if not found
        /// </summary>
        /// <typeparam name="TKey">Dictionary key type</typeparam>
        /// <typeparam name="TValue">Dictionary value type</typeparam>
        /// <param name="dictionary">The dictionary to operate on</param>
        /// <param name="key">Key to try to retrieve a value for</param>
        /// <param name="default">Default value to return should the key not exist</param>
        /// <returns>Either the value for the given key, or the default</returns>
        public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue @default) {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : @default;
        }

        /// <summary>
        /// Get a sub array.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Array"/> items.</typeparam>
        /// <param name="array">The source array.</param>
        /// <param name="begin">The index at which to start the subarray at.</param>
        /// <returns>A new array containing a subset of the original array.</returns>
        public static T[] SubArray<T>(this T[] array, int begin) {
            return SubArray(array, begin, array.Length);
        }

        /// <summary>
        /// Get a sub array.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Array"/> items.</typeparam>
        /// <param name="array">The source array.</param>
        /// <param name="begin">The index at which to start the subarray at.</param>
        /// <param name="end">The index of the last item to include in the subarray.</param>
        /// <returns>A new array containing a subset of the original array.</returns>
        public static T[] SubArray<T>(this T[] array, int begin, int end) {
            begin = NormalizeIndex(array, begin);
            end = NormalizeIndex(array, end);
            int count = Math.Max(0, end - begin);
            T[] result = new T[count];
            Array.Copy(array, begin, result, 0, count);
            return result;
        }

        /// <summary>
        /// Retrieve an element from an array using safe index value lookup.
        /// </summary>
        /// <remarks>
        /// The index is normalized to allow negative and out of range values.
        /// Negative values count from the end. Index values that are too large return the last value in the array.
        /// </remarks>
        /// <typeparam name="T">Type of <see cref="Array"/> items.</typeparam>
        /// <param name="array">Source array.</param>
        /// <param name="index">Index value.</param>
        /// <returns>Value at array index.</returns>
        public static T At<T>(this T[] array, int index) {
            return array[NormalizeIndex(array, index)];
        }

        /// <summary>
        /// Set the value of an element using safe index value lookup.
        /// </summary>
        /// <remarks>
        /// The index is normalized to allow negative and out of range values.
        /// Negative values count from the end. Index values that are too large return the last value in the array.
        /// </remarks>
        /// <typeparam name="T">Type of <see cref="Array"/> items.</typeparam>
        /// <param name="array">Source array.</param>
        /// <param name="index">Index value.</param>
        /// <param name="value">Value to set.</param>
        public static void AtAssign<T>(this T[] array, int index, T value) {
            array[NormalizeIndex(array, index)] = value;
        }

        /// <summary>
        /// Convert an enumerable of key/value pairs into a <see cref="NameValueCollection"/>.
        /// </summary>
        /// <param name="enumerable">Enumerable of key/value pairs.</param>
        /// <returns>Collection of name/value pairs.</returns>
        public static NameValueCollection AsNameValueCollection(this IEnumerable<KeyValuePair<string, string>> enumerable) {
            return new NameValueCollection().AddToNameValueCollection(enumerable);
        }

        /// <summary>
        /// Add an enumerable of key value pairs to a NameValueCollection.
        /// </summary>
        /// <param name="collection">NameValue collection to add pairs to.</param>
        /// <param name="enumerable">An enumerable of key/value pairs</param>
        /// <returns>The same collection provided as input to allow call chaining.</returns>
        public static NameValueCollection AddToNameValueCollection(this NameValueCollection collection, IEnumerable<KeyValuePair<string, string>> enumerable) {
            if(collection == null) {
                throw new ArgumentNullException("collection");
            }
            foreach(KeyValuePair<string, string> pair in enumerable) {
                collection.Add(pair.Key, pair.Value);
            }
            return collection;
        }

        /// <summary>
        /// Get the first value or null for a certain key from a <see cref="NameValueCollection"/>. 
        /// </summary>
        /// <param name="collection">Collection to retrieve value from.</param>
        /// <param name="key">Key to retrieve value for.</param>
        /// <returns>First value for given key or null, if no values are defined for the key.</returns>
        public static string Get(this NameValueCollection collection, string key) {
            string[] values = collection.GetValues(key);
            if((values == null) || (values.Length == 0)) {
                return null;
            }
            return values[0];
        }

        /// <summary>
        /// Append an enumerable of values to an existing list.
        /// </summary>
        /// <typeparam name="TInput">Type of values in list and enumerable.</typeparam>
        /// <param name="list">List to add values to.</param>
        /// <param name="itemsToAdd">Enumerable of values to add.</param>
        public static void AddRange<TInput>(this IList<TInput> list, IEnumerable<TInput> itemsToAdd) {
            if(list == null) {
                throw new ArgumentNullException("list");
            }
            if(itemsToAdd != null) {
                foreach(TInput itemToAdd in itemsToAdd) {
                    list.Add(itemToAdd);
                }
            }
        }

        //--- Class Methods ---

        /// <summary>
        /// Concatenate all items in one or more multi-dimensional array into a single array.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Array"/> items.</typeparam>
        /// <param name="arrays">One or more multi-dimensional arrays to concatenate.</param>
        /// <returns>New array containing all items from provided arrays.</returns>
        public static T[] Concat<T>(params T[][] arrays) {
            int count = 0;
            for(int i = 0; i < arrays.Length; ++i) {
                count += arrays[i].Length;
            }
            Array result = Array.CreateInstance(typeof(T), count);
            int offset = 0;
            for(int i = 0; i < arrays.Length; ++i) {
                Array.Copy(arrays[i], 0, result, offset, arrays[i].Length);
                offset += arrays[i].Length;
            }
            return (T[])result;
        }

        /// <summary>
        /// Compare two Arrays.
        /// </summary>
        /// <remarks>
        /// Compare first checks array lengths and then each item, returning either the size difference of the arrays or the 
        /// <see cref="IComparable{T}.CompareTo"/> value of the first non-matching item.
        /// </remarks>
        /// <typeparam name="T">Type of <see cref="Array"/> items. Must be <see cref="IComparable{T}"/>.</typeparam>
        /// <param name="left">Array to compare.</param>
        /// <param name="right">Array to compare.</param>
        /// <returns>Either the size difference between the two arrays or the <see cref="IComparable{T}.CompareTo"/> value of the first non-matching item.</returns>
        public static int Compare<T>(T[] left, T[] right) where T : IComparable<T> {
            if(left == null) {
                throw new ArgumentNullException("left");
            }
            if(right == null) {
                throw new ArgumentNullException("right");
            }
            int result = left.Length - right.Length;
            if(result != 0) {
                return result;
            }
            for(int i = 0; i < left.Length; ++i) {
                result = ((IComparable<T>)left[i]).CompareTo(right[i]);
                if(result != 0) {
                    return result;
                }
            }
            return 0;
        }

        /// <summary>
        /// Compute the intersection two arrays.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Array"/> items. Must be <see cref="IComparable{T}"/>.</typeparam>
        /// <param name="left">Left Hand Array to intersect.</param>
        /// <param name="right">Right Hand Array to intersect.</param>
        /// <returns>Array containing only the items that occur in both source arrays.</returns>
        public static T[] Intersect<T>(T[] left, T[] right) where T : IComparable<T> {
            return Intersect(left, right, (l, r) => l.CompareTo(r));
        }

        /// <summary>
        /// Compute the intersection two arrays.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Array"/> items. Must be <see cref="IComparable{T}"/>.</typeparam>
        /// <param name="left">Left Hand Array to intersect.</param>
        /// <param name="right">Right Hand Array to intersect.</param>
        /// <param name="comparison">Comparison delegate.</param>
        /// <returns>Array containing only the items that occur in both source arrays.</returns>
        public static T[] Intersect<T>(T[] left, T[] right, Comparison<T> comparison) {
            if(left == null || right == null) {
                return new T[0];
            }
            List<T> intersection = new List<T>();
            for(int i = 0; i < left.Length; i++) {
                for(int j = 0; j < right.Length; j++) {
                    if(comparison.Invoke(left[i], right[j]) == 0) {
                        intersection.Add(right[j]);
                    }
                }
            }
            return intersection.ToArray();
        }

        /// <summary>
        /// Compute the union of two arrays.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Array"/> items. Must be <see cref="IComparable{T}"/>.</typeparam>
        /// <param name="left">Source Array.</param>
        /// <param name="right">Source Array.</param>
        /// <returns>Array containing all unique items from the source arrays.</returns>
        public static T[] Union<T>(T[] left, T[] right) where T : IComparable<T> {
            return Union(left, right, delegate(T l, T r) { return l.CompareTo(r); });
        }

        /// <summary>
        /// Compute the union of two arrays.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Array"/> items. Must be <see cref="IComparable{T}"/>.</typeparam>
        /// <param name="left">Source Array.</param>
        /// <param name="right">Source Array.</param>
        /// <param name="comparison">Comparison delegate.</param>
        /// <returns>Array containing all unique items from the source arrays.</returns>
        public static T[] Union<T>(T[] left, T[] right, Comparison<T> comparison) {
            if(left == null && right == null) {
                return new T[0];
            }
            if(left == null) {
                return right;
            }
            if(right == null) {
                return left;
            }
            List<T> union = new List<T>();
            for(int i = 0; i < left.Length; i++) {
                int inner = i;
                if(!union.Exists(x => comparison.Invoke(x, left[inner]) == 0)) {
                    union.Add(left[i]);
                }
            }
            for(int i = 0; i < right.Length; i++) {
                int inner = i;
                if(!union.Exists(x => comparison.Invoke(x, right[inner]) == 0)) {
                    union.Add(right[i]);
                }
            }
            return union.ToArray();
        }

        /// <summary>
        /// Indicates whether the specified list is null or empty.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="IList{T}"/> items.</typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty<T>(IList<T> items) {
            return (items == null) || (items.Count == 0);
        }

        /// <summary>
        /// Indicates whether the specified arrays are either both null or of the same length.
        /// </summary>
        /// <param name="left">Array to examine.</param>
        /// <param name="right">Array to examine.</param>
        /// <returns><see langword="true"/> if both arrays are either null or have the same length.</returns>
        public static bool AreNullOrEqualLength(Array left, Array right) {
            if((left == null) && (right == null)) {
                return true;
            } else if((left != null) && (right != null)) {
                return left.Length == right.Length;
            }
            return false;
        }

        /// <summary>
        /// Retrieve all key/value pairs from the collection as an enumerable collection.
        /// </summary>
        /// <param name="collection">NameValueCollection instance.</param>
        /// <returns>Enumerable collection of key/value pairs.</returns>
        public static IEnumerable<KeyValuePair<string, string>> AllKeyValues(NameValueCollection collection) {
            if(collection != null) {
                for(int i = 0; i < collection.Count; ++i) {
                    string key = collection.GetKey(i);
                    string values = collection.Get(key);
                    if(values != null) {
                        foreach(string value in collection.GetValues(i)) {
                            yield return new KeyValuePair<string, string>(key, value);
                        }
                    } else {
                        yield return new KeyValuePair<string, string>(key, null);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve all key/value pairs from the collection as an array.
        /// </summary>
        /// <param name="collection">NameValueCollection instance.</param>
        /// <returns>Array of key/value pairs.</returns>
        public static KeyValuePair<string, string>[] AllKeyValuePairs(NameValueCollection collection) {
            if(collection == null) {
                return new KeyValuePair<string, string>[0];
            }
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            for(int i = 0; i < collection.Count; ++i) {
                string key = collection.GetKey(i);
                string values = collection.Get(key);
                if(values != null) {
                    foreach(string value in collection.GetValues(i)) {
                        result.Add(new KeyValuePair<string, string>(key, value));
                    }
                } else {
                    result.Add(new KeyValuePair<string, string>(key, null));
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Compute an array of differences between two arrays.
        /// </summary>
        /// <typeparam name="T">Type of input array items.</typeparam>
        /// <param name="before">Array of original values.</param>
        /// <param name="after">Array of values after modification.</param>
        /// <param name="maxsize">Deprecated parameter, no longer used.</param>
        /// <param name="equal">Delegate for value comparison.</param>
        /// <returns>Array of difference kind and value tuples.</returns>
        public static Tuplet<ArrayDiffKind, T>[] Diff<T>(T[] before, T[] after, int maxsize, Equality<T> equal) where T : class {
            if(before == null) {
                throw new ArgumentNullException("before");
            }
            if(after == null) {
                throw new ArgumentNullException("after");
            }
            if(equal == null) {
                equal = ObjectEquality;
            }

            // reverse lists for procesing
            List<Tuplet<ArrayDiffKind, T>> result = null;

#if false
            // skip matching items at the beginning
            int i_start = 0;
            int j_start = 0;
            while((i_start < before.Length) && (j_start < after.Length) && equal(before[i_start], after[j_start])) {
                ++i_start;
                ++j_start;
            }

            // skip matching items at the end
            int i_end = before.Length - 1;
            int j_end = after.Length - 1;
            while((i_end > i_start) && (j_end > j_start) && equal(before[i_end], after[j_end])) {
                --i_end;
                --j_end;
            }

            // check if the table is bigger than what we want
            if((maxsize <= 0) || ((i_end - i_start + 2) + (j_end - j_start + 2) <= maxsize)) {
                result = new List<Tuple<ArrayDiffKind, T>>(before.Length + after.Length);

                // copy matching beginning
                for(int i = 0; i < i_start; ++i) {
                    result.Add(new Tuple<ArrayDiffKind, T>(ArrayDiffKind.Same, before[i]));
                }

                // check if anything is left to compare
                if((i_start <= i_end) || (j_start <= j_end)) {

                    // find path of longest common subsequence
                    HirschbergDiff(before, i_start, i_end, after, j_start, j_end, equal, result);

                    // copy matching ending
                    for(int i = i_end + 1; i < before.Length; ++i) {
                        result.Add(new Tuple<ArrayDiffKind, T>(ArrayDiffKind.Same, before[i]));
                    }
                }
            }
#else
            // run myers diff algorithm
            result = new List<Tuplet<ArrayDiffKind, T>>(before.Length + after.Length);
            MyersDiff(before, after, equal, result);
#endif
            return (result != null) ? result.ToArray() : null;
        }

        /// <summary>
        /// Merge two diffs.
        /// </summary>
        /// <typeparam name="T">Type of value items in the provided diffs.</typeparam>
        /// <param name="left">Left hand diff.</param>
        /// <param name="right">Right hand diff.</param>
        /// <param name="priority">Priority of resolving ambigious <see cref="ArrayDiffKind"/> values.</param>
        /// <param name="equal">Equality delegate for value comparison.</param>
        /// <param name="track">Tracking function for correlating related diff items, in case on of the related items is ambigious.</param>
        /// <param name="hasConflicts">Indicator whether any conflicts were found.</param>
        /// <returns>Diff result of merge.</returns>
        public static Tuplet<ArrayDiffKind, T>[] MergeDiff<T>(Tuplet<ArrayDiffKind, T>[] left, Tuplet<ArrayDiffKind, T>[] right, ArrayMergeDiffPriority priority, Equality<T> equal, Func<T, object> track, out bool hasConflicts) where T : class {
            if(left == null) {
                throw new ArgumentNullException("left");
            }
            if(right == null) {
                throw new ArgumentNullException("right");
            }

            // set a default equality test if none is provided
            if(equal == null) {
                equal = delegate(T lhs, T rhs) { return ((lhs == null) && (rhs == null)) || (((lhs != null) && (rhs != null)) && (lhs == rhs)); };
            }

            // loop over all entries and mark them as conflicted when appropriate
            List<Tuplet<ArrayDiffKind, T>> result = new List<Tuplet<ArrayDiffKind, T>>(left.Length + right.Length);
            int leftIndex = 0;
            int rightIndex = 0;
            bool ambiguous = false;
            hasConflicts = false;
            while(true) {
                if((leftIndex < left.Length) && (rightIndex < right.Length)) {
                    if(left[leftIndex].Item1 == ArrayDiffKind.Same && right[rightIndex].Item1 == ArrayDiffKind.Same) {

                        // NOTE: both sides agree to keep the current element

                        result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Same, left[leftIndex].Item2));
                        ++leftIndex;
                        ++rightIndex;
                        ambiguous = false;
                    } else if(left[leftIndex].Item1 == ArrayDiffKind.Same && right[rightIndex].Item1 == ArrayDiffKind.Removed) {

                        // NOTE: right-side removed element; this could be conflict

                        result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.RemovedRight, right[rightIndex].Item2));
                        ++leftIndex;
                        ++rightIndex;
                    } else if(left[leftIndex].Item1 == ArrayDiffKind.Same && right[rightIndex].Item1 == ArrayDiffKind.Added) {

                        // NOTE: right-side added an element; conflict depends on ambiguity of current position

                        if(ambiguous) {
                            result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.AddedRight, right[rightIndex].Item2));
                        } else {
                            result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Added, right[rightIndex].Item2));
                        }
                        ++rightIndex;
                    } else if(left[leftIndex].Item1 == ArrayDiffKind.Removed && right[rightIndex].Item1 == ArrayDiffKind.Same) {

                        // NOTE: left-side removed element; this could be conflict

                        result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.RemovedLeft, left[leftIndex].Item2));
                        ++leftIndex;
                        ++rightIndex;
                    } else if(left[leftIndex].Item1 == ArrayDiffKind.Removed && right[rightIndex].Item1 == ArrayDiffKind.Removed) {

                        // NOTE: both sides removed the element; subsequent operations are ambiguous

                        result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Removed, left[leftIndex].Item2));
                        ++leftIndex;
                        ++rightIndex;
                        ambiguous = true;
                    } else if(left[leftIndex].Item1 == ArrayDiffKind.Removed && right[rightIndex].Item1 == ArrayDiffKind.Added) {

                        // NOTE: right-side is attempting to add an item after an item that is deleted on the left-side

                        result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.AddedRight, right[rightIndex].Item2));
                        ++rightIndex;
                        ambiguous = true;
                    } else if(left[leftIndex].Item1 == ArrayDiffKind.Added && right[rightIndex].Item1 == ArrayDiffKind.Same) {

                        // NOTE: left-side added an element; conflict depends on ambiguity of current position

                        if(ambiguous) {
                            result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.AddedLeft, left[leftIndex].Item2));
                        } else {
                            result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Added, left[leftIndex].Item2));
                        }
                        ++leftIndex;
                    } else if(left[leftIndex].Item1 == ArrayDiffKind.Added && right[rightIndex].Item1 == ArrayDiffKind.Removed) {

                        // NOTE: left-side is attempting to add an item after an item that is deleted on the right-side

                        result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.AddedLeft, left[leftIndex].Item2));
                        ++leftIndex;
                        ambiguous = true;
                    } else if(left[leftIndex].Item1 == ArrayDiffKind.Added && right[rightIndex].Item1 == ArrayDiffKind.Added) {

                        // NOTE: both sides are adding element; order is ambgiguous, unless it's the same item

                        if(equal(left[leftIndex].Item2, right[rightIndex].Item2)) {
                            result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Added, left[leftIndex].Item2));
                        } else {
                            result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.AddedLeft, left[leftIndex].Item2));
                            result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.AddedRight, right[rightIndex].Item2));
                        }
                        ++leftIndex;
                        ++rightIndex;
                        ambiguous = true;
                    } else {
                        throw new InvalidOperationException();
                    }
                } else if(leftIndex < left.Length) {
                    if(left[leftIndex].Item1 == ArrayDiffKind.Same) {
                        throw new InvalidOperationException();
                    } else if(left[leftIndex].Item1 == ArrayDiffKind.Removed) {
                        throw new InvalidOperationException();
                    } else if(left[leftIndex].Item1 == ArrayDiffKind.Added) {

                        // NOTE: left-side added an element; conflict depends on ambiguity of current position

                        if(ambiguous) {
                            result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.AddedLeft, left[leftIndex].Item2));
                        } else {
                            result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Added, left[leftIndex].Item2));
                        }
                    }
                    ++leftIndex;
                } else if(rightIndex < right.Length) {
                    if(right[rightIndex].Item1 == ArrayDiffKind.Same) {
                        throw new InvalidOperationException();
                    } else if(right[rightIndex].Item1 == ArrayDiffKind.Removed) {
                        throw new InvalidOperationException();
                    } else if(right[rightIndex].Item1 == ArrayDiffKind.Added) {

                        // NOTE: right-side added an element; conflict depends on ambiguity of current position

                        if(ambiguous) {
                            result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.AddedRight, right[rightIndex].Item2));
                        } else {
                            result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Added, right[rightIndex].Item2));
                        }
                    }
                    ++rightIndex;
                } else {
                    break;
                }
            }

            // convert 'RemovedLeft' and 'RemovedRight' if they aren't followed by 'AddedLeft' or 'AddedRight'
            ambiguous = false;
            for(int i = result.Count - 1; i >= 0; --i) {
                switch(result[i].Item1) {
                case ArrayDiffKind.Same:
                case ArrayDiffKind.Removed:
                case ArrayDiffKind.Added:
                    ambiguous = false;
                    break;
                case ArrayDiffKind.AddedLeft:
                case ArrayDiffKind.AddedRight:
                    ambiguous = true;
                    hasConflicts = true;
                    break;
                case ArrayDiffKind.RemovedLeft:
                case ArrayDiffKind.RemovedRight:
                    if(!ambiguous) {
                        result[i].Item1 = ArrayDiffKind.Removed;
                    } else {
                        hasConflicts = true;
                    }
                    break;
                }
            }


            // check if we need to track dependencies between changes
            if(track != null) {
                Dictionary<object, List<Tuplet<ArrayDiffKind, T>>> dependencies = new Dictionary<object, List<Tuplet<ArrayDiffKind, T>>>();
                Dictionary<object, List<Tuplet<ArrayDiffKind, T>>> conflictsLeft = new Dictionary<object, List<Tuplet<ArrayDiffKind, T>>>();
                Dictionary<object, List<Tuplet<ArrayDiffKind, T>>> conflictsRight = new Dictionary<object, List<Tuplet<ArrayDiffKind, T>>>();

                // detect changes that are conflicting
                for(int i = 0; i < result.Count; ++i) {
                    object key = track(result[i].Item2);
                    if(key != null) {
                        List<Tuplet<ArrayDiffKind, T>> entry;

                        // add change to list
                        if(!dependencies.TryGetValue(key, out entry)) {
                            entry = new List<Tuplet<ArrayDiffKind, T>>();
                            dependencies.Add(key, entry);
                        }
                        entry.Add(result[i]);

                        // if change represents a conflict, add it to the conflict list
                        switch(result[i].Item1) {
                        case ArrayDiffKind.AddedLeft:
                        case ArrayDiffKind.RemovedLeft:
                            conflictsLeft[key] = entry;
                            break;
                        case ArrayDiffKind.AddedRight:
                        case ArrayDiffKind.RemovedRight:
                            conflictsRight[key] = entry;
                            break;
                        }
                    }
                }

                // visit all left conflicts and elevate any 'Added' or 'Removed' items to 'AddedLeft' or 'RemovedLeft'
                foreach(List<Tuplet<ArrayDiffKind, T>> entry in conflictsLeft.Values) {
                    foreach(Tuplet<ArrayDiffKind, T> item in entry) {
                        if(item.Item1 == ArrayDiffKind.Added) {
                            item.Item1 = ArrayDiffKind.AddedLeft;
                        } else if(item.Item1 == ArrayDiffKind.Removed) {
                            item.Item1 = ArrayDiffKind.RemovedLeft;
                        }
                    }
                }

                // visit all right conflicts and elevate any 'Added' or 'Removed' items to 'AddedRight' or 'RemovedRight'
                foreach(List<Tuplet<ArrayDiffKind, T>> entry in conflictsRight.Values) {
                    foreach(Tuplet<ArrayDiffKind, T> item in entry) {
                        if(item.Item1 == ArrayDiffKind.Added) {
                            item.Item1 = ArrayDiffKind.AddedRight;
                        } else if(item.Item1 == ArrayDiffKind.Removed) {
                            item.Item1 = ArrayDiffKind.RemovedRight;
                        }
                    }
                }
            }

            // check if there is a merge priority to remove ambiguous changes
            if(priority != ArrayMergeDiffPriority.None) {

                // copy only accepted changes
                List<Tuplet<ArrayDiffKind, T>> combined = result;
                result = new List<Tuplet<ArrayDiffKind, T>>(combined.Count);
                for(int i = 0; i < combined.Count; ++i) {
                    switch(combined[i].Item1) {
                    case ArrayDiffKind.Same:
                    case ArrayDiffKind.Removed:
                    case ArrayDiffKind.Added:
                        result.Add(combined[i]);
                        break;
                    case ArrayDiffKind.AddedLeft:
                        if(priority == ArrayMergeDiffPriority.Left) {
                            combined[i].Item1 = ArrayDiffKind.Added;
                            result.Add(combined[i]);
                        }
                        break;
                    case ArrayDiffKind.RemovedLeft:
                        if(priority == ArrayMergeDiffPriority.Left) {
                            combined[i].Item1 = ArrayDiffKind.Removed;
                        } else {
                            combined[i].Item1 = ArrayDiffKind.Same;
                        }
                        result.Add(combined[i]);
                        break;
                    case ArrayDiffKind.AddedRight:
                        if(priority == ArrayMergeDiffPriority.Right) {
                            combined[i].Item1 = ArrayDiffKind.Added;
                            result.Add(combined[i]);
                        }
                        break;
                    case ArrayDiffKind.RemovedRight:
                        if(priority == ArrayMergeDiffPriority.Right) {
                            combined[i].Item1 = ArrayDiffKind.Removed;
                        } else {
                            combined[i].Item1 = ArrayDiffKind.Same;
                        }
                        result.Add(combined[i]);
                        break;
                    }
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Removes the last occurrence of an item in a list.
        /// </summary>
        /// <typeparam name="T">Generic type of list.</typeparam>
        /// <param name="list">List from which to remove the item from.</param>
        /// <param name="item">Item to remove.</param>
        public static void RemoveLast<T>(this List<T> list, T item) {
            if(list != null) {
                for(var i = list.Count - 1; i >= 0; --i) {
                    if(list[i].Equals(item)) {
                        list.RemoveAt(i);
                    }
                }
            }
        }

        private static int NormalizeIndex(Array array, int index) {
            return NormalizeIndex(array.Length, index);
        }

        private static int NormalizeIndex(int length, int index) {
            if(index < 0) {
                index += length;
                if(index < 0) {
                    return 0;
                }
            }
            if(index > length) {
                return length;
            }
            return index;
        }

        private static bool ObjectEquality<T>(T left, T right) where T : class {
            return ((left == null) && (right == null)) || (((left != null) && (right != null)) && (left == right));
        }

        private static uint[] FrontToBackMaxLCS<T>(T[] left, int i_start, int i_end, T[] right, int j_start, int j_end, Equality<T> equal) where T : class {
            uint[] pre = new uint[j_end - j_start + 2];
            uint[] cur = new uint[j_end - j_start + 2];
            for(int i = i_start; i <= i_end; ++i) {
                Array.Copy(cur, pre, cur.Length);
                for(int j = j_start; j <= j_end; ++j) {
                    if(equal(left[i], right[j])) {
                        cur[j - j_start + 1] = pre[j - j_start] + 1;
                    } else {
                        cur[j - j_start + 1] = Math.Max(cur[j - j_start], pre[j - j_start + 1]);
                    }
                }
            }
            return cur;
        }

        private static uint[] BackToFrontMaxLCS<T>(T[] left, int i_start, int i_end, T[] right, int j_start, int j_end, Equality<T> equal) where T : class {
            uint[] pre = new uint[j_end - j_start + 2];
            uint[] cur = new uint[j_end - j_start + 2];
            for(int i = i_start; i <= i_end; ++i) {
                Array.Copy(cur, pre, cur.Length);
                for(int j = j_start; j <= j_end; ++j) {
                    if(equal(left[i_end - i + i_start], right[j_end - j + j_start])) {
                        cur[j - j_start + 1] = pre[j - j_start] + 1;
                    } else {
                        cur[j - j_start + 1] = Math.Max(cur[j - j_start], pre[j - j_start + 1]);
                    }
                }
            }
            return cur;
        }

        private static void HirschbergDiff<T>(T[] before, int i_start, int i_end, T[] after, int j_start, int j_end, Equality<T> equal, List<Tuplet<ArrayDiffKind, T>> result) where T : class {
            if(j_start > j_end) {

                // 'after' is empty, so 'before' was removed
                for(int i = i_start; i <= i_end; ++i) {
                    result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Removed, before[i]));
                }
            } else if(i_start > i_end) {

                // 'before' is empty, so 'after' was added
                for(int j = j_start; j <= j_end; ++j) {
                    result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Added, after[j]));
                }
            } else if(i_start == i_end) {
                bool found = false;
                List<Tuplet<ArrayDiffKind, T>> accumulator = new List<Tuplet<ArrayDiffKind, T>>();

                // check if the single 'before' element occurs in the 'after' sequence
                for(int j = j_end; j >= j_start; --j) {
                    if(equal(before[i_start], after[j])) {

                        // found a common element
                        found = true;
                        accumulator.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Same, after[j]));
                    } else {
                        accumulator.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Added, after[j]));
                    }
                }

                // add missing 'before' element as removed
                if(!found) {
                    accumulator.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Removed, before[i_start]));
                }

                // reverse list (we want the removed element always at the beginning) and add it to the result
                accumulator.Reverse();
                result.AddRange(accumulator);
            } else {

                // compute possible solutions for sub-quadrants
                int i_mid = i_start + (i_end - i_start + 1) / 2 - 1;
                uint[] l1 = FrontToBackMaxLCS(before, i_start, i_mid, after, j_start, j_end, equal);
                uint[] l2 = BackToFrontMaxLCS(before, i_mid + 1, i_end, after, j_start, j_end, equal);

                // find optimal partitioning of sub-quadrants
                uint max = 0;
                int j_mid = 0;
                for(int j = l1.Length - 1; j >= 0; --j) {
                    uint value = l1[j] + l2[(l1.Length - 1) - j];
                    if(value >= max) {
                        max = value;
                        j_mid = j_start + j - 1;
                    }
                }

                // recurse into optimal sub-quadrants
                HirschbergDiff(before, i_start, i_mid, after, j_start, j_mid, equal, result);
                HirschbergDiff(before, i_mid + 1, i_end, after, j_mid + 1, j_end, equal, result);
            }
        }

        private static void MyersDiff<T>(T[] before, T[] after, Equality<T> equal, List<Tuplet<ArrayDiffKind, T>> result) where T : class {

            // NOTE (steveb): we mark items as 'Added' when they are actually 'Removed' and vice versa; this means the initial 'before' and 'after' arrays must be passed in reversed order as well;
            //                the reason for doing so is that it will show first 'Removed' items, and then the 'Added' ones, which is what the 3-way merge algorithm requires.

            // initialize temp arrays required for search
            int max = before.Length + after.Length + 1;
            var down_array = new ChunkedArray<int>(2 * max + 2);
            var up_array = new ChunkedArray<int>(2 * max + 2);

            // run myers diff algorithm
            MyersDiffRev(after, 0, after.Length - 1, before, 0, before.Length - 1, equal, result, max, down_array, up_array);
        }

        private static void MyersDiffRev<T>(T[] before, int i_start, int i_end, T[] after, int j_start, int j_end, Equality<T> equal, List<Tuplet<ArrayDiffKind, T>> result, int max, ChunkedArray<int> down_array, ChunkedArray<int> up_array) where T : class {

            // NOTE (steveb): we mark items as 'Added' when they are actually 'Removed' and vice versa; this means the initial 'before' and 'after' arrays must be passed in reversed order as well;
            //                the reason for doing so is that it will show first 'Removed' items, and then the 'Added' ones, which is what the 3-way merge algorithm requires.

            // skip matching items at the beginning
            while((i_start <= i_end) && (j_start <= j_end) && equal(before[i_start], after[j_start])) {

                // add skipped item
                result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Same, before[i_start]));

                // move diagonally
                ++i_start;
                ++j_start;
            }

            // skip matching items at the end
            int tailCopyCount = 0;
            while((i_start <= i_end) && (j_start <= j_end) && equal(before[i_end], after[j_end])) {

                // count skipped items
                ++tailCopyCount;

                // move diagonally
                --i_end;
                --j_end;
            }

            // check if we found a terminal case or if new need to recurse
            if(j_start > j_end) {

                // add remaining items as 'removed'
                while(i_start <= i_end) {
                    result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Added, before[i_start++]));
                }
            } else if(i_start > i_end) {

                // add remaining items as 'added'
                while(j_start <= j_end) {
                    result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Removed, after[j_start++]));
                }
            } else {

                // find the optimal path
                int i_split;
                int j_split;
                ComputeMyersSplit(before, i_start, i_end, out i_split, after, j_start, j_end, out j_split, equal, max, down_array, up_array);

                // solve sub-problem
                MyersDiffRev(before, i_start, i_split - 1, after, j_start, j_split - 1, equal, result, max, down_array, up_array);
                MyersDiffRev(before, i_split, i_end, after, j_split, j_end, equal, result, max, down_array, up_array);
            }

            // add tail items
            for(int k = 1; k <= tailCopyCount; ++k) {
                result.Add(new Tuplet<ArrayDiffKind, T>(ArrayDiffKind.Same, before[i_end + k]));
            }
        }

        private static void ComputeMyersSplit<T>(T[] before, int i_start, int i_end, out int i_split, T[] after, int j_start, int j_end, out int j_split, Equality<T> equal, int max, ChunkedArray<int> down_array, ChunkedArray<int> up_array) {

            // check if problem space is odd or even sized
            bool odd = (((i_end - i_start + 1) - (j_end - j_start + 1)) & 1) != 0;

            // the k-line to start the forward search
            int down_k = i_start - j_start;

            // the k-line to start the reverse search
            int up_k = i_end - j_end;

            // The vectors in the publication accepts negative indexes. the vectors implemented here are 0-based
            // and are access using a specific offset: UpOffset UpVector and DownOffset for DownVektor
            int down_offset = max - down_k;
            int up_offset = max - up_k;

            // initialize arrays
            down_array[down_offset + down_k + 1] = i_start;
            up_array[up_offset + up_k - 1] = i_end + 1;

            // find furthest reaching D-path
            int d_max = (((i_end - i_start + 1) + (j_end - j_start + 1) + 1) / 2);
            for(int d = 0; d <= d_max; ++d) {

                // search forward
                for(int k = down_k - d; k <= down_k + d; k += 2) {

                    // find the only or better starting point
                    int x;
                    int y;
                    if(k == down_k - d) {
                        x = down_array[down_offset + k + 1]; // down
                    } else {
                        x = down_array[down_offset + k - 1] + 1; // a step to the right
                        if((k < down_k + d) && (down_array[down_offset + k + 1] >= x)) {
                            x = down_array[down_offset + k + 1]; // down
                        }
                    }
                    y = x - k;

                    // find the end of the furthest reaching forward D-path in diagonal k
                    while((x <= i_end) && (y <= j_end) && equal(before[x], after[y])) {
                        ++x;
                        ++y;
                    }
                    down_array[down_offset + k] = x;

                    // overlap ?
                    if(odd && (up_k - d < k) && (k < up_k + d)) {
                        if(up_array[up_offset + k] <= down_array[down_offset + k]) {
                            i_split = down_array[down_offset + k];
                            j_split = down_array[down_offset + k] - k;
                            return;
                        }
                    }
                }

                // search backwards
                for(int k = up_k - d; k <= up_k + d; k += 2) {

                    // find the only or better starting point
                    int x;
                    int y;
                    if(k == up_k + d) {
                        x = up_array[up_offset + k - 1]; // up
                    } else {
                        x = up_array[up_offset + k + 1] - 1; // left
                        if((k > up_k - d) && (up_array[up_offset + k - 1] < x)) {
                            x = up_array[up_offset + k - 1]; // up
                        }
                    }
                    y = x - k;

                    while((x > i_start) && (y > j_start) && equal(before[x - 1], after[y - 1])) {
                        --x;
                        --y; // diagonal
                    }
                    up_array[up_offset + k] = x;

                    // overlap ?
                    if(!odd && (down_k - d <= k) && (k <= down_k + d)) {
                        if(up_array[up_offset + k] <= down_array[down_offset + k]) {
                            i_split = down_array[down_offset + k];
                            j_split = down_array[down_offset + k] - k;
                            return;
                        }
                    }
                }
            }
            throw new InvalidOperationException("this should never happen");
        }

        #region --- Obsolete ---
        /// <summary>
        /// Resize(T[], int) is obsolete. Use SubArray(T[], int) instead.
        /// </summary>
        [Obsolete("Resize(T[], int) is obsolete. Use SubArray(T[], int) instead.")]
        public static T[] Resize<T>(T[] array, int length) {
            return SubArray(array, 0, length);
        }

        /// <summary>
        /// AsHash&lt;K, V&gt;() is obsolete. Use IEnumerable&lt;T&gt;.ToDictionary&lt;T, K&gt;() extension method instead (requires System.Linq)
        /// </summary>
        [Obsolete("AsHash<K, V> is obsolete. Use IEnumerable<T>.ToDictionary extension method instead (requires System.Linq)")]
        public static Dictionary<K, V> AsHash<K, V>(this IEnumerable<V> collection, Converter<V, K> makeKey) {
            return collection == null ? new Dictionary<K, V>() : collection.ToDictionary(v => makeKey(v), true);
        }

        /// <summary>
        /// Select(IEnumerable&lt;T&gt;, Predicate&lt;T&gt;) is obsolete. Use IEnumerable&lt;T&gt;.Where(Func&lt;T, bool&gt;) instead (requires System.Linq).
        /// </summary>
        [Obsolete("Select(IEnumerable<T>, Predicate<T>) is obsolete. Use IEnumerable<T>.Where(Func<T, bool>) instead (requires System.Linq).")]
        public static List<T> Select<T>(IEnumerable<T> collection, Predicate<T> predicate) {
            return collection.Where(e => predicate(e)).ToList();
        }

        /// <summary>
        /// Convert(IEnumerable&lt;TInput&gt;, Converter&lt;TInput, TOutput&gt;) is obsolete. Use IEnumerable&lt;TInput&gt;.Select(Func&lt;TInput, TOutput&gt;) instead (requires System.Linq).
        /// </summary>
        [Obsolete("Convert(IEnumerable<TInput>, Converter<TInput, TOutput>) is obsolete. Use IEnumerable<TInput>.Select(Func<TInput, TOutput>) instead (requires System.Linq).")]
        public static List<TOutput> Convert<TInput, TOutput>(IEnumerable<TInput> collection, Converter<TInput, TOutput> converter) {
            return collection.Select(e => converter(e)).ToList();
        }
        #endregion
    }
}
