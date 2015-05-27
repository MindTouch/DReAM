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

using MindTouch.Extensions.Time;
using MindTouch.Sqs;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouchTest.Sqs.SqsClientTests {
    [TestFixture]
    public class SendMessage : _Init {

        //--- Methods ---
        [Test, Ignore]
        public void successfully_sending_messages_to_sqs_returns_http_200_responses() {

            // Setup
            // This assumes the queue name is correct and the queue already exists

            // Act
            _client.SendMessage(TEST_QUEUE, new XDoc("event").Attr("id", 1).ToCompactString());
        }

        [Test, Ignore, ExpectedException(typeof(SqsException))]
        public void sending_a_message_to_unknown_queue_throws_an_amazon_sqs_exception() {

            // Setup
            var queue = new SqsQueueName("unknown-queue");

            // Act
            _client.SendMessage(queue, new XDoc("event").Attr("id", 1).ToCompactString());
        }

        [Test, Ignore]
        public void sending_a_message_with_a_delay_works() {

            // Setup
            var queue = new SqsQueueName("unknown-queue");

            // Act
            for(int i = 0; i < 10; i++) {
                _client.SendMessage(queue, new XDoc("event")
                                               .Start("page")
                                               .Elem("page", "/a/b/c")
                                               .End().ToCompactString(), 30.Seconds());
            }
        }
    }
}
