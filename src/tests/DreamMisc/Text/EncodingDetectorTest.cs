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

using System.IO;
using System.Text;
using log4net;
using MindTouch.Text;
using NUnit.Framework;

namespace MindTouch.Dream.Test.Text {

    [TestFixture]
    public class EncodingDetectorTest {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        private static Stream GetResourceStream(string resourceName) {
            var assembly = typeof(EncodingDetectorTest).Assembly;
            var stream = assembly.GetManifestResourceStream("MindTouch.Dream.Test.Resources." + resourceName);
            if(stream == null) {
                throw new FileNotFoundException("unable to load requested resource: " + resourceName);
            }
            return stream;
        }

        private static void AssertEncoding(string text, string resourceName, Encoding expectedEncoding, long streamOffset) {
            using(var stream = GetResourceStream(resourceName)) {
                stream.Position = streamOffset;
                using(var reader = new StreamReader(stream, expectedEncoding)) {
                    Assert.AreEqual(reader.ReadToEnd(), text, "content comparison failed");
                }
            }
        }

        //--- Methods ---
        
        [Test]
        public void Detect_encoding_for_HTML_file_with_UTF16LE_BOM() {
            const string resource = "html-bom-utf16le.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new BOMEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNotNull(encoding, "encoding detection failed");
                Assert.AreEqual(2, offset, "wrong stream position");

                string text;
                using(var reader = new StreamReader(stream, encoding)) {
                    text = reader.ReadToEnd();
                }

                Assert.AreEqual("Unicode", encoding.EncodingName);
                AssertEncoding(text, resource, Encoding.Unicode, offset);
                _log.DebugFormat("Detected encoding: {0}", encoding.EncodingName);
            }
        }

