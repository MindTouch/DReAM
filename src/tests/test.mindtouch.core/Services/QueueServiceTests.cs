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
using System.Diagnostics;
using System.IO;
using System.Threading;
using log4net;
using MindTouch.Dream;
using MindTouch.Dream.Services;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Core.Test.Services {
    [TestFixture]
    public class QueueServiceTests {

        private static readonly ILog _log = LogUtils.CreateLog();

        private DreamHostInfo _hostInfo;
        private DreamServiceInfo _queueService;
        private Plug _plug;

        [TestFixtureSetUp]
        public void GlobalSetup() {
            _hostInfo = DreamTestHelper.CreateRandomPortHost();
            _queueService = DreamTestHelper.CreateService(_hostInfo, "sid://mindtouch.com/2009/12/dream/queue", "queue", new XDoc("config").Elem("folder", Path.GetTempPath()));
            _plug = _queueService.WithInternalKey().AtLocalHost;
        }

        [TestFixtureTearDown]
        public void GlobalTeardown() {
            _hostInfo.Dispose();
        }

        [Test]
        public void Creating_two_QueueServices_with_same_path_throws_on_same_queue_access() {
            var path = Path.Combine(Path.Combine(Path.GetTempPath(), StringUtil.CreateAlphaNumericKey(6)), "duplicate");
            var s1 = DreamTestHelper.CreateService(_hostInfo, "sid://mindtouch.com/2009/12/dream/queue", "q", new XDoc("config").Elem("folder", path));
            var p1 = s1.WithInternalKey().AtLocalHost;
            var s2 = DreamTestHelper.CreateService(_hostInfo, "sid://mindtouch.com/2009/12/dream/queue", "q", new XDoc("config").Elem("folder", path));
            var p2 = s2.WithInternalKey().AtLocalHost;
            var response = p1.At("queue", "duplo", "size").Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            response = p2.At("queue", "duplo", "size").Get(new Result<DreamMessage>()).Wait();
            response.AssertStatus(DreamStatus.InternalError);
            s1.WithPrivateKey().AtLocalHost.Delete();
            Directory.Delete(path,true);
        }

        [Test]
        public void Can_queue_peek_take() {
            var queue = StringUtil.CreateAlphaNumericKey(4);
            var doc = new XDoc("foo").Elem("bar", "baz");

            // queue item
            var response = _plug.At("queue", queue).Post(doc, new Result<DreamMessage>()).Wait();
            response.AssertSuccess();

            // get item
            response = _plug.At("queue", queue).Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            Assert.AreEqual(doc, response.ToDocument());
            var deletePlug = Plug.New(response.Headers.Location).WithCookieJar(_plug.CookieJar);

            // take item
            response = deletePlug.Delete(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();

            // get no item
            response = _plug.At("queue", queue).Get(new Result<DreamMessage>()).Wait();
            response.AssertStatus(DreamStatus.NoContent);
        }

        [Test]
        public void Can_queue_peek_expire_and_peek_again() {
            var queue = StringUtil.CreateAlphaNumericKey(4);
            var doc = new XDoc("foo").Elem("bar", "baz");

            // queue item
            var response = _plug.At("queue", queue).Post(doc, new Result<DreamMessage>()).Wait();
            response.AssertSuccess();

            // get item with 1 second expiration
            response = _plug.At("queue", queue).With("expire", 1).Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            Assert.AreEqual(doc, response.ToDocument());

            // expire item
            Thread.Sleep(1200);

            // try to take item
            var deletePlug = Plug.New(response.Headers.Location).WithCookieJar(_plug.CookieJar);
            response = deletePlug.Delete(new Result<DreamMessage>()).Wait();
            response.AssertStatus(DreamStatus.Gone);

            // get same item again
            response = _plug.At("queue", queue).Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            Assert.AreEqual(doc, response.ToDocument());
        }

        [Test]
        public void Can_get_queue_size() {
            var queue = StringUtil.CreateAlphaNumericKey(4);
            var doc = new XDoc("foo").Elem("bar", "baz");

            // queue item
            var response = _plug.At("queue", queue).Post(doc, new Result<DreamMessage>()).Wait();
            response.AssertSuccess();

            // check size
            response = _plug.At("queue", queue, "size").Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            Assert.AreEqual("1", response.ToDocument()["size"].AsText);

            // queue item
            response = _plug.At("queue", queue).Post(doc, new Result<DreamMessage>()).Wait();
            response.AssertSuccess();

            // check size
            response = _plug.At("queue", queue, "size").Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            Assert.AreEqual("2", response.ToDocument()["size"].AsText);
        }

        [Test]
        public void Expired_msg_affects_size() {
            var queue = StringUtil.CreateAlphaNumericKey(4);
            var doc = new XDoc("foo").Elem("bar", "baz");

            // queue item
            var response = _plug.At("queue", queue).Post(doc, new Result<DreamMessage>()).Wait();
            response.AssertSuccess();

            // check size
            response = _plug.At("queue", queue, "size").Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            Assert.AreEqual("1", response.ToDocument()["size"].AsText);

            // get item
            response = _plug.At("queue", queue).With("expire", 1).Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();

            // check size
            response = _plug.At("queue", queue, "size").Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            Assert.AreEqual("0", response.ToDocument()["size"].AsText);

            // expire taken item
            Thread.Sleep(1200);

            // check size
            response = _plug.At("queue", queue, "size").Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            Assert.AreEqual("1", response.ToDocument()["size"].AsText);
        }

        [Test]
        public void Can_wipe_queue() {
            var queue = StringUtil.CreateAlphaNumericKey(4);
            var doc = new XDoc("foo").Elem("bar", "baz");

            // queue item
            var response = _plug.At("queue", queue).Post(doc, new Result<DreamMessage>()).Wait();
            response.AssertSuccess();

            // check sizw
            response = _plug.At("queue", queue, "size").Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            Assert.AreEqual("1", response.ToDocument()["size"].AsText);

            // wipe queue
            response = _plug.At("queue", queue).Delete(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();

            // check size
            response = _plug.At("queue", queue, "size").Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            Assert.AreEqual("0", response.ToDocument()["size"].AsText);
        }

        [Test]
        public void Queue_survives_service_restart() {
            var queue = StringUtil.CreateAlphaNumericKey(4);
            var doc = new XDoc("foo").Elem("bar", "baz");

            // queue item
            var response = _plug.At("queue", queue).Post(doc, new Result<DreamMessage>()).Wait();
            response.AssertSuccess();

            // destroy and recreate service
            _queueService.WithPrivateKey().AtLocalHost.Delete(new Result<DreamMessage>()).Wait().AssertSuccess();
            var q = DreamTestHelper.CreateService(_hostInfo, "sid://mindtouch.com/2009/12/dream/queue", "queue", new XDoc("config").Elem("folder", Path.GetTempPath()));
            var p = q.WithInternalKey().AtLocalHost;

            // check size
            response = p.At("queue", queue, "size").Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            Assert.AreEqual("1", response.ToDocument()["size"].AsText);

            // get item
            response = p.At("queue", queue).Get(new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            Assert.AreEqual(doc, response.ToDocument());
        }

        [Ignore("slow perf test")]
        [Test]
        public void Perf_test_single_thread_put_peek_and_take() {
            var queue = StringUtil.CreateAlphaNumericKey(4);
            long totalEnqueue = 0;
            long totalDequeue = 0;
            const int n = 5000;
            const int m = 3;

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
                DreamMessage response;
                var stopwatch = Stopwatch.StartNew();
                foreach(var itm in items) {
                    response = _plug.At("queue", queue).Post(itm, new Result<DreamMessage>()).Wait();
                    response.AssertSuccess();
                }
                response = _plug.At("queue", queue, "size").Get(new Result<DreamMessage>()).Wait();
                response.AssertSuccess();
                Assert.AreEqual(n, response.ToDocument()["size"].AsInt ?? -1);
                stopwatch.Stop();
                totalEnqueue += stopwatch.ElapsedMilliseconds;
                var j = 0;
                stopwatch = Stopwatch.StartNew();
                response = _plug.At("queue", queue).Get(new Result<DreamMessage>()).Wait();
                response.AssertSuccess();
                while(response.Status == DreamStatus.Ok) {
                    var deletePlug = Plug.New(response.Headers.Location).WithCookieJar(_plug.CookieJar);
                    response = deletePlug.Delete(new Result<DreamMessage>()).Wait();
                    response.AssertSuccess();
                    j++;
                    response = _plug.At("queue", queue).Get(new Result<DreamMessage>()).Wait();
                }
                stopwatch.Stop();
                totalDequeue += stopwatch.ElapsedMilliseconds;
                Assert.AreEqual(n, j);
                response = _plug.At("queue", queue, "size").Get(new Result<DreamMessage>()).Wait();
                response.AssertSuccess();
                Assert.AreEqual(0, response.ToDocument()["size"].AsInt ?? -1);
            }
            Console.WriteLine("Enqueue: {0:0,000}/s", n * m * 1000 / totalEnqueue);
            Console.WriteLine("Dequeue: {0:0,000}/s", n * m * 1000 / totalDequeue);
        }
    }
}