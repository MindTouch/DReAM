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

using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Dream.Test {
    [TestFixture]
    public class Service2ServiceTests {
        private DreamHostInfo _hostInfo;
        private MockServiceInfo _service1;
        private MockServiceInfo _service2;

        [TestFixtureSetUp]
        public void FixtureSetup() {
            _hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config")
                .Elem("ip", "foo:11709")
                .Elem("ip", "bar:47583"));
            _service1 = MockService.CreateMockService(_hostInfo);
            _service2 = MockService.CreateMockService(_hostInfo);
        }

        [TestFixtureTearDown]
        public void FixtureTeardown() {
            _hostInfo.Dispose();
        }

        [Test]
        public void Setting_ip_for_host_registers_public_alias() {
            int hitCounter = 0;
            _service1.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> r) {
                hitCounter++;
                r.Return(DreamMessage.Ok());
            };
            var response = _service1.AtLocalHost.Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(1, hitCounter);
            response = Plug.New(_service1.AtLocalHost.Uri.WithHost("foo").WithPort(11709)).Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(2, hitCounter);
            response = Plug.New(_service1.AtLocalHost.Uri.WithHost("bar").WithPort(47583)).Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(3, hitCounter);
            response = Plug.New(_service1.AtLocalHost.Uri.WithHost("baz").WithPort(80)).Get(new Result<DreamMessage>()).Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(3, hitCounter);
        }

        [Test]
        public void Plug_to_local_conversion_sets_host_header() {
            string receivedHost = null;
            _service1.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> r) {
                receivedHost = request.Headers.Host;
                r.Return(DreamMessage.Ok());
            };
            var response = Plug.New(_service1.AtLocalHost.Uri.WithHost("foo").WithPort(11709)).Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual("foo:11709", receivedHost);
        }

        [Test]
        public void Calling_localhost_with_dream_in_arg_sets_host_header() {
            string receivedHost = null;
            _service1.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> r) {
                receivedHost = request.Headers.Host;
                r.Return(DreamMessage.Ok());
            };
            var response = Plug.New(_service1.AtLocalHost.Uri.WithScheme("ext-http")).With("dream.in.host", "proxy").Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual("proxy", receivedHost);
        }

        [Test]
        public void Calling_service_to_service_with_public_uri_sets_host_header() {
            string receivedHost = null;
            _service1.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> r) {
                var r2 = Plug.New(_service2.AtLocalHost.Uri.WithHost("foo").WithPort(11709)).Get();
                r.Return(DreamMessage.Ok());
            };
            _service2.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> r) {
                receivedHost = request.Headers.Host;
                r.Return(DreamMessage.Ok());
            };
            var response = _service1.AtLocalHost.Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual("foo:11709", receivedHost);
        }
    }
}
