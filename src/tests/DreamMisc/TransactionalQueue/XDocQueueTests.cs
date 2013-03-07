/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2013 MindTouch, Inc.
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using log4net;
using MindTouch.Collections;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test.TransactionalQueue {

    [TestFixture]
    public class XDocQueueTests {

        private static readonly ILog _log = LogUtils.CreateLog();

        [Test]
        public void End_to_end_test() {
            var file = Path.GetTempFileName();
            try {
                var queueStream = new SingleFileQueueStream(file);
                var serializer = new XDocQueueItemSerializer();
                using(var queue = new TransactionalQueue<XDoc>(queueStream, serializer)) {
                    var doc = new XDoc("foo");
                    queue.Enqueue(doc);
                    var v = queue.Dequeue();
                    Assert.AreEqual(doc, v.Value);
                    Assert.IsTrue(queue.CommitDequeue(v.Id));
                }
            } finally {
                File.Delete(file);
            }
        }

        public class QHandle {
            private readonly Func<TransactionalQueue<XDoc>> _ctor;
            private readonly object syncroot = new object();
            private TransactionalQueue<XDoc> _q;
            public void Refresh() {
                lock(syncroot) {
                    _q.Dispose();
                    Thread.Sleep(100);
                    _q = _ctor();
                }
            }
            public QHandle(Func<TransactionalQueue<XDoc>> ctor) {
                _ctor = ctor;
                _q = ctor();
            }

            public void Execute(Action<TransactionalQueue<XDoc>> action) {
                TransactionalQueue<XDoc> q;
                for(int i = 0; i < 4; i++) {
                    lock(syncroot) {
                        q = _q;
                    }
                    try {
                        action(q);
                        return;
                    } catch(ObjectDisposedException) { }
                    Thread.Sleep(100);
                }
                lock(syncroot) {
                    q = _q;
                }
                action(q);
            }

            public void Dispose() {
                _q.Dispose();
            }
        }

        [Ignore("slow load test")]
        [Test]
        public void Load_test_mixed_put_peek_take_with_single_file_queue() {
            var file = Path.GetTempFileName();
            var serializer = new XDocQueueItemSerializer();
            Func<TransactionalQueue<XDoc>> ctor = () => {
                var queueStream = new SingleFileQueueStream(file);
                var q = new TransactionalQueue<XDoc>(queueStream, serializer) { DefaultCommitTimeout = TimeSpan.FromSeconds(5) };
                return q;
            };
            try {
                Load_test(ctor);
            } finally {
                File.Delete(file);
            }
        }

        [Ignore("slow load test")]
        [Test]
        public void Load_test_mixed_put_peek_take_with_multi_file_queue() {
            var path = Path.Combine(Path.GetTempPath(), StringUtil.CreateAlphaNumericKey(6));
            var serializer = new XDocQueueItemSerializer();
            Func<TransactionalQueue<XDoc>> ctor = () => {
                var queueStream = new MultiFileQueueStream(path, 500 * 1024);
                var q = new TransactionalQueue<XDoc>(queueStream, serializer) { DefaultCommitTimeout = TimeSpan.FromSeconds(5) };
                return q;
            };
            try {
                Load_test(ctor);
            } finally {
                Directory.Delete(path, true);
            }
        }

        [Ignore("slow perf test")]
        [Test]
        public void Perf_test_single_thread_put_peek_and_take_with_single_file_queue() {
            var file = Path.GetTempFileName();
            try {
                var queueStream = new SingleFileQueueStream(file);
                var serializer = new XDocQueueItemSerializer();
                using(ITransactionalQueue<XDoc> queue = new TransactionalQueue<XDoc>(queueStream, serializer)) {
                    Perf_test_single_thread_put_peek_and_take(queue);
                }
            } finally {
                _log.DebugFormat("wiping xdoc queue");
                File.Delete(file);
            }
        }

        [Ignore("slow perf test")]
        [Test]
        public void Perf_test_single_thread_put_peek_and_take_with_memorystream_queue() {
            using(var stream = new MemoryStream()) {
                var queueStream = new SingleFileQueueStream(stream);
                var serializer = new XDocQueueItemSerializer();
                using(ITransactionalQueue<XDoc> queue = new TransactionalQueue<XDoc>(queueStream, serializer)) {
                    Perf_test_single_thread_put_peek_and_take(queue);
                }
            }
        }

        [Ignore("slow perf test")]
        [Test]
        public void Perf_test_single_thread_put_peek_and_take_with_multi_file_queue() {
            var path = Path.Combine(Path.GetTempPath(), StringUtil.CreateAlphaNumericKey(6));
            try {
                var serializer = new XDocQueueItemSerializer();
                var queueStream = new MultiFileQueueStream(path, 500 * 1024);
                using(ITransactionalQueue<XDoc> queue = new TransactionalQueue<XDoc>(queueStream, serializer) { DefaultCommitTimeout = TimeSpan.FromSeconds(5) }) {
                    Perf_test_single_thread_put_peek_and_take(queue);
                }
            } finally {
                Directory.Delete(path, true);
            }
        }

        [Ignore("slow perf test")]
        [Test]
        public void Perf_test_multi_thread_put_peek_and_take_with_single_file_queue() {
            var file = Path.GetTempFileName();
            try {
                var queueStream = new SingleFileQueueStream(file);
                var serializer = new XDocQueueItemSerializer();
                using(ITransactionalQueue<XDoc> queue = new TransactionalQueue<XDoc>(queueStream, serializer)) {
                    Perf_test_single_thread_put_peek_and_take(queue);
                }
            } finally {
                _log.DebugFormat("wiping xdoc queue");
                File.Delete(file);
            }
        }

        [Ignore("slow perf test")]
        [Test]
        public void Perf_test_multi_thread_put_peek_and_take_with_memorystream_queue() {
            using(var stream = new MemoryStream()) {
                var queueStream = new SingleFileQueueStream(stream);
                var serializer = new XDocQueueItemSerializer();
                using(ITransactionalQueue<XDoc> queue = new TransactionalQueue<XDoc>(queueStream, serializer)) {
                    Perf_test_multi_thread_put_peek_and_take(queue);
                }
            }
        }

        [Ignore("slow perf test")]
        [Test]
        public void Perf_test_multi_thread_put_peek_and_take_with_multi_file_queue() {
            var path = Path.Combine(Path.GetTempPath(), StringUtil.CreateAlphaNumericKey(6));
            try {
                var serializer = new XDocQueueItemSerializer();
                var queueStream = new MultiFileQueueStream(path, 500 * 1024);
                using(ITransactionalQueue<XDoc> queue = new TransactionalQueue<XDoc>(queueStream, serializer) { DefaultCommitTimeout = TimeSpan.FromSeconds(5) }) {
                    Perf_test_multi_thread_put_peek_and_take(queue);
                }
            } finally {
                Directory.Delete(path, true);
            }
        }

        private void Load_test(Func<TransactionalQueue<XDoc>> ctor) {
            var rand = new Random();
            var queue = new QHandle(ctor);
            try {
                var pending = new Queue<ITransactionalQueueEntry<XDoc>>();
                var n = 10000;
                var w = 10;
                var start = new int[n * w];
                var end = new HashSet<int>();
                var expired = 0;
                var dropped = 0;
                var enqueued = 0;
                bool done = false;
                var trigger = new ManualResetEvent(false);
                var results = new List<Result>();
                int v = 0;
                for(var i = 0; i < w; i++) {
                    var values = new List<int>();
                    for(var j = 0; j < n; j++) {
                        values.Add(v);
                        start[v] = v;
                        v++;
                    }
                    results.Add(AsyncUtil.ForkThread(() => {
                        trigger.WaitOne();
                        foreach(var vx in values) {

                            queue.Execute(q => q.Enqueue(new XDoc("doc").Attr("id", vx).Elem("rand", StringUtil.CreateAlphaNumericKey(rand.Next(2, 8)))));
                            Interlocked.Increment(ref enqueued);
                            Thread.Sleep(rand.Next(0, 1));
                        }
                    }, new Result()));
                }
                for(var i = 0; i < 10; i++) {
                    results.Add(AsyncUtil.ForkThread(() => {
                        trigger.WaitOne();
                        while(!done) {
                            ITransactionalQueueEntry<XDoc> vx = null;
                            queue.Execute(q => vx = q.Dequeue());
                            if(vx == null) {
                                Thread.Sleep(1000);
                                continue;
                            }
                            lock(pending) {
                                pending.Enqueue(vx);
                            }
                        }
                    }, new Result()));
                }
                for(var i = 0; i < 5; i++) {
                    results.Add(AsyncUtil.ForkThread(() => {
                        trigger.WaitOne();
                        while(!done) {
                            ITransactionalQueueEntry<XDoc> vx = null;
                            lock(pending) {
                                if(pending.Count == 0) {
                                    Thread.Sleep(1);
                                    continue;
                                }
                                vx = pending.Dequeue();
                            }
                            var success = false;
                            queue.Execute(q => success = q.CommitDequeue(vx.Id));
                            if(!success) {
                                Interlocked.Increment(ref expired);
                                continue;
                            }
                            lock(end) {
                                end.Add(vx.Value["@id"].AsInt ?? -1);
                            }
                        }
                    }, new Result()));
                }
                results.Add(AsyncUtil.ForkThread(() => {
                    trigger.WaitOne();
                    while(!done) {
                        ITransactionalQueueEntry<XDoc> vx = null;
                        lock(pending) {
                            if(pending.Count == 0) {
                                Thread.Sleep(1000);
                                continue;
                            }
                            vx = pending.Dequeue();
                        }
                        queue.Execute(q => q.RollbackDequeue(vx.Id));
                        Interlocked.Increment(ref dropped);
                        Thread.Sleep(500);
                    }
                }, new Result()));
                trigger.Set();
                int waitCounter = 0;
                while(end.Count != start.Length) {
                    var qcount = 0;
                    queue.Execute(q => qcount = q.Count);
                    Console.WriteLine("enqueued: {0}, queue size: {1}, output size: {2}, dropped items: {3}, expired items: {4}",
                                      enqueued,
                                      qcount,
                                      end.Count,
                                      dropped,
                                      expired);
                    foreach(var r in results) {
                        if(r.HasException) {
                            r.Wait();
                        }
                    }

                    Thread.Sleep(1000);
                    waitCounter++;
                    if(waitCounter > 5) {
                        waitCounter = 0;
                        Console.WriteLine("refreshing queue");
                        queue.Refresh();
                    }
                }
                done = true;
                foreach(var r in results) {
                    r.Wait();
                }
                Assert.AreEqual(start, (from x in end orderby x select x).ToArray());
                Console.WriteLine("total queue items expired: {0}", expired);
            } finally {
                queue.Dispose();
            }
        }

        private void Perf_test_single_thread_put_peek_and_take(ITransactionalQueue<XDoc> queue) {
            long totalEnqueue = 0;
            long totalDequeue = 0;
            var n = 20000;
            var m = 4;

            for(var k = 0; k < m; k++) {
                var items = new List<XDoc>();
                for(var i = 0; i < n; i++) {
                    items.Add(new XDoc("test")
                                  .Attr("id", i)
                                  .Elem("foo", "bar")
                                  .Start("baz")
                                  .Attr("meta", "true")
                                  .Value("dsfdsssssssssssssssssssssssssssssfdfsfsfsfsfsfsfd")
                                  .End()
                                  .Start("id", StringUtil.CreateAlphaNumericKey(16)));
                }
                var stopwatch = Stopwatch.StartNew();
                foreach(var itm in items) {
                    queue.Enqueue(itm);
                }
                Assert.AreEqual(n, queue.Count);
                stopwatch.Stop();
                totalEnqueue += stopwatch.ElapsedMilliseconds;
                var j = 0;
                stopwatch = Stopwatch.StartNew();
                var v = queue.Dequeue();
                while(v != null) {
                    Assert.IsTrue(queue.CommitDequeue(v.Id));
                    j++;
                    v = queue.Dequeue();
                }
                stopwatch.Stop();
                totalDequeue += stopwatch.ElapsedMilliseconds;
                Assert.AreEqual(n, j);
                Assert.AreEqual(0, queue.Count);
            }
            Console.WriteLine("Enqueue: {0:0,000}/s", n * m * 1000 / totalEnqueue);
            Console.WriteLine("Dequeue: {0:0,000}/s", n * m * 1000 / totalDequeue);
        }

        public void Perf_test_multi_thread_put_peek_and_take(ITransactionalQueue<XDoc> queue) {
            long totalEnqueue = 0;
            long totalDequeue = 0;
            var w = 5;
            var n = 5000;
            var m = 4;

            for(var k = 0; k < m; k++) {
                var enqueues = new List<Result>();
                var trigger = new ManualResetEvent(false);
                for(var i = 0; i < w; i++) {
                    enqueues.Add(AsyncUtil.ForkThread(() => Enqueue(queue, n, trigger), new Result()));
                }
                var stopwatch = Stopwatch.StartNew();
                trigger.Set();
                foreach(var r in enqueues) {
                    r.Wait();
                }
                stopwatch.Stop();
                Assert.AreEqual(n * w, queue.Count);
                totalEnqueue += stopwatch.ElapsedMilliseconds;
                var j = 0;
                var dequeues = new List<Result<int>>();
                trigger.Reset();
                for(var i = 0; i < w; i++) {
                    dequeues.Add(AsyncUtil.ForkThread(() => Dequeue(queue, trigger), new Result<int>()));
                }
                stopwatch = Stopwatch.StartNew();
                trigger.Set();
                foreach(var r in dequeues) {
                    j += r.Wait();
                }
                stopwatch.Stop();
                totalDequeue += stopwatch.ElapsedMilliseconds;
                Assert.AreEqual(0, queue.Count);
                Assert.AreEqual(n * w, j);
            }
            Console.WriteLine("Enqueue: {0:0,000}/s", n * m * 1000 / totalEnqueue);
            Console.WriteLine("Dequeue: {0:0,000}/s", n * m * 1000 / totalDequeue);
        }

        private int Dequeue(ITransactionalQueue<XDoc> queue, ManualResetEvent trigger) {
            trigger.WaitOne();
            var j = 0;
            ITransactionalQueueEntry<XDoc> value = queue.Dequeue();
            while(value != null) {
                bool success = queue.CommitDequeue(value.Id);
                if(!success) {
                    throw new InvalidOperationException();
                }
                j++;
                value = queue.Dequeue();
            }
            return j;
        }

        private void Enqueue(ITransactionalQueue<XDoc> queue, int n, ManualResetEvent trigger) {
            trigger.WaitOne();
            for(var j = 0; j < n; j++) {
                queue.Enqueue(new XDoc("test")
                    .Attr("id", j)
                    .Elem("foo", "bar")
                    .Start("baz")
                        .Attr("meta", "true")
                        .Value("dsfdsssssssssssssssssssssssssssssfdfsfsfsfsfsfsfd")
                    .End()
                    .Elem("id", StringUtil.CreateAlphaNumericKey(16)));
            }
        }
    }
}
