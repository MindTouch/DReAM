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
        public bool IsDisposed { get { return _isDisposed; } }

         //--- Methods ---
        public IEnumerable<SqsMessageId> DeleteMessages(SqsQueueName queueName, IEnumerable<SqsMessage> messages) {
            foreach(var message in messages) {
                DeleteMessage(queueName, message.MessageReceipt);
            }
            return new SqsMessageId[0];
        }

        public XUri GetQueueUri(SqsQueueName queueName) {
            return new XUri("local://" + queueName.Value);
        }

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

        public void SendMessage(SqsQueueName queueName, string messageBody, TimeSpan delay) {
            var queueUri = GetQueueUri(queueName);
            var msgQueue = GetQueue(queueUri);
            AssertQueueIsNotNull(queueUri, msgQueue);
            lock(msgQueue) {
                var entry = new QueueEntry(new SqsMessage(new SqsMessageId(Guid.NewGuid().ToString()), new SqsMessageReceipt(Guid.NewGuid().ToString()), messageBody), DateTime.MinValue);
                msgQueue.Add(entry);
            }
        }

        public void SendMessage(SqsQueueName queueName, string messageBody) {
            SendMessage(queueName, messageBody, TimeSpan.Zero);
        }

        public IEnumerable<string> SendMessages(SqsQueueName queueName, IEnumerable<string> messageBodies) {
            foreach(var messageBody in messageBodies) {
                SendMessage(queueName, messageBody);
            }
            return Enumerable.Empty<string>();
        }

        public XUri CreateQueue(SqsQueueName queueName) {
            lock(_queues) {
                var queueUri = GetQueueUri(queueName);
                if(!_queues.ContainsKey(queueUri)) {
                    _queues[queueUri] = new List<QueueEntry>();
                }
                return new XUri(queueUri);
            }
        }

        public bool DeleteQueue(SqsQueueName queueName) {
            lock(_queues) {
                var queueUri = GetQueueUri(queueName);
                if(_queues.ContainsKey(queueUri)) {
                    _queues.Remove(queueUri);
                }
                return true;
            }
        }

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

        public IEnumerable<string> ListQueues(string prefix) {
            string[] queues;
            lock(_queues) {
                queues = _queues
                    .Keys
                    .Where(queue => queue.ToString().StartsWith(prefix ?? ""))
                    .Select(queue => queue.ToString()).ToArray();
            }
            return queues;
        }

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
