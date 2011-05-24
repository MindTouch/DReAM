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
using System.IO;
using System.Text;
using log4net;

namespace MindTouch.Text {
    internal class HtmlMetaEncodingDetector : IEncodingDetector {

        //--- Constants ---
        private const int NO = 0;
        private const int M = 1;
        private const int E = 2;
        private const int T = 3;
        private const int A = 4;
        private const int DATA = 0;
        private const int TAG_OPEN = 1;
        private const int SCAN_UNTIL_GT = 2;
        private const int TAG_NAME = 3;
        private const int BEFORE_ATTRIBUTE_NAME = 4;
        private const int ATTRIBUTE_NAME = 5;
        private const int AFTER_ATTRIBUTE_NAME = 6;
        private const int BEFORE_ATTRIBUTE_VALUE = 7;
        private const int ATTRIBUTE_VALUE_DOUBLE_QUOTED = 8;
        private const int ATTRIBUTE_VALUE_SINGLE_QUOTED = 9;
        private const int ATTRIBUTE_VALUE_UNQUOTED = 10;
        private const int AFTER_ATTRIBUTE_VALUE_QUOTED = 11;
        private const int MARKUP_DECLARATION_OPEN = 13;
        private const int MARKUP_DECLARATION_HYPHEN = 14;
        private const int COMMENT_START = 15;
        private const int COMMENT_START_DASH = 16;
        private const int COMMENT = 17;
        private const int COMMENT_END_DASH = 18;
        private const int COMMENT_END = 19;
        private const int SELF_CLOSING_START_TAG = 20;

        //--- Types ---
        private enum CharSetStates {

            // start charset states
            CHARSET_INITIAL = 0,
            CHARSET_C = 1,
            CHARSET_H = 2,
            CHARSET_A = 3,
            CHARSET_R = 4,
            CHARSET_S = 5,
            CHARSET_E = 6,
            CHARSET_T = 7,
            CHARSET_EQUALS = 8,
            CHARSET_SINGLE_QUOTED = 9,
            CHARSET_DOUBLE_QUOTED = 10,
            CHARSET_UNQUOTED = 11
        } ;

