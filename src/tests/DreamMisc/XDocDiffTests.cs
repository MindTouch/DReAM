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
using System.Xml;
using NUnit.Framework;

using MindTouch.Dream;

namespace MindTouch.Xml.Test {

    [TestFixture]
    public class XDocDiffTests {

        //--- Class Methods ---
        private static void Match(Tuple<ArrayDiffKind, XDocDiff.Token> item, ArrayDiffKind change, XmlNodeType type, string value) {
            Assert.AreEqual(item.Item1, change);
            Assert.AreEqual(item.Item2.Type, type);
            Assert.AreEqual(item.Item2.Value, value);
        }

        //--- Methods ---
        [Test]
        public void DiffBasic() {

            // NOTE (steveb): test a simple diff

            XDoc a = XDocFactory.From("<p>before</p>", MimeType.XML);
            XDoc b = XDocFactory.From("<p>after</p>", MimeType.XML);
            var diff = XDocDiff.Diff(a, b, 10000);

            Match(diff[0], ArrayDiffKind.Same, XmlNodeType.Element, "p");
            Match(diff[1], ArrayDiffKind.Same, XmlNodeType.EndElement, string.Empty);
            Match(diff[2], ArrayDiffKind.Removed, XmlNodeType.Text, "before");
            Match(diff[3], ArrayDiffKind.Added, XmlNodeType.Text, "after");
            Match(diff[4], ArrayDiffKind.Same, XmlNodeType.None, "p");
        }

        [Test]
        public void DiffNumbersWithPeriod() {

            // NOTE (steveb): test if digits separated by dot/period are detected as one word

            XDoc a = XDocFactory.From("<p>1.23</p>", MimeType.XML);
            XDoc b = XDocFactory.From("<p>4.56</p>", MimeType.XML);
            var diff = XDocDiff.Diff(a, b, 10000);

            Match(diff[0], ArrayDiffKind.Same, XmlNodeType.Element, "p");
            Match(diff[1], ArrayDiffKind.Same, XmlNodeType.EndElement, string.Empty);
            Match(diff[2], ArrayDiffKind.Removed, XmlNodeType.Text, "1.23");
            Match(diff[3], ArrayDiffKind.Added, XmlNodeType.Text, "4.56");
            Match(diff[4], ArrayDiffKind.Same, XmlNodeType.None, "p");
        }

        [Test]
        public void DiffNumbersWithComma() {

            // NOTE (steveb): test if digits separated by comma are detected as one word

            XDoc a = XDocFactory.From("<p>1,23</p>", MimeType.XML);
            XDoc b = XDocFactory.From("<p>4,56</p>", MimeType.XML);
            var diff = XDocDiff.Diff(a, b, 10000);

            Match(diff[0], ArrayDiffKind.Same, XmlNodeType.Element, "p");
            Match(diff[1], ArrayDiffKind.Same, XmlNodeType.EndElement, string.Empty);
            Match(diff[2], ArrayDiffKind.Removed, XmlNodeType.Text, "1,23");
            Match(diff[3], ArrayDiffKind.Added, XmlNodeType.Text, "4,56");
            Match(diff[4], ArrayDiffKind.Same, XmlNodeType.None, "p");
        }

