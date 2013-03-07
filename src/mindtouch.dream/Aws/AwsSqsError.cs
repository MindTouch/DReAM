/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2013 MindTouch, Inc.
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
using MindTouch.Xml;

namespace MindTouch.Aws {
    public class AwsSqsError : AwsSqsResponse {

        //--- Constructors ---
        public AwsSqsError(XDoc doc) {
            RequestId = doc["sqs:ResponseMetadata/sqs:RequestId"].AsText;
            Type = doc["sqs:Error/sqs:Type"].AsText;
            Code = doc["sqs:Error/sqs:Code"].AsText;
            Message = doc["sqs:Error/sqs:Message"].AsText;
            Detail = doc["sqs:Error/sqs:Detail"].AsText;
        }

        public AwsSqsError(string type, string code, string message, string detail) {
            Type = type;
            Code = code;
            Message = message;
            Detail = detail;
        }

        //--- Properties ---
        public string Type { get; protected set; }
        public string Code { get; protected set; }
        public string Message { get; protected set; }
        public string Detail { get; protected set; }
    }
}