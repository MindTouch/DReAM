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
using System.IO;
using System.Text;

namespace MindTouch.Text {
    internal static class EncodingEx {

        //--- Types ---
        private class Meta {
            
            //--- Properties ---
            public string CanonicalName { get; set; }
            public Encoding Encoding { get; set; }
            public bool AsciiSuperset { get; set; }
            public bool IsObscure { get; set; }
            public bool IsShouldNot { get; set; }
            public bool IsLikelyEbcdic { get; set; }
            public Meta ActualHtmlEncoding { get; set; }
        }

        //--- Class Fields ---
        private static readonly Dictionary<int, Meta> _encodingMeta = new Dictionary<int, Meta>();
        private static readonly Dictionary<string, Meta> _encodingByCanonicalName = new Dictionary<string, Meta>();
        private static readonly String[] SHOULD_NOT = { "jisx02121990", "xjis0208" };
        private static readonly String[] BANNED = { "bocu1", "cesu8", "compoundtext",
            "iscii91", "macarabic", "maccentraleurroman", "maccroatian",
            "maccyrillic", "macdevanagari", "macfarsi", "macgreek",
            "macgujarati", "macgurmukhi", "machebrew", "macicelandic",
            "macroman", "macromanian", "macthai", "macturkish", "macukranian",
            "scsu", "utf32", "utf32be", "utf32le", "utf7", "ximapmailboxname",
            "xjisautodetect", "xutf16bebom", "xutf16lebom", "xutf32bebom",
            "xutf32lebom", "xutf16oppositeendian", "xutf16platformendian",
            "xutf32oppositeendian", "xutf32platformendian" };
        private static readonly String[] NOT_OBSCURE = { "big5", "big5hkscs", "eucjp",
            "euckr", "gb18030", "gbk", "iso2022jp", "iso2022kr", "iso88591",
            "iso885913", "iso885915", "iso88592", "iso88593", "iso88594",
            "iso88595", "iso88596", "iso88597", "iso88598", "iso88599",
            "koi8r", "shiftjis", "tis620", "usascii", "utf16", "utf16be",
            "utf16le", "utf8", "windows1250", "windows1251", "windows1252",
            "windows1253", "windows1254", "windows1255", "windows1256",
            "windows1257", "windows1258" };

        //--- Class Contstructor ---
        static EncodingEx() {
            byte[] testBuf = new byte[0x7F];
            for(int i = 0; i < 0x7F; i++) {
                if(IsAsciiSupersetnessSensitive(i)) {
                    testBuf[i] = (byte)i;
                } else {
                    testBuf[i] = 0x20;
                }
            }

            foreach(var info in Encoding.GetEncodings()) {
                var encoding = info.GetEncoding();
                String name = NormalizeName(encoding.WebName);
                String canonicalName = encoding.WebName.ToLowerInvariant();
                if(!IsBanned(name)) {
                    var asciiSuperset = AsciiMapsToBasicLatin(testBuf, encoding);
                    var meta = new Meta {
                        CanonicalName = canonicalName,
                        Encoding = encoding,
                        AsciiSuperset = asciiSuperset,
                        IsObscure = IsObscure(name),
                        IsShouldNot = IsShouldNot(name),
                        IsLikelyEbcdic = IsLikelyEbcdic(name, asciiSuperset)
                    };
                    _encodingMeta.Add(info.CodePage, meta);
                }
            }

            // Overwrite possible overlapping aliases with the real things--just in
            // case
            foreach(var meta in _encodingMeta.Values) {
                _encodingByCanonicalName.Add(NormalizeName(meta.CanonicalName), meta);
            }
            try {
                Find("iso-8859-1").ActualHtmlEncoding = Find("windows-1252");
            } catch { }
            try {
                Find("iso-8859-9").ActualHtmlEncoding = Find("windows-1254");
            } catch { }
            try {
                Find("iso-8859-11").ActualHtmlEncoding = Find("windows-874");
            } catch { }
            try {
                Find("x-iso-8859-11").ActualHtmlEncoding = Find("windows-874");
            } catch { }
            try {
                Find("tis-620").ActualHtmlEncoding = Find("windows-874");
            } catch { }
            try {
                Find("gb_2312-80").ActualHtmlEncoding = Find("gbk");
            } catch { }
            try {
                Find("gb2312").ActualHtmlEncoding = Find("gbk");
            } catch { }
            try {
                _encodingByCanonicalName[NormalizeName("x-x-big5")] = Find("big5");
            } catch { }
            try {
                _encodingByCanonicalName[NormalizeName("euc-kr")] = Find("windows-949");
            } catch { }
            try {
                _encodingByCanonicalName[NormalizeName("ks_c_5601-1987")] = Find("windows-949");
            } catch { }
        }

