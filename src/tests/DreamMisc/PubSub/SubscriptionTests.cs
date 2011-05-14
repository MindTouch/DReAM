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
using MindTouch.Dream.Services.PubSub;
using MindTouch.Web;
using MindTouch.Xml;
using NUnit.Framework;
using MindTouch.Extensions.Time;

namespace MindTouch.Dream.Test.PubSub {

    [TestFixture]
    public class SubscriptionTests {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

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
            PubSubSubscriptionSet set = new PubSubSubscriptionSet(setDoc, "abc", "def");
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
        public void SubscriptionSet_without_ttl_never_expires() {
            XDoc setDoc = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "123")
                    .Elem("channel", "http://channel")
                    .Elem("uri.resource", "http://resource")
                    .Elem("uri.proxy", "http://proxy")
                    .Start("recipient").Elem("uri", "http:///recipient").End()
                .End();
            var set = new PubSubSubscriptionSet(setDoc, "abc", "def");
            Assert.IsFalse(set.HasExpiration, "set should not have an expiration");
        }

        [Test]
        public void SubscriptionSet_without_ttl_has_max_failures() {
            var setDoc = new XDoc("subscription-set")
                .Attr("max-failures", 42)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "123")
                    .Elem("channel", "http://channel")
                    .Elem("uri.resource", "http://resource")
                    .Elem("uri.proxy", "http://proxy")
                    .Start("recipient").Elem("uri", "http:///recipient").End()
                .End();
            var set = new PubSubSubscriptionSet(setDoc, "abc", "def");
            Assert.AreEqual(42, set.MaxFailures, "set max failures are wrong");
        }

        [Test]
        public void SubscriptionSet_with_ttl_expires() {
            var setDoc = new XDoc("subscription-set")
                .Attr("ttl",1)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "123")
                    .Elem("channel", "http://channel")
                    .Elem("uri.resource", "http://resource")
                    .Elem("uri.proxy", "http://proxy")
                    .Start("recipient").Elem("uri", "http:///recipient").End()
                .End();
            var set = new PubSubSubscriptionSet(setDoc, "abc", "def");
            Assert.IsTrue(set.HasExpiration, "set should have had an expiration");
            Assert.AreEqual(set.ExpirationTTL, 1.Seconds());
        }

        [Test]
        public void SubscriptionSet_with_ttl_has_no_max_failures() {
            var setDoc = new XDoc("subscription-set")
                .Attr("max-failures", 42)
                .Attr("ttl", 1)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "123")
                    .Elem("channel", "http://channel")
                    .Elem("uri.resource", "http://resource")
                    .Elem("uri.proxy", "http://proxy")
                    .Start("recipient").Elem("uri", "http:///recipient").End()
                .End();
            var set = new PubSubSubscriptionSet(setDoc, "abc", "def");
            Assert.AreEqual(int.MaxValue, set.MaxFailures);
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
            PubSubSubscriptionSet set = new PubSubSubscriptionSet(setDoc, "abc", "def");
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
            PubSubSubscriptionSet set1 = new PubSubSubscriptionSet(setDoc1, "abc", "def");
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
            PubSubSubscriptionSet set2 = new PubSubSubscriptionSet(setDoc2, "abc", "def");
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
            PubSubSubscriptionSet set1 = new PubSubSubscriptionSet(setDoc1, "abc", "def");
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
            PubSubSubscriptionSet set2 = set1.Derive(setDoc2, null);
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
            PubSubSubscriptionSet set3 = set2.Derive(setDoc3, null);
            Assert.AreEqual(15, set3.Version);
            Assert.AreSame(set2, set3);
        }

        [Test]
        public void SubscriptionSet_derive_with_same_version_returns_existing_Set() {
            var setDoc1 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 10)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            var set1 = new PubSubSubscriptionSet(setDoc1, "abc", "def");
            var setDoc2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 15)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            var set2 = set1.Derive(setDoc2, null);
            var setDoc3 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 15)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            var set3 = set2.Derive(setDoc3, null);
            Assert.AreEqual(10, set1.Version);
            Assert.AreEqual(15, set2.Version);
            Assert.AreNotSame(set1, set2);
            Assert.AreEqual(15, set3.Version);
            Assert.AreSame(set2, set3);
        }

        [Test]
        public void SubscriptionSet_derive_with_no_version_always_creates_new_set() {
            var setDoc1 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 10)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            var set1 = new PubSubSubscriptionSet(setDoc1, "abc", "def");
            var setDoc2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            var set2 = set1.Derive(setDoc2, null);
            Assert.AreEqual(10, set1.Version);
            Assert.IsFalse(set2.Version.HasValue);
            Assert.AreNotSame(set1, set2);
            Assert.AreEqual(set1.AccessKey, set2.AccessKey);
        }

        [Test]
        public void SubscriptionSet_derive_with_newer_version_always_creates_new_set() {
            var setDoc1 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Attr("version", 10)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            var set1 = new PubSubSubscriptionSet(setDoc1, "abc", "def");
            var setDoc2 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                 .Attr("version", 11)
               .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            var set2 = set1.Derive(setDoc2, null);
            Assert.AreEqual(10, set1.Version);
            Assert.AreEqual(11, set2.Version);
            Assert.AreNotSame(set1, set2);
            Assert.AreEqual(set1.AccessKey, set2.AccessKey);
        }

        [Test]
        public void SubscriptionSet_derive_with_access_key_changes_accesskey() {
            var setDoc1 = new XDoc("subscription-set")
                .Attr("max-failures", 1)
                .Elem("uri.owner", "http://owner")
                .Start("subscription")
                    .Attr("id", "456")
                    .Elem("channel", "http://chanel")
                   .Start("recipient").Attr("auth-token", "xyz").Elem("uri", "http://recipient").End()
                .End();
            var set1 = new PubSubSubscriptionSet(setDoc1, "abc", "def");
            var set2 = set1.Derive(setDoc1, "bob");
            Assert.AreNotSame(set1, set2);
            Assert.AreNotEqual(set1.AccessKey, set2.AccessKey);
        }
    }
}