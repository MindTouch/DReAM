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
using System.Linq;
using System.Reflection;
using log4net;
using MindTouch.Xml;

namespace MindTouch.Dream {

    /// <summary>
    /// Fluent configuration helper for creating creating a Dream service startup script
    /// </summary>
    public class DreamServiceRegistrationBuilder {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly Dictionary<string, DreamServiceConfigurationBuilder> _registrations = new Dictionary<string, DreamServiceConfigurationBuilder>();

        //--- Methods ---

        /// <summary>
        /// Register a service using default conventions.
        /// </summary>
        /// <remarks>
        /// Default convention means no config values and path is either Foo for FooService or type name if not postfixed with Service.
        /// </remarks>
        /// <typeparam name="T"><see cref="IDreamService"/> to register.</typeparam>
        public void RegisterService<T>() {
            RegisterService<T>(c => { });
        }

        /// <summary>
        /// Register a service with callback for configuration.
        /// </summary>
        /// <typeparam name="T"><see cref="IDreamService"/> to register.</typeparam>
        /// <param name="configurationCallback">Callback for modifying the created <see cref="IDreamServiceConfigurationBuilder"/> instance.</param>
        /// <returns>
        /// True if service was configured (only possible to be false if <see cref="IDreamServiceConfigurationBuilder.SkipIfExists"/> was
        /// specified and a service already exists at the configured path.
        /// </returns>
        public bool RegisterService<T>(Action<IDreamServiceConfigurationBuilder> configurationCallback) {
            return RegisterService(typeof(T), configurationCallback);
        }

        /// <summary>
        /// Register a service with callback for configuration.
        /// </summary>
        /// <param name="serviceType">Type of <see cref="IDreamService"/> to register.</param>
        /// <param name="configurationCallback">Callback for modifying the created <see cref="IDreamServiceConfigurationBuilder"/> instance.</param>
        /// <returns>
        /// True if service was configured (only possible to be false if <see cref="IDreamServiceConfigurationBuilder.SkipIfExists"/> was
        /// specified and a service already exists at the configured path.
        /// </returns>
        public bool RegisterService(Type serviceType, Action<IDreamServiceConfigurationBuilder> configurationCallback) {
            var config = new DreamServiceConfigurationBuilder(serviceType);
            configurationCallback(config);
            return RegisterService(config);
        }

        /// <summary>
        /// Build a service configuration script for posting against host/services from the presently configured services.
        /// </summary>
        /// <returns></returns>
        public XDoc Build() {
            var script = new XDoc("script");
            foreach(var registration in _registrations.Values) {
                _log.DebugFormat("adding service '{0}' at path '{1}' to script", registration.ServiceType.Name, registration.Path);
                script.Start("action")
                    .Attr("verb", "POST")
                    .Attr("path", "/host/services")
                    .AddAll(registration.ServiceConfig)
                .End();
            }
            return script;
        }

        /// <summary>
        /// Scan the provided assembly for all <see cref="IDreamService"/> that live in a namespace ending with .Services and configure them
        /// by convention.
        /// </summary>
        /// <param name="assembly">Assembly to scan.</param>
        public void ScanAssemblyForServices(Assembly assembly) {
            ScanAssemblyForServices(assembly, null);
        }

        /// <summary>
        /// Scan the provided assembly for <see cref="IDreamService"/> that live in a namespace ending with .Services and configure them
        /// by convention.
        /// </summary>
        /// <param name="assembly">Assembly to scan.</param>
        /// <param name="filter">Filter expression to exclude services.</param>
        public void ScanAssemblyForServices(Assembly assembly, Func<Type, bool> filter) {
            ScanAssemblyForServices(assembly, filter, null);
        }

        /// <summary>
        /// Scan the provided assembly for <see cref="IDreamService"/> that live in a namespace ending with .Services and configure them.
        /// </summary>
        /// <param name="assembly">Assembly to scan.</param>
        /// <param name="filter">Filter expression to exclude services.</param>
        /// <param name="configurationCallback">Configuration callback expression to be called for each <see cref="Type"/> to be configured.</param>
        public void ScanAssemblyForServices(Assembly assembly, Func<Type, bool> filter, Action<Type, IDreamServiceConfigurationBuilder> configurationCallback) {
            Func<Type, bool> defaultFilter = t => true;
            var serviceTypes = from t in assembly.GetTypes()
                               where !string.IsNullOrEmpty(t.Namespace) &&
                                   t.Namespace.EndsWith(".Services") &&
                                   t.IsA<IDreamService>() &&
                                   t.Name.EndsWith("Service") &&
                                   (filter ?? defaultFilter)(t)
                               select t;
            foreach(var serviceType in serviceTypes) {
                var config = new DreamServiceConfigurationBuilder(serviceType);
                if(configurationCallback != null) {
                    configurationCallback(serviceType, config);
                }
                if(RegisterService(config)) {
                    continue;
                }
                var existing = _registrations[config.Path];
                throw new InvalidOperationException(string.Format("Can't register service '{0}' because '{1}' is already bound to path '{2}'",
                    serviceType,
                    existing.ServiceType,
                    config.Path
                ));
            }
        }

        private bool RegisterService(DreamServiceConfigurationBuilder config) {
            lock(_registrations) {
                if(_registrations.ContainsKey(config.Path)) {
                    if(config.SkipServiceIfExists) {
                        return false;
                    }
                }
                _registrations[config.Path] = config;
            }
            return true;
        }
    }
}