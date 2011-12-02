using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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
        public void LIVE_Produce_Consume() {
            var client = CreateLiveClient();
            var queue = "test-" + StringUtil.CreateAlphaNumericKey(8);
            client.CreateQueue(queue, new Result<AwsSqsResponse>()).Wait();
            try {
                var producer = new Producer(queue, GetConfig());
                var n = 100;
                var r = producer.Produce(n);
                var consumer = new Consumer(queue, CreateLiveClient());
                consumer.Consume();
                var production = r.Wait();
                Assert.AreEqual(n, production.Sent.Count, "wrong number of sent messages");
                Assert.IsTrue(
                    Wait.For(() => consumer.Received == n, (n * 0.5).Seconds()),
                    string.Format("got {0} instead of the expected {1} messages", consumer.Received, n)
                );
                var consumed = consumer.Stop();
                Assert.AreEqual(
                    production.Sent.OrderBy(x => x).ToArray(),
                    consumed.OrderBy(x => x).ToArray(),
                    "wrong set of messages"
                );
            } finally {
                _log.DebugFormat("cleaning up queue '{0}'", queue);
                client.DeleteQueue(queue, new Result<AwsSqsResponse>()).Wait();
            }
        }

        [Test, Ignore("stress test")]
        public void LIVE_STRESSTEST_Produce_Consume() {
            var client = CreateLiveClient();
            var queue = "test-" + StringUtil.CreateAlphaNumericKey(8);
            client.CreateQueue(queue, new Result<AwsSqsResponse>()).Wait();
            try {
                var n = 100;
                var productions = new List<Result<Production>>();
                var producer = new Producer(queue, GetConfig());
                for(var i = 0; i < 4; i++) {
                    productions.Add(producer.Produce(n));
                }
                productions.Join(new Result()).Wait();
                var totalMessages = productions.Count * n;
                var consumers = new List<Consumer>();
                for(var i = 0; i < 10; i++) {
                    var consumer = new Consumer(queue, CreateLiveClient());
                    consumer.Consume();
                    consumers.Add(consumer);
                }
                Assert.IsTrue(
                    Wait.For(() => consumers.Sum(x => x.Received) == totalMessages, (totalMessages * 0.2).Seconds()),
                    string.Format("got {0} instead of the expected {1} messages", consumers.Sum(x => x.Received), totalMessages)
                );
            } finally {
                _log.DebugFormat("cleaning up queue '{0}'", queue);
                client.DeleteQueue(queue, new Result<AwsSqsResponse>()).Wait();
            }
        }

        [Test, Ignore]
        public void LIVE_Send_one_for_inspection() {
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
            return new AwsSqsClient(GetConfig());
        }

        private AwsSqsClientConfig GetConfig() {
            return new AwsSqsClientConfig {
                PublicKey = ConfigurationManager.AppSettings["aws.sqs.publickey"],
                PrivateKey = ConfigurationManager.AppSettings["aws.sqs.privatekey"],
                AccountId = ConfigurationManager.AppSettings["aws.sqs.accountid"]
            };
        }
    }

    public class Consumer {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static int _id = 0;

        //--- Fields ---
        public readonly int Id = _id++;
        private readonly string _queue;
        private readonly IAwsSqsClient _client;
        private readonly List<string> _received = new List<string>();
        private bool _stopped;

        public Consumer(string queue, IAwsSqsClient client) {
            _queue = queue;
            _client = client;
        }

        public int Received {
            get {
                if(Exception != null) {
                    throw Exception;
                }
                lock(_received) {
                    return _received.Count;
                }
            }
        }

        public Exception Exception { get; private set; }

        public void Consume() {
            Receive();
        }

        private void Receive() {
            _log.DebugFormat("{0}: receiving messages", Id);
            if(_stopped) {
                return;
            }
            _client.ReceiveMax(_queue, 10.Minutes(), new Result<IEnumerable<AwsSqsMessage>>()).WhenDone(r => {
                if(r.HasException) {
                    Exception = r.Exception;
                    return;
                }
                if(_stopped) {
                    return;
                }
                var received = new List<AwsSqsMessage>(r.Value);
                if(!received.Any()) {
                    _log.DebugFormat("{0}: no messages in queue, sleeping before retry", Id);
                    Async.Sleep(1.Seconds()).WhenDone(r2 => Receive());
                    return;
                }
                _log.DebugFormat("{0}: received {1} messages", Id, received.Count);
                var deleted = r.Value.Select(x => {
                    var deleteResult = new Result<string>();
                    _client.Delete(x, new Result<AwsSqsResponse>()).WhenDone(r2 => {
                        if(r2.HasException) {
                            deleteResult.Throw(r2.Exception);
                            return;
                        }
                        deleteResult.Return(x.Body);
                    });
                    return deleteResult;
                }).ToList();
                deleted.Join(new Result()).WhenDone(r2 => {
                    if(r2.HasException) {
                        _log.DebugFormat("one or more deletes failed");
                        Exception = r2.Exception;
                        return;
                    }
                    if(_stopped) {
                        return;
                    }
                    lock(_received) {
                        _received.AddRange(deleted.Select(x => x.Value));
                    }
                    Receive();
                });
            });
        }

        public List<string> Stop() {
            if(Exception != null) {
                throw Exception;
            }
            _stopped = true;
            _log.DebugFormat("{0}: stopped", Id);
            return _received;
        }
    }

    public class Producer {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        private readonly string _queue;
        private readonly AwsSqsClientConfig _clientConfig;

        public Producer(string queue, AwsSqsClientConfig clientConfig) {
            _queue = queue;
            _clientConfig = clientConfig;
        }

        public Result<Production> Produce(int messages) {
            var production = new Production();
            _log.DebugFormat("{0}: Producing {1} messages", production.Id, messages);
            var responses = new List<Result<string>>();
            var client = new AwsSqsClient(_clientConfig);
            for(var i = 0; i < messages; i++) {
                var result = new Result<string>();
                responses.Add(result);
                var msg = production.Id + ":" + messages;
                client.Send(_queue, AwsSqsMessage.FromBody(msg), new Result<AwsSqsSendResponse>()).WhenDone(r => {
                    if(r.HasException) {
                        result.Throw(r.Exception);
                        return;
                    }
                    result.Return(msg);
                });
            }
            var final = new Result<Production>();
            responses.Join(new Result()).WhenDone(r => {
                if(r.HasException) {
                    final.Throw(r.Exception);
                    return;
                }
                production.Stopwatch.Stop();
                production.Sent.AddRange(responses.Select(x => x.Value));
                _log.DebugFormat("{0}: Sent {1} messages in {2:0.00}s @ {3:0.00}msg/sec",
                    production.Id,
                    production.Sent.Count,
                    production.Stopwatch.Elapsed.TotalSeconds,
                    production.Sent.Count / production.Stopwatch.Elapsed.TotalSeconds
                );
                final.Return(production);
            });
            return final;
        }

    }

    public class Production {
        private static int _id = 0;

        //--- Fields ---
        public readonly int Id = _id++;
        public readonly List<string> Sent = new List<string>();
        public readonly Stopwatch Stopwatch = Stopwatch.StartNew();
    }
}
