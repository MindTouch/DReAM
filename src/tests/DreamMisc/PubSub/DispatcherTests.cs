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
using System.Threading;
using MindTouch.Dream.Services.PubSub;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test.PubSub {

    [TestFixture]
    public class DispatcherTests {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        [Test]
        public void Dispatcher_creates_set_at_location() {
            var cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            var owner = Plug.New("mock:///pubsub");
            var dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie });
            var subset = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner")
                .Start("subscription")
                    .Attr("id", "123")
                    .Elem("channel", "channel:///foo")
                    .Start("recipient").Attr("auth-token", "abc").Elem("uri", "mailto://foo@bar.com").End()
                .End();
            var set = dispatcher.RegisterSet("abc", subset, "def");
            Assert.AreEqual("abc", set.Item1.Location);
            Assert.AreEqual("def", set.Item1.AccessKey);
            Assert.IsFalse(set.Item2);
            PubSubSubscriptionSet fetchedSet = dispatcher[set.Item1.Location];
            Assert.AreEqual(subset, fetchedSet.AsDocument());
            Tuplet<PubSubSubscriptionSet, bool> reset = dispatcher.RegisterSet("abc", subset, "def");
            Assert.IsTrue(reset.Item2);
            Assert.AreEqual(set.Item1.Location, reset.Item1.Location);
        }

        [Test]
        public void Second_register_for_same_owner_ignores_location_and_accessKey() {
            var cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            var owner = Plug.New("mock:///pubsub");
            var dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie });
            var subset = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner")
                .Start("subscription")
                    .Attr("id", "123")
                    .Elem("channel", "channel:///foo")
                    .Start("recipient").Attr("auth-token", "abc").Elem("uri", "mailto://foo@bar.com").End()
                .End();
            var set = dispatcher.RegisterSet("abc", subset, "def");
            Assert.AreEqual("abc", set.Item1.Location);
            Assert.AreEqual("def", set.Item1.AccessKey);
            Assert.IsFalse(set.Item2);
            PubSubSubscriptionSet fetchedSet = dispatcher[set.Item1.Location];
            Assert.AreEqual(subset, fetchedSet.AsDocument());
            Tuplet<PubSubSubscriptionSet, bool> reset = dispatcher.RegisterSet("sdfsfsd", subset, "werewrwe");
            Assert.IsTrue(reset.Item2);
            Assert.AreEqual(set.Item1.Location, reset.Item1.Location);
            Assert.AreEqual(set.Item1.AccessKey, reset.Item1.AccessKey);
        }

        [Test]
        public void Dispatcher_replaceset_for_wrong_owner_throws() {
            Plug owner = Plug.New("mock:///pubsub");
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            Dispatcher dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie });
            XDoc subset = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner")
                .Start("subscription")
                .Attr("id", "123")
                .Elem("channel", "channel:///foo")
                .Start("recipient").Attr("auth-token", "abc").Elem("uri", "mailto://foo@bar.com").End()
                .End();
            Tuplet<PubSubSubscriptionSet, bool> location = dispatcher.RegisterSet("abc", subset, "def");
            XDoc subset2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///ownerx")
                .Start("subscription")
                .Attr("id", "123")
                .Elem("channel", "channel:///foo")
                .Start("recipient").Attr("auth-token", "abc").Elem("uri", "mailto://foo@bar.com").End()
                .End();
            try {
                dispatcher.ReplaceSet(location.Item1.Location, subset2, null);
            } catch(ArgumentException) {
                return;
            }
            Assert.Fail();
        }

        [Test]
        public void Dispatcher_replaceset_for_unknown_location_returns_false() {
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            Plug owner = Plug.New("mock:///pubsub");
            Dispatcher dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie });
            XDoc subset = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner")
                .Start("subscription")
                .Attr("id", "123")
                .Elem("channel", "channel:///foo")
                .Start("recipient").Attr("auth-token", "abc").Elem("uri", "mailto://foo@bar.com").End()
                .End();
            Assert.IsNull(dispatcher.ReplaceSet("ABCD", subset, null));

        }

        [Test]
        public void Dispatcher_removeset_returns_false_on_missing_set() {
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            Plug owner = Plug.New("mock:///pubsub");
            Dispatcher dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie });
            XDoc subset = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner")
                .Start("subscription")
                .Attr("id", "123")
                .Elem("channel", "channel:///foo")
                .Start("recipient").Attr("auth-token", "abc").Elem("uri", "mailto://foo@bar.com").End()
                .End();
            Tuplet<PubSubSubscriptionSet, bool> location = dispatcher.RegisterSet("abc", subset, "def");
            Assert.IsFalse(location.Item2);
            Assert.IsNotNull(dispatcher[location.Item1.Location]);
            Assert.IsTrue(dispatcher.RemoveSet(location.Item1.Location));
            Assert.IsNull(dispatcher[location.Item1.Location]);
            Assert.IsFalse(dispatcher.RemoveSet(location.Item1.Location));
        }

        [Test]
        public void Dispatch_based_on_channel_match() {
            DispatcherEvent ev = new DispatcherEvent(
                new XDoc("msg"),
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            Dictionary<XUri, DreamMessage> dispatches = new Dictionary<XUri, DreamMessage>();
            XUri testUri = new XUri("http:///").At(StringUtil.CreateAlphaNumericKey(4));
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            int dispatchCounter = 0;
            int expectedDispatches = 0;
            MockPlug.Register(testUri, delegate(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
                dispatches.Add(plug.Uri, request);
                dispatchCounter++;
                // ReSharper disable AccessToModifiedClosure
                if(dispatchCounter >= expectedDispatches) {
                    // ReSharper restore AccessToModifiedClosure
                    resetEvent.Set();
                }
                response.Return(DreamMessage.Ok());
            });
            Plug owner = Plug.New("mock:///pubsub");
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            Dispatcher dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie });
            int expectedCombinedSetUpdates = 2;
            int combinedSetUpdates = 0;
            AutoResetEvent setResetEvent = new AutoResetEvent(false);
            dispatcher.CombinedSetUpdated += delegate {
                combinedSetUpdates++;
                _log.DebugFormat("combinedset updated ({0})", combinedSetUpdates);
                if(combinedSetUpdates >= expectedCombinedSetUpdates) {
                    setResetEvent.Set();
                }
            };
            dispatcher.RegisterSet("abc",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", testUri.At("sub1")).End()
                    .End()
                    .Start("subscription")
                    .Attr("id", "2")
                    .Elem("channel", "channel:///foo/baz/*")
                    .Start("recipient").Elem("uri", testUri.At("sub2")).End()
                    .End(), "def");
            dispatcher.RegisterSet("qwe",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner2")
                    .Start("subscription")
                    .Attr("id", "3")
                    .Elem("channel", "channel:///foo/bar")
                    .Start("recipient").Elem("uri", testUri.At("sub3")).End()
                    .End()
                    .Start("subscription")
                    .Attr("id", "4")
                    .Elem("channel", "channel:///foo/bar/*")
                    .Start("recipient").Elem("uri", testUri.At("sub4")).End()
                    .End(), "asd");

            // combinedset updates happen asynchronously, so give'em a chance
            Assert.IsTrue(setResetEvent.WaitOne(10000, false));
            expectedDispatches = 3;
            dispatcher.Dispatch(ev);

            // dispatch happens async on a worker thread
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Assert.AreEqual(3, dispatches.Count);
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub1")));
            Assert.AreEqual(ev.AsMessage().ToDocument(), dispatches[testUri.At("sub1")].ToDocument());
            Assert.AreEqual(ev.Id, dispatches[testUri.At("sub1")].Headers.DreamEventId);
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub3")));
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub4")));
            MockPlug.Deregister(testUri);
        }

        [Test]
        public void Dispatch_based_on_channel_match_with_different_wikiid_patterns() {
            DispatcherEvent ev = new DispatcherEvent(
                new XDoc("msg"),
                new XUri("event://sales.mindtouch.com/deki/comments/create"),
                new XUri("http://foobar.com/some/comment"));
            Dictionary<XUri, DreamMessage> dispatches = new Dictionary<XUri, DreamMessage>();
            XUri testUri = new XUri("http://sales.mindtouch.com/").At(StringUtil.CreateAlphaNumericKey(4));
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            int dispatchCounter = 0;
            int expectedDispatches = 0;
            MockPlug.Register(testUri, delegate(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
                dispatches.Add(plug.Uri, request);
                dispatchCounter++;
                // ReSharper disable AccessToModifiedClosure
                if(dispatchCounter >= expectedDispatches) {
                    // ReSharper restore AccessToModifiedClosure
                    resetEvent.Set();
                }
                response.Return(DreamMessage.Ok());
            });
            Plug owner = Plug.New("mock:///pubsub");
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            Dispatcher dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie });
            int expectedCombinedSetUpdates = 2;
            int combinedSetUpdates = 0;
            AutoResetEvent setResetEvent = new AutoResetEvent(false);
            dispatcher.CombinedSetUpdated += delegate {
                combinedSetUpdates++;
                _log.DebugFormat("combinedset updated ({0})", combinedSetUpdates);
                if(combinedSetUpdates >= expectedCombinedSetUpdates) {
                    setResetEvent.Set();
                }
            };
            dispatcher.RegisterSet("abc",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "event://sales.mindtouch.com/deki/comments/create")
                    .Elem("channel", "event://sales.mindtouch.com/deki/comments/update")
                    .Start("recipient").Elem("uri", testUri.At("sub1")).End()
                    .End(), "def");
            dispatcher.RegisterSet("qwe",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner2")
                    .Start("subscription")
                    .Attr("id", "3")
                    .Elem("channel", "event://*/deki/comments/create")
                    .Elem("channel", "event://*/deki/comments/update")
                    .Start("recipient").Elem("uri", testUri.At("sub2")).End()
                    .End(), "asd");

            // combinedset updates happen asynchronously, so give'em a chance
            Assert.IsTrue(setResetEvent.WaitOne(10000, false));
            expectedDispatches = 2;
            dispatcher.Dispatch(ev);

            // dispatch happens async on a worker thread
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Assert.AreEqual(2, dispatches.Count);
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub1")));
            Assert.AreEqual(ev.AsMessage().ToDocument(), dispatches[testUri.At("sub1")].ToDocument());
            Assert.AreEqual(ev.Id, dispatches[testUri.At("sub1")].Headers.DreamEventId);
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub2")));
            MockPlug.Deregister(testUri);
        }

        [Test]
        public void Dispatch_based_on_channel_match_with_different_wikiid_patterns_but_same_proxy_destination() {
            DispatcherEvent ev = new DispatcherEvent(
                new XDoc("msg"),
                new XUri("event://sales.mindtouch.com/deki/comments/create"),
                new XUri("http://foobar.com/some/comment"));
            List<DreamMessage> dispatches = new List<DreamMessage>();
            XUri testUri = new XUri("http://sales.mindtouch.com/").At(StringUtil.CreateAlphaNumericKey(4));
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            int dispatchCounter = 0;
            int expectedDispatches = 0;
            MockPlug.Register(testUri, delegate(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
                if(testUri == plug.Uri) {
                    dispatches.Add(request);
                    dispatchCounter++;
                    // ReSharper disable AccessToModifiedClosure
                    if(dispatchCounter >= expectedDispatches) {
                        // ReSharper restore AccessToModifiedClosure
                        resetEvent.Set();
                    }
                }
                response.Return(DreamMessage.Ok());
            });
            Plug owner = Plug.New("mock:///pubsub");
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            Dispatcher dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie });
            int expectedCombinedSetUpdates = 2;
            int combinedSetUpdates = 0;
            AutoResetEvent setResetEvent = new AutoResetEvent(false);
            dispatcher.CombinedSetUpdated += delegate {
                combinedSetUpdates++;
                _log.DebugFormat("combinedset updated ({0})", combinedSetUpdates);
                if(combinedSetUpdates >= expectedCombinedSetUpdates) {
                    setResetEvent.Set();
                }
            };
            dispatcher.RegisterSet("abc",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "event://sales.mindtouch.com/deki/comments/create")
                    .Elem("channel", "event://sales.mindtouch.com/deki/comments/update")
                    .Elem("uri.proxy", testUri)
                    .Start("recipient").Elem("uri", testUri.At("sub1")).End()
                    .End(), "def");
            dispatcher.RegisterSet("qwe",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner2")
                    .Start("subscription")
                    .Attr("id", "3")
                    .Elem("channel", "event://*/deki/comments/create")
                    .Elem("channel", "event://*/deki/comments/update")
                    .Elem("uri.proxy", testUri)
                    .Start("recipient").Elem("uri", testUri.At("sub2")).End()
                    .End(), "asd");

            // combinedset updates happen asynchronously, so give'em a chance
            Assert.IsTrue(setResetEvent.WaitOne(10000, false));
            expectedDispatches = 1;
            dispatcher.Dispatch(ev);

            // dispatch happens async on a worker thread
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Assert.AreEqual(1, dispatches.Count);
            Assert.AreEqual(ev.AsMessage().ToDocument(), dispatches[0].ToDocument());
            Assert.AreEqual(ev.Id, dispatches[0].Headers.DreamEventId);
            MockPlug.Deregister(testUri);
        }

        [Test]
        public void Dispatch_based_on_channel_and_resource_match() {
            DispatcherEvent ev = new DispatcherEvent(
                new XDoc("msg"),
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            Dictionary<XUri, DreamMessage> dispatches = new Dictionary<XUri, DreamMessage>();
            XUri testUri = new XUri("http:///").At(StringUtil.CreateAlphaNumericKey(4));
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            int expectedDispatches = 0;
            MockPlug.Register(testUri, delegate(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
                dispatches.Add(plug.Uri, request);
                // ReSharper disable AccessToModifiedClosure
                if(dispatches.Count >= expectedDispatches) {
                    // ReSharper restore AccessToModifiedClosure
                    resetEvent.Set();
                }
                response.Return(DreamMessage.Ok());
            });
            Plug owner = Plug.New("mock:///pubsub");
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            Dispatcher dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie });
            int expectedCombinedSetUpdates = 2;
            int combinedSetUpdates = 0;
            AutoResetEvent setResetEvent = new AutoResetEvent(false);
            dispatcher.CombinedSetUpdated += delegate {
                combinedSetUpdates++;
                _log.DebugFormat("combinedset updated ({0})", combinedSetUpdates);
                if(combinedSetUpdates >= expectedCombinedSetUpdates) {
                    setResetEvent.Set();
                }
            };
            dispatcher.RegisterSet("abc",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Elem("uri.resource", "http://*/some/*")
                    .Start("recipient").Elem("uri", testUri.At("sub1")).End()
                    .End()
                    .Start("subscription")
                    .Attr("id", "2")
                    .Elem("channel", "channel:///foo/baz")
                    .Elem("uri.resource", "http://*/some/*")
                    .Start("recipient").Elem("uri", testUri.At("sub2")).End()
                    .End(), "def");
            dispatcher.RegisterSet("asd",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner2")
                    .Start("subscription")
                    .Attr("id", "3")
                    .Elem("channel", "channel:///foo/bar")
                    .Elem("uri.resource", "http://foobar.com/some/page")
                    .Start("recipient").Elem("uri", testUri.At("sub3")).End()
                    .End()
                    .Start("subscription")
                    .Attr("id", "4")
                    .Elem("channel", "channel:///foo/bar")
                    .Elem("uri.resource", "http://baz.com/some/*")
                    .Start("recipient").Elem("uri", testUri.At("sub4")).End()
                    .End(), "qwe");

            // combinedset updates happen asynchronously, so give'em a chance
            Assert.IsTrue(setResetEvent.WaitOne(10000, false));
            expectedDispatches = 2;
            dispatcher.Dispatch(ev);

            // dispatch happens async on a worker thread
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Thread.Sleep(200);
            Assert.AreEqual(2, dispatches.Count);
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub1")));
            Assert.AreEqual(ev.AsMessage().ToDocument(), dispatches[testUri.At("sub1")].ToDocument());
            Assert.AreEqual(ev.Id, dispatches[testUri.At("sub1")].Headers.DreamEventId);
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub3")));
            MockPlug.Deregister(testUri);
        }

        [Test]
        public void Dispatch_with_owners_via_throws() {
            XUri loopService = new XUri("local:///infinite/loop-dispatcher");
            DispatcherEvent ev = new DispatcherEvent(new XDoc("foo"), new XUri("channel:///foo"), new XUri("http:///foo"))
                .WithVia(loopService)
                .WithVia(new XUri("local://12345/a"));
            Plug owner = Plug.New(loopService);
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            Dispatcher dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie });
            try {
                dispatcher.Dispatch(ev);
                Assert.Fail("should not have gotten here");
            } catch(DreamBadRequestException) {
                return;
            }
            Assert.Fail("should not have gotten here");
        }

        [Test]
        public void Repeated_dispatch_failure_kicks_subscription_set() {
            var sub1Uri = new XUri("http://sub1/foo");
            var sub1Mock = MockPlug.Register(sub1Uri);
            sub1Mock.Expect().Verb("POST").Response(DreamMessage.BadRequest("nobody home"));
            var sub2Uri = new XUri("http://sub2/foo");
            var sub2Mock = MockPlug.Register(sub2Uri);
            sub2Mock.Expect().Verb("POST").Response(DreamMessage.BadRequest("nobody home"));
            var ev = new DispatcherEvent(
                new XDoc("msg"),
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            var cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            var dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = Plug.New("mock:///pubsub"), ServiceAccessCookie = cookie });
            var expectedCombinedSetUpdates = 2;
            var combinedSetUpdates = 0;
            var setResetEvent = new ManualResetEvent(false);
            dispatcher.CombinedSetUpdated += delegate {
                combinedSetUpdates++;
                _log.DebugFormat("combinedset updated ({0})", combinedSetUpdates);
                if(combinedSetUpdates >= expectedCombinedSetUpdates) {
                    setResetEvent.Set();
                }
            };
            var location1 = dispatcher.RegisterSet("abc",
                new XDoc("subscription-set")
                    .Attr("max-failures", 0)
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", sub1Uri).End()
                    .End(), "def").Item1.Location;
            var location2 = dispatcher.RegisterSet("qwe",
                new XDoc("subscription-set")
                    .Attr("max-failures", 0)
                    .Elem("uri.owner", "http:///owner2")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", sub2Uri).End()
                    .End(), "asd").Item1.Location;
            Assert.IsTrue(setResetEvent.WaitOne(10000, false), "combined set didn't change expected number of times");
            Assert.IsNotNull(dispatcher[location1]);
            Assert.IsNotNull(dispatcher[location2]);
            dispatcher.Dispatch(ev);
            Assert.IsTrue(sub1Mock.WaitAndVerify(TimeSpan.FromSeconds(10)), sub1Mock.VerificationFailure);
            Assert.IsTrue(sub2Mock.WaitAndVerify(TimeSpan.FromSeconds(10)), sub1Mock.VerificationFailure);
            Assert.IsTrue(Wait.For(() => dispatcher[location2] == null, TimeSpan.FromSeconds(10)), "Second set wasn't kicked");
            Assert.IsTrue(Wait.For(() => dispatcher[location1] == null, TimeSpan.FromSeconds(10)), "First set wasn't kicked");
        }

        [Test]
        public void Failed_dispatch_followed_by_success_should_reset_fail_count() {
            bool fail = true;
            DispatcherEvent ev = new DispatcherEvent(
             new XDoc("msg"),
             new XUri("channel:///foo/bar"),
             new XUri("http://foobar.com/some/page"));
            XUri testUri = new XUri("http:///").At(StringUtil.CreateAlphaNumericKey(4));
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            int mockCalled = 0;
            MockPlug.Register(testUri, delegate(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
                mockCalled++;
                // ReSharper disable AccessToModifiedClosure
                _log.DebugFormat("mock called {0} times (fail={1}): {2}", mockCalled, fail, uri);
                resetEvent.Set();
                response.Return(fail ? DreamMessage.InternalError() : DreamMessage.Ok());
                // ReSharper restore AccessToModifiedClosure
            });
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            Dispatcher dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = Plug.New("mock:///pubsub"), ServiceAccessCookie = cookie });
            int expectedCombinedSetUpdates = 1;
            int combinedSetUpdates = 0;
            AutoResetEvent setResetEvent = new AutoResetEvent(false);
            dispatcher.CombinedSetUpdated += delegate {
                combinedSetUpdates++;
                _log.DebugFormat("combinedset updated ({0})", combinedSetUpdates);
                if(combinedSetUpdates >= expectedCombinedSetUpdates) {
                    setResetEvent.Set();
                }
            };
            var location = dispatcher.RegisterSet(
                "abc",
                new XDoc("subscription-set")
                    .Attr("max-failures", 1)
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", testUri.At("foo")).End()
                    .End(), 
                "def"
                ).Item1.Location;
            Assert.IsTrue(setResetEvent.WaitOne(10000, false));
            Assert.IsNotNull(dispatcher[location]);

            _log.DebugFormat("first dispatch (fail={0})", fail);
            dispatcher.Dispatch(ev);
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Thread.Sleep(1000); // failure gets dealt with async
            Assert.IsNotNull(dispatcher[location]);
            fail = false;

            _log.DebugFormat("second dispatch (fail={0})", fail);
            dispatcher.Dispatch(ev);
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Thread.Sleep(1000); // failure reset gets dealt with async
            Assert.IsNotNull(dispatcher[location]);
            fail = true;

            _log.DebugFormat("third dispatch (fail={0})", fail);
            dispatcher.Dispatch(ev);
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Thread.Sleep(1000); // failure gets dealt with async
            Assert.IsNotNull(dispatcher[location]);

            _log.DebugFormat("fourth dispatch (fail={0})", fail);
            dispatcher.Dispatch(ev);
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Thread.Sleep(1000); // failure gets dealt with async
            Assert.IsNull(dispatcher[location]);
            MockPlug.Deregister(testUri);
        }

        [Test]
        public void Dispatch_based_on_recipients() {
            var proxyRecipient1 = "mailto:///userA@foo.com";
            var proxyRecipient2 = "mailto:///userC@foo.com";
            var msg = new XDoc("foo");
            var ev = new DispatcherEvent(
                    msg,
                    new XUri("channel:///foo/bar"),
                    new XUri("http://foobar.com/some/page")
                )
                .WithRecipient(false,
                    new DispatcherRecipient(new XUri(proxyRecipient1)),
                    new DispatcherRecipient(new XUri("mailto:///userB@foo.com")),
                    new DispatcherRecipient(new XUri(proxyRecipient2)
                )
            );
            var dispatches = new Dictionary<XUri, DreamMessage>();
            var testUri = new XUri("http:///").At(StringUtil.CreateAlphaNumericKey(4));
            var dispatchHappened = new AutoResetEvent(false);
            MockPlug.Register(testUri, delegate(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
                dispatches.Add(plug.Uri, request);
                dispatchHappened.Set();
                response.Return(DreamMessage.Ok());
            });
            var owner = Plug.New("mock:///pubsub");
            var cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            var dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie });
            var combinedSetUpdated = new AutoResetEvent(false);
            dispatcher.CombinedSetUpdated += delegate {
                _log.DebugFormat("set updated");
                combinedSetUpdated.Set();
            };

            var proxy = testUri.At("proxy");
            _log.DebugFormat("registering set");
            dispatcher.RegisterSet(
                "abc",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                        .Attr("id", "1")
                        .Elem("channel", "channel:///foo/*")
                        .Elem("uri.proxy", proxy)
                        .Start("recipient").Elem("uri", proxyRecipient1).End()
                        .Start("recipient").Elem("uri", proxyRecipient2).End()
                    .End()
                    .Start("subscription")
                        .Attr("id", "2")
                        .Elem("channel", "channel:///foo/*")
                        .Start("recipient").Elem("uri", testUri.At("sub2")).End()
                    .End(), 
                "def"
            );

            //Set updates happen asynchronously, so give it a chance
            _log.DebugFormat("giving registration a chance to manifest");
            Assert.IsTrue(combinedSetUpdated.WaitOne(10000, false));
            _log.DebugFormat("dispatching event");
            dispatcher.Dispatch(ev);

            // dispatch happens async on a worker thread
            Assert.IsTrue(dispatchHappened.WaitOne(1000, false));
            Thread.Sleep(200);
            Assert.AreEqual(1, dispatches.Count);
            Assert.IsTrue(dispatches.ContainsKey(proxy));
            Assert.AreEqual(msg, dispatches[proxy].ToDocument());
            Assert.AreEqual(ev.Id, dispatches[proxy].Headers.DreamEventId);
            string[] recipients = dispatches[proxy].Headers.DreamEventRecipients;
            Assert.AreEqual(2, recipients.Length);
            Assert.Contains(proxyRecipient1, recipients);
            Assert.Contains(proxyRecipient2, recipients);
            MockPlug.Deregister(testUri);
        }

        [Test]
        public void Dispatch_will_send_https_resources_to_subscriptions_without_resource() {
            DispatcherEvent ev = new DispatcherEvent(
                new XDoc("msg"),
                new XUri("channel:///foo/bar"),
                new XUri("https://foobar.com/some/page"));
            XUri testUri = new XUri("http:///").At(StringUtil.CreateAlphaNumericKey(4));
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            MockPlug.Register(testUri, delegate(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
                resetEvent.Set();
                response.Return(DreamMessage.Ok());
            });
            Plug owner = Plug.New("mock:///pubsub");
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            Dispatcher dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie });
            AutoResetEvent setResetEvent = new AutoResetEvent(false);
            dispatcher.CombinedSetUpdated += delegate {
                setResetEvent.Set();
            };
            dispatcher.RegisterSet("abc",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", testUri).End()
                    .End(), "def");

            // combinedset updates happen asynchronously, so give'em a chance
            Assert.IsTrue(setResetEvent.WaitOne(10000, false));
            dispatcher.Dispatch(ev);

            // dispatch happens async on a worker thread
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            MockPlug.Deregister(testUri);
        }
    }
}