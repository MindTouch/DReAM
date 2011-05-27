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

using System.Collections.Generic;
using System.IO;
using MindTouch.Dream.Services.PubSub;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test.PubSub {

    [TestFixture]
    public class EventTests {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

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
    }
}
