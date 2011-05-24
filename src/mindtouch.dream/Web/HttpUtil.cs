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
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using MindTouch.Dream;

namespace MindTouch.Web {

    /// <summary>
    /// Static utility class containing extension and helper methods for Web and Http related tasks.
    /// </summary>
    public static class HttpUtil {

        // NOTE (steveb): cookie parsing based on RFC2109 (http://rfc.net/rfc2109.html)

        //--- Class Fields ---
        private static readonly MethodInfo _addHeaderMethod = typeof(WebHeaderCollection).GetMethod("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Regex _rangeRegex = new Regex(@"((?<rangeSpecifier>[^\s]+)\s*(=|\s))?\s*(?<from>\d+)(-(?<to>\d+))?", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _encodingFixUpRegex = new Regex(@"[ \[\]{}""'`<>]", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        //--- Extension Methods ---

        /// <summary>
        /// Add a header to a web request.
        /// </summary>
        /// <param name="request">Target web request.</param>
        /// <param name="key">Header Key.</param>
        /// <param name="value">Header Value.</param>
        public static void AddHeader(this HttpWebRequest request, string key, string value) {
            if(string.Compare(key, DreamHeaders.ACCEPT, true) == 0) {
                request.Accept = value;
            } else if(string.Compare(key, DreamHeaders.CONNECTION, true) == 0) {

                // ignored: set automatically
                //request.Connection = value;
            } else if(string.Compare(key, DreamHeaders.CONTENT_LENGTH, true) == 0) {
                request.ContentLength = long.Parse(value);
            } else if(string.Compare(key, DreamHeaders.CONTENT_TYPE, true) == 0) {
                request.ContentType = value;
            } else if(string.Compare(key, DreamHeaders.EXPECT, true) == 0) {

                // ignored: set automatically
                // request.Expect = value;
            } else if(string.Compare(key, DreamHeaders.DATE, true) == 0) {

                // ignored: set automatically
            } else if(string.Compare(key, DreamHeaders.HOST, true) == 0) {

                // ignored: set automatically
            } else if(string.Compare(key, DreamHeaders.IF_MODIFIED_SINCE, true) == 0) {
                request.IfModifiedSince = DateTimeUtil.ParseInvariant(value);
            } else if(string.Compare(key, DreamHeaders.RANGE, true) == 0) {

                // read range-specifier, with range (e.g. "bytes=500-999")
                Match m = _rangeRegex.Match(value);
                if(m.Success) {
                    int from = int.Parse(m.Groups["from"].Value);
                    int to = m.Groups["to"].Success ? int.Parse(m.Groups["to"].Value) : -1;
                    string rangeSpecifier = m.Groups["rangeSpecifier"].Success ? m.Groups["rangeSpecifier"].Value : null;
                    if((rangeSpecifier != null) && (to >= 0)) {
                        request.AddRange(rangeSpecifier, from, to);
                    } else if(rangeSpecifier != null) {
                        request.AddRange(rangeSpecifier, from);
                    } else if(to >= 0) {
                        request.AddRange(from, to);
                    } else {
                        request.AddRange(from);
                    }
                }
            } else if(string.Compare(key, DreamHeaders.REFERER, true) == 0) {
                request.Referer = value;
            } else if(string.Compare(key, DreamHeaders.PROXY_CONNECTION, true) == 0) {

                // TODO (steveb): not implemented
#if DEBUG
                throw new NotImplementedException("missing code");
#endif
            } else if(string.Compare(key, DreamHeaders.TRANSFER_ENCODING, true) == 0) {

                // TODO (steveb): not implemented
#if DEBUG
                throw new NotImplementedException("missing code");
#endif
            } else if(string.Compare(key, DreamHeaders.USER_AGENT, true) == 0) {
                request.UserAgent = value;
            } else {
                request.Headers.Add(key, value);
            }
        }

        /// <summary>
        /// Add a header to a http response.
        /// </summary>
        /// <param name="response">Target http response</param>
        /// <param name="key">Header Key.</param>
        /// <param name="value">Header Value.</param>
        public static void AddHeader(this HttpListenerResponse response, string key, string value) {
            if(string.Compare(key, DreamHeaders.ACCEPT, true) == 0) {
                throw new ArgumentException(key);
            } else if(string.Compare(key, DreamHeaders.CONNECTION, true) == 0) {

                // special case: this header is automatically set, just ignore it
                // throw new ArgumentException(key);
            } else if(string.Compare(key, DreamHeaders.CONTENT_LENGTH, true) == 0) {
                response.ContentLength64 = long.Parse(value);
            } else if(string.Compare(key, DreamHeaders.CONTENT_ENCODING, true) == 0) {

                // special case: not required by WebHeaderCollection, but present in HttpWebResponse
                response.Headers[DreamHeaders.CONTENT_ENCODING] = value;
            } else if(string.Compare(key, DreamHeaders.CONTENT_TYPE, true) == 0) {
                response.ContentType = value;
            } else if(string.Compare(key, DreamHeaders.EXPECT, true) == 0) {
                throw new ArgumentException(key);
            } else if(string.Compare(key, DreamHeaders.DATE, true) == 0) {

                // special case: this header is automatically set, just ignore it
                // throw new ArgumentException(key);
            } else if(string.Compare(key, DreamHeaders.HOST, true) == 0) {
                throw new ArgumentException(key);
            } else if(string.Compare(key, DreamHeaders.IF_MODIFIED_SINCE, true) == 0) {
                throw new ArgumentException(key);
            } else if(string.Compare(key, DreamHeaders.LOCATION, true) == 0) {

                // special case: not required by WebHeaderCollection, but present in HttpWebResponse
                response.RedirectLocation = value;
            } else if(string.Compare(key, DreamHeaders.RANGE, true) == 0) {
                throw new ArgumentException(key);
            } else if(string.Compare(key, DreamHeaders.REFERER, true) == 0) {
                throw new ArgumentException(key);
            } else if(string.Compare(key, DreamHeaders.PROXY_CONNECTION, true) == 0) {

                // TODO (steveb): not implemented
#if DEBUG
                throw new NotImplementedException("missing code");
#endif
            } else if(string.Compare(key, DreamHeaders.TRANSFER_ENCODING, true) == 0) {

                // special case: this header is automatically set, just ignore it
                // throw new ArgumentException(key);
            } else if(string.Compare(key, DreamHeaders.USER_AGENT, true) == 0) {
                throw new ArgumentException(key);
            } else if(string.Compare(key, DreamHeaders.AUTHENTICATE, true) == 0) {

                // NOTE (steveb): we didn't have a choice here; we have to be able to set 'WWW-Authenticate', but WebHeaderCollection won't let us any other way
                UnsafeAddHeader(response.Headers, DreamHeaders.AUTHENTICATE, value);
            } else {
                response.AddHeader(key, value);
            }
        }

        //--- Class Methods ---

        /// <summary>
        /// Retrieve user credentials from a request uri and/or headers.
        /// </summary>
        /// <param name="uri">Request uri.</param>
        /// <param name="headers">Request headers.</param>
        /// <param name="username">Parsed user name.</param>
        /// <param name="password">Parsed password.</param>
        /// <returns><see langword="True"/> if the credentials were succesfully parsed from request information.</returns>
        public static bool GetAuthentication(Uri uri, DreamHeaders headers, out string username, out string password) {
            username = null;
            password = null;

            // Authorization = Basic 1YJ1TTpPcmx4bMQ=

            // check if a user/password pair was provided in the URI
            if(!string.IsNullOrEmpty(uri.UserInfo)) {
                string[] userPwd = uri.UserInfo.Split(new char[] { ':' }, 2);
                username = XUri.Decode(userPwd[0]);
                password = XUri.Decode((userPwd.Length > 1) ? userPwd[1] : string.Empty);
                return true;
            } else {

                // check if authorization is in the request header
                string header = headers[DreamHeaders.AUTHORIZATION];
                if(!string.IsNullOrEmpty(header)) {

                    // extract authorization data
                    string[] value = header.Split(new char[] { ' ' }, 2);
                    if((value.Length == 2) && StringUtil.EqualsInvariantIgnoreCase(value[0], "Basic")) {
                        string[] userPwd = Encoding.UTF8.GetString(Convert.FromBase64String(value[1])).Split(new char[] { ':' }, 2);
                        username = userPwd[0];
                        password = (userPwd.Length > 1) ? userPwd[1] : string.Empty;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Render Basic Authentication value.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <param name="password">Password.</param>
        /// <returns>Basic Authentication string.</returns>
        public static string RenderBasicAuthentication(string username, string password) {
            string credentials = XUri.Decode(username ?? string.Empty) + ":" + XUri.Decode(password ?? string.Empty);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        }

        /// <summary>
        /// Extract <see cref="CultureInfo"/> from a header value.
        /// </summary>
        /// <param name="header">Header value to be parsed.</param>
        /// <param name="def">Default <see cref="CultureInfo"/> to return in case no culture can be parsed from the header.</param>
        /// <returns>Parsed or default culture.</returns>
        public static CultureInfo GetCultureInfoFromHeader(string header, CultureInfo def) {
            if(!string.IsNullOrEmpty(header)) {

                // NOTE: we attempt to find the best acceptable language; format is: da, en-gb;q=0.8, en;q=0.7, *

                // convert language header into sorted list of languages
                List<Tuplet<string, double>> choices = new List<Tuplet<string, double>>();
                foreach(string choice in header.Split(',')) {
                    string[] parts = choice.Split(';');
                    string name = parts[0].Trim();

                    // parse optional quality parameter
                    double quality = (name == "*") ? 0.0 : 1.0;
                    if((parts.Length == 2)) {
                        string value = parts[1].Trim();
                        if(value.StartsWith("q=")) {
                            double.TryParse(value.Substring(2), out quality);
                        }
                    }

                    // add language option
                    choices.Add(new Tuplet<string, double>(name, quality));
                }
                choices.Sort(delegate(Tuplet<string, double> left, Tuplet<string, double> right) {

                    // reverse order sort based on quality
                    return Math.Sign(right.Item2 - left.Item2);
                });

                // find the first acceptable language
                for(int i = 0; i < choices.Count; ++i) {

                    // check for wildcard
                    if(choices[0].Item1 == "*") {
                        return def;
                    }

                    // expand language to full culture
                    CultureInfo culture = CultureUtil.GetNonNeutralCulture(choices[i].Item1);
                    if(culture != null) {
                        return culture;
                    }
                }
            }
            return def;
        }

        /// <summary>
        /// Parse all name value pairs from a header string.
        /// </summary>
        /// <param name="header">Header to be parsed.</param>
        /// <returns>Dictionary of header name value pairs.</returns>
        public static Dictionary<string, string> ParseNameValuePairs(string header) {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int index = 0;
            string name;
            string value;
            int count = 1;
            while(ParseNameValue(out name, out value, header, ref index, false)) {
                if(value == null) {
                    result["#" + count.ToString()] = name;
                    ++count;
                }
                result[name] = value;
            }
            return result;
        }

        private static bool ParseNameValue(out string name, out string value, string text, ref int index, bool useCommaAsSeparator) {
            name = null;
            value = null;
            SkipWhitespace(text, ref index);
            if(!ParseWord(out name, text, ref index)) {
                return false;
            }
            SkipWhitespace(text, ref index);
            if(MatchToken("=", text, ref index)) {
                int useCommaAsSeparatorStartingAtOffset = (useCommaAsSeparator ? 0 : int.MaxValue);

                // NOTE (steveb): 'expires' can contain commas, but cannot be quoted; so we need skip some characters when we find it before we allows commas again
                if(useCommaAsSeparator && StringUtil.EqualsInvariantIgnoreCase(name, "expires")) {
                    useCommaAsSeparatorStartingAtOffset = 6;
                }

                if(!ParseValue(out value, text, ref index, useCommaAsSeparatorStartingAtOffset)) {
                    return false;
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

        private static bool ParseValue(out string word, string text, ref int index, int useCommaAsSeparatorStartingAtOffset) {
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
                for(last = index; (last < text.Length) && (text[last] != ';' && (((last - index) <= useCommaAsSeparatorStartingAtOffset) || (text[last] != ','))); ++last) { }

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

        private static void UnsafeAddHeader(WebHeaderCollection collection, string key, string value) {
            _addHeaderMethod.Invoke(collection, new object[] { key, value });
        }

        /// <summary>
        /// Derive request uri from the HttpContext.
        /// </summary>
        /// <param name="context">Source context.</param>
        /// <returns>Request uri.</returns>
        public static XUri FromHttpContext(HttpListenerContext context) {

            // Note (arnec): RawUrl seems to have a leading //, at least on windows, which we need
            // to trim to at least just a /
            return FromHttpContextComponents(context.Request.Url, context.Request.RawUrl.Remove(0, 1));
        }

        /// <summary>
        /// Derive request uri from the HttpContext.
        /// </summary>
        /// <param name="context">Source context.</param>
        /// <returns>Request uri.</returns>
        public static XUri FromHttpContext(HttpContext context) {
            return FromHttpContextComponents(context.Request.Url, context.Request.RawUrl);
        }

        /// <summary>
        /// Build request uri from decoded uri and raw path.
        /// </summary>
        /// <param name="uri">Already decoded uri.</param>
        /// <param name="rawpath">Raw path string.</param>
        /// <returns>Properly encoded request uri.</returns>
        public static XUri FromHttpContextComponents(Uri uri, string rawpath) {

            // Note (arnec): RawUrl is only the path portion, lacking SchemeHostPort
            // but Url does path decoding, so we have to construct the url ourselves
            var parsed = new XUri(uri.GetLeftPart(UriPartial.Authority));
            try {
                parsed = parsed.AtAbsolutePath(rawpath);
            } catch {
                // need to try to do an extra pass at encoding the uri, just in case
                rawpath = _encodingFixUpRegex.Replace(rawpath, m => String.Format("%{0}", StringUtil.HexStringFromBytes(Encoding.ASCII.GetBytes((string)m.Value))));
                parsed = parsed.AtAbsolutePath(rawpath);
            }
            return parsed;
        }
    }
}
