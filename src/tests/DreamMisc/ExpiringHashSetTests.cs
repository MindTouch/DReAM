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
using log4net;
using MindTouch.Collections;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class ExpiringHashSetTests {

        private static readonly ILog _log = LogUtils.CreateLog();

        [Test]
        public void Can_set_item_via_ttl() {
            _log.Debug("running test");
            var expired = new ManualResetEvent(false);
            var changed = new ManualResetEvent(false);
            var i = 42;
            var ttl = TimeSpan.FromSeconds(1);
            ExpiringHashSet<int>.Entry entry = null;
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entry = e.Entry; expired.Set(); };
            set.CollectionChanged += (s, e) => changed.Set();
            set.SetExpiration(i, ttl);
            Assert.IsTrue(changed.WaitOne(5000));
            Assert.IsTrue(expired.WaitOne(5000));
            Assert.AreEqual(i, entry.Value);
            Assert.AreEqual(ttl, entry.TTL);
            Assert.IsNull(set[i]);
        }


        [Test]
        public void Clearing_set_releases_all_items() {
            var changed = false;
            var ttl = 10.Seconds();
            var set = new ExpiringHashSet<string>(TaskTimerFactory.Current);
            set.SetExpiration("a", ttl);
            set.SetExpiration("b", ttl);
            set.SetExpiration("v", ttl);
            set.CollectionChanged += (s, e) => changed = true;
            Assert.AreEqual(3, set.Count());
            Assert.IsFalse(changed);
            set.Clear();
            Assert.AreEqual(0, set.Count());
            Assert.IsTrue(changed);
        }

        [Test]
        public void Can_access_hashset_during_expiration_event() {
            var expired = new ManualResetEvent(false);
            var changed = new ManualResetEvent(false);
            var i = 42;
            var ttl = TimeSpan.FromSeconds(1);
            ExpiringHashSet<int>.Entry entry = null;
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => {
                entry = e.Entry;
                set.RemoveExpiration(e.Entry.Value);
                expired.Set();
            };
            set.CollectionChanged += (s, e) => changed.Set();
            set.SetExpiration(i, ttl);
            Assert.IsTrue(changed.WaitOne(5000));
            Assert.IsTrue(expired.WaitOne(5000));
            Assert.AreEqual(i, entry.Value);
            Assert.AreEqual(ttl, entry.TTL);
            Assert.IsNull(set[i]);
        }

        [Test]
        public void Can_set_item_via_when() {
            var expired = new ManualResetEvent(false);
            var changed = new ManualResetEvent(false);
            var i = 42;
            var when = DateTime.UtcNow.AddSeconds(1);
            ExpiringHashSet<int>.Entry entry = null;
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entry = e.Entry; expired.Set(); };
            set.CollectionChanged += (s, e) => changed.Set();
            set.SetExpiration(i, when);
            Assert.IsTrue(changed.WaitOne(5000));
            Assert.IsTrue(expired.WaitOne(5000));
            Assert.AreEqual(i, entry.Value);
            Assert.AreEqual(when, entry.When);
            Assert.IsNull(set[i]);

        }

        [Test]
        public void Can_retrieve_item_checking_when() {
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            var v = 42;
            var when = DateTime.UtcNow.AddDays(1);
            set.SetExpiration(v, when);
            var entry = set[v];
            Assert.AreEqual(v, entry.Value);
            Assert.AreEqual(when, entry.When);
        }

        [Test]
        public void Can_retrieve_item_checking_ttl() {
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            var v = 42;
            var ttl = TimeSpan.FromSeconds(10);
            set.SetExpiration(v, ttl);
            var entry = set[v];
            Assert.AreEqual(v, entry.Value);
            Assert.AreEqual(ttl, entry.TTL);
        }

        [Test]
        public void Can_dispose_set() {
            var expired = new ManualResetEvent(false);
            var changed = new ManualResetEvent(false);
            var i = 42;
            var ttl = TimeSpan.FromSeconds(1);
            ExpiringHashSet<int>.Entry entry = null;
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entry = e.Entry; expired.Set(); };
            set.CollectionChanged += (s, e) => changed.Set();
            set.SetExpiration(i, ttl);
            set.Dispose();
            Assert.IsTrue(changed.WaitOne(2000));
            Assert.IsFalse(expired.WaitOne(2000));
        }

        [Test]
        public void Can_set_ttl_on_existing_item() {
            var expired = new ManualResetEvent(false);
            var changed = new ManualResetEvent(false);
            var i = 42;
            var ttl = TimeSpan.FromSeconds(10);
            ExpiringHashSet<int>.Entry entry = null;
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entry = e.Entry; expired.Set(); };
            set.CollectionChanged += (s, e) => changed.Set();
            set.SetExpiration(i, ttl);
            Assert.IsTrue(changed.WaitOne(2000));
            changed.Reset();
            Assert.IsFalse(expired.WaitOne(2000));
            set.SetExpiration(i, TimeSpan.FromSeconds(1));
            Assert.IsTrue(changed.WaitOne(5000));
            Assert.IsTrue(expired.WaitOne(5000));
        }

        [Test]
        public void Can_iterate_over_items() {
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            var n = 10;
            for(var i = 1; i <= n; i++) {
                set.SetExpiration(i, TimeSpan.FromSeconds(i));
            }
            var items = from x in set select x;
            Assert.AreEqual(n, items.Count());
            for(var i = 1; i <= n; i++) {
                var j = i;
                Assert.IsTrue((from x in set where x.Value == j && x.TTL == TimeSpan.FromSeconds(j) select x).Any());
            }
        }

        [Test]
        public void Can_reset_expiration() {
            var expired = new ManualResetEvent(false);
            var changed = new ManualResetEvent(false);
            var i = 42;
            var ttl = TimeSpan.FromSeconds(2);
            ExpiringHashSet<int>.Entry entry = null;
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entry = e.Entry; expired.Set(); };
            set.CollectionChanged += (s, e) => changed.Set();
            set.SetExpiration(i, ttl);
            Assert.IsTrue(changed.WaitOne(500));
            changed.Reset();
            Thread.Sleep(500);
            Assert.IsFalse(expired.WaitOne(500));
            var oldExpire = set[i].When;
            set.RefreshExpiration(i);
            Assert.Greater(set[i].When, oldExpire);
            Assert.IsTrue(changed.WaitOne(5000));
            Assert.IsTrue(expired.WaitOne(5000));
        }

        [Test]
        public void Can_remove_expiration() {
            var expired = new ManualResetEvent(false);
            var changed = new ManualResetEvent(false);
            var i = 42;
            var ttl = TimeSpan.FromSeconds(1);
            ExpiringHashSet<int>.Entry entry = null;
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entry = e.Entry; expired.Set(); };
            set.CollectionChanged += (s, e) => changed.Set();
            set.SetExpiration(i, ttl);
            Assert.IsTrue(changed.WaitOne(2000));
            changed.Set();
            set.RemoveExpiration(i);
            Assert.IsTrue(changed.WaitOne(2000));
            Assert.IsFalse(expired.WaitOne(2000));
        }

        [Test]
        public void Set_will_fire_in_order_of_expirations() {
            var expired = new ManualResetEvent(false);
            var entries = new List<int>();
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entries.Add(e.Entry.Value); expired.Set(); };
            set.SetExpiration(3, TimeSpan.FromMilliseconds(1600));
            set.SetExpiration(2, TimeSpan.FromMilliseconds(1500));
            set.SetExpiration(5, TimeSpan.FromMilliseconds(3000));
            set.SetExpiration(4, TimeSpan.FromMilliseconds(2000));
            set.SetExpiration(1, TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(expired.WaitOne(1000));
            Assert.AreEqual(1, entries.Count);
            Assert.IsTrue(Wait.For(() => entries.Count == 5, TimeSpan.FromSeconds(5000)));
            Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, entries.ToArray());
        }

        [Test]
        public void New_item_with_more_recent_expiration_will_fire_at_expected_time() {
            var expired = new ManualResetEvent(false);
            var entries = new List<int>();
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            set.EntryExpired += (s, e) => { entries.Add(e.Entry.Value); expired.Set(); };
            var n = 1000;
            for(var i = 0; i < n; i++) {
                set.SetExpiration(i, TimeSpan.FromMinutes(10));
            }
            set.SetExpiration(100000, TimeSpan.FromSeconds(2));
            Assert.IsTrue(expired.WaitOne(4000));
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(n, set.Count());
        }

        [Test]
        public void Access_on_tasktimerfactory_ctor_set_does_not_reset_expiration() {
            var i = 42;
            var ttl = TimeSpan.FromSeconds(10);
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            var expireTime = DateTime.UtcNow.AddSeconds(10);
            set.SetExpiration(i, expireTime);
            Thread.Sleep(TimeSpan.FromSeconds(2));
            var entry = set[i];
            AssertEx.AreEqual(ttl, entry.TTL);
            Assert.AreEqual(expireTime,entry.When);
        }

        [Test]
        public void Access_on_autoRefresh_set_does_reset_expiration() {
            var i = 42;
            var ttl = TimeSpan.FromSeconds(10);
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current,true);
            var expireTime = DateTime.UtcNow.AddSeconds(10);
            set.SetExpiration(i, expireTime);
            Thread.Sleep(TimeSpan.FromSeconds(2));
            var entry = set[i];
            AssertEx.AreEqual(ttl, entry.TTL);
            Assert.AreNotEqual(expireTime, entry.When);
            Assert.GreaterOrEqual(entry.When, expireTime);
            Assert.LessOrEqual(entry.When, expireTime.AddSeconds(3));
        }

        [Test]
        public void Refresh_in_autoRefresh_only_fires_every_half_second() {
            var k = 42;
            var ttl = TimeSpan.FromSeconds(10);
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current, true);
            var expireTime = DateTime.UtcNow.AddSeconds(10);
            set.SetExpiration(k, expireTime);
            var when = set[k].When;
            Thread.Sleep(200);
            Assert.AreEqual(when, set[k].When);
            Thread.Sleep(1000);
            Assert.Less(when, set[k].When);
        }

        [Test]
        public void Clear_Empty_Set() {
            
            // http://bugs.developer.mindtouch.com/view.php?id=8739
            var set = new ExpiringHashSet<int>(TaskTimerFactory.Current);
            set.Clear();
        }
    }
}
