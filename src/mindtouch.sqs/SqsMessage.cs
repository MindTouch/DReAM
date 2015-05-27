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

namespace MindTouch.Sqs {

    /// <summary>
    /// SQS wrapper.
    /// </summary>
    public class SqsMessage {
    
        //--- Fields ---

        /// <summary>
        /// SQS message ID.
        /// </summary>
        public readonly SqsMessageId MessageId;

        /// <summary>
        /// SQS message receipt. Used for deleting message later.
        /// </summary>
        public readonly SqsMessageReceipt MessageReceipt;

        /// <summary>
        /// SQS message body.
        /// </summary>
        public readonly string Body;
    
        //--- Constructors ---

        /// <summary>
        /// Constructor for creating an instance.
        /// </summary>
        /// <param name="messageId">Message ID.</param>
        /// <param name="messageReceipt">Message receipt.</param>
        /// <param name="body">Message body.</param>
        public SqsMessage(SqsMessageId messageId, SqsMessageReceipt messageReceipt, string body) {
            this.MessageId = messageId;
            this.MessageReceipt = messageReceipt;
            this.Body = body;
        }
    }
}
