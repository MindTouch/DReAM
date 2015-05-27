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
using MindTouch.Dream;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;

namespace MindTouch.Sqs {

    /// <summary>
    /// An in-memory variation of the ISqsClient interface. Intended for testing in non-cloud environments.
    /// </summary>
    public class InMemorySqsClient : ISqsClient, IDisposable {

        //--- Class types ---
        private sealed class QueueEntry {

            //--- Fields ---
            public SqsMessage Message { get; private set; }
            public DateTime VisibleTime;
        
            //--- Constructors ---
            public QueueEntry(SqsMessage message, DateTime visibleTime) {
                this.Message = message;
                this.VisibleTime = visibleTime;
            }
        }
        
        //--- Class Fields ---
        private static readonly object _synclock = new object();
        private static InMemorySqsClient _instance = new InMemorySqsClient();

        //--- Class Properties ---

        /// <summary>
        /// Access globally shared in-memory queuing client.
        /// </summary>
        public static InMemorySqsClient Instance {
            get {
                var instance = _instance;
                if(!instance.IsDisposed) {
                    return instance;
                }
                lock(_synclock) {
                    if(_instance.IsDisposed) {
                        _instance = new InMemorySqsClient();
                    }
                    return _instance;
                }
            }
        }

        //--- Fields ---
        private bool _isDisposed;
        private readonly Dictionary<XUri, List<QueueEntry>> _queues = new Dictionary<XUri, List<QueueEntry>>();
        private readonly Random _random = new Random();

        //--- Properties ---

        /// <summary>
        /// Returns true if instance is disposed.
        /// </summary>
        public bool IsDisposed { get { return _isDisposed; } }

         //--- Methods ---

        /// <summary>
        /// Delete messages from named queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="messages">Enumeration of messages to delete.</param>
        /// <returns>Enumeration of messages that failed to delete.</returns>
        public IEnumerable<SqsMessageId> DeleteMessages(SqsQueueName queueName, IEnumerable<SqsMessage> messages) {
            foreach(var message in messages) {
                DeleteMessage(queueName, message.MessageReceipt);
            }
            return new SqsMessageId[0];
        }

        /// <summary>
        /// Get an internal URI for named queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <returns>Internal URI for named queue.</returns>
        public XUri GetQueueUri(SqsQueueName queueName) {
            return new XUri("local://" + queueName.Value);
        }

        /// <summary>
        /// Receive zero or more messages from name queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="waitTimeSeconds">Max amount of time to wait until this method returns.</param>
        /// <param name="maxNumberOfMessages">Max number of messages to request.</param>
        /// <returns>Enumeration of received messages.</returns>
        public IEnumerable<SqsMessage> ReceiveMessages(SqsQueueName queueName, TimeSpan waitTimeSeconds, uint maxNumberOfMessages) {
            var start = GlobalClock.UtcNow;

            // keep checking for messages until the wait-timeout kicks in
            while(true) {
                var queueUri = GetQueueUri(queueName);
                var msgQueue = GetQueue(queueUri);
                AssertQueueIsNotNull(queueUri, msgQueue);
                QueueEntry[] entries;
                var now = GlobalClock.UtcNow;
                lock(msgQueue) {
                    var visibilityTimeout = 30.Seconds();
                    entries = msgQueue
                        .Where(x => x.VisibleTime <= now)
                        .OrderBy(x => _random.Next())
                        .Take(10)
                        .ToArray();
                    foreach(var entry in entries) {
                        entry.VisibleTime = now + visibilityTimeout;
                    }
                }
                if(entries.Any()) {
                    return entries.Select(e => new SqsMessage(e.Message.MessageId, e.Message.MessageReceipt, e.Message.Body)).ToArray();
                }
                if((now - start) > waitTimeSeconds) {
                    return Enumerable.Empty<SqsMessage>();                    
                }

                // no message found; go to sleep for 100ms and try again
                AsyncUtil.Sleep(100.Milliseconds());
            }
        }

        /// <summary>
        /// Delete single message from named queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="messageReceipt">Message receipt.</param>
        /// <returns>True if message was deleted.</returns>
        public bool DeleteMessage(SqsQueueName queueName, SqsMessageReceipt messageReceipt) {
            var queueUrl = GetQueueUri(queueName);
            var msgQueue = GetQueue(queueUrl);
            AssertQueueIsNotNull(queueUrl, msgQueue);
            lock(msgQueue) {
                var entry = msgQueue.FirstOrDefault(x => x.Message.MessageReceipt == messageReceipt);
                if(entry != null) {
                    msgQueue.Remove(entry);
                }
            }
            return true;
        }

