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

using System;

namespace MindTouch.Dream.Test {

    /// <summary>
    /// Information about a <see cref="DreamHost"/> created by <see cref="DreamTestHelper.CreateRandomPortHost(MindTouch.Xml.XDoc, Autofac.IContainer, int)"/>
    /// </summary>
    public class DreamHostInfo : IDisposable {

        /// <summary>
        /// The network accessible local uri for the <see cref="DreamHost"/>.
        /// </summary>
        public readonly Plug LocalHost;

        /// <summary>
        /// The <see cref="DreamHost"/> instance.
        /// </summary>
        public readonly DreamHost Host;

        /// <summary>
        /// The apikey for accessing internal and private features of the <see cref="DreamHost"/>.
        /// </summary>
        public readonly string ApiKey;

        internal DreamHostInfo(Plug localhostUri, DreamHost host, string apiKey) {
            LocalHost = localhostUri;
            Host = host;
            ApiKey = apiKey;
        }

        /// <summary>
        /// Dipose the host.
        /// </summary>
        public void Dispose() {
            Host.Dispose();
        }
    }
}
