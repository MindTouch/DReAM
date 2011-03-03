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
    /// Provides a <see cref="XUri"/> map for matching child uri's of an input Uri.
    /// </summary>
    /// <remarks>
    /// This map is a counterpart to <see cref="XUriMap{T}"/>, which matches the closest parent uri, instead of the children.
    /// Parent/Child relationships are determined by scheme/hostpot/path similarity.
    /// </remarks>
    /// <typeparam name="T">Type of object that is associated with each <see cref="XUri"/> entry</typeparam>
    public class XUriChildMap<T> {

        //--- Types ---
        private class Entry {

            //--- Fields ---
            public readonly Dictionary<string, Entry> Children = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            public readonly List<T> Exact = new List<T>();
            public readonly List<T> Wildcard = new List<T>();

            //--- Methods ---
            public void Clear() {
                Children.Clear();
                Exact.Clear();
                Wildcard.Clear();
            }
        }

        //--- Fields ---
        private readonly Entry _root = new Entry();
        private readonly bool _ignoreScheme;

        //--- Constructors ---

        /// <summary>
        /// Create a scheme-sensitive map.
        /// </summary>
        public XUriChildMap()
            : this(false) {
        }

        /// <summary>
        /// Create a map.
        /// </summary>
        /// <param name="ignoreScheme"><see langword="True"/> if this map ignores scheme differences in matches.</param>
        public XUriChildMap(bool ignoreScheme) {
            _ignoreScheme = ignoreScheme;
        }

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if this map ignores scheme differences in matches.
        /// </summary>
        public bool IgnoresScheme { get { return _ignoreScheme; } }

        //--- Methods ---

        /// <summary>
        /// Add a uri to the map.
        /// </summary>
        /// <param name="uri">Uri to add.</param>
        /// <param name="registrant">The reference object for the uri.</param>
        public void Add(XUri uri, T registrant) {
            string scheme = _ignoreScheme ? "any" : uri.Scheme;
            string hostport = uri.HostPort;
            Entry current = _root;
            Entry next;
            bool wildcard = false;
            if(uri.LastSegment == "*") {
                uri = uri.WithoutLastSegment();
                wildcard = true;
            }

            // add scheme
            if(!current.Children.TryGetValue(scheme, out next)) {
                next = new Entry();
                current.Children.Add(scheme, next);
            }
            current = next;

            // add Hostport
            if(!current.Children.TryGetValue(hostport, out next)) {
                next = new Entry();
                current.Children.Add(hostport, next);
            }
            current = next;

            // add rest of Uri
            for(int i = 0; i < uri.Segments.Length; i++) {
                if(!current.Children.TryGetValue(uri.Segments[i], out next)) {
                    next = new Entry();
                    current.Children.Add(uri.Segments[i], next);
                }
                current = next;
            }
            if(wildcard) {
                current.Wildcard.Add(registrant);
            } else {
                current.Exact.Add(registrant);
            }
        }

        /// <summary>
        /// Add a range of uri's for a single reference object
        /// </summary>
        /// <param name="uris">Uris to add.</param>
        /// <param name="registrant">The reference object for the uri.</param>
        public void AddRange(IEnumerable<XUri> uris, T registrant) {
            foreach(XUri uri in uris) {
                Add(uri, registrant);
            }
        }

        /// <summary>
        /// Get all matching reference objects for uri.
        /// </summary>
        /// <param name="uri">Uri to find child uri's for.</param>
        /// <returns>Collection of reference objects.</returns>
        public ICollection<T> GetMatches(XUri uri) {
            return GetMatches(uri, null);
        }

        /// <summary>
        /// Get all matching reference objects for uri that are also in the filter list.
        /// </summary>
        /// <param name="uri">Uri to find child uri's for.</param>
        /// <param name="filter">Collection of of matchable reference objects.</param>
        /// <returns>Filtered collection of reference objects.</returns>
        public ICollection<T> GetMatches(XUri uri, ICollection<T> filter) {
            string scheme = _ignoreScheme ? "any" : uri.Scheme;
            string hostport = uri.HostPort;
            Entry current = _root;
            Entry next;
            List<T> matches = new List<T>();

            // check scheme
            if(!current.Children.TryGetValue(scheme, out next)) {
                return matches;
            }
            current = next;

            // check hostport
            if(current.Children.TryGetValue(hostport, out next)) {
                GetMatches(uri, next, matches, filter);
            }
            if(current.Children.TryGetValue("*", out next)) {
                GetMatches(uri, next, matches, filter);
            }
            return matches;
        }

        private void GetMatches(XUri uri, Entry current, List<T> matches, ICollection<T> filter) {
            Entry next;
            matches.AddRange(FilterMatches(current.Wildcard, filter));
            for(int i = 0; i < uri.Segments.Length; i++) {
                if(!current.Children.TryGetValue(uri.Segments[i], out next)) {
                    break;
                }
                current = next;

                // grab all wildcard matches as we descend
                matches.AddRange(FilterMatches(current.Wildcard, filter));

                // if we're on the last segment, grab the exact matches
                if(i == uri.Segments.Length - 1) {
                    matches.AddRange(FilterMatches(current.Exact, filter));
                }
            }
        }

        private IEnumerable<T> FilterMatches(IEnumerable<T> matches, ICollection<T> filter) {
            if(filter == null) {
                return matches;
            }
            List<T> filtered = new List<T>();
            foreach(T match in matches) {
                if(filter.Contains(match)) {
                    filtered.Add(match);
                }
            }
            return filtered;
        }

        /// <summary>
        /// Clear the map.
        /// </summary>
        public void Clear() {
            _root.Clear();
        }
    }
}
