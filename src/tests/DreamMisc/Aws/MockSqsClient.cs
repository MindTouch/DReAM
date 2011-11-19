/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
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
            var r = new Result<IEnumerable<AwsSqsMessage>>();
            var take = Math.Min(10, maxMessages);
            var taken = Queued.Take(take).ToArray();
            _log.DebugFormat("receive returning {0} messages", taken.Length);
            Delivered.AddRange(taken);
            r.Return(taken);
            Queued.RemoveRange(0, taken.Length);
            return r;
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