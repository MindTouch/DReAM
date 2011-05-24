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
using System.IO;
using System.Text;
using MindTouch.Tasking;
using Moq;
using NUnit.Framework;
using MindTouch.IO;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class StreamUtilTests {

        [Test]
        public void Copy_memorized_stream_chunk() {
            var source = new MemoryStream();
            var writer = new StreamWriter(source) { AutoFlush = true };
            writer.Write("abcdefghijklmnop");
            source.Seek(0, SeekOrigin.Begin);
            var target = new MemoryStream();
            Assert.AreEqual(5, source.CopyTo(target, 5, new Result<long>()).Wait());
            Assert.AreEqual(5, target.Length);
            Assert.AreEqual(5, source.Position);
        }

        [Test]
        public void Copy_stream_with_undefined_length() {
            var bytes = GetBytes(1000);
            var source = new MemoryStream(bytes);
            var target = new MemoryStream();
            var count = source.CopyTo(target, -1, new Result<long>()).Wait();
            Assert.AreEqual(bytes.LongLength, count);
            Assert.AreEqual(bytes, target.ToArray());
        }

        [Test]
        public void Writing_string_larger_than_buffersize_to_stream_calls_stream_in_chunks() {
            var mockStream = new Mock<Stream>();
            var sb = new StringBuilder();
            for(var i = 0; i < StreamUtil.BUFFER_SIZE / sizeof(char) / 11 * 3 + 10; i++) {
                sb.AppendFormat("{0:0000}bottles", i);
            }
            var s = sb.ToString();
            var ms = new MemoryStream();
            mockStream.Setup(x => x.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                .Callback((byte[] bytes, int offset, int length) => ms.Write(bytes, offset, length));
            mockStream.Object.Write(Encoding.UTF8, s);
            mockStream.Verify(x => x.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()), Times.Exactly(4));
            Assert.AreEqual(s, Encoding.UTF8.GetString(ms.ToArray()));
        }

        [Test]
        public void Writing_string_smaller_than_buffersize_to_stream_does_not_call_stream_in_chunks() {
            var mockStream = new Mock<Stream>();
            var sb = new StringBuilder();
            for(var i = 0; i < StreamUtil.BUFFER_SIZE / sizeof(char) / 15; i++) {
                sb.AppendFormat("{0:0000}bottles", i);
            }
            var s = sb.ToString();
            var ms = new MemoryStream();
            mockStream.Setup(x => x.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                .Callback((byte[] bytes, int offset, int length) => ms.Write(bytes, offset, length));
            mockStream.Object.Write(Encoding.UTF8, s);
            mockStream.Verify(x => x.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()), Times.Once());
            Assert.AreEqual(s, Encoding.UTF8.GetString(ms.ToArray()));
        }

        [Test]
        public void Can_copy_stream_to_file() {
            var bytes = GetBytes(1000);
            var stream = new MemoryStream(bytes);
            var tempFile = Path.GetTempFileName();
            try {
                stream.CopyToFile(tempFile, stream.Length);
                var fileBytes = File.ReadAllBytes(tempFile);
                Assert.AreEqual(0, ArrayUtil.Compare(bytes, fileBytes));
            } finally {
                File.Delete(tempFile);
            }
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
