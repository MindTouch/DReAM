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
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace MindTouch.Xml {
    public class Html2Text {

        //--- Types ---
        private sealed class VisitState {

            //--- Fields ---
            private readonly StringBuilder _accumulator = new StringBuilder();
            private bool _linefeed;
            public XmlNode BeingFiltered;

            //--- Methods ---
            public void Break() {
                if(!_linefeed && _accumulator.Length > 0) {
                    _accumulator.AppendLine();
                }
                _linefeed = true;
            }

            public void Append(string text) {
                _linefeed = false;
                _accumulator.Append(text);
            }

            public override string ToString() {
                return _accumulator.ToString();
            }
        }

        //--- Class Fields ---
        private static readonly HashSet<string> _inlineElements = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) {
            "A",
            "ABBR",
            "B",
            "BASEFONT",
            "BIG",
            "CITE",
            "CODE",
            "DFN",
            "EM",
            "FONT",
            "I",
            "KBD",
            "S",
            "SAMP",
            "SMALL",
            "SPAN",
            "STRIKE",
            "STRONG",
            "TT",
            "U",
            "VAR"
        };

        private static readonly HashSet<string> _removeElements = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) {
            "APPLET",
            "EMBED",
            "NOSCRIPT",
            "OBJECT",
            "SCRIPT",
            "STYLE"
        };

        //--- Methods ---
        public string Convert(XDoc html) {
            if(html == null || html.IsEmpty) {
                return "";
            }
            var state = new VisitState();
            var body = html.HasName("body") ? html : html["body[not(@target)]"];
            foreach(var node in body.VisitOnly(x => IncludeNode(x, state), x => CheckBlock(x, state))) {
                if(CheckBlock(node, state)) {
                    continue;
                }
                switch(node.AsXmlNode.NodeType) {
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.CDATA:
                case XmlNodeType.Text:
                    state.Append(node.AsText);
                    break;
                }
            }
            return state.ToString().Trim();
        }

        private bool CheckBlock(XDoc node, VisitState state) {
            if(!(node.AsXmlNode is XmlElement)) {
                return false;
            }
            if(node.AsXmlNode != state.BeingFiltered    // if the current node is not being filtered
                && !_inlineElements.Contains(node.Name) // and it's not an inline element
            ) {
                state.Break();
            }
            return true;
        }

        private bool IncludeNode(XDoc node, VisitState state) {

            // node is not a filtered element and does not have the noindex class
            var skip = _removeElements.Contains(node.Name);
            skip = skip || node["@class"].Contents.ContainsInvariant("noindex");
            skip = skip || (node.Name.EqualsInvariantIgnoreCase("div") && node["@class"].Contents.EqualsInvariant("mt-dekiscript-error"));

            // record the node as filtered depending on the include state
            state.BeingFiltered = skip ? node.AsXmlNode : null;
            return !skip;
        }
    }
}
