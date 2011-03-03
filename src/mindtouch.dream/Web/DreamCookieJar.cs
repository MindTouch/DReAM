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
using MindTouch.Dream;

namespace MindTouch.Web {
    /// <summary>
    /// Provides a hierarchical <see cref="DreamCookie"/> container.
    /// </summary>
    public class DreamCookieJar {

        //--- Fields ---
        private List<DreamCookie> _cookies;
        private Dictionary<string, DreamCookieJar> _jars;

        //--- Properties ---

        /// <summary>
        /// Total cookies in the jar.
        /// </summary>
        public int Count {
            get {
                int result = 0;
                if(_jars != null) {
                    foreach(DreamCookieJar jar in _jars.Values) {
                        result += jar.Count;
                    }
                }
                if(_cookies != null) {
                    result += _cookies.Count;
                }
                return result;
            }
        }

        /// <summary>
        /// <see langword="True"/> if there are no cookies in the jar.
        /// </summary>
        public bool IsEmpty {
            get {
                return ((_jars == null) || (_jars.Count == 0)) && ((_cookies == null) || (_cookies.Count == 0));
            }
        }

        //--- Methods ---

        /// <summary>
        /// Clear all cookies from the jar.
        /// </summary>
        public void Clear() {
            _cookies = null;
            _jars = null;
        }

        /// <summary>
        /// Update the jar with a cookie.
        /// </summary>
        /// <param name="cookie">Cookie to store.</param>
        /// <param name="uri">Uri this cookie applies to.</param>
        public void Update(DreamCookie cookie, XUri uri) {
            List<DreamCookie> list = new List<DreamCookie>();
            list.Add(cookie);
            Update(list, uri);
        }

