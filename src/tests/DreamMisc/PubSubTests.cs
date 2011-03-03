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
using System.Threading;
using MindTouch.Dream.Services.PubSub;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test {
    [TestFixture]
    public class PubSubTests {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static readonly XUri _mockUri = new XUri("http://test.com/");

        //--- Fields ---
        private DreamHostInfo _hostInfo;

        [TearDown]
        public void Teardown() {
            MockPlug.DeregisterAll();
        }

        [TestFixtureTearDown]
        public void FixtureTeardown() {
            if(_hostInfo != null) {
                _hostInfo.Dispose();
            }
        }

        [Test]
        public void New_services_gets_pubsub_uri_in_config() {
            InitHost();
            MockServiceInfo mock = MockService.CreateMockService(_hostInfo);
            Assert.AreEqual(_hostInfo.LocalHost.At("host", "$pubsub").Uri.WithoutQuery(), mock.Service.ServiceConfig["uri.pubsub"].AsUri);
            bool foundKey = false;
            mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                foreach(DreamCookie cookie in context.Service.Cookies.Fetch(mock.Service.PubSub.Uri)) {
                    if(cookie.Path == "/host/$pubsub" && cookie.Name == "service-key") {
                        foundKey = true;
                    }
                }
                response2.Return(DreamMessage.Ok());
            };
            mock.AtLocalHost.Post();
            Assert.IsTrue(foundKey);
        }

        [Test]
        public void Can_inject_custom_dispatcher() {
            Plug pubsub = CreatePubSubService("upstream",
                new XDoc("config")                
                    .Start("components")
                        .Start("component")
                            .Attr("implementation", typeof(MockDispatcher).AssemblyQualifiedName)
                            .Attr("type", typeof(IPubSubDispatcher).AssemblyQualifiedName)
                        .End()
                    .End()
            ).WithInternalKey().AtLocalHost;
            Assert.AreEqual(1,MockDispatcher.Instantiations);
        }

        [Test]
        public void Trying_to_retrieve_subscription_at_an_unknown_location_should_return_Forbidden() {
            Plug pubsub = CreatePubSubService().WithInternalKey().AtLocalHost;
            DreamMessage response = pubsub.At("subscribers", "ABCD").GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }

        [Test]
        public void Registering_the_same_set_twice_should_result_in_Conflict() {
            XDoc set = new XDoc("subscription-set")
               .Attr("max-failures", 1)
               .Elem("uri.owner", "http:///owner1")
               .Start("subscription")
                   .Attr("id", "1")
                   .Elem("channel", "channel:///foo/*")
                   .Start("recipient").Elem("uri", "http:///foo/sub1").End()
               .End()
               .Start("subscription")
                   .Attr("id", "2")
                   .Elem("channel", "channel:///foo/baz/*")
                   .Start("recipient").Elem("uri", "http:///foo/sub2").End()
               .End();

            // create subscription
            Plug pubsub = CreatePubSubService().WithInternalKey().AtLocalHost;
            DreamMessage response = pubsub.At("subscribers").PostAsync(set).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Created, response.Status);
            Assert.IsNull(response.Headers.ContentLocation);
            Assert.IsNotNull(response.Headers.Location);
            XUri location = response.Headers.Location;

            // try to create second time
            response = pubsub.At("subscribers").PostAsync(set).Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Conflict, response.Status);
            Assert.IsNotNull(response.Headers.ContentLocation);
            Assert.IsNull(response.Headers.Location);
            Assert.AreEqual(location.WithoutQuery(), response.Headers.ContentLocation);
        }

        [Test]
        public void Register_retrieve_update_and_delete_subscription_on_pubsub_service_using_access_key_uri() {
            XDoc set = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End()
                .Start("subscription")
                    .Attr("id", "2")
                    .Elem("channel", "channel:///foo/baz/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub2").End()
                .End();

            // create subscription
            Plug pubsub = CreatePubSubService().WithInternalKey().AtLocalHost;
            DreamMessage response = pubsub.At("subscribers").PostAsync(set).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Created, response.Status);
            Assert.IsNull(response.Headers.ContentLocation);
            Assert.IsNotNull(response.Headers.Location);
            XUri location = response.Headers.Location;
            XDoc subDoc = response.ToDocument();
            string accessKey = subDoc["access-key"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(accessKey));
            Assert.AreEqual(location, subDoc["uri.location"].AsUri.With("access-key", accessKey));
            Plug subscription = Plug.New(location);

            // retrieve subscription
            response = subscription.GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(set, response.ToDocument());

            // replace subscription
            XDoc set2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End();
            response = subscription.PutAsync(set2).Wait();
            Assert.IsTrue(response.IsSuccessful);

            // retrieve new subscription
            response = subscription.GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(set2, response.ToDocument());

            // delete subscription
            response = subscription.DeleteAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(0, Plug.GlobalCookies.Fetch(subscription.Uri).Count);

            // check it's really gone (or least no longer accessible
            response = subscription.GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }

        [Test]
        public void Register_retrieve_update_and_delete_subscription_on_pubsub_service_using_access_key_cookie() {
            XDoc set = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End()
                .Start("subscription")
                    .Attr("id", "2")
                    .Elem("channel", "channel:///foo/baz/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub2").End()
                .End();

            // create subscription
            Plug pubsub = CreatePubSubService().WithInternalKey().AtLocalHost;
            DreamMessage response = pubsub.At("subscribers").PostAsync(set).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Created, response.Status);
            Assert.IsNull(response.Headers.ContentLocation);
            Assert.IsNotNull(response.Headers.Location);
            XUri location = response.Headers.Location;
            XDoc subDoc = response.ToDocument();
            string accessKey = subDoc["access-key"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(accessKey));
            Assert.AreEqual(location, subDoc["uri.location"].AsUri.With("access-key", accessKey));
            Plug subscription = pubsub.At("subscribers", location.LastSegment);
            subscription.CookieJar.Update(DreamCookie.NewSetCookie("access-key", accessKey, location), null);

            // retrieve subscription
            response = subscription.GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(set, response.ToDocument());

            // replace subscription
            XDoc set2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End();
            response = subscription.PutAsync(set2).Wait();
            Assert.IsTrue(response.IsSuccessful);

            // retrieve new subscription
            response = subscription.GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(set2, response.ToDocument());

            // delete subscription
            response = subscription.DeleteAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(0, Plug.GlobalCookies.Fetch(subscription.Uri).Count);

            // check it's really gone (or least no longer accessible
            response = subscription.GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }

        [Test]
        public void Update_with_lower_version_number_should_302() {
            XDoc set = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 10)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End()
                .Start("subscription")
                    .Attr("id", "2")
                    .Elem("channel", "channel:///foo/baz/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub2").End()
                .End();

            // create subscription
            Plug pubsub = CreatePubSubService().WithInternalKey().AtLocalHost;
            DreamMessage response = pubsub.At("subscribers").PostAsync(set).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Created, response.Status);
            Assert.IsNull(response.Headers.ContentLocation);
            Assert.IsNotNull(response.Headers.Location);
            XUri location = response.Headers.Location;
            XDoc subDoc = response.ToDocument();
            string accessKey = subDoc["access-key"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(accessKey));
            Assert.AreEqual(location, subDoc["uri.location"].AsUri.With("access-key", accessKey));
            Plug subscription = Plug.New(location);

            // retrieve subscription
            response = subscription.GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(set, response.ToDocument());

            // replace subscription
            XDoc set2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 9)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End();
            response = subscription.PutAsync(set2).Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.NotModified, response.Status);

            // retrieve new subscription
            response = subscription.GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(set, response.ToDocument());
        }

        [Test]
        public void Update_with_no_version_number_should_overwrite_any_version() {
            XDoc set = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 10)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End()
                .Start("subscription")
                    .Attr("id", "2")
                    .Elem("channel", "channel:///foo/baz/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub2").End()
                .End();

            // create subscription
            Plug pubsub = CreatePubSubService().WithInternalKey().AtLocalHost;
            DreamMessage response = pubsub.At("subscribers").PostAsync(set).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Created, response.Status);
            Assert.IsNull(response.Headers.ContentLocation);
            Assert.IsNotNull(response.Headers.Location);
            XUri location = response.Headers.Location;
            XDoc subDoc = response.ToDocument();
            string accessKey = subDoc["access-key"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(accessKey));
            Assert.AreEqual(location, subDoc["uri.location"].AsUri.With("access-key", accessKey));
            Plug subscription = Plug.New(location);

            // retrieve subscription
            response = subscription.GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(set, response.ToDocument());

            // replace subscription
            XDoc set2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End();
            response = subscription.PutAsync(set2).Wait();
            Assert.IsTrue(response.IsSuccessful);

            // retrieve new subscription
            response = subscription.GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(set2, response.ToDocument());
        }

        [Test]
        public void CombinedSet_of_service_combines_all_registered_subs() {
            XUri c1 = new XUri("channel:///c1");
            XUri c2 = new XUri("channel:///c2");
            XUri c3 = new XUri("channel:///c3");
            XUri r1 = new XUri("http:///r1");
            XUri r2 = new XUri("http:///r2");
            XDoc set1 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "123")
                    .Elem("channel", c1)
                    .Elem("channel", c2)
                    .Elem("uri.proxy", "http:///proxy")
                    .Start("recipient").Attr("auth-token", "abc").Elem("uri", r1).End()
                .EndAll();
            XDoc set2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner2")
                .Start("subscription")
                    .Attr("id", "123")
                    .Elem("channel", c1)
                    .Elem("channel", c3)
                    .Elem("uri.proxy", "http:///proxy")
                    .Start("recipient").Attr("auth-token", "abc").Elem("uri", r2).End()
                .EndAll();

            Plug pubsub = CreatePubSubService().WithInternalKey().AtLocalHost;
            DreamMessage response = pubsub.At("subscribers").PostAsync(set1).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Created, response.Status);
            response = pubsub.At("subscribers").PostAsync(set2).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Created, response.Status);
            Thread.Sleep(1000);
            response = pubsub.At("subscribers").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Ok, response.Status);
            PubSubSubscriptionSet combinedSet = new PubSubSubscriptionSet(response.ToDocument());
            Assert.AreEqual(3, combinedSet.Subscriptions.Length);
            XUri owner = pubsub.Uri.WithoutQuery();
            foreach(PubSubSubscription subx in combinedSet.Subscriptions) {
                switch(subx.Channels[0].ToString()) {
                case "channel:///c1":
                    Assert.AreEqual(owner.At("publish"), subx.Destination);
                    Assert.AreEqual(1, subx.Channels.Length);
                    Assert.AreEqual(c1, subx.Channels[0]);
                    Assert.AreEqual(2, subx.Recipients.Length);
                    bool foundR1 = false;
                    bool foundR2 = false;
                    foreach(DispatcherRecipient r in subx.Recipients) {
                        if(r.Uri == r1) {
                            foundR1 = true;
                        } else if(r.Uri == r2) {
                            foundR2 = true;
                        }
                    }
                    Assert.IsTrue(foundR1 && foundR2);
                    break;
                case "channel:///c2":
                    Assert.AreEqual(owner.At("publish"), subx.Destination);
                    Assert.AreEqual(1, subx.Channels.Length);
                    Assert.AreEqual(c2, subx.Channels[0]);
                    Assert.AreEqual(1, subx.Recipients.Length);
                    Assert.AreEqual(r1, subx.Recipients[0].Uri);
                    break;
                case "channel:///c3":
                    Assert.AreEqual(owner.At("publish"), subx.Destination);
                    Assert.AreEqual(1, subx.Channels.Length);
                    Assert.AreEqual(c3, subx.Channels[0]);
                    Assert.AreEqual(1, subx.Recipients.Length);
                    Assert.AreEqual(r2, subx.Recipients[0].Uri);
                    break;
                default:
                    Assert.Fail();
                    break;
                }
            }
        }

        [Test]
        public void Put_sub_at_unknown_location_should_be_Forbidden() {
            XDoc set = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End();

            Plug pubsub = CreatePubSubService().WithInternalKey().AtLocalHost;
            DreamMessage response = pubsub.At("subscribers", "ABCD").PutAsync(set).Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }

        [Test]
        public void Put_sub_with_wrong_owner_should_be_Forbidden() {
            XDoc set = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End();

            // create subscription
            Plug pubsub = CreatePubSubService().WithInternalKey().AtLocalHost;
            DreamMessage response = pubsub.At("subscribers").PostAsync(set).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Created, response.Status);
            Assert.IsNull(response.Headers.ContentLocation);
            Assert.IsNotNull(response.Headers.Location);
            XUri location = response.Headers.Location;
            Plug subscription = Plug.New(location);

            // replace subscription
            XDoc set2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner2")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End();
            response = subscription.PutAsync(set2).Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }

        [Test]
        public void Publish_against_pubsub_channel_should_be_forbidden() {
            InitHost();
            // publish event via a mock service, since publish is marked internal
            MockServiceInfo mock = MockService.CreateMockService(_hostInfo);
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            DreamMessage r = null;
            mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                DreamMessage message = DreamMessage.Ok();
                message.Headers.DreamEventChannel = "pubsub://should/be/forbidden";
                message.Headers.DreamEventOrigin = new string[] { "http://foo/bar/old", "http://foo/bar/new" };
                message.Headers.DreamEventRecipients = new string[] { "mailto://userA@foo.com", "mailto://userB@foo.com", "mailto://userC@foo.com" };
                r = mock.Service.PubSub.At("publish").PostAsync(message).Wait();
                response2.Return(DreamMessage.Ok());
                resetEvent.Set();
            };
            mock.AtLocalHost.Post();

            // wait for async dispatch to happen
            if(!resetEvent.WaitOne(1000, false)) {
                Assert.Fail("async dispatch didn't happen");
            }
            Assert.IsFalse(r.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, r.Status);
        }

        [Test]
        public void PubSub_end_to_end() {
            XUri testUri = _mockUri.At("foo", "sub1");
            string serviceKey = "1234";
            DreamCookie accessCookie = DreamCookie.NewSetCookie("service-key", serviceKey, testUri);
            XDoc msg = new XDoc("foo");
            DispatcherEvent ev = new DispatcherEvent(
                msg,
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            XDoc set = new XDoc("subscription-set")
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Add(accessCookie.AsSetCookieDocument)
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", testUri).End()
                .End();

            // create subscription using a mockservice, so we get the general pubsub subscribe injected
            InitHost();
            MockServiceInfo subMock = MockService.CreateMockService(_hostInfo);
            subMock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                DreamMessage r = subMock.Service.PubSub.At("subscribers").PostAsync(set).Wait();
                Assert.IsTrue(r.IsSuccessful);
                Assert.AreEqual(DreamStatus.Created, r.Status);
                response2.Return(DreamMessage.Ok());
            };
            subMock.AtLocalHost.Post();

            // set up subscription destination mock
            DreamMessage receivedEvent = null;
            XUri recipient = null;
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            MockPlug.Register(_mockUri, delegate(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response2) {
                _log.DebugFormat("destination called: {0}", uri);
                recipient = plug.Uri;
                receivedEvent = request;
                response2.Return(DreamMessage.Ok());
                resetEvent.Set();
            });

            // publish event via a mock service, so we get the general pubsub publish injected
            MockServiceInfo pubMock = MockService.CreateMockService(_hostInfo);
            pubMock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                DreamMessage r = pubMock.Service.PubSub.At("publish").PostAsync(ev.AsMessage()).Wait();
                Assert.IsTrue(r.IsSuccessful);
                Assert.AreEqual(DreamStatus.Ok, r.Status);
                response2.Return(DreamMessage.Ok());
            };
            pubMock.AtLocalHost.Post();

            // wait for async dispatch to happen
            if(!resetEvent.WaitOne(1000, false)) {
                Assert.Fail("async dispatch didn't happen");
            }
            Assert.AreEqual(recipient, testUri);
            Assert.AreEqual(msg, receivedEvent.ToDocument());
            Assert.AreEqual(serviceKey, DreamCookie.GetCookie(receivedEvent.Cookies, "service-key").Value);
            Assert.AreEqual(ev.Id, receivedEvent.Headers.DreamEventId);
        }

        [Test]
        public void Subscribing_to_pubsub_channel_results_in_combined_set_being_pushed() {
            var downstream = CreatePubSubService().WithInternalKey().AtLocalHost;
            var upstreamUri = new XUri("http://upstream/");
            var upstreamMock = MockPlug.Register(upstreamUri);
            XDoc downstreamSet = null;
            upstreamMock.Expect().Verb("POST").RequestDocument(_ => {
                downstreamSet = _;
                return true;
            }).Response(DreamMessage.Ok());
            var subscribeToDownstream = new XDoc("subscription-set")
                .Elem("uri.owner", upstreamUri)
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "foo://bar/baz/*")
                    .Start("recipient").Elem("uri", upstreamUri).End()
                .End()
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "pubsub://*/*")
                    .Start("recipient").Elem("uri", upstreamUri).End()
                .End();

            // subscribe to set updates from downstream, expect most recent version to be pushed
            var downstreamResponse = downstream.At("subscribers").PostAsync(subscribeToDownstream).Wait();
            var location = downstreamResponse.Headers.Location;
            Assert.IsTrue(downstreamResponse.IsSuccessful, downstreamResponse.ToText());
            Assert.IsTrue(upstreamMock.WaitAndVerify(TimeSpan.FromSeconds(10)), upstreamMock.VerificationFailure);
            Assert.AreEqual(1, downstreamSet["subscription[channel='foo://bar/baz/*']"].ListLength);

            subscribeToDownstream
                .Start("subscription")
                    .Attr("id", "3")
                    .Elem("channel", "foo://bar/bob")
                    .Start("recipient").Elem("uri", upstreamUri).End()
                .End();

            // change subscription, expect most recent version to be pushed
            upstreamMock.Expect().Verb("POST").RequestDocument(_ => {
                downstreamSet = _;
                return true;
            }).Response(DreamMessage.Ok());
            downstreamResponse = Plug.New(location).PutAsync(subscribeToDownstream).Wait();
            Assert.IsTrue(downstreamResponse.IsSuccessful, downstreamResponse.ToText());
            Assert.IsTrue(upstreamMock.WaitAndVerify(TimeSpan.FromSeconds(10)), upstreamMock.VerificationFailure);
            Assert.AreEqual(1, downstreamSet["subscription[channel='foo://bar/baz/*']"].ListLength);
            Assert.AreEqual(1, downstreamSet["subscription[channel='foo://bar/bob']"].ListLength);
        }

        [Test]
        public void Adding_new_sub_will_push_combined_set_to_upstream_subscriber() {
            var downstream = CreatePubSubService().WithInternalKey().AtLocalHost;
            var upstreamUri = new XUri("http://upstream/");
            var upstreamMock = MockPlug.Register(upstreamUri);
            XDoc downstreamSet = null;
            upstreamMock.Expect().Verb("POST").RequestDocument(_ => {
                downstreamSet = _;
                return true;
            }).Response(DreamMessage.Ok());
            var subscribeToDownstream = new XDoc("subscription-set")
                .Elem("uri.owner", upstreamUri)
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "pubsub://*/*")
                    .Start("recipient").Elem("uri", upstreamUri).End()
                .End();

            // subscribe to set updates from downstream, expect most recent version to be pushed
            var downstreamResponse = downstream.At("subscribers").PostAsync(subscribeToDownstream).Wait();
            Assert.IsTrue(downstreamResponse.IsSuccessful, downstreamResponse.ToText());
            Assert.IsTrue(upstreamMock.WaitAndVerify(TimeSpan.FromSeconds(10)), upstreamMock.VerificationFailure);

            // someone else posts a subscription, expect a new push including that sub
            upstreamMock.Expect().Verb("POST").RequestDocument(_ => {
                downstreamSet = _;
                return true;
            }).Response(DreamMessage.Ok());
            var subscription = new XDoc("subscription-set")
                .Elem("uri.owner", "http://foo/")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "foo://bar/baz")
                    .Start("recipient").Elem("uri", "http://foo/bar").End()
                .End();
            var subResponse = downstream.At("subscribers").PostAsync(subscription).Wait();
            Assert.IsTrue(subResponse.IsSuccessful, subResponse.ToText());
            Assert.IsTrue(upstreamMock.WaitAndVerify(TimeSpan.FromSeconds(10)), upstreamMock.VerificationFailure);
            Assert.AreEqual(1, downstreamSet["subscription[channel='foo://bar/baz']"].ListLength);
        }

        [Ignore("slow test, run manually")]
        [Test]
        public void Parallel_chaining_subscription_and_message_propagation() {
            InitHost();
            var rootPubsub = Plug.New(_hostInfo.Host.LocalMachineUri.At("host", "$pubsub", "subscribers"));
            var goTrigger = new ManualResetEvent(false);
            var pubsubResults = new List<Result<Plug>>();
            for(var i = 0; i < 10; i++) {
                pubsubResults.Add(Async.ForkThread(() => {
                    goTrigger.WaitOne();
                    return CreatePubSubService("upstream", new XDoc("config").Start("downstream").Elem("uri", rootPubsub).End()).WithInternalKey().AtLocalHost;
                }, new Result<Plug>()));
            }
            var subscriberResults = new List<Result<Tuplet<XUri, AutoMockPlug>>>();
            for(var i = 0; i < 20; i++) {
                var mockUri = new XUri("http://mock/" + i);
                subscriberResults.Add(Async.ForkThread(() => {
                    goTrigger.WaitOne();
                    rootPubsub.With("apikey", _hostInfo.ApiKey).Post(new XDoc("subscription-set")
                        .Elem("uri.owner", mockUri)
                        .Start("subscription")
                            .Attr("id", "1")
                            .Elem("channel", "channel://foo/bar")
                            .Start("recipient").Elem("uri", mockUri).End()
                        .End());
                    var mock = MockPlug.Register(mockUri);
                    return new Tuplet<XUri, AutoMockPlug>(mockUri, mock);
                }, new Result<Tuplet<XUri, AutoMockPlug>>()));
            }
            goTrigger.Set();
            var pubsubs = new List<Plug>();
            foreach(var r in pubsubResults) {
                pubsubs.Add(r.Wait());
            }
            var endpoints = new List<XUri>();
            var mocks = new List<AutoMockPlug>();
            foreach(var r in subscriberResults) {
                var v = r.Wait();
                endpoints.Add(v.Item1);
                mocks.Add(v.Item2);
            }
            foreach(var pubsub in pubsubs) {
                Plug plug = pubsub;
                Wait.For(() => {
                    var set = plug.At("subscribers").Get();
                    return set.ToDocument()["subscription/recipient"].ListLength == endpoints.Count;
                }, TimeSpan.FromSeconds(10));
            }
            var ev = new DispatcherEvent(new XDoc("blah"), new XUri("channel://foo/bar"), new XUri("http://foobar.com/some/page"));

            foreach(var mock in mocks) {
                mock.Expect().Verb("POST").RequestDocument(ev.AsDocument()).Response(DreamMessage.Ok());
            }
            pubsubs[0].At("publish").Post(ev.AsMessage());
            foreach(var mock in mocks) {
                Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(10)), mock.VerificationFailure);
            }
        }

        [Test]
        public void PubSub_downstream_chaining_and_subscription_propagation() {
            XUri downstreamUri = new XUri("http://localhost/downstream");
            var mock = MockPlug.Register(downstreamUri);
            XDoc downstreamDoc = null;
            mock.Expect().Verb("GET").Uri(downstreamUri).Response(DreamMessage.Ok(new XDoc("subscription-set")
                .Elem("uri.owner", downstreamUri)
                .Start("subscription")
                   .Attr("id", "1")
                   .Elem("channel", "channel:///foo/*")
                   .Elem("uri.proxy", downstreamUri.At("publish"))
                   .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End()));
            mock.Expect().Verb("POST").Uri(downstreamUri).RequestDocument(doc => {
                downstreamDoc = doc;
                return true;
            });

            // create service, which should subscribe to the upstream one
            Plug upstreamPubSub = CreatePubSubService(new XDoc("config").Start("downstream").Elem("uri", downstreamUri).End()).WithInternalKey().AtLocalHost;
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(10)), mock.VerificationFailure);
            var subUri = downstreamDoc["subscription/recipient/uri"].AsUri.AsPublicUri();
            Plug.GlobalCookies.Update(DreamCookie.ParseAllSetCookieNodes(downstreamDoc["subscription/set-cookie"]), subUri);
            Assert.AreEqual("pubsub://*/*", downstreamDoc["subscription/channel"].AsText);
            Assert.AreEqual(upstreamPubSub.Uri.WithoutQuery(), downstreamDoc["uri.owner"].AsUri);
            Assert.AreEqual(upstreamPubSub.Uri.At("subscribers").WithoutQuery(), subUri.WithoutLastSegment());

            // check that our set is properly represented by the upstream service
            DreamMessage upstreamSet = upstreamPubSub.At("subscribers").GetAsync().Wait();
            Assert.IsTrue(upstreamSet.IsSuccessful);
            XDoc upstreamDoc = upstreamSet.ToDocument();
            Assert.AreEqual(1, upstreamDoc["subscription"].ListLength);
            Assert.AreEqual("channel:///foo/*", upstreamDoc["subscription/channel"].AsText);

            // update the subscription set on the upstream service
            XDoc set2 = new XDoc("subscription-set")
               .Elem("uri.owner", downstreamUri)
               .Start("subscription")
                   .Attr("id", "1")
                   .Elem("channel", "channel:///bar/*")
                   .Elem("uri.proxy", downstreamUri.At("publish"))
                   .Start("recipient").Elem("uri", "http:///foo/sub1").End()
               .End();
            DreamMessage subPush = Plug.New(subUri).PostAsync(set2).Wait();
            Assert.IsTrue(subPush.IsSuccessful);
            upstreamSet = upstreamPubSub.At("subscribers").GetAsync().Wait();
            Assert.IsTrue(upstreamSet.IsSuccessful);
            upstreamDoc = upstreamSet.ToDocument();
            Assert.AreEqual(1, upstreamDoc["subscription"].ListLength);
            Assert.AreEqual("channel:///bar/*", upstreamDoc["subscription/channel"].AsText);

        }

        [Test]
        public void Chained_pubSub_end_to_end() {
            XUri testUri = _mockUri.At("foo", "sub1");
            string serviceKey = "1234";
            DreamCookie accessCookie = DreamCookie.NewSetCookie("service-key", serviceKey, testUri);
            XDoc msg = new XDoc("foo");
            DispatcherEvent ev = new DispatcherEvent(
                msg,
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            XDoc set = new XDoc("subscription-set")
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Add(accessCookie.AsSetCookieDocument)
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", testUri).End()
                .End();

            // create pubsub chain
            InitHost();
            Plug middle = Plug.New(_hostInfo.Host.LocalMachineUri.At("host", "$pubsub", "subscribers"));
            Plug upstreamPubSub = CreatePubSubService("upstream", new XDoc("config").Start("downstream").Elem("uri", middle).End()).WithInternalKey().AtLocalHost;
            Plug downstreamPubSub = CreatePubSubService("downstream", new XDoc("config").Start("upstream").Elem("uri", middle).End()).WithInternalKey().AtLocalHost;

            // create subscription
            DreamMessage response = downstreamPubSub.At("subscribers").PostAsync(set).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Created, response.Status);

            // check that downstream really has the subscription
            Thread.Sleep(1000);
            DreamMessage downstreamSet = downstreamPubSub.At("subscribers").GetAsync().Wait();
            Assert.IsTrue(downstreamSet.IsSuccessful);
            XDoc downstreamSetDoc = downstreamSet.ToDocument();
            Assert.AreEqual(testUri.ToString(), downstreamSetDoc["subscription/recipient/uri"].AsText);

            // check that upstream really has the subscription
            DreamMessage upstreamSet = downstreamPubSub.At("subscribers").GetAsync().Wait();
            Assert.IsTrue(upstreamSet.IsSuccessful);
            XDoc upstreamSetDoc = upstreamSet.ToDocument();
            Assert.AreEqual(testUri.ToString(), upstreamSetDoc["subscription/recipient/uri"].AsText);

            // set up subscription destination mock
            DreamMessage receivedEvent = null;
            XDoc receivedDoc = null;
            XUri recipient = null;
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            MockPlug.Register(_mockUri, delegate(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response2) {
                recipient = plug.Uri;
                receivedEvent = request;
                receivedDoc = request.ToDocument();
                response2.Return(DreamMessage.Ok());
                resetEvent.Set();
            });

            // publish event via a mock service, since publish is marked internal
            _log.DebugFormat("setting up Mock Service");
            DreamCookie upstreamCookie = upstreamPubSub.CookieJar.Fetch(upstreamPubSub.Uri)[0];
            XDoc upstreamSetCookieElement = upstreamCookie.AsSetCookieDocument;
            MockServiceInfo mock = MockService.CreateMockService(
                _hostInfo,
                new XDoc("config")
                .Elem("uri.pubsub", upstreamPubSub.Uri)
                .Add(upstreamSetCookieElement));
            mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                _log.DebugFormat("publishing event (in MockService)");
                DreamMessage r = mock.Service.PubSub.At("publish").PostAsync(ev.AsMessage()).Wait();
                Assert.IsTrue(r.IsSuccessful);
                Assert.AreEqual(DreamStatus.Ok, r.Status);
                response2.Return(DreamMessage.Ok());
            };
            _log.DebugFormat("publishing event via up Mock Service");
            mock.AtLocalHost.Post();

            // wait for async dispatch to happen
            _log.DebugFormat("waiting on async dispatch");
            if(!resetEvent.WaitOne((int)TimeSpan.FromSeconds(1).TotalMilliseconds, true)) {
                Assert.Fail("async dispatch didn't happen");
            }
            Assert.AreEqual(recipient, testUri);
            Assert.AreEqual(msg, receivedDoc);
            Assert.AreEqual(serviceKey, DreamCookie.GetCookie(receivedEvent.Cookies, "service-key").Value);
            Assert.AreEqual(ev.Id, receivedEvent.Headers.DreamEventId);
        }
        
        [Test]
        public void Can_aggregate_from_multiple_dream_hosts() {

            // set up hosts
            _log.DebugFormat("---- creating upstream hosts");
            var sourceHost1 = DreamTestHelper.CreateRandomPortHost();
            var source1PubSub = Plug.New(sourceHost1.LocalHost.At("host", "$pubsub").With("apikey", sourceHost1.ApiKey));
            var sourceHost2 = DreamTestHelper.CreateRandomPortHost();
            var source2PubSub = Plug.New(sourceHost2.LocalHost.At("host", "$pubsub").With("apikey", sourceHost2.ApiKey));

            // create aggregator
            _log.DebugFormat("---- creating downstream host");
            var aggregatorPath = "pubsubaggregator";
            var aggregatorHost = DreamTestHelper.CreateRandomPortHost();
            aggregatorHost.Host.RunScripts(new XDoc("config")
                .Start("script").Start("action")
                    .Attr("verb", "POST")
                    .Attr("path", "/host/services")
                    .Start("config")
                        .Elem("path", aggregatorPath)
                        .Elem("sid", "sid://mindtouch.com/dream/2008/10/pubsub")
                        .Elem("apikey", "abc")
                        .Start("upstream")
                            .Elem("uri", source1PubSub.At("subscribers"))
                            .Elem("uri", source2PubSub.At("subscribers"))
                    .End()
                .End().End(), null);
            var aggregatorPubSub = aggregatorHost.LocalHost.At(aggregatorPath).With("apikey", "abc");

            // create subscription
            _log.DebugFormat("---- create downstream subscription");
            var testUri = new XUri("http://mock/aggregator");
            var serviceKey = "1234";
            var accessCookie = DreamCookie.NewSetCookie("service-key", serviceKey, testUri);
            var subscriberApiKey = "xyz";
            var set = new XDoc("subscription-set")
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Add(accessCookie.AsSetCookieDocument)
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient")
                        .Attr("authtoken", subscriberApiKey)
                        .Elem("uri", testUri)
                    .End()
                .End();
            var r = aggregatorPubSub.At("subscribers").PostAsync(set).Wait();
            Assert.IsTrue(r.IsSuccessful, r.Status.ToString());
            Assert.AreEqual(DreamStatus.Created, r.Status);

            // Verify that upstream host pubsub services have the subscription
            Func<DreamMessage, bool> waitFunc = response => {
                var sub = response.ToDocument()["subscription-set/subscription[channel='channel:///foo/*']"];
                return (!sub.IsEmpty
                        && sub["recipient/uri"].AsText.EqualsInvariantIgnoreCase(testUri.ToString())
                        && sub["recipient/@authtoken"].AsText.EqualsInvariant(subscriberApiKey));
            };
            Assert.IsTrue(WaitFor(source1PubSub.At("diagnostics", "subscriptions"), waitFunc, TimeSpan.FromSeconds(5)), "source 1 didn't get the subscription");
            Assert.IsTrue(WaitFor(source2PubSub.At("diagnostics", "subscriptions"), waitFunc, TimeSpan.FromSeconds(5)), "source 2 didn't get the subscription");

            // set up destination mock
            DispatcherEvent aggregatorEvent = new DispatcherEvent(
                new XDoc("aggregator"),
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            DispatcherEvent source1Event = new DispatcherEvent(
                new XDoc("source1"),
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            DispatcherEvent source2Event = new DispatcherEvent(
                new XDoc("source2"),
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"));
            var mock = MockPlug.Register(testUri);

            // Publish event into aggregator
            mock.Expect().Verb("POST").RequestDocument(aggregatorEvent.AsDocument());
            r = aggregatorPubSub.At("publish").PostAsync(aggregatorEvent.AsMessage()).Wait();
            Assert.IsTrue(r.IsSuccessful, r.Status.ToString());
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(10)), mock.VerificationFailure);

            // Publish event into source1
            mock.Reset();
            mock.Expect().Verb("POST").RequestDocument(source1Event.AsDocument());
            r = source1PubSub.At("publish").PostAsync(source1Event.AsMessage()).Wait();
            Assert.IsTrue(r.IsSuccessful, r.Status.ToString());
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(10)), mock.VerificationFailure);

            // Publish event into source2
            mock.Reset();
            mock.Expect().Verb("POST").RequestDocument(source2Event.AsDocument());
            r = source2PubSub.At("publish").PostAsync(source2Event.AsMessage()).Wait();
            Assert.IsTrue(r.IsSuccessful, r.Status.ToString());
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(10)), mock.VerificationFailure);
        }

        [Test]
        public void Event_from_DreamMessage_and_back() {
            XDoc doc = new XDoc("foo");
            DreamMessage message = DreamMessage.Ok(doc);
            message.Headers.DreamEventChannel = "channel:///deki/pages/move";
            message.Headers.DreamEventOrigin = new string[] { "http://foo/bar/old", "http://foo/bar/new" };
            message.Headers.DreamEventRecipients = new string[] { "mailto://userA@foo.com", "mailto://userB@foo.com", "mailto://userC@foo.com" };
            message.Headers.DreamEventVia = new string[] { "local://12345/a", "local://12345/a" };
            DispatcherEvent ev = new DispatcherEvent(message);
            Assert.IsNotEmpty(ev.Id);
            Assert.AreEqual("channel:///deki/pages/move", ev.Channel.ToString());
            Assert.AreEqual(2, new List<XUri>(ev.Origins).Count);
            Assert.AreEqual(2, new List<XUri>(ev.Via).Count);
            Assert.AreEqual(3, new List<DispatcherRecipient>(ev.Recipients).Count);
            DreamMessage message2 = ev.AsMessage();
            Assert.AreEqual(ev.Id, message2.Headers.DreamEventId);
            Assert.AreEqual(message.Headers.DreamEventOrigin, message2.Headers.DreamEventOrigin);
            Assert.AreEqual(message.Headers.DreamEventRecipients, message2.Headers.DreamEventRecipients);
            Assert.AreEqual(message.Headers.DreamEventVia, message2.Headers.DreamEventVia);
            Assert.AreEqual(doc, message2.ToDocument());
        }

        [Test]
        public void New_Event_from_XDoc_and_back() {
            XDoc msg = new XDoc("msg");
            XUri channel = new XUri("channel://foo.com/bar");
            XUri origin = new XUri("http://foo.com/baz");
            DispatcherEvent ev = new DispatcherEvent(msg, channel, origin);
            Assert.IsFalse(string.IsNullOrEmpty(ev.Id));
            Assert.AreEqual(channel, ev.Channel);
            List<XUri> origins = new List<XUri>(ev.Origins);
            Assert.AreEqual(1, origins.Count);
            Assert.AreEqual(origin, origins[0]);

            DreamMessage message = ev.AsMessage();
            Assert.AreEqual(msg, message.ToDocument());
            Assert.AreEqual(channel.ToString(), message.Headers.DreamEventChannel);
            Assert.AreEqual(origin.ToString(), message.Headers.DreamEventOrigin[0]);
        }

        [Test]
        public void New_Event_from_bytes_and_back_as_multiple_streams() {
            byte[] bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            DispatcherEvent ev = new DispatcherEvent(DreamMessage.Ok(MimeType.BINARY, bytes), new XUri("channel:///foo"), new XUri("http:///origin"));
            DreamMessage m1 = ev.AsMessage();
            DreamMessage m2 = ev.AsMessage();
            Assert.AreEqual(9, m1.ContentLength);
            Assert.AreEqual(9, m2.ContentLength);
            MemoryStream ms1 = m1.ToStream().ToMemoryStream(m1.ContentLength, new Result<MemoryStream>()).Wait();
            MemoryStream ms2 = m1.ToStream().ToMemoryStream(m1.ContentLength, new Result<MemoryStream>()).Wait();
            Assert.AreEqual(bytes, ms1.GetBuffer());
            Assert.AreEqual(bytes, ms2.GetBuffer());
        }

        [Test]
        public void Add_via_to_event() {
            XDoc msg = new XDoc("msg");
            XUri via1 = new XUri("http://foo.com/route1");
            XUri via2 = new XUri("http://foo.com/route2");
            DispatcherEvent ev1 = new DispatcherEvent(msg, new XUri("channel://foo.com/bar"), new XUri("http://foo.com/baz"));
            DispatcherEvent ev2 = ev1.WithVia(via1);
            Assert.AreEqual(0, ev1.Via.Length);
            Assert.AreEqual(1, ev2.Via.Length);
            Assert.AreEqual(via1, ev2.Via[0]);
            DispatcherEvent ev3 = ev2.WithVia(via2);
            Assert.AreEqual(2, ev3.Via.Length);
            Assert.AreEqual(via1, ev3.Via[0]);
            Assert.AreEqual(via2, ev3.Via[1]);
            DreamMessage ev3msg = ev3.AsMessage();
            Assert.AreEqual(msg, ev3msg.ToDocument());
            Assert.AreEqual(ev1.Id, ev3.Id);
            Assert.AreEqual("channel://foo.com/bar", ev3msg.Headers.DreamEventChannel);
            Assert.AreEqual("http://foo.com/baz", ev3msg.Headers.DreamEventOrigin[0]);
            Assert.AreEqual(via1.ToString(), ev3msg.Headers.DreamEventVia[0]);
        }

        [Test]
        public void Add_recipients_to_event() {
            XDoc msg = new XDoc("msg");
            DispatcherRecipient r1 = new DispatcherRecipient(new XUri("mailto:///u1@bar.com"));
            DispatcherRecipient r2 = new DispatcherRecipient(new XUri("mailto:///u2@bar.com"));
            DispatcherRecipient r3 = new DispatcherRecipient(new XUri("mailto:///u3@bar.com"));
            DispatcherEvent ev1 = new DispatcherEvent(msg, new XUri("channel://foo.com/bar"), new XUri("http://foo.com/baz"));
            DispatcherEvent ev2 = ev1.WithRecipient(false, r1);
            Assert.AreEqual(0, ev1.Recipients.Length);
            Assert.AreEqual(1, ev2.Recipients.Length);
            Assert.AreEqual(r1, ev2.Recipients[0]);
            DispatcherEvent ev3 = ev2.WithRecipient(false, r2, r3);
            Assert.AreEqual(3, ev3.Recipients.Length);
            Assert.AreEqual(r1, ev3.Recipients[0]);
            Assert.AreEqual(r2, ev3.Recipients[1]);
            Assert.AreEqual(r3, ev3.Recipients[2]);
            DreamMessage ev3msg = ev3.AsMessage();
            Assert.AreEqual(msg, ev3msg.ToDocument());
            Assert.AreEqual(ev1.Id, ev3.Id);
            Assert.AreEqual("channel://foo.com/bar", ev3msg.Headers.DreamEventChannel);
            Assert.AreEqual("http://foo.com/baz", ev3msg.Headers.DreamEventOrigin[0]);
            string[] recipients = ev3msg.Headers.DreamEventRecipients;
            Assert.AreEqual(3, recipients.Length);
            Assert.AreEqual(r1.ToString(), recipients[0]);
            Assert.AreEqual(r2.ToString(), recipients[1]);
            Assert.AreEqual(r3.ToString(), recipients[2]);
        }


        [Test]
        public void Replace_recipients_on_event() {
            XDoc msg = new XDoc("msg");
            DispatcherRecipient r1 = new DispatcherRecipient(new XUri("mailto:///u1@bar.com"));
            DispatcherRecipient r2 = new DispatcherRecipient(new XUri("mailto:///u2@bar.com"));
            DispatcherRecipient r3 = new DispatcherRecipient(new XUri("mailto:///u3@bar.com"));
            DispatcherEvent ev1 = new DispatcherEvent(msg, new XUri("channel://foo.com/bar"), new XUri("http://foo.com/baz"));
            DispatcherEvent ev2 = ev1.WithRecipient(true, r1);
            Assert.AreEqual(0, ev1.Recipients.Length);
            Assert.AreEqual(1, ev2.Recipients.Length);
            Assert.AreEqual(r1, ev2.Recipients[0]);
            DispatcherEvent ev3 = ev2.WithRecipient(true, r2, r3);
            Assert.AreEqual(2, ev3.Recipients.Length);
            Assert.AreEqual(r2, ev3.Recipients[0]);
            Assert.AreEqual(r3, ev3.Recipients[1]);
            DreamMessage ev3msg = ev3.AsMessage();
            Assert.AreEqual(msg, ev3msg.ToDocument());
            Assert.AreEqual(ev1.Id, ev3.Id);
            Assert.AreEqual("channel://foo.com/bar", ev3msg.Headers.DreamEventChannel);
            Assert.AreEqual("http://foo.com/baz", ev3msg.Headers.DreamEventOrigin[0]);
            string[] recipients = ev3msg.Headers.DreamEventRecipients;
            Assert.AreEqual(2, recipients.Length);
            Assert.AreEqual(r2.ToString(), recipients[0]);
            Assert.AreEqual(r3.ToString(), recipients[1]);
        }

        [Test]
        public void Subscription_auto_attaches_id() {
            XDoc subDoc = new XDoc("subscription")
                .Elem("channel", "channel:///foo/bar/*")
                .Start("recipient").Elem("uri", "http:///foo/bar").End();
            PubSubSubscription sub = new PubSubSubscription(subDoc, null);
            Assert.IsFalse(string.IsNullOrEmpty(sub.Id));
        }

        [Test]
        public void SubscriptionSet_from_XDoc_and_back() {
            XUri owner = new XUri("http:///owner");
            XUri sub1chan1 = new XUri("channel:///foo/bar/*");
            XUri sub1chan2 = new XUri("channel:///foo/baz/*");
            XUri sub1resource = new XUri("http://resource/1/1");
            XUri sub1proxy = new XUri("http:///proxy");
            XUri sub1recep1 = new XUri("http:///recep1");
            XUri sub1recep2 = new XUri("http:///recep2");
            XUri sub2chan1 = new XUri("channel:///foo/bar/baz");
            XUri sub2resource1 = new XUri("http://resource/2/1");
            XUri sub2resource2 = new XUri("http://resource/2/2");
            XUri sub2recep1 = new XUri("http:///recep1");
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            XDoc setDoc = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", owner)
                .Start("subscription")
                    .Attr("id", "123")
                    .Elem("channel", sub1chan1)
                    .Elem("channel", sub1chan2)
                    .Elem("uri.resource", sub1resource)
                    .Add(cookie.AsSetCookieDocument)
                    .Elem("uri.proxy", sub1proxy)
                    .Start("recipient").Attr("auth-token", "abc").Elem("uri", sub1recep1).End()
                    .Start("recipient").Attr("auth-token", "def").Elem("uri", sub1recep2).End()
                .End()
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", sub2chan1)
                    .Elem("uri.resource", sub2resource1)
                    .Elem("uri.resource", sub2resource2)
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", sub2recep1).End()
                .End();
            PubSubSubscriptionSet set = new PubSubSubscriptionSet(setDoc);
            Assert.AreEqual(1, set.MaxFailures);
            Assert.AreEqual(owner, set.Owner);
            Assert.IsFalse(string.IsNullOrEmpty(set.Location));
            Assert.AreEqual(2, set.Subscriptions.Length);
            PubSubSubscription sub1 = set.Subscriptions[0];
            Assert.IsFalse(string.IsNullOrEmpty(sub1.Id));
            Assert.AreEqual(cookie, sub1.Cookie);
            Assert.AreEqual(sub1chan1, sub1.Channels[0]);
            Assert.AreEqual(sub1chan2, sub1.Channels[1]);
            Assert.AreEqual(sub1proxy, sub1.Destination);
            PubSubSubscription sub2 = set.Subscriptions[1];
            Assert.IsFalse(string.IsNullOrEmpty(sub2.Id));
            Assert.AreEqual(1, sub2.Channels.Length);
            Assert.AreEqual(sub2chan1, sub2.Channels[0]);
            Assert.AreEqual(sub2recep1, sub2.Destination);
            XDoc setDoc2 = set.AsDocument();
            Assert.AreEqual(setDoc, setDoc2);
        }

        [Test]
        public void SubscriptionSet_Cookies_collapse_to_unique_set() {
            DreamCookie cookie1 = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"), DateTime.MaxValue);
            DreamCookie cookie2 = DreamCookie.NewSetCookie("foop", "baz", new XUri("http://xyz/abc/"), DateTime.MaxValue);
            XDoc setDoc = new XDoc("subscription-set")
                .Elem("uri.owner", "http:///owner")
                .Start("subscription")
                    .Elem("channel", "channel:///foo")
                    .Add(cookie1.AsSetCookieDocument)
                    .Start("recipient").Elem("uri", "http:///recipient").End()
                .End()
                .Start("subscription")
                    .Elem("channel", "channel:///foo")
                    .Add(cookie2.AsSetCookieDocument)
                    .Start("recipient").Elem("uri", "http:///recipient").End()
                .End()
                .Start("subscription")
                    .Elem("channel", "channel:///foo")
                    .Add(cookie1.AsSetCookieDocument)
                    .Start("recipient").Elem("uri", "http:///recipient").End()
                .End()
                .Start("subscription")
                    .Elem("channel", "channel:///foo")
                    .Add(cookie2.AsSetCookieDocument)
                    .Start("recipient").Elem("uri", "http:///recipient").End()
                .End()
                .Start("subscription")
                    .Elem("channel", "channel:///foo")
                    .Add(cookie1.AsSetCookieDocument)
                    .Start("recipient").Elem("uri", "http:///recipient").End()
                .End();
            PubSubSubscriptionSet set = new PubSubSubscriptionSet(setDoc);
            Assert.AreEqual(2, set.Cookies.Count);
            Assert.IsTrue(set.Cookies.Contains(cookie1));
            Assert.IsTrue(set.Cookies.Contains(cookie2));
        }

        [Test]
        public void SubscriptionSet_combination_uses_provided_cookie_for_all() {
            DreamCookie cookie1 = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"), DateTime.MaxValue);
            DreamCookie cookie2 = DreamCookie.NewSetCookie("foop", "baz", new XUri("http://xyz/abc/"), DateTime.MaxValue);
            DreamCookie cookie3 = DreamCookie.NewSetCookie("foox", "barxx", new XUri("http://xyz/abc/"), DateTime.MaxValue);
            DreamCookie cookie4 = DreamCookie.NewSetCookie("foopx", "bazxx", new XUri("http://xyz/abc/"), DateTime.MaxValue);
            XDoc setDoc1 = new XDoc("subscription-set")
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Elem("channel", "channel:///foo1")
                    .Add(cookie1.AsSetCookieDocument)
                    .Start("recipient").Elem("uri", "http:///recipient1").End()
                .End()
                .Start("subscription")
                    .Elem("channel", "channel:///foo2")
                    .Add(cookie2.AsSetCookieDocument)
                    .Start("recipient").Elem("uri", "http:///recipient2").End()
                .End();
            PubSubSubscriptionSet set1 = new PubSubSubscriptionSet(setDoc1);
            XDoc setDoc2 = new XDoc("subscription-set")
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Elem("channel", "channel:///foo3")
                    .Add(cookie3.AsSetCookieDocument)
                    .Start("recipient").Elem("uri", "http:///recipient3").End()
                .End()
                .Start("subscription")
                    .Elem("channel", "channel:///foo4")
                    .Add(cookie4.AsSetCookieDocument)
                    .Start("recipient").Elem("uri", "http:///recipient4").End()
                .End();
            PubSubSubscriptionSet set2 = new PubSubSubscriptionSet(setDoc2);
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            PubSubSubscriptionSet combinedSet = new PubSubSubscriptionSet(
                new XUri("http:///combined"),
                0,
                cookie,
                set1.Subscriptions[0],
                set1.Subscriptions[1],
                set2.Subscriptions[0],
                set2.Subscriptions[1]);
            Assert.AreEqual(4, combinedSet.Subscriptions.Length);
            Assert.AreEqual(1, combinedSet.Cookies.Count);
            Assert.AreEqual(cookie, combinedSet.Cookies[0]);
        }

        [Test]
        public void SubscriptionSet_combination_splits_multichannel_subs() {
            XUri owner = new XUri("http:///owner");
            XUri c1 = new XUri("channel:///c1");
            XUri c2 = new XUri("channel:///c2");
            XUri c3 = new XUri("channel:///c3");
            XUri r1 = new XUri("http:///r1");
            PubSubSubscription sub = new PubSubSubscription(
                new XDoc("subscription")
                    .Attr("id", "123")
                    .Elem("channel", c1)
                    .Elem("channel", c2)
                    .Elem("channel", c3)
                    .Elem("uri.proxy", "http:///proxy")
                    .Start("recipient").Attr("auth-token", "abc").Elem("uri", r1).End()
                    .Start("recipient").Attr("auth-token", "def").Elem("uri", "http:///r2").End()
                , null
                );
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            PubSubSubscriptionSet combinedSet = new PubSubSubscriptionSet(owner, 0, cookie, sub);
            Assert.AreEqual(3, combinedSet.Subscriptions.Length);
            PubSubSubscription subx = combinedSet.Subscriptions[0];
            Assert.AreEqual(owner.At("publish"), subx.Destination);
            Assert.AreEqual(1, subx.Channels.Length);
            Assert.AreEqual(c1, subx.Channels[0]);
            Assert.AreEqual(2, subx.Recipients.Length);
            Assert.AreEqual(r1, subx.Recipients[0].Uri);
            subx = combinedSet.Subscriptions[1];
            Assert.AreEqual(owner.At("publish"), subx.Destination);
            Assert.AreEqual(1, subx.Channels.Length);
            Assert.AreEqual(c2, subx.Channels[0]);
            Assert.AreEqual(2, subx.Recipients.Length);
            Assert.AreEqual(r1, subx.Recipients[0].Uri);
        }

        [Test]
        public void SubscriptionSet_combination_merges_subs_for_same_channel() {
            XUri owner = new XUri("http:///owner");
            XUri c1 = new XUri("channel:///c1");
            XUri c2 = new XUri("channel:///c2");
            XUri c3 = new XUri("channel:///c3");
            XDoc x1 = new XDoc("rule").Value("v1");
            XDoc x2 = new XDoc("rule").Value("v2");
            XDoc x3 = new XDoc("super-custom-filter").Elem("foo", "bar");

            XUri r1 = new XUri("http:///r1");
            XUri r2 = new XUri("http:///r2");
            PubSubSubscription sub1 = new PubSubSubscription(
                new XDoc("subscription")
                    .Attr("id", "123")
                    .Elem("channel", c1)
                    .Elem("channel", c2)
                    .Elem("uri.proxy", "http:///proxy")
                    .Start("recipient").Attr("auth-token", "abc").Elem("uri", r1).End()
                , null
                );
            PubSubSubscription sub2 = new PubSubSubscription(
                new XDoc("subscription")
                    .Attr("id", "123")
                    .Elem("channel", c1)
                    .Elem("channel", c3)
                    .Elem("uri.proxy", "http:///proxy")
                    .Start("recipient").Attr("auth-token", "abc").Elem("uri", r2).End()
                , null
                );
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            PubSubSubscriptionSet combinedSet = new PubSubSubscriptionSet(owner, 0, cookie, sub1, sub2);
            Assert.AreEqual(3, combinedSet.Subscriptions.Length);
            foreach(PubSubSubscription subx in combinedSet.Subscriptions) {
                switch(subx.Channels[0].ToString()) {
                case "channel:///c1":
                    Assert.AreEqual(owner.At("publish"), subx.Destination);
                    Assert.AreEqual(1, subx.Channels.Length);
                    Assert.AreEqual(c1, subx.Channels[0]);
                    Assert.AreEqual(2, subx.Recipients.Length);
                    bool foundR1 = false;
                    bool foundR2 = false;
                    foreach(DispatcherRecipient r in subx.Recipients) {
                        if(r.Uri == r1) {
                            foundR1 = true;
                        } else if(r.Uri == r2) {
                            foundR2 = true;
                        }
                    }
                    Assert.IsTrue(foundR1 && foundR2);
                    break;
                case "channel:///c2":
                    Assert.AreEqual(owner.At("publish"), subx.Destination);
                    Assert.AreEqual(1, subx.Channels.Length);
                    Assert.AreEqual(c2, subx.Channels[0]);
                    Assert.AreEqual(1, subx.Recipients.Length);
                    Assert.AreEqual(r1, subx.Recipients[0].Uri);
                    break;
                case "channel:///c3":
                    Assert.AreEqual(owner.At("publish"), subx.Destination);
                    Assert.AreEqual(1, subx.Channels.Length);
                    Assert.AreEqual(c3, subx.Channels[0]);
                    Assert.AreEqual(1, subx.Recipients.Length);
                    Assert.AreEqual(r2, subx.Recipients[0].Uri);
                    break;
                default:
                    Assert.Fail();
                    break;
                }
            }
        }

        [Test]
        public void SubscriptionSet_combined_set_should_not_include_pubsub_channel_subscriptions() {
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            PubSubSubscription sub1 = new PubSubSubscription(
                new XDoc("subscription")
                    .Elem("channel", "pubsub:///foo1")
                    .Add(cookie.AsSetCookieDocument)
                    .Start("recipient").Elem("uri", "http:///recipient1").End(),
                null);
            PubSubSubscriptionSet pubsubset = new PubSubSubscriptionSet(new XUri("http:///owner"), 0, cookie, sub1);
            Assert.AreEqual(0, pubsubset.Subscriptions.Length);
            PubSubSubscription sub2 = new PubSubSubscription(
                new XDoc("subscription")
                    .Elem("channel", "channel:///foo1")
                    .Add(cookie.AsSetCookieDocument)
                    .Start("recipient").Elem("uri", "http:///recipient1").End(),
                null);
            pubsubset = new PubSubSubscriptionSet(new XUri("http:///owner"), 0, cookie, sub1, sub2);
            Assert.AreEqual(1, pubsubset.Subscriptions.Length);
            Assert.AreEqual(1, pubsubset.Subscriptions[0].Channels.Length);
            Assert.AreEqual("channel:///foo1", pubsubset.Subscriptions[0].Channels[0].ToString());
        }

        [Test]
        public void SubscriptionSet_derive_with_older_version_returns_existing_Set() {
            XDoc setDoc1 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 10)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            PubSubSubscriptionSet set1 = new PubSubSubscriptionSet(setDoc1);
            Assert.AreEqual(10, set1.Version);
            XDoc setDoc2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 15)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            PubSubSubscriptionSet set2 = set1.Derive(setDoc2);
            Assert.AreEqual(15, set2.Version);
            Assert.AreNotSame(set1, set2);
            XDoc setDoc3 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 13)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            PubSubSubscriptionSet set3 = set2.Derive(setDoc3);
            Assert.AreEqual(15, set3.Version);
            Assert.AreSame(set2, set3);
        }

        [Test]
        public void SubscriptionSet_derive_with_same_version_returns_existing_Set() {
            XDoc setDoc1 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 10)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            PubSubSubscriptionSet set1 = new PubSubSubscriptionSet(setDoc1);
            Assert.AreEqual(10, set1.Version);
            XDoc setDoc2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 15)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            PubSubSubscriptionSet set2 = set1.Derive(setDoc2);
            Assert.AreEqual(15, set2.Version);
            Assert.AreNotSame(set1, set2);
            XDoc setDoc3 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 15)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            PubSubSubscriptionSet set3 = set2.Derive(setDoc3);
            Assert.AreEqual(15, set3.Version);
            Assert.AreSame(set2, set3);
        }


        [Test]
        public void SubscriptionSet_derive_with_no_version_always_creates_new_set() {
            XDoc setDoc1 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 10)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            PubSubSubscriptionSet set1 = new PubSubSubscriptionSet(setDoc1);
            Assert.AreEqual(10, set1.Version);
            XDoc setDoc2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            PubSubSubscriptionSet set2 = set1.Derive(setDoc2);
            Assert.IsFalse(set2.Version.HasValue);
            Assert.AreNotSame(set1, set2);
        }

        [Test]
        public void Dispatcher_creates_set_at_location() {
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
            Tuplet<PubSubSubscriptionSet, bool> location = dispatcher.RegisterSet(subset);
            Assert.IsFalse(location.Item2);
            PubSubSubscriptionSet set = dispatcher[location.Item1.Location];
            Assert.AreEqual(subset, set.AsDocument());
            Tuplet<PubSubSubscriptionSet, bool> location2 = dispatcher.RegisterSet(subset);
            Assert.IsTrue(location2.Item2);
            Assert.AreEqual(location.Item1.Location, location2.Item1.Location);
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
            Tuplet<PubSubSubscriptionSet, bool> location = dispatcher.RegisterSet(subset);
            XDoc subset2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///ownerx")
                .Start("subscription")
                .Attr("id", "123")
                .Elem("channel", "channel:///foo")
                .Start("recipient").Attr("auth-token", "abc").Elem("uri", "mailto://foo@bar.com").End()
                .End();
            try {
                dispatcher.ReplaceSet(location.Item1.Location, subset2);
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
            Assert.IsNull(dispatcher.ReplaceSet("ABCD", subset));

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
            Tuplet<PubSubSubscriptionSet, bool> location = dispatcher.RegisterSet(subset);
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
            dispatcher.RegisterSet(
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
                    .End());
            dispatcher.RegisterSet(
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
                    .End());

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
            dispatcher.RegisterSet(
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                        .Attr("id", "1")
                        .Elem("channel", "event://sales.mindtouch.com/deki/comments/create")
                        .Elem("channel", "event://sales.mindtouch.com/deki/comments/update")
                        .Start("recipient").Elem("uri", testUri.At("sub1")).End()
                    .End());
            dispatcher.RegisterSet(
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner2")
                    .Start("subscription")
                        .Attr("id", "3")
                        .Elem("channel", "event://*/deki/comments/create")
                        .Elem("channel", "event://*/deki/comments/update")
                        .Start("recipient").Elem("uri", testUri.At("sub2")).End()
                    .End());

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
            dispatcher.RegisterSet(
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                        .Attr("id", "1")
                        .Elem("channel", "event://sales.mindtouch.com/deki/comments/create")
                        .Elem("channel", "event://sales.mindtouch.com/deki/comments/update")
                        .Elem("uri.proxy", testUri)
                        .Start("recipient").Elem("uri", testUri.At("sub1")).End()
                    .End());
            dispatcher.RegisterSet(
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner2")
                    .Start("subscription")
                        .Attr("id", "3")
                        .Elem("channel", "event://*/deki/comments/create")
                        .Elem("channel", "event://*/deki/comments/update")
                        .Elem("uri.proxy", testUri)
                        .Start("recipient").Elem("uri", testUri.At("sub2")).End()
                    .End());

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
            dispatcher.RegisterSet(
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
                    .End());
            dispatcher.RegisterSet(
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
                    .End());

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
            var location1 = dispatcher.RegisterSet(new XDoc("subscription-set")
                .Attr("max-failures", 0)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", sub1Uri).End()
                .End()).Item1.Location;
            var location2 = dispatcher.RegisterSet(new XDoc("subscription-set")
                .Attr("max-failures", 0)
                .Elem("uri.owner", "http:///owner2")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", sub2Uri).End()
                .End()).Item1.Location;
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
                // ReSharper restore AccessToModifiedClosure
                resetEvent.Set();
                // ReSharper disable AccessToModifiedClosure
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
            string location = dispatcher.RegisterSet(
                new XDoc("subscription-set")
                    .Attr("max-failures", 1)
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                        .Attr("id", "1")
                        .Elem("channel", "channel:///foo/*")
                        .Start("recipient").Elem("uri", testUri.At("foo")).End()
                    .End()).Item1.Location;
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
            int workers;
            int io;
            ThreadPool.GetAvailableThreads(out workers, out io);
            _log.DebugFormat("threadpool threads: {0}/{1}", workers, io);
            string proxyRecipient1 = "mailto:///userA@foo.com";
            string proxyRecipient2 = "mailto:///userC@foo.com";
            XDoc msg = new XDoc("foo");
            DispatcherEvent ev = new DispatcherEvent(
                msg,
                new XUri("channel:///foo/bar"),
                new XUri("http://foobar.com/some/page"))
                .WithRecipient(false,
                    new DispatcherRecipient(new XUri(proxyRecipient1)),
                    new DispatcherRecipient(new XUri("mailto:///userB@foo.com")),
                    new DispatcherRecipient(new XUri(proxyRecipient2)));
            Dictionary<XUri, DreamMessage> dispatches = new Dictionary<XUri, DreamMessage>();
            XUri testUri = new XUri("http:///").At(StringUtil.CreateAlphaNumericKey(4));
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            MockPlug.Register(testUri, delegate(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
                dispatches.Add(plug.Uri, request);
                resetEvent.Set();
                response.Return(DreamMessage.Ok());
            });
            Plug owner = Plug.New("mock:///pubsub");
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://xyz/abc/"));
            Dispatcher dispatcher = new Dispatcher(new DispatcherConfig { ServiceUri = owner, ServiceAccessCookie = cookie });
            AutoResetEvent setResetEvent = new AutoResetEvent(false);
            dispatcher.CombinedSetUpdated += delegate {
                _log.DebugFormat("set updated");
                setResetEvent.Set();
            };

            XUri proxy = testUri.At("proxy");
            _log.DebugFormat("registering set");
            dispatcher.RegisterSet(
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
                    .End());

            //Set updates happen asynchronously, so give it a chance
            _log.DebugFormat("giving registration a chance to manifest");
            Assert.IsTrue(setResetEvent.WaitOne(10000, false));
            _log.DebugFormat("dispatching event");
            dispatcher.Dispatch(ev);

            // dispatch happens async on a worker thread
            Assert.IsTrue(resetEvent.WaitOne(1000, false));
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
            dispatcher.RegisterSet(
                new XDoc("subscription-set")
                    .Elem("uri.owner", "http:///owner1")
                    .Start("subscription")
                        .Attr("id", "1")
                        .Elem("channel", "channel:///foo/*")
                        .Start("recipient").Elem("uri", testUri).End()
                    .End());

            // combinedset updates happen asynchronously, so give'em a chance
            Assert.IsTrue(setResetEvent.WaitOne(10000, false));
            dispatcher.Dispatch(ev);

            // dispatch happens async on a worker thread
            Assert.IsTrue(resetEvent.WaitOne(10000, false));
            MockPlug.Deregister(testUri);
        }

        [Test]
        public void Recipient_can_be_used_as_Dictionary_key() {
            Dictionary<DispatcherRecipient, string> dictionary = new Dictionary<DispatcherRecipient, string>();
            DispatcherRecipient r1 = new DispatcherRecipient(new XUri("http://foo.com/bar"));
            DispatcherRecipient r2 = new DispatcherRecipient(new XUri("http://foo.com/baz"));
            DispatcherRecipient r3 = new DispatcherRecipient(new XUri("http://foop.com/bar"));
            dictionary.Add(r1, "r1");
            dictionary.Add(r2, "r2");
            dictionary.Add(r3, "r3");
            Assert.AreEqual("r1", dictionary[r1]);
            Assert.AreEqual("r2", dictionary[r2]);
            Assert.AreEqual("r3", dictionary[r3]);
            DispatcherRecipient r1_1 = new DispatcherRecipient(new XUri("http://foo.com/bar"));
            Assert.AreEqual("r1", dictionary[r1_1]);
            DispatcherRecipient r1_2 = new DispatcherRecipient(new XDoc("recipient").Attr("foo", "bar").Elem("uri", "http://foo.com/bar").Elem("extra", "stuff"));
            Assert.AreEqual("r1", dictionary[r1_1]);
        }

        private bool WaitFor(Plug plug, Func<DreamMessage, bool> func, TimeSpan timeout) {
            var expire = DateTime.Now.Add(timeout);
            while(expire > DateTime.Now) {
                var r = plug.GetAsync().Wait();
                if(func(r)) {
                    return true;
                }
                Thread.Sleep(100);
            }
            return false;
        }

        private void InitHost() {
            if(_hostInfo != null) {
                return;
            }
            _hostInfo = DreamTestHelper.CreateRandomPortHost();
        }

        private DreamServiceInfo CreatePubSubService(XDoc extraConfig) {
            return CreatePubSubService(null, extraConfig);
        }

        private DreamServiceInfo CreatePubSubService(string namePrefix, XDoc extraConfig) {
            InitHost();
            return DreamTestHelper.CreateService(_hostInfo, "sid://mindtouch.com/dream/2008/10/pubsub", namePrefix, extraConfig);
        }

        private DreamServiceInfo CreatePubSubService() {
            return CreatePubSubService(null);
        }
    }

    public class MockDispatcher : IPubSubDispatcher {
        
        public static int Instantiations;
        
        public MockDispatcher() {
            Instantiations++;
        }

        public PubSubSubscriptionSet CombinedSet {
            get { throw new NotImplementedException(); }
        }

        public PubSubSubscriptionSet this[string location] {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<PubSubSubscriptionSet> GetAllSubscriptionSets() {
            throw new NotImplementedException();
        }

        public Tuplet<PubSubSubscriptionSet, bool> RegisterSet(XDoc setDoc) {
            throw new NotImplementedException();
        }

        public void Dispatch(DispatcherEvent ev) {
            throw new NotImplementedException();
        }

        public PubSubSubscriptionSet ReplaceSet(string location, XDoc setDoc) {
            throw new NotImplementedException();
        }

        public bool RemoveSet(string location) {
            throw new NotImplementedException();
        }

        public event EventHandler<EventArgs> CombinedSetUpdated;
    }
}
