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

// TODO (steveb): should we enable this again? (http://youtrack.developer.mindtouch.com/issue/MT-9135)
//#define XURI_USE_NAMETABLE

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.IO;

namespace MindTouch.Dream {

    /// <summary>
    /// Uri Path string encoding format
    /// </summary>
    public enum UriPathFormat {

        /// <summary>
        /// Leave path segment as is.
        /// </summary>
        Original,

        /// <summary>
        /// Uri decode path segment.
        /// </summary>
        Decoded,

        /// <summary>
        /// Normalize encoding.
        /// </summary>
        Normalized
    }

    /// <summary>
    /// Uri encoding options.
    /// </summary>
    public enum UriEncoding {

        /// <summary>
        /// Use only default encoding.
        /// </summary>
        Default,

        /// <summary>
        /// Perform additional encoding for <see cref="XUri.UserInfo"/>
        /// </summary>
        UserInfo,

        /// <summary>
        /// Perform additional encoding for <see cref="XUri.Segments"/>
        /// </summary>
        Segment,

        /// <summary>
        /// Perform additional encoding for <see cref="XUri.Query"/>
        /// </summary>
        Query,

        /// <summary>
        /// Perform additionalencoding for <see cref="XUri.Fragment"/>
        /// </summary>
        Fragment
    }

    // TODO (steveb): implement XUriTemplate/XUriPattern (see http://youtrack.developer.mindtouch.com/issue/MT-9660)
    // {scheme}://{host}/{path-param};[{segment-param}]/[{optional-path-param};{segment-param}}]//{segment-list}?query-arg={query-param}&[optional-query-arg={query-param}]

    /// <summary>
    /// Encapsulation of a Uniform Resource Identifier as an immutable class with a fluent interface for modification.
    /// </summary>
    [Serializable]
    public sealed class XUri : ISerializable {

        // NOTE (steveb): XUri parses absolute URIs based on RFC3986 (http://www.ietf.org/rfc/rfc3986.txt), with the addition of ^, |, [ and ] as a valid character in segments, queries, and fragments; and \ as valid segment separator

        //--- Constants ---

        /// <summary>
        /// Regular expression used to parse a full Uri string.
        /// </summary>
        public const string URI_REGEX = @"(?<scheme>" + SCHEME_REGEX + @")://(?<userinfo>" + USERINFO_REGEX + @"@)?(?<host>" + HOST_REGEX + @")(?<port>:[\d]*)?(?<path>([/\\]" + SEGMENT_REGEX + @")*)(?<query>\?" + QUERY_REGEX + @")?(?<fragment>#" + FRAGMENT_REGEX + @")?";

        /// <summary>
        /// Regular expression to match Uri scheme.
        /// </summary>
        public const string SCHEME_REGEX = @"[a-zA-Z][\w+-\.]*";

        /// <summary>
        /// Regular expression to match Uri User Info.
        /// </summary>
        public const string USERINFO_REGEX = @"[\w-\._~!\$&'\(\)\*\+,;=%:]*";

        /// <summary>
        /// Regular expression to match Uri host.
        /// </summary>
        public const string HOST_REGEX = @"((\[[a-fA-F\d:\.]*(%.+)?\])|([\w-\._~%!\$&'\(\)\*\+,;=]*))";

        /// <summary>
        /// Regular expression for matching path segments.
        /// </summary>
        public const string SEGMENT_REGEX = @"[\w-\._~%!\$&'\(\)\*\+,;=:@\^\[\]]*";

        /// <summary>
        /// Regular expression for matching query name/value pairs.
        /// </summary>
        public const string QUERY_REGEX = @"[\w-\._~%!\$&'\(\)\*\+,;=:@\^/\?|\[\]]*";

        /// <summary>
        /// Regular expression for matching fragment elements.
        /// </summary>
        public const string FRAGMENT_REGEX = @"[\w-\._~%!\$&'\(\)\*\+,;=:@\^/\?|\[\]#]*";

        /// <summary>
        /// An empty string array.
        /// </summary>
        public static readonly string[] EMPTY_ARRAY = new string[0];

        /// <summary>
        /// Invariant string comparer (using <see cref="StringComparer.Ordinal"/> by default).
        /// </summary>
        public static StringComparer INVARIANT = StringComparer.Ordinal;

        /// <summary>
        /// Invariant, case-insensitive string comparer (using <see cref="StringComparer.OrdinalIgnoreCase"/> by default).
        /// </summary>
        public static StringComparer INVARIANT_IGNORE_CASE = StringComparer.OrdinalIgnoreCase;

        private const string UP_SEGMENT = "..";
        private static readonly int HTTP_HASHCODE = StringComparer.OrdinalIgnoreCase.GetHashCode("http");
        private static readonly int HTTPS_HASHCODE = StringComparer.OrdinalIgnoreCase.GetHashCode("https");
        private static readonly int LOCAL_HASHCODE = StringComparer.OrdinalIgnoreCase.GetHashCode("local");
        private static readonly int FTP_HASHCODE = StringComparer.OrdinalIgnoreCase.GetHashCode("ftp");
        private static readonly char[] SLASHES = new char[] { '/', '\\' };

        //--- Class Fields ---

        /// <summary>
        /// XUri for localhost.
        /// </summary>
        public static readonly XUri Localhost;

