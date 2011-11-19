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
using System.Collections.Generic;
using System.Linq;
using MindTouch.Aws;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Dream.Test.Aws {
    [TestFixture]
    public class SqsPollClientTests {

        [Test]
        public void Poll_client_pulls_from_sqs_and_calls_callback() {
            var mockSqsClient = new MockSqsClient();
            mockSqsClient.FillQueue(15);
            var pollster = new SqsPollClient(mockSqsClient, TaskTimerFactory.Current);
            Assert.AreEqual(15, mockSqsClient.Queued.Count, "queue was accessed prematurely");
            var posted = new List<AwsSqsMessage>();
            pollster.Listen("foo", 300.Seconds(), posted.Add);
            Assert.IsTrue(Wait.For(() => mockSqsClient.Queued.Count == 0, 10.Seconds()), "queue did not get depleted in time");
            Assert.IsTrue(
                Wait.For(() => mockSqsClient.ReceiveCalled == 3, 5.Seconds()),
                string.Format("receive called the wrong number of times: {0} != {1}", 3, mockSqsClient.ReceiveCalled)
                );
            Assert.AreEqual(15, mockSqsClient.Delivered.Count, "delivered has the wrong number of messages");
            Assert.AreEqual(
                mockSqsClient.Delivered.Select(x => x.MessageId).ToArray(),
                mockSqsClient.Deleted.Select(x => x.MessageId).ToArray(),
                "delivered and deleted don't match"
                );
            Assert.AreEqual(
                mockSqsClient.Delivered.Select(x => x.MessageId).ToArray(),
                posted.Select(x => x.MessageId).ToArray(),
                "delivered and posted don't match"
                );
        }
    }
}