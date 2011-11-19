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
using MindTouch.Aws;
using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Dream.Test.Aws {
    
    [TestFixture]
    public class InMemorySqsClientTests {

        [Test]
        public void Cannot_sent_a_message_to_a_non_existent_queue() {
            var sqs = new InMemorySqsClient();
            var sent = AwsSqsMessage.FromBody("foo");
            try {
                sqs.Send("bar", sent, new Result<AwsSqsSendResponse>()).Wait();
                Assert.Fail("didn't throw");
            } catch(AwsSqsRequestException e) {
                Assert.AreEqual("AWS.SimpleQueueService.NonExistentQueue", e.Error.Code);
            }
        }

        [Test]
        public void Cannot_receive_a_message_to_a_non_existent_queue() {
            var sqs = new InMemorySqsClient();
            var sent = AwsSqsMessage.FromBody("foo");
            try {
                sqs.Receive("bar", new Result<IEnumerable<AwsSqsMessage>>()).Wait();
                Assert.Fail("didn't throw");
            } catch(AwsSqsRequestException e) {
                Assert.AreEqual("AWS.SimpleQueueService.NonExistentQueue", e.Error.Code);
            }
        }

        [Test]
        public void Can_create_and_delete_queues() {
            var sqs = new InMemorySqsClient();
            sqs.CreateQueue("bar", new Result<AwsSqsResponse>()).Wait();
            var queues = sqs.ListQueues(null, new Result<IEnumerable<string>>()).Wait();
            Assert.AreEqual(1, queues.Count());
            Assert.AreEqual("bar",queues.First());
            sqs.DeleteQueue("bar", new Result<AwsSqsResponse>()).Wait();
            queues = sqs.ListQueues(null, new Result<IEnumerable<string>>()).Wait();
            Assert.IsFalse(queues.Any());
        }

        [Test]
        public void Can_round_trip_message_through_queue() {
            var sqs = new InMemorySqsClient();
            sqs.CreateQueue("bar", new Result<AwsSqsResponse>()).Wait();
            var sent = AwsSqsMessage.FromBody("foo");
            sqs.Send("bar", sent, new Result<AwsSqsSendResponse>()).Wait();
            var msgs = sqs.Receive("bar", new Result<IEnumerable<AwsSqsMessage>>()).Wait();
            Assert.AreEqual(1, msgs.Count());
            var received = msgs.First();
            Assert.AreEqual(sent.Body, received.Body);
        }

        [Test]
        public void Can_see_messages_with_visibility_zero() {
            var sqs = new InMemorySqsClient();
            sqs.CreateQueue("bar", new Result<AwsSqsResponse>()).Wait();
            var sent = sqs.Send("bar", AwsSqsMessage.FromBody("foo"), new Result<AwsSqsSendResponse>()).Wait();
            var received1 = sqs.Receive("bar", 10, TimeSpan.Zero, new Result<IEnumerable<AwsSqsMessage>>()).Wait();
            var received2 = sqs.Receive("bar", 10, TimeSpan.Zero, new Result<IEnumerable<AwsSqsMessage>>()).Wait();
            Assert.AreEqual(1,received1.Count());
            Assert.AreEqual(1, received2.Count());
            Assert.AreEqual(sent.MessageId, received1.First().MessageId);
            Assert.AreEqual(sent.MessageId, received2.First().MessageId);
        }

        [Test]
        public void Can_delete_message_from_queue() {
            var sqs = new InMemorySqsClient();
            sqs.CreateQueue("bar", new Result<AwsSqsResponse>()).Wait();
            var sent = AwsSqsMessage.FromBody("foo");
            sqs.Send("bar", sent, new Result<AwsSqsSendResponse>()).Wait();
            var received = sqs.Receive("bar", 10, TimeSpan.Zero, new Result<IEnumerable<AwsSqsMessage>>()).Wait().First();
            sqs.Delete(received, new Result<AwsSqsResponse>()).Wait();
            var remaining = sqs.Receive("bar", 10, TimeSpan.Zero, new Result<IEnumerable<AwsSqsMessage>>()).Wait();
            Assert.IsFalse(remaining.Any());
        }
    }
}
