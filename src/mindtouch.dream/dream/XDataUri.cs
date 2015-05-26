/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
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

namespace MindTouch.Dream {
    public class XDataUri {

        //--- Constants ---
        private const string DATA_SCHEME = "data:";
        private const string BASE64_ENCODING = "base64";

        //--- Class Methods ---
        public static bool TryParse(string source, out XDataUri uri) {
            if(string.IsNullOrEmpty(source)) {
                uri = null;
                return false;
            }

            // check if source starts with "data:"
            if(!source.StartsWithInvariantIgnoreCase(DATA_SCHEME)) {
                uri = null;
                return false;
            }

            // check if source contains a comma
            var comma = source.IndexOf(",", DATA_SCHEME.Length, StringComparison.Ordinal);
            if(comma < 0) {
                uri = null;
                return false;
            }

            // parse key-value pairs between scheme and comma
            string mime = null;
            string charset = null;
            var base64 = false;
            var temp = source.Substring(DATA_SCHEME.Length, comma - DATA_SCHEME.Length);
            foreach(var header in temp.Split(';')) {
                var equal = header.IndexOfInvariant("=");
                string name;
                string value;
                if(equal == -1) {
                    name = string.IsNullOrEmpty(mime) ? "mime" : "base64";
                    value = XUri.Decode(header).Trim();
                } else {
                    name = XUri.Decode(header.Substring(0, equal)).Trim();
                    value = XUri.Decode(header.Substring(equal + 1)).Trim();
                }
                switch(name.ToLowerInvariant()) {
                case "mime":
                    mime = value;
                    break;
                case "charset":
                    charset = value;
                    break;
                case "base64":
                    base64 = value.EqualsInvariantIgnoreCase(BASE64_ENCODING);
                    break;
                default:

                    // NOTE (steveb): we're ignoring additional meta-data since it's not useful for our use case
                    break;
                }
            }
            
            // compute mime-type
            var mimeType = MimeType.TEXT;
            if(!string.IsNullOrEmpty(mime)) {
                mimeType = !string.IsNullOrEmpty(charset) ? new MimeType(string.Format("{0};charset={1}", mime, charset)) : new MimeType(mime);
            }

            // create data uri wrapper
            uri = new XDataUri {
                MimeType = mimeType,
                Base64 = base64
            };
            return true;
        }

        public static XDataUri TryParse(string source) {
            XDataUri result;
            return TryParse(source, out result) ? result : null;
        }

        //--- Properties ---
        public MimeType MimeType { get; private set; }
        public bool Base64 { get; private set; }
    }
}
