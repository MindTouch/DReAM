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
using MindTouch;
using MindTouch.Sqs;
using MindTouch.Dream;
using log4net;

namespace MindTouchTest.Sqs.Helpers {
    public class MockSqsClient : ISqsClient {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        public List<SqsMessage> Queued = new List<SqsMessage>();
        public List<SqsMessage> Delivered = new List<SqsMessage>();
        public List<SqsMessageReceipt> Deleted = new List<SqsMessageReceipt>();
        public int ReceiveCalled;
        
        public void FillQueue(int count) {
            for(var i = 0; i < count; i++) {
                Queued.Add(MockMessage.NewMockMessage());
            }
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

        public IEnumerable<SqsMessage> ReceiveMessages(SqsQueueName queueName, TimeSpan waitTimeSeconds, uint maxNumberOfMessages) {
            ReceiveCalled++;
            var take = (int)Math.Min(10, maxNumberOfMessages);
            var taken = Queued.Take(take).ToArray();
            _log.DebugFormat("receive returning {0} messages", taken.Length);
            Delivered.AddRange(taken);
            Queued.RemoveRange(0, taken.Length);
            return taken;
        }

        public bool DeleteMessage(SqsQueueName queueName, SqsMessageReceipt messageReceipt) {
            _log.DebugFormat("deleting {0}", messageReceipt);
            Deleted.Add(messageReceipt);
            return true;
        }

        public IEnumerable<SqsMessageId> DeleteMessages(SqsQueueName queueName, IEnumerable<SqsMessage> messages) {
            foreach(var message in messages) {
                DeleteMessage(queueName, message.MessageReceipt);
            }
            return new SqsMessageId[0];
        }

        public XUri CreateQueue(SqsQueueName queueName) {
            throw new NotImplementedException();
        }

        public bool DeleteQueue(SqsQueueName queueName) {
            throw new NotImplementedException();
        }

        public string ListQueues(string prefix) {
            throw new NotImplementedException();
        }
    }
}