        private static readonly Regex _uriRegex = new Regex(@"^" + URI_REGEX + @"$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _schemeRegex = new Regex(SCHEME_REGEX, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _hostRegex = new Regex(HOST_REGEX, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _segmentRegex = new Regex(@"^/*" + SEGMENT_REGEX + @"$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _ipRegex = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        //--- Class Constructor ---
        static XUri() {

            // create localhost uri
            Localhost = new XUri("http", null, null, "localhost", 80, true, null, false, null, null, true);
        }

        //--- Class Operators ---

        /// <summary>
        /// Equality operator overload for Uri comparison.
        /// </summary>
        /// <param name="left">Left Uri.</param>
        /// <param name="right">Right Uri.</param>
        /// <returns><see langword="True"/> if left and right represent the same Uri.</returns>
        public static bool operator ==(XUri left, XUri right) {
            if(object.ReferenceEquals(left, right)) {
                return true;
            }
            if(object.ReferenceEquals(left, null) || object.ReferenceEquals(right, null)) {
                return false;
            }
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator overload for Uri comparison.
        /// </summary>
        /// <param name="left">Left Uri.</param>
        /// <param name="right">Right Uri.</param>
        /// <returns><see langword="True"/> if left and right do not represent the same Uri.</returns>
        public static bool operator !=(XUri left, XUri right) {
            return !(left == right);
        }

        /// <summary>
        /// Implicit conversion operator to convert <see cref="Uri"/> into an <see cref="XUri"/>.
        /// </summary>
        /// <param name="uri">Uri to convert.</param>
        /// <returns>New XUri instance.</returns>
        public static implicit operator Uri(XUri uri) {
            return (uri != null) ? uri.ToUri() : null;
        }

        //--- Class Methods ---

        /// <summary>
        /// Try to parse a string into a valid Uri.
        /// </summary>
        /// <param name="text">Uri string.</param>
        /// <returns>New XUri instance or null.</returns>
        public static XUri TryParse(string text) {
            XUri uri;
            TryParse(text, out uri);
            return uri;
        }

        /// <summary>
        /// Try to parse a string into a valid Uri.
        /// </summary>
        /// <param name="text">Uri string.</param>
        /// <param name="uri">Output for parsed Uri.</param>
        /// <returns><see langword="True"/> if the text was successfully parsed.</returns>
        public static bool TryParse(string text, out XUri uri) {
            string scheme;
            string user;
            string password;
            string host;
            int port;
            bool usesDefaultPort;
            string[] segments;
            bool trailingSlash;
            KeyValuePair<string, string>[] @params;
            string fragment;
            if(!TryParse(text, out scheme, out user, out password, out host, out port, out usesDefaultPort, out segments, out trailingSlash, out @params, out fragment)) {
                uri = null;
                return false;
            }
            uri = new XUri(scheme, user, password, host, port, null, segments, trailingSlash, @params, fragment, true);
            return true;
        }

        /// <summary>
        /// Extract query parameters as key/value pairs.
        /// </summary>
        /// <param name="query">Query string.</param>
        /// <returns>Array of key/value pairs.</returns>
        public static KeyValuePair<string, string>[] ParseParamsAsPairs(string query) {

            // decode query string
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            int len = (query != null) ? query.Length : 0;
            for(int current = 0; current < len; current++) {
                int start = current;

                // check if string contains '='
                int equalIndex = -1;
                for(; current < len; ++current) {
                    if((equalIndex < 0) && (query[current] == '=')) {
                        equalIndex = current;
                    } else if(query[current] == '&') {
                        break;
                    }
                }

                // extract (name,value) pair
                string name = null;
                string value = null;
                if(equalIndex >= 0) {
                    name = query.Substring(start, equalIndex - start);
                    value = query.Substring(equalIndex + 1, (current - equalIndex) - 1);
                } else {
                    name = query.Substring(start, current - start);
                    value = null;
                }

                // decode the (name ,value) pair
                name = Decode(name);
                value = Decode(value);

                // add entry
                result.Add(new KeyValuePair<string, string>(name, value));

                // check if we're at the end and we have a trailing '&'
                if((current == (len - 1)) && (query[current] == '&')) {

                    // ignore it
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Parse a path into segments.
        /// </summary>
        /// <param name="path">Path string to parse.</param>
        /// <param name="relative"><see langword="True"/> if the path is a relative path.</param>
        /// <param name="segments">Output for an array of path segments.</param>
        /// <param name="trailingSlash">Output of <see langword="True"/> if the path had a trailing slash.</param>
        public static void ParsePath(string path, bool relative, out string[] segments, out bool trailingSlash) {
            if(!string.IsNullOrEmpty(path)) {
                List<string> result = new List<string>(path.Split(SLASHES));

                // remove leading empty entry (always present since every path begins with '/')
                if(!relative && (result.Count > 0) && (result[0].Length == 0)) {
                    result.RemoveAt(0);
                }

                // check if path ended with '/'
                trailingSlash = ((result.Count > 0) && (result[result.Count - 1].Length == 0));
                if(trailingSlash) {
                    result.RemoveAt(result.Count - 1);
                }

                // process empty segments and parse segment parameters
                for(int i = result.Count - 1; i >= 0; --i) {
                    string segment = result[i];

                    // replace empty entry with '/'; this enables proper processing of URIs like http://localhost/foo//bar -> { "foo", "/bar" }
                    if(segment.Length == 0) {
                        if((i + 1) < result.Count) {
                            result[i] = "/" + result[i + 1];
                            result.RemoveAt(i + 1);
                        } else {
                            trailingSlash = false;
                            result[i] = "/";
                        }
                    } else {
                        result[i] = segment;
                    }
                }
                segments = result.ToArray();
            } else {
                segments = EMPTY_ARRAY;
                trailingSlash = false;
            }
        }

        /// <summary>
        /// Render an parameter key/value pairs into a query string.
        /// </summary>
        /// <param name="params">Array of key/value pairs.</param>
        /// <returns>Query string.</returns>
        public static string RenderParams(KeyValuePair<string, string>[] @params) {
            if(@params == null) {
                return null;
            }
            StringBuilder result = new StringBuilder();
            if(@params.Length > 0) {
                bool first = true;
                foreach(KeyValuePair<string, string> pair in @params) {
                    if(!first) {
                        result.Append("&");
                    }
                    first = false;
                    result.Append(EncodeQuery(pair.Key));
                    if(pair.Value != null) {
                        result.Append("=");
                        result.Append(EncodeQuery(pair.Value));
                    }
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Url decode a string.
        /// </summary>
        /// <param name="text">Input text.</param>
        /// <returns>Decoded text.</returns>
        public static string Decode(string text) {
            string result = UrlDecode(text);
            return result;
        }

        /// <summary>
        /// Double decode a string.
        /// </summary>
        /// <param name="text">Input text.</param>
        /// <returns>Decoded text.</returns>
        public static string DoubleDecode(string text) {
            return Decode(Decode(text));
        }

        /// <summary>
        /// Uri encode a string.
        /// </summary>
        /// <param name="text">Input text.</param>
        /// <param name="level">Encoding level.</param>
        /// <returns>Encoded string.</returns>
        public static string Encode(string text, UriEncoding level) {
            if(string.IsNullOrEmpty(text)) {
                return text;
            }
            byte[] original = Encoding.UTF8.GetBytes(text);

            // count how many characters are affected by the encoding
            int charsToReplace = 0;
            int charsToEncode = 0;
            int length = original.Length;
            for(int i = 0; i < length; i++) {
                var ch = (char)original[i];
                if(ch == ' ') {
                    charsToReplace++;
                } else if(!IsValidCharInUri(ch, level)) {
                    charsToEncode++;
                }
            }

            // check if any characters are affected
            if((charsToReplace == 0) && (charsToEncode == 0)) {
                return text;
            }

            // copy, replace, and encode characters
            var encoded = new byte[length + (charsToEncode * 2)];
            int index = 0;
            for(int j = 0; j < length; j++) {
                byte asciiByte = original[j];
                char asciiChar = (char)asciiByte;
                if(IsValidCharInUri(asciiChar, level)) {
                    encoded[index++] = asciiByte;
                } else if(asciiChar == ' ') {

                    // replace ' ' with '+'
                    encoded[index++] = 0x2b; // '+'
                } else {

                    // replace char with '%' + code
                    encoded[index++] = 0x25; // '%'
                    encoded[index++] = (byte)StringUtil.IntToHexChar((asciiByte >> 4) & 15);
                    encoded[index++] = (byte)StringUtil.IntToHexChar(asciiByte & 15);
                }
            }
            return Encoding.ASCII.GetString(encoded);
        }

        /// <summary>
        /// Double encode a string.
        /// </summary>
        /// <param name="text">Input text.</param>
        /// <param name="level">Encoding level.</param>
        /// <returns>Encoded string.</returns>
        public static string DoubleEncode(string text, UriEncoding level) {
            if(string.IsNullOrEmpty(text)) {
                return text;
            }
            byte[] original = Encoding.UTF8.GetBytes(text);

            // count how many characters are affected by the encoding
            int charsToReplace = 0;
            int charsToEncode = 0;
            int length = original.Length;
            for(int i = 0; i < length; i++) {
                var ch = (char)original[i];
                if(ch == ' ') {
                    charsToReplace++;
                } else if(!IsValidCharInUri(ch, level)) {
                    charsToEncode++;
                }
            }

            // check if any characters are affected
            if((charsToReplace == 0) && (charsToEncode == 0)) {
                return text;
            }

            // copy, replace, and encode characters
            var encoded = new byte[length + (charsToReplace * 2) + (charsToEncode * 4)];
            int index = 0;
            for(int j = 0; j < length; j++) {
                byte asciiByte = original[j];
                char asciiChar = (char)asciiByte;
                if(IsValidCharInUri(asciiChar, level)) {
                    encoded[index++] = asciiByte;
                } else if(asciiChar == ' ') {

                    // replace ' ' with '%2b'
                    encoded[index++] = 0x25; // '%'
                    encoded[index++] = (byte)'2';
                    encoded[index++] = (byte)'b';
                } else {

                    // replace char with '%25' + code
                    encoded[index++] = 0x25; // '%'
                    encoded[index++] = (byte)'2';
                    encoded[index++] = (byte)'5';
                    encoded[index++] = (byte)StringUtil.IntToHexChar((asciiByte >> 4) & 15);
                    encoded[index++] = (byte)StringUtil.IntToHexChar(asciiByte & 15);
                }
            }
            return Encoding.ASCII.GetString(encoded);
        }

        /// <summary>
        /// Uri encode a string.
        /// </summary>
        /// <param name="text">Input text.</param>
        /// <returns>Encoded string.</returns>
        public static string Encode(string text) {
            return Encode(text, UriEncoding.Default);
        }

        /// <summary>
        /// Double encode a string.
        /// </summary>
        /// <param name="text">Input text.</param>
        /// <returns>Encoded string.</returns>
        public static string DoubleEncode(string text) {
            return DoubleEncode(text, UriEncoding.Default);
        }

        /// <summary>
        /// Encode a path segment.
        /// </summary>
        /// <param name="text">Input segment.</param>
        /// <returns>Encoded segment.</returns>
        public static string EncodeSegment(string text) {
            return Encode(text, UriEncoding.Segment);
        }

        /// <summary>
        /// Double encode a path segment.
        /// </summary>
        /// <param name="text">Input segment.</param>
        /// <returns>Encoded segment.</returns>
        public static string DoubleEncodeSegment(string text) {
            return DoubleEncode(text, UriEncoding.Segment);
        }

        /// <summary>
        /// Encode a query string.
        /// </summary>
        /// <param name="text">Input query.</param>
        /// <returns>Encoded query.</returns>
        public static string EncodeQuery(string text) {
            return Encode(text, UriEncoding.Query);
        }

        /// <summary>
        /// Encode a fragment string.
        /// </summary>
        /// <param name="text">Input fragement.</param>
        /// <returns>Encoded fragment.</returns>
        public static string EncodeFragment(string text) {
            return Encode(text, UriEncoding.Fragment);
        }

        /// <summary>
        /// Encode a Uri user info.
        /// </summary>
        /// <param name="text">Input user info.</param>
        /// <returns>Encoded user info.</returns>
        public static string EncodeUserInfo(string text) {
            return Encode(text, UriEncoding.UserInfo);
        }

        /// <summary>
        /// Validate a scheme.
        /// </summary>
        /// <param name="scheme">Input scheme.</param>
        /// <returns><see langword="True"/> if the scheme is valid</returns>
        public static bool IsValidScheme(string scheme) {
            return _schemeRegex.IsMatch(scheme);
        }

        /// <summary>
        /// Validate a host string.
        /// </summary>
        /// <param name="host">Input host.</param>
        /// <returns><see langword="True"/> if the host is valid</returns>
        public static bool IsValidHost(string host) {
            return _hostRegex.IsMatch(host);
        }

        /// <summary>
        /// Validate a segment.
        /// </summary>
        /// <param name="segment">Input segment.</param>
        /// <returns><see langword="True"/> if the segment is valid</returns>
        public static bool IsValidSegment(string segment) {
            return _segmentRegex.IsMatch(segment);
        }

        private static bool TryParse(string text, out string scheme, out string user, out string password, out string host, out int port, out bool usesDefautPort, out string[] segments, out bool trailingSlash, out KeyValuePair<string, string>[] @params, out string fragment) {
            Group group;
            scheme = null;
            user = null;
            password = null;
            host = null;
            port = -1;
            usesDefautPort = true;
            segments = null;
            trailingSlash = false;
            @params = null;
            fragment = null;
            if(string.IsNullOrEmpty(text)) {
                return false;
            }

            // parse uri
            Match match = _uriRegex.Match(text);
            if(!match.Success) {
                return false;
            }

            // extract scheme and host (mandatory parts);
            scheme = match.Groups["scheme"].Captures[0].Value;
            host = match.Groups["host"].Captures[0].Value;

            // extract user information (optional)
            group = match.Groups["userinfo"];
            if(group.Captures.Count > 0) {
                string userinfo = group.Captures[0].Value;

                // remove the trailing '@' character
                userinfo = userinfo.Substring(0, userinfo.Length - 1);
                string[] parts = userinfo.Split(new char[] { ':' }, 2);
                user = Decode(parts[0]);
                password = (parts.Length > 1) ? Decode(parts[1]) : null;
            }

            // extract port information (optional)
            group = match.Groups["port"];
            if(group.Captures.Count > 0) {
                string porttext = group.Captures[0].Value.Substring(1);
                if(!int.TryParse(porttext, out port)) {
                    return false;
                }
            }
            port = DeterminePort(scheme, port, out usesDefautPort);

            // extract path information (optional)
            group = match.Groups["path"];
            if(group.Captures.Count > 0) {
                string path = group.Captures[0].Value;
                ParsePath(path, false, out segments, out trailingSlash);
            } else {
                segments = EMPTY_ARRAY;
            }

            // extract query information (optional)
            group = match.Groups["query"];
            if(group.Captures.Count > 0) {
                string query = group.Captures[0].Value.Substring(1);
                @params = ParseParamsAsPairs(query);
            } else {
                @params = null;
            }

            // extract fragment information (optional)
            group = match.Groups["fragment"];
            if(group.Captures.Count > 0) {
                fragment = Decode(group.Captures[0].Value.Substring(1));
            }

            // validate parsed result
            if(string.IsNullOrEmpty(scheme)) {
                return false;
            }
            if(!IsValidScheme(scheme)) {
                return false;
            }
            if(host == null) {
                return false;
            }
            if(!IsValidHost(host)) {
                return false;
            }
            if((port < -1) || (port > ushort.MaxValue)) {
                return false;
            }
            if(segments != null) {
                for(int i = 0; i < segments.Length; ++i) {
                    string segment = segments[i];
                    if(string.IsNullOrEmpty(segment)) {
                        return false;
                    }
                    if(!IsValidSegment(segment)) {
                        return false;
                    }
                }
            }

            // all checks were passed successfully
            return true;
        }

        private static int DeterminePort(string scheme, int port, out bool isDefault) {
            int defaultPort = -1;
            int schemeHashCode = INVARIANT_IGNORE_CASE.GetHashCode(scheme);
            if(schemeHashCode == LOCAL_HASHCODE) {

                // use default port number (-1)
            } else if(schemeHashCode == HTTP_HASHCODE) {
                defaultPort = 80;
            } else if(schemeHashCode == HTTPS_HASHCODE) {
                defaultPort = 443;
            } else if(schemeHashCode == FTP_HASHCODE) {
                defaultPort = 21;
            }
            if(port == -1) {
                port = defaultPort;
            }
            isDefault = (port == defaultPort);
            return port;
        }

        private static bool EqualsStrings(string left, string right, bool ignoreCase) {
            int leftLength = (left != null) ? left.Length : -1;
            int rightLength = (right != null) ? right.Length : -1;
            if(leftLength != rightLength) {
                return false;
            }
            if(leftLength > 0) {
                return (ignoreCase ? INVARIANT_IGNORE_CASE : INVARIANT).Compare(left, right) == 0;
            }
            return true;
        }

        /// <summary>
        /// Validate that a character is valid for a uri at a specific encoding level.
        /// </summary>
        /// <param name="ch">Character to check.</param>
        /// <param name="level">Encoding level.</param>
        /// <returns><see langword="True"/> if the character is valid for the uri.</returns>
        public static bool IsValidCharInUri(char ch, UriEncoding level) {
            if((((ch >= 'a') && (ch <= 'z')) || ((ch >= 'A') && (ch <= 'Z'))) || ((ch >= '0') && (ch <= '9'))) {
                return true;
            }

            // the following characters are always safe
            switch(ch) {
            case '\'':
            case '(':
            case ')':
            case '*':
            case '-':
            case '.':
            case '_':
            case '!':
                return true;
            }

            // based on encoding level, additional character may be safe
            switch(level) {
            case UriEncoding.Fragment:

                // the following characters are safe when used in the fragment of a uri
                switch(ch) {
                case '#':
                    return true;
                }

                // all characters safe for UriEncoding.Query are also safe for UriEncoding.Fragment
                goto case UriEncoding.Query;
            case UriEncoding.Query:

                // the following characters are safe when used in the query of a uri
                switch(ch) {
                case '/':
                case ':':
                case '~':
                case '$':
                case ',':
                case ';':

                // NOTE (steveb): we don't encode | characters
                case '|':

                    // NOTE (steveb): don't decode '?', because it's handling is different on various web-servers (e.g. Apache vs. IIS)
                    // case '?':

                    return true;
                }

                // all characters safe for UriEncoding.Segment are also safe for UriEncoding.Query
                goto case UriEncoding.Segment;
            case UriEncoding.Segment:

                // the following characters are safe when used in a segment of a uri
                switch(ch) {
                case '@':

                // NOTE (steveb): we don't encode ^ characters
                case '^':
                    return true;
                }
                break;
            case UriEncoding.UserInfo:

                // the following characters are safe when used in the UserInfo part of a uri
                switch(ch) {
                case '&':
                case '=':
                    return true;
                }
                break;
            }
            return false;
        }

        private static bool HaveSameScheme(XUri left, XUri right, bool strict) {
            return INVARIANT_IGNORE_CASE.Equals(left.Scheme, right.Scheme) || (!strict && left.IsHttpOrHttps && right.IsHttpOrHttps);
        }

        private static bool HaveSamePort(XUri left, XUri right, bool strict) {
            return (left.Port == right.Port) || (!strict && left.UsesDefaultPort && right.UsesDefaultPort);
        }

        #region UrlDecode from Mono
        // This code comes from https://github.com/mono/mono/blob/bb9a8d9550f4b59e6e31ed39314f720115d1f8a8/mcs/class/System.Web/System.Web/HttpUtility.cs
        // This is being copied here to achieve consistent decoding between Mono versions as well as .Net and is used by XUri.Decode

        private static string UrlDecode(string str) {
            return UrlDecode(str, Encoding.UTF8);
        }

        private static char[] GetChars(MemoryStream b, Encoding e) {
            return e.GetChars(b.GetBuffer(), 0, (int)b.Length);
        }

        private static string UrlDecode(string s, Encoding e) {
            if(null == s)
                return null;

            if(s.IndexOf('%') == -1 && s.IndexOf('+') == -1)
                return s;

            if(e == null)
                e = Encoding.UTF8;

            StringBuilder output = new StringBuilder();
            long len = s.Length;
            MemoryStream bytes = new MemoryStream();
            int xchar;

            for(int i = 0; i < len; i++) {
                if(s[i] == '%' && i + 2 < len && s[i + 1] != '%') {
                    if(s[i + 1] == 'u' && i + 5 < len) {
                        if(bytes.Length > 0) {
                            output.Append(GetChars(bytes, e));
                            bytes.SetLength(0);
                        }

                        xchar = GetChar(s, i + 2, 4);
                        if(xchar != -1) {
                            output.Append((char)xchar);
                            i += 5;
                        } else {
                            output.Append('%');
                        }
                    } else if((xchar = GetChar(s, i + 1, 2)) != -1) {
                        bytes.WriteByte((byte)xchar);
                        i += 2;
                    } else {
                        output.Append('%');
                    }
                    continue;
                }

                if(bytes.Length > 0) {
                    output.Append(GetChars(bytes, e));
                    bytes.SetLength(0);
                }

                if(s[i] == '+') {
                    output.Append(' ');
                } else {
                    output.Append(s[i]);
                }
            }

            if(bytes.Length > 0) {
                output.Append(GetChars(bytes, e));
            }

            bytes = null;
            return output.ToString();
        }

        private static int GetInt(byte b) {
            char c = (char)b;
            if(c >= '0' && c <= '9')
                return c - '0';

            if(c >= 'a' && c <= 'f')
                return c - 'a' + 10;

            if(c >= 'A' && c <= 'F')
                return c - 'A' + 10;

            return -1;
        }

        private static int GetChar(string str, int offset, int length) {
            int val = 0;
            int end = length + offset;
            for(int i = offset; i < end; i++) {
                char c = str[i];
                if(c > 127)
                    return -1;

                int current = GetInt((byte)c);
                if(current == -1)
                    return -1;
                val = (val << 4) + current;
            }

            return val;
        }

        #endregion

        //--- Fields ---

        /// <summary>
        /// Uri scheme.
        /// </summary>
        public readonly string Scheme;

        /// <summary>
        /// Uri host.
        /// </summary>
        public readonly string Host;

        /// <summary>
        /// Uri port.
        /// </summary>
        public readonly int Port;

        /// <summary>
        /// User portion of user info.
        /// </summary>
        public readonly string User;

        /// <summary>
        /// Password portion of user info.
        /// </summary>
        public readonly string Password;

        /// <summary>
        /// Uri fragment.
        /// </summary>
        public readonly string Fragment;

        /// <summary>
        /// <see langword="True"/> if the Uri has a trailing slash.
        /// </summary>
        public readonly bool TrailingSlash;

        /// <summary>
        /// <see langword="True"/> if the Uri uses the default port for it's scheme.
        /// </summary>
        public readonly bool UsesDefaultPort;

        private readonly string[] _segments;
        private readonly KeyValuePair<string, string>[] _params;
        private readonly bool _doubleEncode = true;

        //--- Constructors ---

        /// <summary>
        /// Create a new XUri from an <see cref="Uri"/>
        /// </summary>
        /// <param name="uri">Input uri.</param>
        public XUri(Uri uri) : this(uri.OriginalString) { }

        /// <summary>
        /// Create a new XUri from serialized form.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        public XUri(SerializationInfo info, StreamingContext context) : this(info.GetString("uri")) { }

        /// <summary>
        /// Create a new XUri from a valid uri string.
        /// </summary>
        /// <param name="uri">Uri string.</param>
        /// <exception cref="UriFormatException">Thrown if string cannot be parsed into an XUri.</exception>
        public XUri(string uri) {
            if(!TryParse(uri, out this.Scheme, out this.User, out this.Password, out this.Host, out this.Port, out this.UsesDefaultPort, out _segments, out this.TrailingSlash, out _params, out this.Fragment)) {
                throw new UriFormatException("invalid uri syntax: " + uri ?? "(NULL)");
            }
        }

        /// <summary>
        /// Build a new XUri from its components.
        /// </summary>
        /// <param name="scheme">Uri scheme.</param>
        /// <param name="user">User.</param>
        /// <param name="password">Password.</param>
        /// <param name="host">Uri host.</param>
        /// <param name="port">Uri port.</param>
        /// <param name="segments">Path segments.</param>
        /// <param name="trailingSlash"><see langword="True"/> if the uri should have a trailing slash.</param>
        /// <param name="params">Query paramenters.</param>
        /// <param name="fragment">Uri fragment.</param>
        public XUri(string scheme, string user, string password, string host, int port, string[] segments, bool trailingSlash, KeyValuePair<string, string>[] @params, string fragment) {
            if(string.IsNullOrEmpty(scheme)) {
                throw new ArgumentNullException("scheme");
            }
            if(!IsValidScheme(scheme)) {
                throw new ArgumentException("invalid char in scheme: " + scheme);
            }
            if(host == null) {
                throw new ArgumentNullException("host");
            }
            if(!IsValidHost(host)) {
                throw new ArgumentException("invalid char in host: " + host);
            }
            if((port < -1) || (port > ushort.MaxValue)) {
                throw new ArgumentException("port");
            }
#if XURI_USE_NAMETABLE
            if(segments != null) {
                for(int i = 0; i < segments.Length; ++i) {
                    string segment = segments[i];
                    if(string.IsNullOrEmpty(segment)) {
                        throw new ArgumentException(string.Format("segment[{0}] is null", i));
                    }
                    if(!IsValidSegment(segment)) {
                        throw new ArgumentException("invalid char in segment: " + segment);
                    }
                    segments[i] = SysUtil.NameTable.Add(segment);
                }
            }
            this.Scheme = SysUtil.NameTable.Add(scheme);
            this.User = (user != null) ? SysUtil.NameTable.Add(user) : null;
            this.Host = SysUtil.NameTable.Add(host);
            this.Fragment = (fragment != null) ? SysUtil.NameTable.Add(fragment) : null;

            // TODO (steveb): internalize parameter strings
            _params = @params;
#else
            if(segments != null) {
                for(int i = 0; i < segments.Length; ++i) {
                    string segment = segments[i];
                    if(string.IsNullOrEmpty(segment)) {
                        throw new ArgumentException(string.Format("segment[{0}] is null", i));
                    }
                    if(!IsValidSegment(segment)) {
                        throw new ArgumentException("invalid char in segment: " + segment);
                    }
                    segments[i] = segment;
                }
            }
            this.Scheme = scheme;
            this.User = user;
            this.Host = host;
            this.Fragment = fragment;
            _params = @params;
#endif

            // these strings are never internalized
            _segments = segments ?? EMPTY_ARRAY;
            this.Password = password;
            this.Port = DeterminePort(scheme, port, out this.UsesDefaultPort);
            this.TrailingSlash = trailingSlash;
        }

        private XUri(string scheme, string user, string password, string host, int port, bool? defaultPort, string[] segments, bool trailingSlash, KeyValuePair<string, string>[] @params, string fragment, bool doubleEncodeSegments) {

            // NOTE: this constructor is similar to the public constructor, except that it does not use any of the RegEx checks

            if(string.IsNullOrEmpty(scheme)) {
                throw new ArgumentNullException("scheme");
            }
            if(host == null) {
                throw new ArgumentNullException("host");
            }
            if((port < -1) || (port > ushort.MaxValue)) {
                throw new ArgumentException("port");
            }
#if XURI_USE_NAMETABLE

            // TODO (steveb): we should not need to do this since we're deriving the new XUri instance from an existing one; 
            //                instead, we only need to capture the places where new strings can be added (WithHost, With, At, ...)

            if(segments != null) {
                for(int i = 0; i < segments.Length; ++i) {
                    string segment = segments[i];
                    if(string.IsNullOrEmpty(segment)) {
                        throw new ArgumentException("segment is null");
                    }
                    segments[i] = SysUtil.NameTable.Add(segment);
                }
            }
            this.Scheme = SysUtil.NameTable.Add(scheme);
            this.User = (user != null) ? SysUtil.NameTable.Add(user) : null;
            this.Host = SysUtil.NameTable.Add(host);
            this.Fragment = (fragment != null) ? SysUtil.NameTable.Add(fragment) : null;

            // TODO (steveb): internalize parameter strings
            _params = @params;
#else
            if(segments != null) {
                for(int i = 0; i < segments.Length; ++i) {
                    string segment = segments[i];
                    if(string.IsNullOrEmpty(segment)) {
                        throw new ArgumentException("segment is null");
                    }
                    segments[i] = segment;
                }
            }
            this.Scheme = scheme;
            this.User = user;
            this.Host = host;
            this.Fragment = fragment;
            _params = @params;
#endif
            // these strings are never internalized
            this.Password = password;
            if(defaultPort.HasValue) {
                this.Port = port;
                this.UsesDefaultPort = defaultPort.Value;
            } else {
                this.Port = DeterminePort(scheme, port, out this.UsesDefaultPort);
            }
            this.TrailingSlash = trailingSlash;
            _segments = segments ?? EMPTY_ARRAY;
            _doubleEncode = doubleEncodeSegments;
        }

        //--- Properties ---

        /// <summary>
        /// Query Paramenters.
        /// </summary>
        public KeyValuePair<string, string>[] Params { get { return _params; } }

        /// <summary>
        /// Path segments.
        /// </summary>
        public string[] Segments { get { return _segments; } }

        /// <summary>
        /// Uri has a query parameters.
        /// </summary>
        public bool HasQuery { get { return _params != null; } }

        /// <summary>
        /// Uri has a trailing fragment.
        /// </summary>
        public bool HasFragment { get { return Fragment != null; } }

        /// <summary>
        /// True if the instance is set to double-encode path segments.
        /// </summary>
        public bool UsesSegmentDoubleEncoding { get { return _doubleEncode; } }


        /// <summary>
        /// Uri authority, e.g. userinfo and host/port.
        /// </summary>
        public string Authority {
            get {
                string userinfo = UserInfo;
                if(userinfo != null) {
                    StringBuilder result = new StringBuilder();
                    result.Append(userinfo);
                    result.Append('@');
                    result.Append(HostPort);
                    return result.ToString();
                }
                return HostPort;
            }
        }

        /// <summary>
        /// Uri user info.
        /// </summary>
        public string UserInfo {
            get {
                if(User != null) {
                    if(Password != null) {
                        return User + ":" + Password;
                    } else {
                        return User;
                    }
                } else if(Password != null) {
                    return ":" + Password;
                }
                return null;
            }
        }

        /// <summary>
        /// Path.
        /// </summary>
        public string Path {
            get {
                StringBuilder result = new StringBuilder();
                for(int i = 0; i < _segments.Length; ++i) {
                    result.Append('/');
                    result.Append(_segments[i]);
                }
                if(TrailingSlash) {
                    result.Append('/');
                }
                return result.ToString();
            }
        }

        /// <summary>
        /// Query string.
        /// </summary>
        public string Query {
            get {
                return RenderParams(_params);
            }
        }

        /// <summary>
        /// <see cref="Host"/> + ":" + <see cref="Port"/>.
        /// </summary>
        public string HostPort {
            get {
                if(!UsesDefaultPort) {
                    StringBuilder result = new StringBuilder();
                    result.Append(Host);
                    result.Append(':');
                    result.Append(Port);
                    return result.ToString();
                }
                return Host;
            }
        }

        /// <summary>
        /// <see cref="Scheme"/> + "://" + <see cref="Host"/> + ":" + <see cref="Port"/>.
        /// </summary>
        public string SchemeHostPort {
            get {
                StringBuilder uri = new StringBuilder();
                uri.Append(Scheme);
                uri.Append("://");
                uri.Append(HostPort);
                return uri.ToString();
            }
        }

        /// <summary>
        /// <see cref="Scheme"/> + "://" + <see cref="Host"/> + ":" + <see cref="Port"/> + <see cref="Path"/>.
        /// </summary>
        public string SchemeHostPortPath {
            get {
                StringBuilder result = new StringBuilder();
                result.Append(Scheme);
                result.Append("://");
                result.Append(Host);
                if(!UsesDefaultPort) {
                    result.Append(':');
                    result.Append(Port);
                }
                result.Append(Path);
                return result.ToString();
            }
        }

        /// <summary>
        /// <see cref="Path"/> [ + "?" + <see cref="Query"/> ] [ + "#" + <see cref="Fragment"/> ].
        /// </summary>
        /// 
        public string PathQueryFragment {
            get {
                StringBuilder result = new StringBuilder();
                result.Append(Path);
                string query = Query;
                if(query != null) {
                    result.Append("?");
                    result.Append(query);
                }
                if(Fragment != null) {
                    result.Append("#");
                    result.Append(EncodeFragment(Fragment));
                }
                return result.ToString();
            }
        }

        /// <summary>
        /// [ + "?" + <see cref="Query"/> ] [ + "#" + <see cref="Fragment"/> ].
        /// </summary>
        public string QueryFragment {
            get {
                StringBuilder result = new StringBuilder();
                string query = Query;
                if(query != null) {
                    result.Append("?");
                    result.Append(query);
                }
                if(Fragment != null) {
                    result.Append("#");
                    result.Append(EncodeFragment(Fragment));
                }
                return result.ToString();
            }
        }

        /// <summary>
        /// Maximum number of similar tokens for partial comparison.
        /// </summary>
        public int MaxSimilarity {
            get {
                // NOTE (steveb): we count the port as part of the host information
                return ((Scheme != null) ? 1 : 0) + ((Host != null) ? 1 : 0) + Segments.Length;
            }
        }

        /// <summary>
        /// Last segment in uri.
        /// </summary>
        public string LastSegment {
            get {
                if(Segments.Length > 0) {
                    return Segments[Segments.Length - 1];
                }
                return null;
            }
        }

        /// <summary>
        /// <see langword="True"/> if <see cref="Host"/> is an IP address.
        /// </summary>
        public bool HostIsIp {
            get { return _ipRegex.Match(Host).Success; }
        }

        /// <summary>
        /// <see langword="True"/> if  <see cref="Path"/> is either "http" or "https".
        /// </summary>
        public bool IsHttpOrHttps {
            get {
                return INVARIANT_IGNORE_CASE.Equals(Scheme, "http") || INVARIANT_IGNORE_CASE.Equals(Scheme, "https");
            }
        }

        //--- Methods ---

        /// <summary>
        /// Get all segments.
        /// </summary>
        /// <param name="format">Encoding format for segments.</param>
        /// <returns>Array of segments.</returns>
        public string[] GetSegments(UriPathFormat format) {
            string[] result;
            switch(format) {
            case UriPathFormat.Original:
                result = _segments;
                break;
            case UriPathFormat.Decoded:
                result = new string[_segments.Length];
                for(int i = 0; i < result.Length; ++i) {
                    result[i] = Decode(_segments[i]);
                }
                break;
            case UriPathFormat.Normalized:
                result = new string[_segments.Length];
                for(int i = 0; i < result.Length; ++i) {
                    result[i] = Decode(_segments[i]).ToLowerInvariant();
                }
                break;
            default:
                throw new ArgumentException("format");
            }
            return result;
        }

        /// <summary>
        /// Get one segment.
        /// </summary>
        /// <param name="index">Index of segment.</param>
        /// <param name="format">Encoding format for segments.</param>
        /// <returns>Segment string.</returns>
        public string GetSegment(int index, UriPathFormat format) {
            string result = _segments[index];
            switch(format) {
            case UriPathFormat.Original:
                break;
            case UriPathFormat.Decoded:
                result = Decode(result);
                break;
            case UriPathFormat.Normalized:
                result = Decode(result).ToLowerInvariant();
                break;
            default:
                throw new ArgumentException("format");
            }
            return result;
        }

        /// <summary>
        /// Create a new XUri at a appended path.
        /// </summary>
        /// <param name="segments">Path segments.</param>
        /// <returns>New uri instance.</returns>
        public XUri At(params string[] segments) {
            if(segments == null) {
                throw new ArgumentNullException("segments");
            }
            if(segments.Length == 0) {
                return this;
            }
            List<string> newSegments = new List<string>(_segments.Length + segments.Length);
            newSegments.AddRange(_segments);
            bool trailingSlash = false;
            for(int i = 0; i < segments.Length; ++i) {
                string segment = segments[i];

                // check if segment is valid
                if(string.IsNullOrEmpty(segment)) {
                    throw new ArgumentException(string.Format("segment[{0}] is null or empty", i));
                }
                if(!IsValidSegment(segment)) {
                    throw new ArgumentException("invalid char in segment: " + segment);
                }

                // check if the last segment is empty
                if((i == (segments.Length - 1)) && (segments[i].Length == 0)) {

                    // discard segment and indicate that path has a trailing slash
                    trailingSlash = true;
                } else {

                    // add segment
                    newSegments.Add(segment);
                }
            }
            return new XUri(Scheme, User, Password, Host, Port, UsesDefaultPort, newSegments.ToArray(), trailingSlash, _params, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a new XUri with appended path/query/fragment.
        /// </summary>
        /// <param name="pathQueryFragment">Path/query/fragment string.</param>
        /// <returns>New Uri instance.</returns>
        public XUri AtPath(string pathQueryFragment) {
            if(string.IsNullOrEmpty(pathQueryFragment)) {
                return this;
            }
            XUri uri = this;
            string[] pathQuery = pathQueryFragment.Split(new char[] { '?' }, 2);
            string[] pathFragment = pathQueryFragment.Split(new char[] { '#' }, 2);
            string path = pathQuery[0];
            string query = (pathQuery.Length > 1) ? pathQuery[1] : null;
            string fragment = null;
            if(pathFragment.Length > 1) {
                if(query != null) {
                    pathFragment = query.Split(new char[] { '#' }, 2);
                    query = pathFragment[0];
                    fragment = pathFragment[1];
                } else {
                    path = pathFragment[0];
                    fragment = pathFragment[1];
                }
            }
            if(!string.IsNullOrEmpty(path)) {
                string[] segments;
                bool trailingSlash;
                ParsePath(path, true, out segments, out trailingSlash);
                uri = uri.At(segments);
                if(trailingSlash) {
                    uri = uri.WithTrailingSlash();
                } else {
                    uri = uri.WithoutTrailingSlash();
                }
            }
            if(query != null) {
                uri = uri.WithParams(ParseParamsAsPairs(query));
            }
            if(fragment != null) {
                uri = uri.WithFragment(fragment);
            }
            return uri;
        }

        /// <summary>
        /// Create a new Uri at a different absolute path.
        /// </summary>
        /// <param name="path">Path string.</param>
        /// <returns>New uri instance.</returns>
        public XUri AtAbsolutePath(string path) {

            // remove leading '/' if there is one since we're using AppendPath() to create the final uri
            if((path.Length > 0) && (path[0] == '/')) {
                path = path.Substring(1);
            }
            return WithoutPathQueryFragment().AtPath(path);
        }

        /// <summary>
        /// Get the relative portion of the current uri in relation to another uri.
        /// </summary>
        /// <remarks>Uri's must have same scheme, host and port.</remarks>
        /// <param name="uri">Comparison uri.</param>
        /// <returns>Relative path string.</returns>
        public string GetRelativePathTo(XUri uri) {
            return GetRelativePathTo(uri, false);
        }

        /// <summary>
        /// Get the relative portion of the current uri in relation to another uri.
        /// </summary>
        /// <remarks>Uri's must have same scheme, host and port.</remarks>
        /// <param name="uri">Comparison uri.</param>
        /// <param name="strict"><cref langword="True"/> to force a strict comparison.</param>
        /// <returns>Relative path string.</returns>
        public string GetRelativePathTo(XUri uri, bool strict) {
            if(!HaveSameScheme(this, uri, strict) || !INVARIANT_IGNORE_CASE.Equals(this.Host, uri.Host) || !HaveSamePort(this, uri, strict)) {
                throw new ArgumentException("uri is not related");
            }
            var result = new StringBuilder();

            // skip matching segments
            int commonSegments = 0;
            for(; (commonSegments < Segments.Length) && (commonSegments < uri.Segments.Length) && INVARIANT_IGNORE_CASE.Equals(Segments[commonSegments], uri.Segments[commonSegments]); ++commonSegments) { }

            // use '..' path for unmatched segments
            for(int j = commonSegments; j < uri.Segments.Length; ++j) {
                if(result.Length > 0) {
                    result.Append("/");
                }
                result.Append(UP_SEGMENT);
            }

            // append mising segments
            for(int j = commonSegments; j < Segments.Length; ++j) {
                if(result.Length > 0) {
                    result.Append("/");
                }
                result.Append(Segments[j]);
            }
            if(result.Length > 0 && TrailingSlash) {
                result.Append("/");
            }
            return result.ToString();
        }

        /// <summary>
        /// Checks whether a given uri is a prefix to the current instance.
        /// </summary>
        /// <param name="prefix">Prefix uri.</param>
        /// <returns><see langword="True"/> if the given uri is a prefix for the current instance.</returns>
        public bool HasPrefix(XUri prefix) {
            return HasPrefix(prefix, false);
        }

        /// <summary>
        /// Checks whether a given uri is a prefix to the current instance.
        /// </summary>
        /// <param name="prefix">Prefix uri.</param>
        /// <param name="strict"><cref langword="True"/> to force a strict comparison.</param>
        /// <returns><see langword="True"/> if the given uri is a prefix for the current instance.</returns>
        public bool HasPrefix(XUri prefix, bool strict) {

            // check that prefix matches
            if(!HaveSameScheme(this, prefix, strict) || !INVARIANT_IGNORE_CASE.Equals(Host, prefix.Host) || !HaveSamePort(this, prefix, strict)) {
                return false;
            }

            // skip matching segments
            int commonSegments = 0;
            for(; (commonSegments < Segments.Length) && (commonSegments < prefix.Segments.Length); ++commonSegments) {
                if(!INVARIANT_IGNORE_CASE.Equals(Segments[commonSegments], prefix.Segments[commonSegments])) {
                    return false;
                }
            }
            return commonSegments == prefix.Segments.Length;
        }

        /// <summary>
        /// Create a new uri with a changed prefix.
        /// </summary>
        /// <param name="from">Current prefix uri.</param>
        /// <param name="to">New prefix uri.</param>
        /// <returns>New uri with different prefix.</returns>
        public XUri ChangePrefix(XUri from, XUri to) {
            return ChangePrefix(from, to, false);
        }

        /// <summary>
        /// Create a new uri with a changed prefix.
        /// </summary>
        /// <param name="from">Current prefix uri.</param>
        /// <param name="to">New prefix uri.</param>
        /// <param name="strict"><cref langword="True"/> to force a strict comparison.</param>
        /// <returns>New uri with different prefix.</returns>
        public XUri ChangePrefix(XUri from, XUri to, bool strict) {

            // check that prefix matches
            if(!HaveSameScheme(this, from, strict) || !INVARIANT_IGNORE_CASE.Equals(Host, from.Host) || !HaveSamePort(this, from, strict)) {
                throw new ArgumentException(string.Format("uris are not related: unable to change prefix from {0} to {2} using {1}", this, from, to));
            }

            // skip matching segments
            int commonSegments = 0;
            for(; (commonSegments < Segments.Length) && (commonSegments < from.Segments.Length); ++commonSegments) {
                if(!INVARIANT_IGNORE_CASE.Equals(Segments[commonSegments], from.Segments[commonSegments])) {
                    break;
                }
            }
            int upSegments = from.Segments.Length - commonSegments;
            int copySegments = Segments.Length - commonSegments;

            // copy target segments
            string[] segments = new string[to.Segments.Length + upSegments + copySegments];
            Array.Copy(to.Segments, segments, to.Segments.Length);

            // create '..' segments for unmatched segments
            for(int i = 0; i < upSegments; ++i) {
                segments[i + to.Segments.Length] = UP_SEGMENT;
            }

            // add original segments
            for(int i = 0; i < copySegments; ++i) {
                segments[i + to.Segments.Length + upSegments] = Segments[i + commonSegments];
            }

            // build new uri
            return new XUri(to.Scheme, User, Password, to.Host, to.Port, to.UsesDefaultPort, segments, TrailingSlash, Params, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Get parameters values for a query key.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <returns>Array of parameter values, or <see cref="EMPTY_ARRAY"/> if key doesn't exist.</returns>
        public string[] GetParams(string key) {
            if(_params != null) {
                List<string> result = new List<string>();
                for(int i = 0; i < _params.Length; ++i) {
                    if(INVARIANT_IGNORE_CASE.Equals(_params[i].Key, key)) {
                        result.Add(_params[i].Value);
                    }
                }
                return result.ToArray();
            }
            return EMPTY_ARRAY;
        }

        /// <summary>
        /// Get single parameter value.
        /// </summary>
        /// <param name="key">Parameter key.</param>
        /// <returns>Parameter value, or null if key doesn't exist.</returns>
        public string GetParam(string key) {
            return GetParam(key, 0, null);
        }

        /// <summary>
        /// Get single parameter value.
        /// </summary>
        /// <param name="key">Parameter key.</param>
        /// <param name="def">Default to return if key doesn't exist.</param>
        /// <returns>Parameter or default value.</returns>
        public string GetParam(string key, string def) {
            return GetParam(key, 0, def);
        }

        /// <summary>
        /// Get single parameter value.
        /// </summary>
        /// <param name="key">Parameter key.</param>
        /// <param name="index">Index into list of parameter values.</param>
        /// <param name="def">Default to return if key doesn't exist.</param>
        /// <returns>Parameter or default value.</returns>
        public string GetParam(string key, int index, string def) {
            if(_params != null) {
                int counter = 0;
                for(int i = 0; i < _params.Length; ++i) {
                    if(INVARIANT_IGNORE_CASE.Equals(_params[i].Key, key)) {
                        if(counter == index) {
                            return _params[i].Value;
                        }
                        ++counter;
                    }
                }
            }
            return def;
        }

        /// <summary>
        /// Create a new Uri based on the current instance with an additional query parameter.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New uri.</returns>
        public XUri With(string key, string value) {
            return WithParams(new KeyValuePair<string, string>[] { new KeyValuePair<string, string>(key, value) });
        }

        /// <summary>
        /// Create a new Uri based on the current instance with additional query parameters.
        /// </summary>
        /// <param name="args">Array of query key/value pairs.</param>
        /// <returns>New uri.</returns>
        public XUri WithParams(KeyValuePair<string, string>[] args) {
            if((args == null) || (args.Length == 0)) {
                return this;
            }
            KeyValuePair<string, string>[] newParams;
            if(_params != null) {
                newParams = new KeyValuePair<string, string>[_params.Length + args.Length];
                Array.Copy(_params, 0, newParams, 0, _params.Length);
                Array.Copy(args, 0, newParams, _params.Length, args.Length);
            } else {
                newParams = new KeyValuePair<string, string>[args.Length];
                Array.Copy(args, 0, newParams, 0, args.Length);
            }
            return new XUri(Scheme, User, Password, Host, Port, UsesDefaultPort, Segments, TrailingSlash, newParams, Fragment, _doubleEncode);
        }

        /// <summary>
        /// This method is obsolete. Please use <see cref="WithParams(System.Collections.Generic.KeyValuePair{string,string}[])"/> instead
        /// </summary>
        [Obsolete("Please use 'WithParams(KeyValuePair<string, string>[] args)' instead")]
        public XUri WithParams(NameValueCollection args) {
            if(args == null) {
                return this;
            }

            // convert name-value collection to list
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            for(int i = 0; i < args.Count; ++i) {
                string key = args.GetKey(i);
                string values = args.Get(key);
                if(values != null) {
                    foreach(string value in args.GetValues(i)) {
                        list.Add(new KeyValuePair<string, string>(key, value));
                    }
                } else {
                    list.Add(new KeyValuePair<string, string>(key, null));
                }
            }
            return WithParams(list.ToArray());
        }

        /// <summary>
        /// Create new XUri based on the current instance with parameters from another uri added.
        /// </summary>
        /// <param name="uri">Other uri.</param>
        /// <returns>New uri.</returns>
        public XUri WithParamsFrom(XUri uri) {
            return WithParams(uri.Params);
        }

        /// <summary>
        /// Create a new XUri based on the current instance with the provided querystring added.
        /// </summary>
        /// <param name="query">Query string.</param>
        /// <returns>New uri.</returns>
        public XUri WithQuery(string query) {
            if(string.IsNullOrEmpty(query)) {
                return this;
            }
            return WithParams(ParseParamsAsPairs(query));
        }

        /// <summary>
        /// Create a copy of the current XUri with the Query removed.
        /// </summary>
        /// <returns>New uri.</returns>
        public XUri WithoutQuery() {
            return new XUri(Scheme, User, Password, Host, Port, UsesDefaultPort, Segments, TrailingSlash, null, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a copy of the current XUri with the Query parameters removed.
        /// </summary>
        /// <remarks>Same as <see cref="WithoutQuery"/>.</remarks>
        /// <returns>New uri.</returns>
        public XUri WithoutParams() {
            return WithoutQuery();
        }

        /// <summary>
        /// Create a new XUri based on the current instance with the given credentials.
        /// </summary>
        /// <param name="user">User.</param>
        /// <param name="password">Password.</param>
        /// <returns>New uri.</returns>
        public XUri WithCredentials(string user, string password) {
            return new XUri(Scheme, user, password, Host, Port, UsesDefaultPort, Segments, TrailingSlash, Params, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a new XUri based on the current instance with the credentials of another uri.
        /// </summary>
        /// <param name="uri">Input uri.</param>
        /// <returns>New uri.</returns>
        public XUri WithCredentialsFrom(XUri uri) {
            return new XUri(Scheme, uri.User, uri.Password, Host, Port, UsesDefaultPort, Segments, TrailingSlash, Params, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a copy of the current XUri with credentials removed.
        /// </summary>
        /// <returns>New uri.</returns>
        public XUri WithoutCredentials() {
            return new XUri(Scheme, null, null, Host, Port, UsesDefaultPort, Segments, TrailingSlash, Params, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a new XUri based on the current instance with the given fragment.
        /// </summary>
        /// <param name="fragment">Fragment.</param>
        /// <returns>New uri.</returns>
        public XUri WithFragment(string fragment) {
            return new XUri(Scheme, User, Password, Host, Port, UsesDefaultPort, Segments, TrailingSlash, _params, fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a copy of the current XUri with the fragment removed.
        /// </summary>
        /// <returns>New uri.</returns>
        public XUri WithoutFragment() {
            return new XUri(Scheme, User, Password, Host, Port, UsesDefaultPort, Segments, TrailingSlash, _params, null, _doubleEncode);
        }

        /// <summary>
        /// Create a new XUri based on the current instance with only a subset of the original path.
        /// </summary>
        /// <param name="count">Number of segments to keep.</param>
        /// <returns>New uri.</returns>
        public XUri WithFirstSegments(int count) {
            if(count <= 0) {
                throw new ArithmeticException("count");
            }
            if(count == _segments.Length) {
                return this;
            }
            string[] segments = new string[count];
            Array.Copy(_segments, segments, count);
            return new XUri(Scheme, User, Password, Host, Port, UsesDefaultPort, segments, TrailingSlash, Params, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a new XUri based on the current instance with only a subset of the original path.
        /// </summary>
        /// <param name="count">Number of segments to drop.</param>
        /// <returns>New uri.</returns>
        public XUri WithoutFirstSegments(int count) {
            if(count <= 0) {
                throw new ArithmeticException("count");
            }
            if((count == 0) || (_segments.Length == 0)) {
                return this;
            }
            int offset = Math.Min(count, Segments.Length);
            string[] segments = new string[Segments.Length - offset];
            if(segments.Length > 0) {
                Array.Copy(Segments, offset, segments, 0, segments.Length);
            }
            return new XUri(Scheme, User, Password, Host, Port, UsesDefaultPort, segments, TrailingSlash, Params, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a copy of the current XUri with the last segment removed.
        /// </summary>
        /// <returns>New uri.</returns>
        public XUri WithoutLastSegment() {
            return WithoutLastSegments(1);
        }

        /// <summary>
        /// Create a new XUri based on the current instance with only a subset of the original path.
        /// </summary>
        /// <param name="count">Number of segments to remove from end of path.</param>
        /// <returns>New uri.</returns>
        public XUri WithoutLastSegments(int count) {
            if(count < 0) {
                throw new ArgumentException("count");
            }
            if((count == 0) || (_segments.Length == 0)) {
                return this;
            }
            int begin = 0;
            int end = Math.Max(0, Segments.Length - count);
            string[] segments = new string[end - begin];
            if(segments.Length > 0) {
                Array.Copy(Segments, begin, segments, 0, segments.Length);
            }
            return new XUri(Scheme, User, Password, Host, Port, UsesDefaultPort, segments, TrailingSlash, Params, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a copy of the current XUri with the <see cref="Path"/>, <see cref="Query"/> and <see cref="Fragment"/> removed.
        /// </summary>
        /// <returns>New Uri.</returns>
        public XUri WithoutPathQueryFragment() {
            return new XUri(Scheme, User, Password, Host, Port, UsesDefaultPort, null, true, null, null, _doubleEncode);
        }

        /// <summary>
        /// Create a copy of the current XUri with the <see cref="UserInfo"/>, <see cref="Path"/>, <see cref="Query"/> and <see cref="Fragment"/> removed.
        /// </summary>
        /// <returns>New Uri.</returns>
        public XUri WithoutCredentialsPathQueryFragment() {
            return new XUri(Scheme, null, null, Host, Port, UsesDefaultPort, null, true, null, null, _doubleEncode);
        }

        /// <summary>
        /// Create a copy of the current XUri with a specific query parameter removed.
        /// </summary>
        /// <param name="key">Query parameter key.</param>
        /// <returns>New uri.</returns>
        public XUri WithoutParams(string key) {
            if(_params == null) {
                return this;
            }

            // keep all non-matching keys
            List<KeyValuePair<string, string>> rest = new List<KeyValuePair<string, string>>(_params.Length);
            for(int i = 0; i < _params.Length; ++i) {
                if(!INVARIANT_IGNORE_CASE.Equals(_params[i].Key, key)) {
                    rest.Add(_params[i]);
                }
            }
            return new XUri(Scheme, User, Password, Host, Port, UsesDefaultPort, Segments, TrailingSlash, rest.ToArray(), Fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a new XUri based on the current instance with a trailing slash.
        /// </summary>
        /// <returns>New uri.</returns>
        public XUri WithTrailingSlash() {
            return new XUri(Scheme, User, Password, Host, Port, UsesDefaultPort, Segments, true, Params, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a copy of the current XUri with the trailing slash removed.
        /// </summary>
        /// <returns>New uri.</returns>
        public XUri WithoutTrailingSlash() {
            return new XUri(Scheme, User, Password, Host, Port, UsesDefaultPort, Segments, false, Params, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a new XUri based on the current instance with a different scheme.
        /// </summary>
        /// <param name="scheme">New scheme.</param>
        /// <returns>New uri.</returns>
        public XUri WithScheme(string scheme) {
            if(string.IsNullOrEmpty(scheme)) {
                throw new ArgumentNullException("scheme");
            }
            if(!IsValidScheme(scheme)) {
                throw new ArgumentException("invalid char in scheme: " + scheme);
            }
            return new XUri(scheme, User, Password, Host, Port, UsesDefaultPort, Segments, TrailingSlash, Params, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a new XUri based on the current instance with a different host.
        /// </summary>
        /// <param name="host">New host.</param>
        /// <returns>New uri.</returns>
        public XUri WithHost(string host) {
            if(host == null) {
                throw new ArgumentNullException("host");
            }
            if(!IsValidHost(host)) {
                throw new ArgumentException("invalid char in host: " + host);
            }
            return new XUri(Scheme, User, Password, host, Port, UsesDefaultPort, Segments, TrailingSlash, Params, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Create a new XUri based on the current instance with a different port.
        /// </summary>
        /// <param name="port">New port.</param>
        /// <returns>New uri.</returns>
        public XUri WithPort(int port) {
            if((port < -1) || (port > ushort.MaxValue)) {
                throw new ArgumentException("port");
            }
            return new XUri(Scheme, User, Password, Host, port, null, Segments, TrailingSlash, Params, Fragment, _doubleEncode);
        }

        /// <summary>
        /// Turn on double-encoding of segments in <see cref="ToUri"/> conversion.
        /// </summary>
        /// <returns>New uri.</returns>
        public XUri WithSegmentDoubleEncoding() {
            return new XUri(Scheme, User, Password, Host, Port, null, Segments, TrailingSlash, Params, Fragment, true);
        }

        /// <summary>
        /// Turn off double-encoding of segments in <see cref="ToUri"/> conversion.
        /// </summary>
        /// <returns>New uri.</returns>
        public XUri WithoutSegmentDoubleEncoding() {
            return new XUri(Scheme, User, Password, Host, Port, null, Segments, TrailingSlash, Params, Fragment, false);
        }

        /// <summary>
        /// Convert the instance to a <see cref="Uri"/>.
        /// </summary>
        /// <returns></returns>
        public Uri ToUri() {
            StringBuilder result = new StringBuilder();

            // add scheme
            result.Append(Scheme);
            result.Append("://");

            // add user and password information
            if(User != null) {
                result.Append(EncodeUserInfo(User));
                if(Password != null) {
                    result.Append(":");
                    result.Append(EncodeUserInfo(Password));
                }
                result.Append("@");
            }

            // add domain
            result.Append(Host);

            // add port
            if(!UsesDefaultPort) {
                result.Append(":");
                result.Append(Port);
            }

            // add path
            for(int i = 0; i < Segments.Length; ++i) {
                string segment = Segments[i];
                result.Append('/');
                if(_doubleEncode) {

                    // NOTE (steveb): we double-encode ':' and trailing '.' characters, because IIS doesn't like them otherwise

                    // encode trailing '.'
                    int j;
                    for(j = segment.Length - 1; (j >= 0) && (segment[j] == '.'); --j) { }
                    ++j;
                    if(j < segment.Length) {
                        segment = segment.Substring(0, j) + "%252E".RepeatPattern(segment.Length - j);
                    }

                    // encode ':' and '|'
                    segment = segment.ReplaceAll(":", "%253A", "|", "%257C");
                }

                // append encoded segment to path
                result.Append(segment);
            }
            if(TrailingSlash) {
                result.Append('/');
            }

            // add query and fragment
            result.Append(QueryFragment);
            return new Uri(result.ToString());
        }

        /// <summary>
        /// Override of <see cref="object.Equals(object)"/>.
        /// </summary>
        public override bool Equals(object obj) {
            XUri other = obj as XUri;
            if(other == null) {
                throw new ArgumentNullException("obj");
            }

            // check segments length
            if(Segments.Length != other.Segments.Length) {
                return false;
            }

            // check port
            if(Port != other.Port) {
                return false;
            }

            // check scheme
            if(!INVARIANT_IGNORE_CASE.Equals(Scheme, other.Scheme)) {
                return false;
            }

            // check host
            if(!INVARIANT_IGNORE_CASE.Equals(Host, other.Host)) {
                return false;
            }

            // check username and password
            if(!EqualsStrings(User, other.User, true) || !EqualsStrings(Password, other.Password, false)) {
                return false;
            }

            // check path
            for(int i = 0; i < Segments.Length; ++i) {
                if(!INVARIANT_IGNORE_CASE.Equals(Segments[i], other.Segments[i])) {
                    return false;
                }
            }

            // check fragment
            if(!EqualsStrings(Fragment, other.Fragment, true)) {
                return false;
            }

            // check query parameters
            StringComparer provider = INVARIANT_IGNORE_CASE;
            int paramCount = (Params != null) ? Params.Length : -1;
            int otherParamCount = (other.Params != null) ? other.Params.Length : -1;
            if(paramCount != otherParamCount) {
                return false;
            }
            if(paramCount > 0) {
                for(int i = 0; i < paramCount; ++i) {
                    if(!EqualsStrings(Params[i].Key, other.Params[i].Key, true) || !EqualsStrings(Params[i].Value, other.Params[i].Value, false)) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Override of <see cref="object.GetHashCode"/>
        /// </summary>
        /// <returns>Uri hashcode.</returns>
        public override int GetHashCode() {
            StringComparer provider = INVARIANT_IGNORE_CASE;
            int result = provider.GetHashCode(Scheme) ^ provider.GetHashCode(Host) ^ Port;

            // add path
            for(int i = 0; i < Segments.Length; ++i) {
                result ^= provider.GetHashCode(Segments[i]);
            }

            // add query parameters
            if(Params != null) {
                for(int i = 0; i < Params.Length; ++i) {
                    result ^= provider.GetHashCode(Params[i].Key);
                    if(Params[i].Value != null) {
                        result ^= provider.GetHashCode(Params[i].Value);
                    }
                }
            }

            // add fragment
            if(Fragment != null) {
                result ^= provider.GetHashCode(Fragment);
            }
            return result;
        }

        /// <summary>
        /// Renders uri as a string with user info password replaced with 'xxx'.
        /// </summary>
        /// <returns>Uri string.</returns>
        public override string ToString() {
            return ToString(true);
        }

        /// <summary>
        /// Renders uri as a string 
        /// </summary>
        /// <param name="includePassword">If <see langword="True"/> the user info password is replaced with 'xxx'.</param>
        /// <returns>Uri string.</returns>
        public string ToString(bool includePassword) {
            StringBuilder result = new StringBuilder();
            result.Append(Scheme);
            result.Append("://");

            // add user and password information
            if(User != null) {
                result.Append(EncodeUserInfo(User));
                if(Password != null) {
                    result.Append(":");
                    if(includePassword) {
                        result.Append(EncodeUserInfo(Password));
                    } else {
                        result.Append("xxx");
                    }
                }
                result.Append("@");
            }

            // add domain
            result.Append(Host);

            // add port
            if(!UsesDefaultPort) {
                result.Append(":");
                result.Append(Port);
            }

            // add path
            for(int i = 0; i < _segments.Length; ++i) {
                result.Append('/');
                result.Append(_segments[i]);
            }
            if(TrailingSlash) {
                result.Append('/');
            }

            // add query
            if(_params != null) {
                result.Append("?");
                bool first = true;
                foreach(KeyValuePair<string, string> pair in _params) {
                    if(!first) {
                        result.Append("&");
                    }
                    first = false;
                    result.Append(EncodeQuery(pair.Key));
                    if(pair.Value != null) {
                        result.Append("=");
                        result.Append(EncodeQuery(pair.Value));
                    }
                }
            }

            // add fragment
            if(Fragment != null) {
                result.Append("#");
                result.Append(EncodeFragment(Fragment));
            }
            return result.ToString();
        }

        /// <summary>
        /// Compare instance to another uri for similarity.
        /// </summary>
        /// <param name="other">Other uri.</param>
        /// <returns>Total number of uri token matching in sequence.</returns>
        public int Similarity(XUri other) {
            return Similarity(other, false);
        }

        /// <summary>
        /// Compare instance to another uri for similarity.
        /// </summary>
        /// <param name="other">Other uri.</param>
        /// <param name="strict"><cref langword="True"/> to force a strict comparison.</param>
        /// <returns>Total number of uri token matching in sequence.</returns>
        public int Similarity(XUri other, bool strict) {
            int score = 0;

            // check scheme
            if(!HaveSameScheme(this, other, strict)) {
                return 0;
            }
            ++score;

            // check port
            if(!HaveSamePort(this, other, strict)) {
                return 0;
            }

            // check host
            if(!INVARIANT_IGNORE_CASE.Equals(Host, other.Host)) {
                return 0;
            }
            ++score;

            // check path
            int count = Math.Min(Segments.Length, other.Segments.Length);
            for(int i = 0; i < count; ++i) {
                if(!INVARIANT_IGNORE_CASE.Equals(Segments[i], other.Segments[i])) {
                    return 0;
                }
                ++score;
            }
            return score;
        }

        /// <summary>
        /// Check whether the uri starts with a certain path.
        /// </summary>
        /// <param name="segments">Array of segments to match.</param>
        /// <returns><see langword="True"/> if the uri starts with the given segments.</returns>
        public bool PathStartsWith(string[] segments) {
            if(Segments.Length < segments.Length) {
                return false;
            }
            for(int i = 0; i < segments.Length; ++i) {
                if(!INVARIANT_IGNORE_CASE.Equals(Segments[i], segments[i])) {
                    return false;
                }
            }
            return true;
        }

        //--- ISerializable Members ---
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("uri", ToString());
        }
    }
}
