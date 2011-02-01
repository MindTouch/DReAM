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
using System.IO;
using log4net;
using MindTouch.IO;
using NUnit.Framework;

namespace MindTouch.Dream.Test.TransactionalQueue {
    [TestFixture]
    public class MultiFileQueueStreamTests {

        private static readonly ILog _log = LogUtils.CreateLog();
        private static readonly byte[] RecordMarker = new byte[] { 0, 0, 255, 1 };
        private static readonly byte[] DeletedMarker = new byte[] { 0, 0, 1, 255 };

        private string _path;
        private string _firstFile;

        [SetUp]
        public void Setup() {
            _path = Path.Combine(Path.GetTempPath(), StringUtil.CreateAlphaNumericKey(6));
            _firstFile = Path.Combine(_path, "data_1.bin");
        }

        [TearDown]
        public void Teardown() {
        }

        [Test]
        public void Creates_path_if_not_exists() {
            var path = Path.Combine(Path.GetTempPath(), StringUtil.CreateAlphaNumericKey(6));
            var instance1 = new MultiFileQueueStream(path);
            Assert.IsTrue(Directory.Exists(path));
        }

        [Test]
        public void Trying_to_open_two_instances_on_same_path_throws() {
            var path = Path.GetTempPath();
            var instance1 = new MultiFileQueueStream(path);
            try {
                var instance2 = new MultiFileQueueStream(path);
            } catch(IOException) {
                return;
            }
            Assert.Fail("didn't throw IOException");
        }

        [Test]
        public void Check_Helpers() {
            var position = AppendRecord(5, false);
            AssertRecord(5, 0, false);
            AppendRecord(15, true);
            AssertRecord(15, position, true);
        }

        [Test]
        public void Check_Helpers2() {
            var p1 = AppendRecord(1, false);
            var p2 = AppendRecord(2, true);
            var p3 = AppendRecord(3, false);
            var p4 = AppendRecord(4, true);
            var p5 = AppendRecord(5, false);
            AssertRecord(1, 0, false);
            AssertRecord(2, p1, true);
            AssertRecord(3, p2, false);
            AssertRecord(4, p3, true);
            AssertRecord(5, p4, false);
        }

        [Test]
        public void Count_on_empty_stream_returns_0() {
            var queueStream = new MultiFileQueueStream(_path);
            Assert.AreEqual(0, queueStream.UnreadCount);
        }

        [Test]
        public void Mix_of_deleted_and_undeleted_initializes_Count_to_undeleted_only() {
            AppendRecord(1, false);
            AppendRecord(2, true);
            AppendRecord(3, false);
            AppendRecord(4, true);
            var p = AppendRecord(5, false);
            _log.DebugFormat("stream length: {0}", p);
            var queueStream = new MultiFileQueueStream(_path);
            Assert.AreEqual(3, queueStream.UnreadCount);
        }

        [Test]
        public void Can_append_record() {
            using(var queueStream = new MultiFileQueueStream(_path)) {
                var stream = GetStream(10);
                queueStream.AppendRecord(stream, stream.Length);
                Assert.AreEqual(1, queueStream.UnreadCount);
            }
            AssertRecord(10, 0, false);
        }

        [Test]
        public void Can_append_record_to_existing_queue() {
            AppendRecord(1, false);
            var p = AppendRecord(2, true);
            using(var queueStream = new MultiFileQueueStream(_path)) {
                var stream = GetStream(10);
                queueStream.AppendRecord(stream, stream.Length);
                Assert.AreEqual(2, queueStream.UnreadCount);
            }
            AssertRecord(10, p, false);
        }

        [Test]
        public void Can_read_consecutive_records() {
            AppendRecord(1, false);
            AppendRecord(2, true);
            AppendRecord(3, false);
            var queueStream = new MultiFileQueueStream(_path);
            Assert.AreEqual(1, GetValue(queueStream.ReadNextRecord().Stream));
            Assert.AreEqual(3, GetValue(queueStream.ReadNextRecord().Stream));
            Assert.AreEqual(0, queueStream.UnreadCount);
        }

