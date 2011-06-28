using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MindTouch.Aws;
using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Dream.Test.Aws {

    [TestFixture]
    public class AwsSqsClientTests {

        [Test, Ignore]
        public void Livetest_Can_create_and_delete_queue() {
            var config = new AwsSqsClientConfig() {
            };
            var client = new AwsSqsClient(config);
            var queue = "test-" + StringUtil.CreateAlphaNumericKey(8);
            client.CreateQueue(queue, new Result<AwsSqsResponse>()).Wait();
            Assert.AreEqual(new[] { queue }, client.ListQueues(queue, new Result<IEnumerable<string>>()).Wait().ToArray());
            client.DeleteQueue(queue, new Result<AwsSqsResponse>()).Wait();
            Assert.IsFalse(client.ListQueues("test-", new Result<IEnumerable<string>>()).Wait().Any());
        }
    }
}
