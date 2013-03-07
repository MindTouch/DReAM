/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2013 MindTouch, Inc.
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

        [Test]
        public void Receiving_from_non_existent_queue_sets_exception_in_result() {
            var sqs = new InMemorySqsClient();
            var r = sqs.Receive("foo", new Result<IEnumerable<AwsSqsMessage>>()).Block();
            Assert.IsTrue(r.HasException);
        }

        [Test]
        public void Sending_to_non_existent_queue_sets_exception_in_result() {
            var sqs = new InMemorySqsClient();
            var r = sqs.Send("foo", AwsSqsMessage.FromBody("foo"), new Result<AwsSqsSendResponse>()).Block();
            Assert.IsTrue(r.HasException);
        }

        [Test]
        public void Delete_failure_sets_exception_in_result() {
            var sqs = new InMemorySqsClient();
            var r = sqs.Delete(null, new Result<AwsSqsResponse>()).Block();
            Assert.IsTrue(r.HasException);
        }
    }
}
