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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MindTouch.Collections;
using MindTouch.IO;
using MindTouch.Tasking;
using Moq;
using NUnit.Framework;

namespace MindTouch.Dream.Test.TransactionalQueue {
    [TestFixture]
    public class TransactionalQueueTests {
        private Mock<IQueueStream> _mockQueueStream;
        private Mock<IQueueItemSerializer<string>> _mockSerializer;
        private TransactionalQueue<string> _q;
        private Mock<IQueueStreamHandle> _mockHandle;

        [SetUp]
        public void Setup() {
            _mockQueueStream = new Mock<IQueueStream>();
            _mockSerializer = new Mock<IQueueItemSerializer<string>>();
            _mockHandle = new Mock<IQueueStreamHandle>();
            _q = new TransactionalQueue<string>(_mockQueueStream.Object, _mockSerializer.Object);
        }

        [Test]
        public void Can_put_item() {

            // Arrange
            var mockStream = new Mock<Stream>();
            mockStream.Setup(x => x.Length).Returns(10);
            _mockSerializer.Setup(x => x.ToStream("foo")).Returns(mockStream.Object).Verifiable();
            _mockQueueStream.Setup(x => x.AppendRecord(mockStream.Object, 10)).Verifiable();

            // Act
            _q.Enqueue("foo");

            // Assert
            _mockQueueStream.Verify();
            _mockSerializer.Verify();
        }

        [Test]
        public void Can_peek_item() {

            // Arrange
            PrepQueueStreamRead();

            // Act
            var item = _q.Dequeue();

            // Assert
            Assert.AreEqual("foo", item.Value);
            Assert.IsNotNull(item);
            _mockQueueStream.Verify();
            _mockSerializer.Verify();
        }

        [Test]
        public void Can_peek_empty_queue() {

            // Arrange
            _mockQueueStream.Setup(x => x.ReadNextRecord()).Returns(QueueStreamRecord.Empty).Verifiable();

            // Act
            var item = _q.Dequeue();

            // Assert
            Assert.AreEqual(null, item);
            Assert.IsNull(item);
            _mockQueueStream.Verify();
            _mockSerializer.Verify(x => x.FromStream(It.IsAny<Stream>()), Times.Never());
        }

        [Test]
        public void Take_removes_item_from_queue() {

            // Arrange
            PrepQueueStreamRead();
            var item = _q.Dequeue();
            var r2 = new Result();
            r2.Return();
            _mockQueueStream.Setup(x => x.DeleteRecord(_mockHandle.Object)).Verifiable();

            // Act/Assert
            Assert.IsTrue(_q.CommitDequeue(item.Id));
            _mockSerializer.Verify();
            _mockQueueStream.Verify();
        }

        [Test]
        public void Take_a_second_time_returns_false() {

            // Arrange
            PrepQueueStreamRead();
            var item = _q.Dequeue();
            _mockQueueStream.Setup(x => x.DeleteRecord(_mockHandle.Object)).Verifiable();
            _q.CommitDequeue(item.Id);

            // Act/Assert
            Assert.IsFalse(_q.CommitDequeue(item.Id));
            _mockSerializer.Verify();
            _mockQueueStream.Verify();
            _mockQueueStream.Verify(x => x.DeleteRecord(_mockHandle.Object), Times.Once());
        }

        [Test]
        public void Take_expired_item_returns_false() {

            // Arrange
            PrepQueueStreamRead();
            var item = _q.Dequeue(TimeSpan.FromSeconds(1));

            // Act
            Thread.Sleep(2000);

            // Assert
            Assert.IsFalse(_q.CommitDequeue(item.Id));
            _mockSerializer.Verify();
            _mockQueueStream.Verify();
            _mockQueueStream.Verify(x => x.DeleteRecord(_mockHandle.Object), Times.Never());
        }

