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
using MindTouch.Tasking;

namespace MindTouch.Sqs {
    public sealed class SqsDelayedSendClient {
        
        //--- Fields ---
        private readonly ISqsClient _client;
        private Dictionary<string, SqsQueueDelayedSendClient> _directory = new Dictionary<string, SqsQueueDelayedSendClient>();
        private readonly TaskTimerFactory _timerFactory;

        //--- Constructors ---
        public SqsDelayedSendClient(ISqsClient client, TaskTimerFactory timerFactory) {
            if(client == null) {
                throw new ArgumentNullException("client");
            }
            if(timerFactory == null) {
                throw new ArgumentNullException("timerFactory");
            }
            _client = client;
            _timerFactory = timerFactory;
        }

        //--- Methods ---
        public void EnqueueMessage(SqsQueueName queueName, string messageBody) {
            GetEnqueueMessageCallback(queueName)(messageBody);
        }

        public Action<string> GetEnqueueMessageCallback(SqsQueueName queueName) {
        repeat:
            SqsQueueDelayedSendClient queue;
            var directory = _directory;
            if(!directory.TryGetValue(queueName.Value, out queue)) {
                var newDirectory = new Dictionary<string, SqsQueueDelayedSendClient>(directory);
                newDirectory[queueName.Value] = queue = new SqsQueueDelayedSendClient(_client, queueName, _timerFactory);
                if(!SysUtil.CAS(ref _directory, directory, newDirectory)) {
                    goto repeat;
                }
            }
            return queue.EnqueueMessage;
        }
    }
}