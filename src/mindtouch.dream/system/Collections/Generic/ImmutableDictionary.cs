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
    public class ImmutableDictionary<K,V> : IImmutableDictionary<K, V> {

        //--- Fields ---
        private readonly IDictionary<K, V> _dictionary;

        //--- Constructors ---
        public ImmutableDictionary(IDictionary<K, V> dictionary) {
            if(dictionary == null) {
                throw new ArgumentNullException("dictionary");
            }
            _dictionary = dictionary;
        }

        public ImmutableDictionary(IEnumerable<KeyValuePair<K, V>> keyValues) {
            if(keyValues == null) {
                throw new ArgumentNullException("keyValues");
            }
            _dictionary = new Dictionary<K, V>();
            foreach(var valuePair in keyValues) {
                _dictionary[valuePair.Key] = valuePair.Value;
            }
        }

        //--- Properties ---
        public int Count { get { return _dictionary.Count; } }
        public IEnumerable<K> Keys { get { return _dictionary.Keys; } }
        public V this[K key] { get { return _dictionary[key]; } }

        //--- Methods ---
        public bool TryGet(K key, out V value) {
            return _dictionary.TryGetValue(key, out value);
        }

        public V TryGet(K key, V @default) {
            V value;
            if(_dictionary.TryGetValue(key, out value)) {
                return value;
            }
            return @default;
        }

        //--- IEnumerable<KeyValuePair<K,V>> Members ---
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator() {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _dictionary.GetEnumerator();
        }
    }
}