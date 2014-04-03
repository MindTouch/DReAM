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

using System;
using System.Collections.Generic;
using log4net;

using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Dream.Test {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class ServiceExceptionMapTests {
        private DreamHostInfo _hostInfo;
        private DreamServiceInfo _serviceInfo;

        [TestFixtureSetUp]
        public void FixtureSetup() {
            _hostInfo = DreamTestHelper.CreateRandomPortHost();
            _serviceInfo = DreamTestHelper.CreateService(_hostInfo, typeof(TestExceptionalService), "test");
        }

        [TestFixtureTearDown]
        public void FixtureTeardown() {
            _hostInfo.Dispose();
        }

        [Test]
        public void Can_throw_in_Start_with_exception_translators_in_place() {
            try {
                _serviceInfo = DreamTestHelper.CreateService(_hostInfo, typeof(TestExceptionalService), "throwsonstart", new XDoc("config").Elem("throw-on-start", true));
                Assert.Fail("didn't throw on start");
            } catch(Exception e) {
                Assert.IsTrue(e.Message.EndsWith("BadRequest: throwing in service start"),e.Message);
            }
        }
       
        [Test]
        public void Should_return_successfully() {
            var response = _serviceInfo.AtLocalHost.At("test").With("throwwhere", "never").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
        }

        [Test]
        public void Map_plain_exception_to_badrequest_in_main() {
            var response = _serviceInfo.AtLocalHost.At("test").With("throwwhere", "main").GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.BadRequest, response.Status);
        }

        [Test]
        public void Map_plain_exception_to_badrequest_in_prologue() {
            var response = _serviceInfo.AtLocalHost.At("test").With("throwwhere", "prologue").GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.BadRequest, response.Status);
        }

        [Test]
        public void Map_plain_exception_to_badrequest_in_epilogue() {
            var response = _serviceInfo.AtLocalHost.At("test").With("throwwhere", "epilogue").GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.BadRequest, response.Status);
        }

        [Test]
        public void Map_custom_exception_to_notacceptable_in_main() {
            var response = _serviceInfo.AtLocalHost.At("test")
                .With("throwwhere", "main")
                .With("throwwhat", "custom")
                .GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.NotAcceptable, response.Status);
        }

        [Test]
        public void Map_custom_exception_to_notacceptable_in_prologue() {
            var response = _serviceInfo.AtLocalHost.At("test")
                .With("throwwhere", "prologue")
                .With("throwwhat", "custom")
                .GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.NotAcceptable, response.Status);
        }

        [Test]
        public void Map_custom_exception_to_notacceptable_in_epilogue() {
            var response = _serviceInfo.AtLocalHost.At("test")
                .With("throwwhere", "epilogue")
                .With("throwwhat", "custom")
                .GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.NotAcceptable, response.Status);
        }

        [Test]
        public void DreamForbiddenException_in_main_does_not_hit_mapping() {
            var response = _serviceInfo.AtLocalHost.At("test")
                .With("throwwhere", "main")
                .With("throwwhat", "forbidden")
                .GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }

        [Test]
        public void DreamForbiddenException_in_prologue_does_not_hit_mapping() {
            var response = _serviceInfo.AtLocalHost.At("test")
                .With("throwwhere", "prologue")
                .With("throwwhat", "forbidden")
                .GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }

        [Test]
        public void DreamForbiddenException_in_epilogue_does_not_hit_mapping() {
            var response = _serviceInfo.AtLocalHost.At("test")
                .With("throwwhere", "epilogue")
                .With("throwwhat", "forbidden")
                .GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }
    }

    public class CustomException : Exception { }

    [DreamService("TestExceptionalService", "Copyright (c) 200TestExceptionalService MindTouch, Inc.",
        Info = "",
        SID = new[] { "sid://mindtouch.com/TestExceptionalService" }
    )]
    public class TestExceptionalService : DreamService {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        [DreamFeature("*:test", "test")]
        public Yield Test(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            CheckThrow(context, "main");
            response.Return(DreamMessage.Ok());
            yield break;
        }

        private void CheckThrow(DreamContext context, string stage) {
            var throwWhere = context.GetParam("throwwhere", "");
            var throwWhat = context.GetParam("throwwhat", "");
            if(StringUtil.EqualsInvariantIgnoreCase(stage, throwWhere)) {
                if(string.IsNullOrEmpty(throwWhat)) {
                    throw new Exception("plain old exception");
                }
                if(StringUtil.EqualsInvariant("custom", throwWhat)) {
                    throw new CustomException();
                }
                if(StringUtil.EqualsInvariant("forbidden", throwWhat)) {
                    throw new DreamForbiddenException("what? where?");
                }
            }
        }

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            if( config["throw-on-start"].AsBool ?? false) {
                throw new Exception("throwing in service start");
            }
            result.Return();
        }

        public override DreamFeatureStage[] Prologues { get { return new[] { new DreamFeatureStage("prologue", Prologue, DreamAccess.Public), }; } }
        public override DreamFeatureStage[] Epilogues { get { return new[] { new DreamFeatureStage("epilogue", Epilogue, DreamAccess.Public), }; } }
        public override ExceptionTranslator[] ExceptionTranslators { get { return new ExceptionTranslator[] { MapCustomException, MapPlainException }; } }

        private DreamMessage MapPlainException(DreamContext context, Exception exception) {
            return DreamMessage.BadRequest(exception.Message);
        }

        private DreamMessage MapCustomException(DreamContext context, Exception exception) {
            if(typeof(CustomException).IsAssignableFrom(exception.GetType())) {
                return new DreamMessage(DreamStatus.NotAcceptable, null);
            }
            return null;
        }
        private Yield Prologue(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            CheckThrow(context, "prologue");
            response.Return(request);
            yield break;
        }

        private Yield Epilogue(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            CheckThrow(context, "epilogue");
            response.Return(request);
            yield break;
        }

    }

}
