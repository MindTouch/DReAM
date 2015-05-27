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

using MindTouch.Sqs;
using MindTouch.Extensions.Time;
using NUnit.Framework;

namespace MindTouchTest.Sqs.SqsClientTests {
    [TestFixture]
    public class ReceiveMessage : _Init {

        //--- Methods ---
        [Test, Ignore]
        public void when_the_long_poll_internal_is_larger_than_default_nothing_bombs() {
            var messages = _client.ReceiveMessages(TEST_QUEUE, 22.Seconds(), SqsUtils.MAX_NUMBER_OF_MESSAGES_TO_FETCH);
            Assert.AreNotEqual(null, messages);
        }
    }
}
