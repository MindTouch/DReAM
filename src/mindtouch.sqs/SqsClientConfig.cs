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
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Sqs {

    /// <summary>
    /// Class for configuring the SQS client.
    /// </summary>
    public class SqsClientConfig {

        //--- Class Methods ---

        /// <summary>
        /// Initialize SqsClientConfig instance from an XDoc instance.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static SqsClientConfig From(XDoc config) {
            return new SqsClientConfig(
                privateKey: config["privatekey"].AsText, 
                publicKey: config["publickey"].AsText, 
                accountId: config["accountid"].AsText, 
                endpoint: config["endpoint"].AsText ?? "default", 
                secure: config["secure"].AsText == "true"
            );
        }
        
        //--- Fields ---

        /// <summary>
        /// Private AWS key.
        /// </summary>
        public readonly string PrivateKey;

        /// <summary>
        /// Public AWS Key.
        /// </summary>
        public readonly string PublicKey;

        /// <summary>
        /// AWS account ID.
        /// </summary>
        public readonly string AccountId;

        /// <summary>
        /// Region-specific endpoint.
        /// </summary>
        public readonly XUri Endpoint;

        //--- Constructors ---

        /// <summary>
        /// Constructor for creating an instance.
        /// </summary>
        /// <param name="privateKey">Private AWS key.</param>
        /// <param name="publicKey">Public AWS key.</param>
        /// <param name="accountId">AWS account ID.</param>
        /// <param name="endpoint">Region name or custom SQS endpoint to use.</param>
        /// <param name="secure">Boolean indicating if HTTPS should be used.</param>
        public SqsClientConfig(string privateKey, string publicKey, string accountId, string endpoint, bool secure) {
            if(string.IsNullOrEmpty(accountId)) {
                throw new ArgumentNullException("accountId");
            }
            if(string.IsNullOrEmpty(endpoint)) {
                throw new ArgumentNullException("endpoint");
            }
            this.PrivateKey = privateKey;
            this.PublicKey = publicKey;
            this.AccountId = accountId;

            // read end-point settings
            XUri uri;
            switch(endpoint) {
            case "default":
            case "us-east-1":

                // US East (N. Virginia)
                uri = new XUri("http://sqs.us-east-1.amazonaws.com");
                break;
            case "us-west-2":

                // US West (Oregon)
                uri = new XUri("http://sqs.us-west-2.amazonaws.com");
                break;
            case "us-west-1":

                // US West (N. California)
                uri = new XUri("http://sqs.us-west-1.amazonaws.com");
                break;
            case "eu-west-1":

                // EU (Ireland)
                uri = new XUri("http://sqs.eu-west-1.amazonaws.com");
                break;
            case "eu-central-1":

                // EU (Frankfurt)
                uri = new XUri("http://sqs.eu-central-1.amazonaws.com");
                break;
            case "ap-southeast-1":

                // Asia Pacific (Singapore)
                uri = new XUri("http://sqs.ap-southeast-1.amazonaws.com");
                break;
            case "ap-southeast-2":

                // Asia Pacific (Sydney)
                uri = new XUri("http://sqs.ap-southeast-2.amazonaws.com");
                break;
            case "ap-northeast-1":

                // Asia Pacific (Tokyo)
                uri = new XUri("http://sqs.ap-northeast-1.amazonaws.com");
                break;
            case "sa-east-1":

                // South America (Sao Paulo)
                uri = new XUri("http://sqs.sa-east-1.amazonaws.com");
                break;
            default:
                uri = XUri.TryParse(endpoint);
                break;
            }
            if(uri == null) {
                throw new ArgumentException("invalid value for SQS end-point", "endpoint");
            }
            if(secure) {
                uri = uri.WithScheme("https");
            }
            this.Endpoint = uri;
        }
    }
}
