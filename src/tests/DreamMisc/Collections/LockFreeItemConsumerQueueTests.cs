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
using System.Threading;
using log4net;
using MindTouch.Collections;
using MindTouch.Threading;
using NUnit.Framework;

namespace MindTouch.Dream.Test.Collections {

    [TestFixture]
    public class LockFreeItemConsumerQueueTests {

        //--- Class Fields ---
        private static ILog _log = LogUtils.CreateLog();

        //--- Methods ---

        #region --- Item Tests ---

        [Test]
        public void New_ItemCount() {
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);
        }

        [Test]
        public void New_TryDequeue_ItemCount() {
            int value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);
        }

        [Test]
        public void New_TryDequeue_TryDequeue_ItemCount() {
            int value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);
        }

        [Test]
        public void New_TryEnqueue_ItemCount() {
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryEnqueue(42);
            Assert.AreEqual(1, q.ItemCount);
            Assert.IsFalse(q.ItemIsEmpty);
        }

        [Test]
        public void New_TryEnqueue_TryDequeue_ItemCount() {
            int value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryEnqueue(42);
            Assert.AreEqual(1, q.ItemCount);
            Assert.IsFalse(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(42, value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);
        }

        [Test]
        public void New_TryDequeue_TryEnqueue_ItemCount() {
            int value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryEnqueue(42);
            Assert.AreEqual(1, q.ItemCount);
            Assert.IsFalse(q.ItemIsEmpty);
        }

        [Test]
        public void New_TryDequeue_TryEnqueue_TryDequeue_ItemCount() {
            int value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryEnqueue(42);
            Assert.AreEqual(1, q.ItemCount);
            Assert.IsFalse(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(42, value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);
        }

        [Test]
        public void New_TryDequeue_TryEnqueue_TryDequeue_TryDequeue_ItemCount() {
            int value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryEnqueue(42);
            Assert.AreEqual(1, q.ItemCount);
            Assert.IsFalse(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(42, value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);
        }

        [Test]
        public void New_TryEnqueue_x50_ItemCount() {
            int value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            for(int i = 0; i < 50; ++i) {
                q.TryEnqueue(100 - i);
            }
            Assert.AreEqual(50, q.ItemCount);
            Assert.IsFalse(q.ItemIsEmpty);

            for(int i = 0; i < 50; ++i) {
                q.TryDequeue(out value);
                Assert.AreEqual(100 - i, value);
            }
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);
        }

        [Test]
        public void New_TryEnqueue_x50_TryDequeue_x50_ItemCount() {
            int value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            for(int i = 0; i < 50; ++i) {
                q.TryEnqueue(100 - i);
            }
            Assert.AreEqual(50, q.ItemCount);
            Assert.IsFalse(q.ItemIsEmpty);

            for(int i = 0; i < 50; ++i) {
                q.TryDequeue(out value);
                Assert.AreEqual(100 - i, value);
            }
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);
        }

        [Test]
        public void New_TryEnqueue_x50_TryDequeue_x50_TryDequeue_ItemCount() {
            int value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            for(int i = 0; i < 50; ++i) {
                q.TryEnqueue(100 - i);
            }
            Assert.AreEqual(50, q.ItemCount);
            Assert.IsFalse(q.ItemIsEmpty);

            for(int i = 0; i < 50; ++i) {
                q.TryDequeue(out value);
                Assert.AreEqual(100 - i, value);
            }
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);
        }

        [Test]
        public void New_TryDequeue_TryEnqueue_x50_TryDequeue_x50_TryDequeue_ItemCount() {
            int value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            for(int i = 0; i < 50; ++i) {
                q.TryEnqueue(100 - i);
            }
            Assert.AreEqual(50, q.ItemCount);
            Assert.IsFalse(q.ItemIsEmpty);

            for(int i = 0; i < 50; ++i) {
                q.TryDequeue(out value);
                Assert.AreEqual(100 - i, value);
            }
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);
        }
        #endregion

        #region --- Consumer Tests ---

        [Test]
        public void New_ConsumerCount() {
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ConsumerCount);
        }

        [Test]
        public void New_TryDequeue_ConsumerCount() {
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ConsumerCount);

            int value;
            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ConsumerCount);
        }

        [Test]
        public void New_TryDequeue_TryDequeue_ConsumerCount() {
            Action<int> value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ConsumerCount);
        }

        [Test]
        public void New_TryEnqueue_ConsumerCount() {
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryEnqueue(x => { });
            Assert.AreEqual(1, q.ConsumerCount);
        }

        [Test]
        public void New_TryEnqueue_TryDequeue_ConsumerCount() {
            Action<int> value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryEnqueue(x => { });
            Assert.AreEqual(1, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.IsAssignableFrom(typeof(Action<int>), value);
            Assert.AreEqual(0, q.ConsumerCount);
        }

        [Test]
        public void New_TryDequeue_TryEnqueue_ConsumerCount() {
            Action<int> value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryEnqueue(x => { });
            Assert.AreEqual(1, q.ConsumerCount);
        }

        [Test]
        public void New_TryDequeue_TryEnqueue_TryDequeue_ConsumerCount() {
            Action<int> value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryEnqueue(x => { });
            Assert.AreEqual(1, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.IsAssignableFrom(typeof(Action<int>), value);
            Assert.AreEqual(0, q.ConsumerCount);
        }

        [Test]
        public void New_TryDequeue_TryEnqueue_TryDequeue_TryDequeue_ConsumerCount() {
            Action<int> value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryEnqueue(x => { });
            Assert.AreEqual(1, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.IsAssignableFrom(typeof(Action<int>), value);
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ConsumerCount);
        }

        [Test]
        public void New_TryEnqueue_x50_ConsumerCount() {
            Action<int> value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ConsumerCount);

            for(int i = 0; i < 50; ++i) {
                q.TryEnqueue(x => { });
            }
            Assert.AreEqual(50, q.ConsumerCount);

            for(int i = 0; i < 50; ++i) {
                q.TryDequeue(out value);
                Assert.IsAssignableFrom(typeof(Action<int>), value);
            }
            Assert.AreEqual(0, q.ConsumerCount);
        }

        [Test]
        public void New_TryEnqueue_x50_TryDequeue_x50_ConsumerCount() {
            Action<int> value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ConsumerCount);

            for(int i = 0; i < 50; ++i) {
                q.TryEnqueue(x => { });
            }
            Assert.AreEqual(50, q.ConsumerCount);

            for(int i = 0; i < 50; ++i) {
                q.TryDequeue(out value);
                Assert.IsAssignableFrom(typeof(Action<int>), value);
            }
            Assert.AreEqual(0, q.ConsumerCount);
        }

        [Test]
        public void New_TryEnqueue_x50_TryDequeue_x50_TryDequeue_ConsumerCount() {
            Action<int> value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ConsumerCount);

            for(int i = 0; i < 50; ++i) {
                q.TryEnqueue(x => { });
            }
            Assert.AreEqual(50, q.ConsumerCount);

            for(int i = 0; i < 50; ++i) {
                q.TryDequeue(out value);
                Assert.IsAssignableFrom(typeof(Action<int>), value);
            }
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ConsumerCount);
        }

        [Test]
        public void New_TryDequeue_TryEnqueue_x50_TryDequeue_x50_TryDequeue_ConsumerCount() {
            Action<int> value;
            var q = new LockFreeItemConsumerQueue<int>();
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ConsumerCount);

            for(int i = 0; i < 50; ++i) {
                q.TryEnqueue(x => { });
            }
            Assert.AreEqual(50, q.ConsumerCount);

            for(int i = 0; i < 50; ++i) {
                q.TryDequeue(out value);
                Assert.IsAssignableFrom(typeof(Action<int>), value);
            }
            Assert.AreEqual(0, q.ConsumerCount);

            q.TryDequeue(out value);
            Assert.AreEqual(0, q.ConsumerCount);
        }
        #endregion
    
        [Test]
        public void New_TryEnqueueItem_x50_TryEnqueueConsumer_x50_ItemCount_ConsumerCount() {
            var q = new LockFreeItemConsumerQueue<int>();
            var etp = new ElasticThreadPool(10, 10);

            // submit enqueue & dequeue work-items
            const int max = 10000;
            int count = max + 1;
            var e = new ManualResetEvent(false);
            int[] checks = new int[max];
            for(int i = 0; i < max; ++i) {
                int j = i;
                etp.TryQueueWorkItem(() => {
                    int k = Interlocked.Increment(ref checks[j]);
                    Assert.AreEqual(1, k, "value for {0} was already increased", j);
                    q.TryEnqueue(j);
                });
                etp.TryQueueWorkItem(() => {
                    q.TryEnqueue(x => {
                        int k = Interlocked.Decrement(ref checks[x]);
                        Assert.AreEqual(0, k, "value for {0} was already decreased", x);
// ReSharper disable AccessToModifiedClosure
                        if(Interlocked.Decrement(ref count) == 0) {
// ReSharper restore AccessToModifiedClosure
                            e.Set();
                        }
                    });
                });
            }
            if(Interlocked.Decrement(ref count) == 0) {
                e.Set();
            }
            if(!e.WaitOne(TimeSpan.FromSeconds(10))) {
                Assert.Fail("test timed out");
            }
            for(int i = 0; i < max; ++i) {
                Assert.AreEqual(0, checks[i], "entry {0}", i);
            }
            Assert.AreEqual(0, q.ItemCount);
            Assert.IsTrue(q.ItemIsEmpty);
            Assert.AreEqual(0, q.ConsumerCount);
            Assert.IsTrue(q.ConsumerIsEmpty);
        }
    }
}
