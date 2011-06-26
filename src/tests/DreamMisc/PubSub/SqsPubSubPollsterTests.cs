/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using log4net;
using MindTouch.Aws;
using MindTouch.Dream.Services.PubSub;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test.PubSub {

    [TestFixture]
    public class SqsPubSubPollsterTests {

        [Test]
        public void Pollster_pulls_from_sqs_and_posts_to_plug() {
            var mockSqsClient = new MockSqsClient();
            var builder = new ContainerBuilder();
            builder.Register(c => mockSqsClient).As<IAwsSqsClient>();
            var container = builder.Build();
            mockSqsClient.FillQueue(15);
            var pollster = new SqsPubSubPollster(
                new XDoc("config")
                    .Elem("queue", "foo")
                    .Elem("poll-interval", 300)
                    .Elem("cache-ttl", 300),
                container
            );
            Assert.AreEqual(15, mockSqsClient.Queued.Count, "queue was accessed prematurely");
            var destination = new XUri("mock://endpoint");
            var posted = new List<string>();
            MockPlug.Register(destination, (plug, verb, uri, request, response) => {
                posted.Add(request.ToDocument()["id"].AsText);
                response.Return(DreamMessage.Ok());
            });
            pollster.RegisterEndPoint(Plug.New(destination), TaskTimerFactory.Current);
            Assert.IsTrue(Wait.For(() => mockSqsClient.Queued.Count == 0, 10.Seconds()), "queue did not get depleted in time");
            Assert.IsTrue(
                Wait.For(() => mockSqsClient.ReceiveCalled == 3, 5.Seconds()),
                string.Format("receive called the wrong number of times: {0} != {1}",3,mockSqsClient.ReceiveCalled)
            );
            Assert.AreEqual(15, mockSqsClient.Delivered.Count, "delivered has the wrong number of messages");
            Assert.AreEqual(
                mockSqsClient.Delivered.Select(x => x.Id).ToArray(),
                mockSqsClient.Deleted.Select(x => x.Id).ToArray(),
                "delivered and deleted don't match"
            );
            Assert.AreEqual(
                mockSqsClient.Delivered.Select(x => x.Id).ToArray(),
                posted.ToArray(),
                "delivered and posted don't match"
            );
        }
    }

    public class MockSqsClient : IAwsSqsClient {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        public List<AwsSqsMessage> Queued = new List<AwsSqsMessage>();
        public List<AwsSqsMessage> Delivered = new List<AwsSqsMessage>();
        public List<AwsSqsMessage> Deleted = new List<AwsSqsMessage>();
        public int ReceiveCalled;

        public void FillQueue(int count) {
            for(var i = 0; i < count; i++) {
                Queued.Add(new MockMessage());
            }
        }

        public Result<string> Send(string queue, AwsSqsMessage message, Result<string> result) {
            throw new NotImplementedException();
        }

        public Result<IEnumerable<AwsSqsMessage>> Receive(string queue, int maxMessages, TimeSpan visibilityTimeout, Result<IEnumerable<AwsSqsMessage>> result) {
            ReceiveCalled++;
            var r = new Result<IEnumerable<AwsSqsMessage>>();
            var take = Math.Min(10, maxMessages);
            var taken = Queued.Take(take).ToArray();
            _log.DebugFormat("receive returning {0} messages", taken.Length);
            Delivered.AddRange(taken);
            r.Return(taken);
            Queued.RemoveRange(0, taken.Length);
            return r;
        }

        public Result Delete(AwsSqsMessage message, Result result) {
            _log.DebugFormat("deleting {0}", message.Id);
            Deleted.Add(message);
            return new Result().WithReturn();
        }

        public Result CreateQueue(string queue, TimeSpan defaultVisibilityTimeout, Result result) {
            throw new NotImplementedException();
        }

        public Result DeleteQueue(string queue, Result result) {
            throw new NotImplementedException();
        }
    }

    public class MockMessage : AwsSqsMessage {
        private static int NEXT;
        public MockMessage() {
            Id = (++NEXT).ToString();
            ReceiptHandle = Guid.NewGuid().ToString();
            Body = new XDoc("doc").Elem("id", Id).Elem("receipt-handle", ReceiptHandle).ToCompactString();
        }
        public MockMessage(int id) {
            Id = id.ToString();
            ReceiptHandle = Guid.NewGuid().ToString();
            Body = new XDoc("doc").Elem("id", Id).Elem("receipt-handle", ReceiptHandle).ToCompactString();
        }
    }
}
