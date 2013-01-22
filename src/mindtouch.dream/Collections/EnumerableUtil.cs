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
using System.Linq;

namespace MindTouch {
    public static class EnumerableUtil {

        //--- Extension Methods ---

        /// <summary>
        /// Only keep the first occurrence of duplicate items from the Enumerable and preserve the original order.
        /// </summary>
        /// <param name="enumerable">Enumerable of items.</param>
        /// <param name="comparison">Function returning the item key used to determine uniqueness.</param>
        /// <typeparam name="TSource">Item type.</typeparam>
        /// <typeparam name="TKey">Item key type.</typeparam>
        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> comparison) {
            var hashSet = new HashSet<TKey>();
            foreach(var v in enumerable) {
                var c = comparison(v);
                if(hashSet.Contains(c)) {
                    continue;
                }
                hashSet.Add(c);
                yield return v;
            }
        }

        /// <summary>
        /// Concatenate a list of parameters to an Enumerable of the same type.
        /// </summary>
        /// <param name="seq">Enumerable of items.</param>
        /// <param name="value">Values to append to Enumerable.</param>
        /// <typeparam name="T">Item type.</typeparam>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> seq, params T[] value) {
            return seq.Concat((IEnumerable<T>)value);
        }

        /// <summary>
        /// Convert Enumerable into a HashSet of same type.
        /// </summary>
        /// <returns>Hashset built from values.</returns>
        /// <param name="seq">Enumerable of items.</param>
        /// <typeparam name="T">Item type.</typeparam>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> seq) {
            if(seq is HashSet<T>) {
                return (HashSet<T>)seq;
            }
            return new HashSet<T>(seq);
        }

        /// <summary>
        /// Extract values from the Enumerable and convert them into a HachSet.
        /// </summary>
        /// <returns>HashSet built from extracted values.</returns>
        /// <param name="seq">Enumerable of items.</param>
        /// <param name="selector">Function to convert Enumerable item into a value.</param>
        /// <typeparam name="TSource">Item type.</typeparam>
        /// <typeparam name="TKey">Extracted value type.</typeparam>
        public static HashSet<TKey> ToHashSet<TSource, TKey>(this IEnumerable<TSource> seq, Func<TSource, TKey> selector) {
            return seq.Select(selector).ToHashSet();
        }

        /// <summary>
        /// Check if the Enumerable is either null or contains no items.
        /// </summary>
        /// <returns><c>true</c>, if null or empty, <c>false</c> otherwise.</returns>
        /// <param name="enumerable">Enumerable of items.</param>
        /// <typeparam name="T">Item type.</typeparam>
        public static bool NullOrEmpty<T>(this IEnumerable<T> enumerable) {
            return (enumerable == null) || enumerable.None();
        }

        /// <summary>
        /// Split the Enumerable into a sequence of Enumerables with capped length.
        /// </summary>
        /// <returns>Enumerable of Enumerables with capped length.</returns>
        /// <param name="source">Enumerable of items.</param>
        /// <param name="chunkSize">Maximum chunk length.</param>
        /// <typeparam name="T">Item type.</typeparam>
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, int chunkSize) {
            var chunk = new List<T>(chunkSize);
            foreach(var x in source) {
                chunk.Add(x);
                if(chunk.Count <= chunkSize) {
                    continue;
                }
                yield return chunk;
                chunk = new List<T>(chunkSize);
            }
            if(chunk.Any()) {
                yield return chunk;
            }
        }

        /// <summary>
        /// Converted Enumerable into OrderedEnumerable with parametrized ordering direction.
        /// </summary>
        /// <returns>An OrderedEnumerable of items.</returns>
        /// <param name="source">Enumerable of items.</param>
        /// <param name="keySelector">Function returning the item key used for ordering.</param>
        /// <param name="descending">Orders items in descending order when set to <c>true</c>, otherwise items are ordered in ascending order.</param>
        /// <typeparam name="TSource">Item type.</typeparam>
        /// <typeparam name="TKey">Item key type.</typeparam>
        public static IOrderedEnumerable<TSource> OrderByWithDirection<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, bool descending) {
            return descending ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
        }

        /// <summary>
        /// Returns <c>true</c> when the Enumerable is empty.
        /// </summary>
        /// <param name="enumerable">Enumerable of items.</param>
        /// <typeparam name="T">Item type.</typeparam>
        public static bool None<T>(this IEnumerable<T> enumerable) {
            return !enumerable.Any();
        }
    }
}

