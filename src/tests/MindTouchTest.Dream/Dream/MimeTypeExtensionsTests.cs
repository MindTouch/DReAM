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
using NUnit.Framework;

namespace MindTouchTest.Dream {
    
    [TestFixture]
    public class MimeTypeExtensionsTests {
        
        [Test]
        public void GIF() {
            Assert.AreEqual(".gif", MimeType.GIF.ToImageFileExtension());
        }

        [Test]
        public void JPEG() {
            Assert.AreEqual(".jpg", MimeType.JPEG.ToImageFileExtension());
        }

        [Test]
        public void PNG() {
            Assert.AreEqual(".png", MimeType.PNG.ToImageFileExtension());
        }

        [Test]
        public void SVG() {
            Assert.AreEqual(".svg", MimeType.SVG.ToImageFileExtension());
        }

        [Test]
        public void TIFF() {
            Assert.AreEqual(".tiff", MimeType.TIFF.ToImageFileExtension());
        }

        [Test]
        public void BMP() {
            Assert.AreEqual(".bmp", MimeType.BMP.ToImageFileExtension());
        }

        [Test]
        public void NullMimeType() {
            MimeType mimeType = null;
            Assert.IsNull(mimeType.ToImageFileExtension());
        }

        [Test]
        public void Unsupported_image_mime_type_is_null() {
            Assert.IsNull(MimeType.MSOFFICE_DOC.ToImageFileExtension());
        }
    }
}