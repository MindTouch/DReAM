﻿/*
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
using System.Globalization;
using System.Linq;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Aws {
    public class AwsSqsClient : IAwsSqsClient {

        //--- Fields ---
        private readonly AwsSqsClientConfig _config;

        //--- Constructors ---
        public AwsSqsClient(AwsSqsClientConfig config) {
            _config = config;
        }

        //--- Methods ---
        public Result<AwsSqsSendResponse> Send(string queue, AwsSqsMessage message, Result<AwsSqsSendResponse> result) {
            var parameters = new Dictionary<string, string> {
                {"MessageBody", message.Body}
            };
            return HandleResponse(true, queue, "SendMessage", parameters, result,
                m => new AwsSqsSendResponse(m)
            );
        }


        public Result<IEnumerable<AwsSqsMessage>> Receive(string queue, int maxMessages, TimeSpan visibilityTimeout, Result<IEnumerable<AwsSqsMessage>> result) {
            // CLEANUP: punting on attributes right now
            var parameters = new Dictionary<string, string>();
            if(maxMessages != AwsSqsDefaults.DEFAULT_MESSAGES) {
                parameters.Add("MaxNumberOfMessages", maxMessages.ToString());
            }
            if(visibilityTimeout != AwsSqsDefaults.DEFAULT_VISIBILITY) {
                parameters.Add("VisibilityTimeout", Math.Floor(visibilityTimeout.TotalSeconds).ToString());
            }
            return HandleResponse(false, queue, "ReceiveMessage", parameters, result,
                m => AwsSqsMessage.FromSqsResponse(queue, m)
            );
        }

        public Result<AwsSqsResponse> Delete(AwsSqsMessage message, Result<AwsSqsResponse> result) {
            var parameters = new Dictionary<string, string> {
                {"ReceiptHandle", message.ReceiptHandle},
            };
            return HandleResponse(true, message.OriginQueue, "DeleteMessage", parameters, result,
                m => new AwsSqsResponse(m)
            );
        }

        public Result<AwsSqsResponse> CreateQueue(string queue, TimeSpan defaultVisibilityTimeout, Result<AwsSqsResponse> result) {
            var parameters = new Dictionary<string, string> {
                {"QueueName", queue},
            };
            if(defaultVisibilityTimeout != AwsSqsDefaults.DEFAULT_VISIBILITY) {
                parameters.Add("DefaultVisibilityTimeout", Math.Floor(defaultVisibilityTimeout.TotalSeconds).ToString());
            }
            return HandleResponse(true, null, "CreateQueue", parameters, result,
                m => new AwsSqsResponse(m)
            );
        }

        public Result<AwsSqsResponse> DeleteQueue(string queue, Result<AwsSqsResponse> result) {
            return HandleResponse(true, queue, "DeleteQueue", new Dictionary<string, string>(), result,
                m => new AwsSqsResponse(m)
            );
        }

        public Result<IEnumerable<string>> ListQueues(string prefix, Result<IEnumerable<string>> result) {
            var parameters = new Dictionary<string, string>();
            if(!string.IsNullOrEmpty(prefix)) {
                parameters.Add("QueueNamePrefix", prefix);
            }
            return HandleResponse(false, null, "ListQueues", parameters, result,
                m => m["sqs:ListQueuesResult/sqs:QueueUrl"].Select(x => x.AsUri.LastSegment).ToArray()
            );
        }

        protected Result<T> HandleResponse<T>(bool post, string queue, string action, Dictionary<string, string> parameters, Result<T> result, Func<XDoc, T> responseHandler) {
            var time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            parameters.Add("AWSAccessKeyId", _config.PublicKey);
            parameters.Add("Action", action);
            parameters.Add("SignatureMethod", AwsExtensions.HASH_METHOD);
            parameters.Add("SignatureVersion", "2");
            parameters.Add("Version", "2009-02-01");
            parameters.Add(_config.UseExpires ? "Expires" : "Timestamp", time);

            // Note (arnec): We have to build the querystring by hand because Aws expects very specific encoding for the signature to work
            var parameterPairs = parameters
                .OrderBy(param => param.Key, StringComparer.Ordinal)
                .Select(param => CreateKeyPair(param));
            var query = string.Join("&", parameterPairs.ToArray());
            var p = Plug.New(_config.Endpoint.SqsUri).WithQuery(query);
            if(queue != null) {
                p = p.At(_config.AccountId, queue);
            }
            var request = string.Format("{0}\n{1}\n{2}\n{3}", post ? "POST" : "GET", p.Uri.Host, p.Uri.Path, query);
            var signature = request.GetSignature(_config.PrivateKey);
            p = p.With("Signature", signature);
            (post ? p.PostAsForm(new Result<DreamMessage>()) : p.Get(new Result<DreamMessage>())).WhenDone(
                response => {
                    if(response.HasException) {
                        result.Throw(response.Exception);
                        return;
                    }
                    if(!response.Value.IsSuccessful) {
                        try {
                            XDoc doc = null;
                            if(response.Value.HasDocument) {
                                doc = response.Value.ToDocument();
                            } else {
                                var content = response.Value.ToText();
                                if(!string.IsNullOrEmpty(content)) {
                                    doc = XDocFactory.From(content, MimeType.TEXT_XML);
                                }
                            }
                            if(doc != null && doc.Name.EqualsInvariant("ErrorResponse")) {
                                result.Throw(new AwsSqsRequestException(new AwsSqsError(doc.UsePrefix("sqs", "http://queue.amazonaws.com/doc/2009-02-01/")), response.Value));
                                return;
                            }
                        } catch { }
                        result.Throw(new AwsSqsRequestException("Server responded with unexpected error", response.Value));
                        return;
                    }
                    try {
                        result.Return(responseHandler(response.Value.ToDocument().UsePrefix("sqs", "http://queue.amazonaws.com/doc/2009-02-01/")));
                    } catch(Exception) {
                        result.Throw(new AwsSqsRequestException("Response document could not be parsed", response.Value));
                    }

                });
            return result;
        }

        private string CreateKeyPair(KeyValuePair<string, string> param) {

            // Note (arnec): For proper signature generation, spaces must be in %20 format (where spaces are + after XUri.Encode)
            // Note (andyv): Amazon requires * to be encoded, but not ~ as per RFC 1738
            return param.Key + "=" + XUri.Encode(param.Value)
                .ReplaceAll("*", "%2A")
                .ReplaceAll("+", "%20")
                .ReplaceAll("%7E", "~");
        }
    }
}