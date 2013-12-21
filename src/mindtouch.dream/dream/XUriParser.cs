/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2013 MindTouch, Inc.
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
using System.IO;
using System.Text;

namespace MindTouch.Dream {
    public static class XUriParser {

        //--- Constants ---
        private enum State {
            End = 0,
            SchemeFirst,
            SchemeNext,
            HostnameOrUserInfoFirstLetter,
            HostnameOrUserInfoBeforeColon,
            HostnameOrUserInfoAfterColon,
            HostnameOrIPv6Address,
            Hostname,
            IPv6Address,
            PortNumberOrPathOrQueryOrFragmentOrEnd,
            PathNextChar,

            // special values assigned to reduce code complexity
            PortNumber = (int)':',
            PathFirstChar = (int)'/',
            Query = (int)'?',
            Fragment = (int)'#'
        }

        //--- Class Methods ---
        public static bool TryParse(string text, out string scheme, out string user, out string password, out string hostname, out int port, out bool usesDefautPort, out string[] segments, out bool trailingSlash, out string query, out string fragment) {
            scheme = null;
            user = null;
            password = null;
            hostname = null;
            port = -1;
            usesDefautPort = true;
            segments = null;
            trailingSlash = false;
            query = null;
            fragment = null;

            // check for trivial case
            if(string.IsNullOrEmpty(text)) {
                return false;
            }

            // initialize state and loop over all characters
            var state = State.SchemeFirst;
            string hostnameOrUsername = null;
            var segmentList = new List<string>(16);
            var decode = false;
            for(int current = 0, last = 0; current <= text.Length; ++current) {
                char c;
                if(current < text.Length) {
                    c = text[current];
                    switch(c) {
                    case '\0':

                        // '\0' is illegal in a uri string
                        return false;
                    case '\\':
                        c = '/';
                        break;
                    case '%':
                    case '+':
                        decode = true;
                        break;
                    }
                } else {

                    // use '\0' as end-of-string marker
                    c = '\0';
                }
                switch(state) {
                case State.SchemeFirst:
                    if(!IsAlpha(c)) {

                        // scheme must begin with alpha character
                        return false;
                    }
                    decode = false;
                    state = State.SchemeNext;
                    break;
                case State.SchemeNext:
                    if((text.Length - current >= 3) && (string.CompareOrdinal(text, current, "://", 0, 3) == 0)) {

                        // found "://" sequence at current location, we're done with scheme parsing
                        scheme = text.Substring(last, current - last);
                        last = current + 3;
                        current += 2;
                        decode = false;
                        state = State.HostnameOrUserInfoFirstLetter;
                    } else if(!IsAlphaDigit(c)) {

                        // scheme requires alphanumeric characters
                        return false;
                    }
                    break;
                case State.HostnameOrUserInfoFirstLetter:
                    if(c == '[') {

                        // NOTE (steveb): we want to include the leading character in the final result
                        last = current;

                        // IPv6 addresses start with '['
                        decode = false;
                        state = State.IPv6Address;
                    } else {

                        // we might either be parsing the username:password or hostname:port part
                        goto case State.HostnameOrUserInfoBeforeColon;
                    }
                    break;
                case State.HostnameOrUserInfoBeforeColon:
                    if(c == ':') {

                        // part before ':' is either a username or hostname
                        hostnameOrUsername = text.Substring(last, current - last);
                        last = current + 1;
                        state = State.HostnameOrUserInfoAfterColon;
                    } else if(c == '@') {

                        // part before '@' must be username since we didn't find ':'
                        user = text.Substring(last, current - last);
                        if(decode) {
                            user = Decode(user);
                        }
                        last = current + 1;
                        decode = false;
                        state = State.HostnameOrIPv6Address;
                    } else if((c == '/') || (c == '?') || (c == '#') || (c == 0)) {

                        // part before '/' or '\' must be hostname
                        if(decode) {
                            
                            // hostname cannot contain encoded characters
                            return false;
                        }
                        hostname = text.Substring(last, current - last);
                        last = current + 1;
                        decode = false;
                        state = (State)c;
                    } else if(!IsHostnameOrUserInfoChar(c)) {

                        // both username and hostname require alphanumeric characters
                        return false;
                    }
                    break;
                case State.HostnameOrUserInfoAfterColon:
                    if(c == '@') {

                        // part before ':' was username
                        user = hostnameOrUsername;
                        if(decode) {
                            user = Decode(user);
                        }

                        // part after ':' is password
                        password = text.Substring(last, current - last);
                        last = current + 1;
                        decode = false;
                        state = State.HostnameOrIPv6Address;
                    } else if((c == '/') || (c == '?') || (c == '#') || (c == 0)) {

                        // part before ':' was hostname
                        if(decode) {
                            
                            // hostname cannot contain encoded characters
                            return false;
                        }
                        hostname = hostnameOrUsername;

                        // part after ':' is port, parse and validate it
                        if(!int.TryParse(text.Substring(last, current - last), out port) || (port < 0)) {
                            return false;
                        }
                        last = current + 1;
                        decode = false;
                        state = (State)c;
                    } else if(!IsHostnameOrUserInfoChar(c)) {

                        // password requires alphanumeric characters; for port information, we'll validate it once we've identified it
                        return false;
                    }
                    break;
                case State.HostnameOrIPv6Address:
                    if(c == '[') {

                        // NOTE (steveb): we want to include the leading character in the final result
                        last = current;

                        // IPv6 addresses start with '['
                        decode = false;
                        state = State.IPv6Address;
                    } else {
                        goto case State.Hostname;
                    }
                    break;
                case State.Hostname:
                    if((c == ':') || (c == '/') || (c == '?') || (c == '#') || (c == 0)) {
                        if(decode) {

                            // hostname cannot contain encoded characters
                            return false;
                        }
                        hostname = text.Substring(last, current - last);
                        last = current + 1;
                        decode = false;
                        state = (State)c;
                    } else if(!IsHostnameOrUserInfoChar(c)) {

                        // hostname requires alphanumeric characters
                        return false;
                    }
                    break;
                case State.IPv6Address:
                    if(c == ']') {
                        if(decode) {

                            // hostname cannot contain encoded characters
                            return false;
                        }
                        hostname = text.Substring(last, current - last + 1);
                        last = current + 1;
                        decode = false;
                        state = State.PortNumberOrPathOrQueryOrFragmentOrEnd;
                    } else if(!(((c >= 'a') && (c <= 'f')) || ((c >= 'A') && (c <= 'F')) || ((c >= '0') && (c <= '9')) || (c == ':') || (c == '.'))) {

                        // IPv6 address requires hexadecimal characters, colons, or periods
                        return false;
                    }
                    break;
                case State.PortNumberOrPathOrQueryOrFragmentOrEnd:
                    if(c == ':') {
                        last = current + 1;
                        decode = false;
                        state = State.PortNumber;
                    } else if((c == '/') || (c == '?') || (c == '#') || (c == 0)) {
                        last = current + 1;
                        decode = false;
                        state = (State)c;
                    } else {
                        return false;
                    }
                    break;
                case State.PortNumber:
                    if((c == '/') || (c == '?') || (c == '#') || (c == 0)) {
                        if(!int.TryParse(text.Substring(last, current - last), out port)) {
                            return false;
                        }
                        last = current + 1;
                        decode = false;
                        state = (State)c;
                    } else if(!((c >= '0') && (c <= '9'))) {

                        // port number requires decimal characters
                        return false;
                    }
                    break;
                case State.PathFirstChar:
                    if((c == '?') || (c == '#') || (c == 0)) {
                        if(last == current) {
                            trailingSlash = true;
                        } else {
                            segmentList.Add(text.Substring(last, current - last));
                        }
                        last = current + 1;
                        decode = false;
                        state = (State)c;
                    } else if(c == '/') {

                        // we allow leading '/' characters in segments; stay in first-char state
                    } else if(IsPathChar(c)) {
                        state = State.PathNextChar;
                        decode = false;
                    } else {
                        return false;
                    }
                    break;
                case State.PathNextChar:
                    if((c == '?') || (c == '#') || (c == 0)) {
                        segmentList.Add(text.Substring(last, current - last));
                        last = current + 1;
                        decode = false;
                        state = (State)c;
                    } else if(c == '/') {
                        segmentList.Add(text.Substring(last, current - last));
                        last = current + 1;
                        decode = false;
                        state = State.PathFirstChar;
                    } else if(!IsPathChar(c)) {
                        return false;
                    }
                    break;
                case State.Query:
                    if((c == '#') || (c == 0)) {
                        query = text.Substring(last, current - last);
                        last = current + 1;
                        decode = false;
                        state = (State)c;
                    } else if(!IsQueryChar(c)) {
                        return false;
                    }
                    break;
                case State.Fragment:
                    if(c == 0) {
                        fragment = text.Substring(last, current - last);
                        if(decode) {
                            fragment = Decode(fragment);
                        }
                        decode = false;
                        state = State.End;
                    } else if(!IsFragmentChar(c)) {
                        return false;
                    }
                    break;
                case State.End:
                    throw new ShouldNeverHappenException("State.End");
                default:
                    throw new ShouldNeverHappenException("default");
                }
            }

            // TODO:
            // * parse query into key=value pairs

            segments = segmentList.ToArray();
            return true;
        }

