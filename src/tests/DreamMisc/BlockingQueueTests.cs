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
using System.Threading;
using MindTouch.Collections;
using NUnit.Framework;
using System.Linq;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class BlockingQueueTests {

        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        [Test]
        public void Single_threaded_queue_dequeue() {
            int n = 10000;
            List<string> guids = new List<string>();
            BlockingQueue<string> q = new BlockingQueue<string>();
            for(int i = 0; i < n; i++) {
                string guid = Guid.NewGuid().ToString();
                q.Enqueue(guid);
                guids.Add(guid);
            }
            Assert.AreEqual(n, q.Count);
            for(int i = 0; i < n; i++) {
                string guid = q.Dequeue();
                Assert.AreEqual(guids[i], guid);
            }
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Queue_on_closed_queue_throws() {
            BlockingQueue<string> q = new BlockingQueue<string>();
            q.Enqueue("foo");
            Assert.IsFalse(q.IsClosed);
            q.Close();
            Assert.IsTrue(q.IsClosed);
            q.Enqueue("bar");
        }

        [Test]
        [ExpectedException(typeof(QueueClosedException))]
        public void Dequeue_on_closed_queue_throws() {
            BlockingQueue<string> q = new BlockingQueue<string>();
            q.Enqueue("foo");
            Assert.IsFalse(q.IsClosed);
            q.Close();
            Assert.IsTrue(q.IsClosed);
            string x = q.Dequeue();
            Assert.AreEqual("foo", x);
            x = q.Dequeue();
        }

        [Test]
        public void Dequeue_times_out_as_specified() {
            BlockingQueue<string> q = new BlockingQueue<string>();
            DateTime start = DateTime.Now;
            string x;
            Assert.IsFalse(q.TryDequeue(TimeSpan.FromSeconds(1), out x));
            Assert.IsNull(x);
            TimeSpan elapsed = DateTime.Now.Subtract(start);
            Assert.GreaterOrEqual(elapsed.TotalSeconds, 0.95);
            Assert.LessOrEqual(elapsed.TotalSeconds, 1.1d);
        }

        [Test]
        public void One_producer_one_consumer_loop_manually() {
            var n = 10000;
            var enqueued = new List<string>();
            var dequeued = new List<string>();
            var q = new BlockingQueue<string>();
            var consumer = new Thread(SingleConsumerManualLoop);
            consumer.IsBackground = true;
            var reset = new ManualResetEvent(false);
            consumer.Start(new Tuplet<int, IBlockingQueue<string>, List<string>, ManualResetEvent>(n, q, dequeued, reset));
            for(var i = 0; i < n; i++) {
                string guid = Guid.NewGuid().ToString();
                q.Enqueue(guid);
                enqueued.Add(guid);
            }
            Assert.IsTrue(reset.WaitOne(1000, true));
            Assert.AreEqual(n, enqueued.Count);
            Assert.AreEqual(n, dequeued.Count);
            for(var i = 0; i < n; i++) {
                Assert.AreEqual(enqueued[i], dequeued[i]);
            }
        }

        private void SingleConsumerManualLoop(object obj) {
            var state = (Tuplet<int, IBlockingQueue<string>, List<string>, ManualResetEvent>)obj;
            for(int i = 0; i < state.Item1; i++) {
                string guid = state.Item2.Dequeue();
                if( guid == null) {
                    _log.WarnMethodCall("guid is null");
                }
                Assert.IsNotNull(guid);
                state.Item3.Add(guid);
            }
            state.Item4.Set();
        }

        [Test]
        public void One_producer_one_consumer_loop_with_foreach() {
            var n = 10000;
            var enqueued = new List<string>();
            var dequeued = new List<string>();
            var q = new BlockingQueue<string>();
            var consumer = new Thread(SingleConsumerForeachLoop);
            consumer.IsBackground = true;
            var reset = new ManualResetEvent(false);
            consumer.Start(new Tuplet<int, IBlockingQueue<string>, List<string>, ManualResetEvent>(n, q, dequeued, reset));
            for(int i = 0; i < n; i++) {
                string guid = Guid.NewGuid().ToString();
                q.Enqueue(guid);
                enqueued.Add(guid);
            }
            Assert.IsTrue(reset.WaitOne(1000, true));
            Assert.AreEqual(n, enqueued.Count);
            Assert.AreEqual(n, dequeued.Count);
            for(int i = 0; i < n; i++) {
                Assert.AreEqual(enqueued[i], dequeued[i]);
            }
        }

        private void SingleConsumerForeachLoop(object obj) {
            var state = (Tuplet<int, IBlockingQueue<string>, List<string>, ManualResetEvent>)obj;
            int n = 0;
            foreach(string guid in state.Item2) {
                state.Item3.Add(guid);
                if(guid == null) {
                    _log.WarnMethodCall("guid is null");
                }
                n++;
                if(n >= state.Item1) {
                    break;
                }
            }
            state.Item4.Set();
        }

        [Test]
        public void One_producer_one_consumer_loop_with_foreach_and_stop() {
            int n = 10000;
            List<string> enqueued = new List<string>();
            List<string> dequeued = new List<string>();
            BlockingQueue<string> q = new BlockingQueue<string>();
            Thread consumer = new Thread(SingleConsumerForeachLoopAndStop);
            consumer.Start(new Tuplet<IBlockingQueue<string>, List<string>>(q, dequeued));
            for(int i = 0; i < n; i++) {
                string guid = Guid.NewGuid().ToString();
                q.Enqueue(guid);
                enqueued.Add(guid);
            }
            q.Close();
            Assert.IsTrue(consumer.Join(1000));
            Assert.AreEqual(n, enqueued.Count);
            Assert.AreEqual(n, dequeued.Count);
            for(int i = 0; i < n; i++) {
                Assert.AreEqual(enqueued[i], dequeued[i]);
            }
        }

        private void SingleConsumerForeachLoopAndStop(object obj) {
            Tuplet<IBlockingQueue<string>, List<string>> state = (Tuplet<IBlockingQueue<string>, List<string>>)obj;
            foreach(string guid in state.Item1) {
                state.Item2.Add(guid);
            }
        }

        [Test]
        public void Many_consumers_with_timeouts() {
            BlockingQueue<string> q = new BlockingQueue<string>();
            Thread c1 = new Thread(MultiConsumer);
            Thread c2 = new Thread(MultiConsumer);
            Thread c3 = new Thread(MultiConsumer);
            c1.IsBackground = true;
            c2.IsBackground = true;
            c3.IsBackground = true;
            Tuplet<BlockingQueue<string>, string, TimeSpan, ManualResetEvent> v1
                = new Tuplet<BlockingQueue<string>, string, TimeSpan, ManualResetEvent>(q, "x", TimeSpan.FromSeconds(1), new ManualResetEvent(false));
            c1.Start(v1);
            Tuplet<BlockingQueue<string>, string, TimeSpan, ManualResetEvent> v2
                = new Tuplet<BlockingQueue<string>, string, TimeSpan, ManualResetEvent>(q, "x", TimeSpan.FromSeconds(1), new ManualResetEvent(false));
            c2.Start(v2);
            Tuplet<BlockingQueue<string>, string, TimeSpan, ManualResetEvent> v3
                = new Tuplet<BlockingQueue<string>, string, TimeSpan, ManualResetEvent>(q, "x", TimeSpan.FromSeconds(1), new ManualResetEvent(false));
            c3.Start(v3);
            q.Enqueue("foo");
            Assert.IsTrue(v1.Item4.WaitOne(2000, false), "thread 1 did not finish");
            Assert.IsTrue(v2.Item4.WaitOne(2000, false), "thread 2 did not finish");
            Assert.IsTrue(v3.Item4.WaitOne(2000, false), "thread 3 did not finish");
            bool gotValue = false;
            foreach(Tuplet<BlockingQueue<string>, string, TimeSpan, ManualResetEvent> v in new Tuplet<BlockingQueue<string>, string, TimeSpan, ManualResetEvent>[] { v1, v2, v3 }) {
                if(v.Item2 == "foo") {
                    gotValue = true;
                    Assert.Less(v.Item3.TotalSeconds, 1);
                } else {
                    Assert.IsNull(v.Item2);
                    Assert.GreaterOrEqual(v.Item3.TotalSeconds, 0.95);
                }
            }
            Assert.IsTrue(gotValue);
        }

        private void MultiConsumer(object state) {
            Tuplet<BlockingQueue<string>, string, TimeSpan, ManualResetEvent> v = (Tuplet<BlockingQueue<string>, string, TimeSpan, ManualResetEvent>)state;
            DateTime start = DateTime.Now;
            v.Item1.TryDequeue(v.Item3, out v.Item2);
            v.Item3 = DateTime.Now.Subtract(start);
            v.Item4.Set();
        }

        [Test]
        public void One_producer_many_consumers_loop_with_foreach() {
            int n = 500;
            var enqueued = new List<string>();
            var dequeued = new List<string>();
            var q = new BlockingQueue<string>();
            var c1 = new Thread(MultiConsumerForeachLoop) { IsBackground = true };
            var c2 = new Thread(MultiConsumerForeachLoop) { IsBackground = true };
            var c3 = new Thread(MultiConsumerForeachLoop) { IsBackground = true };
            var v1 = new Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent>(q, dequeued, 0, new ManualResetEvent(false));
            c1.Start(v1);
            var v2 = new Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent>(q, dequeued, 0, new ManualResetEvent(false));
            c2.Start(v2);
            var v3 = new Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent>(q, dequeued, 0, new ManualResetEvent(false));
            c3.Start(v3);
            Thread.Sleep(1000);
            for(int i = 0; i < n; i++) {
                string guid = Guid.NewGuid().ToString();
                q.Enqueue(guid);
                enqueued.Add(guid);
            }
            q.Close();
            Assert.IsTrue(v1.Item4.WaitOne(10000, false), "thread 1 did not finish");
            Assert.IsTrue(v2.Item4.WaitOne(10000, false), "thread 2 did not finish");
            Assert.IsTrue(v3.Item4.WaitOne(10000, false), "thread 3 did not finish");
            _log.DebugFormat("Thread 1 processed {0}", v1.Item3);
            _log.DebugFormat("Thread 2 processed {0}", v2.Item3);
            _log.DebugFormat("Thread 3 processed {0}", v3.Item3);
            Console.WriteLine("Thread 1 processed {0}", v1.Item3);
            Console.WriteLine("Thread 2 processed {0}", v2.Item3);
            Console.WriteLine("Thread 3 processed {0}", v3.Item3);
            Assert.GreaterOrEqual(v1.Item3, n / 4);
            Assert.GreaterOrEqual(v2.Item3, n / 4);
            Assert.GreaterOrEqual(v3.Item3, n / 4);
            Assert.AreEqual(n, dequeued.Count);
            Assert.AreEqual(dequeued.OrderBy(x => x).ToArray(), enqueued.OrderBy(x => x).ToArray());
        }

        [Test]
        public void Many_producers_many_consumers_loop_with_foreach() {
            int n = 200;
            List<string> enqueued = new List<string>();
            List<string> dequeued = new List<string>();
            BlockingQueue<string> q = new BlockingQueue<string>();
            Thread c1 = new Thread(MultiConsumerForeachLoop);
            Thread c2 = new Thread(MultiConsumerForeachLoop);
            Thread c3 = new Thread(MultiConsumerForeachLoop);
            c1.IsBackground = true;
            c2.IsBackground = true;
            c3.IsBackground = true;
            Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent> v1
                = new Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent>(q, dequeued, 0, new ManualResetEvent(false));
            c1.Start(v1);
            Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent> v2
                = new Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent>(q, dequeued, 0, new ManualResetEvent(false));
            c2.Start(v2);
            Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent> v3
                = new Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent>(q, dequeued, 0, new ManualResetEvent(false));
            c3.Start(v3);
            Thread p1 = new Thread(MultiProducer);
            Thread p2 = new Thread(MultiProducer);
            Thread p3 = new Thread(MultiProducer);
            p1.IsBackground = true;
            p2.IsBackground = true;
            p3.IsBackground = true;
            Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent> p1v
                = new Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent>(q, enqueued, n, new ManualResetEvent(false));
            p1.Start(p1v);
            Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent> p2v
                = new Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent>(q, enqueued, n, new ManualResetEvent(false));
            p2.Start(p2v);
            Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent> p3v
                = new Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent>(q, enqueued, n, new ManualResetEvent(false));
            p3.Start(p3v);
            Assert.IsTrue(p1v.Item4.WaitOne(5000, false), "producer 1 did not finish");
            Assert.IsTrue(p2v.Item4.WaitOne(5000, false), "producer 2 did not finish");
            Assert.IsTrue(p3v.Item4.WaitOne(5000, false), "producer 3 did not finish");
            q.Close();
            Assert.IsTrue(v1.Item4.WaitOne(15000, false), "consumer 1 did not finish");
            Assert.IsTrue(v2.Item4.WaitOne(15000, false), "consumer 2 did not finish");
            Assert.IsTrue(v3.Item4.WaitOne(15000, false), "consumer 3 did not finish");
            _log.DebugFormat("consumer 1 processed {0}", v1.Item3);
            _log.DebugFormat("consumer 2 processed {0}", v2.Item3);
            _log.DebugFormat("consumer 3 processed {0}", v3.Item3);
            Assert.GreaterOrEqual(v1.Item3, n * 3 / 4);
            Assert.GreaterOrEqual(v2.Item3, n * 3 / 4);
            Assert.GreaterOrEqual(v3.Item3, n * 3 / 4);
            Assert.AreEqual(enqueued.Count, dequeued.Count);
            for(int i = 0; i < n; i++) {
                Assert.Contains(dequeued[i], enqueued);
            }
        }

        private void MultiProducer(object obj) {
            var state = (Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent>)obj;
            List<string> enqueued = new List<string>();
            for(int i = 0; i < state.Item3; i++) {
                string guid = Guid.NewGuid().ToString();
                state.Item1.Enqueue(guid);
                enqueued.Add(guid);
            }
            _log.DebugFormat("production complete");
            lock(state.Item2) {
                state.Item2.AddRange(enqueued);
            }
            state.Item4.Set();
        }

        private void MultiConsumerForeachLoop(object obj) {
            var state = (Tuplet<BlockingQueue<string>, List<string>, int, ManualResetEvent>)obj;
            _log.DebugFormat("consumption started");
            var dequeued = new List<string>();
            foreach(string guid in state.Item1) {
                dequeued.Add(guid);
                state.Item3++;
                Thread.Sleep(10);
            }
            _log.DebugFormat("consumption complete");
            lock(state.Item2) {
                state.Item2.AddRange(dequeued);
            }
            state.Item4.Set();
        }
    }
}
