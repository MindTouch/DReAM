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
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouchTest.Sqs.SqsClientTests {
    [TestFixture]
    public class DeleteMessage : _Init {

        //--- Methods ---
        [Test, Ignore]
        public void deleting_same_message_multiple_times_returns_http_200_responses() {
            
            // Send a message
            _client.SendMessage(
                    TEST_QUEUE, 
                    new XDoc("message")
                        .Attr("id", 1).ToCompactString());
            
            // Receive the message
            var messages = _client.ReceiveMessages(TEST_QUEUE, 0.1.Seconds(), SqsUtils.MAX_NUMBER_OF_MESSAGES_TO_FETCH);
            Assert.AreNotEqual(null, messages, "Receiving messages failed");

            // Delete the message
            var messageDeleted = _client.DeleteMessage(TEST_QUEUE, messages.First().MessageReceipt);
            Assert.AreEqual(true, messageDeleted, "The message was not deleted");

            // Try to delete it again
            var secondMessageDeleted = _client.DeleteMessage(TEST_QUEUE, messages.First().MessageReceipt);
            Assert.AreEqual(true, secondMessageDeleted, "Deleting a message failed, and it must had succeeded");

            var thirdMessageDeleted = _client.DeleteMessage(TEST_QUEUE, messages.First().MessageReceipt);
            Assert.AreEqual(true, thirdMessageDeleted, "The message was not deleted");
        }
    }
}
