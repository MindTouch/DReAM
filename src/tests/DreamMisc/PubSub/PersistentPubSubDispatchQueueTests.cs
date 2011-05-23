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
using System.Collections.Generic;
using System.IO;
using MindTouch.Dream.Services.PubSub;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test.PubSub {
    [TestFixture]
    public class PersistentPubSubDispatchQueueTests {

        [Test]
        public void Can_dispatch_items() {

            // Arrange
            var dispatched = new List<DispatchItem>();
            Func<DispatchItem, Result<bool>> handler = (i) => {
                dispatched.Add(i);
                var result = new Result<bool>();
                result.Return(true);
                return result;
            };
            var dispatchQueue = new PersistentPubSubDispatchQueue(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), TaskTimerFactory.Current, 1.Seconds(), handler);
            var item1 = new DispatchItem(new XUri("http://a"), new DispatcherEvent(new XDoc("msg"), new XUri("http://channl"), new XUri("http://resource")), "a");
            var item2 = new DispatchItem(new XUri("http://b"), new DispatcherEvent(new XDoc("msg"), new XUri("http://channl"), new XUri("http://resource")), "b");

            // Act
            dispatchQueue.Enqueue(item1);
            dispatchQueue.Enqueue(item2);

            // Assert
            Assert.IsTrue(Wait.For(() => dispatched.Count == 2, 5.Seconds()), "items were not dispatched in time");
            Assert.AreEqual(item1.Location, dispatched[0].Location, "wrong item location for first dispatched item");
            Assert.AreEqual(item2.Location, dispatched[1].Location, "wrong item location for second dispatched item");
        }

        [Test]
        public void Failed_dispatch_retries_after_sleep() {

            // Arrange
            var item1 = new DispatchItem(new XUri("http://a"), new DispatcherEvent(new XDoc("msg"), new XUri("http://channl"), new XUri("http://resource")), "a");
            var item2 = new DispatchItem(new XUri("http://b"), new DispatcherEvent(new XDoc("msg"), new XUri("http://channl"), new XUri("http://resource")), "b");

            var dispatched = new List<Tuplet<DateTime, DispatchItem>>();
            var dispatchCounter = 0;
            Func<DispatchItem, Result<bool>> handler = (i) => {
                dispatchCounter++;
                dispatched.Add(new Tuplet<DateTime, DispatchItem>(DateTime.UtcNow, i));
                var result = new Result<bool>();
                result.Return(dispatchCounter > 2);
                return result;
            };
            var dispatchQueue = new PersistentPubSubDispatchQueue(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), TaskTimerFactory.Current, 1.Seconds(), handler);

            // Act
            dispatchQueue.Enqueue(item1);
            dispatchQueue.Enqueue(item2);

            // Assert
            Assert.IsTrue(Wait.For(() => dispatched.Count >= 4, 10.Seconds()), "items were not dispatched in time");
            Assert.AreEqual(item1.Location, dispatched[0].Item2.Location, "wrong item location for first failure item");
            var dispatchTiming1 = dispatched[1].Item1 - dispatched[0].Item1;
            Assert.IsTrue(dispatchTiming1 >= 1.Seconds(), "expected re-try in more than 1 second, was " + dispatchTiming1);
            Assert.AreEqual(item1.Location, dispatched[1].Item2.Location, "wrong item location for second failure item");
            var dispatchTiming2 = dispatched[2].Item1 - dispatched[1].Item1;
            Assert.IsTrue(dispatchTiming2 >= 1.Seconds(), "expected re-try in more than 1 second, was " + dispatchTiming2);
            Assert.AreEqual(item1.Location, dispatched[2].Item2.Location, "wrong item location for first success item");
            var dispatchTiming3 = dispatched[3].Item1 - dispatched[2].Item1;
            Assert.IsTrue(dispatchTiming3 < 1.Seconds(), "expected successful dispatch in less than 1 second, was " + dispatchTiming3);
            Assert.AreEqual(item2.Location, dispatched[3].Item2.Location, "wrong item location for second success item");
        }

        [Test]
        public void Creating_a_queue_with_persisted_items_starts_dispatch_immediately() {

            // Arrange
            var queuePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var item1 = new DispatchItem(new XUri("http://a"), new DispatcherEvent(new XDoc("msg"), new XUri("http://channl"), new XUri("http://resource")), "a");

            var dispatchQueue = new PersistentPubSubDispatchQueue(queuePath, TaskTimerFactory.Current, 1.Seconds(), (item) => new Result<bool>().WithReturn(false));
            dispatchQueue.Enqueue(item1);
            dispatchQueue.Dispose();

            var dispatched = new List<DispatchItem>();
            Func<DispatchItem, Result<bool>> handler = (i) => {
                dispatched.Add(i);
                var result = new Result<bool>();
                result.Return(true);
                return result;
            };

            // Act
            dispatchQueue = new PersistentPubSubDispatchQueue(queuePath, TaskTimerFactory.Current, 1.Seconds(), handler);

            // Assert
            Assert.IsTrue(Wait.For(() => dispatched.Count == 1, 5.Seconds()), "item was not dispatched in time");
            Assert.AreEqual(item1.Location, dispatched[0].Location, "wrong item location for dispatched item");
        }

        [Test]
        public void ClearAndDispose_removes_queue_from_disk() {

            // Arrange
            var queuePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var item1 = new DispatchItem(new XUri("http://a"), new DispatcherEvent(new XDoc("msg"), new XUri("http://channl"), new XUri("http://resource")), "a");

            var dispatchQueue = new PersistentPubSubDispatchQueue(queuePath, TaskTimerFactory.Current, 1.Minutes(), (item) => new Result<bool>().WithReturn(false));
            dispatchQueue.Enqueue(item1);
            Assert.IsTrue(Directory.GetFiles(queuePath).Length > 0, "queue directory did not contain any files");

            // Act
            dispatchQueue.DeleteAndDispose();

            // Assert
            Assert.IsFalse(Directory.Exists(queuePath), "queue directory still exists");
        }

        [Test]
        public void Disposed_queue_throws_on_access() {

            // Arrange
            var queuePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var dispatchQueue = new PersistentPubSubDispatchQueue(queuePath, TaskTimerFactory.Current, 1.Minutes(), i => new Result<bool>().WithReturn(true));

            // Act
            dispatchQueue.Dispose();

            // Assert
            try {
                var item = new DispatchItem(new XUri("http://a"), new DispatcherEvent(new XDoc("msg"), new XUri("http://channl"), new XUri("http://resource")), "a");
                dispatchQueue.Enqueue(item);
                Assert.Fail("Enqueue didn't throw");
            } catch(ObjectDisposedException) {
            } catch(Exception e) {
                Assert.Fail(string.Format("Enqueue threw unexpected exception: {0}", e));
            }
        }

        [Test]
        public void ClearAndDisposed_queue_throws_on_access() {

            // Arrange
            var queuePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var dispatchQueue = new PersistentPubSubDispatchQueue(queuePath, TaskTimerFactory.Current, 1.Minutes(), i => new Result<bool>().WithReturn(true));

            // Act
            dispatchQueue.DeleteAndDispose();

            // Assert
            try {
                var item = new DispatchItem(new XUri("http://a"), new DispatcherEvent(new XDoc("msg"), new XUri("http://channl"), new XUri("http://resource")), "a");
                dispatchQueue.Enqueue(item);
                Assert.Fail("Enqueue didn't throw");
            } catch(ObjectDisposedException) {
            } catch(AssertionException) {
                throw;
            } catch(Exception e) {
                Assert.Fail(string.Format("Enqueue threw unexpected exception: {0}", e));
            }
        }

        [Test]
        public void Can_return_message_after_queue_has_been_disposed() {

            // Arrange
            var queuePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var item1 = new DispatchItem(new XUri("http://a"), new DispatcherEvent(new XDoc("msg"), new XUri("http://channl"), new XUri("http://resource")), "a");

            var dispatchResult = new Result<bool>();
            var dispatchQueue = new PersistentPubSubDispatchQueue(queuePath, TaskTimerFactory.Current, 1.Minutes(), i => dispatchResult);
            dispatchQueue.Enqueue(item1);

            // Act
            dispatchQueue.Dispose();
            dispatchResult.Return(true);

            // Assert

            // should not have thrown on the return, that is all
        }

        [Test]
        public void Dispose_is_idempotent() {

            // Arrange
            var queuePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var dispatchQueue = new PersistentPubSubDispatchQueue(queuePath, TaskTimerFactory.Current, 1.Minutes(), i => new Result<bool>().WithReturn(true));

            // Act
            dispatchQueue.Dispose();

            // Assert
            dispatchQueue.Dispose();
        }

        [Test]
        public void DisposeAndClear_is_idempotent() {

            // Arrange
            var queuePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var dispatchQueue = new PersistentPubSubDispatchQueue(queuePath, TaskTimerFactory.Current, 1.Minutes(), i => new Result<bool>().WithReturn(true));

            // Act
            dispatchQueue.DeleteAndDispose();

            // Assert
            dispatchQueue.DeleteAndDispose();
        }
    }
}
