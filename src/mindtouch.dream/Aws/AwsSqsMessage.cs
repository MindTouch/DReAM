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
using System.Collections.Generic;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Aws {
    public class AwsSqsMessage : AwsSqsResponse {

        //--- Class Methods ---
        public static AwsSqsMessage FromBodyDocument(XDoc body) {
            return new AwsSqsMessage { Body = body.ToCompactString() };
        }

        public static AwsSqsMessage FromBody(string body) {
            return new AwsSqsMessage { Body = body };
        }

        internal static IEnumerable<AwsSqsMessage> FromSqsResponse(string queue, XDoc doc) {
            var messages = new List<AwsSqsMessage>();
            var requestId = doc["sqs:ResponseMetadata/sqs:RequestId"].AsText;
            foreach(var msgDoc in doc["sqs:ReceiveMessageResult/sqs:Message"]) {
                var msg = new AwsSqsMessage {
                    MessageId = msgDoc["sqs:MessageId"].AsText,
                    OriginQueue = queue,
                    ReceiptHandle = msgDoc["sqs:ReceiptHandle"].AsText,
                    MD5OfBody = msgDoc["sqs:MD5OfBody"].AsText,
                    Body = msgDoc["sqs:Body"].AsText,
                    RequestId = requestId
                };
                foreach(var attr in msgDoc["sqs:Attribute"]) {
                    msg.Attributes[attr["Name"].AsText] = attr["Value"].AsText;
                }
                messages.Add(msg);
            }
            return messages;
        }

        //--- Fields ---
        protected readonly IDictionary<string, string> _attributes = new Dictionary<string, string>();

        //--- Properties ---
        public string MessageId { get; protected set; }
        public string OriginQueue { get; protected set; }
        public string ReceiptHandle { get; protected set; }
        public IDictionary<string, string> Attributes { get { return _attributes; } }
        public string Body { get; protected set; }
        public string MD5OfBody { get; protected set; }

        //--- Methods ---
        public XDoc BodyToDocument() {
            return XDocFactory.From(Body, MimeType.TEXT_XML);
        }
    }
}