        //--- Class Fields ---
        private static readonly char[] CHARSET = "charset".ToCharArray();
        private static readonly char[] CONTENT = "content".ToCharArray();
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        private static String ExtractCharsetFromContent(string value) {

            // This is a bit ugly. Converting the string to char array in order to
            // make the portability layer smaller.
            CharSetStates charsetState = CharSetStates.CHARSET_INITIAL;
            int start = -1;
            int end = -1;
            for(int i = 0; i < value.Length; i++) {
                char c = value[i];
                switch(charsetState) {
                case CharSetStates.CHARSET_INITIAL:
                    switch(c) {
                    case 'c':
                    case 'C':
                        charsetState = CharSetStates.CHARSET_C;
                        continue;
                    default:
                        continue;
                    }
                case CharSetStates.CHARSET_C:
                    switch(c) {
                    case 'h':
                    case 'H':
                        charsetState = CharSetStates.CHARSET_H;
                        continue;
                    default:
                        charsetState = CharSetStates.CHARSET_INITIAL;
                        continue;
                    }
                case CharSetStates.CHARSET_H:
                    switch(c) {
                    case 'a':
                    case 'A':
                        charsetState = CharSetStates.CHARSET_A;
                        continue;
                    default:
                        charsetState = CharSetStates.CHARSET_INITIAL;
                        continue;
                    }
                case CharSetStates.CHARSET_A:
                    switch(c) {
                    case 'r':
                    case 'R':
                        charsetState = CharSetStates.CHARSET_R;
                        continue;
                    default:
                        charsetState = CharSetStates.CHARSET_INITIAL;
                        continue;
                    }
                case CharSetStates.CHARSET_R:
                    switch(c) {
                    case 's':
                    case 'S':
                        charsetState = CharSetStates.CHARSET_S;
                        continue;
                    default:
                        charsetState = CharSetStates.CHARSET_INITIAL;
                        continue;
                    }
                case CharSetStates.CHARSET_S:
                    switch(c) {
                    case 'e':
                    case 'E':
                        charsetState = CharSetStates.CHARSET_E;
                        continue;
                    default:
                        charsetState = CharSetStates.CHARSET_INITIAL;
                        continue;
                    }
                case CharSetStates.CHARSET_E:
                    switch(c) {
                    case 't':
                    case 'T':
                        charsetState = CharSetStates.CHARSET_T;
                        continue;
                    default:
                        charsetState = CharSetStates.CHARSET_INITIAL;
                        continue;
                    }
                case CharSetStates.CHARSET_T:
                    switch(c) {
                    case '\t':
                    case '\n':
                    case '\u000C':
                    case '\r':
                    case ' ':
                        continue;
                    case '=':
                        charsetState = CharSetStates.CHARSET_EQUALS;
                        continue;
                    default:
                        return null;
                    }
                case CharSetStates.CHARSET_EQUALS:
                    switch(c) {
                    case '\t':
                    case '\n':
                    case '\u000C':
                    case '\r':
                    case ' ':
                        continue;
                    case '\'':
                        start = i + 1;
                        charsetState = CharSetStates.CHARSET_SINGLE_QUOTED;
                        continue;
                    case '\"':
                        start = i + 1;
                        charsetState = CharSetStates.CHARSET_DOUBLE_QUOTED;
                        continue;
                    default:
                        start = i;
                        charsetState = CharSetStates.CHARSET_UNQUOTED;
                        continue;
                    }
                case CharSetStates.CHARSET_SINGLE_QUOTED:
                    switch(c) {
                    case '\'':
                        end = i;
                        goto charsetloop_done;
                    default:
                        continue;
                    }
                    //goto case CharSetStates.CHARSET_DOUBLE_QUOTED;
                case CharSetStates.CHARSET_DOUBLE_QUOTED:
                    switch(c) {
                    case '\"':
                        end = i;
                        goto charsetloop_done;
                    default:
                        continue;
                    }
                    //goto case CharSetStates.CHARSET_UNQUOTED;
                case CharSetStates.CHARSET_UNQUOTED:
                    switch(c) {
                    case '\t':
                    case '\n':
                    case '\u000C':
                    case '\r':
                    case ' ':
                    case ';':
                        end = i;
                        goto charsetloop_done;
                    default:
                        continue;
                    }
                    //break;
                }
            }
            charsetloop_done:
            string charset = null;
            if(start != -1) {
                if(end == -1) {
                    end = value.Length;
                }
                charset = value.Substring(start, end - start);
            }
            return charset;
        }

        //--- Fields ---
        private Encoding characterEncoding;
        protected Stream readable;
        private int metaState = NO;
        private int contentIndex = -1;
        private int charsetIndex = -1;
        protected int stateSave = DATA;
        private int strBufLen;
        private char[] strBuf;
        
        //--- Constructors ---
        public HtmlMetaEncodingDetector() {
            this.readable = null;
            this.metaState = NO;
            this.contentIndex = -1;
            this.charsetIndex = -1;
            this.stateSave = DATA;
            strBufLen = 0;
            strBuf = new char[36];
            this.characterEncoding = null;
        }

