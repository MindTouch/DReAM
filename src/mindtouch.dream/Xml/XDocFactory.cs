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
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

using MindTouch.Dream;
using MindTouch.IO;

namespace MindTouch.Xml {

    /// <summary>
    /// Provides a static factory methods for creating <see cref="XDoc"/> instances.
    /// </summary>
    public static class XDocFactory {

        //--- Class Fields ---
        private static Sgml.SgmlDtd _dtd;

        //--- Class Methods ---

        /// <summary>
        /// Load a document from a file.
        /// </summary>
        /// <param name="filename">Path to document.</param>
        /// <param name="mime">Document mime-type.</param>
        /// <returns>New document instance.</returns>
        public static XDoc LoadFrom(string filename, MimeType mime) {
            if(string.IsNullOrEmpty(filename)) {
                throw new ArgumentNullException("filename");
            }
            if(mime == null) {
                throw new ArgumentNullException("mime");
            }
            using(TextReader reader = new StreamReader(filename, mime.CharSet, true)) {
                return From(reader, mime);
            }
        }

        /// <summary>
        /// Create a document from a stream.
        /// </summary>
        /// <param name="stream">Document stream.</param>
        /// <param name="mime">Document mime-type.</param>
        /// <returns>New document instance.</returns>
        public static XDoc From(Stream stream, MimeType mime) {
            if(stream == null) {
                throw new ArgumentNullException("stream");
            }
            if(mime == null) {
                throw new ArgumentNullException("mime");
            }
            if(!stream.CanSeek && !(stream is BufferedStream)) {
                stream = new BufferedStream(stream);
            }
            using(TextReader reader = new StreamReader(stream, mime.CharSet)) {
                return From(reader, mime);
            }
        }

        /// <summary>
        /// Create a document from an xml string.
        /// </summary>
        /// <param name="value">Document string.</param>
        /// <param name="mime">Document mime-type.</param>
        /// <returns>New document instance.</returns>
        public static XDoc From(string value, MimeType mime) {
            if(value == null) {
                throw new ArgumentNullException("value");
            }
            if(mime == null) {
                throw new ArgumentNullException("mime");
            }
            return From(new TrimmingStringReader(value), mime);
        }

        /// <summary>
        /// Create a document by serializing an object to xml.
        /// </summary>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="serializer">Xml serializer instance.</param>
        /// <returns>New document instance.</returns>
        public static XDoc From(object obj, XmlSerializer serializer) {

            // BUG #831: there has to be a more efficient way to convert an object to an xdoc

            // serialize the object to a string
            var writer = new StringWriter();
            serializer.Serialize(writer, obj);

            // load the string into an xml document
            XmlDocument doc = XDoc.NewXmlDocument();
            doc.LoadXml(writer.ToString());
            if(doc.DocumentElement == null) {
                return XDoc.Empty;
            }

            // convert the xml document into an xdoc
            var result = new XDoc(doc);
            return result;
        }

        /// <summary>
        /// Create a document from a text reader.
        /// </summary>
        /// <param name="reader">Document text reader.</param>
        /// <param name="mime">Document mime-type.</param>
        /// <returns>New document instance.</returns>
        public static XDoc From(TextReader reader, MimeType mime) {
            if(reader == null) {
                throw new ArgumentNullException("reader");
            }
            if(mime == null) {
                throw new ArgumentNullException("mime");
            }
            if(mime.Match(MimeType.VCAL)) {
                return VersitUtil.FromVersit(reader, "vcal");
            }
            if(mime.Match(MimeType.VERSIT)) {
                return VersitUtil.FromVersit(reader, "vcard");
            }
            if(mime.Match(MimeType.HTML)) {
                return FromHtml(reader);
            }
            if(mime.IsXml) {
                return FromXml(reader);
            }
            if(mime.Match(MimeType.FORM_URLENCODED)) {
                return XPostUtil.FromXPathValuePairs(XUri.ParseParamsAsPairs(reader.ReadToEnd()), "form");
            }
            throw new ArgumentException("unsupported mime-type: " + mime.FullType, "mime");
        }

