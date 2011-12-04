using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using MindTouch.Aws;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test.Aws {

    [TestFixture]
    public class AwsSqsClientTests2 {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        [Test, Ignore]
        public void LIVE_Can_send_and_receive() {
            var client = CreateLiveClient();
            var createQueueRequest = new CreateQueueRequest { QueueName = "test-" + StringUtil.CreateAlphaNumericKey(8) };
            var createQueueResponse = client.CreateQueue(createQueueRequest);
            var queueUrl = createQueueResponse.CreateQueueResult.QueueUrl;
            _log.DebugFormat("using queue {0}", queueUrl);
            try {
                var body1 = "\"&quot; %20+<>:?";
                var sendMessageRequest = new SendMessageRequest() { QueueUrl = queueUrl, MessageBody = body1 };
                var r1 = client.SendMessage(sendMessageRequest);
                _log.DebugFormat("created message {0}", r1.SendMessageResult.MessageId);
                var body2 = "<deki-event wikiid=\"default\" event-time=\"2011-06-30T16:16:36Z\"><channel>event://default/deki/pages/dependentschanged/properties/update</channel><uri>http://ariel.mindtouch.com/@api/deki/pages/22?redirects=0</uri><pageid>22</pageid><user id=\"1\" anonymous=\"false\"><uri>http://ariel.mindtouch.com/@api/deki/users/1</uri></user><content.uri type=\"application/xml\">http://ariel.mindtouch.com/@api/deki/pages/22/contents?redirects=0&amp;revision=45&amp;format=xhtml</content.uri><revision.uri>http://ariel.mindtouch.com/@api/deki/pages/22/revisions?redirects=0&amp;revision=45</revision.uri><tags.uri>http://ariel.mindtouch.com/@api/deki/pages/22/tags?redirects=0</tags.uri><comments.uri>http://ariel.mindtouch.com/@api/deki/pages/22/comments?redirects=0</comments.uri><path></path><frommove>false</frommove></deki-event>";
                var r2 = client.SendMessage(new SendMessageRequest { QueueUrl = queueUrl, MessageBody = body2 });
                _log.DebugFormat("created message {0}", r2.SendMessageResult.MessageId);
                var messages = new List<Message>();
                Assert.IsTrue(
                    Wait.For(() => {
                        var r = client.ReceiveMessage(new ReceiveMessageRequest { QueueUrl = queueUrl, MaxNumberOfMessages = 10 });
                        foreach(var m in r.ReceiveMessageResult.Message) {
                            _log.DebugFormat("retrieved message {0}", m.MessageId);
                            messages.Add(m);
                        }
                        return messages.Count == 2;
                    },
                    10.Seconds()),
                    string.Format("only received {0} messages from queue before timeout", messages.Count)
                );
                var msg1 = messages.Where(x => x.MessageId == r1.SendMessageResult.MessageId).FirstOrDefault();
                var msg2 = messages.Where(x => x.MessageId == r2.SendMessageResult.MessageId).FirstOrDefault();
                Assert.IsNotNull(msg1, "message 1 was not in response");
                Assert.IsNotNull(msg2, "message 2 was not in response");
                Assert.AreEqual(body1, msg1.Body, "msg 1 body didn't match");
                Assert.AreEqual(body2, msg2.Body, "msg 2 body didn't match");
            } finally {
                _log.DebugFormat("cleaning up queue '{0}'", queueUrl);
                client.DeleteQueue(new DeleteQueueRequest { QueueUrl = queueUrl });
            }
        }

        [Test, Ignore]
        public void LIVE_Produce_Consume() {
            var client = CreateLiveClient();
            var createQueueRequest = new CreateQueueRequest { QueueName = "test-" + StringUtil.CreateAlphaNumericKey(8) };
            var createQueueResponse = client.CreateQueue(createQueueRequest);
            var queueUrl = createQueueResponse.CreateQueueResult.QueueUrl;
            _log.DebugFormat("using queue {0}", queueUrl);
            try {
                var producer = new Producer2(queueUrl, GetConfig());
                var n = 100;
                var r = producer.Produce(n);
                var consumer = new Consumer2(queueUrl, CreateLiveClient());
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
                _log.DebugFormat("cleaning up queue '{0}'", queueUrl);
                try {
                    client.DeleteQueue(new DeleteQueueRequest { QueueUrl = queueUrl });
                } catch { }
            }
        }

        [Test, Ignore("stress test")]
        public void LIVE_STRESSTEST_Produce_Consume() {
            var client = CreateLiveClient();
            var createQueueRequest = new CreateQueueRequest { QueueName = "test-" +StringUtil.CreateAlphaNumericKey(8) };
            var createQueueResponse = client.CreateQueue(createQueueRequest);
            var queueUrl = createQueueResponse.CreateQueueResult.QueueUrl;
            _log.DebugFormat("using queue {0}", queueUrl);
            try {
                var n = 100;
                var productions = new List<Result<Production>>();
                var producer = new Producer2(queueUrl, GetConfig());
                for(var i = 0; i < 4; i++) {
                    productions.Add(producer.Produce(n));
                }
                productions.Join(new Result()).Wait();
                var totalMessages = productions.Count * n;
                var consumers = new List<Consumer2>();
                for(var i = 0; i < 10; i++) {
                    var consumer = new Consumer2(queueUrl, CreateLiveClient());
                    consumer.Consume();
                    consumers.Add(consumer);
                }
                Assert.IsTrue(
                    Wait.For(() => consumers.Sum(x => x.Received) == totalMessages, (totalMessages * 0.2).Seconds()),
                    string.Format("got {0} instead of the expected {1} messages", consumers.Sum(x => x.Received), totalMessages)
                );
            } finally {
                _log.DebugFormat("cleaning up queue '{0}'", queueUrl);
                try {
                    client.DeleteQueue(new DeleteQueueRequest { QueueUrl = queueUrl });
                } catch { }
            }
        }

        [Test]
        public void LIVE_LONGTEST_Produce_Consume() {
            var client = CreateLiveClient();
            var createQueueRequest = new CreateQueueRequest { QueueName = "test-" + StringUtil.CreateAlphaNumericKey(8) };
            var createQueueResponse = client.CreateQueue(createQueueRequest);
            var queueUrl = createQueueResponse.CreateQueueResult.QueueUrl;
            _log.DebugFormat("using queue {0}", queueUrl);
            try {
                var n = 500;
                var productions = new List<Result<Production>>();
                var producer = new Producer2(queueUrl, GetConfig());
                for(var i = 0; i < 6; i++) {
                    productions.Add(producer.Produce(n));
                }
                var r = productions.Join(new Result());
                var consumers = new List<Consumer2>();
                for(var i = 0; i < 4; i++) {
                    var consumer = new Consumer2(queueUrl, CreateLiveClient());
                    consumer.Consume();
                    consumers.Add(consumer);
                }
                while(!r.HasFinished) {
                    Thread.Sleep(5000);
                    _log.DebugFormat("received {0} messages", consumers.Sum(x => x.Received));
                }
                var totalMessages = productions.Count * n;
                Assert.IsTrue(
                    Wait.For(() => consumers.Sum(x => x.Received) == totalMessages, (totalMessages * 0.1).Seconds()),
                    string.Format("got {0} instead of the expected {1} messages", consumers.Sum(x => x.Received), totalMessages)
                );
            } finally {
                _log.DebugFormat("cleaning up queue '{0}'", queueUrl);
                try {
                    client.DeleteQueue(new DeleteQueueRequest { QueueUrl = queueUrl });
                } catch { }
            }
        }

        //[Test, Ignore]
        //public void LIVE_Send_one_for_inspection() {
        //    var client = CreateLiveClient();
        //    var queue = "encoding-test";
        //    client.CreateQueue(queue, new Result<AwsSqsResponse>()).Wait();
        //    var msg = AwsSqsMessage.FromBody("<deki-event wikiid=\"default\" event-time=\"2011-06-30T16:16:36Z\"><channel>event://default/deki/pages/dependentschanged/properties/update</channel><uri>http://ariel.mindtouch.com/@api/deki/pages/22?redirects=0</uri><pageid>22</pageid><user id=\"1\" anonymous=\"false\"><uri>http://ariel.mindtouch.com/@api/deki/users/1</uri></user><content.uri type=\"application/xml\">http://ariel.mindtouch.com/@api/deki/pages/22/contents?redirects=0&amp;revision=45&amp;format=xhtml</content.uri><revision.uri>http://ariel.mindtouch.com/@api/deki/pages/22/revisions?redirects=0&amp;revision=45</revision.uri><tags.uri>http://ariel.mindtouch.com/@api/deki/pages/22/tags?redirects=0</tags.uri><comments.uri>http://ariel.mindtouch.com/@api/deki/pages/22/comments?redirects=0</comments.uri><path></path><frommove>false</frommove></deki-event>");
        //    var r = client.Send(queue, msg, new Result<AwsSqsSendResponse>()).Block();
        //    var v = r.Value;
        //}

        //[Test, Ignore]
        //public void LIVE_Can_create_and_delete_queue() {
        //    var client = CreateLiveClient();
        //    var queue = "test-" + StringUtil.CreateAlphaNumericKey(8);
        //    client.CreateQueue(queue, new Result<AwsSqsResponse>()).Wait();
        //    var queues = client.ListQueues(queue, new Result<IEnumerable<string>>()).Wait().ToArray();
        //    foreach(var q in queues) {
        //        _log.DebugFormat("queue: {0}", q);
        //    }
        //    Assert.AreEqual(new[] { queue }, queues);
        //    client.DeleteQueue(queue, new Result<AwsSqsResponse>()).Wait();
        //    Thread.Sleep(10.Seconds());
        //    Assert.IsTrue(Wait.For(() => {
        //        Thread.Sleep(1.Seconds());
        //        return !client.ListQueues(queue, new Result<IEnumerable<string>>()).Wait().Any();
        //    }, 30.Seconds()), "queue did not get deleted"
        //    );
        //}

        //[Test, Ignore]
        //public void LIVE_List_test_queues() {
        //    var client = CreateLiveClient();
        //    foreach(var q in client.ListQueues("test-", new Result<IEnumerable<string>>()).Wait().ToArray()) {
        //        _log.DebugFormat("queue: {0}", q);
        //    }
        //}

        //[Test, Ignore]
        //public void LIVE_List_all_queues() {
        //    var client = CreateLiveClient();
        //    foreach(var q in client.ListQueues(null, new Result<IEnumerable<string>>()).Wait().ToArray()) {
        //        _log.DebugFormat("queue: {0}", q);
        //    }
        //}

        //[Test, Ignore]
        //public void LIVE_Delete_all_test_queues() {
        //    var client = CreateLiveClient();
        //    foreach(var q in client.ListQueues("test-", new Result<IEnumerable<string>>()).Wait().ToArray()) {
        //        _log.DebugFormat("queue: {0}", q);
        //        client.DeleteQueue(q, new Result<AwsSqsResponse>()).Wait();
        //    }
        //}

        //[Test, Ignore]
        //public void LIVE_Delete_some_queues() {
        //    var client = CreateLiveClient();
        //    var prefix = "test-";
        //    if(!string.IsNullOrEmpty(prefix)) {
        //        foreach(var q in client.ListQueues(prefix, new Result<IEnumerable<string>>()).Wait().ToArray()) {
        //            _log.DebugFormat("deleting queue: {0}", q);
        //            client.DeleteQueue(q, new Result<AwsSqsResponse>()).Wait();
        //        }
        //    }
        //}

        private AmazonSQS CreateLiveClient() {
            var config = GetConfig();
            return new AmazonSQSClient(config.PublicKey, config.PrivateKey);
        }

        private AwsSqsClientConfig GetConfig() {
            return new AwsSqsClientConfig {
                PublicKey = ConfigurationManager.AppSettings["aws.sqs.publickey"],
                PrivateKey = ConfigurationManager.AppSettings["aws.sqs.privatekey"],
                AccountId = ConfigurationManager.AppSettings["aws.sqs.accountid"]
            };
        }
    }

    public class Consumer2 {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static int _id = 0;

        //--- Fields ---
        public readonly int Id = _id++;
        private readonly string _queue;
        private readonly AmazonSQS _client;
        private readonly List<string> _received = new List<string>();
        private bool _stopped;

        public Consumer2(string queue, AmazonSQS client) {
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
            Async.Fork(Receive);
        }

        private void Receive() {
            if(_stopped) {
                return;
            }
            try {
                var r = _client.ReceiveMessage(new ReceiveMessageRequest { QueueUrl = _queue, MaxNumberOfMessages = 10, VisibilityTimeout = 60 });
                if(_stopped) {
                    return;
                }
                var received = r.ReceiveMessageResult.Message;
                if(!received.Any()) {
                    _log.DebugFormat("{0}: no messages in queue, sleeping before retry", Id);
                    Async.Sleep(1.Seconds()).WhenDone(r2 => Receive());
                    return;
                }
                _log.DebugFormat("{0}: received {1} messages", Id, received.Count);
                _client.DeleteMessageBatch(new DeleteMessageBatchRequest {
                    QueueUrl = _queue, 
                    Entries = received.Select(x =>
                        new DeleteMessageBatchRequestEntry { ReceiptHandle = x.ReceiptHandle, Id = x.MessageId }
                    ).ToList()
                });
                if(_stopped) {
                    return;
                }
                lock(_received) {
                    _received.AddRange(received.Select(x => x.Body));
                }
            } catch(Exception e) {
                Exception = e;
                return;
            }
            Receive();
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

    public class Producer2 {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        private readonly string _queue;
        private readonly AwsSqsClientConfig _clientConfig;
        private readonly Random _random = new Random();
        public Producer2(string queue, AwsSqsClientConfig clientConfig) {
            _queue = queue;
            _clientConfig = clientConfig;
        }

        public Result<Production> Produce(int messages) {
            var production = new Production();
            var final = new Result<Production>();
            Async.Fork(() => {
                try {
                    _log.DebugFormat("{0}: Producing {1} messages", production.Id, messages);
                    var client = new AmazonSQSClient(_clientConfig.PublicKey, _clientConfig.PrivateKey);
                    for(var i = 0; i < messages; i++) {
                        var msg = production.Id + ":" + messages;
                        client.SendMessage(new SendMessageRequest { QueueUrl = _queue, MessageBody = msg });
                        production.Sent.Add(msg);
                    }
                    production.Stopwatch.Stop();
                    _log.DebugFormat("{0}: Sent {1} messages in {2:0.00}s @ {3:0.00}msg/sec",
                        production.Id,
                        production.Sent.Count,
                        production.Stopwatch.Elapsed.TotalSeconds,
                        production.Sent.Count / production.Stopwatch.Elapsed.TotalSeconds
                    );
                    final.Return(production);
                } catch(Exception e) {
                    final.Throw(e);
                }
            });
            return final;
        }

    }
}
