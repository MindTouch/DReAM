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
using System.Text;

namespace MindTouch.Dream {
    public static class XUriParser {

        // NOTE (steveb): XUriParser parses absolute URIs based on RFC3986 (http://www.ietf.org/rfc/rfc3986.txt), with the addition of ^, |, [, ], { and } as a valid character in segments, queries, and fragments; and \ as valid segment separator

        //--- Types ---
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
            QueryStart = (int)'?',
            QueryKey = (int)'&',
            QueryValue = (int)'=',
            Fragment = (int)'#'
        }

        //--- Class Fields ---
        private static readonly StringComparer INVARIANT_IGNORE_CASE = StringComparer.OrdinalIgnoreCase;
        private static readonly int HTTP_HASHCODE = StringComparer.OrdinalIgnoreCase.GetHashCode("http");
        private static readonly int HTTPS_HASHCODE = StringComparer.OrdinalIgnoreCase.GetHashCode("https");
        private static readonly int LOCAL_HASHCODE = StringComparer.OrdinalIgnoreCase.GetHashCode("local");
        private static readonly int FTP_HASHCODE = StringComparer.OrdinalIgnoreCase.GetHashCode("ftp");

        //--- Class Methods ---
        public static XUri TryParse(string text) {
            string scheme;
            string user;
            string password;
            string hostname;
            int port;
            bool usesDefaultPort;
            bool trailingSlash;
            string[] segements;
            string fragment;
            KeyValuePair<string, string>[] @params;
            if(!TryParse(text, out scheme, out user, out password, out hostname, out port, out usesDefaultPort, out segements, out trailingSlash, out @params, out fragment)) {
                return null;
            }
            return XUri.NewUnsafe(scheme, user, password, hostname, port, usesDefaultPort, segements, trailingSlash, @params, fragment, true);
        }