        //--- Extension Methods ---
        internal static string GetCanonicalName(this Encoding encoding) {
            return _encodingMeta[encoding.CodePage].CanonicalName;
        }

        internal static bool IsAsciiSuperset(this Encoding encoding) {
            return _encodingMeta[encoding.CodePage].AsciiSuperset;
        }

        internal static bool IsObscure(this Encoding encoding) {
            return _encodingMeta[encoding.CodePage].IsObscure;
        }

        internal static bool IsShouldNot(this Encoding encoding) {
            return _encodingMeta[encoding.CodePage].IsShouldNot;
        }

        internal static bool IsRegistered(this Encoding encoding) {
            return !GetCanonicalName(encoding).StartsWithInvariant("x-");
        }

        internal static Encoding GetActualHtmlEncoding(this Encoding encoding) {
            return _encodingMeta[encoding.CodePage].ActualHtmlEncoding.Encoding;
        }

        //--- Class Methods ---
        private static bool AsciiMapsToBasicLatin(byte[] testBuf, Encoding encoding) {
            var reader = new StreamReader(new MemoryStream(testBuf), encoding);
            try {
                for(int i = 0; i < 0x7F; i++) {
                    if(IsAsciiSupersetnessSensitive(i)) {
                        if(reader.Read() != i) {
                            return false;
                        }
                    } else {
                        if(reader.Read() != 0x20) {
                            return false;
                        }
                    }
                }
            } catch(Exception) {
                return false;
            }
            return true;
        }

        private static bool IsAsciiSupersetnessSensitive(int c) {
            return (c >= 0x09 && c <= 0x0D) 
                || (c >= 0x20 && c <= 0x22)
                || (c >= 0x26 && c <= 0x27) 
                || (c >= 0x2C && c <= 0x3F)
                || (c >= 0x41 && c <= 0x5A) 
                || (c >= 0x61 && c <= 0x7A);
        }

        private static bool IsObscure(String lowerCasePreferredIanaName) {
            return !(Array.BinarySearch(NOT_OBSCURE, lowerCasePreferredIanaName) > -1);
        }

        private static bool IsBanned(String lowerCasePreferredIanaName) {
            if(lowerCasePreferredIanaName.StartsWithInvariant("xibm")) {
                return true;
            }
            return Array.BinarySearch(BANNED, lowerCasePreferredIanaName) > -1;
        }

        private static bool IsShouldNot(String lowerCasePreferredIanaName) {
            return Array.BinarySearch(SHOULD_NOT, lowerCasePreferredIanaName) > -1;
        }

        private static bool IsLikelyEbcdic(String canonName, bool asciiSuperset) {
            if(!asciiSuperset) {
                return canonName.StartsWithInvariant("cp") || canonName.StartsWithInvariant("ibm") || canonName.StartsWithInvariant("xibm");
            }
            return false;
        }

        private static String NormalizeName(String str) {
            if(str == null) {
                return null;
            }
            int j = 0;
            char[] buf = new char[str.Length];
            for(int i = 0; i < str.Length; i++) {
                char c = str[i];
                if(c >= 'A' && c <= 'Z') {
                    c = (char)(c + 0x20);
                }
                if(!((c >= '\t' && c <= '\r') || (c >= '\u0020' && c <= '\u002F')
                        || (c >= '\u003A' && c <= '\u0040')
                        || (c >= '\u005B' && c <= '\u0060') || (c >= '\u007B' && c <= '\u007E'))) {
                    buf[j] = c;
                    j++;
                }
            }
            return new String(buf, 0, j);
        }

        private static Meta Find(String name) {
            return _encodingByCanonicalName[NormalizeName(name)];
        }
    }
}
