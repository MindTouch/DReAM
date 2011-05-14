/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream.Test {

    /// <summary>
    /// Provides information about a service created via <see cref="DreamTestHelper.CreateService(MindTouch.Dream.Test.DreamHostInfo,System.Type,string)"/> or one of its overrides.
    /// </summary>
    /// <remarks>
    /// Services created and wrapped with <see cref="DreamServiceInfo"/> are meant for testing purposes only.
    /// </remarks>
    public class DreamServiceInfo {

        //--- Fields ---

        /// <summary>
        /// Public address of the service.
        /// </summary>
        public readonly Plug AtLocalHost;
        private readonly XDoc _internalSetCookie;
        private readonly XDoc _privateSetCookie;

        //--- Constructors ---
        internal DreamServiceInfo(DreamHostInfo hostInfo, string path, XDoc serviceResponse) {
            _internalSetCookie = serviceResponse["internal-key/set-cookie"];
            _privateSetCookie = serviceResponse["private-key/set-cookie"];
            AtLocalHost = Plug.New(hostInfo.LocalHost.At(path));
        }

        private DreamServiceInfo(DreamServiceInfo info, DreamCookieJar cookies) {
            _internalSetCookie = info._internalSetCookie;
            _privateSetCookie = info._privateSetCookie;
            AtLocalHost = info.AtLocalHost.WithCookieJar(cookies);
        }

        //--- Methods ---

        /// <summary>
        /// Get a new instance of the service info initialized with the service's internal key.
        /// </summary>
        /// <returns>A new instance.</returns>
        public DreamServiceInfo WithInternalKey() {
            return new DreamServiceInfo(this, GetJar(_internalSetCookie));
        }

        /// <summary>
        /// Get a new instance of the service info initialized with the service's private key.
        /// </summary>
        /// <returns>A new instance.</returns>
        public DreamServiceInfo WithPrivateKey() {
            return new DreamServiceInfo(this, GetJar(_privateSetCookie));
        }

        /// <summary>
        /// Get a new instance of the service info initialized without any the service keys.
        /// </summary>
        /// <returns>A new instance.</returns>
        public DreamServiceInfo WithoutKeys() {
            return new DreamServiceInfo(this, null);
        }

        private DreamCookieJar GetJar(XDoc setCookieElement) {
            DreamCookieJar jar = new DreamCookieJar();
            List<DreamCookie> collection = new List<DreamCookie>();
            collection.Add(DreamCookie.ParseSetCookie(setCookieElement));
            jar.Update(collection, null);
            return jar;
        }
    }

}
