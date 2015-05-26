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

namespace MindTouch.Sqs {

    /// <summary>
    /// Value type for SQS message ID.
    /// </summary>
    public struct SqsMessageId {

        //--- Operators ---

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="a">First SQS message ID.</param>
        /// <param name="b">Second SQS message ID.</param>
        /// <returns>True if equal.</returns>
        public static bool operator ==(SqsMessageId a, SqsMessageId b) {
            return a.Value == b.Value;
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="a">First SQS message ID.</param>
        /// <param name="b">Second SQS message ID.</param>
        /// <returns>True if not equal.</returns>
        public static bool operator !=(SqsMessageId a, SqsMessageId b) {
            return !(a == b);
        }
    
        //--- Fields ---

        /// <summary>
        /// Value of the SQS message ID.
        /// </summary>
        public readonly string Value;

        //--- Constructor ---

        /// <summary>
        /// Constructor for creating an instance.
        /// </summary>
        /// <param name="messageId">Message ID.</param>
        public SqsMessageId(string messageId) {
            if(messageId == null) {
                throw new ArgumentNullException("messageId");
            }
            this.Value = messageId;
        }

        //--- Methods ---

        /// <summary>
        /// Convert SQS message ID to string.
        /// </summary>
        /// <returns>String value.</returns>
        public override string ToString() {
            return Value;
        }

        /// <summary>
        /// Get hash code of SQS message ID.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Compare SQS message IDs.
        /// </summary>
        /// <param name="messageId">Other SQS message ID.</param>
        /// <returns>True if equal.</returns>
        public bool Equals(SqsMessageId messageId) {
            return Value.Equals(messageId.Value);
        }

        /// <summary>
        /// Compare SQS message ID to other object.
        /// </summary>
        /// <param name="obj">Other object.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj) {
            if(obj == null || !(obj is SqsMessageId)) {
                return false;
            }
            var messageId = (SqsMessageId)obj;
            return Value == messageId.Value;
        }
    }
}
