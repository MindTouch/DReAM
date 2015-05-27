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

using System.Text;
using MindTouch.Dream;
using NUnit.Framework;

namespace MindTouchTest.Dream.XDataUriTests {

    [TestFixture]
    public class TryParse {

        //--- Methods ---
        [Test]
        public void Parse_null_uri() {
            XDataUri uri;
            var success = XDataUri.TryParse(null, out uri);

            Assert.IsFalse(success, "null uri incorrectly recognized as data uri");
            Assert.IsNull(uri, "data uri result expected to be null");
        }

        [Test]
        public void Parse_absolute_uri() {
            XDataUri uri;
            var success = XDataUri.TryParse("http://localhost/foo/bar?q=abc", out uri);

            Assert.IsFalse(success, "absolute uri incorrectly recognized as data uri");
            Assert.IsNull(uri, "data uri result expected to be null");
        }

        [Test]
        public void Parse_relative_uri() {
            XDataUri uri;
            var success = XDataUri.TryParse("foo/bar?q=abc", out uri);

            Assert.IsFalse(success, "relative uri incorrectly recognized as data uri");
            Assert.IsNull(uri, "data uri result expected to be null");
        }

        [Test]
        public void Parse_image_data_uri() {
            XDataUri uri;
            var success = XDataUri.TryParse("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==", out uri);

            Assert.IsTrue(success, "data uri was not recognized");
            Assert.IsNotNull(uri, "data uri result was not initialized");
            Assert.AreEqual(uri.MimeType.MainType, "image", "data uri was not recognized as image");
            Assert.AreEqual(uri.MimeType.SubType, "png", "data uri was not recognized as png");
            Assert.IsTrue(uri.Base64, "data uri encoding was not correctly recognized");
        }

        [Test]
        public void Parse_html_data_uri() {
            XDataUri uri;
            var success = XDataUri.TryParse("data:text/html;charset=utf-8,%3C%21DOCTYPE%20html%3E%0D%0A%3Chtml%20lang%3D%22en%22%3E%0D%0A%3Chead%3E%3Ctitle%3EEmbedded%20Window%3C%2Ftitle%3E%3C%2Fhead%3E%0D%0A%3Cbody%3E%3Ch1%3E42%3C%2Fh1%3E%3C%2Fbody%3E%0A%3C%2Fhtml%3E%0A%0D%0A", out uri);

            Assert.IsTrue(success, "data uri was not recognized");
            Assert.IsNotNull(uri, "data uri result was not initialized");
            Assert.AreEqual(uri.MimeType.MainType, "text", "data uri was not recognized as text");
            Assert.AreEqual(uri.MimeType.SubType, "html", "data uri was not recognized as html");
            Assert.AreEqual(uri.MimeType.CharSet, Encoding.UTF8, "data uri charset was not recognized as utf-8");
            Assert.IsFalse(uri.Base64, "data uri encoding was not correctly recognized");
        }
    }
}
