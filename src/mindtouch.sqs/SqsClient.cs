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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using MindTouch.Dream;

namespace MindTouch.Sqs {
    public class SqsClient : ISqsClient {

        //--- Fields ---
        private readonly AmazonSQSClient _client;
        private readonly SqsClientConfig _config;
        private const string RECEIVING_MESSAGE = "receiving message";
        private const string DELETING_MESSAGE = "deleting message";
        private const string SENDING_MESSAGE = "sending message";
        private const string SENDING_BATCH_MESSAGES = "sending batch messages";
        private const string CREATING_QUEUE = "creating queue";
        private const string DELETING_QUEUE = "deleting queue";
        private const string BATCH_DELETING_MESSAGES = "batch deleting messages";

        //--- Constructors ---
        public SqsClient(SqsClientConfig sqsClientConfig) {
            if(sqsClientConfig == null) {
                throw new ArgumentNullException("sqsClientConfig");
            }
            _config = sqsClientConfig;            
            if(!string.IsNullOrEmpty(_config.PublicKey) && !string.IsNullOrEmpty(_config.PrivateKey)) {
                _client = new AmazonSQSClient(
                    new BasicAWSCredentials(_config.PublicKey, _config.PrivateKey),
                    new AmazonSQSConfig { ServiceURL = _config.Endpoint.ToString() });
            } else {
                _client = new AmazonSQSClient(new AmazonSQSConfig { ServiceURL = _config.Endpoint.ToString() });
            }
        }

        //--- Methods ---
        public IEnumerable<SqsMessage> ReceiveMessages(SqsQueueName queueName, TimeSpan waitTimeSeconds, uint maxNumberOfMessages) {

            // Check preconditions
            if(waitTimeSeconds.TotalSeconds > SqsUtils.MAX_LONG_POLL_WAIT_TIME.TotalSeconds) {
                throw new ArgumentException(string.Format("The argument waitTimeSeconds is larger than '{0}', which is the maximum value allowed", SqsUtils.MAX_LONG_POLL_WAIT_TIME.TotalSeconds));
            }
            if(maxNumberOfMessages > SqsUtils.MAX_NUMBER_OF_MESSAGES_TO_FETCH) {
                throw new ArgumentException(string.Format("The argument maxNumberOfMessages is larger than '{0}', which is the maximum value allowed", SqsUtils.MAX_NUMBER_OF_MESSAGES_TO_FETCH));
            }

            // Perform request            
            ReceiveMessageResponse response = Invoke(() =>
                _client.ReceiveMessage(new ReceiveMessageRequest {
                    QueueUrl = GetQueueUrl(queueName.Value),
                    WaitTimeSeconds = (int)waitTimeSeconds.TotalSeconds,
                    MaxNumberOfMessages = (int)maxNumberOfMessages
                }),
                queueName,
                RECEIVING_MESSAGE
            );
            AssertSuccessfulStatusCode(response.HttpStatusCode, queueName, RECEIVING_MESSAGE);
            return response.Messages.Select(msg => new SqsMessage(new SqsMessageId(msg.MessageId), new SqsMessageReceipt(msg.ReceiptHandle), msg.Body)).ToArray();
        }

        public bool DeleteMessage(SqsQueueName queueName, SqsMessageReceipt messageReceipt) {
            DeleteMessageResponse response = Invoke(() =>
                _client.DeleteMessage(new DeleteMessageRequest {
                    QueueUrl = GetQueueUrl(queueName.Value),
                    ReceiptHandle = messageReceipt.Value
                }),
                queueName,
                DELETING_MESSAGE
            );
            AssertSuccessfulStatusCode(response.HttpStatusCode, queueName, DELETING_MESSAGE);
            return response.HttpStatusCode == HttpStatusCode.OK;
        }

