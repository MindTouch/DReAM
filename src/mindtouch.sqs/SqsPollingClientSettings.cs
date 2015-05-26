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
using MindTouch.Extensions.Time;

namespace MindTouch.Sqs {
    public class SqsPollingClientSettings {
       
        //--- Fields ---
        public readonly SqsQueueName QueueName;
        public readonly Action<IEnumerable<SqsMessage>> Callback;
        public readonly TimeSpan LongPollInterval;
        public readonly uint MaxNumberOfMessages;
        public readonly TimeSpan WaitTimeOnError;

        //--- Constructors ---
        public SqsPollingClientSettings(SqsQueueName queueName, Action<IEnumerable<SqsMessage>> callback, TimeSpan longPollInterval, uint maxNumberOfMessages, TimeSpan waitTimeOnError) {
            if(callback == null) {
                throw new ArgumentNullException("callback");
            }
            if(longPollInterval == null) {
                throw new ArgumentNullException("longPollInterval");
            }
            if(longPollInterval > 20.Seconds()) {
                throw new ArgumentException("longPollInterval exceeds the limit allowed");
            }
            if(waitTimeOnError == null) {
                throw new ArgumentNullException("waitTimeOnError");
            }
            if(waitTimeOnError < 0.Seconds() || waitTimeOnError > 5.Minutes()) {
                throw new ArgumentException("waitTimeOnError must be greater than 0 up to 5 minutes");
            }
            this.QueueName = queueName;
            this.Callback = callback;
            this.LongPollInterval = longPollInterval;
            this.MaxNumberOfMessages = maxNumberOfMessages;
            this.WaitTimeOnError = waitTimeOnError;
        }
    }
}
