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

using System;
using System.Collections.Generic;
using System.Linq;
using MindTouch.Sqs;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Extensions.Time;
using MindTouchTest.Sqs.Helpers;
using NUnit.Framework;

namespace MindTouchTest.Sqs.SqsPollingClientTests {
    [TestFixture]
    public class Listen {

        //--- Methods ---
        [Test]
        public void polling_client_pulls_from_sqs_and_calls_callback() {
            var mockSqsClient = new MockSqsClient();
            mockSqsClient.FillQueue(15);
            var pollster = new SqsPollingClient(mockSqsClient);
            Assert.AreEqual(15, mockSqsClient.Queued.Count, "queue was accessed prematurely");
            var posted = new List<SqsMessage>();
            var fooQueue = new SqsQueueName("foo");
            pollster.Listen(new SqsPollingClientSettings(
                                queueName: fooQueue,
                                callback: messages => {
                                    posted.AddRange(messages);
                                    foreach(var msg in messages) {
                                        mockSqsClient.DeleteMessage(fooQueue, msg.MessageReceipt);
                                    }
                                },
                                longPollInterval: 0.Seconds(),
                                maxNumberOfMessages: SqsUtils.MAX_NUMBER_OF_MESSAGES_TO_FETCH,
                                waitTimeOnError: 1.Seconds()));
            Assert.IsTrue(Wait.For(() => mockSqsClient.Queued.Count == 0, 10.Seconds()), "queue did not get depleted in time");
            Assert.IsTrue(
                Wait.For(() => mockSqsClient.ReceiveCalled > 0, 5.Seconds()),
                string.Format("receive called the wrong number of times: Expected {0} != {1}", 3, mockSqsClient.ReceiveCalled)
                );
            Assert.AreEqual(15, mockSqsClient.Delivered.Count, "delivered has the wrong number of messages");

            // Compare delivered and deleted
            Assert.AreEqual(mockSqsClient.Delivered.Count(), mockSqsClient.Deleted.Count(), "The count of delivered messages and deleted messages does not match and it must");
            for(var i = 0; i < mockSqsClient.Delivered.Count(); i++) {
                Assert.AreEqual(
                    mockSqsClient.Delivered[i].MessageReceipt,
                    mockSqsClient.Deleted[i],
                    "delivered message and deleted message don't match on index " + i);
            }
            Assert.AreEqual(mockSqsClient.Delivered.Count(), posted.Count(), "The number of delivered messages and posted messages does not match and it should");
            for(var i = 0; i < mockSqsClient.Delivered.Count(); i++) {
                Assert.AreEqual(
                    mockSqsClient.Delivered[i],
                    posted[i],
                    "delivered and posted don't match");
            }
        }

        [Test]
        public void polling_client_pulls_from_sqs_and_calls_callback_but_does_not_automatically_deletes_messages() {
            var mockSqsClient = new MockSqsClient();
            mockSqsClient.FillQueue(15);
            var pollster = new SqsPollingClient(mockSqsClient);
            Assert.AreEqual(15, mockSqsClient.Queued.Count, "queue was accessed prematurely");
            var posted = new List<SqsMessage>();
            var queueName = new SqsQueueName("foo");
            pollster.Listen(new SqsPollingClientSettings(
                                queueName: queueName,
                                callback: posted.AddRange,
                                longPollInterval: 10.Seconds(),
                                maxNumberOfMessages: SqsUtils.MAX_NUMBER_OF_MESSAGES_TO_FETCH,
                                waitTimeOnError: 1.Seconds()));
            Assert.IsTrue(Wait.For(() => mockSqsClient.Queued.Count == 0, 10.Seconds()), "queue did not get depleted in time");
            Assert.IsTrue(
                Wait.For(() => mockSqsClient.ReceiveCalled > 0, 5.Seconds()),
                string.Format("receive called the wrong number of times: {0} != {1}", 3, mockSqsClient.ReceiveCalled)
                );
            Assert.AreEqual(15, mockSqsClient.Delivered.Count, "delivered has the wrong number of messages");
            Assert.AreNotEqual(
                mockSqsClient.Delivered.Count,
                mockSqsClient.Deleted.Count,
                "delivered and deleted don't match");
            Assert.AreEqual(
                mockSqsClient.Delivered.Count,
                posted.Count,
                "delivered and posted don't match");
        }

        private class ExceptionalSqsClient : ISqsClient {

            //--- Fields ---
            private int _calls;
            private bool _threw;

            //--- Properties ---
            public int Calls { get { return _calls; } }
            public bool Threw { get { return _threw; } }

            //--- Methods ---
            public IEnumerable<SqsMessage> ReceiveMessages(SqsQueueName queueName, TimeSpan waitTimeSeconds, uint maxNumberOfMessages) {
                if(_calls < 3) {
                    _calls++;
                    _threw = true;
                    throw new Exception();
                }
                _calls++;
                return new SqsMessage[0];
            }

            public bool DeleteMessage(SqsQueueName queueName, SqsMessageReceipt messageReceipt) {
                throw new NotImplementedException();
            }

            public IEnumerable<SqsMessageId> DeleteMessages(SqsQueueName queueName, IEnumerable<SqsMessage> messages) {
                throw new NotImplementedException();
            }

            public void SendMessage(SqsQueueName queueName, string messageBody, TimeSpan delay) {
                throw new NotImplementedException();
            }

            public void SendMessage(SqsQueueName queueName, string messageBody) {
                throw new NotImplementedException();
            }

            public IEnumerable<string> SendMessages(SqsQueueName queueName, IEnumerable<string> messageBodies) {
                throw new NotImplementedException();
            }

            public XUri CreateQueue(SqsQueueName queueName) {
                throw new NotImplementedException();
            }

            public bool DeleteQueue(SqsQueueName queueName) {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void polling_client_retries_if_sqs_client_throws_on_receive() {

            // Arrange
            var exceptionSqsClient = new ExceptionalSqsClient();
            var pollster = new SqsPollingClient(exceptionSqsClient);

            // Act
            var messages = 0;
            pollster.Listen(new SqsPollingClientSettings(
                queueName: new SqsQueueName("test"),
                callback: msgs => messages++, 
                longPollInterval: (0.5).Seconds(), 
                maxNumberOfMessages: SqsUtils.MAX_NUMBER_OF_MESSAGES_TO_FETCH, 
                waitTimeOnError: (0.5).Seconds()
            ));

            // Assert
            Assert.IsTrue(Wait.For(() => exceptionSqsClient.Calls > 3, 10.Seconds()), "never got past the first receive call, calls = {0}", exceptionSqsClient.Calls);
            Assert.IsTrue(Wait.For(() => exceptionSqsClient.Threw, 1.Seconds()), "exception was never thrown in receive call");
            Assert.AreEqual(0, messages, "somehow received messages");            
            pollster.Dispose();
        }
    }
}
