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
using MindTouch.Xml;

namespace MindTouch.Dream {
    /// <summary>
    /// Concrete implementation of <see cref="IDreamServiceConfigurationBuilder"/>.
    /// </summary>
    /// <remarks>
    ///  Cannot be used by itself since only configuration invocation happens via callback providing the instance.
    /// </remarks>
    public class DreamServiceConfigurationBuilder : IDreamServiceConfigurationBuilder {

        //--- Fields ---
        private readonly Type _type;
        private string _path;
        private XDoc _serviceConfig;
        private readonly Dictionary<string, object> _pairs = new Dictionary<string, object>();
        private bool _skipIfExists;

        //--- Constructors ---

        /// <summary>
        /// Create a new configuration for a given type.
        /// </summary>
        /// <param name="type"><see cref="IDreamService"/> instance to configure.</param>
        public DreamServiceConfigurationBuilder(Type type) {
            _type = type;
            _path = _type.Name.EndsWith("Service") ? _type.Name.Substring(0, _type.Name.Length - 7) : _type.Name;
        }

        //--- Properties ---

        /// <summary>
        /// Type of the <see cref="IDreamService"/> configured by this instance.
        /// </summary>
        public Type ServiceType { get { return _type; } }

        /// <summary>
        /// Server path the service is to be located at.
        /// </summary>
        public string Path { get { return _path; } }

        /// <summary>
        /// True if service configuration is to be skipped if one already exists at the specified <see cref="Path"/>.
        /// </summary>
        public bool SkipServiceIfExists { get { return _skipIfExists; } }

        //--- Methods ---
        IDreamServiceConfigurationBuilder IDreamServiceConfigurationBuilder.AtPath(string path) {
            _path = path;
            return this;
        }

        IDreamServiceConfigurationBuilder IDreamServiceConfigurationBuilder.WithConfig(XDoc config) {
            _serviceConfig = config;
            return this;
        }

        IDreamServiceConfigurationBuilder IDreamServiceConfigurationBuilder.With(string key, string value) {
            _pairs[key] = value;
            return this;
        }

        IDreamServiceConfigurationBuilder IDreamServiceConfigurationBuilder.With(string key, XDoc childDoc) {
            _pairs[key] = childDoc;
            return this;
        }

        IDreamServiceConfigurationBuilder IDreamServiceConfigurationBuilder.SkipIfExists() {
            _skipIfExists = true;
            return this;
        }

        /// <summary>
        /// Retrieve the service configuration document.
        /// </summary>
        public XDoc ServiceConfig {
            get {
                var config = _serviceConfig == null ? new XDoc("config") : _serviceConfig.Clone();
                foreach(var pair in _pairs) {
                    var childDoc = pair.Value as XDoc;
                    config[pair.Key].Remove();
                    if(childDoc != null) {
                        config.Start(pair.Key).AddAll(childDoc["*"]);
                    } else {
                        config.Elem(pair.Key, pair.Value);
                    }
                }
                config["class"].Remove();
                config["sid"].Remove();
                config.Elem("class", _type.FullName);
                if(!string.IsNullOrEmpty(_path)) {
                    config["path"].Remove();
                    config.Elem("path", _path);
                }
                return config;
            }
        }
    }
}