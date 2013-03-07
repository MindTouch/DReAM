/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2011 MindTouch, Inc.
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
using System.Linq;
using log4net;
using MindTouch.Aws;
using MindTouch.Tasking;

namespace MindTouch.Dream.Test.Aws {
    public class MockSqsClient : IAwsSqsClient {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        public List<AwsSqsMessage> Queued = new List<AwsSqsMessage>();
        public List<AwsSqsMessage> Delivered = new List<AwsSqsMessage>();
        public List<AwsSqsMessage> Deleted = new List<AwsSqsMessage>();
        public int ReceiveCalled;

        public void FillQueue(int count) {
            for(var i = 0; i < count; i++) {
                Queued.Add(new MockMessage());
            }
        }

        public Result<AwsSqsSendResponse> Send(string queue, AwsSqsMessage message, Result<AwsSqsSendResponse> result) {
            throw new NotImplementedException();
        }

        public Result<IEnumerable<AwsSqsMessage>> Receive(string queue, int maxMessages, TimeSpan visibilityTimeout, Result<IEnumerable<AwsSqsMessage>> result) {
            ReceiveCalled++;
            var take = Math.Min(10, maxMessages);
            var taken = Queued.Take(take).ToArray();
            _log.DebugFormat("receive returning {0} messages", taken.Length);
            Delivered.AddRange(taken);
            result.Return(taken);
            Queued.RemoveRange(0, taken.Length);
            return result;
        }

        public Result<AwsSqsResponse> Delete(AwsSqsMessage message, Result<AwsSqsResponse> result) {
            _log.DebugFormat("deleting {0}", message.MessageId);
            Deleted.Add(message);
            return new Result<AwsSqsResponse>().WithReturn(null);
        }

        public Result<AwsSqsResponse> CreateQueue(string queue, TimeSpan defaultVisibilityTimeout, Result<AwsSqsResponse> result) {
            throw new NotImplementedException();
        }

        public Result<AwsSqsResponse> DeleteQueue(string queue, Result<AwsSqsResponse> result) {
            throw new NotImplementedException();
        }

        public Result<IEnumerable<string>> ListQueues(string prefix, Result<IEnumerable<string>> result) {
            throw new NotImplementedException();
        }
    }
}