        /// <summary>
        /// Create a document from a collection of key/value pairs
        /// </summary>
        /// <param name="values">Enumeration of key/value pairs.</param>
        /// <param name="root">Name of the root element for document.</param>
        /// <returns></returns>
        public static XDoc From(IEnumerable<KeyValuePair<string, string>> values, string root) {
            if(values == null) {
                throw new ArgumentNullException("values");
            }
            return XPostUtil.FromXPathValuePairs(values, root);
        }

        private static XDoc FromXml(TextReader reader) {

            // check if reader is a StringReader, so that we can use an optimized trimming reader for it
            if(!reader.IsTrimmingReader()) {
                reader = new TrimmingTextReader(reader);
            }

            // check if stream is either empty or does not start with a legal symbol
            int peek = reader.Peek();
            if((peek == -1) || (peek != '<')) {
                return XDoc.Empty;
            }
            try {
                XmlDocument doc = XDoc.NewXmlDocument();
                doc.Load(reader);
                if(doc.DocumentElement == null) {
                    return XDoc.Empty;
                }
                return new XDoc(doc);
            } catch {
                return XDoc.Empty;
            }
        }

        private static XDoc FromHtml(TextReader reader) {
            Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader(XDoc.XmlNameTable) {
                Dtd = _dtd,
                DocType = "HTML",
                WhitespaceHandling = WhitespaceHandling.All,
                CaseFolding = Sgml.CaseFolding.ToLower,
                InputStream = reader
            };
            try {
                XmlDocument doc = XDoc.NewXmlDocument();
                doc.Load(sgmlReader);
                if(doc.DocumentElement == null) {
                    return XDoc.Empty;
                }
                if(_dtd == null) {
                    _dtd = sgmlReader.Dtd;
                }
                return new XDoc(doc);
            } catch(Exception) {
                return XDoc.Empty;
            }
        }
    }

    /// <summary>
    /// Provides static helper methods for working with <see cref="XDoc"/>.
    /// </summary>
    public static class XDocUtil {

