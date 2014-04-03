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
using System.IO;
using System.Threading;
using MindTouch.Cache;
using MindTouch.IO;
using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class InMemoryKeyValueCacheTests {
        private InMemoryKeyValueCacheFactory _factory;

        [TestFixtureSetUp]
        public void FixtureSetup() {
            _factory = new InMemoryKeyValueCacheFactory(TaskTimerFactory.Current);
            _factory.SetSerializer<int>(new IntSerializer());
        }

        private InMemoryKeyValueCache CreateCache(int size) {
            var factory = new InMemoryKeyValueCacheFactory(size, TaskTimerFactory.Current);
            factory.SetSerializer<int>(new IntSerializer());
            return (InMemoryKeyValueCache)factory.Create();
        }

        [Test]
        public void Can_roundtrip_values_through_cache() {
            var cache = _factory.Create();
            cache.Set("foo", 42);
            Assert.AreEqual(42, cache.Get<int>("foo"));
        }

        [Test]
        public void Can_delete_values_from_cache() {
            var cache = _factory.Create();
            cache.Set("foo", 42);
            Assert.IsTrue(cache.Delete("foo"));
            int x;
            Assert.IsFalse(cache.TryGet("foo", out x));
        }

        [Test]
        public void Trying_to_get_undefined_key_returns_false() {
            var cache = _factory.Create();
            int x;
            Assert.IsFalse(cache.TryGet("foo", out x));
        }

        [Test]
        public void Trying_to_delete_undefined_key_returns_false() {
            var cache = _factory.Create();
            Assert.IsFalse(cache.Delete("foo"));
        }

        [Test]
        public void Timespan_MinValue_does_not_immidiately_expire() {
            var cache = _factory.Create();
            cache.Set("foo", 42, TimeSpan.MinValue);
            Thread.Sleep(100);
            Assert.AreEqual(42, cache.Get<int>("foo"));
        }

        [Test]
        public void Can_expire_cache_items() {
            var cache = _factory.Create();
            cache.Set("foo", 42, TimeSpan.FromMilliseconds(10));
            Thread.Sleep(500);
            int x;
            Assert.IsFalse(cache.TryGet("foo", out x));
        }

        [Test]
        public void Can_set_max_memory_on_cache() {
            var cache = CreateCache(10);
            Assert.AreEqual(10, cache.MemoryCapacity);
        }

        [Test]
        public void Can_see_memory_usage_cache() {
            var cache = CreateCache(10);
            cache.Set("foo", 42);
            Assert.AreEqual(sizeof(int), cache.MemorySize);
        }

        [Test]
        public void Changing_value_does_not_change_usage() {
            var cache = CreateCache(10);
            cache.Set("foo", 42);
            cache.Set("foo", 36);
            Assert.AreEqual(sizeof(int), cache.MemorySize);
        }
        [Test]
        public void Delete_decrements_usage() {
            var cache = CreateCache(10);
            cache.Set("foo", 42);
            cache.Delete("foo");
            Assert.AreEqual(0, cache.MemorySize);
        }

        [Test]
        public void Cache_forces_expiration_as_memory_is_exceeded() {
            var cache = CreateCache(4 * sizeof(int));
            var keys = new[] { "a", "b", "c", "d", "e" };
            foreach(var key in keys) {
                cache.Set(key, 42);
            }
            cache.Flush();
            var itemCount = 0;
            foreach(var k in keys) {
                int x;
                if(cache.TryGet(k, out x)) {
                    itemCount++;
                }
            }
            Assert.AreEqual(3, itemCount);
        }

        [Test]
        public void Can_dispose_cache() {
            var cache = _factory.Create();
            cache.Set("foo", 42);
            cache.Dispose();
        }

        private class IntSerializer : ISerializer {
            public T Deserialize<T>(Stream stream) {
                return (T)(object)BitConverter.ToInt32(stream.ReadBytes(stream.Length), 0);
            }

            public void Serialize<T>(Stream stream, T data) {
                stream.Write(BitConverter.GetBytes((int)(object)data));
            }
        }
    }
}
