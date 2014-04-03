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

using System;
using System.IO;
using System.Text;
using log4net;
using MindTouch.IO;
using NUnit.Framework;

namespace MindTouch.Dream.Test.TransactionalQueue {
    [TestFixture]
    public class SingleFileQueueStreamTests {

        private static readonly ILog _log = LogUtils.CreateLog();
        private static readonly byte[] RecordMarker = new byte[] { 0, 0, 255, 1 };
        private static readonly byte[] DeletedMarker = new byte[] { 0, 0, 1, 255 };

        private Stream _stream;

        [SetUp]
        public void Setup() {
            _stream = new MemoryStream();
        }

        [Test]
        public void Creates_path_if_not_exists() {
            var path = Path.Combine(Path.Combine(Path.GetTempPath(), StringUtil.CreateAlphaNumericKey(6)), "queue.bin");
            var instance1 = new SingleFileQueueStream(path);
            Assert.IsTrue(File.Exists(path));
        }

        [Test]
        public void Trying_to_open_two_instances_on_same_path_throws() {
            var path = Path.GetTempFileName();
            var instance1 = new SingleFileQueueStream(path);
            try {
                var instance2 = new SingleFileQueueStream(path);
            } catch(IOException) {
                return;
            }
            Assert.Fail("didn't throw IOException");
        }

        [Test]
        public void Check_Helpers() {
            AppendRecord(5, false);
            AssertRecord(5, 0, false);
            var position = _stream.Position;
            AppendRecord(15, true);
            AssertRecord(15, position, true);
        }

        [Test]
        public void Check_Helpers2() {
            AppendRecord(1, false);
            AppendRecord(2, true);
            AppendRecord(3, false);
            AppendRecord(4, true);
            AppendRecord(5, false);
            AssertRecord(1, 0, false);
            AssertRecord(2, _stream.Position, true);
            AssertRecord(3, _stream.Position, false);
            AssertRecord(4, _stream.Position, true);
            AssertRecord(5, _stream.Position, false);
        }

        [Test]
        public void Count_on_empty_stream_returns_0() {
            var queueStream = new SingleFileQueueStream(_stream);
            Assert.AreEqual(0, queueStream.UnreadCount);
        }

        [Test]
        public void Mix_of_deleted_and_undeleted_initializes_Count_to_undeleted_only() {
            AppendRecord(1, false);
            AppendRecord(2, true);
            AppendRecord(3, false);
            AppendRecord(4, true);
            AppendRecord(5, false);
            _log.DebugFormat("stream length: {0}", _stream.Length);
            var queueStream = new SingleFileQueueStream(_stream);
            Assert.AreEqual(3, queueStream.UnreadCount);
        }

        [Test]
        public void Can_append_record() {
            var queueStream = new SingleFileQueueStream(_stream);
            using(var stream = new MemoryStream()) {
                stream.Write(BitConverter.GetBytes(10));
                stream.Seek(0, SeekOrigin.Begin);
                queueStream.AppendRecord(stream, stream.Length);
                AssertRecord(10, 0, false);
                Assert.AreEqual(1, queueStream.UnreadCount);
            }
        }

        [Test]
        public void Can_append_record_to_existing_queue() {
            AppendRecord(1, false);
            AppendRecord(2, true);
            var queueStream = new SingleFileQueueStream(_stream);
            using(var stream = new MemoryStream()) {
                stream.Write(BitConverter.GetBytes(10));
                stream.Seek(0, SeekOrigin.Begin);
                var position = _stream.Length;
                queueStream.AppendRecord(stream, stream.Length);
                AssertRecord(10, position, false);
                Assert.AreEqual(2, queueStream.UnreadCount);
            }
        }

        [Test]
        public void Can_read_consecutive_records() {
            AppendRecord(1, false);
            AppendRecord(2, true);
            AppendRecord(3, false);
            var queueStream = new SingleFileQueueStream(_stream);
            Assert.AreEqual(1, GetIntValue(queueStream.ReadNextRecord().Stream));
            Assert.AreEqual(3, GetIntValue(queueStream.ReadNextRecord().Stream));
            Assert.AreEqual(0, queueStream.UnreadCount);
        }

        [Test]
        public void Can_delete_record() {
            AppendRecord(1, false);
            AppendRecord(2, false);
            var queueStream = new SingleFileQueueStream(_stream);
            var handle = queueStream.ReadNextRecord().Handle;
            queueStream.DeleteRecord(handle);
            AssertRecord(1, 0, true);
        }

