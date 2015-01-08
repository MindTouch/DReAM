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
using System.Linq;
using System.Threading;
using log4net;
using MindTouch.Collections;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class ExpiringDictionaryTests {

        private static readonly ILog _log = LogUtils.CreateLog();


        [SetUp]
        public void Setup() {
            var reset = new ManualResetEvent(false);
            var timer = TaskTimerFactory.Current.New(GlobalClock.UtcNow, (t) => {
                _log.Debug("warm up");
                reset.Set();
            }, null, TaskEnv.Current);
            reset.WaitOne(1000);
        }
        [Test]
        public void Can_set_item_via_ttl() {
            _log.Debug("running test");
            var expired = new ManualResetEvent(false);
            var changed = new ManualResetEvent(false);
            var k = 42;
            var v = "foo";
            var ttl = TimeSpan.FromSeconds(1);
            ExpiringDictionary<int, string>.Entry entry = null;
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entry = e.Entry; expired.Set(); };
            set.CollectionChanged += (s, e) => changed.Set();
            set.Set(k, v, ttl);
            Assert.IsTrue(changed.WaitOne(5000));
            Assert.IsTrue(expired.WaitOne(5000));
            Assert.AreEqual(k, entry.Key);
            Assert.AreEqual(v, entry.Value);
            Assert.AreEqual(ttl.WithoutMilliseconds(), entry.TTL.WithoutMilliseconds());
            Assert.IsNull(set[k]);
        }

        [Test]
        public void Clearing_set_releases_all_items() {
            var changed = false;
            var ttl = 10.Seconds();
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.Set(1, "a", ttl);
            set.Set(2, "b", ttl);
            set.Set(3, "v", ttl);
            set.CollectionChanged += (s, e) => changed = true;
            Assert.AreEqual(3, set.Count());
            Assert.IsFalse(changed);
            set.Clear();
            Assert.AreEqual(0, set.Count());
            Assert.IsTrue(changed);
        }

        [Test]
        public void Can_access_dictionary_during_expiration_event() {
            var expired = new ManualResetEvent(false);
            var changed = new ManualResetEvent(false);
            var k = 42;
            var v = "foo";
            var ttl = TimeSpan.FromSeconds(1);
            ExpiringDictionary<int, string>.Entry entry = null;
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => {
                entry = e.Entry;
                set.Delete(e.Entry.Key);
                expired.Set();
            };
            set.CollectionChanged += (s, e) => changed.Set();
            set.Set(k, v, ttl);
            Assert.IsTrue(changed.WaitOne(5000));
            Assert.IsTrue(expired.WaitOne(5000));
            Assert.AreEqual(k, entry.Key);
            Assert.AreEqual(v, entry.Value);
            Assert.AreEqual(ttl.WithoutMilliseconds(), entry.TTL.WithoutMilliseconds());
            Assert.IsNull(set[k]);
        }

        [Test]
        public void Can_set_item_via_when() {
            var expired = new ManualResetEvent(false);
            var changed = new ManualResetEvent(false);
            var k = 42;
            var v = "foo";
            var when = GlobalClock.UtcNow.AddSeconds(1);
            ExpiringDictionary<int, string>.Entry entry = null;
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entry = e.Entry; expired.Set(); };
            set.CollectionChanged += (s, e) => changed.Set();
            set.Set(k, v, when);
            Assert.IsTrue(changed.WaitOne(5000));
            Assert.IsTrue(expired.WaitOne(5000));
            Assert.AreEqual(k, entry.Key);
            Assert.AreEqual(v, entry.Value);
            Assert.AreEqual(when.WithoutMilliseconds(), entry.When.WithoutMilliseconds());
            Assert.IsNull(set[k]);

        }

        [Test]
        public void Can_retrieve_item_checking_when() {
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            var k = 42;
            var v = "foo";
            var when = GlobalClock.UtcNow.AddDays(1);
            set.Set(k, v, when);
            var entry = set[k];
            Assert.AreEqual(k, entry.Key);
            Assert.AreEqual(v, entry.Value);
            Assert.AreEqual(when.WithoutMilliseconds(), entry.When.WithoutMilliseconds());
        }

        [Test]
        public void Can_retrieve_item_checking_ttl() {
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            var k = 42;
            var v = "foo";
            var ttl = TimeSpan.FromSeconds(10);
            set.Set(k, v, ttl);
            var entry = set[k];
            Assert.AreEqual(k, entry.Key);
            Assert.AreEqual(v, entry.Value);
            Assert.AreEqual(ttl.WithoutMilliseconds(), entry.TTL.WithoutMilliseconds());
        }

        [Test]
        public void Disposing_set_expires_all_items_before_dispose_returns_but_does_not_trigger_collection_changed() {
            var expired = false;
            var changed = false;
            var expiredEntries = new List<string>();
            var ttl = TimeSpan.FromSeconds(1);
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { expiredEntries.Add(e.Entry.Key + ":" + e.Entry.Value); expired = true; };
            set.CollectionChanged += (s, e) => { changed = true; };
            set.Set(12, "foo", ttl);
            set.Set(21, "bar", ttl);
            Assert.IsFalse(expired, "expired was triggered");
            Assert.IsTrue(changed, "changed wasn't triggered");
            changed = false;
            set.Dispose();
            Assert.IsFalse(changed, "changed was triggered");
            Assert.IsTrue(expired, "expired wasn't triggered");
            Assert.AreEqual(new[] { "12:foo", "21:bar" }, expiredEntries.OrderBy(x => x).ToArray());
        }

        [Test]
        public void Can_set_ttl_on_existing_item() {
            var expired = new ManualResetEvent(false);
            var changed = new ManualResetEvent(false);
            var k = 42;
            var v = "foo";
            var ttl = TimeSpan.FromSeconds(10);
            ExpiringDictionary<int, string>.Entry entry = null;
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entry = e.Entry; expired.Set(); };
            set.CollectionChanged += (s, e) => changed.Set();
            set.Set(k, v, ttl);
            Assert.IsTrue(changed.WaitOne(2000));
            changed.Reset();
            Assert.IsFalse(expired.WaitOne(2000));
            set.SetExpiration(k, TimeSpan.FromSeconds(1));
            Assert.IsTrue(changed.WaitOne(5000));
            Assert.IsTrue(expired.WaitOne(5000));
        }

        [Test]
        public void Can_iterate_over_items() {
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            var n = 10;
            for(var i = 1; i <= n; i++) {
                set.Set(i, "v" + i, TimeSpan.FromSeconds(i));
            }
            var items = from x in set select x;
            Assert.AreEqual(n, items.Count());
            for(var i = 1; i <= n; i++) {
                // ReSharper disable AccessToModifiedClosure
                Assert.IsTrue((from x in set where x.Key == i && x.Value == "v" + x.Key && x.TTL.WithoutMilliseconds() == TimeSpan.FromSeconds(i) select x).Any());
                // ReSharper restore AccessToModifiedClosure
            }
        }

        [Test]
        public void Can_reset_expiration() {
            var expired = new ManualResetEvent(false);
            var changed = new ManualResetEvent(false);
            var k = 42;
            var v = "foo";
            var ttl = TimeSpan.FromSeconds(2);
            ExpiringDictionary<int, string>.Entry entry = null;
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entry = e.Entry; expired.Set(); };
            set.CollectionChanged += (s, e) => changed.Set();
            set.Set(k, v, ttl);
            Assert.IsTrue(changed.WaitOne(500));
            changed.Reset();
            Thread.Sleep(500);
            Assert.IsFalse(expired.WaitOne(500));
            var oldExpire = set[k].When;
            set.RefreshExpiration(k);
            Assert.Greater(set[k].When.WithoutMilliseconds(), oldExpire.WithoutMilliseconds());
            Assert.IsTrue(changed.WaitOne(5000));
            Assert.IsTrue(expired.WaitOne(5000));
        }

        [Test]
        public void Can_remove_expiration() {
            var expired = new ManualResetEvent(false);
            var changed = new ManualResetEvent(false);
            var k = 42;
            var v = "foo";
            var ttl = TimeSpan.FromSeconds(1);
            ExpiringDictionary<int, string>.Entry entry = null;
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entry = e.Entry; expired.Set(); };
            set.CollectionChanged += (s, e) => changed.Set();
            set.Set(k, v, ttl);
            Assert.IsTrue(changed.WaitOne(2000));
            changed.Set();
            set.Delete(k);
            Assert.IsTrue(changed.WaitOne(2000));
            Assert.IsFalse(expired.WaitOne(2000));
        }

        [Test]
        public void Set_will_fire_in_order_of_expirations() {
            var expired = new ManualResetEvent(false);
            var keys = new List<int>();
            var values = new List<string>();
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => {
                keys.Add(e.Entry.Key);
                values.Add(e.Entry.Value);
                expired.Set();
            };
            set.Set(3, "c", TimeSpan.FromMilliseconds(1600));
            set.Set(2, "b", TimeSpan.FromMilliseconds(1500));
            set.Set(5, "e", TimeSpan.FromMilliseconds(3000));
            set.Set(4, "d", TimeSpan.FromMilliseconds(2000));
            set.Set(1, "a", TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(expired.WaitOne(1000));
            Assert.AreEqual(1, keys.Count);
            Assert.IsTrue(Wait.For(() => keys.Count == 5, TimeSpan.FromSeconds(5000)));
            Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, keys.ToArray());
            Assert.AreEqual(new[] { "a", "b", "c", "d", "e" }, values.ToArray());
        }

        [Test]
        public void New_item_with_more_recent_expiration_will_fire_at_expected_time() {
            var expired = new ManualResetEvent(false);
            var entries = new List<int>();
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entries.Add(e.Entry.Key); expired.Set(); };
            var n = 1000;
            for(var i = 0; i < n; i++) {
                set.Set(i, "v" + i, TimeSpan.FromMinutes(10));
            }
            set.Set(100000, "bar", TimeSpan.FromSeconds(2));
            Assert.IsTrue(expired.WaitOne(4000));
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(n, set.Count());
        }

        [Test]
        public void Access_on_tasktimerfactory_ctor_set_does_not_reset_expiration() {
            var k = 42;
            var v = "foo";
            var ttl = TimeSpan.FromSeconds(10);
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            var expireTime = GlobalClock.UtcNow.AddSeconds(10);
            set.Set(k, v, expireTime);
            Thread.Sleep(TimeSpan.FromSeconds(2));
            var entry = set[k];
            AssertEx.AreEqual(ttl, entry.TTL);
            Assert.AreEqual(expireTime, entry.When);
        }

        [Test]
        public void Access_on_autoRefresh_set_does_reset_expiration() {
            var k = 42;
            var v = "foo";
            var ttl = TimeSpan.FromSeconds(10);
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current, true);
            var expireTime = GlobalClock.UtcNow.AddSeconds(10);
            set.Set(k, v, expireTime);
            Thread.Sleep(TimeSpan.FromSeconds(2));
            var entry = set[k];
            Assert.AreEqual(Math.Round(ttl.TotalSeconds), Math.Round(entry.TTL.TotalSeconds));
            Assert.AreNotEqual(expireTime, entry.When);
            Assert.GreaterOrEqual(entry.When, expireTime);
            Assert.LessOrEqual(entry.When, expireTime.AddSeconds(3));
        }

        [Test]
        public void Additional_Set_on_same_key_updates_value() {
            var k = 42;
            var v1 = "foo";
            var v2 = "bar";
            var ttl = TimeSpan.FromSeconds(10);
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.Set(k, v1, ttl);
            set.Set(k, v2, ttl);
            Assert.AreEqual(v2, set[k].Value);
        }

        [Test]
        public void Update_changes_value() {
            var k = 42;
            var v1 = "foo";
            var v2 = "bar";
            var ttl = TimeSpan.FromSeconds(10);
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.Set(k, v1, ttl);
            Assert.IsTrue(set.Update(k, v2));
            Assert.AreEqual(v2, set[k].Value);
        }

        [Test]
        public void Refresh_in_autoRefresh_dictionary_only_fires_every_half_second() {
            var k = 42;
            var v = "foo";
            var ttl = TimeSpan.FromSeconds(10);
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current, true);
            var expireTime = GlobalClock.UtcNow.AddSeconds(10);
            set.Set(k, v, expireTime);
            var when = set[k].When;
            Thread.Sleep(200);
            Assert.AreEqual(when.WithoutMilliseconds(), set[k].When.WithoutMilliseconds());
            Thread.Sleep(1000);
            Assert.Less(when.WithoutMilliseconds(), set[k].When.WithoutMilliseconds());
        }

        [Test]
        public void Update_does_not_create_item() {
            var k = 42;
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            Assert.IsFalse(set.Update(k, "foo"));
            Assert.IsNull(set[k]);
        }

        [Test]
        public void SetExpiration_updates_expiration_time() {
            var k = 42;
            var v = "foo";
            var ttl = TimeSpan.FromSeconds(10);
            var ttl2 = TimeSpan.FromSeconds(50);
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.Set(k, v, ttl);
            Assert.IsTrue(set.SetExpiration(k, ttl2));
            Assert.AreEqual(ttl2.WithoutMilliseconds(), set[k].TTL.WithoutMilliseconds());
        }

        [Test]
        public void SetExpiration_does_not_create_item() {
            var k = 42;
            var ttl = TimeSpan.FromSeconds(10);
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            Assert.IsFalse(set.SetExpiration(k, ttl));
        }

        [Test]
        public void Adding_item_after_all_current_items_have_expired_expires_as_expected() {
            var expired = new AutoResetEvent(false);
            var set = new ExpiringDictionary<int, string>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => expired.Set();
            set.Set(1, "b", 2.Seconds());
            set.Set(2, "c", 1.Seconds());
            Assert.IsTrue(expired.WaitOne(2000));
            Assert.IsNull(set[2]);
            Assert.IsTrue(expired.WaitOne(2000));
            Assert.IsNull(set[1]);
            set.Set(3, "a", 500.Milliseconds());
            Assert.IsTrue(expired.WaitOne(2000));
            Assert.IsNull(set[3]);
        }
    }
}
