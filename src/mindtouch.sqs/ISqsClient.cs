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
    /// Interface for Simple Queue Service (SQS) provider.
    /// </summary>
    public interface ISqsClient {

        //--- Methods ---

        /// <summary>
        /// Receive zero or more messages from name queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="waitTimeSeconds">Max amount of time to wait until this method returns.</param>
        /// <param name="maxNumberOfMessages">Max number of messages to request.</param>
        /// <returns>Enumeration of received messages.</returns>
        IEnumerable<SqsMessage> ReceiveMessages(SqsQueueName queueName, TimeSpan waitTimeSeconds, uint maxNumberOfMessages);

        /// <summary>
        /// Delete single message from named queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="messageReceipt">Message receipt.</param>
        /// <returns>True if message was deleted.</returns>
        bool DeleteMessage(SqsQueueName queueName, SqsMessageReceipt messageReceipt);

        /// <summary>
        /// Delete messages from named queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="messages">Enumeration of messages to delete.</param>
        /// <returns>Enumeration of messages that failed to delete.</returns>
        IEnumerable<SqsMessageId> DeleteMessages(SqsQueueName queueName, IEnumerable<SqsMessage> messages);

        /// <summary>
        /// Send message on named queue with a visibility delay.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="messageBody">Message body.</param>
        /// <param name="delay">Time to wait until the message becomes visible.</param>
        void SendMessage(SqsQueueName queueName, string messageBody, TimeSpan delay);

        /// <summary>
        /// Send message on named queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="messageBody">Message body.</param>
        void SendMessage(SqsQueueName queueName, string messageBody);

        /// <summary>
        /// Send one or more message to a named queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="messageBodies">Enumeration of message bodies.</param>
        /// <returns>Enumeration of message bodies that failed to send.</returns>
        IEnumerable<string> SendMessages(SqsQueueName queueName, IEnumerable<string> messageBodies);

        /// <summary>
        /// Create a new named queue and gets its URI.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <returns>True if the named queue was created.</returns>
        bool CreateQueue(SqsQueueName queueName);

        /// <summary>
        /// Delete a named queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <returns>True if the named queue was deleted</returns>
        bool DeleteQueue(SqsQueueName queueName);
    }
}
