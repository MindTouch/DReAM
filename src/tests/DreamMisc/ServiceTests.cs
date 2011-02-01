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
using log4net;

using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Dream.Test {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class ServiceTests {
        private DreamHostInfo _hostInfo;

        [SetUp]
        public void Init() {
            _hostInfo = DreamTestHelper.CreateRandomPortHost();
            _hostInfo.Host.Self.At("load").With("name", "test.mindtouch.dream").Post(DreamMessage.Ok());

        }

        [Test]
        public void Service_can_create_child_service() {
            XDoc config = new XDoc("config")
                .Elem("path", "parent")
                .Elem("sid", "sid://mindtouch.com/TestParentService");
            DreamMessage result = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).PostAsync(config).Wait();
            Assert.IsTrue(result.IsSuccessful, result.ToText());
            Plug localhost = Plug.New(_hostInfo.LocalHost.Uri.WithoutQuery());
            result = localhost.At("parent", "child", "test").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.NotFound, result.Status, result.ToText());
            result = localhost.At("parent", "createchild").GetAsync().Wait();
            Assert.IsTrue(result.IsSuccessful, result.ToText());
            result = localhost.At("parent", "child", "test").GetAsync().Wait();
            Assert.IsTrue(result.IsSuccessful, result.ToText());
        }

        [Test]
        public void Can_provide_list_of_args_as_repeated_params_to_feature() {
            MockServiceInfo mock = MockService.CreateMockService(_hostInfo);
            mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                XDoc msg = new XDoc("ids");
                foreach(KeyValuePair<string, string> kv in context.GetParams()) {
                    if(kv.Key == "id") {
                        msg.Elem("id", kv.Value);
                    }
                }
                response2.Return(DreamMessage.Ok(msg));
            };
            Plug p = mock.AtLocalHost;
            int n = 100;
            List<string> ids = new List<string>();
            for(int i = 0; i < n; i++) {
                p = p.With("id", i);
                ids.Add(i.ToString());
            }
            DreamMessage result = p.GetAsync().Wait();
            Assert.IsTrue(result.IsSuccessful);
            List<string> seen = new List<string>();
            foreach(XDoc id in result.ToDocument()["id"]) {
                string v = id.AsText;
                Assert.Contains(v, ids);
                Assert.IsFalse(seen.Contains(v));
                seen.Add(v);
            }
            Assert.AreEqual(ids.Count, seen.Count);
        }

        [Test]
        public void Service_throwing_on_start_does_not_grab_the_uri() {
            XDoc config = new XDoc("config")
                .Elem("path", "bad")
                .Elem("throw", "true")
                .Elem("sid", "sid://mindtouch.com/TestBadStartService");
            try {
                DreamTestHelper.CreateService(_hostInfo, config);
                Assert.Fail("service creation should have failed");
            } catch { }
            var response = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).Get();
            Assert.AreEqual(0, response.ToDocument()[string.Format("service[sid='sid://mindtouch.com/TestBadStartService']")].ListLength);
            config = new XDoc("config")
                .Elem("path", "bad")
                .Elem("throw", "false")
                .Elem("sid", "sid://mindtouch.com/TestBadStartService");
            DreamTestHelper.CreateService(_hostInfo, config);
            response = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).Get();
            Assert.AreEqual(1, response.ToDocument()[string.Format("service[sid='sid://mindtouch.com/TestBadStartService']")].ListLength);
        }

        [Test]
        public void Child_service_throwing_on_start_does_not_grab_the_uri() {
            XDoc config = new XDoc("config")
                 .Elem("path", "empty")
                 .Elem("sid", "sid://mindtouch.com/TestParentService");
            var serviceInfo = DreamTestHelper.CreateService(_hostInfo, config);
            Plug parent = serviceInfo.WithPrivateKey().AtLocalHost;
            var response = parent.At("createbadstartchild").With("throw", "true").GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful, response.ToText());
            response = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).Get();
            Assert.AreEqual(0, response.ToDocument()[string.Format("service[sid='sid://mindtouch.com/TestBadStartService']")].ListLength);
            response = parent.At("createbadstartchild").With("throw", "false").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            response = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).Get();
            Assert.AreEqual(1, response.ToDocument()[string.Format("service[sid='sid://mindtouch.com/TestBadStartService']")].ListLength);
        }

        [Test]
        public void Service_throwing_on_delete_does_not_prevent_cleanup_and_restart() {
            XDoc config = new XDoc("config")
                .Elem("path", "bad")
                .Elem("sid", "sid://mindtouch.com/TestBadStopService");
            var serviceInfo = DreamTestHelper.CreateService(_hostInfo, config);
            var response = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).Get();
            Assert.AreEqual("/bad", response.ToDocument()[string.Format("service[sid='sid://mindtouch.com/TestBadStopService']/path")].Contents);
            response = serviceInfo.WithPrivateKey().AtLocalHost.DeleteAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            response = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).Get();
            Assert.AreEqual(0, response.ToDocument()[string.Format("service[sid='sid://mindtouch.com/TestBadStopService']")].ListLength);
            DreamTestHelper.CreateService(_hostInfo, config);
            response = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).Get();
            Assert.AreEqual("/bad", response.ToDocument()[string.Format("service[sid='sid://mindtouch.com/TestBadStopService']/path")].Contents);
        }

        [Test]
        public void Child_service_throwing_on_delete_does_not_prevent_cleanup_and_restart_of_child() {
            XDoc config = new XDoc("config")
                 .Elem("path", "empty")
                 .Elem("sid", "sid://mindtouch.com/TestParentService");
            var serviceInfo = DreamTestHelper.CreateService(_hostInfo, config);
            Plug parent = serviceInfo.WithPrivateKey().AtLocalHost;
            var response = parent.At("createbadchild").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            response = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).Get();
            Assert.AreEqual(1, response.ToDocument()[string.Format("service[sid='sid://mindtouch.com/TestBadStopService']")].ListLength);
            response = parent.At("destroybadchild").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            response = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).Get();
            Assert.AreEqual(0, response.ToDocument()[string.Format("service[sid='sid://mindtouch.com/TestBadStopService']")].ListLength);
        }

        [Test]
        public void Creating_two_services_at_same_uri_fails() {
            XDoc config = new XDoc("config")
                .Elem("path", "empty")
                .Elem("sid", "sid://mindtouch.com/TestEmptyService");
            var serviceInfo = DreamTestHelper.CreateService(_hostInfo, config);
            var response = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).Get();
            Assert.AreEqual("/empty", response.ToDocument()[string.Format("service[sid='sid://mindtouch.com/TestEmptyService']/path")].Contents);
            try {
                serviceInfo = DreamTestHelper.CreateService(_hostInfo, config);
                Assert.Fail();
            } catch { }
        }

        [Test]
        public void Creating_two_child_services_at_same_uri_fails() {
            XDoc config = new XDoc("config")
                 .Elem("path", "empty")
                 .Elem("sid", "sid://mindtouch.com/TestParentService");
            var serviceInfo = DreamTestHelper.CreateService(_hostInfo, config);
            Plug parent = serviceInfo.WithPrivateKey().AtLocalHost;
            var response = parent.At("createchild").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            response = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).Get();
            Assert.AreEqual(1, response.ToDocument()[string.Format("service[sid='sid://mindtouch.com/TestChildService']")].ListLength);
            response = parent.At("createchild").GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful, response.ToText());
        }
    }

    [DreamService("TestParentService", "Copyright (c) 2008 MindTouch, Inc.",
        Info = "",
        SID = new[] { "sid://mindtouch.com/TestParentService" }
    )]
    public class TestParentService : DreamService {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();
        private Plug _badChild;

        [DreamFeature("*:createchild", "test")]
        public Yield CreateChildService(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            yield return CreateService("child", "sid://mindtouch.com/TestChildService", null, new Result<Plug>());
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("*:createbadstartchild", "test")]
        public Yield CreateBadStartChild(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            yield return CreateService(
                "badchild",
                "sid://mindtouch.com/TestBadStartService",
                new XDoc("config").Elem("throw", context.GetParam("throw", "false")),
                new Result<Plug>()).Set(v => _badChild = v);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("*:createbadchild", "test")]
        public Yield CreateBadChild(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            yield return CreateService("badchild", "sid://mindtouch.com/TestBadStopService", null, new Result<Plug>()).Set(v => _badChild = v);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("*:destroybadchild", "test")]
        public Yield DestroyBadChild(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(_badChild != null) {
                yield return _badChild.DeleteAsync().CatchAndLog(_log);
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            result.Return();
        }
    }

    [DreamService("TestChildService", "Copyright (c) 2008 MindTouch, Inc.",
        Info = "",
        SID = new[] { "sid://mindtouch.com/TestChildService" }
    )]
    public class TestChildService : DreamService {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        [DreamFeature("*:test", "test")]
        public Yield Test(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            response.Return(DreamMessage.Ok());
            yield break;
        }
    }

    [DreamService("TestBadStartService", "Copyright (c) 2009 MindTouch, Inc.",
        Info = "",
        SID = new[] { "sid://mindtouch.com/TestBadStartService" }
    )]
    public class TestBadStartService : DreamService {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            if(config["throw"].AsBool ?? false) {
                _log.DebugFormat("start about to throw");
                throw new Exception("don't get me started");
            }
            _log.DebugFormat("start not throwing");
            result.Return();
        }
    }

    [DreamService("TestBadStopService", "Copyright (c) 2009 MindTouch, Inc.",
        Info = "",
        SID = new[] { "sid://mindtouch.com/TestBadStopService" }
    )]
    public class TestBadStopService : DreamService {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            result.Return();
        }

        protected override Yield Stop(Result result) {
            yield return Coroutine.Invoke(base.Stop, new Result());
            _log.DebugFormat("stop about to throw");
            throw new Exception("You can't stop me!");
        }
    }

    [DreamService("TestEmptyService", "Copyright (c) 2009 MindTouch, Inc.",
        Info = "",
        SID = new[] { "sid://mindtouch.com/TestEmptyService" }
    )]
    public class TestEmptyService : DreamService { }


}