        private static bool IsAlpha(char c) {
            return ((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z'));
        }

        private static bool IsAlphaDigit(char c) {
            return ((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')) || ((c >= '0') && (c <= '9'));
        }

        private static bool IsHostnameOrUserInfoChar(char c) {

            // Implements: [\w\-\._~%!\$&'\(\)\*\+,;=]

            return ((c >= 'a') && (c <= 'z')) || 
                ((c >= 'A') && (c <= 'Z')) || 
                ((c >= '0') && (c <= '9')) ||
                ((c >= '$') && (c <= '.')) || /* one of: $%&'()*+,-. */
                (c == '!') || 
                (c == ';') || 
                (c == '=') || 
                (c == '_') || 
                (c == '~');
        }

        private static bool IsPathChar(char c) {

            // Implements: [\w\-\._~%!\$&'\(\)\*\+,;=:@\^\|\[\]{}]

            return (c == '!') ||
                ((c >= '$') && (c <= '=')) ||
                ((c >= '@') && (c <= '_')) ||
                ((c >= 'a') && (c <= '~'));
        }

        private static bool IsQueryChar(char c) {

            // Implements: [\w\-\._~%!\$&'\(\)\*\+,;=:@\^/\?|\[\]{}]

            return (c == '!') ||
                ((c >= '$') && (c <= '_')) ||
                ((c >= 'a') && (c <= '~'));
        }

        private static bool IsFragmentChar(char c) {

            // Implements: [\w\-\._~%!\$&'\(\)\*\+,;=:@\^/\?|\[\]{}#]

            // ! 33

            // # 35 (not valid in path and query)
            // $ 36
            // % 37
            // & 38
            // ' 39
            // ( 40
            // ) 41
            // * 42
            // + 43
            // , 44
            // - 45
            // . 46
            // / 47
            // 0..9 48..57
            // : 58
            // ; 59
            // = 61
            // ? 63 (not valid in path)
            // @ 64
            // A..Z 65..90
            // [ 91
            // \ 92 (new)
            // ] 93
            // ^ 94
            // _ 95

            // a..z 97..122
            // { 123
            // | 124
            // } 125
            // ~ 126

            return (c == '!') ||
                ((c >= '#') && (c <= '_')) ||
                ((c >= 'a') && (c <= '~'));
        }

        private static string Decode(string text) {
            if(null == text) {
                return null;
            }
            var output = new StringBuilder();
            long len = text.Length;
            var bytes = new MemoryStream();
            for(var i = 0; i < len; i++) {
                if(text[i] == '%' && i + 2 < len && text[i + 1] != '%') {
                    int xchar;
                    if(text[i + 1] == 'u' && i + 5 < len) {
                        if(bytes.Length > 0) {
                            output.Append(GetChars(bytes));
                            bytes.SetLength(0);
                        }
                        xchar = GetChar(text, i + 2, 4);
                        if(xchar != -1) {
                            output.Append((char)xchar);
                            i += 5;
                        } else {
                            output.Append('%');
                        }
                    } else if((xchar = GetChar(text, i + 1, 2)) != -1) {
                        bytes.WriteByte((byte)xchar);
                        i += 2;
                    } else {
                        output.Append('%');
                    }
                    continue;
                }
                if(bytes.Length > 0) {
                    output.Append(GetChars(bytes));
                    bytes.SetLength(0);
                }
                output.Append(text[i] == '+' ? ' ' : text[i]);
            }
            if(bytes.Length > 0) {
                output.Append(GetChars(bytes));
            }
            return output.ToString();
        }

        private static char[] GetChars(MemoryStream b) {
            return Encoding.UTF8.GetChars(b.GetBuffer(), 0, (int)b.Length);
        }

        private static int GetChar(string text, int offset, int length) {
            var result = 0;
            var end = length + offset;
            for(var i = offset; i < end; i++) {
                var c = text[i];
                int value;
                if(c >= '0' && c <= '9') {
                    value = c - '0';
                } else if(c >= 'a' && c <= 'f') {
                    value = c - 'a' + 10;
                } else if(c >= 'A' && c <= 'F') {
                    value = c - 'A' + 10;
                } else {
                    return -1;
                }
                result = (result << 4) + value;
            }
            return result;
        }
    }
}

