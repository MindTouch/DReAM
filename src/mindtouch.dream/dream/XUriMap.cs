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

namespace MindTouch.Dream {

    /// <summary>
    /// Provides an <see cref="XUri"/> map to match the best parent uri.
    /// </summary>
    /// <remarks>
    /// This map is a counterpart to <see cref="XUriChildMap{T}"/>, which matches all child uris, instead of the best parent.
    /// Parent/Child relationships are determined by scheme/hostpot/path similarity.
    /// </remarks>
    /// <typeparam name="T">Type of object that is associated with each <see cref="XUri"/> entry</typeparam>
    public class XUriMap<T> {

        // NOTE (steveb): scheme -> host-port -> segment -> ... -> segment -> query#fragment => value

        //--- Types ---
        private class Entry {

            //--- Fields ---
            public readonly Dictionary<string, Entry> Segments;
            private T _value;
            private bool _hasValue;

            //--- Constructors ---
            public Entry() {
                this.Segments = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            }

            //--- Properties ---
            public T Value {
                get { return _value; }
                set {
                    _value = value;
                    _hasValue = true;
                }
            }

            public bool HasValue { get { return _hasValue; } }

            //--- Methods ---
            public void RemoveValue() {
                _value = default(T);
                _hasValue = false;
            }
        }

        //--- Class Methods ---
        private static void DigMatches(List<T> matches, Entry currentEntry) {
            if(currentEntry.HasValue) {
                matches.Add(currentEntry.Value);
            }
            foreach(Entry child in currentEntry.Segments.Values) {
                DigMatches(matches, child);
            }
        }


        //--- Fields ---
        private readonly Dictionary<string, Dictionary<string, Entry>> _schemes = new Dictionary<string, Dictionary<string, Entry>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<XUri> _keys = new List<XUri>();

        //--- Properties ---

        /// <summary>
        /// Total number of Uri's in map.
        /// </summary>
        public int Count { get { return _keys.Count; } }

        /// <summary>
        /// Enumerable of all Uri's in map.
        /// </summary>
        public IEnumerable<XUri> Keys { get { return _keys; } }

        /// <summary>
        /// Retrieve a reference object by it's Uri.
        /// </summary>
        /// <param name="key">Key uri.</param>
        /// <returns>Mapped reference object.</returns>
        public T this[XUri key] {
            get {
                T result;
                int similarity;
                if(!TryGetValue(key, out result, out similarity)) {
                    throw new KeyNotFoundException();
                }
                return result;
            }
            set {
                Add(key, value, false);
            }
        }

        //--- Methods ---

        /// <summary>
        /// Clear map of all entries.
        /// </summary>
        public void Clear() {
            _schemes.Clear();
            _keys.Clear();
        }

        /// <summary>
        /// Try to get the best matching parent uri.
        /// </summary>
        /// <param name="key">Uri to match.</param>
        /// <param name="value">When the method returns <see langword="True"/>, this variable contains the reference object for matched parent Uri.</param>
        /// <returns><see langword="True"/> if a match was found.</returns>
        public bool TryGetValue(XUri key, out T value) {
            int similarity;
            return TryGetValue(key, out value, out similarity);
        }

        /// <summary>
        /// Try to get the best matching parent uri.
        /// </summary>
        /// <param name="key">Uri to match.</param>
        /// <param name="value">When the method returns <see langword="True"/>, this variable contains the reference object for matched parent Uri.</param>
        /// <param name="similarity">When the method returns <see langword="True"/>, this variable contains of similarity match between the input and matched Uri.</param>
        /// <returns><see langword="True"/> if a match was found.</returns>
        public bool TryGetValue(XUri key, out T value, out int similarity) {
            Entry entry;
            bool result = TryGetEntry(key, out entry, out similarity);
            if((entry != null) && entry.HasValue) {
                value = entry.Value;
            } else {
                value = default(T);
            }
            return result;
        }

        /// <summary>
        /// Add a new entry to the map.
        /// </summary>
        /// <param name="key">Key Uri.</param>
        /// <param name="value">Reference object.</param>
        public void Add(XUri key, T value) {
            Add(key, value, true);
        }

