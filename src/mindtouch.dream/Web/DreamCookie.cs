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
using System.Globalization;
using System.Text;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Web {
    /// <summary>
    /// Provides encapsulation for an Http cookie.
    /// </summary>
    public class DreamCookie {

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---

        /// <summary>
        /// Gets the matching cookie with the longest path from a collection of cookies.
        /// </summary>
        /// <param name="cookies">Collection of cookies.</param>
        /// <param name="name">Cookie name.</param>
        /// <returns>Matching cookie with longest path, or null if no cookies matched.</returns>
        public static DreamCookie GetCookie(List<DreamCookie> cookies, string name) {

            // TODO (steveb): consider making this an extension method

            // TODO (arnec): Should also match on domain/path as sanity check
            DreamCookie result = null;
            int maxPathLength = -1;
            foreach(DreamCookie cookie in cookies) {
                int length = cookie.Path == null ? 0 : cookie.Path.Length;
                if((cookie.Name != name) || (length <= maxPathLength)) {
                    continue;
                }
                maxPathLength = length;
                result = cookie;
            }
            return result;
        }

        /// <summary>
        /// Create a cookie collection from a cookie header.
        /// </summary>
        /// <param name="header">Http cookie header.</param>
        /// <returns>Cookie collection.</returns>
        public static List<DreamCookie> ParseCookieHeader(string header) {
            List<DreamCookie> result = new List<DreamCookie>();
            if(string.IsNullOrEmpty(header)) {
                return result;
            }
            int index = 0;
            string name;
            string value;
            if(!ParseNameValue(out name, out value, header, ref index, false)) {
                return result;
            }

            // check if we read the cookie version information
            if(string.Compare(name, "$Version", true) != 0) {

                // we read something else; let's forget that we read it
                index = 0;
            }
            while(true) {
                DreamCookie cookie = ParseCookie(header, ref index);
                if(cookie != null) {
                    result.Add(cookie);
                } else {
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Parse a collection cookies from a set cookie header.
        /// </summary>
        /// <param name="setCookieHeader">Http set cookie header.</param>
        /// <returns>Collection of cookies.</returns>
        public static List<DreamCookie> ParseSetCookieHeader(string setCookieHeader) {
            List<DreamCookie> result = new List<DreamCookie>();
            if(string.IsNullOrEmpty(setCookieHeader)) {
                return result;
            }
            int index = 0;
            DreamCookie cookie;
            while((cookie = ParseSetCookie(setCookieHeader, ref index)) != null) {
                result.Add(cookie);
            }
            return result;
        }

        /// <summary>
        /// Parse a collection of cookies from enumerable of xml serialized set cookies.
        /// </summary>
        /// <remarks>
        /// Since <see cref="XDoc"/> implements <see cref="IEnumerable{XDoc}"/>, the input to this method can be a single XDoc instance or the result
        /// of an Xpath query, in addition to a collection of <see cref="XDoc"/> instances.
        /// </remarks>
        /// <param name="setCookieElements">Enumerable of XDocs</param>
        /// <returns>Cookie collection.</returns>
        public static List<DreamCookie> ParseAllSetCookieNodes(IEnumerable<XDoc> setCookieElements) {
            List<DreamCookie> cookieCollection = new List<DreamCookie>();
            foreach(XDoc setCookie in setCookieElements) {
                cookieCollection.Add(ParseSetCookie(setCookie));
            }
            return cookieCollection;
        }

        /// <summary>
        /// Parse a single cookie from a xml serialized cookie.
        /// </summary>
        /// <param name="cookie">Xml serialized cookie.</param>
        /// <returns>New cookie instance.</returns>
        public static DreamCookie ParseSetCookie(XDoc cookie) {
            DateTime expires = cookie["expires"].AsDate ?? DateTime.MaxValue;
            int version = cookie["@version"].AsInt ?? 0;
            string comment = cookie["comment"].AsText;
            XUri commentUri = cookie["uri.comment"].AsUri;
            bool discard = cookie["@discard"].AsBool ?? false;
            bool secure = cookie["@secure"].AsBool ?? false;
            bool httpOnly = cookie["@http-only"].AsBool ?? false;
            return new DreamCookie(cookie["name"].AsText, cookie["value"].AsText, cookie["uri"].AsUri, expires, version, secure, discard, comment, commentUri, httpOnly, false);
        }

        /// <summary>
        /// Parse all cookies from an enumerable of xml serialized cookies.
        /// </summary>
        /// <remarks>
        /// Since <see cref="XDoc"/> implements <see cref="IEnumerable{XDoc}"/>, the input to this method can be a single XDoc instance or the result
        /// of an Xpath query, in addition to a collection of <see cref="XDoc"/> instances.
        /// </remarks>
        /// <param name="cookieElements">Enumerable of XDocs</param>
        /// <returns>Collection of cookies.</returns>
        public static List<DreamCookie> ParseAllCookieNodes(IEnumerable<XDoc> cookieElements) {
            List<DreamCookie> cookieCollection = new List<DreamCookie>();
            foreach(XDoc setCookie in cookieElements) {
                cookieCollection.Add(ParseSetCookie(setCookie));
            }
            return cookieCollection;
        }

        /// <summary>
        /// Parse a single cookie from a xml serialized cookie.
        /// </summary>
        /// <param name="cookie">Xml serialized cookie.</param>
        /// <returns>New cookie instance.</returns>
        public static DreamCookie ParseCookie(XDoc cookie) {
            return new DreamCookie(cookie["name"].AsText, cookie["value"].AsText, cookie["uri"].AsUri);
        }

        /// <summary>
        /// Render a collection of cookies into a cookie header.
        /// </summary>
        /// <param name="cookies">Collection of cookies.</param>
        /// <returns>Http cookie header.</returns>
        public static string RenderCookieHeader(List<DreamCookie> cookies) {
            if((cookies == null) || (cookies.Count == 0)) {
                return string.Empty;
            }
            StringBuilder result = new StringBuilder();
            result.AppendFormat("$Version=\"{0}\"", 1);
            bool first = true;
            foreach(DreamCookie cookie in cookies) {

                // NOTE (steveb): Dream 1.5 and earlier REQUIRES the $Version value to be separated by a semi-colon (;) instead of a comma (,)

                if(first) {
                    first = false;
                    result.Append("; ");
                } else {
                    result.Append(", ");
                }
                result.Append(cookie.ToCookieHeader());
            }
            return result.ToString();
        }

        /// <summary>
        /// Create a new set cookie.
        /// </summary>
        /// <param name="name">Cookie name.</param>
        /// <param name="value">Cookie value.</param>
        /// <param name="uri">Cookie Uri.</param>
        /// <returns>New cookie instance.</returns>
        public static DreamCookie NewSetCookie(string name, string value, XUri uri) {
            return NewSetCookie(name, value, uri, DateTime.MaxValue);
        }

        /// <summary>
        /// Create a new set cookie.
        /// </summary>
        /// <param name="name">Cookie name.</param>
        /// <param name="value">Cookie value.</param>
        /// <param name="uri">Cookie Uri.</param>
        /// <param name="expires">Cookie expiration.</param>
        /// <returns>New cookie instance.</returns>
        public static DreamCookie NewSetCookie(string name, string value, XUri uri, DateTime expires) {
            return NewSetCookie(name, value, uri, expires, false, null, null);
        }

        /// <summary>
        /// Create a new set cookie.
        /// </summary>
        /// <param name="name">Cookie name.</param>
        /// <param name="value">Cookie value.</param>
        /// <param name="uri">Cookie Uri.</param>
        /// <param name="expires">Cookie expiration.</param>
        /// <param name="secure"><see langword="True"/> if the cookie should only be used on https requests.</param>
        /// <param name="comment">Comment.</param>
        /// <param name="commentUri">Uri for comment.</param>
        /// <returns>New cookie instance.</returns>
        public static DreamCookie NewSetCookie(string name, string value, XUri uri, DateTime expires, bool secure, string comment, XUri commentUri) {
            return new DreamCookie(name, value, uri, expires, 1, secure, false, comment, commentUri, false, false);
        }

        /// <summary>
        /// Create a new set cookie.
        /// </summary>
        /// <param name="name">Cookie name.</param>
        /// <param name="value">Cookie value.</param>
        /// <param name="uri">Cookie Uri.</param>
        /// <param name="expires">Cookie expiration.</param>
        /// <param name="secure"><see langword="True"/> if the cookie should only be used on https requests.</param>
        /// <param name="comment">Comment.</param>
        /// <param name="commentUri">Uri for comment.</param>
        /// <param name="httpOnly"><see langword="True"/> if cookie is only accessible to the http transport, i.e. not client side scripts.</param>
        /// <returns>New cookie instance.</returns>
        public static DreamCookie NewSetCookie(string name, string value, XUri uri, DateTime expires, bool secure, string comment, XUri commentUri, bool httpOnly) {
            return new DreamCookie(name, value, uri, expires, 1, secure, false, comment, commentUri, httpOnly, false);
        }

        /// <summary>
        /// Convert a list of cookies with possibly internal Uri's to use public Uri's instead.
        /// </summary>
        /// <param name="cookies">Cookie collection.</param>
        /// <returns>Cookie collection.</returns>
        public static List<DreamCookie> ConvertToPublic(List<DreamCookie> cookies) {
            List<DreamCookie> result = new List<DreamCookie>(cookies.Count);
            foreach(DreamCookie cookie in cookies) {
                result.Add(cookie.ToPublicCookie());
            }
            return result;
        }

        /// <summary>
        /// Format <see cref="DateTime"/> in standard cookie format.
        /// </summary>
        /// <param name="date">Date.</param>
        /// <returns>Cookie datetime string.</returns>
        public static string FormatCookieDateTimeString(DateTime date) {
            return date.ToSafeUniversalTime().ToString("ddd, dd-MMM-yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture.DateTimeFormat);
        }

        #region Cookie Parsing Helpers
        private static DreamCookie ParseCookie(string text, ref int index) {
            string cookieName;
            string cookieValue;
            string domain = string.Empty;
            string path = string.Empty;
            if(!ParseNameValue(out cookieName, out cookieValue, text, ref index, true)) {
                return null;
            }
            int index2 = index;
            bool parsing = true;
            string name;
            string value;
            while(parsing && ParseNameValue(out name, out value, text, ref index2, true)) {
                switch(name) {
                case "$Path":
                    path = value;
                    break;
                case "$Domain":
                    domain = value;
                    break;
                default:

                    // unrecognized attribute; let's stop parsing
                    parsing = false;
                    index2 = index;
                    break;
                }
                index = index2;
            }
            SkipComma(text, ref index);
            XUri uri = null;
            if(!string.IsNullOrEmpty(domain) || !string.IsNullOrEmpty(path)) {
                uri = new XUri(string.Format("http://{0}{1}", domain, path));
            }
            return new DreamCookie(cookieName, cookieValue, uri);
        }

        private static DreamCookie ParseSetCookie(string text, ref int index) {
            string cookieName;
            string cookieValue;
            DateTime expires = DateTime.MaxValue;
            int version = 1;
            string domain = string.Empty;
            string path = string.Empty;
            string comment = string.Empty;
            XUri commentUri = null;
            bool discard = false;
            bool secure = false;
            bool httpOnly = false;
            if(!ParseNameValue(out cookieName, out cookieValue, text, ref index, true)) {
                return null;
            }

            string name;
            string value;
            while(ParseNameValue(out name, out value, text, ref index, true)) {
                if(StringUtil.EqualsInvariantIgnoreCase(name, "comment")) {
                    comment = value;
                } else if(StringUtil.EqualsInvariantIgnoreCase(name, "commenturl")) {
                    commentUri = new XUri(value);
                } else if(StringUtil.EqualsInvariantIgnoreCase(name, "domain")) {
                    domain = value;
                } else if(StringUtil.EqualsInvariantIgnoreCase(name, "path")) {
                    path = value;
                } else if(StringUtil.EqualsInvariantIgnoreCase(name, "max-age")) {
                    expires = DateTime.UtcNow.AddSeconds(int.Parse(value, NumberFormatInfo.InvariantInfo));
                } else if(StringUtil.EqualsInvariantIgnoreCase(name, "expires")) {
                    expires = ParseCookieDateTimeString(value);
                } else if(StringUtil.EqualsInvariantIgnoreCase(name, "port")) {

                    // TODO (steveb): why is this commented out?
                    // result.Port = value;
                } else if(StringUtil.EqualsInvariantIgnoreCase(name, "version")) {
                    version = int.Parse(value, NumberFormatInfo.InvariantInfo);
                } else if(StringUtil.EqualsInvariantIgnoreCase(name, "discard")) {
                    discard = true;
                } else if(StringUtil.EqualsInvariantIgnoreCase(name, "secure")) {
                    secure = true;
                } else if(StringUtil.EqualsInvariantIgnoreCase(name, "httponly")) {
                    httpOnly = true;
                } else {

                    // unrecognized attribute; let's skip it
                }
            }
            SkipComma(text, ref index);

            // TODO (steveb): why are we doing this?
            XUri uri = null;
            if((domain != null) || (path != null)) {
                if((domain != null) && domain.StartsWith(".")) {

                    // TODO (steveb): why are we modifying the original domain value?
                    domain = domain.Remove(0, 1);
                }

                // TODO (steveb): the produced URI always uses 'http' even when the cookie is secure, why?
                uri = new XUri(string.Format("http://{0}{1}", domain, path));
            }
            return new DreamCookie(cookieName, cookieValue, uri, expires, version, secure, discard, comment, commentUri, httpOnly, false);
        }

        private static DateTime ParseCookieDateTimeString(string cookieExpires) {
            DateTime ret;
            if(!DateTimeUtil.TryParseExactInvariant(cookieExpires, "ddd, dd-MMM-yyyy HH:mm:ss 'GMT'", out ret)) {
                ret = DateTime.MaxValue;
            }
            return ret;
        }

        private static bool ParseNameValue(out string name, out string value, string text, ref int index, bool useCommaAsSeparator) {
            value = null;
            SkipWhitespace(text, ref index);
            if(!ParseWord(out name, text, ref index)) {
                return false;
            }
            SkipWhitespace(text, ref index);
            if(MatchToken("=", text, ref index)) {
                bool quoted;
                if(!ParseValue(out value, out quoted, text, ref index, useCommaAsSeparator)) {
                    return false;
                }

                // check if we stopped parsing because the value contained a ','
                if(!quoted && useCommaAsSeparator && (index < text.Length) && (text[index] == ',')) {

                    // only accept ',' as separator if we can find an '=' sign following it
                    int next = text.IndexOfAny(new[] { '=', ';' }, index);
                    if(next < 0) {
                        value += text.Substring(index);
                        index = text.Length;
                    } else if((next > 0) && (text[next] == ';')) {
                        value += text.Substring(index, next - index);
                        index = next;
                    }
                }
            }
            SkipWhitespace(text, ref index);
            if(useCommaAsSeparator) {
                SkipSemiColon(text, ref index);
            } else {
                SkipDelimiter(text, ref index);
            }
            return true;
        }

        private static bool SkipWhitespace(string text, ref int index) {

            // skip whitespace
            while((index < text.Length) && char.IsWhiteSpace(text[index])) {
                ++index;
            }
            return true;
        }

        private static bool SkipSemiColon(string text, ref int index) {

            // skip whitespace
            if((index < text.Length) && (text[index] == ';')) {
                ++index;
            }
            return true;
        }

        private static bool SkipComma(string text, ref int index) {

            // skip whitespace
            if((index < text.Length) && (text[index] == ',')) {
                ++index;
            }
            return true;
        }

        private static bool SkipDelimiter(string text, ref int index) {

            // skip whitespace
            if((index < text.Length) && ((text[index] == ',') || (text[index] == ';'))) {
                ++index;
            }
            return true;
        }

        private static bool MatchToken(string token, string text, ref int index) {
            if(string.Compare(text, index, token, 0, token.Length) == 0) {
                index += token.Length;
                return true;
            }
            return false;
        }

        private static bool ParseWord(out string word, string text, ref int index) {
            word = null;
            if(index >= text.Length) {
                return false;
            }

            // check if we're parsing a quoted string
            if(text[index] == '"') {
                ++index;
                int last;
                for(last = index; (last < text.Length) && (text[last] != '"'); ++last) {

                    // check if the next character is escaped
                    if(text[last] == '\\') {
                        ++last;
                        if(last == text.Length) {
                            break;
                        }
                    }
                }
                if(last == text.Length) {
                    word = text.Substring(index);
                    index = last;
                } else {
                    word = text.Substring(index, last - index);
                    index = last + 1;
                }
                word = word.UnescapeString();
                return true;
            } else {

                // parse an alphanumeric token
                int last;
                for(last = index; (last < text.Length) && IsTokenChar(text[last]); ++last) { }
                if(last == index) {
                    return false;
                }
                word = text.Substring(index, last - index);
                index = last;
                return true;
            }
        }

        private static bool ParseValue(out string word, out bool quoted, string text, ref int index, bool useCommaAsSeparator) {
            word = null;
            quoted = false;
            if(index >= text.Length) {
                return false;
            }

            // check if we're parsing a quoted string
            if(text[index] == '"') {
                quoted = true;
                ++index;
                int last;
                for(last = index; (last < text.Length) && (text[last] != '"'); ++last) {

                    // check if the next character is escaped
                    if(text[last] == '\\') {
                        ++last;
                        if(last == text.Length) {
                            break;
                        }
                    }
                }
                if(last == text.Length) {
                    word = text.Substring(index);
                    index = last;
                } else {
                    word = text.Substring(index, last - index);
                    index = last + 1;
                }
                word = word.UnescapeString();
                return true;
            } else {

                // parse an alphanumeric token
                int last;
                for(last = index; (last < text.Length) && (text[last] != ';' && (!useCommaAsSeparator || (text[last] != ','))); ++last) { }

                word = text.Substring(index, last - index).Trim();
                index = last;
                return true;
            }
        }

        private static bool IsTokenChar(char c) {
            return ((c >= 'A') && (c <= 'Z')) || ((c >= 'a') && (c <= 'z')) ||
                   ((c > 32) && (c < 127) && (c != '(') && (c != ')') && (c != '<') &&
                    (c != '<') && (c != '>') && (c != '@') && (c != ',') && (c != ';') &&
                    (c != ':') && (c != '\\') && (c != '"') && (c != '/') && (c != '[') &&
                    (c != ']') && (c != '?') && (c != '=') && (c != '{') && (c != '}'));
        }
        #endregion

        //--- Fields ---
        private readonly XUri _uri;
        private readonly XUri _publicUri;
        private readonly XUri _localMachineUri;
        private readonly DateTime _expires;
        private readonly int _version;
        private readonly string _name;
        private readonly bool _secure;
        private readonly string _value;
        private readonly string _comment;
        private readonly XUri _commentUri;
        private readonly bool _discard;
        private readonly bool _httpOnly;

        //--- Constructors ---

        /// <summary>
        /// Create new instance.
        /// </summary>
        /// <param name="name">Cookie name.</param>
        /// <param name="value">Cookie value.</param>
        /// <param name="uri">Cookie Uri.</param>
        public DreamCookie(string name, string value, XUri uri) : this(name, value, uri, DateTime.MaxValue, 0) { }

        private DreamCookie(string name, string value, XUri uri, DateTime expires, int version) : this(name, value, uri, expires, version, false, false, null, null, false, false) { }

        private DreamCookie(string name, string value, XUri uri, DateTime expires, int version, bool secure, bool discard, string comment, XUri commentUri, bool httpOnly, bool skipContextDiscovery) {
            if(string.IsNullOrEmpty(name)) {
                throw new ArgumentException("Name cannot be empty");
            }
            _name = name;
            _value = value;
            if(uri != null) {
                _uri = uri.WithoutQuery().WithoutCredentials().WithoutFragment().AsLocalUri();
                if(!skipContextDiscovery) {
                    DreamContext dc = DreamContext.CurrentOrNull;
                    if(dc != null) {
                        _publicUri = dc.PublicUri;
                        _localMachineUri = dc.Env.LocalMachineUri;
                    }
                }
            }

            // auto-convert very old expiration dates to max since they are most likely bogus
            if(expires.Year < 2000) {
                expires = DateTime.MaxValue;
            }
            if(expires != DateTime.MaxValue) {
                expires = expires.ToUniversalTime();

                // need to trim milliseconds of the passed in date
                expires = new DateTime(expires.Year, expires.Month, expires.Day, expires.Hour, expires.Minute, expires.Second, 0, DateTimeKind.Utc).ToUniversalTime();
            }

            // initialize cookie
            _expires = expires;
            _version = version;
            _secure = secure;
            _discard = discard;
            _comment = comment;
            _commentUri = commentUri;
            _httpOnly = httpOnly;
        }

        //--- Properties ---

        /// <summary>
        /// Cookie Uri (derived from Domain and Path).
        /// </summary>
        public XUri Uri {
            get { return _uri; }
        }

        /// <summary>
        /// Cookie domain.
        /// </summary>
        public string Domain {
            get { return Uri == null ? null : Uri.HostPort; }
        }
        
        /// <summary>
        /// Cookie Path.
        /// </summary>
        public string Path {
            get { return Uri == null ? null : Uri.Path; }
        }

        /// <summary>
        /// Cookie expiration
        /// </summary>
        public DateTime Expires {
            get { return _expires; }
        }

        /// <summary>
        /// Cookie version.
        /// </summary>
        public int Version {
            get { return _version; }
        }

        /// <summary>
        /// Cookie value.
        /// </summary>
        public string Value {
            get { return _value; }
        }

        /// <summary>
        /// Cookie name.
        /// </summary>
        public string Name {
            get { return _name; }
        }

        /// <summary>
        /// <see langword="True"/> if the cookie is already exipired.
        /// </summary>
        public bool Expired {
            get { return DateTime.UtcNow > Expires; }
        }

        /// <summary>
        /// <see langword="True"/> if the cookie is used on Https only.
        /// </summary>
        public bool Secure {
            get { return _secure; }
        }

        /// <summary>
        /// <see langword="True"/> if the cookie is to be discarded.
        /// </summary>
        public bool Discard {
            get { return _discard; }
        }

        /// <summary>
        /// Cookie comment.
        /// </summary>
        public string Comment {
            get { return _comment; }
        }

        /// <summary>
        /// Cookie comment uri.
        /// </summary>
        public XUri CommentUri {
            get { return _commentUri; }
        }

        /// <summary>
        /// <see langword="True"/> if cookie is only accessible to the http transport, i.e. not client side scripts.
        /// </summary>
        public bool HttpOnly {
            get { return _httpOnly; }
        }

        /// <summary>
        /// Serialize the cookie as a set cookie xml document.
        /// </summary>
        public XDoc AsSetCookieDocument {
            get {
                XDoc result = new XDoc("set-cookie")
                    .Attr("version", 1)
                    .Elem("name", Name)
                    .Elem("uri", Uri)
                    .Elem("value", Value);
                if(Expires < DateTime.MaxValue) {
                    result.Elem("expires", Expires);
                }
                if(!string.IsNullOrEmpty(Comment)) {
                    result.Elem("comment", Comment);
                }
                if(CommentUri != null) {
                    result.Elem("uri.comment", CommentUri.ToString());
                }
                if(Discard) {
                    result.Attr("discard", true);
                }
                if(Secure) {
                    result.Attr("secure", true);
                }
                return result;
            }
        }

        /// <summary>
        /// Serialize the cookie as a cookie xml document.
        /// </summary>
        public XDoc AsCookieDocument {
            get { return new XDoc("cookie").Elem("name", Name).Elem("uri", Uri).Elem("value", Value); }
        }

        //--- Methods ---
        /// <summary>
        /// Compare <see cref="DreamCookie"/> instances for identical content.
        /// </summary>
        /// <param name="cookie">Cookie to compare.</param>
        /// <returns><see langword="True"/> if the cookies are the same.</returns>
        public bool Equals(DreamCookie cookie) {
            return ToString().Equals(cookie.ToString());
        }

        /// <summary>
        /// Comparison override for Cookies.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            if(obj is DreamCookie) {
                return Equals(obj as DreamCookie);
            }
            return base.Equals(obj);
        }

        /// <summary>
        /// GetHashCode override to ensure that cookies with the same content generate the same hashcode.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Human readable version of the cookie contents (not suitable as headers).
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return string.Format("{0}@{1}{2}={3}", Name, Domain, Path, Value);
        }

        /// <summary>
        /// Create new cookie based on current instance with host/port.
        /// </summary>
        /// <param name="hostPort">Host/port to use for new instance.</param>
        /// <returns>New cookie.</returns>
        public DreamCookie WithHostPort(string hostPort) {
            string scheme = _uri == null ? "local" : _uri.Scheme;
            string path = _uri == null ? string.Empty : _uri.Path;
            return new DreamCookie(Name, Value, new XUri(string.Format("{0}://{1}{2}", scheme, hostPort, path)), Expires, Version, Secure, Discard, Comment, CommentUri, HttpOnly, false);
        }

        /// <summary>
        /// Create new cookie based on current instance with path.
        /// </summary>
        /// <param name="path">Path to use for new instance.</param>
        /// <returns>New Cookie.</returns>
        public DreamCookie WithPath(string path) {
            string scheme = _uri == null ? "local" : _uri.Scheme;
            string hostPort = _uri == null ? string.Empty : _uri.Path;
            return new DreamCookie(Name, Value, new XUri(string.Format("{0}://{1}{2}", scheme, hostPort, path)), Expires, Version, Secure, Discard, Comment, CommentUri, HttpOnly, false);
        }

        /// <summary>
        /// Create new cookie based on current instance with expiration.
        /// </summary>
        /// <param name="expires">Cookie expiration.</param>
        /// <returns>New Cookie.</returns>
        public DreamCookie WithExpiration(DateTime expires) {
            return new DreamCookie(Name, Value, Uri, expires, Version, Secure, Discard, Comment, CommentUri, HttpOnly, false);
        }

        /// <summary>
        /// Create new cookie based on current instance with discard flag.
        /// </summary>
        /// <param name="discard"><see langword="True"/> if the cookie should be discarded.</param>
        /// <returns></returns>
        public DreamCookie WithDiscard(bool discard) {
            return new DreamCookie(Name, Value, Uri, Expires, Version, Secure, discard, Comment, CommentUri, HttpOnly, false);
        }

        /// <summary>
        /// Create an Http cookie header from the current instance.
        /// </summary>
        /// <returns>Http cookie header string.</returns>
        public string ToCookieHeader() {

            // Note (arnec): We always translate cookies to the public form before serializing
            XUri uri = GetPublicUri();
            StringBuilder result = new StringBuilder();
            result.AppendFormat("{0}=\"{1}\"", Name, Value.EscapeString());
            if(!string.IsNullOrEmpty(Path)) {
                result.AppendFormat("; $Path=\"{0}\"", uri.Path);
            }
            if(!string.IsNullOrEmpty(Domain)) {
                result.AppendFormat("; $Domain=\"{0}\"", uri.HostPort);
            }
            return result.ToString();
        }

        /// <summary>
        /// Create an Http set-cookie header from the current instance.
        /// </summary>
        /// <returns>Http set-cookie header string.</returns>
        public string ToSetCookieHeader() {

            // Note (arnec): We always translate cookies to the public form before serializing
            XUri uri = GetPublicUri();
            StringBuilder result = new StringBuilder(1024);
            result.AppendFormat("{0}=\"{1}\"", Name, Value.EscapeString());
            if(!string.IsNullOrEmpty(Comment)) {
                result.Append("; Comment=\"" + Comment.EscapeString() + "\"");
            }
            if(CommentUri != null) {
                result.Append("; CommentURL=\"" + CommentUri.ToString().EscapeString() + "\"");
            }
            if(Discard) {
                result.Append(result + "; Discard");
            }

            // Note (arnec): domain names require a dot prefix (so that a cookie can't be set to .com)
            //               while hostnames (including localhost) should force Domain to be omitted
            //               so that the receiver can appropriately set it
            if(!string.IsNullOrEmpty(Domain) && Domain.Contains(".") && !Domain.StartsWith(".")) {
                string domain = uri.HostIsIp ? uri.Host : "." + uri.Host;
                result.AppendFormat("; Domain={0}", domain);
            }
            if(Expires < DateTime.MaxValue) {
                result.Append("; Expires=" + FormatCookieDateTimeString(Expires));
            }

            // NOTE (arnec): Do not remove, but re-evaluate, since port is a problem with the standard.
            //if(!string.IsNullOrEmpty(Port)) {
            //    result.Append("; Port=\"" + Port + "\"");
            //}
            if(Version > 1) {
                result.Append("; Version=\"" + Version.ToString(NumberFormatInfo.InvariantInfo) + "\"");
            } else if(Version > 0) {
                result.Append("; Version=" + Version.ToString(NumberFormatInfo.InvariantInfo));
            }
            if(!string.IsNullOrEmpty(Path)) {
                result.Append("; Path=" + uri.Path);
            }
            if(Secure) {
                result.Append("; Secure");
            }
            if(HttpOnly) {
                result.Append("; HttpOnly");
            }
            return result.ToString();
        }

        /// <summary>
        /// Create new cookie with public Uri, in case the current instance is using 
        /// </summary>
        /// <remarks>
        /// This call uses the public uri captured at cookie creation time, so can be used independently of request context.
        /// </remarks>
        /// <returns></returns>
        public DreamCookie ToPublicCookie() {
            return new DreamCookie(Name, Value, GetPublicUri(), Expires, Version, Secure, Discard, Comment, CommentUri, HttpOnly, true);
        }

        private XUri GetPublicUri() {

            // Note (arnec): This is not the same as AsPublicUri, since we do this independently of a DreamContext
            XUri uri = Uri;
            if((uri != null) && (_publicUri != null) && (_localMachineUri != null) && uri.HasPrefix(_localMachineUri)) {
                uri = uri.ChangePrefix(_localMachineUri, _publicUri);
            }
            return uri;
        }
    }
}