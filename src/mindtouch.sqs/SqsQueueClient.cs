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
using MindTouch.Dream;

namespace MindTouch.Sqs {

    /// <summary>
    /// Class for working with named queues.
    /// </summary>
    public sealed class SqsQueueClient {

        //--- Fields ---

        /// <summary>
        /// Queue name.
        /// </summary>
        public readonly SqsQueueName QueueName;

        private readonly ISqsClient _client;

        //--- Constructors ---

        /// <summary>
        /// Constructor for creating an instance.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="client">ISqsClient instance.</param>
        public SqsQueueClient(SqsQueueName queueName, ISqsClient client) {
            if(client == null) {
                throw new ArgumentNullException("client");
            }
            this.QueueName = queueName;
            _client = client;
        }

        //--- Methods ---

        /// <summary>
        /// 
        /// Receive zero or more messages.
        /// </summary>
        /// <param name="waitTimeSeconds">Max amount of time to wait until this method returns.</param>
        /// <param name="maxNumberOfMessages">Max number of messages to request.</param>
        /// <returns>Enumeration of received messages.</returns>
        public IEnumerable<SqsMessage> ReceiveMessages(TimeSpan waitTimeSeconds, uint maxNumberOfMessages) {
            return _client.ReceiveMessages(QueueName, waitTimeSeconds, maxNumberOfMessages);
        }
        
        /// <summary>
        /// Delete single message.
        /// </summary>
        /// <param name="messageReceipt">Message receipt.</param>
        /// <returns>True if message was deleted.</returns>
        public bool DeleteMessage(SqsMessageReceipt messageReceipt) {
            return _client.DeleteMessage(QueueName, messageReceipt);
        }

        /// <summary>
        /// Delete messages.
        /// </summary>
        /// <param name="messages">Enumeration of messages to delete.</param>
        /// <returns>Enumeration of messages that failed to delete.</returns>
        public IEnumerable<SqsMessageId> DeleteMessages(IEnumerable<SqsMessage> messages) {
            return _client.DeleteMessages(QueueName, messages);
        }

        /// <summary>
        /// Send message with a visibility delay.
        /// </summary>
        /// <param name="messageBody">Message body.</param>
        /// <param name="delay">Time to wait until the message becomes visible.</param>
        public void SendMessage(string messageBody, TimeSpan delay) {
            _client.SendMessage(QueueName, messageBody, delay);
        }

        /// <summary>
        /// Send message.
        /// </summary>
        /// <param name="messageBody">Message body.</param>
        public void SendMessage(string messageBody) {
            _client.SendMessage(QueueName, messageBody);
        }

        /// <summary>
        /// Create the named queue and gets its URI.
        /// </summary>
        /// <returns>URI for the newly created queue.</returns>
        public XUri CreateQueue() {
            return _client.CreateQueue(QueueName);
        }

        /// <summary>
        /// Delete the named queue.
        /// </summary>
        /// <returns>True if the named queue was deleted</returns>
        public bool DeleteQueue() {
            return _client.DeleteQueue(QueueName);
        }
    }
}