        /// <summary>
        /// Removed all messages from named queue.
        /// </summary>
        /// <param name="queueName">Queue name</param>
        public void ClearQueue(SqsQueueName queueName) {
            lock(_queues) {
                var msgQueue = GetQueue(GetQueueUri(queueName));
                if(msgQueue == null) {
                    return;
                }
                lock(msgQueue) {
                    msgQueue.Clear();
                }
            }
        }

        /// <summary>
        /// Send message on named queue with a visibility delay.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="messageBody">Message body.</param>
        /// <param name="delay">Time to wait until the message becomes visible.</param>
        public void SendMessage(SqsQueueName queueName, string messageBody, TimeSpan delay) {
            var queueUri = GetQueueUri(queueName);
            var msgQueue = GetQueue(queueUri);
            AssertQueueIsNotNull(queueUri, msgQueue);
            lock(msgQueue) {
                var entry = new QueueEntry(new SqsMessage(new SqsMessageId(Guid.NewGuid().ToString()), new SqsMessageReceipt(Guid.NewGuid().ToString()), messageBody), DateTime.MinValue);
                msgQueue.Add(entry);
            }
        }

        /// <summary>
        /// Send message on named queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="messageBody">Message body.</param>
        public void SendMessage(SqsQueueName queueName, string messageBody) {
            SendMessage(queueName, messageBody, TimeSpan.Zero);
        }

        /// <summary>
        /// Send one or more message to a named queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="messageBodies">Enumeration of message bodies.</param>
        /// <returns>Enumeration of message bodies that failed to send.</returns>
        public IEnumerable<string> SendMessages(SqsQueueName queueName, IEnumerable<string> messageBodies) {
            foreach(var messageBody in messageBodies) {
                SendMessage(queueName, messageBody);
            }
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Create a new named queue and gets its URI.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <returns>True if the named queue was created.</returns>
        public bool CreateQueue(SqsQueueName queueName) {
            lock(_queues) {
                var queueUri = GetQueueUri(queueName);
                if(!_queues.ContainsKey(queueUri)) {
                    _queues[queueUri] = new List<QueueEntry>();
                }
                return true;
            }
        }

        /// <summary>
        /// Delete a named queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <returns>True if the named queue was deleted</returns>
        public bool DeleteQueue(SqsQueueName queueName) {
            lock(_queues) {
                var queueUri = GetQueueUri(queueName);
                if(_queues.ContainsKey(queueUri)) {
                    _queues.Remove(queueUri);
                }
                return true;
            }
        }

        /// <summary>
        /// Get enumeration of all messages that are currently held by the in-memory named queue.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <returns>Enumeration of messages.</returns>
        public IEnumerable<SqsMessage> InspectQueue(SqsQueueName queueName) {
            lock(_queues) {
                var msgQueue = GetQueue(GetQueueUri(queueName));
                if(msgQueue == null) {
                    return new SqsMessage[0];
                }
                lock(msgQueue) {
                    return msgQueue.Select(x => x.Message).ToArray();
                }
            }
        }

        /// <summary>
        /// Get enumeration of all in-memory queue names that start with a prefix (optiopnal).
        /// </summary>
        /// <param name="prefix">Prefix to match on. Can be NULL.</param>
        /// <returns>Enumeration of queue names.</returns>
        public IEnumerable<string> ListQueues(string prefix) {
            string[] queues;
            lock(_queues) {
                queues = _queues
                    .Keys
                    .Where(queue => queue.ToString().StartsWithInvariant(prefix ?? ""))
                    .Select(queue => queue.ToString()).ToArray();
            }
            return queues;
        }

        /// <summary>
        /// Dispose of ISqsClient.
        /// </summary>
        public void Dispose() {
            if(_isDisposed) {
                return;
            }
            _isDisposed = true;
        }

        private List<QueueEntry> GetQueue(XUri queueUri) {
            lock(_queues) {
                List<QueueEntry> msgQueue;
                _queues.TryGetValue(queueUri, out msgQueue);
                return msgQueue;
            }
        }

        private void AssertQueueIsNotNull(XUri queueUrl, List<QueueEntry> queue) {
            if(queue == null) {
                throw new InMemorySqsNullQueueException(string.Format("Queue '{0}' is null", queueUrl));
            }
        }
    }
}
