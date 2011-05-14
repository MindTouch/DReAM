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
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class JsonUtilTests {

        [Test]
        public void Can_convert_doc_to_json() {
            var doc = new XDoc("x").Elem("foo", "bar");
            Assert.AreEqual("{\"foo\":\"bar\"}",doc.ToJson());
        }

        [Test]
        public void Will_not_escape_single_quote() {
            var doc = new XDoc("x").Value("F'oo");
            Assert.AreEqual("\"F'oo\"", doc.ToJson());
        }

        [Test]
        public void Will_escape_double_quote() {
            var doc = new XDoc("foo").Value("\"Foo\"");
            Assert.AreEqual("\"\\\"Foo\\\"\"", doc.ToJson());
        }

        [Test]
        public void Will_escape_backslash_quote() {
            var doc = new XDoc("foo").Value("backslash: \\");
            Assert.AreEqual("\"backslash: \\\\\"", doc.ToJson());
        }

        [Test]
        public void Will_escape_backspace_quote() {
            var doc = new XDoc("foo").Value("backspace: \b");
            Assert.AreEqual("\"backspace: \\b\"", doc.ToJson());
        }

        [Test]
        public void Will_escape_formfeed_quote() {
            var doc = new XDoc("foo").Value("formfeed: \f");
            Assert.AreEqual("\"formfeed: \\f\"", doc.ToJson());
        }

        [Test]
        public void Will_escape_newline_quote() {
            var doc = new XDoc("foo").Value("newline: \n");
            Assert.AreEqual("\"newline: \\n\"", doc.ToJson());
        }

        [Test]
        public void Will_escape_carriage_return_quote() {
            var doc = new XDoc("foo").Value("carriage return: \r");
            Assert.AreEqual("\"carriage return: \\r\"", doc.ToJson());
        }

        [Test]
        public void Will_escape_tab_quote() {
            var doc = new XDoc("foo").Value("tab: \t");
            Assert.AreEqual("\"tab: \\t\"", doc.ToJson());
        }

        [Test]
        public void Will_escape_hex_quote() {
            var doc = new XDoc("foo").Value("hex: \u4f4f");
            Assert.AreEqual("\"hex: \\u4f4f\"", doc.ToJson());
        }
    }
}
