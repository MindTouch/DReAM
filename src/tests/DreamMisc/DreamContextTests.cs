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
    public class DreamContextTests {
        private static readonly ILog _log = LogUtils.CreateLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private DreamHostInfo _hostInfo;
        private Plug _plug;

        [SetUp]
        public void Init() {
            _hostInfo = DreamTestHelper.CreateRandomPortHost();
            _hostInfo.Host.Self.At("load").With("name", "test.mindtouch.dream").Post(DreamMessage.Ok());
            var config = new XDoc("config")
               .Elem("path", "test")
               .Elem("sid", "sid://mindtouch.com/DreamContextTestService");
            DreamMessage result = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).PostAsync(config).Wait();
            Assert.IsTrue(result.IsSuccessful, result.ToText());
            _plug = Plug.New(_hostInfo.LocalHost.Uri.WithoutQuery()).At("test");
            DreamContextTestService.ContextVar = null;
        }

        [TearDown]
        public void Teardown() {
            _hostInfo.Dispose();
        }

        [Test]
        public void ContextVar_is_disposed_after_request() {
            _log.Debug("starting test");
            var response = _plug.At("ping").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.GetErrorString());
            Assert.IsTrue(DreamContextTestService.ContextVar.IsDisposed);
        }

        [Test]
        public void Exception_translators_get_proper_context() {
            _log.Debug("starting test");
            var response = _plug.At("exception").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.GetErrorString());
            Assert.IsTrue(DreamContextTestService.ContextVar.IsDisposed);
        }

        [Test]
        public void FeatureChain_does_not_touch_disposed_context() {
            _log.Debug("starting test");
            var response = _plug.At("disposal").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.GetErrorString());
        }

        [Test]
        public void State_set_in_prologue_is_available_to_feature() {
            var response = _plug.At("prologue").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.GetErrorString());
        }

        [Test]
        public void State_set_in_feature_is_available_to_epilogue() {
            var response = _plug.At("epilogue").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.GetErrorString());
        }

        [Test]
        public void State_set_in_prologue_is_available_to_epilogue() {
            var response = _plug.At("prologueepilogue").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.GetErrorString());
        }

        [Test]
        public void Can_call_coroutine_without_affecting_state() {
            var response = _plug.At("callcoroutine").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.GetErrorString());
        }

        [Test]
        public void Can_call_plug_without_affecting_state() {
            var response = _plug.At("callplug").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.GetErrorString());
        }

        [Test]
        public void Can_call_and_spawn_separate_context_without_affecting_state() {
            var response = _plug.At("spawn").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.GetErrorString());
        }

        [DreamService("DreamContextTestService", "Copyright (c) 2011-2014 MindTouch, Inc.",
            Info = "",
            SID = new[] { "sid://mindtouch.com/DreamContextTestService" }
        )]
        public class DreamContextTestService : DreamService {

            public static ContextLifeSpan ContextVar;
            public static string PrologueData;
            public static string EpilogueData;

            //--- Class Fields ---
            private static readonly ILog _log = LogUtils.CreateLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            [DreamFeature("*:ping", "test")]
            public Yield Ping(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                var guid = Guid.NewGuid();
                ContextVar = new ContextLifeSpan(guid);
                context.SetState(ContextVar);
                response.Return(DreamMessage.Ok());
                yield break;
            }

            [DreamFeature("*:exception", "test")]
            public Yield Exception(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                _log.Debug("in exception feature");
                var guid = Guid.NewGuid();
                ContextVar = new ContextLifeSpan(guid);
                context.SetState(ContextVar);
                throw new CustomException();
            }

            [DreamFeature("*:disposal", "test")]
            public Yield Disposal(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                if(context.IsTaskEnvDisposed) {
                    throw new Exception("context disposed in feature");
                }
                response.Return(DreamMessage.Ok());
                yield break;
            }

            [DreamFeature("*:prologue", "test")]
            public Yield CheckPrologue(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                _log.DebugFormat("checking prologue data in Env #{0}", TaskEnv.Current.GetHashCode());
                if(string.IsNullOrEmpty(PrologueData)) {
                    throw new Exception("no prologue data in slot");
                }
                if(PrologueData != context.GetState<string>("prologue")) {
                    throw new Exception("state from prologue didn't make it to feature");
                }
                response.Return(DreamMessage.Ok());
                yield break;
            }

            [DreamFeature("*:epilogue", "test")]
            public Yield CheckEpilogue(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                EpilogueData = StringUtil.CreateAlphaNumericKey(8);
                _log.DebugFormat("setting epilogue data in Env #{0}", TaskEnv.Current.GetHashCode());
                context.SetState("epilogue", EpilogueData);
                response.Return(DreamMessage.Ok());
                yield break;
            }

            [DreamFeature("*:prologueepilogue", "test")]
            public Yield PrologueEpilogue(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                response.Return(DreamMessage.Ok());
                yield break;
            }

            [DreamFeature("*:spawn", "test")]
            public Yield Spawn(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                var guid = Guid.NewGuid();
                ContextVar = new ContextLifeSpan(guid);
                context.SetState(guid);
                context.SetState(ContextVar);
                ContextLifeSpan capturedInner = null;
                yield return AsyncUtil.Fork(() =>
                {
                    var innerContextVar = DreamContext.Current.GetState<ContextLifeSpan>();
                    capturedInner = innerContextVar;
                    if(innerContextVar == ContextVar) {
                        throw new Exception("spawned context instances were same");
                    }
                    if(innerContextVar.Guid != guid) {
                        throw new Exception("spawned context guid is wrong");
                    }
                    if(innerContextVar.IsDisposed) {
                        throw new Exception("subcall: context is disposed");
                    }
                }, new Result());
                var contextVar = context.GetState<ContextLifeSpan>();
                if(contextVar == null) {
                    throw new Exception("context instance is gone");
                }
                if(capturedInner == contextVar) {
                    throw new Exception("outer instance was changed to inner");
                }
                if(!capturedInner.IsDisposed) {
                    throw new Exception("inner instance wasn't disposed after closure completion");
                }
                if(contextVar.Guid != guid) {
                    throw new Exception("context guid is wrong");
                }
                if(contextVar != ContextVar) {
                    throw new Exception("context instance changed");
                }
                if(contextVar.IsDisposed) {
                    throw new Exception("context is disposed");
                }
                response.Return(DreamMessage.Ok());
                yield break;
            }

            [DreamFeature("*:callplug", "test")]
            public Yield CallPlug(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                _log.Debug("callplug start");
                var guid = Guid.NewGuid();
                ContextVar = new ContextLifeSpan(guid);
                context.SetState(guid);
                _log.Debug("setting disposable state");
                context.SetState(ContextVar);
                Result<DreamMessage> sub;
                _log.Debug("calling plug");
                yield return sub = Self.At("calledplug").GetAsync();
                _log.Debug("return from plug");
                if(!sub.Value.IsSuccessful) {
                    response.Return(sub.Value);
                }
                var contextVar = context.GetState<ContextLifeSpan>();
                if(contextVar == null) {
                    throw new Exception("context instance is gone");
                }
                if(contextVar.Guid != guid) {
                    throw new Exception("context guid is wrong");
                }
                if(contextVar != ContextVar) {
                    throw new Exception("context instance changed");
                }
                if(contextVar.IsDisposed) {
                    throw new Exception("context is disposed");
                }
                _log.Debug("callplug return");
                response.Return(DreamMessage.Ok());
                _log.Debug("callplug end");
                yield break;
            }

            [DreamFeature("*:calledplug", "test")]
            public Yield CalledPlug(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                _log.Debug("calledplug start");
                var contextVar = context.GetState<ContextLifeSpan>();
                if(contextVar != null) {
                    throw new Exception("called plug context instance already exists");
                }
                context.SetState(new ContextLifeSpan(Guid.NewGuid()));
                _log.Debug("calledplug return");
                response.Return(DreamMessage.Ok());
                _log.Debug("calledplug end");
                yield break;
            }

            [DreamFeature("*:callcoroutine", "test")]
            public Yield CallCoroutine(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                _log.Debug("callcoroutine start");
                var guid = Guid.NewGuid();
                ContextVar = new ContextLifeSpan(guid);
                context.SetState(guid);
                context.SetState(ContextVar);
                if(context.GetState<ContextLifeSpan>() == null) {
                    throw new Exception("context instance was never set");
                }
                _log.DebugFormat("callcoroutine calling coroutine within Env #{0}", TaskEnv.Current.GetHashCode());
                yield return Coroutine.Invoke(SubCall, new Result());
                var contextVar = context.GetState<ContextLifeSpan>();
                _log.DebugFormat("callcoroutine coroutine returned within Env #{0}", TaskEnv.Current.GetHashCode());
                if(contextVar == null) {
                    throw new Exception("context instance is gone");
                }
                if(contextVar.Guid != guid) {
                    throw new Exception("context guid is wrong");
                }
                if(contextVar != ContextVar) {
                    throw new Exception("context instance changed");
                }
                if(contextVar.IsDisposed) {
                    throw new Exception("context is disposed");
                }
                response.Return(DreamMessage.Ok());
                yield break;
            }

            private Yield SubCall(Result result) {
                var contextVar = DreamContext.Current.GetState<ContextLifeSpan>();
                _log.DebugFormat("coroutine started within Env #{0}", TaskEnv.Current.GetHashCode());
                if(contextVar != ContextVar) {
                    throw new Exception("subcall: context instance changed");
                }
                if(contextVar.IsDisposed) {
                    throw new Exception("subcall: context is disposed");
                }
                result.Return();
                _log.DebugFormat("coroutine ended within Env #{0}", TaskEnv.Current.GetHashCode());
                yield break;
            }

            protected override Yield Start(XDoc config, Result result) {
                yield return Coroutine.Invoke(base.Start, config, new Result());
                result.Return();
            }

            public override DreamFeatureStage[] Prologues {
                get { return new[] { new DreamFeatureStage("prologue", Prologue, DreamAccess.Public) }; }
            }

            private Yield Prologue(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                _log.Debug("in prologue");
                if("disposal".EqualsInvariant(context.Feature.Signature) && context.IsTaskEnvDisposed) {
                    throw new Exception("context disposed in prologue");
                }
                PrologueData = StringUtil.CreateAlphaNumericKey(8);
                _log.DebugFormat("setting prologue data in Env #{0}", TaskEnv.Current.GetHashCode());
                context.SetState("prologue", PrologueData);
                response.Return(DreamMessage.Ok());
                yield break;
            }

            public override DreamFeatureStage[] Epilogues {
                get { return new[] { new DreamFeatureStage("epilogue", Epilogue, DreamAccess.Public) }; }
            }

            private Yield Epilogue(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                _log.Debug("in epilogue");
                if(request.IsSuccessful && "disposal".EqualsInvariant(context.Feature.Signature) && context.IsTaskEnvDisposed) {
                    throw new Exception("context disposed in epilogue");
                }
                if("prologueepilogue".EqualsInvariant(context.Feature.Signature)) {
                    _log.DebugFormat("checking prologue data for epilogue in Env #{0}", TaskEnv.Current.GetHashCode());
                    if(string.IsNullOrEmpty(PrologueData)) {
                        throw new Exception("no prologue data in slot");
                    }
                    if(PrologueData != context.GetState<string>("prologue")) {
                        throw new Exception("state from prologue didn't make it to epilogue");
                    }
                } else if("epilogue".EqualsInvariant(context.Feature.Signature)) {
                    _log.DebugFormat("checking epilogue data for epilogue in Env #{0}", TaskEnv.Current.GetHashCode());
                    if(string.IsNullOrEmpty(EpilogueData)) {
                        throw new Exception("no epilogue data in slot");
                    }
                    if(EpilogueData != context.GetState<string>("epilogue")) {
                        throw new Exception("state from feature didn't make it to epilogue");
                    }
                }
                response.Return(request);
                yield break;
            }

            public override ExceptionTranslator[] ExceptionTranslators { get { return new ExceptionTranslator[] { ExceptionTranslation }; } }

            private DreamMessage ExceptionTranslation(DreamContext context, Exception exception) {
                if(exception is CustomException) {
                    _log.DebugFormat("caught custom exception");
                    if(context.IsTaskEnvDisposed) {
                        return DreamMessage.BadRequest("context is disposed");
                    }
                    if(ContextVar == null) {
                        return DreamMessage.BadRequest("context var wasn't set");
                    }
                    if(ContextVar != context.GetState<ContextLifeSpan>()) {
                        return DreamMessage.BadRequest("context vars didn't match");
                    }
                    return DreamMessage.Ok();
                }
                return null;
            }
        }

        public class ContextLifeSpan : ITaskLifespan {

            public readonly Guid Guid;
            private bool _disposed;

            public ContextLifeSpan(Guid guid) {
                Guid = guid;
            }

            public bool IsDisposed {
                get { return _disposed; }
            }

            public object Clone() {
                return new ContextLifeSpan(Guid);
            }

            public void Dispose() {
                _disposed = true;
            }
        }
    }
}
