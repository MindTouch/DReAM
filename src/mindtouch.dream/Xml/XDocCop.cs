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
using System.Xml;
using System.Text;

namespace MindTouch.Xml {

    /// <summary>
    /// Provides a mechanism to verify and enforce document structure and content rules on an <see cref="XDoc"/>.
    /// </summary>
    public class XDocCop {

        //--- Types ---
        private enum ProcessingMode {
            Default,
            RemoveEmpty,
            PadEmpty
        }

        private class Element {

            //--- Fields ---
            public readonly string[] Names;
            public ProcessingMode Mode;
            public Dictionary<string, Attribute> Attributes = new Dictionary<string, Attribute>(StringComparer.Ordinal);

            //--- Constructors ---
            public Element(string[] names, ProcessingMode mode, params Attribute[] attributes) {
                if(ArrayUtil.IsNullOrEmpty(names)) {
                    throw new ArgumentNullException("names");
                }
                this.Names = names;
                this.Mode = mode;
                foreach(Attribute attribute in attributes) {
                    this.Attributes[attribute.Name] = attribute;
                }
            }

            //--- Properties ---
            public string Name { get { return Names[0]; } }

            //--- Methods ---
            public Element Attr(string name, string defaultValue, params string[] legalValues) {
                Attributes[name] = new Attribute(name, defaultValue, legalValues);
                return this;
            }

            public Element Attr(string name) {
                Attributes[name] = new Attribute(name, null);
                return this;
            }

            public override string ToString() {
                StringBuilder result = new StringBuilder();
                switch(Mode) {
                case ProcessingMode.PadEmpty:
                    result.Append("+");
                    break;
                case ProcessingMode.RemoveEmpty:
                    result.Append("-");
                    break;
                }
                result.Append(string.Join("/", Names));
                if(Attributes.Count > 0) {
                    result.Append("[");
                    bool first = true;
                    foreach(Attribute attribute in Attributes.Values) {
                        if(!first) {
                            result.Append("|");
                        }
                        first = false;
                        result.Append(attribute.ToString());
                    }
                    result.Append("]");
                }
                return result.ToString();
            }
        }

        private class Attribute {

            //--- Fields ---
            public readonly string Name;
            public readonly string DefaultValue;
            public readonly string[] LegalValues;

            //--- Constructors ---
            public Attribute(string name, string defaultValue, params string[] legalValues) {
                this.Name = name;
                this.DefaultValue = defaultValue;
                this.LegalValues = legalValues;
                Array.Sort(this.LegalValues, StringComparer.Ordinal);
            }

            //--- Methods ---
            public override string ToString() {
                StringBuilder result = new StringBuilder();
                result.Append(Name);
                if(DefaultValue != null) {
                    result.Append("=");
                    result.Append(DefaultValue);
                }
                if(LegalValues.Length > 0) {
                    result.Append("<");
                    bool first = true;
                    foreach(string value in LegalValues) {
                        if(!first) {
                            result.Append("?");
                        }
                        first = false;
                        result.Append(value);
                    }
                }
                return result.ToString();
            }
        }

        //--- Fields ---
        private readonly Dictionary<string, Element> Elements = new Dictionary<string, Element>(StringComparer.Ordinal);
        private Element _wildcardElement;
        private bool _initialized = false;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance without any predefined rules.
        /// </summary>
        public XDocCop() { }

        /// <summary>
        /// Create a new instance with a set of initial rules.
        /// </summary>
        /// <param name="rules">Array of rules.</param>
        public XDocCop(string[] rules) {
            AddRules(rules);
        }

        //--- Methods ---
        /// <summary>
        /// Add rules to the instance.
        /// </summary>
        /// <param name="rules">Array of rules.</param>
        public void AddRules(string[] rules) {            
            if(rules == null) {
                throw new ArgumentNullException("rules");
            }
            foreach(string rule in rules) {
                AddRule(rule);
            }
        }

