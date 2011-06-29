using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using MindTouch.Aws;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test.Aws {

    [TestFixture]
    public class AwsSqsClientTests {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        [Test, Ignore]
        public void LIVE_Can_send_and_receive() {
            var client = CreateLiveClient();
            var queue = "test-" + StringUtil.CreateAlphaNumericKey(8);
            client.CreateQueue(queue, new Result<AwsSqsResponse>()).Wait();
            try {
                var doc = new XDoc("doc").Elem("foo", StringUtil.CreateAlphaNumericKey(10));
                var r = client.Send(queue, AwsSqsMessage.FromBodyDocument(doc), new Result<AwsSqsSendResponse>()).Wait();
                _log.DebugFormat("create message {0}", r.MessageId);
                var messages = client.Receive(queue, new Result<IEnumerable<AwsSqsMessage>>()).Wait();
                Assert.AreEqual(1, messages.Count());
                var msg = messages.First();
                Assert.AreEqual(r.MessageId, msg.MessageId);
                Assert.AreEqual(doc.ToCompactString(), msg.Body);
            } finally {
                _log.DebugFormat("cleaning up queue '{0}'", queue);
                client.DeleteQueue(queue, new Result<AwsSqsResponse>()).Wait();
            }
        }

        [Test, Ignore]
        public void LIVE_Can_create_and_delete_queue() {
            var client = CreateLiveClient();
            var queue = "test-" + StringUtil.CreateAlphaNumericKey(8);
            client.CreateQueue(queue, new Result<AwsSqsResponse>()).Wait();
            var queues = client.ListQueues(queue, new Result<IEnumerable<string>>()).Wait().ToArray();
            foreach(var q in queues) {
                _log.DebugFormat("queue: {0}", q);
            }
            Assert.AreEqual(new[] { queue }, queues);
            client.DeleteQueue(queue, new Result<AwsSqsResponse>()).Wait();
            Thread.Sleep(10.Seconds());
            Assert.IsTrue(Wait.For(() => {
                Thread.Sleep(1.Seconds());
                return !client.ListQueues(queue, new Result<IEnumerable<string>>()).Wait().Any();
            }, 30.Seconds()), "queue did not get deleted"
            );
        }

        [Test, Ignore]
        public void LIVE_List_test_queues() {
            var client = CreateLiveClient();
            foreach(var q in client.ListQueues("test-", new Result<IEnumerable<string>>()).Wait().ToArray()) {
                _log.DebugFormat("queue: {0}", q);
            }
        }

        [Test, Ignore]
        public void LIVE_List_all_queues() {
            var client = CreateLiveClient();
            foreach(var q in client.ListQueues(null, new Result<IEnumerable<string>>()).Wait().ToArray()) {
                _log.DebugFormat("queue: {0}", q);
            }
        }

        [Test, Ignore]
        public void LIVE_Delete_all_test_queues() {
            var client = CreateLiveClient();
            foreach(var q in client.ListQueues("test-", new Result<IEnumerable<string>>()).Wait().ToArray()) {
                _log.DebugFormat("queue: {0}", q);
                client.DeleteQueue(q, new Result<AwsSqsResponse>()).Wait();
            }
        }

        private AwsSqsClient CreateLiveClient() {
            return new AwsSqsClient(new AwsSqsClientConfig() {
                PublicKey = ConfigurationManager.AppSettings["aws.sqs.publickey"],
                PrivateKey = ConfigurationManager.AppSettings["aws.sqs.privatekey"],
                AccountId = ConfigurationManager.AppSettings["aws.sqs.accountid"]
            });
        }
    }
}