        [Test]
        public void Can_delete_record() {
            AppendRecord(1, false);
            AppendRecord(2, false);
            using(var queueStream = new MultiFileQueueStream(_path)) {
                var handle = queueStream.ReadNextRecord().Handle;
                queueStream.DeleteRecord(handle);
            }
            AssertRecord(1, 0, true);
        }

        [Test]
        public void Deleting_last_record_truncates_file() {
            AppendRecord(1, false);
            AppendRecord(2, true);
            AppendRecord(3, false);
            using(var queueStream = new MultiFileQueueStream(_path)) {
                var handle1 = queueStream.ReadNextRecord().Handle;
                var handle2 = queueStream.ReadNextRecord().Handle;
                queueStream.DeleteRecord(handle1);
                queueStream.DeleteRecord(handle2);
            }
            Assert.AreEqual(0, File.Open(_firstFile, FileMode.Open).Length);
        }

        [Test]
        public void Can_append_record_to_truncated_file() {
            AppendRecord(1, false);
            using(var queueStream = new MultiFileQueueStream(_path)) {
                var handle = queueStream.ReadNextRecord().Handle;
                queueStream.DeleteRecord(handle);
                var s1 = GetStream(10);
                queueStream.AppendRecord(s1, s1.Length);
                Assert.AreEqual(1, queueStream.UnreadCount);
            }
            AssertRecord(10, 0, false);
        }

        [Test]
        public void Byte_overage_spills_into_consecutive_files() {
            using(var queueStream = new MultiFileQueueStream(_path, 4)) {
                var s1 = GetStream(1);
                var s2 = GetStream(2);
                var s3 = GetStream(3);
                var s4 = GetStream(4);
                queueStream.AppendRecord(s1, s1.Length);
                queueStream.AppendRecord(s2, s2.Length);
                queueStream.AppendRecord(s3, s3.Length);
                queueStream.AppendRecord(s4, s4.Length);
                Assert.AreEqual(5, Directory.GetFiles(_path).Length);
            }
        }

        [Test]
        public void Creating_queue_from_chunks_reports_proper_unreadcount() {
            using(var queueStream = new MultiFileQueueStream(_path, 4)) {
                var s1 = GetStream(1);
                var s2 = GetStream(2);
                var s3 = GetStream(3);
                var s4 = GetStream(4);
                queueStream.AppendRecord(s1, s1.Length);
                queueStream.AppendRecord(s2, s2.Length);
                queueStream.AppendRecord(s3, s3.Length);
                queueStream.AppendRecord(s4, s4.Length);
            }
            using(var queueStream = new MultiFileQueueStream(_path, 4)) {
                Assert.AreEqual(4, queueStream.UnreadCount);
            }
        }

        [Test]
        public void Item_depletion_removes_read_files() {
            using(var queueStream = new MultiFileQueueStream(_path, 4)) {
                var s1 = GetStream(1);
                var s2 = GetStream(2);
                var s3 = GetStream(3);
                var s4 = GetStream(4);
                queueStream.AppendRecord(s1, s1.Length);
                queueStream.AppendRecord(s2, s2.Length);
                queueStream.AppendRecord(s3, s3.Length);
                queueStream.AppendRecord(s4, s4.Length);
                var h1 = queueStream.ReadNextRecord();
                var h2 = queueStream.ReadNextRecord();
                var h3 = queueStream.ReadNextRecord();
                var h4 = queueStream.ReadNextRecord();
                Assert.IsNotNull(h4);
                Assert.AreEqual(5, Directory.GetFiles(_path).Length);
                queueStream.DeleteRecord(h2.Handle);
                Assert.AreEqual(4, Directory.GetFiles(_path).Length);
                queueStream.DeleteRecord(h3.Handle);
                Assert.AreEqual(3, Directory.GetFiles(_path).Length);
                queueStream.DeleteRecord(h1.Handle);
                Assert.AreEqual(2, Directory.GetFiles(_path).Length);
                queueStream.DeleteRecord(h4.Handle);
                Assert.AreEqual(1, Directory.GetFiles(_path).Length);
            }
        }

