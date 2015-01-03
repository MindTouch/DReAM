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
using MindTouch.Tasking;
using MindTouch.Extensions.Time;
using MindTouch.Threading.Timer;

namespace MindTouch.Aws {
    public class InMemorySqsClient : IAwsSqsClient, IDisposable {

        //--- Types ---
        private class Response : AwsSqsResponse {

            //--- Constructors ---
            public Response() {
                RequestId = Guid.NewGuid().ToString();
            }
        }

        private class SendResponse : AwsSqsSendResponse {

            //--- Constructors ---
            public SendResponse(AwsSqsMessage message) {
                RequestId = message.RequestId;
                MD5OfMessageBody = message.MD5OfBody;
                MessageId = message.MessageId;
            }
        }

        private class Message : AwsSqsMessage {

            //--- Constructors ---
            public Message(AwsSqsMessage message, string queue) {
                foreach(var attr in message.Attributes) {
                    _attributes.Add(attr);
                }
                this.Body = message.Body;
                this.MD5OfBody = StringUtil.ComputeHashString(Body);
                this.MessageId = Guid.NewGuid().ToString();
                this.OriginQueue = queue;
                this.ReceiptHandle = Guid.NewGuid().ToString();
            }

            //--- Methods ---
            public AwsSqsMessage WithNewRequestId() {
                this.RequestId = Guid.NewGuid().ToString();
                return this;
            }
        }

        private class QueueEntry {

            //--- Fields ---
            public readonly Message Message;
            public DateTime VisibleTime;

            //--- Constructors ---
            public QueueEntry(AwsSqsMessage message, string queue) {
                Message = new Message(message, queue);
                VisibleTime = DateTime.MinValue;
            }
        }

        //--- Class Fields ---
        private static readonly object _synclock = new object();
        private static InMemorySqsClient _instance = new InMemorySqsClient();

        //--- Class Properties
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
        private readonly Dictionary<string, List<QueueEntry>> _queues = new Dictionary<string, List<QueueEntry>>();
        private readonly Random _random = new Random();
        private ulong _messageCounter;
        private bool _isDisposed;

        //--- Properties ---
        public ulong TotalMessagesReceived { get { return _messageCounter; } }
        public bool IsDisposed { get { return _isDisposed; } }

        public IEnumerable<KeyValuePair<string, int>> QueueSizes {
            get {
                lock(_queues) {
                    return _queues.Select(x => new KeyValuePair<string, int>(x.Key, x.Value.Count)).ToArray();
                }
            }
        }

        //--- Methods ---
        public IEnumerable<AwsSqsMessage> InspectQueue(string queue) {
            lock(_queues) {
                var msgQueue = GetQueue(queue);
                if(msgQueue == null) {
                    return new AwsSqsMessage[0];
                }
                lock(msgQueue) {
                    return msgQueue.Select(x => x.Message).ToArray();
                }
            }
        }

        public void ClearQueue(string queue) {
            lock(_queues) {
                var msgQueue = GetQueue(queue);
                if(msgQueue == null) {
                    return;
                }
                lock(msgQueue) {
                    msgQueue.Clear();
                }
            }
        }

        public Result<AwsSqsSendResponse> Send(string queue, AwsSqsMessage message, Result<AwsSqsSendResponse> result) {
            try {
                var msgQueue = GetQueue(queue);
                ThrowIfQueueIsNull(msgQueue);
                if(msgQueue == null) {
                    throw new AwsSqsRequestException("AWS.SimpleQueueService.NonExistentQueue", DreamMessage.InternalError());
                }
                var enqueued = new QueueEntry(message, queue);
                lock(msgQueue) {
                    msgQueue.Add(enqueued);
                }
                _messageCounter++;
                result.Return(new SendResponse(enqueued.Message));
                return result;
            } catch(Exception e) {
                result.Throw(e);
                return result;
            }
        }

        public Result<IEnumerable<AwsSqsMessage>> Receive(string queue, int maxMessages, TimeSpan visibilityTimeout, Result<IEnumerable<AwsSqsMessage>> result) {
            try {
                if(maxMessages == AwsSqsDefaults.DEFAULT_MESSAGES) {
                    maxMessages = 1;
                }
                if(visibilityTimeout == AwsSqsDefaults.DEFAULT_VISIBILITY) {
                    visibilityTimeout = 30.Seconds();
                }
                var msgQueue = GetQueue(queue);
                ThrowIfQueueIsNull(msgQueue);
                QueueEntry[] entries;
                var now = GlobalClock.UtcNow;
                lock(msgQueue) {
                    entries = msgQueue
                        .Where(x => x.VisibleTime <= now)
                        .OrderBy(x => _random.Next())
                        .Take(maxMessages)
                        .ToArray();
                    foreach(var entry in entries) {
                        entry.VisibleTime = now + visibilityTimeout;
                    }
                }
                result.Return(entries.Select(x => x.Message.WithNewRequestId()));
                return result;
            } catch(Exception e) {
                result.Throw(e);
                return result;
            }
        }

        public Result<AwsSqsResponse> Delete(AwsSqsMessage message, Result<AwsSqsResponse> result) {
            try {
                var queue = message.OriginQueue;
                var msgQueue = GetQueue(queue);
                if(msgQueue != null) {
                    lock(msgQueue) {
                        var entry = msgQueue.FirstOrDefault(x => x.Message.ReceiptHandle == message.ReceiptHandle);
                        if(entry != null) {
                            msgQueue.Remove(entry);
                        }
                    }
                }
                result.Return(new SendResponse(message));
                return result;
            } catch(Exception e) {
                result.Throw(e);
                return result;
            }
        }

        public Result<AwsSqsResponse> CreateQueue(string queue, TimeSpan defaultVisibilityTimeout, Result<AwsSqsResponse> result) {
            try {
                lock(_queues) {
                    if(!_queues.ContainsKey(queue)) {
                        _queues[queue] = new List<QueueEntry>();
                    }
                }
                result.Return(new Response());
                return result;
            } catch(Exception e) {
                result.Throw(e);
                return result;
            }
        }

        public Result<AwsSqsResponse> DeleteQueue(string queue, Result<AwsSqsResponse> result) {
            try {
                lock(_queues) {
                    _queues.Remove(queue);
                }
                result.Return(new Response());
                return result;
            } catch(Exception e) {
                result.Throw(e);
                return result;
            }
        }

        public Result<IEnumerable<string>> ListQueues(string prefix, Result<IEnumerable<string>> result) {
            try {
                string[] queues;
                lock(_queues) {
                    queues = _queues.Keys.Where(x => x.StartsWith(prefix ?? "")).ToArray();
                }
                result.Return(queues);
                return result;
            } catch(Exception e) {
                result.Throw(e);
                return result;
            }
        }

        private List<QueueEntry> GetQueue(string queue) {
            lock(_queues) {
                List<QueueEntry> msgQueue;
                _queues.TryGetValue(queue, out msgQueue);
                return msgQueue;
            }
        }

        private void ThrowIfQueueIsNull(List<QueueEntry> msgQueue) {
            if(msgQueue == null) {
                throw new AwsSqsRequestException(new AwsSqsError("Sender", "AWS.SimpleQueueService.NonExistentQueue", "AWS.SimpleQueueService.NonExistentQueue", ""), new DreamMessage(DreamStatus.BadRequest, null));
            }
        }

        public void Dispose() {
            if(_isDisposed) {
                return;
            }
            _isDisposed = true;
        }
    }
}
