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

using System.Collections.Generic;
using System.Linq;
using MindTouch;
using MindTouch.Extensions.Time;
using MindTouch.Sqs;
using NUnit.Framework;

namespace MindTouchTest.Sqs.SqsClientTests {
    [TestFixture]
    public class SendMessages : _Init {

        //--- Methods ---
        [Test, Ignore]
        public void send_messages_in_batch() {

            // Act
            var messages = new[] { "msg1", "msg2", "msg3" };
            _client.SendMessages(TEST_QUEUE, messages);

            // Assert
            var messagesReceived = new List<string>();
            while(true) {
                var sqsMessages = _client.ReceiveMessages(TEST_QUEUE, 1.Seconds(), SqsUtils.MAX_NUMBER_OF_MESSAGES_TO_FETCH);
                if(sqsMessages.None()) {
                    break;
                }
                messagesReceived.AddRange(sqsMessages.Select(msg => msg.Body));
                _client.DeleteMessages(TEST_QUEUE, sqsMessages);
            }
            Assert.AreEqual(messages.Length, messagesReceived.Count, "did not receive the same number of messages as sent");
            messagesReceived.Sort();
            Assert.AreEqual(messages, messagesReceived.ToArray(), "messages differ");
        }
    }
}