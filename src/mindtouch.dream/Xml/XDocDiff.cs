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

    /// <summary>
    /// Provides a facility for comparing <see cref="XDoc"/> instances.
    /// </summary>
    public static class XDocDiff {

        //--- Constants ---
        private const string DELETED = "del";
        private const string INSERTED = "ins";
        private const int MAX_SAME_COUNTER = 3;

        //--- Types ---

        /// <summary>
        /// Provides a xml node token.
        /// </summary>
        public class Token {

            //--- Class Methods ---

            /// <summary>
            /// Compare two Token to determine whether they represent the same value.
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <returns></returns>
            public static bool Equal(Token left, Token right) {
                return (left.Type == right.Type) && (left.Value == right.Value);
            }

            //--- Fields ---

            /// <summary>
            /// Type of node.
            /// </summary>
            public readonly XmlNodeType Type;

            /// <summary>
            /// String value of Xml node.
            /// </summary>
            public readonly string Value;

            /// <summary>
            /// Unique key to identify token by.
            /// </summary>
            public readonly object Key;

            //--- Constructors ---

            /// <summary>
            /// Create a new node token.
            /// </summary>
            /// <param name="type">Type of node.</param>
            /// <param name="value">String value of node.</param>
            /// <param name="key">Unique key to identify token by.</param>
            public Token(XmlNodeType type, string value, object key) {
                this.Type = type;
                this.Value = value;
                this.Key = key;
            }

            //--- Methods ---

            /// <summary>
            /// Return a string representation of the the Token's value.
            /// </summary>
            /// <returns>A string instance.</returns>
            public override string ToString() {
                return Type + "(" + Value + ")";
            }
        }

        //--- Class Methods ---

        /// <summary>
        /// Diff two documents.
        /// </summary>
        /// <param name="left">Left hand document.</param>
        /// <param name="right">Right hand document.</param>
        /// <param name="maxsize">Maximum size of the difference response.</param>
        /// <returns>Array of difference tuples.</returns>
        public static Tuplet<ArrayDiffKind, Token>[] Diff(XDoc left, XDoc right, int maxsize) {
            if(left == null) {
                throw new ArgumentNullException("left");
            }
            if(right == null) {
                throw new ArgumentNullException("right");
            }
            return Diff(Tokenize(left), Tokenize(right), maxsize);
        }

        /// <summary>
        /// Diff two token sets.
        /// </summary>
        /// <param name="left">Left hand token set.</param>
        /// <param name="right">Right hand token set.</param>
        /// <param name="maxsize">Maximum size of the difference response.</param>
        /// <returns>Array of difference tuples.</returns>
        public static Tuplet<ArrayDiffKind, Token>[] Diff(Token[] left, Token[] right, int maxsize) {
            if(left == null) {
                throw new ArgumentNullException("left");
            }
            if(right == null) {
                throw new ArgumentNullException("right");
            }
            return ArrayUtil.Diff(left, right, maxsize, Token.Equal);
        }

        /// <summary>
        /// Perform a three-way merge of documents.
        /// </summary>
        /// <param name="original">Original document.</param>
        /// <param name="left">Left hand modification of document.</param>
        /// <param name="right">Right hand modification of document.</param>
        /// <param name="maxsize">Maximum size of the difference response.</param>
        /// <param name="priority">Merge priority.</param>
        /// <param name="conflict">Output of conflict flag.</param>
        /// <returns>Merged document.</returns>
        public static XDoc Merge(XDoc original, XDoc left, XDoc right, int maxsize, ArrayMergeDiffPriority priority, out bool conflict) {
            if(original == null) {
                throw new ArgumentNullException("original");
            }
            if(left == null) {
                throw new ArgumentNullException("left");
            }
            if(right == null) {
                throw new ArgumentNullException("right");
            }
            return Merge(Tokenize(original), Tokenize(left), Tokenize(right), maxsize, priority, out conflict);
        }

        /// <summary>
        /// Perform a three-way merge between token sets resulting in a document.
        /// </summary>
        /// <param name="original">Original Token set.</param>
        /// <param name="left">Left hand token set.</param>
        /// <param name="right">Right hand token set.</param>
        /// <param name="maxsize">Maximum size of the difference response.</param>
        /// <param name="priority">Merge priority.</param>
        /// <param name="conflict">Output of conflict flag.</param>
        /// <returns>Merged document.</returns>
        public static XDoc Merge(Token[] original, Token[] left, Token[] right, int maxsize, ArrayMergeDiffPriority priority, out bool conflict) {
            if(original == null) {
                throw new ArgumentNullException("original");
            }
            if(left == null) {
                throw new ArgumentNullException("left");
            }
            if(right == null) {
                throw new ArgumentNullException("right");
            }

            // create left diff
            Tuplet<ArrayDiffKind, Token>[] leftDiff = Diff(original, left, maxsize);
            if(leftDiff == null) {
                conflict = false;
                return null;
            }

            // create right diff
            Tuplet<ArrayDiffKind, Token>[] rightDiff = Diff(original, right, maxsize);
            if(rightDiff == null) {
                conflict = false;
                return null;
            }

            // merge changes
            Tuplet<ArrayDiffKind, Token>[] mergeDiff = ArrayUtil.MergeDiff(leftDiff, rightDiff, priority, Token.Equal, x => x.Key, out conflict);
            return Detokenize(mergeDiff);
        }

        /// <summary>
        /// Create a highlight document from a set of differences.
        /// </summary>
        /// <param name="diff">Difference set.</param>
        /// <returns>Highlight document.</returns>
        public static XDoc Highlight(Tuplet<ArrayDiffKind, Token>[] diff) {
            XDoc combined;
            List<Tuplet<string, string, string>> invisibleChanges;
            XDoc before;
            XDoc after;
            Highlight(diff, out combined, out invisibleChanges, out before, out after);
            return combined;
        }

        /// <summary>
        /// Create before, after and combined highlight documents for a set of differences.
        /// </summary>
        /// <param name="diff">Difference set.</param>
        /// <param name="combined">Output of combined highlight document.</param>
        /// <param name="combinedInvisible">Output of the combined invisible differences.</param>
        /// <param name="before">Output of before difference highlight document.</param>
        /// <param name="after">Output of after difference highlight document.</param>
        public static void Highlight(Tuplet<ArrayDiffKind, Token>[] diff, out XDoc combined, out List<Tuplet<string, string, string>> combinedInvisible /* tuple(xpath, before, after) */, out XDoc before, out XDoc after) {
            if(diff == null) {
                throw new ArgumentNullException("diff");
            }
            List<Tuplet<ArrayDiffKind, Token>> combinedChanges = new List<Tuplet<ArrayDiffKind, Token>>(diff.Length);
            combinedInvisible = new List<Tuplet<string, string, string>>();
            List<Tuplet<ArrayDiffKind, Token>> beforeChanges = new List<Tuplet<ArrayDiffKind, Token>>(diff.Length);
            List<Tuplet<ArrayDiffKind, Token>> afterChanges = new List<Tuplet<ArrayDiffKind, Token>>(diff.Length);
            bool changedElement = false;
            Stack<List<string>> path = new Stack<List<string>>();
            Dictionary<string, Tuplet<string, string, string>> invisibleChangesLookup = new Dictionary<string, Tuplet<string, string, string>>();
            path.Push(new List<string>());
            for(int i = 0; i < diff.Length; ++i) {
                Tuplet<ArrayDiffKind, Token> item = diff[i];
                Token token = item.Item2;
                switch(item.Item1) {
                case ArrayDiffKind.Added:
                    switch(token.Type) {
                    case XmlNodeType.Text:
                        if((token.Value.Length > 0) && !char.IsWhiteSpace(token.Value[0])) {
                            Highlight_InlineTextChanges(diff, i, combinedChanges, beforeChanges, afterChanges, out i);

                            // adjust iterator since it will be immediately increased again
                            --i;
                            continue;
                        }
                        break;
                    case XmlNodeType.Attribute: 
                        if(!changedElement) {
                            string[] parts = token.Value.Split(new char[] { '=' }, 2);
                            string xpath = ComputeXPath(path, "@" + parts[0]);
                            Tuplet<string, string, string> beforeAfter;
                            if(invisibleChangesLookup.TryGetValue(xpath, out beforeAfter)) {
                                beforeAfter.Item3 = parts[1];
                            } else {
                                beforeAfter = new Tuplet<string, string, string>(xpath, null, parts[1]);
                                combinedInvisible.Add(beforeAfter);
                                invisibleChangesLookup[xpath] = beforeAfter;
                            }
                        }
                        break;
                    case XmlNodeType.Element:

                        // NOTE (steveb): this check shouldn't be needed, but just in case, it's better to have a wrong path than an exception!
                        if(path.Count > 0) {
                            path.Peek().Add(token.Value);
                        }
                        path.Push(new List<string>());
                        changedElement = true;
                        break;
                    case XmlNodeType.None:

                        // NOTE (steveb): this check shouldn't be needed, but just in case, it's better to have a wrong path than an exception!
                        if(path.Count > 0) {
                            path.Pop();
                        }
                        break;
                    }
                    item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, token);
                    afterChanges.Add(item);
                    combinedChanges.Add(item);
                    break;
                case ArrayDiffKind.Removed:
                    switch(token.Type) {
                    case XmlNodeType.Text:
                        if((token.Value.Length > 0) && !char.IsWhiteSpace(token.Value[0])) {
                            Highlight_InlineTextChanges(diff, i, combinedChanges, beforeChanges, afterChanges, out i);

                            // adjust iterator since it will be immediately increased again
                            --i;
                            continue;
                        } else {

                            // keep whitespace text
                            combinedChanges.Add(new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, token));
                        }
                        break;
                    case XmlNodeType.Attribute:
                        if(!changedElement) {
                            string[] parts = token.Value.Split(new char[] { '=' }, 2);
                            string xpath = ComputeXPath(path, "@" + parts[0]);
                            Tuplet<string, string, string> beforeAfter;
                            if(invisibleChangesLookup.TryGetValue(xpath, out beforeAfter)) {
                                beforeAfter.Item2 = parts[1];
                            } else {
                                beforeAfter = new Tuplet<string, string, string>(xpath, parts[1], null);
                                combinedInvisible.Add(beforeAfter);
                                invisibleChangesLookup[xpath] = beforeAfter;
                            }
                        }
                        break;
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:

                        // keep whitespace text
                        combinedChanges.Add(new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, token));
                        break;
                    case XmlNodeType.Element:
                        changedElement = true;
                        break;
                    }
                    beforeChanges.Add(new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, token));
                    break;
                case ArrayDiffKind.Same:
                    switch(token.Type) {
                    case XmlNodeType.Element:
                        changedElement = false;

                        // NOTE (steveb): this check shouldn't be needed, but just in case, it's better to have a wrong path than an exception!
                        if(path.Count > 0) {
                            path.Peek().Add(token.Value);
                        }
                        path.Push(new List<string>());
                        break;
                    case XmlNodeType.None:

                        // NOTE (steveb): this check shouldn't be needed, but just in case, it's better to have a wrong path than an exception!
                        if(path.Count > 0) {
                            path.Pop();
                        }
                        break;
                    }
                    combinedChanges.Add(item);
                    beforeChanges.Add(item);
                    afterChanges.Add(item);
                    break;
                case ArrayDiffKind.AddedLeft:
                case ArrayDiffKind.AddedRight:
                case ArrayDiffKind.RemovedLeft:
                case ArrayDiffKind.RemovedRight:

                    // TODO (steveb): process conflicting changes
                    throw new NotImplementedException("cannot highlight changes for a diff with conflicts");
                }
            }
            before = Detokenize(beforeChanges.ToArray());
            after = Detokenize(afterChanges.ToArray());
            combined = Detokenize(combinedChanges.ToArray());
        }

        private static void Highlight_InlineTextChanges(Tuplet<ArrayDiffKind, Token>[] diff, int index, List<Tuplet<ArrayDiffKind, Token>> combinedChanges, List<Tuplet<ArrayDiffKind, Token>> beforeChanges, List<Tuplet<ArrayDiffKind, Token>> afterChanges, out int next) {
            int lastAdded = index;
            int lastRemoved = index;
            int firstAdded = -1;
            int firstRemoved = -1;
            Tuplet<ArrayDiffKind, Token> item;

            // determine how long the chain of intermingled changes is
            for(int i = index, sameCounter = 0; (i < diff.Length) && ((diff[i].Item2.Type == XmlNodeType.Text) || (diff[i].Item2.Type == XmlNodeType.Whitespace) || diff[i].Item2.Type == XmlNodeType.SignificantWhitespace) && (sameCounter <= MAX_SAME_COUNTER); ++i) {
                item = diff[i];
                Token token = item.Item2;
                if((token.Value.Length > 0) && !char.IsWhiteSpace(token.Value[0])) {
                    if(item.Item1 == ArrayDiffKind.Added) {
                        sameCounter = 0;
                        if(firstAdded == -1) {
                            firstAdded = i;
                        }
                        lastAdded = i;
                    } else if(item.Item1 == ArrayDiffKind.Removed) {
                        sameCounter = 0;
                        if(firstRemoved == -1) {
                            firstRemoved = i;
                        }
                        lastRemoved = i;
                    } else {

                        // we count the number of non-changed elements to break-up long runs with no changes
                        ++sameCounter;
                    }
                }
            }

            // set index of next element
            next = Math.Max(lastAdded, lastRemoved) + 1;

            // check if any text was added
            if(firstAdded != -1) {

                // add all unchanged text before the first added text
                for(int i = index; i < firstAdded; ++i) {
                    if(diff[i].Item1 == ArrayDiffKind.Same) {
                        item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, diff[i].Item2);
                        combinedChanges.Add(item);                            
                        afterChanges.Add(item);
                    }
                }

                // add all text nodes that were added in a row
                object key = new object();
                item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, new Token(XmlNodeType.Element, INSERTED, key));
                combinedChanges.Add(item);
                afterChanges.Add(item);
                item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, new Token(XmlNodeType.EndElement, string.Empty, null));
                combinedChanges.Add(item);
                afterChanges.Add(item);
                for(int i = firstAdded; i <= lastAdded; ++i) {
                    if(diff[i].Item1 != ArrayDiffKind.Removed) {
                        item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, diff[i].Item2);
                        combinedChanges.Add(item);
                        afterChanges.Add(item);
                    }
                }
                item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, new Token(XmlNodeType.None, INSERTED, key));
                combinedChanges.Add(item);
                afterChanges.Add(item);

                // add all unchanged text after the last added text
                for(int i = lastAdded + 1; i < next; ++i) {
                    if(diff[i].Item1 == ArrayDiffKind.Same) {
                        item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, diff[i].Item2);
                        combinedChanges.Add(item);
                        afterChanges.Add(item);
                    }
                }
            } else {

                // add all unchanged text before the first added text
                for(int i = index; i < next; ++i) {
                    if(diff[i].Item1 == ArrayDiffKind.Same) {
                        item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, diff[i].Item2);
                        combinedChanges.Add(item);
                        afterChanges.Add(item);
                    }
                }
            }

            // check if any text was removed
            if(firstRemoved != -1) {

                // add all unchanged text before the first removed text
                for(int i = index; i < firstRemoved; ++i) {
                    if(diff[i].Item1 == ArrayDiffKind.Same) {
                        item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, diff[i].Item2);
                        if((item.Item2.Value.Length > 0) && !char.IsWhiteSpace(item.Item2.Value[0])) {
                            combinedChanges.Add(item);
                        }
                        beforeChanges.Add(item);
                    }
                }

                // add all text nodes that were removed in a row
                object key = new object();
                item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, new Token(XmlNodeType.Element, DELETED, key));
                combinedChanges.Add(item);
                beforeChanges.Add(item);
                item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, new Token(XmlNodeType.EndElement, string.Empty, null));
                combinedChanges.Add(item);
                beforeChanges.Add(item);
                for(int i = firstRemoved; i <= lastRemoved; ++i) {
                    if(diff[i].Item1 != ArrayDiffKind.Added) {
                        item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, diff[i].Item2);
                        combinedChanges.Add(item);
                        beforeChanges.Add(item);
                    }
                }
                item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, new Token(XmlNodeType.None, DELETED, key));
                combinedChanges.Add(item);
                beforeChanges.Add(item);

                // add all unchanged text after the last removed text
                for(int i = lastRemoved + 1; i < next; ++i) {
                    if(diff[i].Item1 == ArrayDiffKind.Same) {
                        item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, diff[i].Item2);
                        combinedChanges.Add(item);
                        beforeChanges.Add(item);
                    }
                }
            } else {

                // add all unchanged text before the first removed text
                for(int i = index; i < next; ++i) {
                    if(diff[i].Item1 == ArrayDiffKind.Same) {
                        item = new Tuplet<ArrayDiffKind, Token>(ArrayDiffKind.Same, diff[i].Item2);
                        if((item.Item2.Value.Length > 0) && !char.IsWhiteSpace(item.Item2.Value[0])) {
                            combinedChanges.Add(item);
                        }
                        beforeChanges.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// Create a document from a difference set.
        /// </summary>
        /// <param name="tokens">Difference set.</param>
        /// <returns>Detokenized document.</returns>
        public static XDoc Detokenize(Tuplet<ArrayDiffKind, Token>[] tokens) {
            XmlDocument doc = XDoc.NewXmlDocument();
            Detokenize(tokens, 0, null, doc);
            return new XDoc(doc);
        }

        /// <summary>
        /// Convert a document to a token set.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns>Set of tokens.</returns>
        public static Token[] Tokenize(XDoc doc) {
            if(doc.IsEmpty) {
                throw new ArgumentException("XML document is empty", "doc");
            }
            List<Token> result = new List<Token>();
            XmlNode start = doc.AsXmlNode;
            if(start is XmlDocument) {
                start = start.OwnerDocument.DocumentElement;
            }
            Tokenize(start, result);
            return result.ToArray();
        }

        /// <summary>
        /// Write a difference set.
        /// </summary>
        /// <param name="diffset">Difference set.</param>
        /// <param name="writer">TextWriter to write the set to.</param>
        public static void Write(Tuplet<ArrayDiffKind, Token>[] diffset, TextWriter writer) {
            foreach(Tuplet<ArrayDiffKind, Token> entry in diffset) {
                switch(entry.Item1) {
                case ArrayDiffKind.Same:
                    writer.WriteLine(" " + entry.Item2);
                    break;
                case ArrayDiffKind.Removed:
                    writer.WriteLine("-" + entry.Item2);
                    break;
                case ArrayDiffKind.Added:
                    writer.WriteLine("+" + entry.Item2);
                    break;
                case ArrayDiffKind.AddedLeft:
                    writer.WriteLine("+<" + entry.Item2);
                    break;
                case ArrayDiffKind.AddedRight:
                    writer.WriteLine("+>" + entry.Item2);
                    break;
                case ArrayDiffKind.RemovedLeft:
                    writer.WriteLine("-<" + entry.Item2);
                    break;
                case ArrayDiffKind.RemovedRight:
                    writer.WriteLine("->" + entry.Item2);
                    break;
                }
            }
        }

        private static int Detokenize(Tuplet<ArrayDiffKind, Token>[] tokens, int index, XmlElement current, XmlDocument doc) {
            for(; index < tokens.Length; ++index) {
                Tuplet<ArrayDiffKind, Token> token = tokens[index];
                switch(token.Item1) {
                case ArrayDiffKind.Same:
                case ArrayDiffKind.Added:
                    switch(token.Item2.Type) {
                    case XmlNodeType.CDATA:
                        if(current == null) {
                            throw new ArgumentNullException("current");
                        }
                        current.AppendChild(doc.CreateCDataSection(token.Item2.Value));
                        break;
                    case XmlNodeType.Comment:
                        if(current == null) {
                            throw new ArgumentNullException("current");
                        }
                        current.AppendChild(doc.CreateComment(token.Item2.Value));
                        break;
                    case XmlNodeType.SignificantWhitespace:
                        if(current == null) {
                            throw new ArgumentNullException("current");
                        }
                        current.AppendChild(doc.CreateSignificantWhitespace(token.Item2.Value));
                        break;
                    case XmlNodeType.Text:
                        if(current == null) {
                            throw new ArgumentNullException("current");
                        }
                        current.AppendChild(doc.CreateTextNode(token.Item2.Value));
                        break;
                    case XmlNodeType.Whitespace:
                        if(current == null) {
                            throw new ArgumentNullException("current");
                        }
                        current.AppendChild(doc.CreateWhitespace(token.Item2.Value));
                        break;
                    case XmlNodeType.Element:
                        XmlElement next = doc.CreateElement(token.Item2.Value);
                        if(current == null) {
                            doc.AppendChild(next);
                        } else {
                            current.AppendChild(next);
                        }
                        index = Detokenize(tokens, index + 1, next, doc);
                        break;
                    case XmlNodeType.Attribute:
                        if(current == null) {
                            throw new ArgumentNullException("current");
                        }
                        string[] parts = token.Item2.Value.Split(new char[] { '=' }, 2);
                        current.SetAttribute(parts[0], parts[1]);
                        break;
                    case XmlNodeType.EndElement:

                        // nothing to do
                        break;
                    case XmlNodeType.None:
                        if(current == null) {
                            throw new ArgumentNullException("current");
                        }

                        // ensure we're closing the intended element
                        if(token.Item2.Value != current.Name) {
                            throw new InvalidOperationException(string.Format("mismatched element ending; found </{0}>, expected </{1}>", token.Item2.Value, current.Name));
                        }

                        // we're done with this sequence
                        return index;
                    default:
                        throw new InvalidOperationException("unhandled node type: " + token.Item2.Type);
                    }
                    break;
                case ArrayDiffKind.Removed:

                    // ignore removed nodes
                    break;
                default:
                    throw new InvalidOperationException("invalid diff kind: " + token.Item1);
                }
            }
            if(current != null) {
                throw new InvalidOperationException("unexpected end of tokens");
            }
            return index;
        }

        private static void Tokenize(XmlNode node, List<Token> tokens) {
            switch(node.NodeType) {
            case XmlNodeType.CDATA:
            case XmlNodeType.Comment:
            case XmlNodeType.SignificantWhitespace:
            case XmlNodeType.Whitespace: 
                tokens.Add(new Token(node.NodeType, node.Value, null));
                break;
            case XmlNodeType.Text: {

                    // split text nodes
                    string text = node.Value;
                    int start = 0;
                    int end = text.Length;
                    while(start < end) {

                        // check if first character is a whitespace
                        int index = start + 1;
                        if(char.IsWhiteSpace(text[start])) {

                            // skip whitespace
                            while((index < end) && char.IsWhiteSpace(text[index])) {
                                ++index;
                            }
                        } else if(char.IsLetterOrDigit(text[start]) || (text[start] == '_')) {

                            // skip alphanumeric, underscore (_), and dot (.)/comma (,) if preceded by a digit
                            while(index < end) {
                                char c = text[index];
                                if(char.IsLetterOrDigit(c) || (c == '_') || (((c == '.') || (c == ',')) && (index > start) && char.IsDigit(text[index-1]))) {
                                    ++index;
                                } else {
                                    break;
                                }
                            }

                        } else {

                            // skip non-whitespace & non-alphanumeric
                            while((index < end) && !char.IsWhiteSpace(text[index]) && !char.IsLetterOrDigit(text[index])) {
                                ++index;
                            }
                        }
                        if((start == 0) && (index == end)) {
                            tokens.Add(new Token(XmlNodeType.Text, text, null));
                        } else {
                            tokens.Add(new Token(XmlNodeType.Text, text.Substring(start, index - start), null));
                        }
                        start = index;
                    }
                }
                break;
            case XmlNodeType.Element:
                object key = new object();
                tokens.Add(new Token(XmlNodeType.Element, node.Name, key));

                // enumerate attribute in sorted order
                if(node.Attributes.Count > 0) {
                    List<XmlAttribute> attributes = new List<XmlAttribute>();
                    foreach(XmlAttribute attribute in node.Attributes) {
                        attributes.Add(attribute);
                    }
                    attributes.Sort(delegate(XmlAttribute left, XmlAttribute right) { return StringUtil.CompareInvariant(left.Name, right.Name); });
                    foreach(XmlAttribute attribute in attributes) {
                        tokens.Add(new Token(XmlNodeType.Attribute, attribute.Name + "=" + attribute.Value, null));
                    }
                }
                tokens.Add(new Token(XmlNodeType.EndElement, string.Empty, null));
                if(node.HasChildNodes) {
                    foreach(XmlNode child in node.ChildNodes) {
                        Tokenize(child, tokens);
                    }
                }
                tokens.Add(new Token(XmlNodeType.None, node.Name, key));
                break;
            }
        }

        private static string ComputeXPath(Stack<List<string>> path, string last) {
            StringBuilder result = new StringBuilder();
            List<string>[] levels = path.ToArray();
            Array.Reverse(levels);
            foreach(List<string> level in levels) {
                if(level.Count > 0) {
                    string current = level[level.Count - 1];
                    int count = 1;
                    for(int i = 0; i < level.Count - 1; ++i) {
                        if(StringUtil.EqualsInvariant(current, level[i])) {
                            ++count;
                        }
                    }
                    result.Append('/').Append(current);
                    if(count > 1) {
                        result.Append('[').Append(count).Append(']');
                    }
                }
            }
            if(!string.IsNullOrEmpty(last)) {
                result.Append('/').Append(last);
            }
            return result.ToString();
        }
    }

    /// <summary>
    /// Provides a utility for extracting and replacing words from an xml document.
    /// </summary>
    public class XDocWord {

        //--- Types ---

        /// <summary>
        /// Delegate for replacing and <see cref="XDocWord"/> instance in a document with a new XmlNode.
        /// </summary>
        /// <param name="doc">Parent document node of the word.</param>
        /// <param name="word">Word to replace.</param>
        /// <returns>Replacement Xml node.</returns>
        public delegate XmlNode ReplacementHandler(XmlDocument doc, XDocWord word);

        //--- Class Methods ---

        /// <summary>
        /// Create a word list from an <see cref="XDoc"/> instance.
        /// </summary>
        /// <param name="doc">Document to extract words from.</param>
        /// <returns>Array of word instances.</returns>
        public static XDocWord[] ConvertToWordList(XDoc doc) {
            List<XDocWord> result = new List<XDocWord>();
            if(!doc.IsEmpty) {
                ConvertToWordList(doc.AsXmlNode, result);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Replace words in a document.
        /// </summary>
        /// <param name="wordlist">List of words to run replacement function for.</param>
        /// <param name="handler">Word replacement delegate.</param>
        public static void ReplaceText(XDocWord[] wordlist, ReplacementHandler handler) {
            if(wordlist == null) {
                throw new ArgumentNullException("wordlist");
            }
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }

            // loop through words backwards
            for(int i = wordlist.Length - 1; i >= 0; --i) {
                XDocWord word = wordlist[i];
                if(word.IsText) {
                    XmlNode replacement = handler(word.Node.OwnerDocument, word);
                    if(replacement != null) {

                        // split the node
                        XmlText node = (XmlText)word.Node;

                        // split off the part after the text
                        if(node.Value.Length > (word.Offset + word.Value.Length)) {
                            node.SplitText(word.Offset + word.Value.Length);
                        }

                        // split off the part before the text
                        if(word.Offset > 0) {
                            node = node.SplitText(word.Offset);
                        }

                        // replace text with new result
                        node.ParentNode.InsertAfter(replacement, node);
                        node.ParentNode.RemoveChild(node);
                    }
                }
            }
        }

        private static void ConvertElementToWord(XmlNode node, string[] attributes, List<XDocWord> words) {
            StringBuilder tag = new StringBuilder();
            tag.Append("<");
            tag.Append(node.Name);
            foreach(string attribute in attributes) {
                XmlAttribute attr = node.Attributes[attribute];
                if(attr != null) {
                    tag.AppendFormat(" {0}=\"{1}\"", attr.Name, attr.Value);
                }
            }
            tag.Append(">");
            words.Add(new XDocWord(tag.ToString(), 0, node));
        }

        private static void ConvertToWordList(XmlNode node, List<XDocWord> words) {
            switch(node.NodeType) {
            case XmlNodeType.Document:
                ConvertToWordList(((XmlDocument)node).DocumentElement, words);
                break;
            case XmlNodeType.Element:
                switch(node.Name) {
                case "img":
                    ConvertElementToWord(node, new string[] { "src" }, words);
                    break;
                default:
                    if(node.HasChildNodes) {
                        foreach(XmlNode child in node.ChildNodes) {
                            ConvertToWordList(child, words);
                        }
                    }
                    break;
                }
                break;
            case XmlNodeType.Text: {

                    // split text nodes
                    string text = node.Value;
                    if((text.Length == 0) && (words.Count == 0)) {

                        // NOTE (steveb): we add the empty text node at the beginning of each document as a reference point

                        words.Add(new XDocWord(text, 0, node));
                    } else {
                        int start = 0;
                        int end = text.Length;
                        while(start < end) {

                            // check if first character is a whitespace
                            int index = start + 1;
                            if(char.IsWhiteSpace(text[start])) {

                                // skip whitespace
                                while((index < end) && char.IsWhiteSpace(text[index])) {
                                    ++index;
                                }
                            } else if(char.IsLetterOrDigit(text[start])) {

                                // skip alphanumeric, underscore (_), and dot (.)/comma (,) if preceded by a digit
                                while(index < end) {
                                    char c = text[index];
                                    if(char.IsLetterOrDigit(c) || (c == '_') || (((c == '.') || (c == ',')) && (index > start) && char.IsDigit(text[index - 1]))) {
                                        ++index;
                                    } else {
                                        break;
                                    }
                                }
                            } else {

                                // skip non-whitespace & non-alphanumeric
                                while((index < end) && !char.IsWhiteSpace(text[index]) && !char.IsLetterOrDigit(text[index])) {
                                    ++index;
                                }
                            }

                            // only add non-whitespace nodes
                            if(!char.IsWhiteSpace(text[start])) {
                                if((start == 0) && (index == end)) {
                                    words.Add(new XDocWord(text, 0, node));
                                } else {
                                    words.Add(new XDocWord(text.Substring(start, index - start), start, node));
                                }
                            }
                            start = index;
                        }
                    }
                }
                break;
            }
        }

        //--- Fields ---

        /// <summary>
        /// The word.
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// Word count offset into document.
        /// </summary>
        public readonly int Offset;

        /// <summary>
        /// The Xml node containing the word.
        /// </summary>
        public readonly XmlNode Node;

        //--- Constructors ---
        private XDocWord(string value, int offset, XmlNode node) {
            this.Value = value;
            this.Offset = offset;
            this.Node = node;
        }

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if the node is a text node.
        /// </summary>
        public bool IsText { get { return Node is XmlText; } }

        /// <summary>
        /// <see langword="True"/> if the parsed contents are an alphanumeric sequence.
        /// </summary>
        public bool IsWord { get { return IsText && (Value.Length > 0) && char.IsLetterOrDigit(Value[0]); } }

        /// <summary>
        /// XPath to the Node.
        /// </summary>
        public string Path {
            get {
                List<string> parents = new List<string>();
                XmlNode current = Node;
                switch(Node.NodeType) {
                case XmlNodeType.Text:
                    parents.Add("#");
                    current = Node.ParentNode;
                    break;
                }
                for(; current != null; current = current.ParentNode) {
                    parents.Add(current.Name);
                }
                parents.Reverse();
                return string.Join("/", parents.ToArray());
            }
        }

        //--- Methods ---

        /// <summary>
        /// Create a string represenation of the instance.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            switch(Node.NodeType) {
            case XmlNodeType.Element:
                if(Value != null) {
                    return Path + ": " + Value;
                } else {
                    return Path + ": </ " + Node.Name + ">";
                }
            case XmlNodeType.Text:
                return Path + ": '" + Value + "'";
            default:
                throw new InvalidDataException("unknown node type");
            }
        }
    }
}
