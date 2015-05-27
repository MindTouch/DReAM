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
using MindTouch.Sqs;
using MindTouch.Extensions.Time;
using NUnit.Framework;

namespace MindTouchTest.Sqs.InMemorySqsClientTests {
    [TestFixture]
    public class DeleteMessage {
        
        //--- Methods ---
        [Test]
        public void Can_delete_message_from_queue() {
            var sqs = new InMemorySqsClient();
            sqs.CreateQueue(new SqsQueueName("bar"));
            sqs.SendMessage(new SqsQueueName("bar"), "msg body");
            var receivedMessages = sqs.ReceiveMessages(new SqsQueueName("bar"), 0.Seconds(), 10).First();
            sqs.DeleteMessage(new SqsQueueName("bar"), receivedMessages.MessageReceipt);
            var remainingMessages = sqs.ReceiveMessages(new SqsQueueName("bar"), 0.Seconds(), 10);
            Assert.IsFalse(remainingMessages.Any());
        }

        [Test]
        [ExpectedException(typeof(InMemorySqsNullQueueException))]
        public void Delete_failure_throws() {
            var sqs = new InMemorySqsClient();
            sqs.DeleteMessage(new SqsQueueName("non-existant"), new SqsMessageReceipt("msg receipt"));
        }
    }
}
