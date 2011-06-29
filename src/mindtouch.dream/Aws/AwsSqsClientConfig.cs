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
using MindTouch.Xml;

namespace MindTouch.Aws {
    public class AwsSqsClientConfig {
        
        //--- Constructors ---
        public AwsSqsClientConfig(XDoc config) {
            PrivateKey = config["privatekey"].AsText;
            PublicKey = config["publickey"].AsText;
            AccountId = config["accountid"].AsText;
            Endpoint = AwsEndpoint.GetEndpoint(config["endpoint"].AsText ?? "default");
            UseExpires = config["use-expires"].AsBool ?? false;
        }

        public AwsSqsClientConfig() {
            Endpoint = AwsEndpoint.Default;
            UseExpires = false;
        }

        //--- Properties ---

        public string AccountId { get; set; }
        public AwsEndpoint Endpoint { get; set; }
        /// <summary>
        /// Private Key.
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// Public Key.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Use Expires parameter instead of Timestamp
        /// </summary>
        public bool UseExpires { get; set; }
    }
}