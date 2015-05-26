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

namespace MindTouch.Sqs {

    /// <summary>
    /// SQS constants.
    /// </summary>
    public static class SqsUtils {

        //--- Constants ---

        /// <summary>
        /// Max number of messages that can be fetched from SQS at once.
        /// </summary>
        public const uint MAX_NUMBER_OF_MESSAGES_TO_FETCH = 10u;

        /// <summary>
        /// Max number of message that can be deleted from SQS at once.
        /// </summary>
        public const uint MAX_NUMBER_OF_BATCH_DELETE_MESSAGES = 10u;

        /// <summary>
        /// Max number of message that can be sent to SQS at once.
        /// </summary>
        public const int MAX_NUMBER_OF_BATCH_SEND_MESSAGES = 10;

        /// <summary>
        /// Max time-span that long-poll can wait for messages from SQS.
        /// </summary>
        public static readonly TimeSpan MAX_LONG_POLL_WAIT_TIME = 20.Seconds();

        /// <summary>
        /// Default time-span to wait between attempts to read message from SQS.
        /// </summary>
        public static readonly TimeSpan DEFAULT_WAIT_TIME_ON_ERROR = 1.Seconds();
    }
}
