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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MindTouch.Dream;

namespace MindTouch.Xml {
    public class SimpleHtmlFormatter {

        //--- Class Fields ---
        private static readonly Regex _whitespace = new Regex("[\t ][\t ]", RegexOptions.Compiled);
        private static readonly Regex _paragraphBreaks = new Regex("\n\n+", RegexOptions.Compiled);
        private static readonly string[] _substitutions;

        //--- Class Constructor ---
        static SimpleHtmlFormatter() {
            _substitutions = new[] {
                "\r", "",
                "&", "&amp;",
                "\"", "&quot;",
                "<", "&lt;",
                ">", "&gt;"
            };
        }

        //--- Class Methods ---
        public static XDoc Format(string text) {
            return new SimpleHtmlFormatter(text).ToDocument();
        }

        //--- Fields ---
        private string _text;

        //--- Constructors ---
        private SimpleHtmlFormatter(string text) {
            _text = text ?? "";
            _text = _text.ReplaceAll(_substitutions);
            while(_whitespace.IsMatch(_text)) {
                _text = _whitespace.Replace(_text, "&#160; ");
            }
            FormatBlocks();
        }

        //--- Methods ---
        private void FormatBlocks() {
            var paragraphs = _paragraphBreaks.Split(_text);
            var builder = new StringBuilder();
            foreach(var paragraph in paragraphs.Select(p => p.Replace("\n", "<br />\n"))) {
                builder.AppendLine("<p>" + paragraph + "</p>");
            }
            _text = builder.ToString();
        }

        private XDoc ToDocument() {
            return XDocFactory.From("<html><body>" + _text + "</body></html>", MimeType.XHTML);
        }
    }
}