        /// <summary>
        /// Update the jar with a collection cookies.
        /// </summary>
        /// <param name="collection">List of cookies to store.</param>
        /// <param name="uri">Uri cookies apply to.</param>
        public void Update(List<DreamCookie> collection, XUri uri) {
            if(collection == null) {
                throw new ArgumentNullException("collection");
            }

            // process all cookies
            foreach(DreamCookie c in collection) {
                DreamCookie cookie = c;
                if(!string.IsNullOrEmpty(cookie.Name)) {
                    string[] segments = null;
                    if(uri != null) {

                        // set default domain if needed
                        if(string.IsNullOrEmpty(cookie.Domain)) {
                            cookie = cookie.WithHostPort(uri.HostPort);
                        } else if(!StringUtil.EqualsInvariantIgnoreCase(cookie.Domain, uri.HostPort)) {

                            // domain doesn't match, ignore cookie
                            continue;
                        }

                        // set default path if needed
                        if(string.IsNullOrEmpty(cookie.Path)) {
                            cookie = cookie.WithPath(uri.Path);
                            segments = uri.Segments;
                        } else {
                            segments = cookie.Uri == null ? new string[0] : cookie.Uri.Segments;
                            if(!uri.PathStartsWith(segments)) {

                                // path doesn't match ignore cookie
                                continue;
                            }
                        }
                    }
                    if(!string.IsNullOrEmpty(cookie.Path) && !string.IsNullOrEmpty(cookie.Domain)) {
                        if(segments == null) {
                            segments = cookie.Uri == null ? new string[0] : cookie.Uri.Segments;
                        }
                        if(cookie.Expired) {
                            Delete(cookie, segments, 0);
                        } else {
                            Insert(cookie, segments, 0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve all cookies that apply to a Uri.
        /// </summary>
        /// <param name="uri">Uri to match.</param>
        /// <returns>List of cookies.</returns>
        public List<DreamCookie> Fetch(XUri uri) {
            if(uri == null) {
                throw new ArgumentNullException("uri");
            }
            List<DreamCookie> result = new List<DreamCookie>();
            Fetch(uri, 0, result);
            XUri localUri = uri.AsLocalUri();
            if(localUri != uri) {
                Fetch(localUri, 0, result);
            }
            return result;
        }

        private void Insert(DreamCookie updatedCookie, string[] segments, int depth) {

            // find leaf node
            if(depth < segments.Length) {
                if(_jars == null) {
                    _jars = new Dictionary<string, DreamCookieJar>(StringComparer.OrdinalIgnoreCase);
                }
                DreamCookieJar subjar;
                if(!_jars.TryGetValue(segments[depth], out subjar)) {
                    subjar = new DreamCookieJar();
                    _jars.Add(segments[depth], subjar);
                }
                subjar.Insert(updatedCookie, segments, depth + 1);
            } else {
                if(_cookies == null) {
                    _cookies = new List<DreamCookie>();
                }
                List<DreamCookie> expired = new List<DreamCookie>();
                for(int i = 0; i < _cookies.Count; ++i) {
                    DreamCookie cookie = _cookies[i];

                    // check if cookie is expired; if so, remove it
                    if(cookie.Expired) {
                        expired.Add(cookie);
                        continue;
                    }

                    // TODO (steveb): we need to add support for '.' prefixes on the domain name

                    // check if cookie matches the expired cookie
                    if(StringUtil.EqualsInvariantIgnoreCase(cookie.Domain, updatedCookie.Domain) && StringUtil.EqualsInvariantIgnoreCase(cookie.Name, updatedCookie.Name) && (cookie.Secure == updatedCookie.Secure)) {
                        _cookies[i] = updatedCookie;
                        return;
                    }
                }
                foreach(DreamCookie cookie in expired) {
                    _cookies.Remove(cookie);
                }
                _cookies.Add(updatedCookie);
            }
        }

        private void Delete(DreamCookie expiredCookie, string[] segments, int depth) {

            // find leaf node
            if(depth < segments.Length) {
                if(_jars != null) {
                    DreamCookieJar subjar;
                    if(_jars.TryGetValue(segments[depth], out subjar)) {
                        subjar.Delete(expiredCookie, segments, depth + 1);
                        if(subjar.IsEmpty) {
                            _jars.Remove(segments[depth]);
                        }
                    }
                }
            } else if(_cookies != null) {
                List<DreamCookie> expired = new List<DreamCookie>();
                foreach(DreamCookie cookie in _cookies) {

                    // check if cookie is expired; if so, remove it
                    if(cookie.Expired) {
                        expired.Add(cookie);
                        continue;
                    }

                    // TODO (steveb): we need to add support for '.' prefixes on the domain name

                    // check if cookie matches the expired cookie
                    if(StringUtil.EqualsInvariantIgnoreCase(cookie.Domain, expiredCookie.Domain) && StringUtil.EqualsInvariantIgnoreCase(cookie.Name, expiredCookie.Name) && (cookie.Secure == expiredCookie.Secure)) {
                        expired.Add(cookie);
                        continue;
                    }
                }
                foreach(DreamCookie cookie in expired) {
                    _cookies.Remove(cookie);
                }
            }
        }

        private void Fetch(XUri uri, int depth, List<DreamCookie> result) {

            // if available, fetch cookies from deeper in the path
            if((depth < uri.Segments.Length) && (_jars != null)) {
                DreamCookieJar subjar;
                if(_jars.TryGetValue(uri.Segments[depth], out subjar)) {
                    subjar.Fetch(uri, depth + 1, result);
                }
            }

            // collect all cookies that are valid and apply to this uri
            if(_cookies != null) {
                List<DreamCookie> expired = new List<DreamCookie>();
                foreach(DreamCookie cookie in _cookies) {

                    // check if cookie is expired; if so, remove it
                    if(cookie.Expired) {
                        expired.Add(cookie);
                        continue;
                    }

                    // TODO (steveb): we need to add support for '.' prefixes on the domain name

                    // check if cookie matches the host and uri
                    if(StringUtil.EqualsInvariantIgnoreCase(cookie.Domain, uri.HostPort) && (!cookie.Secure || (cookie.Secure && StringUtil.EqualsInvariantIgnoreCase(uri.Scheme, "https")))) {
                        result.Add(cookie);
                    }
                }
                foreach(DreamCookie cookie in expired) {
                    _cookies.Remove(cookie);
                }
            }
        }
    }
}