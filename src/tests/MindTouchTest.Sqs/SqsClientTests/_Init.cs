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
using MindTouch;
using MindTouch.Extensions.Time;
using MindTouch.Sqs;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouchTest.Sqs.SqsClientTests {
    public class _Init {
    
        //--- Fields ---
        protected SqsClient _client;
        protected SqsQueueName TEST_QUEUE;

        //--- Methods ---
        [SetUp]
        public void Init() {
            var accountId = Environment.GetEnvironmentVariable("SQS_ACCOUNT_ID");
            if(string.IsNullOrEmpty(accountId)) {
                accountId = "accountid";
            }
            var publicKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            if(string.IsNullOrEmpty(publicKey)) {
                publicKey = "publickey";
            }
            var privateKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            if(string.IsNullOrEmpty("privatekey")) {
                privateKey = "privatekey";
            }
            _client = new SqsClient(SqsClientConfig.From(new XDoc("sqs-config")
                .Elem("endpoint", "default")
                .Elem("accountid", accountId)
                .Elem("publickey", publicKey)
                .Elem("privatekey", privateKey)));
            TEST_QUEUE = new SqsQueueName("steveb-events");

            // purge the queue
            while(true) {
                var messages = _client.ReceiveMessages(TEST_QUEUE, 1.Seconds(), SqsUtils.MAX_NUMBER_OF_MESSAGES_TO_FETCH);
                if(messages.None()) {
                    break;
                }
                _client.DeleteMessages(TEST_QUEUE, messages);
            }
        }

        [TearDown]
        public void Teardown() {
            _client = null;
        }
    }
}