        [Test]
        public void Deleting_last_record_truncates_file() {
            AppendRecord(1, false);
            AppendRecord(2, true);
            AppendRecord(3, false);
            var queueStream = new SingleFileQueueStream(_stream);
            var handle1 = queueStream.ReadNextRecord().Handle;
            var handle2 = queueStream.ReadNextRecord().Handle;
            queueStream.DeleteRecord(handle1);
            queueStream.DeleteRecord(handle2);
            Assert.AreEqual(0, _stream.Length);
        }

        [Test]
        public void Can_append_record_to_truncated_file() {
            AppendRecord(1, false);
            var queueStream = new SingleFileQueueStream(_stream);
            var handle = queueStream.ReadNextRecord().Handle;
            queueStream.DeleteRecord(handle);
            using(var stream = new MemoryStream()) {
                stream.Write(BitConverter.GetBytes(10));
                stream.Seek(0, SeekOrigin.Begin);
                queueStream.AppendRecord(stream, stream.Length);
                AssertRecord(10, 0, false);
                Assert.AreEqual(1, queueStream.UnreadCount);
            }
        }

        [Test]
        public void Corrupt_record_is_ignored_at_initialization() {
            var v1 = "asdsdasdasdasdasd";
            var v2 = "asdasdsadasdwwdsdw";
            AppendRecord(v1, false);
            for(byte i = 0; i < 6; i++) {
                _stream.WriteByte(i);
            }
            AppendRecord(v2, false);
            var queueStream = new SingleFileQueueStream(_stream);
            Assert.AreEqual(2,queueStream.UnreadCount);
            Assert.AreEqual(v1, GetStringValue(queueStream.ReadNextRecord().Stream));
            Assert.AreEqual(v2, GetStringValue(queueStream.ReadNextRecord().Stream));
        }

        [Test]
        public void Corrupt_record_does_not_stop_further_appending_dequeueing() {
            var v1 = "asdsdasdasdasdasd";
            var v2 = "asdasdsadasdwwdsdw";
            AppendRecord(v1, false);
            for(byte i = 0; i < 6; i++) {
                _stream.WriteByte(i);
            }
            var position = _stream.Position;
            var queueStream = new SingleFileQueueStream(_stream);
            Assert.AreEqual(1, queueStream.UnreadCount);
            using(var stream = new MemoryStream()) {
                stream.Write(Encoding.UTF8.GetBytes(v2));
                stream.Seek(0, SeekOrigin.Begin);
                queueStream.AppendRecord(stream, stream.Length);
                AssertRecord(v2, position, false);
                Assert.AreEqual(2, queueStream.UnreadCount);
            }
            Assert.AreEqual(v1, GetStringValue(queueStream.ReadNextRecord().Stream));
            Assert.AreEqual(v2, GetStringValue(queueStream.ReadNextRecord().Stream));
        }

        private int GetIntValue(Stream stream) {
            return BitConverter.ToInt32(stream.ReadBytes(stream.Length), 0);
        }

        private void AssertRecord(int value, long position, bool deleted) {
            _stream.Seek(position, SeekOrigin.Begin);
            Assert.AreEqual(deleted ? DeletedMarker : RecordMarker, _stream.ReadBytes(4));
            var bytes = BitConverter.GetBytes(value);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            Assert.AreEqual(lengthBytes, _stream.ReadBytes(lengthBytes.Length));
            Assert.AreEqual(bytes, _stream.ReadBytes(bytes.Length));
        }

        public void AppendRecord(int value, bool deleted) {
            _stream.Write(deleted ? DeletedMarker : RecordMarker);
            var bytes = BitConverter.GetBytes(value);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            _stream.Write(lengthBytes);
            _stream.Write(bytes);
        }

        private string GetStringValue(Stream stream) {
            var bytes = stream.ReadBytes(stream.Length);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        private void AssertRecord(string value, long position, bool deleted) {
            _stream.Seek(position, SeekOrigin.Begin);
            Assert.AreEqual(deleted ? DeletedMarker : RecordMarker, _stream.ReadBytes(4));
            var bytes = Encoding.UTF8.GetBytes(value);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            Assert.AreEqual(lengthBytes, _stream.ReadBytes(lengthBytes.Length));
            Assert.AreEqual(bytes, _stream.ReadBytes(bytes.Length));
        }

        public void AppendRecord(string value, bool deleted) {
            _stream.Write(deleted ? DeletedMarker : RecordMarker);
            var bytes = Encoding.UTF8.GetBytes(value);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            _stream.Write(lengthBytes);
            _stream.Write(bytes);
        }
    }
}