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
    public class PubSubServiceTests {

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
            CreatePubSubService("upstream",
                new XDoc("config")
                    .Start("components")
                        .Start("component")
                            .Attr("implementation", typeof(MockDispatcher).AssemblyQualifiedName)
                            .Attr("type", typeof(IPubSubDispatcher).AssemblyQualifiedName)
                        .End()
                    .End()
            );
            Assert.AreEqual(1, MockDispatcher.Instantiations);
        }

        [Test]
        public void Can_inject_custom_dispatch_queue() {
            CreatePubSubService("upstream",
                new XDoc("config")
                    .Start("components")
                        .Start("component")
                            .Attr("implementation", typeof(MockPubSubDispatchQueueRepository).AssemblyQualifiedName)
                            .Attr("type", typeof(IPubSubDispatchQueueRepository).AssemblyQualifiedName)
                        .End()
                    .End()
            );
            Assert.AreEqual(1, MockPubSubDispatchQueueRepository.Instantiations);
            Assert.AreEqual(1, MockPubSubDispatchQueueRepository.InitCalled);
            Assert.AreEqual(1, MockPubSubDispatchQueueRepository.GetUninitializedSetsCalled);
        }

        [Test]
        public void Trying_to_retrieve_subscription_at_an_unknown_location_should_return_Forbidden() {
            Plug pubsub = CreatePubSubService().WithInternalKey().AtLocalHost;
            DreamMessage response = pubsub.At("subscribers", "ABCD").GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }

        [Test]
        public void Register_retrieve_update_and_delete_subscription_on_pubsub_service_at_provided_location() {
            var locationKey = "custom-location";
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
            DreamMessage response = pubsub.At("subscribers").WithHeader("X-Set-Location-Key",locationKey).Post(set, new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Created, response.Status);
            Assert.IsNull(response.Headers.ContentLocation);
            Assert.IsNotNull(response.Headers.Location);
            XUri location = response.Headers.Location;
            Assert.AreEqual(location.LastSegment, locationKey);
            XDoc subDoc = response.ToDocument();
            string accessKey = subDoc["access-key"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(accessKey));
            Assert.AreEqual(location, subDoc["uri.location"].AsUri.With("access-key", accessKey));
            Plug subscription = Plug.New(location);

            // retrieve subscription
            response = subscription.Get(new Result<DreamMessage>()).Wait();
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
            response = subscription.Put(set2,new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);

            // retrieve new subscription
            response = subscription.Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(set2, response.ToDocument());

            // delete subscription
            response = subscription.Delete(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(0, Plug.GlobalCookies.Fetch(subscription.Uri).Count);

            // check it's really gone (or least no longer accessible
            response = subscription.Get(new Result<DreamMessage>()).Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }

        [Test]
        public void Can_register_set_with_provided_access_key() {
            var accessKey = "provided-key";
            var set = new XDoc("subscription-set")
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
            var pubsub = CreatePubSubService().WithInternalKey().AtLocalHost;
            var response = pubsub.At("subscribers").WithHeader("X-Set-Access-Key", accessKey).Post(set, new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Created, response.Status);
            Assert.IsNull(response.Headers.ContentLocation);
            Assert.IsNotNull(response.Headers.Location);
            var location = response.Headers.Location;
            var subDoc = response.ToDocument();
            Assert.AreEqual(accessKey, subDoc["access-key"].AsText);
            Assert.IsFalse(string.IsNullOrEmpty(accessKey));
            Assert.AreEqual(location, subDoc["uri.location"].AsUri.With("access-key", accessKey));
            var subscription = Plug.New(location);

            // retrieve subscription
            response = subscription.Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(set, response.ToDocument());

            // replace subscription
            var set2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End();
            response = subscription.Put(set2, new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);

            // retrieve new subscription
            response = subscription.Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(set2, response.ToDocument());

            // delete subscription
            response = subscription.Delete(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(0, Plug.GlobalCookies.Fetch(subscription.Uri).Count);

            // check it's really gone (or least no longer accessible
            response = subscription.Get(new Result<DreamMessage>()).Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }

        [Test]
        public void Can_update_access_key_on_existing_subscription() {
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
            DreamMessage response = pubsub.At("subscribers").Post(set, new Result<DreamMessage>()).Wait();
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

            // replace subscription
            var newAccessKey = "provided-key";
            var set2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End();
            response = subscription.WithHeader("X-Set-Access-Key", newAccessKey).Put(set2, new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);

            // retrieve new subscription with old key
            response = subscription.Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);

            // retrieve new subscription with new key
            subscription = Plug.New(location.WithoutQuery().With("access-key", newAccessKey));
            response = subscription.Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(set2, response.ToDocument());

            // delete subscription
            response = subscription.Delete(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(0, Plug.GlobalCookies.Fetch(subscription.Uri).Count);

            // check it's really gone (or least no longer accessible
            response = subscription.Get(new Result<DreamMessage>()).Wait();
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
            PubSubSubscriptionSet combinedSet = new PubSubSubscriptionSet(response.ToDocument(), "abc", "def");
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
            DreamMessage response = pubsub.At("subscribers", "ABCD").Put(set, new Result<DreamMessage>()).Wait();
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
            DreamMessage response = pubsub.At("subscribers").Post(set, new Result<DreamMessage>()).Wait();
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
            response = subscription.Put(set2, new Result<DreamMessage>()).Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }

        [Test]
        public void Put_sub_with_wrong_access_key_should_be_Forbidden() {
            XDoc set = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End();

            // create subscription
            var pubsub = CreatePubSubService().WithInternalKey().AtLocalHost;
            var response = pubsub.At("subscribers").Post(set, new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Created, response.Status);
            Assert.IsNull(response.Headers.ContentLocation);
            Assert.IsNotNull(response.Headers.Location);
            var location = response.Headers.Location.WithoutQuery();
            var subscription = Plug.New(location).WithCookieJar(pubsub.CookieJar);

            // replace subscription
            var set2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http:///owner1")
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", "http:///foo/sub1").End()
                .End();
            response = subscription
                .With("access-key", "wrongkey")
                .Put(set2, new Result<DreamMessage>()).Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }

        [Test]
        public void Publish_against_pubsub_channel_should_be_forbidden() {
            InitHost();
            // publish event via a mock service, since publish is marked internal
            var mock = MockService.CreateMockService(_hostInfo);
            var resetEvent = new AutoResetEvent(false);
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
}
