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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream {

    /// <summary>
    /// Provides common constants, properties, extension methods and static helpers for working with the Dream framework.
    /// </summary>
    public static class DreamUtil {

        //--- Constants ---

        /// <summary>
        /// Default timeout (30 seconds).
        /// </summary>
        public const int TIMEOUT_DEFAULT = 30 * 1000;

        /// <summary>
        /// Long timeout (30 minutes).
        /// </summary>
        public const int TIMEOUT_LONG = 30 * 60 * 1000;

        /// <summary>
        /// Short timeout (10 seconds).
        /// </summary>
        public const int TIMEOUT_SHORT = 10 * 1000;

        /// <summary>
        /// Default Packetsize (1024 bytes).
        /// </summary>
        public const int DEFAULT_PACKETSIZE = 1024;

        private const int BUFFER_SIZE = 32768;

        //--- Class Fields ---
        private static string _version;

        //--- Class Properties ---

        /// <summary>
        /// Dream Version.
        /// </summary>
        public static string DreamVersion {
            get {
                if(_version == null) {
                    _version = Assembly.GetAssembly(typeof(DreamUtil)).GetName().Version.ToString();
                }
                return _version;
            }
        }

        //--- Methods ---

        /// <summary>
        /// Prepare an incoming message with host environment information.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="transport"></param>
        /// <param name="requestClientIp"></param>
        /// <param name="userAgent"></param>
        public static void PrepareIncomingMessage(DreamMessage message, Encoding contentEncoding, string transport, string requestClientIp, string userAgent) {
            if(message == null) {
                throw new ArgumentNullException("message");
            }

            // set content-encoding
            if(contentEncoding != null) {
                message.Headers.ContentEncoding = contentEncoding.WebName;
            }

            // set dream-transport information (i.e. point of entry)
            message.Headers.DreamTransport = transport;

            // set the client IP header
            List<string> clientIps = new List<string>();
            if(!string.IsNullOrEmpty(requestClientIp)) {
                clientIps.Add(requestClientIp.Split(':')[0]);
            }
            string[] forwardeFor = message.Headers.ForwardedFor;
            Array.Reverse(forwardeFor);
            clientIps.AddRange(forwardeFor);
            message.Headers.DreamClientIP = clientIps.ToArray();

            // set host header
            if(string.IsNullOrEmpty(message.Headers.Host)) {
                message.Headers.Host = new XUri(transport).HostPort;
            }

            // set user agent
            message.Headers.UserAgent = userAgent;
        }

        /// <summary>
        /// Append Dream specific headers from a source message to an internally forwarded one.
        /// </summary>
        /// <param name="original">Source messag.</param>
        /// <param name="forwarded">Message to be forwarded.</param>
        /// <returns>Message to be forwarded instance.</returns>
        public static DreamMessage AppendHeadersToInternallyForwardedMessage(DreamMessage original, DreamMessage forwarded) {

            // pass along host and public-uri information
            forwarded.Headers.DreamTransport = original.Headers.DreamTransport;
            forwarded.Headers.DreamPublicUri = original.Headers.DreamPublicUri;
            forwarded.Headers.DreamUserHost = original.Headers.DreamUserHost;
            forwarded.Headers.DreamOrigin = original.Headers.DreamOrigin;
            forwarded.Headers.DreamClientIP = original.Headers.DreamClientIP;
            return forwarded;
        }
    }
}