        protected bool tryCharset(string encodingName) {
            encodingName = encodingName.ToLowerInvariant();
            try {
                // XXX spec says only UTF-16
                if("utf-16" == encodingName || "utf-16be" == encodingName || "utf-16le" == encodingName || "utf-32" == encodingName || "utf-32be" == encodingName || "utf-32le" == encodingName) {
                    this.characterEncoding = Encoding.UTF8;
                    _log.Warn("The internal character encoding declaration specified \u201C" + encodingName + "\u201D which is not a rough superset of ASCII. Using \u201CUTF-8\u201D instead.");
                    return true;
                }
                Encoding cs = Encoding.GetEncoding(encodingName);
                string canonName = cs.GetCanonicalName();
                if(!cs.IsAsciiSuperset()) {
                    _log.Warn("The encoding \u201C" + encodingName + "\u201D is not an ASCII superset and, therefore, cannot be used in an internal encoding declaration. Continuing the sniffing algorithm.");
                    return false;
                }
                if(!cs.IsRegistered()) {
                    if(encodingName.StartsWithInvariant("x-")) {
                        _log.Warn("The encoding \u201C" + encodingName + "\u201D is not an IANA-registered encoding. (Charmod C022)");
                    } else {
                        _log.Warn("The encoding \u201C" + encodingName + "\u201D is not an IANA-registered encoding and did not use the \u201Cx-\u201D prefix. (Charmod C023)");
                    }
                } else if(!cs.GetCanonicalName().EqualsInvariant(encodingName)) {
                    _log.Warn("The encoding \u201C" + encodingName + "\u201D is not the preferred name of the character encoding in use. The preferred name is \u201C" + canonName + "\u201D. (Charmod C024)");
                }
                if(cs.IsShouldNot()) {
                    _log.Warn("Authors should not use the character encoding \u201C" + encodingName + "\u201D. It is recommended to use \u201CUTF-8\u201D.");
                } else if(cs.IsObscure()) {
                    _log.Warn("The character encoding \u201C" + encodingName + "\u201D is not widely supported. Better interoperability may be achieved by using \u201CUTF-8\u201D.");
                }
                Encoding actual = cs.GetActualHtmlEncoding();
                if(actual == null) {
                    this.characterEncoding = cs;
                } else {
                    _log.Warn("Using \u201C" + actual.GetCanonicalName() + "\u201D instead of the declared encoding \u201C" + encodingName + "\u201D.");
                    this.characterEncoding = actual;
                }
                return true;
            } catch(ArgumentException) {
                _log.Warn("Unsupported character encoding name: \u201C" + encodingName + "\u201D. Will continue sniffing.");
            }
            return false;
        }

        public Encoding Detect(Stream stream) {
            this.readable = stream;
            stateLoop(stateSave);
            return characterEncoding;
        }

        private int read() {
            return readable.ReadByte();
        }

