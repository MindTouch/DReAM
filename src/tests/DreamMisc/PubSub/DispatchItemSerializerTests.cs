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
using System.Diagnostics;
using System.IO;
using MindTouch.Dream.Services.PubSub;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test.PubSub {

    [TestFixture]
    public class DispatchItemSerializerTests {

        [Test]
        public void Can_roundtrip_DispatchItem() {
            var msg = new XDoc("msg");
            var channel = new XUri("channel://foo.com/bar");
            var origin = new XUri("http://foo.com/baz");
            var ev = new DispatcherEvent(msg, channel, origin);
            var item = new DispatchItem(
                new XUri("http://foo"),
                ev,
                "abc"
            );
            var serializer = new DispatchItemSerializer();
            var stream = serializer.ToStream(item);
            var item2 = serializer.FromStream(stream);
            Assert.AreEqual(item.Uri, item2.Uri, "uri mismatch");
            Assert.AreEqual(item.Location, item2.Location, "location mismatch");
            Assert.AreEqual(item.Event.Id, item2.Event.Id, "id mismatch");
        }

        [Test]
        public void Can_roundtrip_DispatchItem_with_complex_event() {
            var body = new XDoc("msg").Elem("foo", "bar");
            var channel = new XUri("channel://foo.com/bar");
            var resource = new XUri("http://foo.com/baz/0");
            var origin1 = new XUri("http://foo.com/baz/1");
            var origin2 = new XUri("http://foo.com/baz/2");
            var recipient1 = new DispatcherRecipient(new XUri("http://recipient1"));
            var recipient2 = new DispatcherRecipient(new XUri("http://recipient2"));
            var via1 = new XUri("http://via1");
            var via2 = new XUri("http://via2");
            var ev = new DispatcherEvent(body, channel, resource, origin1, origin2);
            ev = ev.WithRecipient(false, recipient1, recipient2).WithVia(via1).WithVia(via2);
            var item = new DispatchItem(
                new XUri("http://foo"),
                ev,
                "abc"
            );
            var serializer = new DispatchItemSerializer();
            var stream = serializer.ToStream(item);
            var item2 = serializer.FromStream(stream);
            Assert.AreEqual(item.Uri, item2.Uri, "uri mismatch");
            Assert.AreEqual(item.Location, item2.Location, "location mismatch");
            Assert.AreEqual(item.Event.Id, item2.Event.Id, "id mismatch");
            Assert.AreEqual(body.ToCompactString(), item2.Event.AsDocument().ToCompactString(), "body mismatch");
            Assert.AreEqual(channel, item2.Event.Channel, "channel mismatch");
            Assert.AreEqual(resource, item2.Event.Resource, "resource mismatch");
            Assert.AreEqual(origin1, item2.Event.Origins[0], "first origin mismatch");
            Assert.AreEqual(origin2, item2.Event.Origins[1], "second origin mismatch");
            Assert.AreEqual(recipient1.Uri, item2.Event.Recipients[0].Uri, "first recipient mismatch");
            Assert.AreEqual(recipient2.Uri, item2.Event.Recipients[1].Uri, "second recipient mismatch");
            Assert.AreEqual(via1, item2.Event.Via[0], "first via mismatch");
            Assert.AreEqual(via2, item2.Event.Via[1], "second via mismatch");
        }

        [Test]
        public void Deserialize_wrong_version_throws() {
            var msg = new XDoc("msg");
            var channel = new XUri("channel://foo.com/bar");
            var origin = new XUri("http://foo.com/baz");
            var ev = new DispatcherEvent(msg, channel, origin);
            var item = new DispatchItem(
                new XUri("http://foo"),
                ev,
                "abc"
            );
            var serializer = new DispatchItemSerializer();
            var stream = serializer.ToStream(item);
            stream.WriteByte(5);
            stream.Position = 0;
            try {
                serializer.FromStream(stream);
                Assert.Fail("should have thrown");
            } catch(InvalidDataException) {
                return;
            }
        }

        [Test, Ignore("perf test")]
        public void Speed() {
            var body = new XDoc("msg").Elem("foo", "bar");
            var channel = new XUri("channel://foo.com/bar");
            var resource = new XUri("http://foo.com/baz/0");
            var origin1 = new XUri("http://foo.com/baz/1");
            var origin2 = new XUri("http://foo.com/baz/2");
            var recipient1 = new DispatcherRecipient(new XUri("http://recipient1"));
            var recipient2 = new DispatcherRecipient(new XUri("http://recipient2"));
            var via1 = new XUri("http://via1");
            var via2 = new XUri("http://via2");
            var ev = new DispatcherEvent(body, channel, resource, origin1, origin2);
            ev = ev.WithRecipient(false, recipient1, recipient2).WithVia(via1).WithVia(via2);
            var item = new DispatchItem(
                new XUri("http://foo"),
                ev,
                "abc"
            );
            var serializer = new DispatchItemSerializer();
            Stream stream = null;
            var n = 100000;
            var t = Stopwatch.StartNew();
            for(var i = 0; i < n; i++) {
                stream = serializer.ToStream(item);
            }
            t.Stop();
            Console.WriteLine("serialize {0:0} items/sec", n / t.Elapsed.TotalSeconds);
            t = Stopwatch.StartNew();
            for(var i = 0; i < n; i++) {
                serializer.FromStream(stream);
                stream.Seek(0, SeekOrigin.Begin);
            }
            t.Stop();
            Console.WriteLine("deserialize {0:0} items/sec", n / t.Elapsed.TotalSeconds);
        }
    }
}
