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

using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream.Services.PubSub {

    /// <summary>
    /// Configuration class that may be injected into <see cref="IPubSubDispatcher"/> instances if a matching constructor is found.
    /// </summary>
    public class DispatcherConfig {

        //--- Fields ---

        /// <summary>
        /// Uri of the hosting service.
        /// </summary>
        public XUri ServiceUri;

        /// <summary>
        /// Accesss cookie of the hosting service.
        /// </summary>
        public DreamCookie ServiceAccessCookie;

        /// <summary>
        /// Cookies the service has provided by other services.
        /// </summary>
        public DreamCookieJar ServiceCookies;

        /// <summary>
        /// Service configuration.
        /// </summary>
        public XDoc ServiceConfig;
    }
}
