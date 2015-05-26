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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using log4net;

namespace MindTouch.Sqs {
    public sealed class SqsQueueDelayedSendClient {

        //--- Constants ---
        private static readonly TimeSpan AUTOFLUSH_TIME = 250.Milliseconds();
        
        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        public readonly SqsQueueName QueueName;
        private readonly ISqsClient _client;
        private readonly TimedAccumulator<KeyValuePair<int, string>> _timedSendAccumulator;

        //--- Constructors ---
        public SqsQueueDelayedSendClient(ISqsClient client, SqsQueueName queueName, TaskTimerFactory timerFactory) {
            if(client == null) {
                throw new ArgumentNullException("client");
            }
            this.QueueName = queueName;
            _client = client;
            _timedSendAccumulator = new TimedAccumulator<KeyValuePair<int, string>>(items => AsyncUtil.ForkBackgroundSender(() => BatchSendMessages(items)), SqsUtils.MAX_NUMBER_OF_BATCH_SEND_MESSAGES, AUTOFLUSH_TIME, timerFactory);
        }

        //--- Methods ---
        public void EnqueueMessage(string messageBody) {
            _timedSendAccumulator.Enqueue(new KeyValuePair<int, string>(0, messageBody));
        }

        private void BatchSendMessages(IEnumerable<KeyValuePair<int, string>> items) {
            var failedMessages = _client.SendMessages(QueueName, items.Select(item => item.Value));
            if(failedMessages.Any()) {

                // give failed messages up to 2 additional attempts
                var messageLookup = items.ToDictionary(item => item.Value, item => item.Key);
                foreach(var failedMessage in failedMessages) {
                    int retryCount;
                    if(messageLookup.TryGetValue(failedMessage, out retryCount) && (retryCount < 2)) {
                        _timedSendAccumulator.Enqueue(new KeyValuePair<int, string>(retryCount + 1, failedMessage));
                    } else {

                        // attempts failed, log it into the error log in a format that may allow us to recover it at a later date
                        _log.ErrorFormat("failed to send message on '{0}': {1}", QueueName, failedMessage.EscapeString());
                    }
                }
            }
        }
    }
}