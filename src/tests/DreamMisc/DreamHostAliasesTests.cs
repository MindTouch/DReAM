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
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class DreamHostAliasesTests {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        [Test]
        public void Can_remember_incoming_uri_for_local_resolution_at_request_time() {
            var host = DreamTestHelper.CreateRandomPortHost();
            var service = MockService.CreateMockService(host);
            var paths = new List<string>();
            service.Service.CatchAllCallback = (context, request, result) => {
                paths.Add(context.GetSuffixes(UriPathFormat.Original).First());
                var call = context.GetParam("call");
                if(!string.IsNullOrEmpty(call)) {
                    Plug.New(call).Get();
                }
                result.Return(DreamMessage.Ok());
            };
            var ext1 = new XUri("http://external1/").WithPort(service.AtLocalHost.Uri.Port).AtPath(service.AtLocalHost.Uri.Path);
            var ext2 = new XUri("http://external2/").WithPort(service.AtLocalHost.Uri.Port).AtPath(service.AtLocalHost.Uri.Path);
            var ext1called = 0;
            MockPlug.Register(ext1, (plug, verb, uri, request, response) => {
                ext1called++;
                response.Return(DreamMessage.Ok());
            });
            var ext2called = 0;
            MockPlug.Register(ext2, (plug, verb, uri, request, response) => {
                ext2called++;
                response.Return(DreamMessage.Ok());
            });
            Plug.New(ext1).Get();
            Plug.New(ext2).Get();
            service.AtLocalHost.With(DreamInParam.URI, ext1.WithoutPathQueryFragment().ToString()).With("call", ext1.At("ext1callback").ToString()).At("ext1").Get();
            service.AtLocalHost.With(DreamInParam.URI, ext1.WithoutPathQueryFragment().ToString()).With("call", ext2.At("ext2callback").ToString()).At("ext2").Get();
            Assert.AreEqual(new[] { "ext1", "ext1callback", "ext2" }, paths.ToArray());
            Assert.AreEqual(1, ext1called);
            Assert.AreEqual(2, ext2called);
        }
    }
}
