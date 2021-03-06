﻿/*
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

    /// <summary>
    /// SqsPollingClient listener settings.
    /// </summary>
    public class SqsPollingClientSettings {
       
        //--- Fields ---

        /// <summary>
        /// Queue name to listen on.
        /// </summary>
        public readonly SqsQueueName QueueName;

        /// <summary>
        /// Callback for received SQS messages.
        /// </summary>
        public readonly Action<IEnumerable<SqsMessage>> Callback;

        /// <summary>
        /// Max amount of time to wait for SQS message to arrive.
        /// </summary>
        public readonly TimeSpan LongPollInterval;

        /// <summary>
        /// Max number of SQS messages to request.
        /// </summary>
        public readonly uint MaxNumberOfMessages;

        /// <summary>
        /// Amount of time to wait until trying again to listen after an error occurred.
        /// </summary>
        public readonly TimeSpan WaitTimeOnError;

        //--- Constructors ---

        /// <summary>
        /// Constructor for creating an instance.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="callback">Callback for received SQS messages.</param>
        /// <param name="longPollInterval">Max amount of time to wait for SQS message to arrive.</param>
        /// <param name="maxNumberOfMessages">Max number of SQS messages to request.</param>
        /// <param name="waitTimeOnError">Amount of time to wait until trying again to listen after an error occurred.</param>
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
