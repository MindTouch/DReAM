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

using System.IO;

using MindTouch.IO;
using MindTouch.Xml;

namespace MindTouch.Dream.IO {
    internal interface IDreamResponseFormatter {

        //--- Methods ---
        MimeType GetContentType(XDoc doc);
        Stream Format(XDoc doc);
    }

    internal class DreamResponseXHtmlFormatter : IDreamResponseFormatter {

        //--- Methods ---
        public MimeType GetContentType(XDoc doc) {
            return MimeType.XHTML;
        }

        public Stream Format(XDoc doc) {
        	
        	// TODO (steveb): convert XML to XHTML without generating a string first
            var bytes = MimeType.XHTML.CharSet.GetBytes(doc.ToXHtml());
            return new MemoryStream(bytes);
        }
    }

    internal class DreamResponseXSpanFormatter : IDreamResponseFormatter {

        //--- Methods ---
        public MimeType GetContentType(XDoc doc) {
            return MimeType.HTML;
        }

        public Stream Format(XDoc doc) {
        	
        	// TODO (steveb): convert XML to XSPAN without generating a string first
            var bytes = MimeType.HTML.CharSet.GetBytes(XSpanUtil.ToXSpan(doc).Contents);
            return new MemoryStream(bytes);
        }
    }

    internal class DreamResponseJsonFormatter : IDreamResponseFormatter {

        //--- Fields ---
        private string _callback;
        private string _prefix;
        private string _postfix;

        //--- Constructors ---
        public DreamResponseJsonFormatter(string callback, string prefix, string postfix) {
            _callback = callback;
            _prefix = prefix;
            _postfix = postfix;
        }

        //--- Methods ---
        public MimeType GetContentType(XDoc doc) {
            return MimeType.JSON;
        }

        public Stream Format(XDoc doc) {
        	
        	// TODO (steveb): convert XML to JSON without generating a string first
            byte[] bytes;
            if(!string.IsNullOrEmpty(_callback)) {
                bytes = MimeType.JSON.CharSet.GetBytes(string.Format("{0}{1}({2});{3}", _prefix, _callback, JsonUtil.ToJson(doc), _postfix));
            } else {
                bytes = MimeType.JSON.CharSet.GetBytes(_prefix + JsonUtil.ToJson(doc) + _postfix);
            }
            return new MemoryStream(bytes);
        }
    }

    internal class DreamResponseJsonpFormatter : IDreamResponseFormatter {

        //--- Fields ---
        private string _prefix;

        //--- Constructors ---
        public DreamResponseJsonpFormatter(string prefix) {
            _prefix = prefix;
        }

        //--- Methods ---
        public MimeType GetContentType(XDoc doc) {
            return MimeType.JSON;
        }

        public Stream Format(XDoc doc) {        	
        	
        	// TODO (steveb): convert XML to JSONP without generating a string first
            var bytes = MimeType.JSON.CharSet.GetBytes(_prefix + JsonUtil.ToJsonp(doc));
            return new MemoryStream(bytes);
        }
    }

    internal class DreamResponsePhpFormatter : IDreamResponseFormatter {

        //--- Methods ---
        public MimeType GetContentType(XDoc doc) {
            return MimeType.PHP;
        }

        public Stream Format(XDoc doc) {
            var stream = new MemoryStream();
            PhpUtil.WritePhp(doc, stream, MimeType.PHP.CharSet);
            stream.Position = 0;
            return stream;
        }
    }

    internal class DreamResponseVersitFormatter : IDreamResponseFormatter {

        //--- Methods ---
        public MimeType GetContentType(XDoc doc) {
            return MimeType.TEXT;
        }

        public Stream Format(XDoc doc) {
        	
        	// TODO (steveb): convert XML to VERSIT without generating a string first
            var bytes = MimeType.TEXT.CharSet.GetBytes(VersitUtil.ToVersit(doc));
            return new MemoryStream(bytes);
        }
    }
}
