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
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test {
    using Yield = IEnumerator<IYield>;

    // ReSharper disable InconsistentNaming
    [TestFixture]
    public class DreamFeatureTests {
        private static readonly ILog _log = LogUtils.CreateLog();
        private DreamHostInfo _hostInfo;
        private Plug _plug;
        private XDoc _blueprint;

        [TestFixtureSetUp]
        public void Init() {
            _hostInfo = DreamTestHelper.CreateRandomPortHost();
            _hostInfo.Host.Self.At("load").With("name", "test.mindtouch.dream").Post(DreamMessage.Ok());
            var config = new XDoc("config")
               .Elem("path", "test")
               .Elem("sid", "http://services.mindtouch.com/dream/test/2010/07/featuretestserver");
            DreamMessage result = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).PostAsync(config).Wait();
            Assert.IsTrue(result.IsSuccessful, result.ToText());
            _plug = Plug.New(_hostInfo.LocalHost.Uri.WithoutQuery()).At("test");
            _blueprint = _plug.At("@blueprint").Get().ToDocument();
        }

        [TestFixtureTearDown]
        public void Teardown() {
            _hostInfo.Dispose();
        }

        [Test]
        public void Can_define_sync_no_arg_feature() {
            AssertFeature(
                "GET:sync/nada",
                _plug.At("sync", "nada"));
        }

        [Test]
        public void Can_define_async_no_arg_feature() {
            AssertFeature(
                "GET:async/nada",
                _plug.At("async", "nada"));
        }

        [Test]
        public void Can_inject_path_parameters() {
            AssertFeature(
                "GET:sync/{x}/{y}",
                _plug.At("sync", "xx", "yy"),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_path_parameters_without_attribute() {
            AssertFeature(
                "GET:sync/noattr/{x}/{y}",
                _plug.At("sync", "noattr", "xx", "yy"),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_path_parameter_and_cast_to_int() {
            AssertFeature(
                "GET:sync/int/{x}",
                _plug.At("sync", "int", "123"),
                new XDoc("r").Elem("x", "123"));
        }

        [Test]
        public void Can_inject_path_parameter_and_cast_to_int_without_attribute() {
            AssertFeature(
                "GET:sync/noattr/int/{x}",
                _plug.At("sync", "noattr", "int", "123"),
                new XDoc("r").Elem("x", "123"));
        }

        [Test]
        public void Auto_casting_path_parameter_with_invalid_value_returns_informative_500() {
            var response = _plug.At("sync", "noattr", "int", "abc").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.InternalError, response.Status);
            var exception = response.ToDocument();
            Assert.AreEqual("MindTouch.Dream.FeatureArgumentParseException", exception["type"].AsText);
            Assert.AreEqual("Cannot parse feature argument 'x'", exception["message"].AsText);
        }

        [Test]
        public void Can_inject_path_parameter_and_cast_to_bool() {
            AssertFeature(
                "GET:sync/bool/{x}",
                _plug.At("sync", "bool", "true"),
                new XDoc("r").Elem("x", true));
        }

        [Test]
        public void Can_inject_path_parameter_and_cast_to_bool_without_attribute() {
            AssertFeature(
                "GET:sync/noattr/bool/{x}",
                _plug.At("sync", "noattr", "bool", "true"),
                new XDoc("r").Elem("x", true));
        }

        [Test]
        public void Can_inject_path_parameter_and_cast_to_enum() {
            AssertFeature(
                "GET:sync/enum/{x}",
                _plug.At("sync", "enum", DreamStatus.SeeOther.ToString()),
                new XDoc("r").Elem("x", DreamStatus.SeeOther));
        }

        [Test]
        public void Can_inject_path_parameter_and_cast_to_enum_without_attribute() {
            AssertFeature(
                "GET:sync/noattr/enum/{x}",
                _plug.At("sync", "noattr", "enum", DreamStatus.SeeOther.ToString()),
                new XDoc("r").Elem("x", DreamStatus.SeeOther));
        }

        [Test]
        public void Can_inject_mixed_type_path_parameter_and_cast_appropriately() {
            AssertFeature(
                "GET:sync/mixed/{x}/{y}/{z}",
                _plug.At("sync", "mixed", "123", "true", DreamStatus.SeeOther.ToString()),
                new XDoc("r").Elem("x", "123").Elem("y", "true").Elem("z", DreamStatus.SeeOther));
        }

        [Test]
        public void Can_inject_mixed_type_path_parameter_and_cast_appropriately_without_attribute() {
            AssertFeature(
                "GET:sync/noattr/mixed/{x}/{y}/{z}",
                _plug.At("sync", "noattr", "mixed", "123", "true", DreamStatus.SeeOther.ToString()),
                new XDoc("r").Elem("x", "123").Elem("y", "true").Elem("z", DreamStatus.SeeOther));
        }

        [Test]
        public void Can_inject_query_args() {
            AssertFeature(
                "GET:sync/queryargs",
                _plug.At("sync", "queryargs").With("x", "xx").With("y", "yy"),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_query_args_without_attributes() {
            AssertFeature(
                "GET:sync/queryargs/noattr",
                _plug.At("sync", "queryargs", "noattr").With("x", "xx").With("y", "yy"),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_ommit_injectable_string_query_args_without_attribute() {
            AssertFeature(
                "GET:sync/queryargs/noattr",
                _plug.At("sync", "queryargs", "noattr"),
                new XDoc("r").Elem("x", "null").Elem("y", "null"));
        }

        [Test]
        public void Can_inject_list_query_args() {
            AssertFeature(
                "GET:sync/multiqueryargs",
                _plug.At("sync", "multiqueryargs").With("x", "1").With("x", "2").With("x", "3").With("y", "yy"),
                new XDoc("r").Elem("x", "1:2:3").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_list_query_args_without_attributes() {
            AssertFeature(
                "GET:sync/multiqueryargs/noattr",
                _plug.At("sync", "multiqueryargs", "noattr").With("x", "1").With("x", "2").With("x", "3").With("y", "yy"),
                new XDoc("r").Elem("x", "1:2:3").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_query_args_and_cast_to_int() {
            AssertFeature(
                "GET:sync/queryargs/int",
                _plug.At("sync", "queryargs", "int").With("x", "123"),
                new XDoc("r").Elem("x", "123"));
        }

        [Test]
        public void Can_inject_query_args_and_cast_to_int_without_attribute() {
            AssertFeature(
                "GET:sync/queryargs/noattr/int",
                _plug.At("sync", "queryargs", "noattr", "int").With("x", "123"),
                new XDoc("r").Elem("x", "123"));
        }

        [Test]
        public void Ommitting_injectable_int_query_args_throws() {
            var response = _plug.At("sync", "queryargs", "noattr", "int").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.BadRequest, response.Status);
            var exception = response.ToDocument();
            Assert.AreEqual("invalid value for feature parameter 'x'", exception["message"].AsText);
        }

        [Test]
        public void Can_inject_query_args_and_cast_to_nullable_int_without_attribute() {
            AssertFeature(
                "GET:sync/queryargs/noattr/nullableint",
                _plug.At("sync", "queryargs", "noattr", "nullableint").With("x", "123"),
                new XDoc("r").Elem("x", "123"));
        }

        [Test]
        public void Can_ommit_nullable_int_query_arg() {
            AssertFeature(
                "GET:sync/queryargs/noattr/nullableint",
                _plug.At("sync", "queryargs", "noattr", "nullableint"),
                new XDoc("r").Elem("x", "null"));
        }

        [Test]
        public void Can_inject_query_args_and_cast_to_bool() {
            AssertFeature(
                "GET:sync/queryargs/bool",
                _plug.At("sync", "queryargs", "bool").With("x", "true"),
                new XDoc("r").Elem("x", true));
        }

        [Test]
        public void Can_inject_query_args_and_cast_to_bool_without_attribute() {
            AssertFeature(
                "GET:sync/queryargs/noattr/bool",
                _plug.At("sync", "queryargs", "noattr", "bool").With("x", "true"),
                new XDoc("r").Elem("x", true));
        }

        [Test]
        public void Ommitting_injectable_bool_query_args_throws() {
            var response = _plug.At("sync", "queryargs", "noattr", "bool").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.BadRequest, response.Status);
            var exception = response.ToDocument();
            Assert.AreEqual("invalid value for feature parameter 'x'", exception["message"].AsText);
        }

        [Test]
        public void Can_inject_query_args_and_cast_to_nullable_bool_without_attribute() {
            AssertFeature(
                "GET:sync/queryargs/noattr/nullablebool",
                _plug.At("sync", "queryargs", "noattr", "nullablebool").With("x", "true"),
                new XDoc("r").Elem("x", "True"));
        }

        [Test]
        public void Can_ommit_nullable_bool_query_arg() {
            AssertFeature(
                "GET:sync/queryargs/noattr/nullablebool",
                _plug.At("sync", "queryargs", "noattr", "nullablebool"),
                new XDoc("r").Elem("x", "null"));
        }

        [Test]
        public void Can_inject_query_args_and_cast_to_enum() {
            AssertFeature(
                "GET:sync/queryargs/enum",
                _plug.At("sync", "queryargs", "enum").With("x", DreamStatus.SeeOther.ToString()),
                new XDoc("r").Elem("x", DreamStatus.SeeOther));
        }

        [Test]
        public void Can_inject_query_args_and_cast_to_enum_without_attribute() {
            AssertFeature(
                "GET:sync/queryargs/noattr/enum",
                _plug.At("sync", "queryargs", "noattr", "enum").With("x", DreamStatus.SeeOther.ToString()),
                new XDoc("r").Elem("x", DreamStatus.SeeOther));
        }

        [Test]
        public void Ommitting_injectable_enum_query_args_throws() {
            var response = _plug.At("sync", "queryargs", "noattr", "enum").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.BadRequest, response.Status);
            var exception = response.ToDocument();
            Assert.AreEqual("invalid value for feature parameter 'x'", exception["message"].AsText);
        }

        [Test]
        public void Can_inject_query_args_and_cast_to_nullable_enum_without_attribute() {
            AssertFeature(
                "GET:sync/queryargs/noattr/nullableenum",
                _plug.At("sync", "queryargs", "noattr", "nullableenum").With("x", DreamStatus.SeeOther.ToString()),
                new XDoc("r").Elem("x", DreamStatus.SeeOther));
        }

        [Test]
        public void Can_ommit_nullable_enum_query_arg() {
            AssertFeature(
                "GET:sync/queryargs/noattr/nullableenum",
                _plug.At("sync", "queryargs", "noattr", "nullableenum"),
                new XDoc("r").Elem("x", "null"));
        }

        [Test]
        public void Can_inject_mixed_type_query_args_and_cast_appropriately() {
            AssertFeature(
                "GET:sync/queryargs/mixed",
                _plug.At("sync", "queryargs", "mixed").With("x", "123").With("y", "true").With("z", DreamStatus.SeeOther.ToString()),
                new XDoc("r").Elem("x", "123").Elem("y", "true").Elem("z", DreamStatus.SeeOther));
        }

        [Test]
        public void Can_inject_mixed_type_query_args_and_cast_appropriately_without_attribute() {
            AssertFeature(
                "GET:sync/queryargs/noattr/mixed",
                _plug.At("sync", "queryargs", "noattr", "mixed").With("x", "123").With("y", "true").With("z", DreamStatus.SeeOther.ToString()),
                new XDoc("r").Elem("x", "123").Elem("y", "true").Elem("z", DreamStatus.SeeOther));
        }

        [Test]
        public void Can_inject_mixed_type_query_args_and_path_parameters_and_cast_appropriately() {
            AssertFeature(
                "GET:sync/mixed/{x}/{z}",
                _plug.At("sync", "mixed", "123", DreamStatus.SeeOther.ToString())
                    .With("y", "true")
                    .With("v", 1.2),
                new XDoc("r").Elem("x", "123").Elem("y", "true").Elem("z", DreamStatus.SeeOther).Elem("v", "1.2"));
        }

        [Test]
        public void Can_inject_mixed_type_query_args_and_path_parameters_and_cast_appropriately_under_non_us_locale() {
            System.Globalization.CultureInfo old = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("FR-fr");
            AssertFeature(
                "GET:sync/mixed/{x}/{z}",
                _plug.At("sync", "mixed", "123", DreamStatus.SeeOther.ToString())
                    .With("y", "true")
                    .With("v", 1.2),
                new XDoc("r").Elem("x", "123").Elem("y", "true").Elem("z", DreamStatus.SeeOther).Elem("v", "1.2"));
            System.Threading.Thread.CurrentThread.CurrentCulture = old;
        }

        [Test]
        public void Can_inject_headers() {
            AssertFeature(
                "GET:sync/headers",
                _plug.At("sync", "headers").WithHeader("x", "xx").WithHeader("y", "yy"),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_cookie_strings() {
            var jar = new DreamCookieJar();
            jar.Update(new DreamCookie("x", "xx", _plug), _plug);
            jar.Update(new DreamCookie("y", "yy", _plug), _plug);
            AssertFeature(
                "GET:sync/cookies/string",
                _plug.At("sync", "cookies", "string").WithCookieJar(jar),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_cookie_objects() {
            var jar = new DreamCookieJar();
            jar.Update(new DreamCookie("x", "xx", _plug), _plug);
            jar.Update(new DreamCookie("y", "yy", _plug), _plug);
            AssertFeature(
                "GET:sync/cookies/obj",
                _plug.At("sync", "cookies", "obj").WithCookieJar(jar),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_cookie_objects_without_cookie_attribute() {
            var jar = new DreamCookieJar();
            jar.Update(new DreamCookie("x", "xx", _plug), _plug);
            AssertFeature(
                "GET:sync/cookies/obj",
                _plug.At("sync", "cookies", "obj").WithCookieJar(jar),
                new XDoc("r").Elem("x", "xx"));
        }

        [Test]
        public void Can_inject_verb() {
            AssertFeature(
                "GET:sync/verb",
                _plug.At("sync", "verb"),
                new XDoc("r").Elem("verb", "GET"));
        }

        [Test]
        public void Can_inject_path_as_segments() {
            AssertFeature(
                "GET:sync/path/segments",
                _plug.At("sync", "path", "segments"),
                new XDoc("r").Elem("path", "sync:path:segments"));
        }

        [Test]
        public void Can_inject_path_as_segments_ignoring_trailing_slash() {
            AssertFeature(
                "GET:sync/path/segments",
                _plug.At("sync", "path", "segments").WithTrailingSlash(),
                new XDoc("r").Elem("path", "sync:path:segments"));
        }

        [Test]
        public void Can_inject_path_as_string() {
            AssertFeature(
                "GET:sync/path/string",
                _plug.At("sync", "path", "string"),
                new XDoc("r").Elem("path", "sync/path/string"));
        }

        [Test]
        public void Can_inject_path_as_string_respecting_trailing_slash() {
            AssertFeature(
                "GET:sync/path/string",
                _plug.At("sync", "path", "string").WithTrailingSlash(),
                new XDoc("r").Elem("path", "sync/path/string/"));
        }

        [Test]
        public void Can_inject_uri() {
            var plug = _plug.At("sync", "uri");
            AssertFeature(
                "GET:sync/uri",
                plug,
                new XDoc("r").Elem("uri", plug));
        }

        [Test]
        public void Can_inject_document_body() {
            AssertFeature(
                "POST:sync/body/xdoc",
                _plug.At("sync", "body", "xdoc").Post(new XDoc("body").Elem("foo", "Bar"), new Result<DreamMessage>()),
                new XDoc("body").Elem("foo", "Bar"));
        }

        [Test]
        public void Can_inject_string_body() {
            AssertFeature(
                "POST:sync/body/string",
                _plug.At("sync", "body", "string").Post(DreamMessage.Ok(MimeType.TEXT, "foo"), new Result<DreamMessage>()),
                new XDoc("body").Value("foo"));
        }

        [Test]
        public void Can_inject_stream_body() {
            AssertFeature(
                "POST:sync/body/stream",
                _plug.At("sync", "body", "stream").Post(DreamMessage.Ok(MimeType.TEXT, "foo"), new Result<DreamMessage>()),
                new XDoc("body").Value("foo"));
        }

        [Test]
        public void Can_inject_byte_array_body() {
            AssertFeature(
                "POST:sync/body/bytes",
                _plug.At("sync", "body", "bytes").Post(DreamMessage.Ok(MimeType.TEXT, "foo"), new Result<DreamMessage>()),
                new XDoc("body").Value("foo"));
        }

        [Test]
        public void Can_inject_DreamContext() {
            AssertFeature(
                "POST:sync/context",
                _plug.At("sync", "context").Post(new XDoc("body").Elem("foo", "Bar"), new Result<DreamMessage>()),
                new XDoc("body").Elem("foo", "Bar"));
        }

        [Test]
        public void Can_inject_request_DreamMessage() {
            AssertFeature(
                "POST:sync/message",
                _plug.At("sync", "message").Post(new XDoc("body").Elem("foo", "Bar"), new Result<DreamMessage>()),
                new XDoc("body").Elem("foo", "Bar"));
        }

        [Test]
        public void Can_get_feature_path_without_trailing_slash() {
            var response = _plug.At("capture", "some", "stuff").WithoutTrailingSlash().Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual("capture/some/stuff", response.ToDocument()["captured"].AsText);
        }


        [Test]
        public void Can_get_feature_path_with_trailing_slash() {
            var response = _plug.At("capture", "some", "stuff").WithTrailingSlash().Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual("capture/some/stuff/", response.ToDocument()["captured"].AsText);
        }

        [Test]
        public void Can_use_xdoc_as_feature_return_type() {
            var msg = new XDoc("doc").Elem("woot", "true");
            var response = _plug.At("xdoc").Post(msg);
            Assert.AreEqual(msg.ToCompactString(), response.ToDocument().ToCompactString());
        }

        private void AssertFeature(string pattern, Plug plug) {
            AssertFeature(pattern, plug, null);
        }

        private void AssertFeature(string pattern, Plug plug, XDoc expected) {
            AssertFeature(pattern, plug.Get(new Result<DreamMessage>()), expected);
        }

        private void AssertFeature(string pattern, Result<DreamMessage> result, XDoc expected) {
            var feature = _blueprint[string.Format("features/feature[pattern='{0}']", pattern)];
            Assert.IsTrue(feature.Any(), string.Format("service doesn't have a feature for {0}", pattern));
            var doc = new XDoc("response").Elem("method", feature["method"].AsText);
            if(expected != null) {
                doc.Add(expected);
            }
            var response = result.Wait();
            Assert.IsTrue(response.IsSuccessful, response.GetErrorString());
            Assert.AreEqual(doc.ToCompactString(), response.ToDocument().ToCompactString());
        }

        [DreamService("MindTouch Feature Test Service", "Copyright (c) 2006-2011 MindTouch, Inc.",
            Info = "http://www.mindtouch.com",
            SID = new[] { "http://services.mindtouch.com/dream/test/2010/07/featuretestserver" }
        )]
        public class FeatureTestService : DreamService {

            // --- Class Fields ---
            private static readonly ILog _log = LogUtils.CreateLog();

            //-- Fields ---
            private Plug _inner;

            //--- Features ---
            [DreamFeature("GET:sync/nada", "")]
            public DreamMessage SyncNada() {
                return Response(null);
            }

            [DreamFeature("GET:async/nada", "")]
            public Yield AsyncNada(Result<DreamMessage> response) {
                response.Return(Response("AsyncNada", null));
                yield break;
            }

            [DreamFeature("GET:sync/{x}/{y}", "")]
            public DreamMessage SyncXY(
                [Path] string x,
                [Path] string y
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y));
            }

            [DreamFeature("GET:sync/noattr/{x}/{y}", "")]
            public DreamMessage SyncXYNoAttr(
                string x,
                string y
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y));
            }

            [DreamFeature("GET:sync/int/{x}", "")]
            public DreamMessage SyncIntPathArg(
                [Path] int x
            ) {
                return Response(new XDoc("r").Elem("x", x));
            }

            [DreamFeature("GET:sync/noattr/int/{x}", "")]
            public DreamMessage SyncIntPathArgNoAttr(
                int x
            ) {
                return Response(new XDoc("r").Elem("x", x));
            }

            [DreamFeature("GET:sync/bool/{x}", "")]
            public DreamMessage SyncBoolPathArg(
                [Path] bool x
            ) {
                return Response(new XDoc("r").Elem("x", x));
            }

            [DreamFeature("GET:sync/noattr/bool/{x}", "")]
            public DreamMessage SyncBoolPathArgNoAttr(
                bool x
            ) {
                return Response(new XDoc("r").Elem("x", x));
            }

            [DreamFeature("GET:sync/enum/{x}", "")]
            public DreamMessage SyncEnumPathArg(
                [Path] DreamStatus x
            ) {
                return Response(new XDoc("r").Elem("x", x));
            }

            [DreamFeature("GET:sync/noattr/enum/{x}", "")]
            public DreamMessage SyncEnumPathArgNoAttr(
                DreamStatus x
            ) {
                return Response(new XDoc("r").Elem("x", x));
            }

            [DreamFeature("GET:sync/mixed/{x}/{y}/{z}", "")]
            public DreamMessage SyncMixedPathArg(
                [Path] int x,
                [Path] bool y,
                [Path] DreamStatus z
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y).Elem("z", z));
            }

            [DreamFeature("GET:sync/noattr/mixed/{x}/{y}/{z}", "")]
            public DreamMessage SyncMixedPathArgNoAttr(
                int x,
                bool y,
                DreamStatus z
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y).Elem("z", z));
            }

            [DreamFeature("GET:sync/multiqueryargs", "")]
            public DreamMessage SyncMultiQueryArgs(
                [Query] string[] x,
                [Query] string y
            ) {
                return Response(new XDoc("r").Elem("x", string.Join(":", x)).Elem("y", y));
            }

            [DreamFeature("GET:sync/multiqueryargs/noattr", "")]
            public DreamMessage SyncMultiQueryArgsNoAttr(
                string[] x,
                string y
            ) {
                return Response(new XDoc("r").Elem("x", string.Join(":", x)).Elem("y", y));
            }

            [DreamFeature("GET:sync/queryargs", "")]
            public DreamMessage SyncQueryArgs(
                [Query] string x,
                [Query] string y
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y));
            }

            [DreamFeature("GET:sync/queryargs/noattr", "")]
            public DreamMessage SyncQueryArgsNoAttr(
                string x,
                string y
            ) {
                if(x == null) { x = "null"; }
                if(y == null) { y = "null"; }
                return Response(new XDoc("r").Elem("x", x).Elem("y", y));
            }

            [DreamFeature("GET:sync/queryargs/int", "")]
            public DreamMessage SyncQueryargsIntPathArg(
                [Query] int x
            ) {
                return Response(new XDoc("r").Elem("x", x));
            }

            [DreamFeature("GET:sync/queryargs/noattr/int", "")]
            public DreamMessage SyncQueryargsIntPathArgNoAttr(
                int x
            ) {
                return Response(new XDoc("r").Elem("x", x));
            }

            [DreamFeature("GET:sync/queryargs/noattr/nullableint", "")]
            public DreamMessage SyncQueryargsNullableIntPathArgNoAttr(
                int? x
            ) {
                var v = x.HasValue ? x.ToString() : "null";
                return Response(new XDoc("r").Elem("x", v));
            }

            [DreamFeature("GET:sync/queryargs/bool", "")]
            public DreamMessage SyncQueryargsBoolPathArg(
                [Query] bool x
            ) {
                return Response(new XDoc("r").Elem("x", x));
            }

            [DreamFeature("GET:sync/queryargs/noattr/bool", "")]
            public DreamMessage SyncQueryargsBoolPathArgNoAttr(
                bool x
            ) {
                return Response(new XDoc("r").Elem("x", x));
            }

            [DreamFeature("GET:sync/queryargs/noattr/nullablebool", "")]
            public DreamMessage SyncQueryargsNullableBoolPathArgNoAttr(
                bool? x
            ) {
                var v = x.HasValue ? x.ToString() : "null";
                return Response(new XDoc("r").Elem("x", v));
            }

            [DreamFeature("GET:sync/queryargs/enum", "")]
            public DreamMessage SyncQueryargsEnumPathArg(
                [Query] DreamStatus x
            ) {
                return Response(new XDoc("r").Elem("x", x));
            }

            [DreamFeature("GET:sync/queryargs/noattr/enum", "")]
            public DreamMessage SyncQueryargsEnumPathArgNoAttr(
                DreamStatus x
            ) {
                return Response(new XDoc("r").Elem("x", x));
            }

            [DreamFeature("GET:sync/queryargs/noattr/nullableenum", "")]
            public DreamMessage SyncQueryargsNullableEnumPathArgNoAttr(
                DreamStatus? x
            ) {
                var v = x.HasValue ? x.ToString() : "null";
                return Response(new XDoc("r").Elem("x", v));
            }

            [DreamFeature("GET:sync/queryargs/mixed", "")]
            public DreamMessage SyncQueryargsMixedPathArg(
                [Query] int x,
                [Query] bool y,
                [Query] DreamStatus z
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y).Elem("z", z));
            }

            [DreamFeature("GET:sync/queryargs/noattr/mixed", "")]
            public DreamMessage SyncQueryargsMixedPathArgNoAttr(
                int x,
                bool y,
                DreamStatus z
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y).Elem("z", z));
            }

            [DreamFeature("GET:sync/mixed/{x}/{z}", "")]
            public DreamMessage SyncMixedArgs(
                int x,
                bool y,
                [Path] DreamStatus z,
                [Query] Decimal v
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y).Elem("z", z).Elem("v", v));
            }

            [DreamFeature("GET:sync/headers", "")]
            public DreamMessage SyncHeaders(
                [Header] string x,
                [Header] string y
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y));
            }

            [DreamFeature("GET:sync/cookies/string", "")]
            public DreamMessage SyncStringCookies(
                [Cookie] string x,
                [Cookie] string y
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y));
            }

            [DreamFeature("GET:sync/cookies/obj", "")]
            public DreamMessage SyncDreamCookies(
                [Cookie] DreamCookie x,
                [Cookie] string y
            ) {
                return Response(new XDoc("r").Elem("x", x.Value).Elem("y", y));
            }

            [DreamFeature("GET:sync/cookies/obj/noattr", "")]
            public DreamMessage SyncDreamCookiesNoAttr(
                DreamCookie x
            ) {
                return Response(new XDoc("r").Elem("x", x.Value));
            }

            [DreamFeature("GET:sync/verb", "")]
            public DreamMessage SyncVerb(string verb) {
                return Response(new XDoc("r").Elem("verb", verb));
            }

            [DreamFeature("GET:sync/path/segments", "")]
            public DreamMessage SyncPathSegments(string[] path) {
                return Response(new XDoc("r").Elem("path", string.Join(":", path)));
            }

            [DreamFeature("GET:sync/path/string", "")]
            public DreamMessage SyncPathString(string path) {
                return Response(new XDoc("r").Elem("path", path));
            }

            [DreamFeature("GET:sync/uri", "")]
            public DreamMessage SyncUri(XUri uri) {
                return Response(new XDoc("r").Elem("uri", uri.WithoutQuery()));
            }

            [DreamFeature("POST:sync/body/xdoc", "")]
            public DreamMessage SyncBodyXDoc(XDoc body) {
                return Response(body);
            }

            [DreamFeature("POST:sync/body/string", "")]
            public DreamMessage SyncBodyString(string body) {
                return Response(new XDoc("body").Value(body));
            }

            [DreamFeature("POST:sync/body/bytes", "")]
            public DreamMessage SyncBodyBytes(byte[] body) {
                return Response(new XDoc("body").Value(Encoding.UTF8.GetString(body)));
            }

            [DreamFeature("POST:sync/body/stream", "")]
            public DreamMessage SyncBodyStream(Stream body) {
                using(var reader = new StreamReader(body)) {
                    return Response(new XDoc("body").Value(reader.ReadToEnd()));
                }
            }

            [DreamFeature("POST:sync/context", "")]
            public DreamMessage SyncContext(DreamContext context) {
                return Response(context.Request.ToDocument());
            }

            [DreamFeature("POST:sync/message", "")]
            public DreamMessage SyncMessage(DreamMessage request) {
                return Response(request.ToDocument());
            }

            [DreamFeature("GET:capture//*", "")]
            public DreamMessage Capture(DreamContext context) {
                var requestPath = context.Uri.GetRelativePathTo(context.Service.Self.Uri);
                return DreamMessage.Ok(new XDoc("body").Elem("captured", requestPath));
            }

            [DreamFeature("POST:xdoc", "")]
            public XDoc XDoc(XDoc body) {
                return body;
            }

            //--- Methods ---
            protected override Yield Start(XDoc config, Result result) {
                yield return Coroutine.Invoke(base.Start, config, new Result());
                yield return CreateService("inner", "http://services.mindtouch.com/dream/test/2007/03/sample-inner", new XDoc("config").Start("prologue").Attr("name", "dummy").Value("p3").End().Start("epilogue").Attr("name", "dummy").Value("e3").End(), new Result<Plug>()).Set(v => _inner = v);
                result.Return();
            }

            protected override Yield Stop(Result result) {
                if(_inner != null) {
                    yield return _inner.DeleteAsync().CatchAndLog(_log);
                    _inner = null;
                }
                yield return Coroutine.Invoke(base.Stop, new Result());
                result.Return();
            }

            private static DreamMessage Response(XDoc body) {
                var frame = new System.Diagnostics.StackFrame(1, false);
                return Response(frame.GetMethod().Name, body);
            }

            private static DreamMessage Response(string methodName, XDoc body) {
                var doc = new XDoc("response").Elem("method", methodName);
                if(body != null) {
                    doc.Add(body);
                }
                return DreamMessage.Ok(doc);
            }
        }
    }
    // ReSharper restore InconsistentNaming
}
