/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
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

namespace System.Collections.Generic {
    public static class DictionaryUtil {

        //--- Class Methods ---
        public static Dictionary<K, V> ToDictionaryWithDuplicateErrorCallback<K, V, T>(this IEnumerable<T> source, Func<T, K> keySelector, Func<T, V> valueSelector, Action<T, Dictionary<K, V>, DictionaryDuplicateKeyException> errorCallback) {
            if(errorCallback == null) {
                throw new ArgumentNullException("errorCallback");
            }
            var result = new Dictionary<K, V>();
            foreach(var item in source) {
                var key = keySelector(item);
                var value = valueSelector(item);
                try {
                    result.Add(key, value);
                } catch(ArgumentException e) {
                    errorCallback(item, result, new DictionaryDuplicateKeyException("An element with the same key already exists in the dictionary.", e));
                }
            }
            return result;
        }

        public static IEnumerable<TValue> CollectAllValues<TKey, TValue>(this IDictionary<TKey, ICollection<TValue>> dictionary, IEnumerable<TKey> keys, out IEnumerable<TKey> missing) {
            var result = new List<TValue>();
            var missingResult = new List<TKey>();
            foreach(var key in keys) {
                ICollection<TValue> value;
                if(dictionary.TryGetValue(key, out value)) {
                    result.AddRange(value);
                } else {
                    missingResult.Add(key);
                }
            }
            missing = missingResult;
            return result;
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, bool overwriteDuplicates = false) {
            var result = new Dictionary<TKey, TValue>();
            if(overwriteDuplicates) {
                foreach(var pair in source) {
                    result[pair.Key] = pair.Value;
                }
            } else {
                foreach(var pair in source) {
                    result.Add(pair.Key, pair.Value);
                }
            }
            return result;
        } 
    }
}
