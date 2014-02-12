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

        // NOTE (steveb): XUriParser parses absolute URIs based on RFC3986 (http://www.ietf.org/rfc/rfc3986.txt), with 
        //                the addition of ^, |, [, ], { and } as a valid character in segments, queries, and fragments; 
        //                and \ as valid segment separator.

        //--- Constants ---
        private const char END_OF_STRING = '\uFFFF';

        //--- Types ---
        private enum State {
            Error = -1,
            End = 0xFFFF,

            // special values assigned to reduce code complexity
            Path = (int)'/',
            PathBackslash = (int)'\\',
            Query = (int)'?',
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
            string[] segments;
            KeyValuePair<string, string>[] @params;
            string fragment;
            if(!TryParse(text, out scheme, out user, out password, out hostname, out port, out usesDefaultPort, out segments, out trailingSlash, out @params, out fragment)) {
                return null;
            }
            return XUri.NewUnsafe(scheme, user, password, hostname, port, usesDefaultPort, segments, trailingSlash, @params, fragment, true);
        }

        public static bool TryParse(string text, out string scheme, out string user, out string password, out string hostname, out int port, out bool usesDefautPort, out string[] segments, out bool trailingSlash, out KeyValuePair<string, string>[] @params, out string fragment) {
            scheme = null;
            user = null;
            password = null;
            hostname = null;
            port = -1;
            usesDefautPort = true;
            segments = new string[0];
            trailingSlash = false;
            @params = null;
            fragment = null;

            // check for trivial case
            if(string.IsNullOrEmpty(text)) {
                return false;
            }
            var length = text.Length;
            var current = 0;
            if((current = TryParseScheme(text, length, current, out scheme)) < 0) {
                return false;
            }
            State nextState;
            if((current = TryParseAuthority(text, length, current, out nextState, out user, out password, out hostname, out port)) < 0) {
                return false;
            }
            if((nextState == State.Path) || (nextState == State.PathBackslash)) {
                if((current = TryParsePath(text, length, current, out nextState, ref trailingSlash, out segments)) < 0) {
                    return false;
                }
            }
            if(nextState == State.Query) {
                if((current = TryParseQuery(text, length, current, out nextState, out @params)) < 0) {
                    return false;
                }
            }
            if(nextState == State.Fragment) {
                if(!TryParseFragment(text, length, current, out fragment)) {
                    return false;
                }
                nextState = State.End;
            }
            if(nextState != State.End) {
                throw new ShouldNeverHappenException();
            }
            port = DeterminePort(scheme, port, out usesDefautPort);
            return true;
        }

        private static int TryParseScheme(string text, int length, int current, out string scheme) {
            var last = current;
            scheme = null;
            var c = text[current++];
            if(!(((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')))) {

                // scheme must begin with alpha character
                return -1;
            }
            for(; current < length; ++current) {
                c = text[current];
                if(((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')) || ((c >= '0') && (c <= '9'))) {

                    // valid character, keep parsing
                } else if(c == ':') {
                    if((length - current >= 3) && (string.CompareOrdinal(text, current + 1, "//", 0, 2) == 0)) {

                        // found "://" sequence at current location, we're done with scheme parsing
                        scheme = text.Substring(last, current - last);
                        return current + 3;
                    }
                    return -1;
                } else {
                    return -1;
                }
            }
            return -1;
        }

        private static int TryParseAuthority(string text, int length, int current, out State nextState, out string user, out string password, out string hostname, out int port) {
            var last = current;
            nextState = State.Error;
            user = null;
            password = null;
            hostname = null;
            port = -1;

            // check first character; it could tell us if we're parsing an IPv6 address
            var decode = false;
            char c;
            if(current < length) {
                c = text[current];
                switch(c) {
                case '%':
                case '+':
                    decode = true;
                    break;
                case '[':
                    goto ipv6;
                }
            } else {

                // use '\uFFFF' as end-of-string marker
                c = END_OF_STRING;
            }

            // parse hostname -OR- user-info
            string hostnameOrUsername;
            for(;;) {
                if(
                    ((c >= 'a') && (c <= 'z')) ||
                    ((c >= 'A') && (c <= 'Z')) ||
                    ((c >= '0') && (c <= '9')) ||
                    ((c >= '$') && (c <= '.')) ||   // one of: $%&'()*+,-.
                    (c == '!') || (c == ';') || (c == '=') || (c == '_') || (c == '~')
                ) {

                    // valid character, keep parsing
                } else if(c == ':') {

                    // part before ':' is either a username or hostname
                    hostnameOrUsername = text.Substring(last, current - last);
                    last = current + 1;
                    goto hostnameOrUserInfoAfterColon;
                } else if(c == '@') {

                    // part before '@' must be username since we didn't find ':'
                    user = text.Substring(last, current - last);
                    if(decode) {
                        user = Decode(user);
                        decode = false;
                    }
                    last = current + 1;
                    goto hostnameOrIPv6Address;
                } else if((c == '/') || (c == '\\') || (c == '?') || (c == '#') || (c == END_OF_STRING)) {

                    // part before '/', '\', '?', '#' must be hostname
                    if(decode) {

                        // hostname cannot contain encoded characters
                        return -1;
                    }
                    hostname = text.Substring(last, current - last);
                    nextState = (State)c;
                    return current + 1;
                } else {
                    return -1;
                }

                // continue on by reading the next character
                ++current;
                if(current < length) {
                    c = text[current];
                    switch(c) {
                    case '%':
                    case '+':
                        decode = true;
                        break;
                    }
                } else {

                    // use '\uFFFF' as end-of-string marker
                    c = END_OF_STRING;
                }
            }
            throw new ShouldNeverHappenException("hostnameOrUsername");

            // parse hostname -OR- user-info AFTER we're parsed a colon (':')
        hostnameOrUserInfoAfterColon:
            for(;;) {
                ++current;
                if(current < length) {
                    c = text[current];
                    switch(c) {
                    case '%':
                    case '+':
                        decode = true;
                        break;
                    }
                } else {

                    // use '\uFFFF' as end-of-string marker
                    c = END_OF_STRING;
                }
                if(
                    ((c >= 'a') && (c <= 'z')) ||
                    ((c >= 'A') && (c <= 'Z')) ||
                    ((c >= '0') && (c <= '9')) ||
                    ((c >= '$') && (c <= '.')) ||   // one of: $%&'()*+,-.
                    (c == '!') || (c == ';') || (c == '=') || (c == '_') || (c == '~')
                ) {

                    // valid character, keep parsing
                } else if(c == '@') {

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
                    goto hostnameOrIPv6Address;
                } else if((c == '/') || (c == '\\') || (c == '?') || (c == '#') || (c == END_OF_STRING)) {

                    // part before ':' was hostname
                    if(decode) {

                        // hostname cannot contain encoded characters
                        return -1;
                    }
                    hostname = hostnameOrUsername;

                    // part after ':' is port, parse and validate it
                    if(!int.TryParse(text.Substring(last, current - last), out port) || (port < 0) || (port > ushort.MaxValue)) {
                        return -1;
                    }
                    nextState = (State)c;
                    return current + 1;
                } else {
                    return -1;
                }
            }
            throw new ShouldNeverHappenException("hostnameOrUserInfoAfterColon");

        hostnameOrIPv6Address:
            ++current;
            if(current < length) {
                c = text[current];
                switch(c) {
                case '%':
                case '+':
                    decode = true;
                    break;
                case '[':

                    // NOTE (steveb): we want to include the leading character in the final result
                    last = current;

                    // IPv6 addresses start with '['
                    goto ipv6;
                }
            } else {

                // use '\uFFFF' as end-of-string marker
                c = END_OF_STRING;
            }
            for(;;) {
                if(
                    ((c >= 'a') && (c <= 'z')) ||
                    ((c >= 'A') && (c <= 'Z')) ||
                    ((c >= '0') && (c <= '9')) ||
                    ((c >= '$') && (c <= '.')) ||   // one of: $%&'()*+,-.
                    (c == '!') || (c == ';') || (c == '=') || (c == '_') || (c == '~')
                ) {

                    // valid character, keep parsing
                } else if(c == ':') {
                    if(decode) {

                        // hostname cannot contain encoded characters
                        return -1;
                    }
                    hostname = text.Substring(last, current - last);
                    last = current + 1;
                    goto portNumber;
                } else if((c == '/') || (c == '\\') || (c == '?') || (c == '#') || (c == END_OF_STRING)) {
                    if(decode) {

                        // hostname cannot contain encoded characters
                        return -1;
                    }
                    hostname = text.Substring(last, current - last);
                    nextState = (State)c;
                    return current + 1;
                } else {
                    return -1;
                }

                // continue on by reading the next character
                ++current;
                if(current < length) {
                    c = text[current];
                    switch(c) {
                    case '%':
                    case '+':
                        decode = true;
                        break;
                    }
                } else {

                    // use '\uFFFF' as end-of-string marker
                    c = END_OF_STRING;
                }
            }
            throw new ShouldNeverHappenException("hostname");

        portNumber:
            for(;;) {
                ++current;
                c = (current < length) ? text[current] : END_OF_STRING;
                if((c >= '0') && (c <= '9')) {

                    // valid character, keep parsing
                } else if((c == '/') || (c == '\\') || (c == '?') || (c == '#') || (c == END_OF_STRING)) {
                    if(!int.TryParse(text.Substring(last, current - last), out port) || (port < 0) || (port > ushort.MaxValue)) {
                        return -1;
                    }
                    nextState = (State)c;
                    return current + 1;
                } else {
                    return -1;
                }
            }
            throw new ShouldNeverHappenException("portNumber");

        ipv6:
            for(;;) {
                ++current;
                c = (current < length) ? text[current] : END_OF_STRING;
                if(((c >= 'a') && (c <= 'f')) || ((c >= 'A') && (c <= 'F')) || ((c >= '0') && (c <= '9')) || (c == ':') || (c == '.')) {

                    // valid character, keep parsing
                } else if(c == ']') {
                    hostname = text.Substring(last, current - last + 1);

                    // check next character to determine correct state to transition to
                    ++current;
                    c = (current < length) ? text[current] : END_OF_STRING;
                    if(c == ':') {
                        last = current + 1;
                        goto portNumber;
                    } else if((c == '/') || (c == '\\') || (c == '?') || (c == '#') || (c == END_OF_STRING)) {
                        nextState = (State)c;
                        return current + 1;
                    } else {
                        return -1;
                    }
                } else {
                    return -1;
                }
            }
            throw new ShouldNeverHappenException("ipv6");
        }

        private static int TryParsePath(string text, int length, int current, out State nextState, ref bool trailingSlash, out string[] segments) {
            nextState = State.Error;
            segments = null;
            var last = current;
            var hasLeadingBackslashes = false;
            var segmentList = new List<string>(16);
            var leading = true;
            char c;
            for(; ; ++current) {
                c = (current < length) ? text[current] : END_OF_STRING;
                if((c == '/') || (c == '\\')) {
                    if(leading) {
                        hasLeadingBackslashes = hasLeadingBackslashes || (c == '\\');
                    } else {
                        var segment = text.Substring(last, current - last);
                        if(hasLeadingBackslashes) {
                            segment = segment.Replace('\\', '/');
                            hasLeadingBackslashes = false;
                        }
                        segmentList.Add(segment);
                        last = current + 1;
                        leading = true;
                    }
                } else if(
                    ((c >= 'a') && (c <= '~')) ||   // one of: abcdefghijklmnopqrstuvwxyz{|}~
                    ((c >= '@') && (c <= '_')) ||   // one of: @ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_
                    ((c >= '$') && (c <= ';')) ||   // one of: $%&'()*+,-./0123456789:;
                    (c == '=') || (c == '!') || char.IsLetter(c)
                ) {

                    // no longer accept leading '/' or '\' characters
                    leading = false;
                } else if((c == '?') || (c == '#') || (c == END_OF_STRING)) {
                    if(last == current) {
                        trailingSlash = true;
                    } else {
                        var segment = text.Substring(last, current - last);
                        if(hasLeadingBackslashes) {
                            segment = segment.Replace('\\', '/');
                        }
                        segmentList.Add(segment);
                    }

                    // we're done parsing the path string
                    break;
                } else {
                    return -1;
                }
            }

            // initialize return values
            segments = segmentList.ToArray();
            nextState = (State)c;
            return current + 1;
        }

        private static int TryParseQuery(string text, int length, int current, out State nextState, out KeyValuePair<string, string>[] @params) {
            nextState = State.Error;
            @params = null;
            var last = current;
            var paramsList = new List<KeyValuePair<string, string>>(16);
            string paramsKey = null;
            var decode = false;
            var parsingKey = true;
            char c;
            for(; ; ++current) {
                if(current < length) {
                    c = text[current];
                    switch(c) {
                    case '%':
                    case '+':
                        decode = true;
                        break;
                    }
                } else {

                    // use '\uFFFF' as end-of-string marker
                    c = END_OF_STRING;
                }
                if(
                    ((c >= 'a') && (c <= '~')) || // one of: abcdefghijklmnopqrstuvwxyz{|}~
                    ((c >= '?') && (c <= '_')) || // one of: ?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_
                    ((c >= '\'') && (c <= ';')) || // one of: '()*+,-./0123456789:;
                    (c == '$') || (c == '%') || (c == '!') || char.IsLetter(c)
                ) {

                    // valid character, keep parsing
                } else if((c == '&') || (c == '#') || (c == END_OF_STRING)) {
                    if(parsingKey) {
                        if(current != last) {

                            // add non-empty key with empty value
                            paramsKey = text.Substring(last, current - last);
                            if(decode) {
                                paramsKey = Decode(paramsKey);
                                decode = false;
                            }
                            paramsList.Add(new KeyValuePair<string, string>(paramsKey, null));
                        } else if(c == '&') {

                            // this occurs in the degenerate case of two consecutive ampersands (e.g. "&&")
                            paramsList.Add(new KeyValuePair<string, string>("", null));
                        }
                    } else {

                        // add key with value
                        var paramsValue = text.Substring(last, current - last);
                        if(decode) {
                            paramsValue = Decode(paramsValue);
                            decode = false;
                        }
                        paramsList.Add(new KeyValuePair<string, string>(paramsKey, paramsValue));
                        parsingKey = true;
                    }

                    // check if we found a query parameter separator
                    if(c == '&') {
                        last = current + 1;
                        continue;
                    }

                    // we're done parsing the query string
                    break;
                } else if(c == '=') {
                    if(parsingKey) {
                        paramsKey = text.Substring(last, current - last);
                        if(decode) {
                            paramsKey = Decode(paramsKey);
                            decode = false;
                        }
                        last = current + 1;
                        parsingKey = false;
                    }
                } else {
                    return -1;
                }
            }

            // initialize return values
            nextState = (State)c;
            @params = paramsList.ToArray();
            return current + 1;
        }

        private static bool TryParseFragment(string text, int length, int current, out string fragment) {
            fragment = null;
            var last = current;
            var decode = false;
            for(;; ++current) {
                char c;
                if(current < length) {
                    c = text[current];
                    switch(c) {
                    case '%':
                    case '+':
                        decode = true;
                        break;
                    }
                } else {
                    fragment = text.Substring(last, current - last);
                    if(decode) {
                        fragment = Decode(fragment);
                    }
                    return true;
                }
                if(IsFragmentChar(c)) {

                    // valid character, keep parsing
                } else {
                    return false;
                }
            }
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

            return
                ((c >= 'a') && (c <= '~')) ||   // one of: abcdefghijklmnopqrstuvwxyz{|}~
                ((c >= '?') && (c <= '_')) ||   // one of: ?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_
                ((c >= '#') && (c <= ';')) ||   // one of: #$%&'()*+,-./0123456789:;
                (c == '=') ||
                (c == '!') ||
                char.IsLetter(c);
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
                    char next;
                    if(((textIndex + 2) < length) && ((next = text[textIndex + 1]) != '%')) {
                        int xchar;
                        if(next == 'u') {
                            if(((textIndex + 5) < length) && (xchar = GetChar(text, textIndex + 2, 4)) != -1) {
                                chars[0] = (char)xchar;
                                bytesIndex += Encoding.UTF8.GetBytes(chars, 0, 1, bytes, bytesIndex);
                                textIndex += 5;
                                continue;
                            }
                        } else if((xchar = GetChar(text, textIndex + 1, 2)) != -1) {
                            bytes[bytesIndex++] = (byte)xchar;
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
            return result;
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

