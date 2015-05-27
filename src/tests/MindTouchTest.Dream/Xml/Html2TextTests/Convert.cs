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

using MindTouch.Dream;
using MindTouch.Xml;
using NUnit.Framework;
using Environment = System.Environment;

namespace MindTouchTest.Xml.Html2TextTests {

    [TestFixture]
    public class Convert {

        //--- Class Methods ---
        private static void AssertConversion(string html, string text) {
            var doc = XDocFactory.From(html, MimeType.XML);
            Assert.AreEqual(text, new Html2Text().Convert(doc));
        }

        [Test]
        public void Can_convert_html() {
            AssertConversion(@"<html><body><script>script</script><h1>Title</h1><div>Paragraph with <b>bold</b> text</div><style>style</style><p class=""noindex"">don't index</p><div>Paragraph with <i>italic</i> text</div></body></html>",
                             "Title" + Environment.NewLine + "Paragraph with bold text" + Environment.NewLine + "Paragraph with italic text");
        }

        [Test]
        public void NoIndex_class_is_omitted_from_output() {
            AssertConversion(
                "<html><body>foo<i class=\"noindex\">bar</i>baz</body></html>",
                "foobaz"
            );
        }

        [Test]
        public void Script_is_removed() {
            AssertConversion(
                "<html><body>foo<script>bar</script>baz</body></html>",
                "foobaz"
            );
        }

        [Test]
        public void Style_is_removed() {
            AssertConversion(
                "<html><body>foo<style>bar</style>baz</body></html>",
                "foobaz"
            );
        }

        [Test]
        public void Only_body_content_is_considered() {
            AssertConversion(
                "<html>foo<body>bar</body>baz</html>",
                "bar"
            );
        }

        [Test]
        public void Body_with_target_is_ignored() {
            AssertConversion(
                "<html><body target=\"first\">foo</body><body>bar</body></html>",
                "bar"
            );
        }

        [Test]
        public void Block_elements_add_leading_and_trailing_linefeed() {
            AssertConversion(
                "<html><body>foo<div>bar</div>baz</body></html>",
                "foo" + Environment.NewLine + "bar" + Environment.NewLine + "baz"
            );
        }

        [Test]
        public void Non_block_elements_are_just_removed() {
            AssertConversion(
                "<html><body>foo<i>bar</i>baz</body></html>",
                "foobarbaz"
            );
        }

        [Test]
        public void Whitespace_stays_intact() {
            AssertConversion(
                "<html><body><i>foo</i> <i>bar</i></body></html>",
                "foo bar"
            );
        }
    }
}
