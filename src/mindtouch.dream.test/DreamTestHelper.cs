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
using System.Globalization;
using Autofac;
using log4net;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.Test {

    /// <summary>
    /// A static helper class for creating <see cref="DreamHost"/> and services for testing.
    /// </summary>
    public static class DreamTestHelper {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Extension Methods ---
        /// <summary>
        /// Create a <see cref="IDreamService"/> on a given <see cref="DreamHost"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IDreamService"/> to create.</typeparam>
        /// <param name="hostInfo">The info instance for the target <see cref="DreamHost"/>.</param>
        /// <param name="pathPrefix">Path prefix to use for randomly generated path (primarily used to more easily recognize the service in logs).</param>
        /// <param name="extraConfig">Additional configuration to use for service instantiation.</param>
        /// <returns>An instance of <see cref="DreamServiceInfo"/> for easy service access</returns>
        public static DreamServiceInfo CreateService<T>(this DreamHostInfo hostInfo, string pathPrefix, XDoc extraConfig) where T : IDreamService {
            return CreateService(hostInfo, typeof(T), pathPrefix, extraConfig);
        }

        /// <summary>
        /// Create a <see cref="IDreamService"/> on a given <see cref="DreamHost"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IDreamService"/> to create.</typeparam>
        /// <param name="hostInfo">The info instance for the target <see cref="DreamHost"/>.</param>
        /// <param name="pathPrefix">Path prefix to use for randomly generated path (primarily used to more easily recognize the service in logs).</param>
        /// <returns>An instance of <see cref="DreamServiceInfo"/> for easy service access</returns>
        public static DreamServiceInfo CreateService<T>(this DreamHostInfo hostInfo, string pathPrefix) where T : IDreamService {
            return CreateService(hostInfo, typeof(T), pathPrefix);
        }

        /// <summary>
        /// Create a <see cref="IDreamService"/> on a given <see cref="DreamHost"/>.
        /// </summary>
        /// <param name="hostInfo">The info instance for the target <see cref="DreamHost"/>.</param>
        /// <param name="serviceType">Type of the <see cref="IDreamService"/> to create.</param>
        /// <param name="pathPrefix">Path prefix to use for randomly generated path (primarily used to more easily recognize the service in logs).</param>
        /// <param name="extraConfig">Additional configuration to use for service instantiation.</param>
        /// <returns>An instance of <see cref="DreamServiceInfo"/> for easy service access</returns>
        public static DreamServiceInfo CreateService(this DreamHostInfo hostInfo, Type serviceType, string pathPrefix, XDoc extraConfig) {
            string path = (string.IsNullOrEmpty(pathPrefix)) ? StringUtil.CreateAlphaNumericKey(6).ToLower() : pathPrefix + "_" + StringUtil.CreateAlphaNumericKey(3).ToLower();
            XDoc config = new XDoc("config")
                .Elem("class", serviceType.FullName)
                .Elem("path", path);
            if(extraConfig != null) {
                foreach(XDoc extra in extraConfig["*"]) {
                    config.Add(extra);
                }
            }
            return CreateService(hostInfo, config);
        }

        /// <summary>
        /// Create a <see cref="IDreamService"/> on a given <see cref="DreamHost"/>.
        /// </summary>
        /// <param name="hostInfo">The info instance for the target <see cref="DreamHost"/>.</param>
        /// <param name="sid">Service Identifier</param>
        /// <param name="pathPrefix">Path prefix to use for randomly generated path (primarily used to more easily recognize the service in logs).</param>
        /// <param name="extraConfig">Additional configuration to use for service instantiation.</param>
        /// <returns>An instance of <see cref="DreamServiceInfo"/> for easy service access</returns>
        public static DreamServiceInfo CreateService(this DreamHostInfo hostInfo, string sid, string pathPrefix, XDoc extraConfig) {
            string path = (string.IsNullOrEmpty(pathPrefix)) ? StringUtil.CreateAlphaNumericKey(6).ToLower() : pathPrefix + "_" + StringUtil.CreateAlphaNumericKey(3).ToLower();
            XDoc config = new XDoc("config")
                .Elem("sid", sid)
                .Elem("path", path);
            if(extraConfig != null) {
                foreach(XDoc extra in extraConfig["*"]) {
                    config.Add(extra);
                }
            }
            return CreateService(hostInfo, config);
        }

        /// <summary>
        /// Create a <see cref="IDreamService"/> on a given <see cref="DreamHost"/>.
        /// </summary>
        /// <param name="hostInfo">The info instance for the target <see cref="DreamHost"/>.</param>
        /// <param name="serviceType">Type of the <see cref="IDreamService"/> to create.</param>
        /// <param name="pathPrefix">Path prefix to use for randomly generated path (primarily used to more easily recognize the service in logs).</param>
        /// <returns>An instance of <see cref="DreamServiceInfo"/> for easy service access</returns>
        public static DreamServiceInfo CreateService(this DreamHostInfo hostInfo, Type serviceType, string pathPrefix) {
            string path = (string.IsNullOrEmpty(pathPrefix)) ? StringUtil.CreateAlphaNumericKey(6).ToLower() : pathPrefix + "_" + StringUtil.CreateAlphaNumericKey(3).ToLower();
            XDoc config = new XDoc("config")
                .Elem("class", serviceType.FullName)
                .Elem("path", path);
            return CreateService(hostInfo, config);
        }

        /// <summary>
        /// Create a <see cref="IDreamService"/> on a given <see cref="DreamHost"/>.
        /// </summary>
        /// <param name="hostInfo">The info instance for the target <see cref="DreamHost"/>.</param>
        /// <param name="config">Configuration to use for service instantiation.</param>
        /// <returns>An instance of <see cref="DreamServiceInfo"/> for easy service access</returns>
        public static DreamServiceInfo CreateService(this DreamHostInfo hostInfo, XDoc config) {
            string path = config["path"].AsText;
            DreamMessage result = hostInfo.Host.Self.At("services").Post(config, new Result<DreamMessage>()).Wait();
            if(!result.IsSuccessful) {
                throw new Exception(string.Format(
                    "Unable to start service with config:\r\n{0}\r\n{1}",
                    config.ToPrettyString(),
                    result.HasDocument
                        ? string.Format("{0}: {1}", result.Status, result.ToDocument()["message"].AsText)
                        : result.Status.ToString()));
            }
            return new DreamServiceInfo(hostInfo, path, result.ToDocument());

        }

        /// <summary>
        /// Create a new mock service instance.
        /// </summary>
        /// <param name="hostInfo">Host info.</param>
        /// <returns>New mock service info instance.</returns>
        public static MockServiceInfo CreateMockService(this DreamHostInfo hostInfo) {
            return MockService.CreateMockService(hostInfo);
        }

        /// <summary>
        /// Create a new mock service instance.
        /// </summary>
        /// <param name="hostInfo">Host info.</param>
        /// <param name="extraConfig">Additional service configuration.</param>
        /// <returns>New mock service info instance.</returns>
        public static MockServiceInfo CreateMockService(this DreamHostInfo hostInfo, XDoc extraConfig) {
            return MockService.CreateMockService(hostInfo, extraConfig);
        }

        /// <summary>
        /// Create a new mock service instance.
        /// </summary>
        /// <param name="hostInfo">Host info.</param>
        /// <param name="extraConfig">Additional service configuration.</param>
        /// <param name="privateStorage">Use private storage</param>
        /// <returns>New mock service info instance.</returns>
        public static MockServiceInfo CreateMockService(this DreamHostInfo hostInfo, XDoc extraConfig, bool privateStorage) {
            return MockService.CreateMockService(hostInfo, extraConfig, privateStorage);
        }

        //--- Class Methods ---

        /// <summary>
        /// Create a <see cref="DreamHost"/> at a random port (to avoid collisions in tests).
        /// </summary>
        /// <param name="config">Additional configuration for the host.</param>
        /// <param name="container">(optional) IoC Container to use.</param>
        /// <param name="connectionLimit">(optional) Maximum number of concurrent requests to process.</param>
        /// <returns>A <see cref="DreamHostInfo"/> instance for easy access to the host.</returns>
        public static DreamHostInfo CreateRandomPortHost(XDoc config = null, IContainer container = null, int connectionLimit = 10) {
            if(config == null) {
                config = new XDoc("config");
            }
            var path = "/";
            if(!config["uri.public"].IsEmpty) {
                path = config["uri.public"].AsText;
            }
            for(var i = 1; i <= 10; i++) {
                var port = TestPortSource.GetAvailablePort();
                var localhost = string.Format("http://localhost:{0}{1}", port, path);
                UpdateElement(config, "http-port", port.ToInvariantString());
                UpdateElement(config, "uri.public", localhost);
                if(connectionLimit > 0) {
                    UpdateElement(config, "connect-limit", connectionLimit.ToInvariantString());
                }
                var apikey = config["apikey"].Contents;
                if(string.IsNullOrEmpty(apikey)) {
                    apikey = StringUtil.CreateAlphaNumericKey(32); //generate a random api key
                    config.Elem("apikey", apikey);
                }
                _log.DebugFormat("api key: {0}", apikey);
                _log.DebugFormat("port:    {0}", port);
                _log.DebugFormat("config:\r\n{0}", config.ToPrettyString());
                try {
                    var host = (container == null) ? new DreamHost(config) : new DreamHost(config, container);
                    host.Self.At("load").With("name", "mindtouch.dream.test").Post(DreamMessage.Ok());
                    return new DreamHostInfo(Plug.New(localhost), host, apikey);
                } catch(Exception e) {
                    _log.DebugFormat("Failed attempt {0} of creating a host: {1}", i, e.Message);
                }
            }
            throw new InvalidOperationException("Unable to create a new host after 10 attempts");
        }

        private static void UpdateElement(XDoc config, string element, string value) {
            if(config[element].IsEmpty) {
                config.Elem(element, value);
            } else {
                config[element].Replace(new XDoc(element).Value(value));
            }
        }
    }
}
