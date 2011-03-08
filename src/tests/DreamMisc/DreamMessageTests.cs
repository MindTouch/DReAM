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
using log4net;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class DreamMessageTests {

        private static readonly ILog _log = LogUtils.CreateLog();

        [Test]
        public void Can_get_status_message_from_message() {
            var msg = DreamMessage.Conflict("huh?");
            Assert.AreEqual(string.Format("HTTP Status: {0}({1})", msg.Status, (int)msg.Status), DreamMessage.GetStatusStringOrNull(msg));
        }

        [Test]
        public void Trying_to_get_status_message_from_null_message_returns_null() {
            DreamMessage msg = null;
            Assert.IsNull(DreamMessage.GetStatusStringOrNull(msg));
        }

        [Test]
        public void DreamResponseException_from_message_contains_status_message_in_ToString() {
            var msg = DreamMessage.Conflict("huh?");
            var exception = new DreamResponseException(msg);
            Assert.IsTrue(exception.ToString().Contains(DreamMessage.GetStatusStringOrNull(msg)));
        }

        [Test]
        public void DreamResponseException_from_message_contains_status_message_as_exception_message() {
            var msg = DreamMessage.Conflict("huh?");
            var exception = new DreamResponseException(msg);
            Assert.AreEqual(DreamMessage.GetStatusStringOrNull(msg), exception.Message);
        }

        [Test]
        public void DreamResponseException_from_null_message_returns_default_exception_message() {
            DreamMessage msg = null;
            var exception = new DreamResponseException(msg);
            Assert.AreEqual("Exception of type 'MindTouch.Dream.DreamResponseException' was thrown.", exception.Message);
        }

        [Test]
        public void Can_clone_no_content_message() {
            var m = new DreamMessage(DreamStatus.Ok, new DreamHeaders().Add("foo", "bar"));
            m.Headers.Add("baz", "blah");
            var m2 = m.Clone();
            Assert.AreEqual(m.ToText(), m2.ToText());
            Assert.AreEqual(m.Headers["foo"], m2.Headers["foo"]);
            Assert.AreEqual(m.Headers["baz"], m2.Headers["baz"]);
        }

        [Test]
        public void Can_clone_xdoc_message() {
            var m = new DreamMessage(DreamStatus.Ok, new DreamHeaders().Add("foo", "bar"), new XDoc("doc"));
            m.Headers.Add("baz", "blah");
            var m2 = m.Clone();
            Assert.AreEqual(m.ToDocument().ToCompactString(), m2.ToDocument().ToCompactString());
            Assert.AreEqual(m.Headers["foo"], m2.Headers["foo"]);
            Assert.AreEqual(m.Headers["baz"], m2.Headers["baz"]);
        }

        [Test]
        public void Can_clone_byte_message() {
            var m = new DreamMessage(DreamStatus.Ok, new DreamHeaders().Add("foo", "bar"), MimeType.TIFF, new byte[] { 1, 2, 3, 4 });
            m.Headers.Add("baz", "blah");
            var m2 = m.Clone();
            Assert.AreEqual(m.ToBytes(), m2.ToBytes());
            Assert.AreEqual(m.ContentType.ToString(), m2.ContentType.ToString());
            Assert.AreEqual(m.Headers["foo"], m2.Headers["foo"]);
            Assert.AreEqual(m.Headers["baz"], m2.Headers["baz"]);
        }

        [Test]
        public void Can_clone_a_memory_stream_message() {
            var text = "blah";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(text);
            writer.Flush();
            stream.Position = 0;
            var m = new DreamMessage(DreamStatus.Ok, new DreamHeaders().Add("foo", "bar"), MimeType.TEXT, stream.Length, stream);
            m.Headers.Add("baz", "blah");
            _log.Debug("about to clone");
            var m2 = m.Clone();
            var reader = new StreamReader(m2.ToStream());
            Assert.AreEqual(text, reader.ReadToEnd());
            Assert.AreEqual(m.ContentType.ToString(), m2.ContentType.ToString());
            Assert.AreEqual(m.Headers["foo"], m2.Headers["foo"]);
            Assert.AreEqual(m.Headers["baz"], m2.Headers["baz"]);
        }

        [Test]
        public void Can_clone_a_memory_stream_message_that_has_been_read() {
            var text = "blah";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(text);
            writer.Flush();
            var m = new DreamMessage(DreamStatus.Ok, new DreamHeaders().Add("foo", "bar"), MimeType.TEXT, stream.Length, stream);
            m.Headers.Add("baz", "blah");
            var m2 = m.Clone();
            var reader = new StreamReader(m2.ToStream());
            Assert.AreEqual(text, reader.ReadToEnd());
            Assert.AreEqual(m.ContentType.ToString(), m2.ContentType.ToString());
            Assert.AreEqual(m.Headers["foo"], m2.Headers["foo"]);
            Assert.AreEqual(m.Headers["baz"], m2.Headers["baz"]);
        }

        [Test]
        public void Can_clone_a_null_stream_message() {
            var m = new DreamMessage(DreamStatus.Ok, new DreamHeaders().Add("foo", "bar"), MimeType.TEXT, Stream.Null.Length, Stream.Null);
            m.Headers.Add("baz", "blah");
            var m2 = m.Clone();
            Assert.AreEqual(0, m2.ContentLength);
            Assert.AreEqual(m.ContentType.ToString(), m2.ContentType.ToString());
            Assert.AreEqual(m.Headers["foo"], m2.Headers["foo"]);
            Assert.AreEqual(m.Headers["baz"], m2.Headers["baz"]);
        }

        [Test]
        public void Cannot_clone_a_filestream_message() {
            var file = Path.GetTempFileName();
            var text = "blah";
            File.WriteAllText(file, text);
            var m = DreamMessage.FromFile(file);
            m.Headers.Add("baz", "blah");
            try {
                var m2 = m.Clone();
                Assert.Fail("clone worked");
            } catch(InvalidOperationException) {
                return;
            }
        }
    }
}
