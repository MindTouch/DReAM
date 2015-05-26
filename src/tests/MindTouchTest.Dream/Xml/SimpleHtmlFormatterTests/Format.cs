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

using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouchTest.Xml.SimpleHtmlFormatterTests {

    [TestFixture]
    public class Format {

        [Test]
        public void Null_creates_empty_doc() {
            AssertHtml("<html><body><p></p>\n</body></html>", null);
        }

        [Test]
        public void Empty_string_creates_empty_doc() {
            AssertHtml("<html><body><p></p>\n</body></html>", "");
        }

        [Test]
        public void Entities_get_converted() {
            AssertHtml("<html><body><p>&amp;&lt;&gt;&quot;</p>\n</body></html>", "&<>\"");
        }

        [Test]
        public void Html_string_gets_encoded() {
            AssertHtml("<html><body><p>&lt;html&gt;&lt;body&gt;&lt;a href=&quot;http://foo.com/&quot;&gt;x&lt;/a&gt;&lt;/body&gt;&lt;/html&gt;</p>\n</body></html>", "<html><body><a href=\"http://foo.com/\">x</a></body></html>");
        }

        [Test]
        public void Running_spaces_create_nbsp_runs() {
            AssertHtml("<html><body><p>foo&nbsp; bar&nbsp;&nbsp; baz</p>\n</body></html>", "foo  bar   baz");
        }

        [Test]
        public void Double_line_breaks_define_paragraphs() {
            AssertHtml(
@"<html><body><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit.</p>
<p>Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.</p>
<p>Nam varius aliquet orci quis elementum. Donec in dapibus eros.</p>
<p>Pellentesque habitant morbi tristique senectus et netus et malesuada.</p>
<p>Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet</p>
</body></html>",

@"Lorem ipsum dolor sit amet, consectetur adipiscing elit.

Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.

Nam varius aliquet orci quis elementum. Donec in dapibus eros.

Pellentesque habitant morbi tristique senectus et netus et malesuada.

Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet");
        }

        [Test]
        public void Triple_and_more_line_breaks_also_define_paragraphs() {
            AssertHtml(
@"<html><body><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit.</p>
<p>Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.</p>
<p>Nam varius aliquet orci quis elementum. Donec in dapibus eros.</p>
<p>Pellentesque habitant morbi tristique senectus et netus et malesuada.</p>
<p>Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet</p>
</body></html>",

@"Lorem ipsum dolor sit amet, consectetur adipiscing elit.


Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.




Nam varius aliquet orci quis elementum. Donec in dapibus eros.

Pellentesque habitant morbi tristique senectus et netus et malesuada.


Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet");
        }

        [Test]
        public void Single_line_breaks_define_break_elements() {
            AssertHtml(
@"<html><body><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit.<br />
Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.<br />
Nam varius aliquet orci quis elementum. Donec in dapibus eros.<br />
Pellentesque habitant morbi tristique senectus et netus et malesuada.<br />
Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet</p>
</body></html>",

@"Lorem ipsum dolor sit amet, consectetur adipiscing elit.
Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.
Nam varius aliquet orci quis elementum. Donec in dapibus eros.
Pellentesque habitant morbi tristique senectus et netus et malesuada.
Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet");
        }

        [Test]
        public void Single_and_double_line_breaks_define_paragraphs_and_simple_breaks() {
            AssertHtml(
@"<html><body><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit.<br />
Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.</p>
<p>Nam varius aliquet orci quis elementum. Donec in dapibus eros.<br />
Pellentesque habitant morbi tristique senectus et netus et malesuada.</p>
<p>Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet</p>
</body></html>",

@"Lorem ipsum dolor sit amet, consectetur adipiscing elit.
Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.

Nam varius aliquet orci quis elementum. Donec in dapibus eros.
Pellentesque habitant morbi tristique senectus et netus et malesuada.

Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet");
        }


        private void AssertHtml(string html, string input) {
            Assert.AreEqual(html.Replace("\r\n", "\n"), SimpleHtmlFormatter.Format(input).ToXHtml().Replace("\r\n", "\n"));
        }
    }
}
