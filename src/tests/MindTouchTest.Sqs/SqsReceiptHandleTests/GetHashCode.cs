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
using System.Globalization;
using System.Linq;
using MindTouch.Sqs;
using NUnit.Framework;

namespace MindTouchTest.Sqs.SqsReceiptHandleTests {
    [TestFixture]
    public class GetHashCode {
    
        //--- Methods ---
        [Test]
        public void hash_codes_for_receipt_handles_with_same_handle_value_are_always_equal() {
            var random = new Random((int)GlobalClock.UtcNow.Ticks);
            for(var i = 0; i < 1000; i++) {
                var sqsReceiptHandle = random.Next();
                var sqsReceiptHandleHash = new SqsMessageReceipt(sqsReceiptHandle.ToString(CultureInfo.InvariantCulture)).GetHashCode();
                var sqsReceiptHandles = Enumerable.Repeat(sqsReceiptHandle, 1000).Select(integer => integer.ToString(CultureInfo.InvariantCulture).GetHashCode()).ToArray();
                
                // Test that the hash codes match
                for(var j = 0; j < sqsReceiptHandles.Count(); j++) {
                    Assert.AreEqual(sqsReceiptHandleHash, sqsReceiptHandles[j], string.Format("SqsReceiptHandle '{0}' with hash '{1}', its clone did not match its hash '{2}'", sqsReceiptHandle, sqsReceiptHandleHash, sqsReceiptHandles[j]));
                }
            }
        }
    }
}