        [Test]
        public void Peek_after_pending_expires_returns_same_data_again() {

            // Arrange
            PrepQueueStreamRead();

            // Act
            var item = _q.Dequeue(TimeSpan.FromMilliseconds(100));
            Thread.Sleep(500);
            var item2 = _q.Dequeue();

            // Assert
            Assert.AreEqual("foo", item2.Value);
            Assert.AreEqual(item.Value, item2.Value);
            Assert.AreNotEqual(item.Id, item2.Id);
            _mockSerializer.Verify();
            _mockQueueStream.Verify(x => x.ReadNextRecord(), Times.Once());
            _mockQueueStream.Verify(x => x.DeleteRecord(_mockHandle.Object), Times.Never());
        }

        [Test]
        public void Peek_with_pending_item_returns_new_data() {

            // Arrange
            var mockStream = new Mock<Stream>();
            var items = new Queue<string>();
            items.Enqueue("foo");
            items.Enqueue("bar");
            _mockSerializer.Setup(x => x.FromStream(mockStream.Object)).Returns(items.Dequeue);
            _mockQueueStream.Setup(x => x.ReadNextRecord())
                .Returns(() => new QueueStreamRecord(mockStream.Object, _mockHandle.Object));

            // Act
            var item1 = _q.Dequeue();
            var item2 = _q.Dequeue();

            // Assert
            Assert.AreEqual("foo", item1.Value);
            Assert.AreEqual("bar", item2.Value);
            Assert.AreNotEqual(item1.Id, item2.Id);
            _mockSerializer.Verify(x => x.FromStream(mockStream.Object), Times.Exactly(2));
            _mockQueueStream.Verify(x => x.ReadNextRecord(), Times.Exactly(2));
            _mockQueueStream.Verify(x => x.DeleteRecord(_mockHandle.Object), Times.Never());
        }

        [Test]
        public void Can_get_count() {

            // Arrange
            _mockQueueStream.Setup(x => x.UnreadCount).Returns(2).Verifiable();

            // Act / Assert
            Assert.AreEqual(2, _q.Count);
            _mockQueueStream.Verify();
        }

        [Test]
        public void Expired_items_affect_count() {

            // Arrange
            PrepQueueStreamRead();
            _mockQueueStream.Setup(x => x.UnreadCount).Returns(0).Verifiable();

            // Act
            _q.Dequeue(TimeSpan.FromMilliseconds(100));
            var count1 = _q.Count;
            Thread.Sleep(500);
            var count2 = _q.Count;

            // Assert
            Assert.AreEqual(0, count1);
            Assert.AreEqual(1, count2);
        }

        [Test]
        public void Clear_invalidates_pending_items() {

            // Arrange
            PrepQueueStreamRead();

            // Act/Assert
            var item1 = _q.Dequeue();
            var item2 = _q.Dequeue();
            Assert.IsTrue(_q.CommitDequeue(item1.Id));
            _q.Clear();
            Assert.IsFalse(_q.CommitDequeue(item2.Id));
            _mockQueueStream.Verify();
            _mockSerializer.Verify();
        }

        [Test]
        public void Clear_affects_count() {

            // Arrange
            PrepQueueStreamRead();

            // Act/Assert
            var item1 = _q.Dequeue();
            _q.RollbackDequeue(item1.Id);
            Assert.AreEqual(1, _q.Count);
            _q.Clear();
            Assert.AreEqual(0, _q.Count);
            _mockQueueStream.Verify();
            _mockSerializer.Verify();
        }

        [Test]
        public void Clear_truncates_queuestream() {
            _q.Clear();
            _mockQueueStream.Verify(x => x.Truncate(), Times.Exactly(1));
        }

        private void PrepQueueStreamRead() {
            var mockStream = new Mock<Stream>();
            _mockSerializer.Setup(x => x.FromStream(mockStream.Object)).Returns("foo").Verifiable();
            _mockQueueStream.Setup(x => x.ReadNextRecord()).Returns(new QueueStreamRecord(mockStream.Object, _mockHandle.Object)).Verifiable();
        }
    }
}