        public IEnumerable<SqsMessageId> DeleteMessages(SqsQueueName queueName, IEnumerable<SqsMessage> messages) {
            if(messages.Count() > SqsUtils.MAX_NUMBER_OF_BATCH_DELETE_MESSAGES) {
                throw new ArgumentException(string.Format("messageReceipts is larger than {0}, which is the maximum", SqsUtils.MAX_NUMBER_OF_BATCH_DELETE_MESSAGES));
            }
            var deleteEntries = messages.Select(message => new DeleteMessageBatchRequestEntry { Id = message.MessageId.Value, ReceiptHandle = message.MessageReceipt.Value }).ToList();
            DeleteMessageBatchResponse response = Invoke(() =>
                _client.DeleteMessageBatch(new DeleteMessageBatchRequest {
                    QueueUrl = GetQueueUrl(queueName.Value),
                    Entries = deleteEntries
                }),
                queueName,
                BATCH_DELETING_MESSAGES);
            AssertSuccessfulStatusCode(response.HttpStatusCode, queueName, BATCH_DELETING_MESSAGES);
            return response.Failed.Select(failed => new SqsMessageId(failed.Id)).ToArray();
        }

        public void SendMessage(SqsQueueName queueName, string messageBody) {
            SendMessage(queueName, messageBody, TimeSpan.Zero);
        }

        public void SendMessage(SqsQueueName queueName, string messageBody, TimeSpan delay) {
            SendMessageResponse response = Invoke(() =>
                _client.SendMessage(new SendMessageRequest {
                    QueueUrl = GetQueueUrl(queueName.Value),
                    MessageBody = messageBody,
                    DelaySeconds = (int)delay.TotalSeconds
                }),
                queueName,
                SENDING_MESSAGE);
            AssertSuccessfulStatusCode(response.HttpStatusCode, queueName, SENDING_MESSAGE);
        }

        public IEnumerable<string> SendMessages(SqsQueueName queueName, IEnumerable<string> messageBodies) {
            if(messageBodies.Count() > SqsUtils.MAX_NUMBER_OF_BATCH_SEND_MESSAGES) {
                throw new ArgumentException(string.Format("messageBodies is larger than {0}, which is the maximum", SqsUtils.MAX_NUMBER_OF_BATCH_SEND_MESSAGES));
            }
            var msgId = 1;
            var sendEntries = (from messageBody in messageBodies select new SendMessageBatchRequestEntry { MessageBody = messageBody, Id = string.Format("msg-{0}", msgId++)}).ToList();
            SendMessageBatchResponse response = Invoke(() =>
                _client.SendMessageBatch(new SendMessageBatchRequest {
                    QueueUrl = GetQueueUrl(queueName.Value),
                    Entries = sendEntries
                }),
                queueName,
                SENDING_BATCH_MESSAGES);
            AssertSuccessfulStatusCode(response.HttpStatusCode, queueName, SENDING_BATCH_MESSAGES);
            return response.Failed.Select(failed => failed.Message).ToArray();
        }

        public XUri CreateQueue(SqsQueueName queueName) {
            CreateQueueResponse response = Invoke(() =>
                _client.CreateQueue(new CreateQueueRequest {QueueName = queueName.Value}),
                queueName,
                CREATING_QUEUE);
            AssertSuccessfulStatusCode(response.HttpStatusCode, queueName, CREATING_QUEUE);
            return new XUri(response.QueueUrl);
        }

        public bool DeleteQueue(SqsQueueName queueName) {
            DeleteQueueResponse response = Invoke(() =>
                _client.DeleteQueue(new DeleteQueueRequest {QueueUrl = GetQueueUrl(queueName.Value)}),
                queueName,
                DELETING_QUEUE);
            AssertSuccessfulStatusCode(response.HttpStatusCode, queueName, DELETING_QUEUE);
            return true;
        }

        private string GetQueueUrl(string queueName) {
            return _config.Endpoint.At(_config.AccountId, queueName).ToString();
        }

        private void AssertSuccessfulStatusCode(HttpStatusCode statusCode, SqsQueueName queueName, string sqsOperation) {
            if(statusCode != HttpStatusCode.OK) {
                throw new SqsException(
                    string.Format("Got a '{0}' response while '{1}' from SQS queue '{2}'", statusCode, sqsOperation, queueName.Value),
                    null);
            }
        }

        private static T Invoke<T>(Func<T> function, SqsQueueName queueName, string sqsOperation) {
            try {
                return function();
            } catch(AmazonSQSException e) {
                throw new SqsException(string.Format("There was an SQS error '{0}', from the '{1}' SQS queue",  sqsOperation, queueName.Value), e);
            } catch(Exception e) {
                throw new SqsException(string.Format("There was an error '{0}', from the '{1}' SQS queue", sqsOperation, queueName.Value), e);
            }
        }
    }
}