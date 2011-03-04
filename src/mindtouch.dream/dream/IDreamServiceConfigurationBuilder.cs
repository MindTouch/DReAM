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

namespace MindTouch.Dream {
    /// <summary>
    /// Fluent interface for confirguring a <see cref="IDreamService"/> registration.
    /// </summary>
    public interface IDreamServiceConfigurationBuilder {

        //--- Methods ---

        /// <summary>
        /// The path at which the service is to be instantiated.
        /// </summary>
        /// <param name="path">Full path to service.</param>
        /// <returns>This instance.</returns>
        IDreamServiceConfigurationBuilder AtPath(string path);

        /// <summary>
        /// Full configuration for service.
        /// </summary>
        /// <remarks>Values can be overwritten by keys set via <see cref="With(string,string)"/>.</remarks>
        /// <param name="config">Service configuration document.</param>
        /// <returns>This instance.</returns>
        IDreamServiceConfigurationBuilder WithConfig(XDoc config);

        /// <summary>
        /// Add a configuration key/value pair.
        /// </summary>
        /// <param name="key">Key to add.</param>
        /// <param name="value">Value to add.</param>
        /// <returns>This instance.</returns>
        IDreamServiceConfigurationBuilder With(string key, string value);

        /// <summary>
        /// Add a configuration key/value pair.
        /// </summary>
        /// <param name="key">Key to add.</param>
        /// <param name="childDoc">Child document value to add.</param>
        /// <returns>This instance.</returns>
        IDreamServiceConfigurationBuilder With(string key, XDoc childDoc);

        /// <summary>
        /// Skip configuring this service if one already exists at its path.
        /// </summary>
        /// <returns>This instance.</returns>
        IDreamServiceConfigurationBuilder SkipIfExists();
    }
}