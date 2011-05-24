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
using System.Collections.Generic;
using System.Linq;
using log4net;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class DreamHostAliasesTests {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();
        private DreamHostInfo _hostAliasMemorize;
        private DreamHostInfo _hostNoAliasMemorize;
        private MockServiceInfo _service;
        private XUri _external;
        //private XUri _ext2;
        private int _externalCalled;
        //private int _ext2called;
        private List<string> _calledPaths;

        [TestFixtureTearDown]
        public void TearDown() {
            if(_hostAliasMemorize != null) {
                _hostAliasMemorize.Dispose();
            }
            if(_hostNoAliasMemorize != null) {
                _hostNoAliasMemorize.Dispose();
            }
        }

        [Test]
        public void Incoming_uri_does_not_get_memorized_all_by_itself() {
            SetupService(true);
            _service.AtLocalHost.With(DreamInParam.URI, "http://somehost").With("call", _external.At("externalCallback").ToString()).At("external").Get();
            Assert.AreEqual(new[] { "external" }, _calledPaths.ToArray(), "the wrong paths were called on the mock service");
            Assert.AreEqual(2, _externalCalled, "external endpoint was not called the expected number of time");
        }

        [Test]
        public void Can_inject_alias_via_DreamUriIn_param_for_current_request() {
            SetupService(true);
            _service.AtLocalHost.With(DreamInParam.URI, _external.WithoutPathQueryFragment().WithoutTrailingSlash().ToString()).With("call", _external.At("externalCallback").ToString()).At("external").Get();
            Assert.AreEqual(new[] { "external", "externalCallback" }, _calledPaths.ToArray(), "the wrong paths were called on the mock service");
            Assert.AreEqual(1, _externalCalled, "external endpoint was not called the expected number of time");
        }

        [Test]
        public void Can_inject_alias_via_dream_transport_header_for_current_request() {
            SetupService(true);
            _service.AtLocalHost.WithHeader(DreamHeaders.DREAM_TRANSPORT, _external.WithoutPathQueryFragment().WithoutTrailingSlash().ToString()).With("call", _external.At("externalCallback").ToString()).At("external").Get();
            Assert.AreEqual(new[] { "external", "externalCallback" }, _calledPaths.ToArray(), "the wrong paths were called on the mock service");
            Assert.AreEqual(1, _externalCalled, "external endpoint was not called the expected number of time");
        }

        [Test]
        public void Can_inject_alias_via_DreamUriIn_param_for_future_requests() {
            SetupService(true);
            _service.AtLocalHost.With(DreamInParam.URI, _external.WithoutPathQueryFragment().WithoutTrailingSlash().ToString()).At("external").Get();
            _service.AtLocalHost.With("call", _external.At("externalCallback").ToString()).At("externalAgain").Get();
            Assert.AreEqual(new[] { "external", "externalAgain", "externalCallback" }, _calledPaths.ToArray(), "the wrong paths were called on the mock service");
            Assert.AreEqual(1, _externalCalled, "external endpoint was not called the expected number of time");
        }

        [Test]
        public void Can_inject_alias_via_dream_transport_header_for_future_requests() {
            SetupService(true);
            _service.AtLocalHost.WithHeader(DreamHeaders.DREAM_TRANSPORT, _external.WithoutPathQueryFragment().WithoutTrailingSlash().ToString()).At("external").Get();
            _service.AtLocalHost.With("call", _external.At("externalCallback").ToString()).At("externalAgain").Get();
            Assert.AreEqual(new[] { "external", "externalAgain", "externalCallback" }, _calledPaths.ToArray(), "the wrong paths were called on the mock service");
            Assert.AreEqual(1, _externalCalled, "external endpoint was not called the expected number of time");
        }

        [Test]
        public void Can_disable_alias_injection_via_DreamUriIn_param() {
            SetupService(false);
            _service.AtLocalHost.With(DreamInParam.URI, _external.WithoutPathQueryFragment().WithoutTrailingSlash().ToString()).With("call", _external.At("externalCallback").ToString()).At("external").Get();
            Assert.AreEqual(new[] { "external", }, _calledPaths.ToArray(), "the wrong paths were called on the mock service");
            Assert.AreEqual(2, _externalCalled, "external endpoint was not called the expected number of time");
        }

        [Test]
        public void Can_disable_alias_injection_via_dream_transport_header() {
            SetupService(false);
            _service.AtLocalHost.WithHeader(DreamHeaders.DREAM_TRANSPORT, _external.WithoutPathQueryFragment().WithoutTrailingSlash().ToString()).With("call", _external.At("externalCallback").ToString()).At("external").Get();
            Assert.AreEqual(new[] { "external", }, _calledPaths.ToArray(), "the wrong paths were called on the mock service");
            Assert.AreEqual(2, _externalCalled, "external endpoint was not called the expected number of time");
        }

        private void SetupService(bool memorizeAliases) {
            DreamHostInfo host;
            if(memorizeAliases) {
                host = _hostAliasMemorize = DreamTestHelper.CreateRandomPortHost();
            } else {
                host = _hostNoAliasMemorize = DreamTestHelper.CreateRandomPortHost(new XDoc("config").Elem("memorize-aliases", false));
            }
            _service = MockService.CreateMockService(host);
            _calledPaths = new List<string>();
            _service.Service.CatchAllCallback = (context, request, result) => {
                _calledPaths.Add(context.GetSuffixes(UriPathFormat.Original).First());
                var call = context.GetParam("call", null);
                if(!string.IsNullOrEmpty(call)) {
                    Plug.New(call).Get();
                }
                result.Return(DreamMessage.Ok());
            };
            _external = new XUri("http://external1/").WithPort(_service.AtLocalHost.Uri.Port).At(_service.AtLocalHost.Uri.Segments);
            //_ext2 = new XUri("http://external2/").WithPort(_service.AtLocalHost.Uri.Port).At(_service.AtLocalHost.Uri.Segments);
            _externalCalled = 0;
            MockPlug.Register(_external, (plug, verb, uri, request, response) => {
                _externalCalled++;
                response.Return(DreamMessage.Ok());
            }, 100);
            //_ext2called = 0;
            //MockPlug.Register(_ext2, (plug, verb, uri, request, response) => {
            //    _ext2called++;
            //    response.Return(DreamMessage.Ok());
            //}, 100);
            Plug.New(_external).Get();
            //Plug.New(_ext2).Get();
            //return _calledPaths;
        }
    }
}
