/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
using System.Linq;

using MindTouch.Dream;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.IO.Test {

    [TestFixture]
    public class ChunkedMemoryStreamTest {

        [Test]
        public void Write_100_bytes() {
            byte[] bytes = GetBytes(100);
            var stream = new ChunkedMemoryStream();
            stream.Write(bytes, 0, bytes.Length);
            Assert.AreEqual(100, stream.Length);
        }

        [Test]
        public void Write_100_bytes_Read_100_bytes() {
            byte[] bytes = GetBytes(100);
            var stream = new ChunkedMemoryStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;

            byte[] buffer = new byte[bytes.Length];
            int read = stream.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(buffer.Length, read);
            Assert.AreEqual(bytes, buffer);
            Assert.AreEqual(0, stream.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Write_100_bytes_100_times() {
            byte[] bytes = GetBytes(100);
            var stream = new ChunkedMemoryStream();
            for(int i = 0; i < 100; ++i) {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        [Test]
        public void Write_100_bytes_100_times_Read_100_bytes_100_times() {
            byte[] bytes = GetBytes(100);
            var stream = new ChunkedMemoryStream();
            for(int i = 0; i < 100; ++i) {
                stream.Write(bytes, 0, bytes.Length);
            }
            stream.Position = 0;

            byte[] buffer = new byte[bytes.Length];
            for(int i = 0; i < 100; ++i) {
                int read = stream.Read(buffer, 0, buffer.Length);
                Assert.AreEqual(buffer.Length, read);
                Assert.AreEqual(bytes, buffer);
            }
            Assert.AreEqual(0, stream.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Write_100_bytes_100_times_Read_10000_bytes() {
            byte[] bytes = GetBytes(100);
            byte[][] arrays = new byte[100][];
            var stream = new ChunkedMemoryStream();
            for(int i = 0; i < 100; ++i) {
                stream.Write(bytes, 0, bytes.Length);
                arrays[i] = bytes;
            }
            stream.Position = 0;

            byte[] buffer = new byte[100 * bytes.Length];
            int read = stream.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(buffer.Length, read);
            Assert.AreEqual(ArrayUtil.Concat(arrays), buffer);
            Assert.AreEqual(0, stream.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Write_64k_bytes() {
            byte[] bytes = GetBytes(64 * 1024);
            var stream = new ChunkedMemoryStream();
            stream.Write(bytes, 0, bytes.Length);
        }

        [Test]
        public void Write_64k_bytes_Read_64k_bytes() {
            byte[] bytes = GetBytes(64 * 1024);
            var stream = new ChunkedMemoryStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;

            byte[] buffer = new byte[bytes.Length];
            int read = stream.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(buffer.Length, read);
            Assert.AreEqual(bytes, buffer);
            Assert.AreEqual(0, stream.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Write_199k_bytes() {
            byte[] bytes = GetBytes(199 * 1024);
            var stream = new ChunkedMemoryStream();
            stream.Write(bytes, 0, bytes.Length);
        }

        [Test]
        public void Write_199k_bytes_Read_199k_bytes() {
            byte[] bytes = GetBytes(199 * 1024);
            var stream = new ChunkedMemoryStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;

            byte[] buffer = new byte[bytes.Length];
            int read = stream.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(buffer.Length, read);
            Assert.AreEqual(bytes, buffer);
            Assert.AreEqual(0, stream.Read(buffer, 0, buffer.Length));
        }


        [Test]
        public void Write_64k_bytes_Truncate_17k_Read_64k() {
            byte[] bytes = GetBytes(64 * 1024);
            var stream = new ChunkedMemoryStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.SetLength(17 * 1024);
            stream.Position = 0;

            byte[] buffer = new byte[bytes.Length];
            int read = stream.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(17 * 1024, read);
            Assert.AreEqual(bytes.Take(17 * 1024).ToArray(), buffer.Take(17 * 1024).ToArray());
            Assert.AreEqual(0, stream.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void Write_XDoc() {
            var doc = new XDoc("test").Start("content").Value("this is content").End();
            var stream = new ChunkedMemoryStream();
            doc.WriteTo(stream);
            stream.Position = 0;
            var newdoc = XDocFactory.From(stream, MimeType.XML);
            Assert.AreEqual(doc, newdoc, "xdoc changed during serialization");
        }

        [Test]
        public void Write_fixed_buffer() {
            var bytes = GetBytes(100);
            var stream = new ChunkedMemoryStream(bytes);
            var writtenBytes = GetBytes(10).Reverse().ToArray();
            stream.Write(writtenBytes, 0, writtenBytes.Length);

            stream.Position = 0;
            var readBytes = new byte[10];
            stream.Read(readBytes, 0, readBytes.Length);
            Assert.AreEqual(writtenBytes, readBytes);
        }

        [Test]
        public void Write_fixed_buffer_with_offset() {
            var bytes = GetBytes(100);
            var stream = new ChunkedMemoryStream(bytes, 10, 10);
            var writtenBytes = GetBytes(10).Reverse().ToArray();
            stream.Write(writtenBytes, 0, writtenBytes.Length);

            stream.Position = 0;
            var readBytes = new byte[10];
            stream.Read(readBytes, 0, readBytes.Length);
            Assert.AreEqual(writtenBytes, readBytes);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException))]
        public void Write_fixed_buffer_with_offset_and_overflow() {
            var bytes = GetBytes(100);
            var stream = new ChunkedMemoryStream(bytes, 10, 9);
            var writtenBytes = GetBytes(10).Reverse().ToArray();
            stream.Write(writtenBytes, 0, writtenBytes.Length);
        }

        [Test]
        public void Read_from_an_initialized_buffer() {
            var originalBytes = GetBytes(100);
            var stream = new ChunkedMemoryStream(originalBytes);
            var readBytes = new byte[100];
            stream.Read(readBytes, 0, readBytes.Length);
            Assert.AreEqual(originalBytes, readBytes);
        }

        private static byte[] GetBytes(int count) {
            var result = new byte[count];
            for(int i = 0; i < count; ++i) {
                unchecked {
                    result[i] = (byte)(i % 251);
                }
            }
            return result;
        }
    }
}