        [Test]
        public void Detect_encoding_for_HTML_file_with_UTF16BE_BOM() {
            const string resource = "html-bom-utf16be.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new BOMEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNotNull(encoding, "encoding detection failed");
                Assert.AreEqual(2, offset, "wrong stream position");

                string text;
                using(var reader = new StreamReader(stream, encoding)) {
                    text = reader.ReadToEnd();
                }

                Assert.AreEqual("Unicode (Big-Endian)", encoding.EncodingName);
                AssertEncoding(text, resource, Encoding.BigEndianUnicode, offset);
                _log.DebugFormat("Detected encoding: {0}", encoding.EncodingName);
            }
        }

        [Test]
        public void Detect_encoding_for_HTML_file_with_UTF8_BOM() {
            const string resource = "html-bom-utf8.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new BOMEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNotNull(encoding, "encoding detection failed");
                Assert.AreEqual(3, offset, "wrong stream position");

                string text;
                using(var reader = new StreamReader(stream, encoding)) {
                    text = reader.ReadToEnd();
                }

                Assert.AreEqual("Unicode (UTF-8)", encoding.EncodingName);
                AssertEncoding(text, resource, Encoding.UTF8, offset);
                _log.DebugFormat("Detected encoding: {0}", encoding.EncodingName);
            }
        }

        [Test]
        public void Detect_encoding_for_XML_file_with_BIG5() {
            const string resource = "xml-big5.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new CharacterEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNotNull(encoding, "encoding detection failed");
                Assert.AreEqual(0, offset, "wrong stream position");

                string text;
                using(var reader = new StreamReader(stream, encoding)) {
                    text = reader.ReadToEnd();
                }

                Assert.AreEqual("Chinese Traditional (Big5)", encoding.EncodingName);
                AssertEncoding(text, resource, Encoding.GetEncoding("Big5"), offset);
                _log.DebugFormat("Detected encoding: {0}", encoding.EncodingName);
            }
        }

        [Test]
        public void Detect_encoding_for_XML_file_with_GB2312() {
            const string resource = "xml-gb2312.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new CharacterEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNotNull(encoding, "encoding detection failed");
                Assert.AreEqual(0, offset, "wrong stream position");

                string text;
                using(var reader = new StreamReader(stream, encoding)) {
                    text = reader.ReadToEnd();
                }

                Assert.AreEqual("Chinese Simplified (GB2312)", encoding.EncodingName);
                AssertEncoding(text, resource, Encoding.GetEncoding("GB2312"), offset);
                _log.DebugFormat("Detected encoding: {0}", encoding.EncodingName);
            }
        }

        [Test]
        public void Detect_encoding_for_XML_file_with_UTF8_chinese() {
            const string resource = "xml-utf8-chinese.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new CharacterEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNotNull(encoding, "encoding detection failed");
                Assert.AreEqual(0, offset, "wrong stream position");

                string text;
                using(var reader = new StreamReader(stream, encoding)) {
                    text = reader.ReadToEnd();
                }

                Assert.AreEqual("Unicode (UTF-8)", encoding.EncodingName);
                AssertEncoding(text, resource, Encoding.UTF8, offset);
                _log.DebugFormat("Detected encoding: {0}", encoding.EncodingName);
            }
        }

        [Test, Ignore("for some reason, CharDet estimates EUC-JP to be the worst fit. :(")]
        public void Detect_encoding_for_XML_file_with_EUC_JP() {

            // BUGBUGBUG (steveb): for some reason, CharDet estimates EUC-JP to be the worst fit. :(

            const string resource = "xml-euc-jp.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new CharacterEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNotNull(encoding, "encoding detection failed");
                Assert.AreEqual(0, offset, "wrong stream position");

                string text;
                using(var reader = new StreamReader(stream, encoding)) {
                    text = reader.ReadToEnd();
                }

                Assert.AreEqual("EUC-JP", encoding.EncodingName);
                AssertEncoding(text, resource, Encoding.GetEncoding("EUC-JP"), offset);
                _log.DebugFormat("Detected encoding: {0}", encoding.EncodingName);
            }
        }

        [Test]
        public void Detect_encoding_for_XML_file_with_ISO_2022() {
            const string resource = "xml-iso-2022-jp.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new CharacterEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNotNull(encoding, "encoding detection failed");
                Assert.AreEqual(0, offset, "wrong stream position");

                string text;
                using(var reader = new StreamReader(stream, encoding)) {
                    text = reader.ReadToEnd();
                }

                Assert.AreEqual("Japanese (JIS)", encoding.EncodingName);
                AssertEncoding(text, resource, Encoding.GetEncoding("iso-2022-jp"), offset);
                _log.DebugFormat("Detected encoding: {0}", encoding.EncodingName);
            }
        }

        [Test]
        public void Detect_encoding_for_XML_file_with_LE_JP() {
            const string resource = "xml-little-endian-jp.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new CharacterEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNotNull(encoding, "encoding detection failed");
                Assert.AreEqual(0, offset, "wrong stream position");

                string text;
                using(var reader = new StreamReader(stream, encoding)) {
                    text = reader.ReadToEnd();
                }

                Assert.AreEqual("Unicode", encoding.EncodingName);
                AssertEncoding(text, resource, Encoding.Unicode, offset);
                _log.DebugFormat("Detected encoding: {0}", encoding.EncodingName);
            }
        }

        [Test]
        public void Detect_encoding_for_XML_file_with_Shift_JIS() {
            const string resource = "xml-shift-jis.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new CharacterEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNotNull(encoding, "encoding detection failed");
                Assert.AreEqual(0, offset, "wrong stream position");

                string text;
                using(var reader = new StreamReader(stream, encoding)) {
                    text = reader.ReadToEnd();
                }

                Assert.AreEqual("Japanese (Shift-JIS)", encoding.EncodingName);
                AssertEncoding(text, resource, Encoding.GetEncoding("shift_jis"), offset);
                _log.DebugFormat("Detected encoding: {0}", encoding.EncodingName);
            }
        }

        [Test]
        public void Detect_encoding_for_XML_file_with_UTF8_JP() {
            const string resource = "xml-utf-8-jp.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new CharacterEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNotNull(encoding, "encoding detection failed");
                Assert.AreEqual(0, offset, "wrong stream position");

                string text;
                using(var reader = new StreamReader(stream, encoding)) {
                    text = reader.ReadToEnd();
                }

                Assert.AreEqual("Unicode (UTF-8)", encoding.EncodingName);
                AssertEncoding(text, resource, Encoding.UTF8, offset);
                _log.DebugFormat("Detected encoding: {0}", encoding.EncodingName);
            }
        }

        [Test]
        public void Detect_encoding_for_XML_file_with_UTF16_JP() {
            const string resource = "xml-utf-16-jp.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new CharacterEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNotNull(encoding, "encoding detection failed");
                Assert.AreEqual(0, offset, "wrong stream position");

                string text;
                using(var reader = new StreamReader(stream, encoding)) {
                    text = reader.ReadToEnd();
                }

                Assert.AreEqual("Unicode (Big-Endian)", encoding.EncodingName);
                AssertEncoding(text, resource, Encoding.BigEndianUnicode, offset);
                _log.DebugFormat("Detected encoding: {0}", encoding.EncodingName);
            }
        }

        [Test]
        public void Detect_encoding_for_HTML_file_with_Windows1252() {
            const string resource = "html-meta-windows-1252.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new HtmlMetaEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNotNull(encoding, "encoding detection failed");
                Assert.AreEqual(0, offset, "wrong stream position");

                string text;
                using(var reader = new StreamReader(stream, encoding)) {
                    text = reader.ReadToEnd();
                }

                Assert.AreEqual("Western European (Windows)", encoding.EncodingName);
                AssertEncoding(text, resource, Encoding.GetEncoding("Windows-1252"), offset);
                _log.DebugFormat("Detected encoding: {0}", encoding.EncodingName);
            }
        }

        [Test]
        public void Detect_encoding_for_HTML_file_without_meta() {
            const string resource = "html-bom-utf8.txt";
            using(var stream = GetResourceStream(resource)) {
                var detector = new HtmlMetaEncodingDetector();
                var encoding = detector.Detect(stream);
                var offset = stream.Position;
                Assert.IsNull(encoding, "encoding detection failed");
                Assert.AreEqual(0, offset, "wrong stream position");
            }
        }
    }
}
