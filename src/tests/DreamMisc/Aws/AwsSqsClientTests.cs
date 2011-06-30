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
                var body1 = "\"&quot; %20+<>:?";
                var r1 = client.Send(queue, AwsSqsMessage.FromBody(body1), new Result<AwsSqsSendResponse>()).Wait();
                _log.DebugFormat("created message {0}", r1.MessageId);
                var doc = new XDoc("doc")
                    .Elem("foo", StringUtil.CreateAlphaNumericKey(100))
                    .Start("baz")
                        .Elem("bar", StringUtil.CreateAlphaNumericKey(100))
                        .Elem("baz", StringUtil.CreateAlphaNumericKey(100))
                    .End()
                    .Elem("bing", StringUtil.CreateAlphaNumericKey(100));
                var body2 = "<deki-event wikiid=\"default\" event-time=\"2011-06-30T16:16:36Z\"><channel>event://default/deki/pages/dependentschanged/properties/update</channel><uri>http://ariel.mindtouch.com/@api/deki/pages/22?redirects=0</uri><pageid>22</pageid><user id=\"1\" anonymous=\"false\"><uri>http://ariel.mindtouch.com/@api/deki/users/1</uri></user><content.uri type=\"application/xml\">http://ariel.mindtouch.com/@api/deki/pages/22/contents?redirects=0&amp;revision=45&amp;format=xhtml</content.uri><revision.uri>http://ariel.mindtouch.com/@api/deki/pages/22/revisions?redirects=0&amp;revision=45</revision.uri><tags.uri>http://ariel.mindtouch.com/@api/deki/pages/22/tags?redirects=0</tags.uri><comments.uri>http://ariel.mindtouch.com/@api/deki/pages/22/comments?redirects=0</comments.uri><path></path><frommove>false</frommove></deki-event>";
                var r2 = client.Send(queue, AwsSqsMessage.FromBody(body2), new Result<AwsSqsSendResponse>()).Wait();
                _log.DebugFormat("created message {0}", r2.MessageId);
                var messages = new List<AwsSqsMessage>();
                Assert.IsTrue(
                    Wait.For(() => {
                        var r = client.Receive(queue, new Result<IEnumerable<AwsSqsMessage>>()).Wait();
                        foreach(var m in r) {
                            _log.DebugFormat("retrieved message {0}", m.MessageId);
                            messages.Add(m);
                        }
                        return messages.Count == 2;
                    },
                    10.Seconds()),
                    string.Format("only received {0} messages from queue before timeout", messages.Count)
                );
                var msg1 = messages.Where(x => x.MessageId == r1.MessageId).FirstOrDefault();
                var msg2 = messages.Where(x => x.MessageId == r2.MessageId).FirstOrDefault();
                Assert.IsNotNull(msg1, "message 1 was not in response");
                Assert.IsNotNull(msg2, "message 2 was not in response");
                Assert.AreEqual(body1, msg1.Body, "msg 1 body didn't match");
                Assert.AreEqual(body2, msg2.Body, "msg 2 body didn't match");
            } finally {
                _log.DebugFormat("cleaning up queue '{0}'", queue);
                client.DeleteQueue(queue, new Result<AwsSqsResponse>()).Wait();
            }
        }
        [Test, Ignore]
        public void LIVE_Send() {
            var client = CreateLiveClient();
            var queue = "encoding-test";
            client.CreateQueue(queue, new Result<AwsSqsResponse>()).Wait();
            var msg = AwsSqsMessage.FromBody("<deki-event wikiid=\"default\" event-time=\"2011-06-30T16:16:36Z\"><channel>event://default/deki/pages/dependentschanged/properties/update</channel><uri>http://ariel.mindtouch.com/@api/deki/pages/22?redirects=0</uri><pageid>22</pageid><user id=\"1\" anonymous=\"false\"><uri>http://ariel.mindtouch.com/@api/deki/users/1</uri></user><content.uri type=\"application/xml\">http://ariel.mindtouch.com/@api/deki/pages/22/contents?redirects=0&amp;revision=45&amp;format=xhtml</content.uri><revision.uri>http://ariel.mindtouch.com/@api/deki/pages/22/revisions?redirects=0&amp;revision=45</revision.uri><tags.uri>http://ariel.mindtouch.com/@api/deki/pages/22/tags?redirects=0</tags.uri><comments.uri>http://ariel.mindtouch.com/@api/deki/pages/22/comments?redirects=0</comments.uri><path></path><frommove>false</frommove></deki-event>");
            var r = client.Send(queue, msg, new Result<AwsSqsSendResponse>()).Block();
            var v = r.Value;
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

        [Test, Ignore]
        public void LIVE_Delete_some_queues() {
            var client = CreateLiveClient();
            var prefix = "test-";
            if(!string.IsNullOrEmpty(prefix)) {
                foreach(var q in client.ListQueues(prefix, new Result<IEnumerable<string>>()).Wait().ToArray()) {
                    _log.DebugFormat("deleting queue: {0}", q);
                    client.DeleteQueue(q, new Result<AwsSqsResponse>()).Wait();
                }
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