        [Test]
        public void DiffUnambiguosAddRemoveReplace() {

            // NOTE (steveb): test unamibiguous addition, deletion, and replacement

            XDoc a = XDocFactory.From("<p>the quick brown fox jumped over the lazy dog</p>", MimeType.XML);
            XDoc b = XDocFactory.From("<p>the slow black fox jumped far over the dog</p>", MimeType.XML);
            var diff = XDocDiff.Diff(a, b, 10000);

            Match(diff[0], ArrayDiffKind.Same, XmlNodeType.Element, "p");
            Match(diff[1], ArrayDiffKind.Same, XmlNodeType.EndElement, string.Empty);
            Match(diff[2], ArrayDiffKind.Same, XmlNodeType.Text, "the");
            Match(diff[3], ArrayDiffKind.Same, XmlNodeType.Text, " ");
            Match(diff[4], ArrayDiffKind.Removed, XmlNodeType.Text, "quick");
            Match(diff[5], ArrayDiffKind.Added, XmlNodeType.Text, "slow");
            Match(diff[6], ArrayDiffKind.Same, XmlNodeType.Text, " ");
            Match(diff[7], ArrayDiffKind.Removed, XmlNodeType.Text, "brown");
            Match(diff[8], ArrayDiffKind.Added, XmlNodeType.Text, "black");
            Match(diff[9], ArrayDiffKind.Same, XmlNodeType.Text, " ");
            Match(diff[10], ArrayDiffKind.Same, XmlNodeType.Text, "fox");
            Match(diff[11], ArrayDiffKind.Same, XmlNodeType.Text, " ");
            Match(diff[12], ArrayDiffKind.Same, XmlNodeType.Text, "jumped");
            Match(diff[13], ArrayDiffKind.Same, XmlNodeType.Text, " ");
            Match(diff[14], ArrayDiffKind.Added, XmlNodeType.Text, "far");
            Match(diff[15], ArrayDiffKind.Added, XmlNodeType.Text, " ");
            Match(diff[16], ArrayDiffKind.Same, XmlNodeType.Text, "over");
            Match(diff[17], ArrayDiffKind.Same, XmlNodeType.Text, " ");
            Match(diff[18], ArrayDiffKind.Same, XmlNodeType.Text, "the");
            Match(diff[19], ArrayDiffKind.Removed, XmlNodeType.Text, " ");
            Match(diff[20], ArrayDiffKind.Removed, XmlNodeType.Text, "lazy");
            Match(diff[21], ArrayDiffKind.Same, XmlNodeType.Text, " ");
            Match(diff[22], ArrayDiffKind.Same, XmlNodeType.Text, "dog");
            Match(diff[23], ArrayDiffKind.Same, XmlNodeType.None, "p");
        }

        [Test]
        public void DiffAttributes() {

            // NOTE (steveb): test attribute changes

            XDoc a = XDocFactory.From("<p>the <span class=\"red\">brown</span> fox</p>", MimeType.XML);
            XDoc b = XDocFactory.From("<p>the <span class=\"blue\">brown</span> fox</p>", MimeType.XML);
            var diff = XDocDiff.Diff(a, b, 10000);

            Match(diff[0], ArrayDiffKind.Same, XmlNodeType.Element, "p");
            Match(diff[1], ArrayDiffKind.Same, XmlNodeType.EndElement, string.Empty);
            Match(diff[2], ArrayDiffKind.Same, XmlNodeType.Text, "the");
            Match(diff[3], ArrayDiffKind.Same, XmlNodeType.Text, " ");
            Match(diff[4], ArrayDiffKind.Same, XmlNodeType.Element, "span");
            Match(diff[5], ArrayDiffKind.Removed, XmlNodeType.Attribute, "class=red");
            Match(diff[6], ArrayDiffKind.Added, XmlNodeType.Attribute, "class=blue");
            Match(diff[7], ArrayDiffKind.Same, XmlNodeType.EndElement, "");
            Match(diff[8], ArrayDiffKind.Same, XmlNodeType.Text, "brown");
            Match(diff[9], ArrayDiffKind.Same, XmlNodeType.None, "span");
            Match(diff[10], ArrayDiffKind.Same, XmlNodeType.Text, " ");
            Match(diff[11], ArrayDiffKind.Same, XmlNodeType.Text, "fox");
            Match(diff[12], ArrayDiffKind.Same, XmlNodeType.None, "p");
        }

        [Test]
        public void HighlightAttributes() {

            // NOTE (steveb): test unamibiguous addition, deletion, and replacement

            XDoc a = XDocFactory.From("<p>the <span class=\"red\">brown</span> fox</p>", MimeType.XML);
            XDoc b = XDocFactory.From("<p>the <span class=\"blue\">brown</span> fox</p>", MimeType.XML);
            var diff = XDocDiff.Diff(a, b, 10000);
            List<Tuple<string, string, string>> invisible;
            XDoc doc;
            XDoc before;
            XDoc after;
            XDocDiff.Highlight(diff, out doc, out invisible, out before, out after);

            Assert.AreEqual("<p>the <span class=\"blue\">brown</span> fox</p>", doc.ToString());
            Assert.AreEqual(1, invisible.Count);
            Assert.AreEqual(invisible[0].Item1, "/p/span/@class");
            Assert.AreEqual(invisible[0].Item2, "red");
            Assert.AreEqual(invisible[0].Item3, "blue");
            Assert.AreEqual(a.ToString(), before.ToString());
            Assert.AreEqual(b.ToString(), after.ToString());
        }
    }
}