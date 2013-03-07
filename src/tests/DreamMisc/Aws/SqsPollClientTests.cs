/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2011 MindTouch, Inc.
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
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;
using Moq;

namespace MindTouch.Dream.Test.Aws {
    [TestFixture]
    public class SqsPollClientTests {

        [Test]
        public void Poll_client_pulls_from_sqs_and_calls_callback() {
            var mockSqsClient = new MockSqsClient();
            mockSqsClient.FillQueue(15);
            var pollster = new SqsPollClient(mockSqsClient, TaskTimerFactory.Current);
            Assert.AreEqual(15, mockSqsClient.Queued.Count, "queue was accessed prematurely");
            var posted = new List<AwsSqsMessage>();
            pollster.Listen("foo", 300.Seconds(), posted.Add);
            Assert.IsTrue(Wait.For(() => mockSqsClient.Queued.Count == 0, 10.Seconds()), "queue did not get depleted in time");
            Assert.IsTrue(
                Wait.For(() => mockSqsClient.ReceiveCalled == 3, 5.Seconds()),
                string.Format("receive called the wrong number of times: {0} != {1}", 3, mockSqsClient.ReceiveCalled)
                );
            Assert.AreEqual(15, mockSqsClient.Delivered.Count, "delivered has the wrong number of messages");
            Assert.AreEqual(
                mockSqsClient.Delivered.Select(x => x.MessageId).ToArray(),
                mockSqsClient.Deleted.Select(x => x.MessageId).ToArray(),
                "delivered and deleted don't match"
                );
            Assert.AreEqual(
                mockSqsClient.Delivered.Select(x => x.MessageId).ToArray(),
                posted.Select(x => x.MessageId).ToArray(),
                "delivered and posted don't match"
                );
        }

        [Test]
        public void Poll_client_retries_if_sqs_client_throws_on_receive() {

            // Arrange
            var mockSqsClient = new Mock<IAwsSqsClient>();
            var pollster = new SqsPollClient(mockSqsClient.Object, TaskTimerFactory.Current);
            var call = 0;
            var threw = false;
            mockSqsClient.Setup(x => x.Receive("test", AwsSqsDefaults.MAX_MESSAGES, AwsSqsDefaults.DEFAULT_VISIBILITY, It.IsAny<Result<IEnumerable<AwsSqsMessage>>>()))
                .Returns((string queue, int maxMessages, TimeSpan visiblityTimeout, Result<IEnumerable<AwsSqsMessage>> result) => {
                    if(call == 0) {
                        result.Throw(new Exception());
                        threw = true;
                    } else {
                        result.Return(new AwsSqsMessage[0]);
                    }
                    call++;
                    return result;
                });

            // Act
            var messages = 0;
            pollster.Listen("test", 100.Milliseconds(), m => messages++);

            // Assert
            Assert.IsTrue(Wait.For(() => call > 1, 10.Seconds()), "never got past the first receive call");
            Assert.IsTrue(threw, "exception was never thrown in receive call");
            Assert.AreEqual(0, messages, "somehow received messages");
            pollster.Dispose();
        }

        [Test]
        public void Poll_client_soldiers_on_if_delete_fails() {

            // Arrange
            var mockSqsClient = new Mock<IAwsSqsClient>();
            var pollster = new SqsPollClient(mockSqsClient.Object, TaskTimerFactory.Current);
            var call = 0;
            var threw = false;
            mockSqsClient.Setup(x => x.Receive("test", AwsSqsDefaults.MAX_MESSAGES, AwsSqsDefaults.DEFAULT_VISIBILITY, It.IsAny<Result<IEnumerable<AwsSqsMessage>>>()))
                .Returns((string queue, int maxMessages, TimeSpan visiblityTimeout, Result<IEnumerable<AwsSqsMessage>> result) => {
                    result.Return(new AwsSqsMessage[] { new MockMessage() });
                    return result;
                });
            mockSqsClient.Setup(x => x.Delete(It.IsAny<AwsSqsMessage>(), It.IsAny<Result<AwsSqsResponse>>()))
                .Returns((AwsSqsMessage message, Result<AwsSqsResponse> result) => {
                    if(call == 0) {
                        result.Throw(new Exception());
                        threw = true;
                    } else {
                        result.WithReturn(new AwsSqsResponse(XDoc.Empty));
                    }
                    call++;
                    return result;
                });
            // Act
            var messages = 0;
            pollster.Listen("test", 100.Milliseconds(), m => messages++);

            // Assert
            Assert.IsTrue(Wait.For(() => call > 1, 10.Seconds()), "never got past the first delete call");
            Assert.IsTrue(threw, "exception was never thrown in receive call");
            pollster.Dispose();
            Assert.AreEqual(call, messages);
        }
    }
}