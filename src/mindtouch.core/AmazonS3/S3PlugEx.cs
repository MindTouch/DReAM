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
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MindTouch.Dream.AmazonS3 {

    /// <summary>
    /// Amazon S3 related extension methods for <see cref="Plug"/>
    /// </summary>
    /// <remarks>This class is deprecated and has been replaced with MindTouch.Aws.AwsExtensions. It will be removed in a future version.</remarks>
    [Obsolete("This class has been replaced with MindTouch.Aws.AwsExtensions and will be removed in a future version")]
    public static class S3PlugEx {

        //--- Constants ---
        private const string AWS_DATE = "X-Amz-Date";

        //--- Extension Methods ---

        /// <summary>
        /// Add a Plug Pre-Handler to attach the appropriate auth header.
        /// </summary>
        /// <param name="plug">Plug instance to base operation on.</param>
        /// <param name="privateKey">Amazon S3 private key.</param>
        /// <param name="publicKey">Amazon S3 public key.</param>
        /// <returns>New Plug instance with pre-handler.</returns>
        public static Plug WithS3Authentication(this Plug plug, string privateKey, string publicKey) {
            return plug.WithPreHandler((verb, uri, normalizedUri, message) => {
                
                // add amazon date header
                var date = DateTime.UtcNow.ToString("r");
                message.Headers[AWS_DATE] = date;

                // add authorization header
                var authString = new StringBuilder()
                    .Append(verb)
                    .Append("\n")
                    .Append(message.Headers[DreamHeaders.CONTENT_MD5])
                    .Append("\n")
                    .Append(message.ContentType.ToString())
                    .Append("\n")
                    .Append("\n");
                foreach(var header in message.Headers.OrderBy(x => x.Key.ToLowerInvariant(), StringComparer.Ordinal)) {
                    if(!header.Key.StartsWithInvariantIgnoreCase("x-amz-")) {
                        continue;
                    }
                    authString.AppendFormat("{0}:{1}\n", header.Key.ToLowerInvariant(), header.Value);
                }
                authString.Append(normalizedUri.Path);
                var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(privateKey));
                var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(authString.ToString())));
                message.Headers.Authorization = string.Format("AWS {0}:{1}", publicKey, signature);
                message.Headers.ContentType = message.ContentType;
                return message;
            });
        }
    }
}