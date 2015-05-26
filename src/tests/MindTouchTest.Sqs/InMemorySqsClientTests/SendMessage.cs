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

using MindTouch.Sqs;
using NUnit.Framework;

namespace MindTouchTest.Sqs.InMemorySqsClientTests {
    [TestFixture]
    public class SendMessage {
    
        //--- Methods ---
        [Test]
        [ExpectedException(typeof(InMemorySqsNullQueueException))]
        public void Cannot_sent_a_message_to_a_non_existent_queue() {
            var sqs = new InMemorySqsClient();
            sqs.SendMessage(new SqsQueueName("myqueuename"), "message body");
        }

        [Test]
        [ExpectedException(typeof(InMemorySqsNullQueueException))]
        public void Sending_to_non_existent_queue_sets_exception_in_result() {
            var sqs = new InMemorySqsClient();
            sqs.SendMessage(new SqsQueueName("foo"), "msg body");
        }
    }
}