        /// <summary>
        /// Add a single rule to the instance.
        /// </summary>
        /// <param name="rule">Rule to add.</param>
        public void AddRule(string rule) {
            if(string.IsNullOrEmpty(rule)) {
                return;
            }
            rule = rule.Trim();
            if(rule.StartsWith("#")) {
                return;
            }
            ProcessingMode mode = ProcessingMode.Default;
            switch(rule[0]) {
            case '-':
                mode = ProcessingMode.RemoveEmpty;
                rule = rule.Substring(1);
                break;
            case '+':
                mode = ProcessingMode.PadEmpty;
                rule = rule.Substring(1);
                break;
            }
            int square = rule.IndexOf('[');
            if(square >= 0) {
                string[] names = rule.Substring(0, square).Split('/');
                List<Attribute> attributes = new List<Attribute>();
                foreach(string attr in rule.Substring(square + 1).TrimEnd(']').Split('|')) {

                    // TODO (steveb): we should support '=' and '<' for default and valid values set
                    int sep = attr.IndexOfAny(new char[] { '=', '<' });

                    attributes.Add(new Attribute((sep >= 0) ? attr.Substring(0, sep) : attr, null));
                }
                Add(new Element(names, mode, attributes.ToArray()));
            } else {
                Add(rule);
            }
        }

        /// <summary>
        /// Enforce the specified rules on the document.
        /// </summary>
        /// <param name="doc">Document to process.</param>
        public void Enforce(XDoc doc) {
            Initialize();
            Enforce(doc, true, true);
        }

        /// <summary>
        /// Enforce the specified rules on the document.
        /// </summary>
        /// <param name="doc">Document to process.</param>
        /// <param name="removeIllegalElements">If <see langword="True"/> strip illegal elements from the document.</param>
        public void Enforce(XDoc doc, bool removeIllegalElements) {
            Initialize();
            Enforce(doc, removeIllegalElements, true);
        }

        /// <summary>
        /// Verify that the document conforms to the specified rules.
        /// </summary>
        /// <param name="doc">Document to process.</param>
        /// <returns></returns>
        public bool Verify(XDoc doc) {
            Initialize();
            Elements.TryGetValue("*", out _wildcardElement);
            return Verify(doc, true);
        }

        /// <summary>
        /// Checks if the tag name is legal for the specified rules.
        /// </summary>
        /// <param name="tag">XML tag name to check.</param>
        /// <returns></returns>
        public bool IsLegalElement(string tag) {
            Initialize();
            return Elements.ContainsKey(tag);
        }

        /// <summary>
        /// Checks if the attribute name is legal for the given tag name is legal using the specified rules.
        /// </summary>
        /// <param name="tag">XML tag name to check.</param>
        /// <param name="name">XML attribute name to check.</param>
        /// <returns></returns>
        public bool IsLegalAttribute(string tag, string name) {
            Initialize();
            Element element;
            return (Elements.TryGetValue(tag, out element) && element.Attributes.ContainsKey(name)) || ((_wildcardElement != null) && _wildcardElement.Attributes.ContainsKey(name));
        }

        /// <summary>
        /// Create a string representation of the document analysis.
        /// </summary>
        /// <returns>A string instance.</returns>
        public override string ToString() {
            StringBuilder result = new StringBuilder();
            foreach(KeyValuePair<string, Element> element in Elements) {
                if(element.Key == element.Value.Name) {
                    result.AppendLine(element.Value.ToString());
                }
            }
            return result.ToString();
        }