        //--- Class Fields ---
        private static readonly Regex _xmlDateTimeRex = new Regex(@"^(\d{4})-(\d{1,2})-(\d{1,2})T(\d{1,2}):(\d{2}):(\d{2})(\.(\d{2,7}))?Z$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Methods ---

        /// <summary>
        /// Try to parse an xml datetime string.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <param name="date">Output of parsed Datetime.</param>
        /// <returns><see langword="True"/> if a <see cref="DateTime"/> instance was successfully parsed.</returns>
        public static bool TryParseXmlDateTime(string value, out DateTime date) {
            date = DateTime.MinValue;
            if(_xmlDateTimeRex.Match(value).Success) {
                try {
                    date = DateTimeUtil.ParseExactInvariant(value, "yyyy-MM-dd\\THH:mm:ssZ");
                    return true;
                } catch { }
            }
            return false;
        }

        /// <summary>
        /// Encode a string into xml-safe form.
        /// </summary>
        /// <param name="text">String to encode.</param>
        /// <returns>Encoded string.</returns>
        public static string EncodeXmlString(string text) {

            // check if text is in date format
            DateTime date;
            if(TryParseXmlDateTime(text, out date)) {

                // convert text to RFC1123 formatted date
                text = date.ToUniversalTime().ToString("R");
            }

            // escape any special characters
            return "\"" + text.EscapeString() + "\"";
        }

        /// <summary>
        /// Collapse a list of <see cref="XmlNode"/> instances into a dictionary of node name and matching nodes.
        /// </summary>
        /// <param name="nodes">Enumeration of <see cref="XmlNode"/>.</param>
        /// <returns>Dictionary indexed by XmlNode names containing all nodes for that name.</returns>
        public static IDictionary<string, List<XmlNode>> CollapseNodeList(System.Collections.IEnumerable nodes) {
            IDictionary<string, List<XmlNode>> result = new SortedDictionary<string, List<XmlNode>>(StringComparer.Ordinal);
            foreach(XmlNode node in nodes) {
                List<XmlNode> list;
                switch(node.NodeType) {
                case XmlNodeType.Element:
                case XmlNodeType.Text:
                    if(!result.TryGetValue(node.Name, out list)) {
                        list = new List<XmlNode>();
                        result.Add(node.Name, list);
                    }
                    list.Add(node);
                    break;
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Provides static helper functions for creating Xml documents from Versit documents (VCal, VCard).
    /// </summary>
    public static class VersitUtil {

        //--- Constants ---

        /// <summary>
        /// Max line length for Versit format, 76.
        /// </summary>
        public const int MAX_VERSIT_LINE_LENGTH = 76;

        //--- Class Fields ---
        private static readonly Regex _versitDateTimeRex = new Regex(@"^(\d{4})(\d{1,2})(\d{1,2})T(\d{1,2})(\d{2})(\d{2})Z$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Methods ---

        /// <summary>
        /// Convert a versit based xml document to the versit string serialization.
        /// </summary>
        /// <param name="doc">Source document.</param>
        /// <returns>Versit string serialization.</returns>
        public static string ToVersit(XDoc doc) {
            if(doc.IsEmpty) {
                return "\r\n";
            }
            StringBuilder result = new StringBuilder();
            foreach(XmlNode node in doc.Root.AsXmlNode.ChildNodes) {
                WriteVersit(node, result);
            }
            return result.ToString();
        }

        /// <summary>
        /// Parse a Versit string into an Xml document.
        /// </summary>
        /// <param name="versit">Versit string.</param>
        /// <param name="root">Name to use as thexml document root node.</param>
        /// <returns>Xml document instance.</returns>
        public static XDoc FromVersit(string versit, string root) {
            using(StringReader reader = new StringReader(versit)) {
                return FromVersit(reader, root);
            }
        }

        /// <summary>
        /// Read a Versit string from a text reader and parse it into an Xml document.
        /// </summary>
        /// <param name="reader">Source reader.</param>
        /// <param name="root">Name to use as thexml document root node.</param>
        /// <returns>Xml document instance.</returns>
        public static XDoc FromVersit(TextReader reader, string root) {
            string value = reader.ReadToEnd().Trim();
            XDoc result = new XDoc(string.IsNullOrEmpty(root) ? "root" : root);

            // NOTE (steveb): the versit format is "KEY;ATTR_KEY=ATTR_VALUE;ATTR_KEY=ATTR_VALUE:VALUE\r\n"

            // join line continuations
            value = value.Replace("\r\n ", "");

            // split lines
            string[] lines = value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // process each line
            foreach(string line in lines) {
                if(line.StartsWithInvariant("BEGIN:")) {
                    result.Start(line.Substring(6).ToLowerInvariant());
                } else if(line.StartsWithInvariant("END:")) {
                    result.End();
                } else {
                    string[] pair = line.Split(new[] { ':' }, 2);
                    string[] attrs = pair[0].Split(';');
                    result.Start(attrs[0].ToLowerInvariant());
                    for(int i = 1; i < attrs.Length; ++i) {
                        string[] attr = attrs[i].Split(new[] { '=' }, 2);
                        result.Attr(attr[0].ToLowerInvariant(), (attr.Length > 1) ? DecodeVersitString(attr[1]) : string.Empty);
                    }
                    if(pair.Length > 1) {
                        result.Value(DecodeVersitString(pair[1]));
                    }
                    result.End();
                }
            }
            return result;
        }

        private static void WriteVersit(XmlNode node, StringBuilder text) {
            switch(node.NodeType) {
            case XmlNodeType.Document:
                WriteVersit(((XmlDocument)node).DocumentElement, text);
                break;
            case XmlNodeType.Element:
                if((node.ChildNodes.Count == 0) || (node.InnerText.Trim() == string.Empty)) {

                    // ignore empty nodes
                } else if((node.ChildNodes.Count == 1) && (node.ChildNodes[0].NodeType == XmlNodeType.Text)) {
                    string attrs = string.Empty;
                    foreach(XmlNode attr in node.Attributes) {
                        attrs += string.Format(";{0}={1}", attr.Name.ToUpperInvariant(), EncodeVersitString(attr.Value));
                    }
                    AppendLineVersit(text, string.Format("{0}{1}:{2}", node.Name.ToUpperInvariant(), attrs, EncodeVersitString(ConvertDateTimeVersit(node.InnerText))));
                } else {
                    string tag = node.Name.ToUpperInvariant();
                    AppendLineVersit(text, string.Format("BEGIN:{0}", tag));
                    foreach(XmlNode subnode in node.ChildNodes) {
                        WriteVersit(subnode, text);
                    }
                    AppendLineVersit(text, string.Format("END:{0}", tag));
                }
                break;
            }
        }

        private static string DecodeVersitString(string text) {

            // escape any special characters
            StringBuilder acc = new StringBuilder(2 * text.Length);
            bool slash = false;
            foreach(char c in text) {
                switch(c) {
                case '\\':
                    slash = true;
                    break;
                case (char)0xA0:
                    acc.Append(" ");
                    break;
                default:
                    if(slash) {
                        switch(c) {
                        case 'n':
                            acc.Append("\n");
                            break;
                        case '\\':
                            acc.Append("\\");
                            break;
                        }
                        slash = false;
                    } else
                        acc.Append(c);
                    break;
                }
            }
            string result = acc.ToString();

            // check if text is VERST date-time string
            if(_versitDateTimeRex.Match(result).Success) {
                try {
                    DateTime date = DateTimeUtil.ParseExactInvariant(result, "yyyyMMdd\\THHmmssZ");
                    result = date.ToUniversalTime().ToString(XDoc.RFC_DATETIME_FORMAT);
                } catch { }
            }
            return result;
        }

        private static string EncodeVersitString(string text) {

            // escape any special characters
            StringBuilder result = new StringBuilder(2 * text.Length);
            foreach(char c in text) {
                switch(c) {
                case '\n':
                    result.Append("\\n");
                    break;
                case '\r':
                    break;
                case '\\':
                    result.Append("\\\\");
                    break;
                case (char)0xA0:
                    result.Append(" ");
                    break;
                default:
                    result.Append(c);
                    break;
                }
            }
            return result.ToString();
        }

        private static void AppendLineVersit(StringBuilder text, string value) {
            if(value.Length > MAX_VERSIT_LINE_LENGTH) {
                while(value.Length > MAX_VERSIT_LINE_LENGTH) {
                    int cut = MAX_VERSIT_LINE_LENGTH;
                    if((value[cut] == '\\') && (value[cut - 1] == '\\')) {
                        --cut;
                    }
                    text.Append(value.Substring(0, cut));
                    text.Append("\r\n");
                    value = " " + value.Substring(cut);
                }
            } else {
                text.Append(value);
                text.Append("\r\n");
            }
        }

        private static string ConvertDateTimeVersit(string text) {

            // check if text is in date format
            DateTime date;
            if(XDocUtil.TryParseXmlDateTime(text, out date)) {
                text = date.ToUniversalTime().ToString("yyyyMMdd\\THHmmssZ");
            }
            return text;
        }
    }


    /// <summary>
    /// Provides extension methods for converting Xml into Json.
    /// </summary>
    public static class JsonUtil {

        //--- Extension Methods ---

        /// <summary>
        /// Convert an xml document into a Json string.
        /// </summary>
        /// <param name="doc">Document to convert.</param>
        /// <returns>Json string.</returns>
        public static string ToJson(this XDoc doc) {
            if(doc.IsEmpty) {
                return "{}";
            }
            StringWriter result = new StringWriter();
            WriteJson(doc.Root.AsXmlNode, result);
            return result.ToString();
        }

        /// <summary>
        /// Convert an xml document into a Json-p string.
        /// </summary>
        /// <param name="doc">Document to convert.</param>
        /// <returns>Json string.</returns>
        public static string ToJsonp(this XDoc doc) {
            if(doc.IsEmpty) {
                return "({})";
            }
            StringWriter result = new StringWriter();
            result.Write("({");
            XmlNode[] nodes = new XmlNode[doc.ListLength];
            int index = 0;
            foreach(XDoc item in doc) {
                nodes[index++] = item.AsXmlNode;
            }
            WriteJson(nodes, result);
            result.Write("})");
            return result.ToString();
        }

        //--- Class Methods ---
        private static void WriteJson(XmlNode node, TextWriter writer) {
            bool first = true;
            switch(node.NodeType) {
            case XmlNodeType.Document:
                writer.Write("{");
                WriteJson(XDoc.NewListXmlNode(node.ChildNodes), writer);
                writer.Write("}");
                break;
            case XmlNodeType.Element:

                // check if we have the common case of a simple node (tag and text only without attributes, or tag only without text or attributes)
                if((node.Attributes.Count == 0) && (node.ChildNodes.Count == 0)) {
                    writer.Write("\"\"");
                } else if((node.Attributes.Count > 0) || (node.ChildNodes.Count != 1) || (node.ChildNodes[0].NodeType != XmlNodeType.Text)) {
                    writer.Write("{");
                    foreach(XmlNode sub in node.Attributes) {
                        if(!first) {
                            writer.Write(",");
                        }
                        WriteJson(sub, writer);
                        first = false;
                    }
                    if(!first && (node.ChildNodes.Count > 0)) {
                        writer.Write(",");
                    }
                    WriteJson(XDoc.NewListXmlNode(node.ChildNodes), writer);
                    writer.Write("}");
                } else {
                    WriteJson(node.ChildNodes[0], writer);
                }
                break;
            case XmlNodeType.Text:
                writer.Write(EncodeString(node.Value));
                break;
            case XmlNodeType.Attribute:
                writer.Write("{0}:{1}", EncodeString("@" + node.Name), EncodeString(node.Value));
                break;
            }
        }

        private static void WriteJson(XmlNode[] nodes, TextWriter writer) {
            IDictionary<string, List<XmlNode>> elems = XDocUtil.CollapseNodeList(nodes);
            bool firstOuter = true;
            foreach(KeyValuePair<string, List<XmlNode>> entry in elems) {
                if(!firstOuter) {
                    writer.Write(",");
                }
                writer.Write("{0}:", XDocUtil.EncodeXmlString(entry.Key));
                if(entry.Value.Count > 1) {
                    writer.Write("[");
                    bool firstInner = true;
                    foreach(XmlNode node in entry.Value) {
                        if(!firstInner) {
                            writer.Write(",");
                        }
                        if(node.NodeType == XmlNodeType.Text) {
                            writer.Write(XDocUtil.EncodeXmlString(node.Value));
                        } else {
                            WriteJson(node, writer);
                        }
                        firstInner = false;
                    }
                    writer.Write("]");
                } else {
                    WriteJson(entry.Value[0], writer);
                }
                firstOuter = false;
            }
        }

        private static string EncodeString(string text) {

            // check if text is in date format
            DateTime date;
            if(XDocUtil.TryParseXmlDateTime(text, out date)) {

                // convert text to RFC1123 formatted date
                text = date.ToUniversalTime().ToString("R");
            }

            // escape any special characters
            return "\"" + EscapeString(text) + "\"";
        }

        private static string EscapeString(string text) {

            // Note (arnec): This is a copy of StringUtil.EscapeString with rules adjusted according to json.org

            if(string.IsNullOrEmpty(text)) {
                return string.Empty;
            }

            // escape any special characters
            StringBuilder result = new StringBuilder(2 * text.Length);
            foreach(char c in text) {
                switch(c) {
                case '\b':
                    result.Append("\\b");
                    break;
                case '\f':
                    result.Append("\\f");
                    break;
                case '\n':
                    result.Append("\\n");
                    break;
                case '\r':
                    result.Append("\\r");
                    break;
                case '\t':
                    result.Append("\\t");
                    break;
                case '"':
                    result.Append("\\\"");
                    break;
                case '\\':
                    result.Append("\\\\");
                    break;
                default:
                    if((c < 32) || (c >= 127)) {
                        result.Append("\\u");
                        result.Append(((int)c).ToString("x4"));
                    } else {
                        result.Append(c);
                    }
                    break;
                }
            }
            return result.ToString();
        }
    }

    /// <summary>
    /// Provides static helpers for converting regular Xml documents to and from XSpan format.
    /// </summary>
    public static class XSpanUtil {

        //--- Class Methods ---

        /// <summary>
        /// Convert an XSpan document to a regular Xml document.
        /// </summary>
        /// <param name="xspan">XSpan document.</param>
        /// <returns>New Xml document instance.</returns>
        public static XDoc FromXSpan(XDoc xspan) {
            XDoc xml = new XDoc(xspan["@class"].Contents);
            foreach(XDoc attr in xspan["@*"]) {
                if(attr.Name.StartsWithInvariant("xml:"))
                    xml.Attr(attr.Name.Substring("xml:".Length), attr.Contents);
            }
            string value = xspan["text()"].Contents;
            if(value.Length != 0)
                xml.Value(value);
            foreach(XDoc child in xspan.Elements)
                xml.Add(FromXSpan(child));
            return xml;
        }

        /// <summary>
        /// Convert an Xml document to XSpan format.
        /// </summary>
        /// <param name="doc">Xml document to be converted.</param>
        /// <returns>XSpan xml document.</returns>
        public static XDoc ToXSpan(XDoc doc) {
            if(doc.IsEmpty) {
                return XDoc.Empty;
            }
            XDoc result = new XDoc("data");
            WriteXSpan(doc.Root.AsXmlNode, result);
            return result;
        }

        private static void WriteXSpan(XmlNode node, XDoc output) {
            switch(node.NodeType) {
            case XmlNodeType.Document:
                WriteXSpan(((XmlDocument)node).DocumentElement, output);
                break;
            case XmlNodeType.Element:
                XDoc childOutput = output.Start("span");
                childOutput.Attr("class", node.Name);
                foreach(XmlNode attr in node.Attributes) {
                    output.Attr("xml:" + attr.Name, attr.Value);
                }
                foreach(XmlNode child in node.ChildNodes) {
                    WriteXSpan(child, output);
                }
                output.End();
                break;
            case XmlNodeType.Text:
                output.Value(node.Value);
                break;
            }
        }
    }

    /// <summary>
    /// Provides static helpers for converting between a key/value POST argument body and an Xml representation.
    /// </summary>
    public static class XPostUtil {

        //--- Class Methods ---

        /// <summary>
        /// Convert a key/value pair into an xml document.
        /// </summary>
        /// <param name="enumerator">Enumerable of key value pairs.</param>
        /// <param name="root">Name of the root node of the output document.</param>
        /// <returns>New Xml document.</returns>
        public static XDoc FromXPathValuePairs(IEnumerable<KeyValuePair<string, string>> enumerator, string root) {
            XDoc doc = new XDoc(root);
            foreach(KeyValuePair<string, string> pair in enumerator) {
                doc.InsertValueAt(pair.Key, pair.Value);
            }
            return doc;
        }

        /// <summary>
        /// Convert an xml document into a key/value pair list.
        /// </summary>
        /// <param name="doc">Document to convert.</param>
        /// <returns>Array of key/value pairs.</returns>
        public static KeyValuePair<string, string>[] ToXPathValuePairs(XDoc doc) {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            if(!doc.IsEmpty) {
                CollectKeyValuePairs(string.Empty, doc.Root.AsXmlNode, result);
            }
            return result.ToArray();
        }

        private static void CollectKeyValuePairs(string prefix, XmlNode node, List<KeyValuePair<string, string>> pairs) {

            // TODO (steveb): we need a notation to support mixed content!

            // compute text value for current node (Note: we don't allow mixed mode content; so we concatenate all text values into a single text value)
            StringBuilder text = null;
            foreach(XmlNode child in node.ChildNodes) {
                if((child.NodeType == XmlNodeType.Text) || (child.NodeType == XmlNodeType.CDATA)) {
                    if(text == null) {
                        text = new StringBuilder();
                    }
                    text.Append(child.Value);
                }
            }
            if(!string.IsNullOrEmpty(prefix)) {
                pairs.Add(new KeyValuePair<string, string>(prefix, (text == null) ? null : text.ToString()));
            }

            // expand prefix
            if(!string.IsNullOrEmpty(prefix)) {
                prefix += "/";
            }

            // generate attribute keys
            foreach(XmlAttribute attr in node.Attributes) {
                pairs.Add(new KeyValuePair<string, string>(prefix + "@" + attr.Name, attr.Value));
            }

            // recurse into child nodes
            foreach(XmlNode child in node.ChildNodes) {
                if(child.NodeType == XmlNodeType.Element) {
                    CollectKeyValuePairs(prefix + child.Name, child, pairs);
                }
            }
        }

    }
}