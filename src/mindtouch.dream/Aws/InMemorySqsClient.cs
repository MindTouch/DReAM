/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */
using System;
using System.Collections.Generic;
using System.Linq;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Extensions.Time;

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
            get { var instance = _instance;
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
        }

        public Result<IEnumerable<AwsSqsMessage>> Receive(string queue, int maxMessages, TimeSpan visibilityTimeout, Result<IEnumerable<AwsSqsMessage>> result) {
            if(maxMessages == AwsSqsDefaults.DEFAULT_MESSAGES) {
                maxMessages = 1;
            }
            if(visibilityTimeout == AwsSqsDefaults.DEFAULT_VISIBILITY) {
                visibilityTimeout = 30.Seconds();
            }
            var msgQueue = GetQueue(queue);
            ThrowIfQueueIsNull(msgQueue);
            QueueEntry[] entries;
            var now = DateTime.UtcNow;
            lock(msgQueue) {
                entries = msgQueue
                    .Where(x => x.VisibleTime <= now)
                    .Take(maxMessages)
                    .ToArray();
                foreach(var entry in entries) {
                    entry.VisibleTime = now + visibilityTimeout;
                }
            }
            result.Return(entries.Select(x => x.Message.WithNewRequestId()));
            return result;
        }

        public Result<AwsSqsResponse> Delete(AwsSqsMessage message, Result<AwsSqsResponse> result) {
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
        }

        public Result<AwsSqsResponse> CreateQueue(string queue, TimeSpan defaultVisibilityTimeout, Result<AwsSqsResponse> result) {
            lock(_queues) {
                if(!_queues.ContainsKey(queue)) {
                    _queues[queue] = new List<QueueEntry>();
                }
            }
            result.Return(new Response());
            return result;
        }

        public Result<AwsSqsResponse> DeleteQueue(string queue, Result<AwsSqsResponse> result) {
            lock(_queues) {
                _queues.Remove(queue);
            }
            result.Return(new Response());
            return result;
        }

        public Result<IEnumerable<string>> ListQueues(string prefix, Result<IEnumerable<string>> result) {
            string[] queues;
            lock(_queues) {
                queues = _queues.Keys.Where(x => x.StartsWith(prefix ?? "")).ToArray();
            }
            result.Return(queues);
            return result;
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
