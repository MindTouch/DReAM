/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2010 MindTouch, Inc.
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
    public class AwsSqsMessage {

        //--- Class Methods ---
        public static AwsSqsMessage FromBodyDocument(XDoc body) {
            return new AwsSqsMessage { Body = body.ToCompactString() };
        }

        internal static IEnumerable<AwsSqsMessage> FromSqsResponse(DreamMessage response) {
            var messages = new List<AwsSqsMessage>();
            foreach(var msgDoc in response.ToDocument()["ReceiveMessageResult/Message"]) {
                var msg = new AwsSqsMessage {
                    Id = msgDoc["MessageId"].AsText,
                    ReceiptHandle = msgDoc["ReceiptHandle"].AsText,
                    MD5OfBody = msgDoc["MD5OfBody"].AsText,
                    Body = msgDoc["Body"].AsText
                };
                foreach(var attr in msgDoc["Attribute"]) {
                    msg.Attibutes[attr["Name"].AsText] = attr["Value"].AsText;
                }
                messages.Add(msg);
            }
            return messages;
        }

        //--- Fields ---
        private readonly IDictionary<string, string> _attributes = new Dictionary<string, string>();

        //--- Properties ---
        public string Id { get; protected set; }
        public string ReceiptHandle { get; protected set; }
        public IDictionary<string, string> Attibutes { get { return _attributes; } }
        public string Body { get; protected set; }
        public string MD5OfBody { get; protected set; }

        //--- Methods ---
        public XDoc BodyToDocument() {
            return XDocFactory.From(Body, MimeType.TEXT_XML);
        }
    }
}