        [Test]
        public void Item_depletion_of_head_does_nothing_to_file_count() {
            using(var queueStream = new MultiFileQueueStream(_path, 14)) {
                var s1 = GetStream(1);
                var s2 = GetStream(2);
                var s3 = GetStream(3);
                var s4 = GetStream(4);
                var s5 = GetStream(5);
                queueStream.AppendRecord(s1, s1.Length);
                queueStream.AppendRecord(s2, s2.Length);
                queueStream.AppendRecord(s3, s3.Length);
                queueStream.AppendRecord(s4, s4.Length);
                queueStream.AppendRecord(s5, s5.Length);
                Assert.AreEqual(3, Directory.GetFiles(_path).Length);
                var h1 = queueStream.ReadNextRecord();
                var h2 = queueStream.ReadNextRecord();
                var h3 = queueStream.ReadNextRecord();
                var h4 = queueStream.ReadNextRecord();
                var h5 = queueStream.ReadNextRecord();
                Assert.IsNotNull(h5);
                queueStream.DeleteRecord(h5.Handle);
                Assert.AreEqual(3, Directory.GetFiles(_path).Length);
            }
        }

        [Test]
        public void Item_depletion_of_head_resets_file_numbering() {
            using(var queueStream = new MultiFileQueueStream(_path, 14)) {
                var s1 = GetStream(1);
                var s2 = GetStream(2);
                var s3 = GetStream(3);
                var s4 = GetStream(4);
                var s5 = GetStream(5);
                queueStream.AppendRecord(s1, s1.Length);
                queueStream.AppendRecord(s2, s2.Length);
                queueStream.AppendRecord(s3, s3.Length);
                queueStream.AppendRecord(s4, s4.Length);
                queueStream.AppendRecord(s5, s5.Length);
                Assert.AreEqual(3, Directory.GetFiles(_path).Length);
                var h1 = queueStream.ReadNextRecord();
                var h2 = queueStream.ReadNextRecord();
                var h3 = queueStream.ReadNextRecord();
                var h4 = queueStream.ReadNextRecord();
                var h5 = queueStream.ReadNextRecord();
                Assert.IsNotNull(h5);
                queueStream.DeleteRecord(h1.Handle);
                queueStream.DeleteRecord(h2.Handle);
                queueStream.DeleteRecord(h3.Handle);
                queueStream.DeleteRecord(h4.Handle);
                var files = Directory.GetFiles(_path);
                Assert.AreEqual(1, files.Length);
                Assert.AreEqual("data_3.bin", Path.GetFileName(files[0]));
                queueStream.DeleteRecord(h5.Handle);
                files = Directory.GetFiles(_path);
                Assert.AreEqual(1, files.Length);
                Assert.AreEqual("data_1.bin", Path.GetFileName(files[0]));
            }
        }

        [Test]
        public void Item_depletion_of_non_head_resets_file_numbering_if_all_other_files_are_empty_already() {
            using(var queueStream = new MultiFileQueueStream(_path, 14)) {
                var s1 = GetStream(1);
                var s2 = GetStream(2);
                var s3 = GetStream(3);
                var s4 = GetStream(4);
                var s5 = GetStream(5);
                queueStream.AppendRecord(s1, s1.Length);
                queueStream.AppendRecord(s2, s2.Length);
                queueStream.AppendRecord(s3, s3.Length);
                queueStream.AppendRecord(s4, s4.Length);
                queueStream.AppendRecord(s5, s5.Length);
                Assert.AreEqual(3, Directory.GetFiles(_path).Length);
                var h1 = queueStream.ReadNextRecord();
                var h2 = queueStream.ReadNextRecord();
                var h3 = queueStream.ReadNextRecord();
                var h4 = queueStream.ReadNextRecord();
                var h5 = queueStream.ReadNextRecord();
                Assert.IsNotNull(h5);
                queueStream.DeleteRecord(h1.Handle);
                queueStream.DeleteRecord(h2.Handle);
                queueStream.DeleteRecord(h3.Handle);
                queueStream.DeleteRecord(h5.Handle);
                var files = Directory.GetFiles(_path);
                Assert.AreEqual(2, files.Length);
                Assert.AreEqual("data_2.bin", Path.GetFileName(files[0]));
                Assert.AreEqual("data_3.bin", Path.GetFileName(files[1]));
                queueStream.DeleteRecord(h4.Handle);
                files = Directory.GetFiles(_path);
                Assert.AreEqual(1, files.Length);
                Assert.AreEqual("data_1.bin", Path.GetFileName(files[0]));
            }
        }

