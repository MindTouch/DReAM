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

using MindTouch.IO;
using MindTouch.Xml;

namespace MindTouch.Dream {

    /// <summary>
    /// Provides static helper methods for Php Http interop.
    /// </summary>
    public static class PhpUtil {

        //--- Class Fields ---

        /// <summary>
        /// Unix "Epoch" time, i.e. seconds since January 1st, 1970, UTC.
        /// </summary>
        public static readonly DateTime UnixTimeZero = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        private static Regex _dollarParamsRegex = new Regex(@"(?<param>\$\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Methods ---

        /// <summary>
        /// Convert a php $n placeholder convention format string to a .NET format string. 
        /// </summary>
        /// <param name="paramsString"></param>
        /// <returns></returns>
        public static string ConvertToFormatString(string paramsString) {

            // replace all string parameters of the form $x with {x-1}, where x is an integer
            paramsString = _dollarParamsRegex.Replace(paramsString, delegate(Match m) {
                int paramValue = 0;
                if(Int32.TryParse(m.Groups["param"].Value.Substring(1), out paramValue)) {
                    return "{" + (paramValue - 1) + "}";
                } else {
                    return m.Groups["param"].Value;
                }
            });
            return paramsString;
        }

        /// <summary>
        /// Convert an xml document into Php object notation.
        /// </summary>
        /// <param name="doc">Document to convert.</param>
        /// <param name="stream">Stream to write converted document to.</param>
        /// <param name="encoding">Encoding to use for output.</param>
        public static void WritePhp(XDoc doc, Stream stream, Encoding encoding) {
            if(doc.IsEmpty) {
                stream.Write(encoding, "a:0:{}");
                return;
            } else {
                List<XmlNode> nodeList = new List<XmlNode>();
                foreach(XDoc item in doc) {
                    nodeList.Add(item.AsXmlNode);
                }
                IDictionary<string, List<XmlNode>> list = XDocUtil.CollapseNodeList(nodeList);
                stream.Write(encoding, "a:{0}:{{", list.Count);
                WritePhp(list, stream, encoding);
                stream.Write(encoding, "}");
            }
        }

        private static void WritePhp(XmlNode node, Stream stream, Encoding encoding) {
            IDictionary<string, List<XmlNode>> list;
            switch(node.NodeType) {
            case XmlNodeType.Document:
                list = XDocUtil.CollapseNodeList(node.ChildNodes);
                stream.Write(encoding, "a:{0}:{{", list.Count);
                WritePhp(list, stream, encoding);
                stream.Write(encoding, "}");
                break;
            case XmlNodeType.Element:

                // check if we have the common case of a simple node (tag and text only without attributes, or tag only without text or attributes)
                if((node.Attributes.Count == 0) && (node.ChildNodes.Count == 0)) {
                    Serialize(string.Empty, stream, encoding);
                    stream.Write(encoding, ";");
                } else if((node.Attributes.Count > 0) || (node.ChildNodes.Count != 1) || (node.ChildNodes[0].NodeType != XmlNodeType.Text)) {
                    list = XDocUtil.CollapseNodeList(node.ChildNodes);
                    stream.Write(encoding, "a:{0}:{{", node.Attributes.Count + list.Count);
                    foreach(XmlNode sub in node.Attributes) {
                        WritePhp(sub, stream, encoding);
                    }
                    WritePhp(list, stream, encoding);
                    stream.Write(encoding, "}");
                } else {
                    WritePhp(node.ChildNodes[0], stream, encoding);
                }
                break;
            case XmlNodeType.Text:
                Serialize(node.Value, stream, encoding);
                stream.Write(encoding, ";");
                break;
            case XmlNodeType.Attribute:
                Serialize("@" + node.Name, stream, encoding);
                stream.Write(encoding, ";");
                Serialize(node.Value, stream, encoding);
                stream.Write(encoding, ";");
                break;
            }
        }

        private static void WritePhp(IDictionary<string, List<XmlNode>> list, Stream stream, Encoding encoding) {
            foreach(KeyValuePair<string, List<XmlNode>> entry in list) {
                Serialize(entry.Key, stream, encoding);
                stream.Write(encoding, ";");
                if(entry.Value.Count > 1) {
                    stream.Write(encoding, "a:{0}:{{", entry.Value.Count);
                    int index = 0;
                    foreach(XmlNode node in entry.Value) {
                        Serialize(index, stream, encoding);
                        stream.Write(encoding, ";");
                        if(node.NodeType == XmlNodeType.Text) {
                            Serialize(node.Value, stream, encoding);
                            stream.Write(encoding, ";");
                        } else {
                            WritePhp(node, stream, encoding);
                        }
                        ++index;
                    }
                    stream.Write(encoding, "}");
                } else {
                    WritePhp(entry.Value[0], stream, encoding);
                }
            }
        }

        private static void SerializeObject(object arg, Stream stream, Encoding encoding) {
            if(arg == null) {
                stream.Write(encoding, "N");
            } else if(arg is string) {
                Serialize((string)arg, stream, encoding);
            } else if(arg is int || arg is long) {
                Serialize((long)arg, stream, encoding);
            } else if(arg is float || arg is double) {
                Serialize((double)arg, stream, encoding);
            } else if(arg is bool) {
                Serialize((bool)arg, stream, encoding);
            } else if(arg is XDoc) {
                WritePhp((XDoc)arg, stream, encoding);
            } else if(arg is System.Collections.IList) {
                Serialize((System.Collections.IList)arg, stream, encoding);
            } else {
                throw new NotSupportedException("Type " + arg.GetType() + " not supported");
            }
        }

        private static void Serialize(string arg, Stream stream, Encoding encoding) {
            stream.Write(encoding, "s:{0}:\"", encoding.GetByteCount(arg));
            stream.Write(encoding, arg);
            stream.Write(encoding, "\"");
        }

        private static void Serialize(long arg, Stream stream, Encoding encoding) {
            stream.Write(encoding, "i:");
            stream.Write(encoding, arg.ToString());
        }

        private static void Serialize(bool arg, Stream stream, Encoding encoding) {
            stream.Write(encoding, "b:");
            stream.Write(encoding, arg ? "1" : "0");
        }

        private static void Serialize(double arg, Stream stream, Encoding encoding) {
            stream.Write(encoding, "d:");
            stream.Write(encoding, arg.ToString());
        }

        private static void Serialize(System.Collections.IList list, Stream stream, Encoding encoding) {
            stream.Write(encoding, "a:{0}:{{", list.Count);
            for(int i = 0; i < list.Count; ++i) {
                stream.Write(encoding, "i:{0};", i);
                SerializeObject(list[i], stream, encoding);
                stream.Write(encoding, ";");
            }
            stream.Write(encoding, ";");
        }
    }
}