        public static bool TryParse(string text, out string scheme, out string user, out string password, out string hostname, out int port, out bool usesDefautPort, out string[] segments, out bool trailingSlash, out KeyValuePair<string, string>[] @params, out string fragment) {
            scheme = null;
            user = null;
            password = null;
            hostname = null;
            port = -1;
            usesDefautPort = true;
            segments = null;
            trailingSlash = false;
            @params = null;
            fragment = null;

            // check for trivial case
            if(string.IsNullOrEmpty(text)) {
                return false;
            }

            // initialize state and loop over all characters
            var state = State.SchemeFirst;
            string hostnameOrUsername = null;
            string paramsKey = null;
            var segmentList = new List<string>(16);
            List<KeyValuePair<string, string>> paramsList = null;
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
                        if(decode) {
                            password = Decode(password);
                        }
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
                        if(!int.TryParse(text.Substring(last, current - last), out port) || (port < 0) || (port > ushort.MaxValue)) {
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
                case State.QueryStart:
                case State.QueryKey:
                    if(paramsList == null) {
                        paramsList = new List<KeyValuePair<string, string>>(16);
                    }
                    if((c == '&') || (c == '#') || (c == 0)) {
                        if(current != last) {
                            paramsKey = text.Substring(last, current - last);
                            if(decode) {
                                paramsKey = Decode(paramsKey);
                            }
                            paramsList.Add(new KeyValuePair<string, string>(paramsKey, null));
                        }
                        last = current + 1;
                        decode = false;
                        state = (State)c;
                    } else if(c == '=') {
                        paramsKey = text.Substring(last, current - last);
                        if(decode) {
                            paramsKey = Decode(paramsKey);
                        }
                        last = current + 1;
                        decode = false;
                        state = State.QueryValue;
                    } else if(!IsQueryChar(c)) {
                        return false;
                    }
                    break;
                case State.QueryValue:
                    if((c == '&') || (c == '#') || (c == 0)) {
                        var paramsValue = text.Substring(last, current - last);
                        if(decode) {
                            paramsValue = Decode(paramsValue);
                        }
                        paramsList.Add(new KeyValuePair<string, string>(paramsKey, paramsValue));
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
            port = DeterminePort(scheme, port, out usesDefautPort);
            segments = segmentList.ToArray();
            if(paramsList != null) {
                @params = paramsList.ToArray();
            }
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
                ((c >= '$') && (c <= '.')) ||   // one of: $%&'()*+,-.
                (c == '!') || 
                (c == ';') || 
                (c == '=') || 
                (c == '_') || 
                (c == '~');
        }

        private static bool IsPathChar(char c) {

            // Implements: [\w\-\._~%!\$&'\(\)\*\+,;=:@\^\|\[\]{}]

            return (c == '!') ||
                ((c >= '$') && (c <= ';')) ||   // one of: $%&'()*+,-./0123456789:;
                (c == '=') ||
                ((c >= '@') && (c <= '_')) ||   // one of: @ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_
                ((c >= 'a') && (c <= '~'));     // one of: abcdefghijklmnopqrstuvwxyz{|}~
        }

        private static bool IsQueryChar(char c) {

            // Implements: [\w\-\._~%!\$&'\(\)\*\+,;=:@\^/\?|\[\]{}]

            return (c == '!') ||
                ((c >= '$') && (c <= ';')) ||   // one of: $%&'()*+,-./0123456789:;
                (c == '=') ||
                ((c >= '?') && (c <= '_')) ||   // one of: ?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_
                ((c >= 'a') && (c <= '~'));     // one of: abcdefghijklmnopqrstuvwxyz{|}~
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
                ((c >= '#') && (c <= ';')) ||   // one of: #$%&'()*+,-./0123456789:;
                (c == '=') ||
                ((c >= '?') && (c <= '_')) ||   // one of: ?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_
                ((c >= 'a') && (c <= '~'));     // one of: abcdefghijklmnopqrstuvwxyz{|}~
        }

        private static string Decode(string text) {

            // NOTE (steveb): justification for why 'bytes' cannot be longer than 'text';
            //                for ascii characters, we need 1 byte per character
            //                for encoded 8-bit characters (%XX), we need 1-2 byte(s) per 3 characters
            //                for encoded 16-bit characters (%uXXXX), we need 2-4 bytes per 6 characters

            var length = text.Length;
            var bytes = new byte[text.Length];
            var bytesIndex = 0;
            var chars = new char[1];
            for(var textIndex = 0; textIndex < length; textIndex++) {
                var c = text[textIndex];
                switch(c) {
                case '+':
                    bytes[bytesIndex++] = (byte)' ';
                    break;
                case '%':
                    if((textIndex + 2) < length) {
                        int xchar;
                        if((text[textIndex + 1] == 'u') && ((textIndex + 5) < length) && (xchar = GetChar(text, textIndex + 2, 4)) != -1) {
                            chars[0] = (char)xchar;
                            bytesIndex += Encoding.UTF8.GetBytes(chars, 0, 1, bytes, bytesIndex);
                            textIndex += 5;
                            continue;
                        }
                        if((xchar = GetChar(text, textIndex + 1, 2)) != -1) {
                            if(xchar <= 127) {
                                bytes[bytesIndex++] = (byte)xchar;
                            } else {
                                chars[0] = (char)xchar;
                                bytesIndex += Encoding.UTF8.GetBytes(chars, 0, 1, bytes, bytesIndex);
                            }
                            textIndex += 2;
                            continue;
                        }
                    }
                    bytes[bytesIndex++] = (byte)'%';
                    break;
                default:
                    bytes[bytesIndex++] = (byte)c;
                    break;
                }
            }
            return Encoding.UTF8.GetString(bytes, 0, bytesIndex);
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
            return (char)result;
        }

        private static int DeterminePort(string scheme, int port, out bool usesDefault) {
            if(port == -1) {
                var schemeHashCode = INVARIANT_IGNORE_CASE.GetHashCode(scheme);
                if(schemeHashCode == LOCAL_HASHCODE) {

                    // use default port number (-1)
                } else if(schemeHashCode == HTTP_HASHCODE) {
                    port = 80;
                } else if(schemeHashCode == HTTPS_HASHCODE) {
                    port = 443;
                } else if(schemeHashCode == FTP_HASHCODE) {
                    port = 21;
                }
                usesDefault = true;
            } else {
                usesDefault = false;
            }
            return port;
        }
    }
}

