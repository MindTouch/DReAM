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
    public static class DictionaryUtil {

        //--- Methods ---

        /// <summary>
        /// Check which keys are missing from supplied dictionary.
        /// </summary>
        /// <returns>Keys missing in dictionary.</returns>
        /// <param name="dictionary">Dictionary to check.</param>
        /// <param name="keys">Keys to find.</param>
        /// <typeparam name="TKey">Dictionary key type.</typeparam>
        /// <typeparam name="TValue">Dictionary value type.</typeparam>
        public static IEnumerable<TKey> MissingKeys<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys) {
            var missing = (from key in keys where !dictionary.ContainsKey(key) select key).ToArray();
            return missing;
        }

        /// <summary>
        /// Collects the values for the supplied keys and provide an Enumerable of missing keys.
        /// </summary>
        /// <returns>The values found in dictionary.</returns>
        /// <param name="dictionary">Dictionary to check.</param>
        /// <param name="keys">Keys to find.</param>
        /// <param name="missing">Enumerable of missing keys.</param>
        /// <typeparam name="TKey">Dictionary key type.</typeparam>
        /// <typeparam name="TValue">Dictionary value type.</typeparam>
        public static IEnumerable<TValue> CollectValues<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys, out IEnumerable<TKey> missing) {
            var result = new List<TValue>();
            var missingResult = new List<TKey>();
            foreach(var key in keys) {
                TValue value;
                if(dictionary.TryGetValue(key, out value)) {
                    result.Add(value);
                } else {
                    missingResult.Add(key);
                }
            }
            missing = missingResult;
            return result;
        }

        /// <summary>
        /// Collects the key-value pairs for the supplied keys and provide an Enumerable of missing keys.
        /// </summary>
        /// <returns>The key-value pairs found in dictionary.</returns>
        /// <param name="dictionary">Dictionary to check.</param>
        /// <param name="keys">Keys to find.</param>
        /// <param name="missing">Enumerable of missing keys.</param>
        /// <typeparam name="TKey">Dictionary key type.</typeparam>
        /// <typeparam name="TValue">Dictionary value type.</typeparam>
        public static IEnumerable<KeyValuePair<TKey, TValue>> Collect<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys, out IEnumerable<TKey> missing) {
            var result = new List<KeyValuePair<TKey, TValue>>();
            var missingResult = new List<TKey>();
            foreach(var key in keys) {
                TValue value;
                if(dictionary.TryGetValue(key, out value)) {
                    result.Add(new KeyValuePair<TKey, TValue>(key, value));
                } else {
                    missingResult.Add(key);
                }
            }
            missing = missingResult;
            return result;
        }

        /// <summary>
        /// Add an Enumerable of key-value pairs to dictionary, overwriting duplicate entries as they are discovered.
        /// </summary>
        /// <returns>Supplied dictionary.</returns>
        /// <param name="dictionary">Dictionary to add to.</param>
        /// <param name="keyValuePairs">Enumerable of key-value pairs to add.</param>
        /// <typeparam name="TKey">Dictionary key type.</typeparam>
        /// <typeparam name="TValue">Dictionary value type.</typeparam>
        public static IDictionary<TKey, TValue> AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) {
            foreach(var pair in keyValuePairs) {
                dictionary[pair.Key] = pair.Value;
            }
            return dictionary;
        }

        /// <summary>
        /// Add an Enumerable of two-value tuples to dictionary, overwriting duplicate entries as they are discovered.
        /// </summary>
        /// <returns>Supplied dictionary.</returns>
        /// <param name="dictionary">Dictionary to add to.</param>
        /// <param name="keyValuePairs">Enumerable of two-value tuples to add.</param>
        /// <typeparam name="TKey">Dictionary key type.</typeparam>
        /// <typeparam name="TValue">Dictionary value type.</typeparam>
        public static IDictionary<TKey, TValue> AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<Tuple<TKey, TValue>> keyValuePairs) {
            foreach(var pair in keyValuePairs) {
                dictionary[pair.Item1] = pair.Item2;
            }
            return dictionary;
        }

        /// <summary>
        /// Remove all keys from supplied dictionary.
        /// </summary>
        /// <returns>Keys to remove from dictionary.</returns>
        /// <param name="dictionary">Dictionary to modify.</param>
        /// <param name="keys">Keys to remove.</param>
        /// <typeparam name="TKey">Dictionary key type.</typeparam>
        /// <typeparam name="TValue">Dictionary value type.</typeparam>
        public static IDictionary<TKey, TValue> RemoveKeys<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys) {
            foreach(var key in keys) {
                dictionary.Remove(key);
            }
            return dictionary;
        }
    }
}

