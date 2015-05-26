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
    public struct SqsMessageReceipt {
    
        //--- Fields ---
        public readonly string Value;

        //--- Constructors ---
        public SqsMessageReceipt(string messageReceipt) {
            if(messageReceipt == null) {
                throw new ArgumentNullException("messageReceipt");
            }
            this.Value = messageReceipt;
        }
        
        //--- Methods ---
        public override string ToString() {
            return Value;
        }

        public override int GetHashCode() {
            return Value.GetHashCode();
        }

        public bool Equals(SqsMessageReceipt messageReceipt) {
            return Value.Equals(messageReceipt.Value);
        }

        public override bool Equals(object obj) {
            if(obj == null || !(obj is SqsMessageReceipt)) {
                return false;
            }
            var receipt = (SqsMessageReceipt)obj;
            return Value == receipt.Value;
        }

        public static bool operator ==(SqsMessageReceipt a, SqsMessageReceipt b) {
            return a.Value == b.Value;
        }

        public static bool operator !=(SqsMessageReceipt a, SqsMessageReceipt b) {
            return !(a == b);
        }
    }
}