        private void Enforce(XDoc doc, bool removeIllegalElements, bool isRoot) {
            if(doc.IsEmpty) {
                return;
            }

            // process child elements
            List<XDoc> list = doc.Elements.ToList();
            foreach(XDoc child in list) {
                Enforce(child, removeIllegalElements, false);
            }

            // skip processing of root element
            if(!isRoot) {

                // process element
                Element elementRule;
                if(!Elements.TryGetValue(doc.Name, out elementRule)) {

                    // element is not valid; determine what to do with it
                    if(removeIllegalElements) {

                        // replace it with its contents
                        doc.ReplaceWithNodes(doc);
                    } else {
                        StringBuilder attributes = new StringBuilder();
                        foreach(XmlAttribute attribute in doc.AsXmlNode.Attributes) {
                            attributes.Append(" ");
                            attributes.Append(attribute.OuterXml);
                        }

                        // replace it with text version of itself
                        if(doc.AsXmlNode.ChildNodes.Count == 0) {
                            doc.AddBefore("<" + doc.Name + attributes.ToString() + "/>");
                            doc.Remove();
                        } else {
                            doc.AddBefore("<" + doc.Name + attributes.ToString() + ">");
                            doc.AddAfter("</" + doc.Name + ">");
                            doc.ReplaceWithNodes(doc);
                        }
                    }
                    return;
                } else if(doc.Name != elementRule.Name) {

                    // element has an obsolete name, substitute it
                    doc.Rename(elementRule.Name);
                }

                // process attributes
                List<XmlAttribute> attributeList = new List<XmlAttribute>();
                foreach(XmlAttribute attribute in doc.AsXmlNode.Attributes) {
                    attributeList.Add(attribute);
                }

                // remove unsupported attributes
                if(!elementRule.Attributes.ContainsKey("*")) {
                    foreach(XmlAttribute attribute in attributeList) {
                        Attribute attributeRule;
                        elementRule.Attributes.TryGetValue(attribute.Name, out attributeRule);
                        if((attributeRule == null) && (_wildcardElement != null)) {
                            _wildcardElement.Attributes.TryGetValue(attribute.Name, out attributeRule);
                        }
                        if((attributeRule == null) || ((attributeRule.LegalValues.Length > 0) && (Array.BinarySearch<string>(attributeRule.LegalValues, attribute.Value) < 0))) {
                            doc.RemoveAttr(attribute.Name);
                        }
                    }
                }

                // add default attributes
                foreach(Attribute attributeRule in elementRule.Attributes.Values) {
                    if((attributeRule.DefaultValue != null) && (doc.AsXmlNode.Attributes[attributeRule.Name] == null)) {
                        doc.Attr(attributeRule.Name, attributeRule.DefaultValue);
                    }
                }

                // process empty element
                if(list.Count == 0) {

                    // check if the contents are empty
                    string contents = doc.Contents;
                    if((contents.Trim().Length == 0) && (contents.IndexOf('\u00A0') < 0)) {
                        switch(elementRule.Mode) {
                        case ProcessingMode.PadEmpty:

                            // add '&nbsp;'
                            doc.ReplaceValue("\u00A0");
                            break;
                        case ProcessingMode.RemoveEmpty:
                            doc.Remove();
                            break;
                        }
                    }
                }
            }
        }

        private bool Verify(XDoc doc, bool isRoot) {
            if(doc.IsEmpty) {
                return true;
            }

            // check child elements
            List<XDoc> list = doc.Elements.ToList();
            foreach(XDoc child in list) {
                if(!Verify(child, false)) {
                    return false;
                }
            }

            // skip processing of root element
            if(!isRoot) {

                // process element
                Element elementRule;
                if(!Elements.TryGetValue(doc.Name, out elementRule)) {

                    // element is not valid
                    return false;
                }

                // process attributes
                List<XmlAttribute> attributeList = new List<XmlAttribute>();
                foreach(XmlAttribute attribute in doc.AsXmlNode.Attributes) {
                    attributeList.Add(attribute);
                }

                // check for unsupported attributes
                if(!elementRule.Attributes.ContainsKey("*")) {
                    foreach(XmlAttribute attribute in attributeList) {
                        Attribute attributeRule;
                        elementRule.Attributes.TryGetValue(attribute.Name, out attributeRule);
                        if((attributeRule == null) && (_wildcardElement != null)) {
                            _wildcardElement.Attributes.TryGetValue(attribute.Name, out attributeRule);
                        }
                        if((attributeRule == null) || ((attributeRule.LegalValues.Length > 0) && (Array.BinarySearch<string>(attributeRule.LegalValues, attribute.Value) < 0))) {

                            // attribute is not valid
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void Add(Element element) {
            _initialized = false;
            foreach(string name in element.Names) {
                Elements.Add(name, element);
            }
        }

        private void Add(string[] names, ProcessingMode mode, params Attribute[] attributes) {
            Element result = new Element(names, mode, attributes);
            Add(result);
        }

        private void Add(string names, ProcessingMode mode) {
            Add(names.Split('/'), mode, new Attribute[0]);
        }

        private void Add(string names) {
            Add(names, ProcessingMode.Default);
        }

        private void Initialize() {
            if(!_initialized) {
                Elements.TryGetValue("*", out _wildcardElement);
                _initialized = true;
            }
        }
    }
}
