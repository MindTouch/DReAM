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
using log4net;
using MindTouch.IO;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class TrimmingTextReaderTests {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        [Test]
        public void Nothing_to_trim() {
            TextReader reader = new StringReader("this is plain text");
            reader = new TrimmingTextReader(reader);
            string result = reader.ReadToEnd();
            Assert.AreEqual("this is plain text", result);
        }

        [Test]
        public void Trim_from_the_beginning() {
            TextReader reader = new StringReader("   this is plain text");
            reader = new TrimmingTextReader(reader);
            string result = reader.ReadToEnd();
            Assert.AreEqual("this is plain text", result);
        }

        [Test]
        public void Trim_from_the_end() {
            TextReader reader = new StringReader("this is plain text   ");
            reader = new TrimmingTextReader(reader);
            string result = reader.ReadToEnd();
            Assert.AreEqual("this is plain text", result);
        }

        [Test]
        public void Trim_both_the_beginning_and_end() {
            TextReader reader = new StringReader("   this is plain text   ");
            reader = new TrimmingTextReader(reader);
            string result = reader.ReadToEnd();
            Assert.AreEqual("this is plain text", result);
        }

        [Test]
        public void Trim_both_the_beginning_and_end_via_readbuffer() {
            TextReader reader = new StringReader("   this is plain text   ");
            reader = new TrimmingTextReader(reader);
            var buffer = new char[1024];
            var read = reader.Read(buffer, 0, buffer.Length);
            var result = new StringBuilder();
            result.Append(buffer, 0, read);
            Assert.AreEqual("this is plain text", result.ToString());
        }

        [Test]
        public void Readbuffer_in_multiple_attempts_with_whitespace_between_chunks() {
            var text = "   this is some text                                          with lots of whitespace in the middle   ";
            var reader = new TrimmingTextReader(new StringReader(text));
            var result = new StringBuilder();
            var buffer = new char[1024];
            var read = reader.Read(buffer, 0, 24);
            var total = read;
            Assert.AreEqual(24, read);
            Assert.AreEqual('t', buffer[0]);
            read = reader.Read(buffer, read, buffer.Length);
            total += read;
            result.Append(buffer, 0, total);
            Assert.AreEqual("this is some text                                          with lots of whitespace in the middle", result.ToString());
        }

        [Test]
        public void Readbuffer_in_multiple_attempts_with_more_whitespace_than_buffersize_between_chunks() {
            var text = "this is some text                                                                                                                                                                                                                                                                                           with lots of whitespace in the middle";
            var reader = new TrimmingTextReader(new StringReader(text));
            var result = new StringBuilder();
            var buffer = new char[1024];
            var read = reader.Read(buffer, 0, 24);
            var total = read;
            Assert.AreEqual(24, read);
            Assert.AreEqual('t', buffer[0]);
            read = reader.Read(buffer, read, buffer.Length);
            total += read;
            result.Append(buffer, 0, total);
            Assert.AreEqual(text, result.ToString());
        }

        [Test]
        public void Readbuffer_in_multiple_attempts_with_more_whitespace_than_buffersize_trailing() {
            var text = "this is some text                                                                                                                                                                                                                                                                                    ";
            var reader = new TrimmingTextReader(new StringReader(text));
            var result = new StringBuilder();
            var buffer = new char[1024];
            var read = reader.Read(buffer, 0, 24);
            var total = read;
            Assert.AreEqual('t', buffer[0]);
            read = reader.Read(buffer, read, buffer.Length);
            total += read;
            result.Append(buffer, 0, total);
            Assert.AreEqual("this is some text", result.ToString());
        }

        [Test]
        public void Trimming_reader_retries_against_stream_when_request_bytes_are_not_met() {
            var stream = new MockStream();
            using(var reader = new TrimmingTextReader(new StreamReader(stream, Encoding.ASCII))) {
                var buffer = new char[4000];
                var total = 0;
                while(total < buffer.Length) {
                    var read = reader.Read(buffer, total, buffer.Length - total);
                    _log.DebugFormat("requested {0}, got {1}", buffer.Length - total, read);
                    Assert.IsTrue(read > 0);
                    total += read;
                }
            }
        }

        [Test]
        public void Trimming_reader_retries_against_stream_when_request_bytes_are_not_met_using_Read() {
            var stream = new MockStream();
            using(var reader = new ReaderWrapper(new TrimmingTextReader(new StreamReader(stream, Encoding.ASCII)))) {
                var buffer = new char[4000];
                var total = 0;
                while(total < buffer.Length) {
                    var read = reader.Read(buffer, total, buffer.Length - total);
                    _log.DebugFormat("requested {0}, got {1}", buffer.Length - total, read);
                    Assert.IsTrue(read > 0);
                    total += read;
                }
            }
        }

        class ReaderWrapper : TextReader {
            private readonly TextReader _reader;

            public ReaderWrapper(TextReader reader) {
                _reader = reader;
            }

            public override int Read() {
                return _reader.Read();
            }
        }

        class MockStream : Stream {

            private static readonly ILog _log = LogUtils.CreateLog();
            public override bool CanRead { get { return true; } }

            public override int Read(byte[] buffer, int offset, int count) {
                int read = count - 100;
                for(int i = 0; i < read; i++) {
                    buffer[offset + i] = (int)'a';
                }
                _log.DebugFormat("returning {0}",read);
                return read;
            }

            public override void Flush() { throw new NotImplementedException(); }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException(); }
            public override void SetLength(long value) { throw new NotImplementedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }
            public override bool CanSeek { get { throw new NotImplementedException(); } }
            public override bool CanWrite { get { throw new NotImplementedException(); } }
            public override long Length { get { throw new NotImplementedException(); } }
            public override long Position {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
        }
    }
}
