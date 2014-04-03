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
using MindTouch.Xml;

namespace MindTouch.Dream {

    /// <summary>
    /// <see cref="DreamApplication"/> configuration model. Built via <see cref="DreamApplicationConfigurationBuilder"/>.
    /// </summary>
    internal class DreamApplicationConfiguration {

        //--- Fields ---

        /// <summary>
        /// <see cref="DreamHostService"/> configuration document.
        /// </summary>
        public XDoc HostConfig;

        /// <summary>
        /// Host apikey.
        /// </summary>
        public string Apikey;

        /// <summary>
        /// Location of dynamically loaded service assemblies.
        /// </summary>
        public string ServicesDirectory;

        /// <summary>
        /// Host Uri prefix.
        /// </summary>
        public string Prefix;

        /// <summary>
        /// Host script.
        /// </summary>
        internal XDoc Script;

        /// <summary>
        /// Expected Authtoken for whitelist behavior for dream.in.* parameters.
        /// </summary>
        public string DreamInParamAuthToken;
    }
}