        /// <summary>
        /// Remove an entry from the map by its key Uri.
        /// </summary>
        /// <param name="key">Key uri.</param>
        /// <returns><see langword="True"/> if the call found and removed an entry for the key.</returns>
        public bool Remove(XUri key) {
            Entry entry;
            int similarity;
            if(TryGetEntry(key, out entry, out similarity)) {
                entry.RemoveValue();
                _keys.Remove(key);
                return true;
            }
            return false;
        }

        private void Add(XUri key, T value, bool failIfExists) {

            // find/add uri scheme
            Dictionary<string, Entry> hostport;
            if(!_schemes.TryGetValue(key.Scheme, out hostport)) {
                hostport = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
                _schemes[key.Scheme] = hostport;
            }

            // find/add uri host-port
            Entry entry;
            string hostportKey = key.HostPort;
            if(!hostport.TryGetValue(hostportKey, out entry)) {
                entry = new Entry();
                hostport[hostportKey] = entry;
            }

            // find/add uri segments
            Dictionary<string, Entry> segments = entry.Segments;
            for(int i = 0; i < key.Segments.Length; ++i) {
                if(!segments.TryGetValue(key.Segments[i], out entry)) {
                    entry = new Entry();
                    segments[key.Segments[i]] = entry;
                    segments = entry.Segments;
                } else {
                    segments = entry.Segments;
                }
            }

            // set value
            if(failIfExists && entry.HasValue) {
                throw new ArgumentException("key already exists");
            } else if(!entry.HasValue) {
                _keys.Add(key);
            }
            entry.Value = value;
        }

        private bool TryGetEntry(XUri key, out Entry entry, out int similarity) {
            Entry newEntry;
            int score = 0;

            // set out parameters
            entry = null;
            similarity = 0;

            // find uri scheme
            Dictionary<string, Entry> hostport;
            if(!_schemes.TryGetValue(key.Scheme, out hostport)) {
                return false;
            }
            ++score;

            // find uri host-port
            string hostportKey = key.HostPort;
            if(!hostport.TryGetValue(hostportKey, out newEntry)) {
                return false;
            }
            ++score;

            // update result
            if(newEntry.HasValue) {
                entry = newEntry;
                similarity = score;
            }

            // find uri segments
            for(int i = 0; i < key.Segments.Length; ++i) {
                if(!newEntry.Segments.TryGetValue(key.Segments[i], out newEntry)) {
                    return false;
                }

                // update result
                ++score;
                if(newEntry.HasValue) {
                    entry = newEntry;
                    similarity = score;
                }
            }
            return (entry != null) && entry.HasValue;
        }

        /// <summary>
        /// Get all parent matches for a key.
        /// </summary>
        /// <param name="uri">Uri to match.</param>
        /// <returns>Enumerable of reference objects whose key Uri is a parent of the input uri.</returns>
        public IEnumerable<T> GetValues(XUri uri) {
            Entry entry;
            int similarity;
            List<T> matches = new List<T>();
            if(uri.Segments[uri.Segments.Length - 1] == "*") {

                // found a trailing wildcard, so our match works for any child of the uri
                uri = uri.WithoutLastSegment();

                // find uri scheme
                Dictionary<string, Entry> hostport;
                if(!_schemes.TryGetValue(uri.Scheme, out hostport)) {
                    return matches;
                }

                // find uri host-port
                string hostportKey = uri.HostPort;
                if(!hostport.TryGetValue(hostportKey, out entry)) {
                    return matches;
                }

                if(entry == null) {
                    return matches;
                }


                // find uri segments
                for(int i = 0; i < uri.Segments.Length; ++i) {
                    if(!entry.Segments.TryGetValue(uri.Segments[i], out entry)) {
                        return matches;
                    }
                }
                DigMatches(matches, entry);

            } else if(TryGetEntry(uri, out entry, out similarity) && similarity == uri.MaxSimilarity) {

                // no wildcard degenerates into a normal TryGetValue with exact similarity matches
                matches.Add(entry.Value);
            }
            return matches;
        }
    }
}
