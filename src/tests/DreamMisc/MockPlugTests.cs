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
using System.IO;
using System.Threading;
using log4net;
using MindTouch.Dream.Test.Mock;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Dream.Test {
    [TestFixture]
    public class MockPlugTests {

        private static readonly ILog _log = LogUtils.CreateLog();

        [TearDown]
        public void PerTestCleanup() {
            MockPlug.DeregisterAll();
        }

        [Test]
        public void Default_uri_works_as_no_op_without_registrations() {
            var msg = Plug.New(MockPlug.DefaultUri).Get();
            Assert.AreEqual("empty", msg.ToDocument().Name);
        }

        [Test]
        public void Default_uri_keeps_working_as_no_op_after_DeregisterAll() {
            MockPlug.DeregisterAll();
            var msg = Plug.New(MockPlug.DefaultUri).Get();
            Assert.AreEqual("empty", msg.ToDocument().Name);
        }

        [Test]
        public void Register_twice_throws() {
            XUri uri = new XUri("http://www.mindtouch.com/foo");
            MockPlug.Register(uri, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                r2.Return(DreamMessage.Ok());
            });
            try {
                MockPlug.Register(uri, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                    r2.Return(DreamMessage.Ok());
                });
            } catch(ArgumentException) {
                return;
            } catch(Exception e) {
                Assert.Fail("wrong exception: " + e);
            }
            Assert.Fail("no exception`");
        }

        [Test]
        public void Deregister_allows_reregister_of_uri() {
            XUri uri = new XUri("http://www.mindtouch.com/foo");
            int firstCalled = 0;
            MockPlug.Register(uri, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                firstCalled++;
                r2.Return(DreamMessage.Ok());
            });
            Assert.IsTrue(Plug.New(uri).GetAsync().Wait().IsSuccessful);
            Assert.AreEqual(1, firstCalled);
            MockPlug.Deregister(uri);
            int secondCalled = 0;
            MockPlug.Register(uri, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                secondCalled++;
                r2.Return(DreamMessage.Ok());
            });
            Assert.IsTrue(Plug.New(uri).GetAsync().Wait().IsSuccessful);
            Assert.AreEqual(1, firstCalled);
            Assert.AreEqual(1, secondCalled);
        }

        [Test]
        public void DeregisterAll_clears_all_mocks() {
            int firstCalled = 0;
            XUri uri = new XUri("http://www.mindtouch.com/foo");
            MockPlug.Register(uri, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                firstCalled++;
                r2.Return(DreamMessage.Ok());
            });
            MockPlug.Register(new XUri("http://www.mindtouch.com/bar"), delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                r2.Return(DreamMessage.Ok());
            });
            Assert.IsTrue(Plug.New(uri).GetAsync().Wait().IsSuccessful);
            Assert.AreEqual(1, firstCalled);
            MockPlug.DeregisterAll();
            int secondCalled = 0;
            MockPlug.Register(uri, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                secondCalled++;
                r2.Return(DreamMessage.Ok());
            });
            MockPlug.Register(new XUri("http://www.mindtouch.com/bar"), delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                r2.Return(DreamMessage.Ok());
            });
            Assert.IsTrue(Plug.New(uri).GetAsync().Wait().IsSuccessful);
            Assert.AreEqual(1, firstCalled);
            Assert.AreEqual(1, secondCalled);
        }

        [Test]
        public void Mock_intercepts_exact_match() {
            int called = 0;
            Plug calledPlug = null;
            string calledVerb = null;
            XUri calledUri = null;
            DreamMessage calledRequest;
            Result<DreamMessage> calledResponse;
            MockPlug.Register(new XUri("http://www.mindtouch.com/foo"), delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                calledPlug = p;
                calledVerb = v;
                calledUri = u;
                calledRequest = r;
                calledResponse = r2;
                called++;
                calledResponse.Return(DreamMessage.Ok());
            });

            DreamMessage response = Plug.New("http://www.mindtouch.com").At("foo").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            Assert.AreEqual(1, called);
            Assert.AreEqual("GET", calledVerb);
            Assert.AreEqual(calledUri, new XUri("http://www.mindtouch.com/foo"));
        }

        [Test]
        public void Mock_intercepts_child_path() {
            int called = 0;
            Plug calledPlug = null;
            string calledVerb = null;
            XUri calledUri = null;
            DreamMessage calledRequest;
            Result<DreamMessage> calledResponse;
            MockPlug.Register(new XUri("http://www.mindtouch.com"), delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                calledPlug = p;
                calledVerb = v;
                calledUri = u;
                calledRequest = r;
                calledResponse = r2;
                called++;
                calledResponse.Return(DreamMessage.Ok());
            });

            Plug plug = Plug.New("http://www.mindtouch.com").At("foo");
            DreamMessage response = plug.GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            Assert.AreEqual(1, called);
            Assert.AreEqual("GET", calledVerb);
            Assert.AreEqual(new XUri("http://www.mindtouch.com").At("foo"), calledUri);
            Assert.AreEqual(plug, calledPlug);
        }

        [Test]
        public void Mock_receives_proper_request_body() {
            int called = 0;
            Plug calledPlug = null;
            string calledVerb = null;
            XUri calledUri = null;
            DreamMessage calledRequest = null;
            Result<DreamMessage> calledResponse;
            MockPlug.Register(new XUri("http://www.mindtouch.com/foo"), delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                calledPlug = p;
                calledVerb = v;
                calledUri = u;
                calledRequest = r;
                calledResponse = r2;
                called++;
                calledResponse.Return(DreamMessage.Ok());
            });
            XDoc doc = new XDoc("message").Elem("foo");
            DreamMessage response = Plug.New("http://www.mindtouch.com").At("foo").PostAsync(doc).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            Assert.AreEqual(1, called);
            Assert.AreEqual("POST", calledVerb);
            Assert.AreEqual(doc, calledRequest.ToDocument());
            Assert.AreEqual(calledUri, new XUri("http://www.mindtouch.com/foo"));
        }

        [Test]
        public void Mock_sends_back_proper_response_body() {
            int called = 0;
            Plug calledPlug = null;
            string calledVerb = null;
            XUri calledUri = null;
            DreamMessage calledRequest = null;
            Result<DreamMessage> calledResponse;
            XDoc responseDoc = new XDoc("message").Elem("foo");
            MockPlug.Register(new XUri("http://www.mindtouch.com/foo"), delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                calledPlug = p;
                calledVerb = v;
                calledUri = u;
                calledRequest = r;
                calledResponse = r2;
                called++;
                calledResponse.Return(DreamMessage.Ok(responseDoc));
            });
            XDoc doc = new XDoc("message").Elem("foo");
            DreamMessage response = Plug.New("http://www.mindtouch.com").At("foo").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            Assert.AreEqual(1, called);
            Assert.AreEqual("GET", calledVerb);
            Assert.AreEqual(responseDoc, response.ToDocument());
            Assert.AreEqual(calledUri, new XUri("http://www.mindtouch.com/foo"));
        }

        [Test]
        public void PostAsync_from_nested_async_workers() {
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            MockPlug.Register(new XUri("http://foo/bar"), delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                resetEvent.Set();
                r2.Return(DreamMessage.Ok());
            });
            Plug.New("http://foo/bar").PostAsync();
            Assert.IsTrue(resetEvent.WaitOne(1000, false), "no async failed");
            Async.Fork(() => Async.Fork(() => Plug.New("http://foo/bar").PostAsync(), new Result()), new Result());
            Assert.IsTrue(resetEvent.WaitOne(1000, false), "async failed");
            Async.Fork(() => Async.Fork(() => Plug.New("http://foo/bar").PostAsync(), new Result()), new Result());
            Assert.IsTrue(resetEvent.WaitOne(1000, false), "nested async failed");
            Async.Fork(() => Async.Fork(() => Async.Fork(() => Plug.New("http://foo/bar").PostAsync(), new Result()), new Result()), new Result());
            Assert.IsTrue(resetEvent.WaitOne(1000, false), "double async failed");
        }

        [Test]
        public void MockPlug_can_verify_call_via_VerifyAll() {
            MockPlug.Setup(new XUri("http://mock/foo")).ExpectCalls(Times.AtLeastOnce());
            Assert.IsTrue(Plug.New("http://mock/foo").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            MockPlug.VerifyAll();
        }

        [Test]
        public void Can_verify_call() {
            var mock = MockPlug.Setup(new XUri("http://mock/foo")).ExpectCalls(Times.AtLeastOnce());
            Assert.IsTrue(Plug.New("http://mock/foo").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            mock.Verify();
        }

        [Test]
        public void Can_verify_lack_of_call() {
            var mock = MockPlug.Setup(new XUri("http://mock/foo")).ExpectCalls(Times.Never());
            mock.Verify(TimeSpan.FromSeconds(3));
        }

        [Test]
        public void MockPlug_without_call_expectation_does_not_throw_on_Verify() {
            var mock = MockPlug.Setup(new XUri("http://mock/foo"));
            mock.Verify(TimeSpan.FromSeconds(3));
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_verb() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).Verb("POST");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).Verb("GET");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).Verb("DELETE");
            Assert.IsTrue(Plug.New("http://mock/foo").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Specific_verb_gets_picked_over_wildcard1() {
            var a = MockPlug.Setup(new XUri("http://mock/foo"));
            var b = MockPlug.Setup(new XUri("http://mock/foo")).Verb("GET");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).Verb("DELETE");
            Assert.IsTrue(Plug.New("http://mock/foo").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Specific_verb_gets_picked_over_wildcard2() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).Verb("DELETE");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).Verb("GET");
            var c = MockPlug.Setup(new XUri("http://mock/foo"));
            Assert.IsTrue(Plug.New("http://mock/foo").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_subpath() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).At("bar");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).At("eek");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).At("baz");
            Assert.IsTrue(Plug.New("http://mock/foo/eek").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_query() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", "a");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).With("eek", "b");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).With("baz", "c");
            Assert.IsTrue(Plug.New("http://mock/foo/").With("eek", "b").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_queryarg_values() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", "a");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", "b");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", "c");
            Assert.IsTrue(Plug.New("http://mock/foo/").With("bar", "b").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_queryarg_values_via_callback() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", x => x == "a");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", x => x == "b");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", x => x == "c");
            Assert.IsTrue(Plug.New("http://mock/foo/").With("bar", "b").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_most_specific_query_args() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", "a");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", "a").With("x", "1").With("y", "2");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", "a").With("x", "1");
            Assert.IsTrue(Plug.New("http://mock/foo/").With("bar", "a").With("x", "1").With("y", "2").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_most_less_specific_query_arg_with_value_match() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", "a");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", "a").With("x", "1");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", "a").With("x", "2").With("y", "2");
            Assert.IsTrue(Plug.New("http://mock/foo/").With("bar", "a").With("x", "1").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Extraneous_args_are_not_considered_in_matching() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", "a");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", "a").With("x", "1");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).With("bar", "a").With("x", "2").With("y", "2");
            Assert.IsTrue(Plug.New("http://mock/foo/").With("bar", "a").With("x", "1").With("y", "2").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_differentiate_multiple_plugs_and_their_call_counts() {
            var bar = MockPlug.Setup(new XUri("http://mock/foo")).At("bar").ExpectAtLeastOneCall();
            var eek = MockPlug.Setup(new XUri("http://mock/foo")).At("eek").With("a", "b").ExpectCalls(Times.Exactly(3));
            var baz = MockPlug.Setup(new XUri("http://mock/foo")).At("eek").With("b", "c").ExpectAtLeastOneCall();
            Assert.IsTrue(Plug.New("http://mock/foo/bar").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            Assert.IsTrue(Plug.New("http://mock/foo/bar").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            Assert.IsTrue(Plug.New("http://mock/foo/eek").With("a", "b").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            Assert.IsTrue(Plug.New("http://mock/foo/eek").With("a", "b").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            Assert.IsTrue(Plug.New("http://mock/foo/eek").With("a", "b").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            Assert.IsTrue(Plug.New("http://mock/foo/eek").With("b", "c").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            bar.Verify();
            baz.Verify();
            eek.Verify();
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_headers() {
            var bar = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a");
            var eek = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("eek", "b");
            var baz = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("baz", "c");
            Assert.IsTrue(Plug.New("http://mock/foo/").WithHeader("eek", "b").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            bar.Verify(2.Seconds(), Times.Never());
            baz.Verify(0.Seconds(), Times.Never());
            eek.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_header_values() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", "b");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", "c");
            Assert.IsTrue(Plug.New("http://mock/foo/").WithHeader("bar", "b").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_header_values_via_callback() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", x => x == "a");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", x => x == "b");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", x => x == "c");
            Assert.IsTrue(Plug.New("http://mock/foo/").WithHeader("bar", "b").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_most_specific_headers() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a").WithHeader("x", "1").WithHeader("y", "2");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a").WithHeader("x", "1");
            Assert.IsTrue(Plug.New("http://mock/foo/").WithHeader("bar", "a").WithHeader("x", "1").WithHeader("y", "2").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_most_less_specific_header_with_value_match() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a").WithHeader("x", "1");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a").WithHeader("x", "2").WithHeader("y", "2");
            Assert.IsTrue(Plug.New("http://mock/foo/").WithHeader("bar", "a").WithHeader("x", "1").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Extraneous_headers_are_not_considered_in_matching() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a");
            var b = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a").WithHeader("x", "1");
            var c = MockPlug.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a").WithHeader("x", "2").WithHeader("y", "2");
            Assert.IsTrue(Plug.New("http://mock/foo/").WithHeader("bar", "a").WithHeader("x", "1").WithHeader("y", "2").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }


        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_body() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).WithBody(new XDoc("doc").Elem("x", "a"));
            var b = MockPlug.Setup(new XUri("http://mock/foo")).WithBody(new XDoc("doc").Elem("x", "b"));
            var c = MockPlug.Setup(new XUri("http://mock/foo")).WithBody(new XDoc("doc").Elem("x", "c"));
            Assert.IsTrue(Plug.New("http://mock/foo/").Post(new XDoc("doc").Elem("x", "b"), new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_provide_callback_for_custom_body_check_and_pick_mockplug_based_on_its_return() {
            var a = MockPlug.Setup(new XUri("http://mock/foo")).WithMessage(msg => false);
            var b = MockPlug.Setup(new XUri("http://mock/foo")).WithMessage(msg => true);
            var c = MockPlug.Setup(new XUri("http://mock/foo")).WithMessage(msg => false);
            Assert.IsTrue(Plug.New("http://mock/foo/").Post(new XDoc("doc"), new Result<DreamMessage>()).Wait().IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_add_headers_to_the_response() {
            MockPlug.Setup(new XUri("http://mock/foo")).WithResponseHeader("foo", "bar");
            var msg = Plug.New("http://mock/foo").Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.IsSuccessful);
            Assert.AreEqual("bar", msg.Headers["foo"]);
        }

        [Test]
        public void Can_add_headers_to_the_response_after_specifying_message() {
            MockPlug.Setup(new XUri("http://mock/foo")).Returns(DreamMessage.NotModified()).WithResponseHeader("foo", "bar");
            var msg = Plug.New("http://mock/foo").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.NotModified, msg.Status);
            Assert.AreEqual("bar", msg.Headers["foo"]);
        }

        [Test]
        public void Can_add_headers_to_the_response_before_specifying_message() {
            MockPlug.Setup(new XUri("http://mock/foo")).WithResponseHeader("foo", "bar").Returns(DreamMessage.NotModified());
            var msg = Plug.New("http://mock/foo").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.NotModified, msg.Status);
            Assert.AreEqual("bar", msg.Headers["foo"]);
        }

        public void Can_return_XDoc() {
            var doc = new XDoc("doc").Elem("foo", StringUtil.CreateAlphaNumericKey(6));
            MockPlug.Setup(new XUri("http://mock/foo")).Returns(doc);
            var msg = Plug.New("http://mock/foo").Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.IsSuccessful);
            Assert.AreEqual(doc, msg.ToDocument());
        }

        [Test]
        public void Can_add_headers_to_the_response_after_specifying_document() {
            var doc = new XDoc("doc").Elem("foo", StringUtil.CreateAlphaNumericKey(6));
            MockPlug.Setup(new XUri("http://mock/foo")).Returns(doc).WithResponseHeader("foo", "bar");
            var msg = Plug.New("http://mock/foo").Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.IsSuccessful);
            Assert.AreEqual("bar", msg.Headers["foo"]);
            Assert.AreEqual(doc, msg.ToDocument());
        }

        [Test]
        public void Can_add_headers_to_the_response_before_specifying_document() {
            var doc = new XDoc("doc").Elem("foo", StringUtil.CreateAlphaNumericKey(6));
            MockPlug.Setup(new XUri("http://mock/foo")).WithResponseHeader("foo", "bar").Returns(doc);
            var msg = Plug.New("http://mock/foo").Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.IsSuccessful);
            Assert.AreEqual("bar", msg.Headers["foo"]);
            Assert.AreEqual(doc, msg.ToDocument());
        }

        [Test]
        public void Returns_callback_gets_request_data() {
            var doc = new XDoc("doc").Elem("foo", StringUtil.CreateAlphaNumericKey(6));
            var success = new XDoc("yay");
            var uri = new XUri("http://mock/foo/").With("foo", "baz");
            MockPlug.Setup(new XUri("http://mock/foo")).Returns(invocation => {
                if(invocation.Verb != "POST") {
                    return DreamMessage.BadRequest("wrong verb: " + invocation.Verb);
                }
                if(invocation.Uri != uri) {
                    return DreamMessage.BadRequest("wrong uri: " + invocation.Uri);
                }
                if(invocation.Request.Headers["header"] != "value") {
                    return DreamMessage.BadRequest("wrong header value");
                }
                if(invocation.Request.ToDocument() != doc) {
                    return DreamMessage.BadRequest("wrong body");
                }
                return DreamMessage.Ok(success);
            });
            var msg = Plug.New(uri).WithHeader("header", "value").Post(doc, new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.IsSuccessful, msg.ToDocument().ToPrettyString());
            Assert.AreEqual(success, msg.ToDocument());
        }

        [Test]
        public void Returns_callback_gets_response_headers_if_added_before_callback() {
            var success = new XDoc("yay");
            MockPlug.Setup(new XUri("http://mock/foo"))
                .WithResponseHeader("foo", "bar")
                .Returns(invocation => {
                    return invocation.ResponseHeaders["foo"] != "bar" ? DreamMessage.BadRequest("wrong response header") : DreamMessage.Ok(success);
                });
            var msg = Plug.New("http://mock/foo/").Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.IsSuccessful, msg.ToDocument().ToPrettyString());
            Assert.AreEqual(success, msg.ToDocument());
        }

        [Test]
        public void Returns_callback_gets_response_headers_if_added_after_callback() {
            var success = new XDoc("yay");
            MockPlug.Setup(new XUri("http://mock/foo"))
                .Returns(invocation => {
                    return invocation.ResponseHeaders["foo"] != "bar" ? DreamMessage.BadRequest("wrong response header") : DreamMessage.Ok(success);
                })
                .WithResponseHeader("foo", "bar");
            var msg = Plug.New("http://mock/foo/").Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.IsSuccessful, msg.ToDocument().ToPrettyString());
            Assert.AreEqual(success, msg.ToDocument());
        }

        [Test]
        public void Can_mock_a_request_with_a_stream_request() {
            var tmp = Path.GetTempFileName();
            var payload = "blahblah";
            File.WriteAllText(tmp, payload);
            var message = DreamMessage.FromFile(tmp);
            var uri = new XUri("http://mock/post/stream");
            MockPlug.Setup(uri).Verb("POST")
                .WithMessage(m => m.ToText() == payload)
                .ExpectAtLeastOneCall();
            var response = Plug.New(uri).Post(message, new Result<DreamMessage>()).Wait();
            response.AssertSuccess();
            MockPlug.VerifyAll(1.Seconds());
        }
    }
}
