/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2010 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
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
using MindTouch.Tasking;

namespace MindTouch.Aws {
    public class AwsSqsClient : IAwsSqsClient {

        //--- Fields ---
        private readonly AwsSqsClientConfig _config;

        //--- Constructors ---
        public AwsSqsClient(AwsSqsClientConfig config) {
            _config = config;
        }

        //--- Methods ---
        public Result<string> Send(string queue, AwsSqsMessage message, Result<string> result) {
            throw new NotImplementedException();
        }

        public Result<IEnumerable<AwsSqsMessage>> Receive(string queue, int maxMessages, TimeSpan visibilityTimeout, Result<IEnumerable<AwsSqsMessage>> result) {
            throw new NotImplementedException();
        }

        public Result Delete(AwsSqsMessage message, Result result) {
            throw new NotImplementedException();
        }

        public Result CreateQueue(string queue, TimeSpan defaultVisibilityTimeout, Result result) {
            throw new NotImplementedException();
        }

        public Result DeleteQueue(string queue, Result result) {
            throw new NotImplementedException();
        }
    }
}