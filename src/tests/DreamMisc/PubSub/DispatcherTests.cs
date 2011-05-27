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
using System.Linq;
using System.Threading;
using MindTouch.Dream.Services.PubSub;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;
using NUnit.Framework;
using Moq;
using MindTouch.Extensions.Time;

namespace MindTouch.Dream.Test.PubSub {

    [TestFixture]
    public class DispatcherTests {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private Dispatcher _dispatcher;
        private Mock<IPubSubDispatchQueueRepository> _queueRepositoryMock;
        private Func<DispatchItem, Result<bool>> _dequeueHandler;

        [SetUp]
        public void Setup() {
            MockPlug.DeregisterAll();
            var cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            var owner = Plug.New("mock:///pubsub");
            _queueRepositoryMock = new Mock<IPubSubDispatchQueueRepository>();
            _queueRepositoryMock.Setup(x => x.InitializeRepository(It.IsAny<Func<DispatchItem, Result<bool>>>()))
                .Callback((Func<DispatchItem, Result<bool>> handler) => _dequeueHandler = handler);
            _queueRepositoryMock.Setup(x => x.GetUninitializedSets())
                .Returns(new PubSubSubscriptionSet[0]);
            _dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie }, _queueRepositoryMock.Object);

        }

        [Test]
        public void Dispatcher_creates_set_at_location() {
            var subset = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner")
                .Start("subscription")
                    .Attr("id", "123")
                    .Elem("channel", "channel:///foo")
                    .Start("recipient").Attr("auth-token", "abc").Elem("uri", "mailto://foo@bar.com").End()
                .End();
            var set = _dispatcher.RegisterSet("abc", subset, "def");
            Assert.AreEqual("abc", set.Item1.Location);
            Assert.AreEqual("def", set.Item1.AccessKey);
            Assert.IsFalse(set.Item2);
            PubSubSubscriptionSet fetchedSet = _dispatcher[set.Item1.Location];
            Assert.AreEqual(subset, fetchedSet.AsDocument());
            Tuplet<PubSubSubscriptionSet, bool> reset = _dispatcher.RegisterSet("abc", subset, "def");
            Assert.IsTrue(reset.Item2);
            Assert.AreEqual(set.Item1.Location, reset.Item1.Location);
        }

        [Test]
        public void Second_register_for_same_owner_ignores_location_and_accessKey() {
            var subset = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner")
                .Start("subscription")
                    .Attr("id", "123")
                    .Elem("channel", "channel:///foo")
                    .Start("recipient").Attr("auth-token", "abc").Elem("uri", "mailto://foo@bar.com").End()
                .End();
            var set = _dispatcher.RegisterSet("abc", subset, "def");
            Assert.AreEqual("abc", set.Item1.Location);
            Assert.AreEqual("def", set.Item1.AccessKey);
            Assert.IsFalse(set.Item2);
            PubSubSubscriptionSet fetchedSet = _dispatcher[set.Item1.Location];
            Assert.AreEqual(subset, fetchedSet.AsDocument());
            Tuplet<PubSubSubscriptionSet, bool> reset = _dispatcher.RegisterSet("sdfsfsd", subset, "werewrwe");
            Assert.IsTrue(reset.Item2);
            Assert.AreEqual(set.Item1.Location, reset.Item1.Location);
            Assert.AreEqual(set.Item1.AccessKey, reset.Item1.AccessKey);
        }

        [Test]
        public void Dispatcher_replaceset_for_wrong_owner_throws() {
            XDoc subset = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner")
                .Start("subscription")
                .Attr("id", "123")
                .Elem("channel", "channel:///foo")
                .Start("recipient").Attr("auth-token", "abc").Elem("uri", "mailto://foo@bar.com").End()
                .End();
            Tuplet<PubSubSubscriptionSet, bool> location = _dispatcher.RegisterSet("abc", subset, "def");
            XDoc subset2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///ownerx")
                .Start("subscription")
                .Attr("id", "123")
                .Elem("channel", "channel:///foo")
                .Start("recipient").Attr("auth-token", "abc").Elem("uri", "mailto://foo@bar.com").End()
                .End();
            try {
                _dispatcher.ReplaceSet(location.Item1.Location, subset2, null);
            } catch(ArgumentException) {
                return;
            }
            Assert.Fail();
        }

        [Test]
        public void Dispatcher_replaceset_for_unknown_location_returns_false() {
            XDoc subset = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner")
                .Start("subscription")
                .Attr("id", "123")
                .Elem("channel", "channel:///foo")
                .Start("recipient").Attr("auth-token", "abc").Elem("uri", "mailto://foo@bar.com").End()
                .End();
            Assert.IsNull(_dispatcher.ReplaceSet("ABCD", subset, null));

        }

        [Test]
        public void Dispatcher_removeset_returns_false_on_missing_set() {
            XDoc subset = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner")
                .Start("subscription")
                .Attr("id", "123")
                .Elem("channel", "channel:///foo")
                .Start("recipient").Attr("auth-token", "abc").Elem("uri", "mailto://foo@bar.com").End()
                .End();
            Tuplet<PubSubSubscriptionSet, bool> location = _dispatcher.RegisterSet("abc", subset, "def");
            Assert.IsFalse(location.Item2);
            Assert.IsNotNull(_dispatcher[location.Item1.Location]);
            Assert.IsTrue(_dispatcher.RemoveSet(location.Item1.Location));
            Assert.IsNull(_dispatcher[location.Item1.Location]);
            Assert.IsFalse(_dispatcher.RemoveSet(location.Item1.Location));
        }

        [Test]
        public void Dispatch_of_event_without_recipients_gets_matched_subscription_recipients_attached() {
            var ev = new DispatcherEvent(
                new XDoc("msg"),
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            var dispatches = new Dictionary<XUri, DispatcherEvent>();
            var testUri = new XUri("http:///").At(StringUtil.CreateAlphaNumericKey(4));
            MockPlug.Register(testUri, delegate(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
                dispatches.Add(plug.Uri, new DispatcherEvent(request));
                response.Return(DreamMessage.Ok());
            });
            var combinedSetUpdates = 0;
            _dispatcher.CombinedSetUpdated += delegate {
                combinedSetUpdates++;
                _log.DebugFormat("combinedset updated ({0})", combinedSetUpdates);
            };
            var r1 = testUri.At("sub1");
            var r2 = testUri.At("sub2");
            _dispatcher.RegisterSet("abc",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", r1).End()
                    .End()
                    .Start("subscription")
                    .Attr("id", "2")
                    .Elem("channel", "channel:///foo/baz/*")
                    .Start("recipient").Elem("uri", r2).End()
                    .End(), "def");
            var r3 = testUri.At("sub3");
            var r4 = testUri.At("sub4");
            _dispatcher.RegisterSet("qwe",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner2")
                    .Start("subscription")
                    .Attr("id", "3")
                    .Elem("channel", "channel:///foo/bar")
                    .Start("recipient").Elem("uri", r3).End()
                    .End()
                    .Start("subscription")
                    .Attr("id", "4")
                    .Elem("channel", "channel:///foo/bar/*")
                    .Start("recipient").Elem("uri", r4).End()
                    .End(), "asd");

            // combinedset updates happen asynchronously, so give'em a chance
            const int expectedCombinedSetUpdates = 2;
            Assert.IsTrue(
                Wait.For(() => combinedSetUpdates >= expectedCombinedSetUpdates, 10.Seconds()),
                string.Format("expected at least {0} combined set updates, gave up after {1}", expectedCombinedSetUpdates, combinedSetUpdates)
            );
            _dispatcher.Dispatch(ev);
            const int expectedDispatches = 3;

            // dispatch happens async on a worker thread
            Assert.IsTrue(
                Wait.For(() => {

                    // Doing extra sleeping to improve the chance of catching excess dispatches
                    Thread.Sleep(100);
                    return dispatches.Count == expectedDispatches;
                }, 10.Seconds()),
                string.Format("expected at exactly {0} dispatches, gave up after {1}", expectedDispatches, dispatches.Count)
            );
            Assert.IsTrue(dispatches.ContainsKey(r1), "did not receive an event for sub 1");
            var sub1Event = dispatches[r1];
            Assert.AreEqual(1, sub1Event.Recipients.Length, "wrong number of recipient for sub 1");
            Assert.AreEqual(r1, sub1Event.Recipients[0].Uri, "wrong recipient for sub 1");
            Assert.IsTrue(dispatches.ContainsKey(r3), "did not receive an event for sub 3");
            var sub3Event = dispatches[r3];
            Assert.AreEqual(1, sub3Event.Recipients.Length, "wrong number of recipient for sub 3");
            Assert.AreEqual(r3, sub3Event.Recipients[0].Uri, "wrong recipient for sub 3");
            Assert.IsTrue(dispatches.ContainsKey(r4), "did not receive an event for sub 4");
            var sub4Event = dispatches[r4];
            Assert.AreEqual(1, sub4Event.Recipients.Length, "wrong number of recipient for sub 4");
            Assert.AreEqual(r4, sub4Event.Recipients[0].Uri, "wrong recipient for sub 4");
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
            int expectedCombinedSetUpdates = 2;
            int combinedSetUpdates = 0;
            AutoResetEvent setResetEvent = new AutoResetEvent(false);
            _dispatcher.CombinedSetUpdated += delegate {
                combinedSetUpdates++;
                _log.DebugFormat("combinedset updated ({0})", combinedSetUpdates);
                if(combinedSetUpdates >= expectedCombinedSetUpdates) {
                    setResetEvent.Set();
                }
            };
            _dispatcher.RegisterSet("abc",
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
            _dispatcher.RegisterSet("qwe",
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
            _dispatcher.Dispatch(ev);

            // dispatch happens async on a worker thread
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Assert.AreEqual(3, dispatches.Count);
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub1")));
            Assert.AreEqual(ev.AsMessage().ToDocument(), dispatches[testUri.At("sub1")].ToDocument());
            Assert.AreEqual(ev.Id, dispatches[testUri.At("sub1")].Headers.DreamEventId);
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub3")));
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub4")));
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
            int expectedCombinedSetUpdates = 2;
            int combinedSetUpdates = 0;
            AutoResetEvent setResetEvent = new AutoResetEvent(false);
            _dispatcher.CombinedSetUpdated += delegate {
                combinedSetUpdates++;
                _log.DebugFormat("combinedset updated ({0})", combinedSetUpdates);
                if(combinedSetUpdates >= expectedCombinedSetUpdates) {
                    setResetEvent.Set();
                }
            };
            _dispatcher.RegisterSet("abc",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "event://sales.mindtouch.com/deki/comments/create")
                    .Elem("channel", "event://sales.mindtouch.com/deki/comments/update")
                    .Start("recipient").Elem("uri", testUri.At("sub1")).End()
                    .End(), "def");
            _dispatcher.RegisterSet("qwe",
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
            _dispatcher.Dispatch(ev);

            // dispatch happens async on a worker thread
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Assert.AreEqual(2, dispatches.Count);
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub1")));
            Assert.AreEqual(ev.AsMessage().ToDocument(), dispatches[testUri.At("sub1")].ToDocument());
            Assert.AreEqual(ev.Id, dispatches[testUri.At("sub1")].Headers.DreamEventId);
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub2")));
        }

        [Test]
        public void Dispatch_based_on_channel_match_with_different_wikiid_patterns_but_same_proxy_destination() {
            var ev = new DispatcherEvent(
                new XDoc("msg"),
                new XUri("event://sales.mindtouch.com/deki/comments/create"),
                new XUri("http://foobar.com/some/comment"));
            var dispatches = new List<DispatcherEvent>();
            XUri testUri = new XUri("http://sales.mindtouch.com/").At(StringUtil.CreateAlphaNumericKey(4));
            int dispatchCounter = 0;
            MockPlug.Register(testUri, delegate(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
                if(testUri == plug.Uri) {
                    lock(dispatches) {
                        dispatches.Add(new DispatcherEvent(request));
                        dispatchCounter++;
                    }
                }
                response.Return(DreamMessage.Ok());
            });
            int combinedSetUpdates = 0;
            _dispatcher.CombinedSetUpdated += delegate {
                combinedSetUpdates++;
                _log.DebugFormat("combinedset updated ({0})", combinedSetUpdates);
            };
            var recipient1Uri = testUri.At("sub1");
            _dispatcher.RegisterSet("abc",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "event://sales.mindtouch.com/deki/comments/create")
                    .Elem("channel", "event://sales.mindtouch.com/deki/comments/update")
                    .Elem("uri.proxy", testUri)
                    .Start("recipient").Elem("uri", recipient1Uri).End()
                    .End(), "def");
            var recipient2Uri = testUri.At("sub1");
            _dispatcher.RegisterSet("qwe",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner2")
                    .Start("subscription")
                    .Attr("id", "2")
                    .Elem("channel", "event://*/deki/comments/create")
                    .Elem("channel", "event://*/deki/comments/update")
                    .Elem("uri.proxy", testUri)
                    .Start("recipient").Elem("uri", recipient2Uri).End()
                    .End(), "asd");

            // combinedset updates happen asynchronously, so give'em a chance
            const int expectedCombinedSetUpdates = 2;
            Assert.IsTrue(
                Wait.For(() => combinedSetUpdates >= expectedCombinedSetUpdates, 10.Seconds()),
                string.Format("expected at least {0} combined set updates, gave up after {1}", expectedCombinedSetUpdates, combinedSetUpdates)
            );
            const int expectedDispatches = 2;
            _dispatcher.Dispatch(ev);

            // dispatch happens async on a worker thread
            Assert.IsTrue(
                Wait.For(() => {

                    // Doing extra sleeping to improve the chance of catching excess dispatches
                    Thread.Sleep(100);
                    return dispatchCounter == expectedDispatches;
                }, 10.Seconds()),
                string.Format("expected at exactly {0} dispatches, gave up after {1}", expectedDispatches, dispatchCounter)
            );
            var sub1Event = dispatches.Where(x => x.Recipients.Any() && x.Recipients.FirstOrDefault().Uri == recipient1Uri).FirstOrDefault();
            Assert.IsNotNull(sub1Event, "did not receive an event with recipient matching our first subscription");
            Assert.AreEqual(ev.AsDocument(), sub1Event.AsDocument(), "event document is wrong");
            Assert.AreEqual(ev.Id, sub1Event.Id, "event id is wrong");
            var sub2Event = dispatches.Where(x => x.Recipients.Any() && x.Recipients.FirstOrDefault().Uri == recipient2Uri).FirstOrDefault();
            Assert.IsNotNull(sub2Event, "did not receive an event with recipient matching our second subscription");
            Assert.AreEqual(ev.AsDocument(), sub2Event.AsDocument(), "event document is wrong");
            Assert.AreEqual(ev.Id, sub2Event.Id, "event id is wrong");
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
            int expectedCombinedSetUpdates = 2;
            int combinedSetUpdates = 0;
            AutoResetEvent setResetEvent = new AutoResetEvent(false);
            _dispatcher.CombinedSetUpdated += delegate {
                combinedSetUpdates++;
                _log.DebugFormat("combinedset updated ({0})", combinedSetUpdates);
                if(combinedSetUpdates >= expectedCombinedSetUpdates) {
                    setResetEvent.Set();
                }
            };
            _dispatcher.RegisterSet("abc",
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
            _dispatcher.RegisterSet("asd",
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
            _dispatcher.Dispatch(ev);

            // dispatch happens async on a worker thread
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Thread.Sleep(200);
            Assert.AreEqual(2, dispatches.Count);
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub1")));
            Assert.AreEqual(ev.AsMessage().ToDocument(), dispatches[testUri.At("sub1")].ToDocument());
            Assert.AreEqual(ev.Id, dispatches[testUri.At("sub1")].Headers.DreamEventId);
            Assert.IsTrue(dispatches.ContainsKey(testUri.At("sub3")));
        }

        [Test]
        public void Dispatch_with_owners_via_throws() {
            var loopService = new XUri("local:///infinite/loop-_dispatcher");
            var ev = new DispatcherEvent(new XDoc("foo"), new XUri("channel:///foo"), new XUri("http:///foo"))
                .WithVia(loopService)
                .WithVia(new XUri("local://12345/a"));
            var owner = Plug.New(loopService);
            var cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            var dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie }, _queueRepositoryMock.Object);
            try {
                dispatcher.Dispatch(ev);
                Assert.Fail("should not have gotten here");
            } catch(DreamBadRequestException) {
                return;
            }
            Assert.Fail("should not have gotten here");
        }

        [Test]
        public void Repeated_dispatch_failure_kicks_non_expiring_subscription_set() {
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
            var expectedCombinedSetUpdates = 2;
            var combinedSetUpdates = 0;
            var setResetEvent = new ManualResetEvent(false);
            _dispatcher.CombinedSetUpdated += delegate {
                combinedSetUpdates++;
                _log.DebugFormat("combinedset updated ({0})", combinedSetUpdates);
                if(combinedSetUpdates >= expectedCombinedSetUpdates) {
                    setResetEvent.Set();
                }
            };
            var location1 = _dispatcher.RegisterSet("abc",
                new XDoc("subscription-set")
                    .Attr("max-failures", 0)
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", sub1Uri).End()
                    .End(), "def").Item1.Location;
            var location2 = _dispatcher.RegisterSet("qwe",
                new XDoc("subscription-set")
                    .Attr("max-failures", 0)
                    .Elem("uri.owner", "http:///owner2")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", sub2Uri).End()
                    .End(), "asd").Item1.Location;
            Assert.IsTrue(setResetEvent.WaitOne(10000, false), "combined set didn't change expected number of times");
            Assert.IsNotNull(_dispatcher[location1]);
            Assert.IsNotNull(_dispatcher[location2]);
            _dispatcher.Dispatch(ev);
            Assert.IsTrue(sub1Mock.WaitAndVerify(TimeSpan.FromSeconds(10)), sub1Mock.VerificationFailure);
            Assert.IsTrue(sub2Mock.WaitAndVerify(TimeSpan.FromSeconds(10)), sub1Mock.VerificationFailure);
            Assert.IsTrue(Wait.For(() => _dispatcher[location2] == null, TimeSpan.FromSeconds(10)), "Second set wasn't kicked");
            Assert.IsTrue(Wait.For(() => _dispatcher[location1] == null, TimeSpan.FromSeconds(10)), "First set wasn't kicked");
        }

        [Test]
        public void Failed_dispatch_followed_by_success_should_reset_fail_count_for_non_expiring_subscription_set() {
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
            int expectedCombinedSetUpdates = 1;
            int combinedSetUpdates = 0;
            AutoResetEvent setResetEvent = new AutoResetEvent(false);
            _dispatcher.CombinedSetUpdated += delegate {
                combinedSetUpdates++;
                _log.DebugFormat("combinedset updated ({0})", combinedSetUpdates);
                if(combinedSetUpdates >= expectedCombinedSetUpdates) {
                    setResetEvent.Set();
                }
            };
            var location = _dispatcher.RegisterSet(
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
            Assert.IsNotNull(_dispatcher[location]);

            _log.DebugFormat("first dispatch (fail={0})", fail);
            _dispatcher.Dispatch(ev);
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Thread.Sleep(1000); // failure gets dealt with async
            Assert.IsNotNull(_dispatcher[location]);
            fail = false;

            _log.DebugFormat("second dispatch (fail={0})", fail);
            _dispatcher.Dispatch(ev);
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Thread.Sleep(1000); // failure reset gets dealt with async
            Assert.IsNotNull(_dispatcher[location]);
            fail = true;

            _log.DebugFormat("third dispatch (fail={0})", fail);
            _dispatcher.Dispatch(ev);
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Thread.Sleep(1000); // failure gets dealt with async
            Assert.IsNotNull(_dispatcher[location]);

            _log.DebugFormat("fourth dispatch (fail={0})", fail);
            _dispatcher.Dispatch(ev);
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            Thread.Sleep(1000); // failure gets dealt with async
            Assert.IsNull(_dispatcher[location]);
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
            var combinedSetUpdated = new AutoResetEvent(false);
            _dispatcher.CombinedSetUpdated += delegate {
                _log.DebugFormat("set updated");
                combinedSetUpdated.Set();
            };

            var proxy = testUri.At("proxy");
            _log.DebugFormat("registering set");
            _dispatcher.RegisterSet(
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
            _dispatcher.Dispatch(ev);

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
            AutoResetEvent setResetEvent = new AutoResetEvent(false);
            _dispatcher.CombinedSetUpdated += delegate {
                setResetEvent.Set();
            };
            _dispatcher.RegisterSet("abc",
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", testUri).End()
                    .End(), "def");

            // combinedset updates happen asynchronously, so give'em a chance
            Assert.IsTrue(setResetEvent.WaitOne(10000, false));
            _dispatcher.Dispatch(ev);

            // dispatch happens async on a worker thread
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
        }

        [Test]
        public void Registration_of_expiring_set_triggers_set_specific_dispatch_queue_registration() {
            _queueRepositoryMock.Setup(x => x.RegisterOrUpdate(It.Is<PubSubSubscriptionSet>(y => y.Location == "abc")));
            _dispatcher.RegisterSet(
                "abc",
                new XDoc("subscription-set")
                    .Attr("max-failure-duration", 100)
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                        .Attr("id", "1")
                        .Elem("channel", "channel:///foo/*")
                        .Start("recipient").Elem("uri", "http:///recipient").End()
                    .End(),
                "def"
            );
            _queueRepositoryMock.VerifyAll();
        }

        [Test]
        public void Dispatch_for_expiring_set_will_hit_set_specific_queue() {

            // Arrange
            var ev = new DispatcherEvent(
                new XDoc("msg"),
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            var setUpdated = false;
            _dispatcher.CombinedSetUpdated += delegate {
                setUpdated = true;
            };
            var recipientUri = new XUri("http://recipient");
            var dispatchQueueMock = new Mock<IPubSubDispatchQueue>();
            var queueResolved = false;
            _queueRepositoryMock.Setup(x => x[It.Is<PubSubSubscriptionSet>(y => y.Location == "abc")])
                .Callback(() => queueResolved = true)
                .Returns(dispatchQueueMock.Object)
                .Verifiable();
            _dispatcher.RegisterSet(
                "abc",
                new XDoc("subscription-set")
                    .Attr("max-failure-duration", 100)
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                        .Attr("id", "1")
                        .Elem("channel", "channel:///foo/bar")
                        .Start("recipient").Elem("uri", recipientUri).End()
                    .End(),
                "def"
            );

            // combinedset updates happen asynchronously, so give'em a chance
            Assert.IsTrue(Wait.For(() => setUpdated, 10.Seconds()), "combined set didn't update in time");

            // Act
            _dispatcher.Dispatch(ev);

            // Assert

            // dispatch happens asynchronously so we need to wait until our mock repository was accessed
            Assert.IsTrue(
                Wait.For(() => queueResolved, 10.Seconds()),
                "mock repository was not accessed in time"
            );
            _queueRepositoryMock.VerifyAll();
            dispatchQueueMock.Verify(
                x => x.Enqueue(It.Is<DispatchItem>(y => y.Uri == recipientUri && y.Event.Channel == ev.Channel && y.Event.Id == ev.Id)),
                Times.Once(),
                "event wasn't enqueued"
            );
        }

        [Test]
        public void Dispatch_for_expiring_set_happens_only_once_for_an_event() {

            // Arrange
            var doc = new XDoc("msg").Elem("foo", StringUtil.CreateAlphaNumericKey(10));
            var ev = new DispatcherEvent(
                doc,
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            var setUpdated = false;
            _dispatcher.CombinedSetUpdated += delegate {
                setUpdated = true;
            };
            var recipientUri = new XUri("http://recipient");
            XDoc dispatched = null;
            var dispatchCount = 0;
            MockPlug.Register(recipientUri, (plug, verb, uri, request, response) => {
                dispatchCount++;
                dispatched = request.ToDocument();
                response.Return(DreamMessage.Ok());
            });
            var dispatchQueue = new MockPubSubDispatchQueue(_dequeueHandler);
            var queueResolved = false;
            _queueRepositoryMock.Setup(x => x[It.Is<PubSubSubscriptionSet>(y => y.Location == "abc")])
                .Callback(() => queueResolved = true)
                .Returns(dispatchQueue)
                .Verifiable();
            _dispatcher.RegisterSet(
                "abc",
                new XDoc("subscription-set")
                    .Attr("max-failure-duration", 100)
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                        .Attr("id", "1")
                        .Elem("channel", "channel:///foo/bar")
                        .Start("recipient").Elem("uri", recipientUri).End()
                    .End(),
                "def"
            );

            // combinedset updates happen asynchronously, so give'em a chance
            Assert.IsTrue(Wait.For(() => setUpdated, 10.Seconds()), "combined set didn't update in time");

            // Act
            _dispatcher.Dispatch(ev);

            // Assert

            // dispatch happens asynchronously so we need to wait until our mock repository was accessed
            Assert.IsTrue(
                Wait.For(() => dispatched != null, 10.Seconds()),
                "dispatch didn't happen"
            );
            Assert.IsFalse(
                Wait.For(() => dispatchCount > 1, 2.Seconds()),
                "more than one dispatch happened"
            );
            _queueRepositoryMock.VerifyAll();
            Assert.AreEqual(doc.ToCompactString(), dispatched.ToCompactString());
        }

        [Test]
        public void Dispatch_failure_for_expiring_set_will_continue_to_retry() {

            // Arrange
            var ev = new DispatcherEvent(
                new XDoc("msg"),
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            var setUpdated = false;
            _dispatcher.CombinedSetUpdated += delegate {
                setUpdated = true;
            };
            var recipientUri = new XUri("http://recipient");
            var failures = 5;
            var requestCount = 0;
            var done = false;
            MockPlug.Register(recipientUri, (plug, verb, uri, request, response) => {
                requestCount++;
                if(requestCount > failures) {
                    response.Return(DreamMessage.Ok());
                    done = true;
                } else {
                    response.Return(DreamMessage.BadRequest("bad"));
                }
            });
            var dispatchQueue = new MockPubSubDispatchQueue(_dequeueHandler);
            var queueResolved = false;
            _queueRepositoryMock.Setup(x => x[It.Is<PubSubSubscriptionSet>(y => y.Location == "abc")])
                .Callback(() => queueResolved = true)
                .Returns(dispatchQueue)
                .Verifiable();
            _dispatcher.RegisterSet(
                "abc",
                new XDoc("subscription-set")
                    .Attr("max-failure-duration", 100)
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                        .Attr("id", "1")
                        .Elem("channel", "channel:///foo/bar")
                        .Start("recipient").Elem("uri", recipientUri).End()
                    .End(),
                "def"
            );

            // combinedset updates happen asynchronously, so give'em a chance
            Assert.IsTrue(Wait.For(() => setUpdated, 10.Seconds()), "combined set didn't update in time");

            // Act
            _dispatcher.Dispatch(ev);

            // Assert

            // dispatch happens asynchronously so we need to wait until our mock repository was accessed
            Assert.IsTrue(
                Wait.For(() => queueResolved, 10.Seconds()),
                "mock repository was not accessed in time"
            );
            _queueRepositoryMock.VerifyAll();
            Assert.IsTrue(Wait.For(() => done, 10.Seconds()), "dispatch didn't complete");
            Assert.AreEqual(failures + 1, requestCount, "wrong number of requests");
            Assert.AreEqual(failures, dispatchQueue.FailureCount, "wrong number of failure responses reported to dispatchqueue");
        }

        [Test]
        public void Dispatch_failures_lasting_longer_than_set_MaxFailureDuration_will_drop_subscription_and_events() {

            // Arrange
            var ev = new DispatcherEvent(
                new XDoc("msg"),
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            var setUpdated = false;
            _dispatcher.CombinedSetUpdated += delegate {
                setUpdated = true;
            };
            var recipientUri = new XUri("http://recipient");
            MockPlug.Register(recipientUri, (plug, verb, uri, request, response) => response.Return(DreamMessage.BadRequest("bad")));
            var dispatchQueue = new MockPubSubDispatchQueue(_dequeueHandler) {
                FailureWindow = 10.Seconds()
            };
            var queueResolved = false;
            _queueRepositoryMock.Setup(x => x[It.Is<PubSubSubscriptionSet>(y => y.Location == "abc")])
                .Callback(() => queueResolved = true)
                .Returns(dispatchQueue)
                .Verifiable();
            _dispatcher.RegisterSet(
                "abc",
                new XDoc("subscription-set")
                    .Attr("max-failure-duration", 1)
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                        .Attr("id", "1")
                        .Elem("channel", "channel:///foo/bar")
                        .Start("recipient").Elem("uri", recipientUri).End()
                    .End(),
                "def"
            );

            // combinedset updates happen asynchronously, so give'em a chance
            Assert.IsTrue(Wait.For(() => setUpdated, 10.Seconds()), "combined set didn't update in time");

            // Act
            _dispatcher.Dispatch(ev);
            setUpdated = false;

            // Assert

            // we observe the removal of the subscription by seeing the combined set updated
            Assert.IsTrue(Wait.For(() => setUpdated, 10.Seconds()), "subscription wasn't removed in tim");
            _queueRepositoryMock.VerifyAll();
            foreach(var set in _dispatcher.GetAllSubscriptionSets()) {
                Assert.AreNotEqual("abc", set.Location, "found set in list of subscriptions after it should have been dropped");
            }
        }

        [Test]
        public void Removing_set_while_dispatches_are_pending_drops_dispatches() {

            // Arrange
            var ev = new DispatcherEvent(
                new XDoc("msg"),
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            var setUpdated = false;
            _dispatcher.CombinedSetUpdated += delegate {
                setUpdated = true;
            };
            var recipientUri = new XUri("http://recipient");
            var fail = true;
            var success = false;
            var attempts = 0;
            MockPlug.Register(recipientUri, (plug, verb, uri, request, response) => {
                attempts++;
                if(fail) {
                    response.Return(DreamMessage.BadRequest("bad"));
                } else {
                    response.Return(DreamMessage.Ok());
                    success = true;
                }
            });
            var dispatchQueue = new MockPubSubDispatchQueue(_dequeueHandler);
            var queueResolved = false;
            _queueRepositoryMock.Setup(x => x[It.Is<PubSubSubscriptionSet>(y => y.Location == "abc")])
                .Callback(() => queueResolved = true)
                .Returns(dispatchQueue)
                .Verifiable();
            _dispatcher.RegisterSet(
                "abc",
                new XDoc("subscription-set")
                    .Attr("max-failure-duration", 100)
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                        .Attr("id", "1")
                        .Elem("channel", "channel:///foo/bar")
                        .Start("recipient").Elem("uri", recipientUri).End()
                    .End(),
                "def"
            );

            // combinedset updates happen asynchronously, so give'em a chance
            Assert.IsTrue(Wait.For(() => setUpdated, 10.Seconds()), "combined set didn't update in time");

            // Act
            _dispatcher.Dispatch(ev);
            Assert.IsTrue(Wait.For(() => attempts > 0, 10.Seconds()), "never attempted to dispatch event");
            _dispatcher.RemoveSet("abc");
            // we observe the removal of the subscription by seeing the combined set updated
            Assert.IsTrue(Wait.For(() => setUpdated, 10.Seconds()), "subscription wasn't removed in tim");
            fail = false;

            // Assert
            Assert.IsFalse(Wait.For(() => success, 2.Seconds()), "dispatch made it through after all");
        }

        public class MockPubSubDispatchQueue : IPubSubDispatchQueue {
            private Func<DispatchItem, Result<bool>> _dequeueHandler;
            public int FailureCount;
            public MockPubSubDispatchQueue(Func<DispatchItem, Result<bool>> dequeueHandler) {
                _dequeueHandler = dequeueHandler;
                FailureWindow = TimeSpan.Zero;
            }

            public TimeSpan FailureWindow { get; set; }

            public void Enqueue(DispatchItem item) {
                bool success;
                do {
                    success = _dequeueHandler(item).Wait();
                    if(!success) {
                        FailureCount++;
                    }
                } while(!success);
            }

            public void SetDequeueHandler(Func<DispatchItem, Result<bool>> dequeueHandler) {
                throw new InvalidOperationException("should never be called");
            }

            #region Implementation of IDisposable
            public void Dispose() { }
            #endregion
        }
    }
}