        [Test]
        public void Truncate_removes_files() {
            using(var queueStream = new MultiFileQueueStream(_path, 14)) {
                var s1 = GetStream(1);
                var s2 = GetStream(2);
                var s3 = GetStream(3);
                var s4 = GetStream(4);
                var s5 = GetStream(5);
                queueStream.AppendRecord(s1, s1.Length);
                queueStream.AppendRecord(s2, s2.Length);
                queueStream.AppendRecord(s3, s3.Length);
                queueStream.AppendRecord(s4, s4.Length);
                queueStream.AppendRecord(s5, s5.Length);
                Assert.AreEqual(3, Directory.GetFiles(_path).Length);
                queueStream.Truncate();
                Assert.AreEqual(1, Directory.GetFiles(_path).Length);
                Assert.AreEqual(0, queueStream.UnreadCount);
                Assert.AreEqual(QueueStreamRecord.Empty, queueStream.ReadNextRecord());
            }
        }

        [Test]
        public void Can_append_read_after_truncate() {
            using(var queueStream = new MultiFileQueueStream(_path, 14)) {
                var s1 = GetStream(1);
                var s2 = GetStream(2);
                queueStream.AppendRecord(s1, s1.Length);
                queueStream.AppendRecord(s2, s2.Length);
                queueStream.Truncate();
                var s3 = GetStream(3);
                queueStream.AppendRecord(s3, s3.Length);
                Assert.AreEqual(1, queueStream.UnreadCount);
                var h3 = queueStream.ReadNextRecord();
                Assert.AreEqual(0, queueStream.UnreadCount);
                Assert.AreEqual(GetValue(s3),GetValue(h3.Stream));
            }
        }

        private Stream GetStream(int value) {
            var stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(value));
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private int GetValue(Stream stream) {
            stream.Position = 0;
            return BitConverter.ToInt32(stream.ReadBytes(stream.Length), 0);
        }

        private void AssertRecord(int value, long position, bool deleted) {
            if(!Directory.Exists(_path)) {
                Directory.CreateDirectory(_path);
            }
            using(var stream = File.Open(_firstFile, FileMode.OpenOrCreate)) {
                stream.Seek(position, SeekOrigin.Begin);
                Assert.AreEqual(deleted ? DeletedMarker : RecordMarker, stream.ReadBytes(4));
                var bytes = BitConverter.GetBytes(value);
                var lengthBytes = BitConverter.GetBytes(bytes.Length);
                Assert.AreEqual(lengthBytes, stream.ReadBytes(lengthBytes.Length));
                Assert.AreEqual(bytes, stream.ReadBytes(bytes.Length));
            }
        }

        public long AppendRecord(int value, bool deleted) {
            if(!Directory.Exists(_path)) {
                Directory.CreateDirectory(_path);
            }
            using(var stream = File.Open(_firstFile, FileMode.OpenOrCreate)) {
                stream.Seek(0, SeekOrigin.End);
                stream.Write(deleted ? DeletedMarker : RecordMarker);
                var bytes = BitConverter.GetBytes(value);
                var lengthBytes = BitConverter.GetBytes(bytes.Length);
                stream.Write(lengthBytes);
                stream.Write(bytes);
                return stream.Position;
            }
        }
    }
}