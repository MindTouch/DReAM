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
    /// Value type for SQS message receipt.
    /// </summary>
    public struct SqsMessageReceipt {

        //--- Operators ---

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="a">First SQS message receipt.</param>
        /// <param name="b">Second SQS message receipt.</param>
        /// <returns>True if equal.</returns>
        public static bool operator ==(SqsMessageReceipt a, SqsMessageReceipt b) {
            return a.Value == b.Value;
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="a">First SQS message receipt.</param>
        /// <param name="b">Second SQS message receipt.</param>
        /// <returns>True if not equal.</returns>
        public static bool operator !=(SqsMessageReceipt a, SqsMessageReceipt b) {
            return !(a == b);
        }
    
        //--- Fields ---

        /// <summary>
        /// Value of the SQS message ID.
        /// </summary>
        public readonly string Value;

        //--- Constructors ---

        /// <summary>
        /// Constructor for creating an instance.
        /// </summary>
        /// <param name="messageReceipt">Message receipt.</param>
        public SqsMessageReceipt(string messageReceipt) {
            if(messageReceipt == null) {
                throw new ArgumentNullException("messageReceipt");
            }
            this.Value = messageReceipt;
        }
        
        //--- Methods ---

        /// <summary>
        /// Convert SQS message receipt to string.
        /// </summary>
        /// <returns>String value.</returns>
        public override string ToString() {
            return Value;
        }

        /// <summary>
        /// Get hash code of SQS message receipt.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Compare SQS message receipts.
        /// </summary>
        /// <param name="messageReceipt">Other SQS message receipt.</param>
        /// <returns>True if equal.</returns>
        public bool Equals(SqsMessageReceipt messageReceipt) {
            return Value.Equals(messageReceipt.Value);
        }

        /// <summary>
        /// Compare SQS message receipt to other object.
        /// </summary>
        /// <param name="obj">Other object.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj) {
            if(obj == null || !(obj is SqsMessageReceipt)) {
                return false;
            }
            var receipt = (SqsMessageReceipt)obj;
            return Value == receipt.Value;
        }
    }
}
