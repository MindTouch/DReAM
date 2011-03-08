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
using System.Xml;

namespace MindTouch.Xml {
    internal class XHtmlTextWriter : XmlTextWriter {

        //--- Class Fields --
        private static readonly string[] _emptyElements = new[] { "area", "atopara", "audioscopebasefont", "base", "br", "choose", "col", "frame", "hr", "img", "isindex", "keygen", "left", "limittext", "link", "meta", "nextid", "of", "over", "param", "range", "right", "spacer", "spot", "tab", "wbr" };
        private static readonly string[] _cdataElements = new[] { "script", "style" };

        //--- Fields ---
        private readonly Stack<string> _elements = new Stack<string>();
        private bool _inAttribute;
        private bool _hasCData;
        private readonly Encoding _encoding;
        private readonly bool _useEntityNames;

        //--- Constructors ---
        public XHtmlTextWriter(string filename, Encoding encoding, bool useEntityNames) : base(filename, encoding) {
            _encoding = encoding;
            _useEntityNames = useEntityNames;
        }

        public XHtmlTextWriter(Stream w, Encoding encoding, bool useEntityNames) : base(w, encoding) {
            _encoding = encoding;
            _useEntityNames = useEntityNames;
        }

        public XHtmlTextWriter(TextWriter w, bool useEntityNames) : base(w) {
            _encoding = w.Encoding;
            _useEntityNames = useEntityNames;
        }

        //--- Methods ---
        public override void WriteStartDocument() {
            return;
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns) {
            if((ns.Length == 0) && (prefix.Length != 0)) {
                prefix = string.Empty;
            }
            _inAttribute = true;
            base.WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteStartElement(string prefix, string localName, string ns) {
            if((ns.Length == 0) && (prefix.Length != 0)) {
                prefix = string.Empty;
            }
            _elements.Push(localName.ToLowerInvariant());
            base.WriteStartElement(prefix, localName, ns);
        }

        public override void WriteFullEndElement() {
            if(_hasCData) {
                _hasCData = false;
                base.WriteRaw("/*]]>*/");
            }
            _elements.Pop();
            base.WriteFullEndElement();
        }

        public override void WriteEndElement() {
            if(_hasCData) {
                _hasCData = false;
                base.WriteRaw("/*]]>*/");
            }
            string element = _elements.Pop();
            if(Array.BinarySearch(_emptyElements, element.ToLowerInvariant()) >= 0) {
                base.WriteEndElement();
            } else {
                base.WriteFullEndElement();
            }
        }

        public override void WriteEndAttribute() {
            _inAttribute = false;
            base.WriteEndAttribute();
        }

        public override void WriteString(string text) {
            if(_inAttribute || (Array.BinarySearch(_cdataElements, _elements.Peek()) < 0)) {
                base.WriteRaw(text.EncodeHtmlEntities(Encoding.ASCII, _useEntityNames));
            } else {

                // write contents of <script> elements inside a fake CDATA section
                if(!_hasCData && !text.StartsWithInvariant("/*<![CDATA[*/")) {
                    _hasCData = true;
                    base.WriteRaw("/*<![CDATA[*/");
                }
                base.WriteRaw(text);
            }
        }

        public override void WriteCData(string text) {
            if(Array.BinarySearch(_cdataElements, _elements.Peek()) < 0) {
                base.WriteCData(text);
            } else {
                base.WriteRaw("/*<![CDATA[*/");
                base.WriteRaw(text);
                base.WriteRaw("/*]]>*/");
            }
        }
    }
}
