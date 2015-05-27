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
    /// Value type for SQS queue name.
    /// </summary>
    public struct SqsQueueName {
    
        //--- Fields ---

        /// <summary>
        /// Value of the SQS message ID.
        /// </summary>
        public readonly string Value;

        //--- Constructors ---
        
        /// <summary>
        /// Constructor for creating an instance.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        public SqsQueueName(string queueName) {
            if(string.IsNullOrEmpty("queueName")) {
                throw new ArgumentNullException("queueName");
            }
            this.Value = queueName;
        }

        /// <summary>
        /// Convert SQS queue name to string.
        /// </summary>
        /// <returns>String value.</returns>
        public override string ToString() {
            return Value;
        }

        /// <summary>
        /// Get hash code of SQS queue name.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Compare SQS queue names to other object.
        /// </summary>
        /// <param name="obj">Other object.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj) {
            if(obj == null || !(obj is SqsQueueName)) {
                return false;
            }
            var other = (SqsQueueName)obj;
            return Value == other.Value;
        }
    }
}
