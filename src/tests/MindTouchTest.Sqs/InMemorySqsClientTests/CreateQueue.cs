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
using NUnit.Framework;
using MindTouch.Sqs;

namespace MindTouchTest.Sqs.InMemorySqsClientTests {
    [TestFixture]
    public class CreateQueue {

        //--- Methods ---
        [Test]
        public void Can_create_and_delete_queues() {
            var sqs = new InMemorySqsClient();
            var queueName = new SqsQueueName("bar");
            sqs.CreateQueue(queueName);
            var queues = sqs.ListQueues(null);
            Assert.AreEqual(1, queues.Count());
            Assert.AreEqual("local://bar", queues.First());
            sqs.DeleteQueue(queueName);
            queues = sqs.ListQueues(null);
            Assert.IsFalse(queues.Any());
        }
    }
}
