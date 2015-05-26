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
using MindTouch.Extensions.Time;
using System.Collections.Generic;
using log4net;

namespace MindTouch.Sqs {
    public static class SqsUtils {

        //--- Constants ---
        public const uint MAX_NUMBER_OF_MESSAGES_TO_FETCH = 10u;
        public const uint MAX_NUMBER_OF_BATCH_DELETE_MESSAGES = 10u;
        public const int MAX_NUMBER_OF_BATCH_SEND_MESSAGES = 10;
        public static readonly TimeSpan MAX_LONG_POLL_WAIT_TIME = 20.Seconds();
        public static readonly TimeSpan DEFAULT_WAIT_TIME_ON_ERROR = 1.Seconds();

        //--- Class Methods ---
        public static Action<IEnumerable<SqsMessage>> MessagesHandler(ISqsClient client, SqsQueueName queueName, Action<SqsMessage> messageHandler, ILog log) {
            return messages => {
                foreach(var message in messages) {
                    try {
                        messageHandler(message);
                    } catch(Exception e) {
                        log.DebugFormat(e, "Failed to handle message '{0}' from queue '{1}'", message.MessageReceipt, queueName);
                        continue;
                    }
                    try {
                        client.DeleteMessage(queueName, message.MessageReceipt);
                    } catch(SqsException e) {
                        log.WarnExceptionFormat(e, "Failed to delete message '{0}' from queue '{1}'", message.MessageReceipt, queueName);
                    }
                }
            };
        }
    }
}