        // WARNING When editing this, makes sure the bytecode length shown by javap
        // stays under 8000 bytes!
        private void stateLoop(int state) {
            int c = -1;
            bool reconsume = false;
            for(; ; ) {
                switch(state) {
                case DATA:
                    for(; ; ) {
                        if(reconsume) {
                            reconsume = false;
                        } else {
                            c = read();
                        }
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case '<':
                            state = TAG_OPEN;
                            goto dataloop_end; // FALL THROUGH continue
                            // stateloop;
                        default:
                            continue;
                        }
                    }
                    dataloop_end:
                    goto case TAG_OPEN;
                case TAG_OPEN:
                    for(; ; ) {
                        c = read();
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case 'm':
                        case 'M':
                            metaState = M;
                            state = TAG_NAME;
                            goto tagopenloop_end;
                        case '!':
                            state = MARKUP_DECLARATION_OPEN;
                            goto stateloop_continue;
                        case '?':
                        case '/':
                            state = SCAN_UNTIL_GT;
                            goto stateloop_continue;
                        case '>':
                            state = DATA;
                            goto stateloop_continue;
                        default:
                            if((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) {
                                metaState = NO;
                                state = TAG_NAME;
                                goto tagopenloop_end;
                            }
                            state = DATA;
                            reconsume = true;
                            goto stateloop_continue;
                        }
                    }
                    tagopenloop_end:
                    // FALL THROUGH DON'T REORDER
                    goto case TAG_NAME;
                case TAG_NAME:
                    for(; ; ) {
                        c = read();
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case ' ':
                        case '\t':
                        case '\n':
                        case '\u000C':
                            state = BEFORE_ATTRIBUTE_NAME;
                            goto tagnameloop_end;
                            // goto stateloop_continue;
                        case '/':
                            state = SELF_CLOSING_START_TAG;
                            goto stateloop_continue;
                        case '>':
                            state = DATA;
                            goto stateloop_continue;
                        case 'e':
                        case 'E':
                            if(metaState == M) {
                                metaState = E;
                            } else {
                                metaState = NO;
                            }
                            continue;
                        case 't':
                        case 'T':
                            if(metaState == E) {
                                metaState = T;
                            } else {
                                metaState = NO;
                            }
                            continue;
                        case 'a':
                        case 'A':
                            if(metaState == T) {
                                metaState = A;
                            } else {
                                metaState = NO;
                            }
                            continue;
                        default:
                            metaState = NO;
                            continue;
                        }
                    }
                    tagnameloop_end:
                    goto case BEFORE_ATTRIBUTE_NAME;
                case BEFORE_ATTRIBUTE_NAME:
                    for(; ; ) {
                        if(reconsume) {
                            reconsume = false;
                        } else {
                            c = read();
                        }
                        /*
                         * Consume the next input character:
                         */
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case ' ':
                        case '\t':
                        case '\n':
                        case '\u000C':
                            continue;
                        case '/':
                            state = SELF_CLOSING_START_TAG;
                            goto stateloop_continue;
                        case '>':
                            state = DATA;
                            goto stateloop_continue;
                        case 'c':
                        case 'C':
                            contentIndex = 0;
                            charsetIndex = 0;
                            state = ATTRIBUTE_NAME;
                            goto beforeattributenameloop_end;
                        default:
                            contentIndex = -1;
                            charsetIndex = -1;
                            state = ATTRIBUTE_NAME;
                            goto beforeattributenameloop_end;
                            // goto stateloop_continue;
                        }
                    }
                    beforeattributenameloop_end:
                    goto case ATTRIBUTE_NAME;
                case ATTRIBUTE_NAME:
                    for(; ; ) {
                        c = read();
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case ' ':
                        case '\t':
                        case '\n':
                        case '\u000C':
                            state = AFTER_ATTRIBUTE_NAME;
                            goto stateloop_continue;
                        case '/':
                            state = SELF_CLOSING_START_TAG;
                            goto stateloop_continue;
                        case '=':
                            strBufLen = 0;
                            state = BEFORE_ATTRIBUTE_VALUE;
                            goto attributenameloop_end;
                            // goto stateloop_continue;
                        case '>':
                            state = DATA;
                            goto stateloop_continue;
                        default:
                            if(metaState == A) {
                                if(c >= 'A' && c <= 'Z') {
                                    c += 0x20;
                                }
                                if(contentIndex == 6) {
                                    contentIndex = -1;
                                } else if(contentIndex > -1
                                          && contentIndex < 6
                                          && (c == CONTENT[contentIndex + 1])) {
                                    contentIndex++;
                                }
                                if(charsetIndex == 6) {
                                    charsetIndex = -1;
                                } else if(charsetIndex > -1
                                          && charsetIndex < 6
                                          && (c == CHARSET[charsetIndex + 1])) {
                                    charsetIndex++;
                                }
                            }
                            continue;
                        }
                    }
                    attributenameloop_end:
                    // FALLTHRU DON'T REORDER
                    goto case BEFORE_ATTRIBUTE_VALUE;
                case BEFORE_ATTRIBUTE_VALUE:
                    for(; ; ) {
                        c = read();
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case ' ':
                        case '\t':
                        case '\n':
                        case '\u000C':
                            continue;
                        case '"':
                            state = ATTRIBUTE_VALUE_DOUBLE_QUOTED;
                            goto beforeattributevalueloop_end;
                            // goto stateloop_continue;
                        case '\'':
                            state = ATTRIBUTE_VALUE_SINGLE_QUOTED;
                            goto stateloop_continue;
                        case '>':
                            state = DATA;
                            goto stateloop_continue;
                        default:
                            if(charsetIndex == 6 || contentIndex == 6) {
                                addToBuffer(c);
                            }
                            state = ATTRIBUTE_VALUE_UNQUOTED;
                            goto stateloop_continue;
                        }
                    }
                    beforeattributevalueloop_end:
                    // FALLTHRU DON'T REORDER
                    goto case ATTRIBUTE_VALUE_DOUBLE_QUOTED;
                case ATTRIBUTE_VALUE_DOUBLE_QUOTED:
                    for(; ; ) {
                        if(reconsume) {
                            reconsume = false;
                        } else {
                            c = read();
                        }
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case '"':
                            if(tryCharset()) {
                                goto stateloop_end;
                            }
                            state = AFTER_ATTRIBUTE_VALUE_QUOTED;
                            goto attributevaluedoublequotedloop_end;
                            // goto stateloop_continue;
                        default:
                            if(metaState == A && (contentIndex == 6 || charsetIndex == 6)) {
                                addToBuffer(c);
                            }
                            continue;
                        }
                    }
                    attributevaluedoublequotedloop_end:
                    // FALLTHRU DON'T REORDER
                    goto case AFTER_ATTRIBUTE_VALUE_QUOTED;
                case AFTER_ATTRIBUTE_VALUE_QUOTED:
                    for(; ; ) {
                        c = read();
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case ' ':
                        case '\t':
                        case '\n':
                        case '\u000C':
                            state = BEFORE_ATTRIBUTE_NAME;
                            goto stateloop_continue;
                        case '/':
                            state = SELF_CLOSING_START_TAG;
                            goto afterattributevaluequotedloop_end;
                            // goto stateloop_continue;
                        case '>':
                            state = DATA;
                            goto stateloop_continue;
                        default:
                            state = BEFORE_ATTRIBUTE_NAME;
                            reconsume = true;
                            goto stateloop_continue;
                        }
                    }
                    afterattributevaluequotedloop_end:
                    // FALLTHRU DON'T REORDER
                    goto case SELF_CLOSING_START_TAG;
                case SELF_CLOSING_START_TAG:
                    c = read();
                    switch(c) {
                    case -1:
                        goto stateloop_end;
                    case '>':
                        state = DATA;
                        goto stateloop_continue;
                    default:
                        state = BEFORE_ATTRIBUTE_NAME;
                        reconsume = true;
                        goto stateloop_continue;
                    }
                    // XXX reorder point
                case ATTRIBUTE_VALUE_UNQUOTED:
                    for(; ; ) {
                        if(reconsume) {
                            reconsume = false;
                        } else {
                            c = read();
                        }
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case ' ':
                        case '\t':
                        case '\n':

                        case '\u000C':
                            if(tryCharset()) {
                                goto stateloop_end;
                            }
                            state = BEFORE_ATTRIBUTE_NAME;
                            goto stateloop_continue;
                        case '>':
                            if(tryCharset()) {
                                goto stateloop_end;
                            }
                            state = DATA;
                            goto stateloop_continue;
                        default:
                            if(metaState == A && (contentIndex == 6 || charsetIndex == 6)) {
                                addToBuffer(c);
                            }
                            continue;
                        }
                    }
                    // XXX reorder point
                case AFTER_ATTRIBUTE_NAME:
                    for(; ; ) {
                        c = read();
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case ' ':
                        case '\t':
                        case '\n':
                        case '\u000C':
                            continue;
                        case '/':
                            if(tryCharset()) {
                                goto stateloop_end;
                            }
                            state = SELF_CLOSING_START_TAG;
                            goto stateloop_continue;
                        case '=':
                            state = BEFORE_ATTRIBUTE_VALUE;
                            goto stateloop_continue;
                        case '>':
                            if(tryCharset()) {
                                goto stateloop_end;
                            }
                            state = DATA;
                            goto stateloop_continue;
                        case 'c':
                        case 'C':
                            contentIndex = 0;
                            charsetIndex = 0;
                            state = ATTRIBUTE_NAME;
                            goto stateloop_continue;
                        default:
                            contentIndex = -1;
                            charsetIndex = -1;
                            state = ATTRIBUTE_NAME;
                            goto stateloop_continue;
                        }
                    }
                    // XXX reorder point
                case MARKUP_DECLARATION_OPEN:
                    for(; ; ) {
                        c = read();
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case '-':
                            state = MARKUP_DECLARATION_HYPHEN;
                            goto markupdeclarationopenloop_end;
                            // goto stateloop_continue;
                        default:
                            state = SCAN_UNTIL_GT;
                            reconsume = true;
                            goto stateloop_continue;
                        }
                    }
                    markupdeclarationopenloop_end:
                    // FALLTHRU DON'T REORDER
                    goto case MARKUP_DECLARATION_HYPHEN;
                case MARKUP_DECLARATION_HYPHEN:
                    for(; ; ) {
                        c = read();
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case '-':
                            state = COMMENT_START;
                            goto markupdeclarationhyphenloop_end;
                            // goto stateloop_continue;
                        default:
                            state = SCAN_UNTIL_GT;
                            reconsume = true;
                            goto stateloop_continue;
                        }
                    }
                    markupdeclarationhyphenloop_end:
                    // FALLTHRU DON'T REORDER
                    goto case COMMENT_START;
                case COMMENT_START:
                    for(; ; ) {
                        c = read();
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case '-':
                            state = COMMENT_START_DASH;
                            goto stateloop_continue;
                        case '>':
                            state = DATA;
                            goto stateloop_continue;
                        default:
                            state = COMMENT;
                            goto commentstartloop_end;
                            // goto stateloop_continue;
                        }
                    }
                    commentstartloop_end:
                    // FALLTHRU DON'T REORDER
                    goto case COMMENT;
                case COMMENT:
                    for(; ; ) {
                        c = read();
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case '-':
                            state = COMMENT_END_DASH;
                            goto commentloop_end;
                            // goto stateloop_continue;
                        default:
                            continue;
                        }
                    }
                    commentloop_end:
                    // FALLTHRU DON'T REORDER
                    goto case COMMENT_END_DASH;
                case COMMENT_END_DASH:
                    for(; ; ) {
                        c = read();
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case '-':
                            state = COMMENT_END;
                            goto commentenddashloop_end;
                            // goto stateloop_continue;
                        default:
                            state = COMMENT;
                            goto stateloop_continue;
                        }
                    }
                    commentenddashloop_end:
                    // FALLTHRU DON'T REORDER
                    goto case COMMENT_END;
                case COMMENT_END:
                    for(; ; ) {
                        c = read();
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case '>':
                            state = DATA;
                            goto stateloop_continue;
                        case '-':
                            continue;
                        default:
                            state = COMMENT;
                            goto stateloop_continue;
                        }
                    }
                    // XXX reorder point
                case COMMENT_START_DASH:
                    c = read();
                    switch(c) {
                    case -1:
                        goto stateloop_end;
                    case '-':
                        state = COMMENT_END;
                        goto stateloop_continue;
                    case '>':
                        state = DATA;
                        goto stateloop_continue;
                    default:
                        state = COMMENT;
                        goto stateloop_continue;
                    }
                    // XXX reorder point
                case ATTRIBUTE_VALUE_SINGLE_QUOTED:
                    for(; ; ) {
                        if(reconsume) {
                            reconsume = false;
                        } else {
                            c = read();
                        }
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case '\'':
                            if(tryCharset()) {
                                goto stateloop_end;
                            }
                            state = AFTER_ATTRIBUTE_VALUE_QUOTED;
                            goto stateloop_continue;
                        default:
                            if(metaState == A && (contentIndex == 6 || charsetIndex == 6)) {
                                addToBuffer(c);
                            }
                            continue;
                        }
                    }
                    // XXX reorder point
                case SCAN_UNTIL_GT:
                    for(; ; ) {
                        if(reconsume) {
                            reconsume = false;
                        } else {
                            c = read();
                        }
                        switch(c) {
                        case -1:
                            goto stateloop_end;
                        case '>':
                            state = DATA;
                            goto stateloop_continue;
                        default:
                            continue;
                        }
                    }
                }
                stateloop_continue:

                // Note (arnec): the below exist so that stateloop_continue has a place to land and there is no warning about an unused variable
                var x = 0;
                x++;
            }
            stateloop_end:
            stateSave = state;
        }

        private void addToBuffer(int c) {
            if(strBufLen == strBuf.Length) {
                char[] newBuf = new char[strBuf.Length + (strBuf.Length << 1)];
                Array.Copy(strBuf, 0, newBuf, 0, strBuf.Length);
                strBuf = newBuf;
            }
            strBuf[strBufLen++] = (char)c;
        }

        private bool tryCharset() {
            if(metaState != A || !(contentIndex == 6 || charsetIndex == 6)) {
                return false;
            }
            string attVal = new string(strBuf, 0, strBufLen);
            string candidateEncoding;
            if(contentIndex == 6) {
                candidateEncoding = ExtractCharsetFromContent(attVal);
            } else {
                candidateEncoding = attVal;
            }
            if(candidateEncoding == null) {
                return false;
            }
            bool success = tryCharset(candidateEncoding);
            contentIndex = -1;
            charsetIndex = -1;
            return success;
        }
    }
}