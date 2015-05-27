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

using System.Linq;
using MindTouch.Extensions.Time;
using NUnit.Framework;
using MindTouch.Sqs;

namespace MindTouchTest.Sqs.InMemorySqsClientTests {
    [TestFixture]
    public class ReceiveMessage {

        //--- Methods ---
        [Test]
        [ExpectedException(typeof(InMemorySqsNullQueueException))]
        public void Cannot_receive_a_message_to_a_non_existent_queue() {
            var sqs = new InMemorySqsClient();
            sqs.ReceiveMessages(new SqsQueueName("queuename"), 0.Seconds(), 10);
        }

        [Test]
        public void Can_round_trip_message_through_queue() {
            var sqs = new InMemorySqsClient();
            var queueName = new SqsQueueName("bar");
            sqs.CreateQueue(queueName);
            sqs.SendMessage(queueName, "msg body");
            var messages = sqs.ReceiveMessages(queueName, 0.Seconds(), 10);
            Assert.AreEqual(1, messages.Count());
            var received = messages.First();
            Assert.AreEqual("msg body", received.Body);
        }

        [Test]
        [ExpectedException(typeof(InMemorySqsNullQueueException))]
        public void Receiving_from_non_existent_queue_throws() {
            var sqs = new InMemorySqsClient();
            sqs.ReceiveMessages(new SqsQueueName("foo"), 0.Seconds(), 10);
        }
    }
}