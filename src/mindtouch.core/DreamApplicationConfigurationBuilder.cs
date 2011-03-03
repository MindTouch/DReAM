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
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Web;
using log4net;
using MindTouch.Xml;

namespace MindTouch.Dream {

    /// <summary>
    /// Fluent interface for building an <see cref="DreamApplication"/>.
    /// </summary>
    public class DreamApplicationConfigurationBuilder {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---

        /// <summary>
        /// Create configuration from <see cref="ConfigurationManager.AppSettings"/> and execution base path.
        /// </summary>
        /// <returns>Configuration instance.</returns>
        public static DreamApplicationConfigurationBuilder FromAppSettings() {
            var basePath = HttpContext.Current.Server.MapPath("~");
            return FromAppSettings(basePath, null);
        }

        /// <summary>
        /// Create configuration from <see cref="ConfigurationManager.AppSettings"/> and execution base path.
        /// </summary>
        /// <param name="basePath">File system path to execution base.</param>
        /// <returns>Configuration instance.</returns>
        public static DreamApplicationConfigurationBuilder FromAppSettings(string basePath) {
            return FromAppSettings(basePath, null);
        }

        /// <summary>
        /// Create configuration from <see cref="ConfigurationManager.AppSettings"/>, execution base path and storage path.
        /// </summary>
        /// <param name="basePath">File system path to execution base.</param>
        /// <param name="storagePath">File sytem path to where the host should keep it's local storage.</param>
        /// <returns>Configuration instance.</returns>
        public static DreamApplicationConfigurationBuilder FromAppSettings(string basePath, string storagePath) {
            var config = new DreamApplicationConfiguration();
            var settings = ConfigurationManager.AppSettings;
            var debugSetting = settings["dream.env.debug"] ?? "debugger-only";
            if(string.IsNullOrEmpty(storagePath)) {
                storagePath = settings["dream.storage.path"] ?? settings["storage-dir"] ?? Path.Combine(basePath, "App_Data");
            }
            if(string.IsNullOrEmpty(storagePath)) {
                storagePath = Path.Combine(basePath, "storage");
            } else if(!Path.IsPathRooted(storagePath)) {
                storagePath = Path.Combine(basePath, storagePath);
            }
            var guid = settings["dream.guid"] ?? settings["guid"];
            var hostPath = settings["dream.host.path"] ?? settings["host-path"];
            config.Apikey = settings["dream.apikey"] ?? settings["apikey"] ?? StringUtil.CreateAlphaNumericKey(8);
            config.HostConfig = new XDoc("config")
                .Elem("guid", guid)
                .Elem("storage-dir", storagePath)
                .Elem("host-path", hostPath)
                .Elem("connect-limit", settings["connect-limit"])
                .Elem("apikey", config.Apikey)
                .Elem("debug", debugSetting);
            var rootRedirect = settings["dream.root.redirect"];
            if(!string.IsNullOrEmpty(rootRedirect)) {
                config.HostConfig.Elem("root-redirect", rootRedirect);
            }
            config.ServicesDirectory = settings["dream.service.path"] ?? settings["service-dir"] ?? Path.Combine("bin", "services");
            if(!Path.IsPathRooted(config.ServicesDirectory)) {
                config.ServicesDirectory = Path.Combine(basePath, config.ServicesDirectory);
            }
            return new DreamApplicationConfigurationBuilder(config);
        }

        /// <summary>
        /// Create a new default configuration builder, i.e. not pre-initialized with external settings.
        /// </summary>
        /// <returns>New configuration instance.</returns>
        public static DreamApplicationConfigurationBuilder Create() {
            return new DreamApplicationConfigurationBuilder(new DreamApplicationConfiguration());
        }

        //--- Fields ---
        private Assembly _assembly;
        private readonly DreamApplicationConfiguration _configuration;
        private DreamServiceRegistrationBuilder _serviceRegistrationBuilder;

        //--- Constructors ---
        private DreamApplicationConfigurationBuilder(DreamApplicationConfiguration configuration) {
            _configuration = configuration;
        }

        //--- Methods ---

        /// <summary>
        /// Defined the master apikey for the application.
        /// </summary>
        /// <param name="apikey">Api key</param>
        /// <returns>Current builder instance.</returns>
        public DreamApplicationConfigurationBuilder WithApikey(string apikey) {
            _configuration.Apikey = apikey;
            return this;
        }

        /// <summary>
        /// Define the directory to scan for <see cref="IDreamService"/> types.
        /// </summary>
        /// <param name="servicesDirectory">Absolute path to directory containing assemblies with service types.</param>
        /// <returns>Current builder instance.</returns>
        public DreamApplicationConfigurationBuilder WithServicesDirectory(string servicesDirectory) {
            _configuration.ServicesDirectory = servicesDirectory;
            return this;
        }

        /// <summary>
        /// Define the <see cref="DreamHostService"/> xml configuration.
        /// </summary>
        /// <param name="hostConfig"></param>
        /// <returns>Current builder instance.</returns>
        public DreamApplicationConfigurationBuilder WithHostConfig(XDoc hostConfig) {
            _configuration.HostConfig = hostConfig;
            return this;
        }

        /// <summary>
        /// Attach the <see cref="HttpApplication"/> that the <see cref="DreamApplication"/> to be built will be attached to.
        /// </summary>
        /// <param name="application">HttpApplication to attach to.</param>
        /// <returns>Current builder instance.</returns>
        public DreamApplicationConfigurationBuilder ForHttpApplication(HttpApplication application) {
            return WithApplicationAssembly(application.GetType().BaseType.Assembly);
        }

        /// <summary>
        /// Add a service configuration.
        /// </summary>
        /// <param name="configurationCallback">Service configuration callback.</param>
        /// <returns>Current builder instance.</returns>
        public DreamApplicationConfigurationBuilder WithServiceConfiguration(Action<DreamServiceRegistrationBuilder> configurationCallback) {
            if(_serviceRegistrationBuilder == null) {
                _serviceRegistrationBuilder = new DreamServiceRegistrationBuilder();
            }
            configurationCallback(_serviceRegistrationBuilder);
            return this;
        }

        /// <summary>
        /// Attach the assembly to scan for services by the conventions of <see cref="DreamServiceRegistrationBuilder.ScanAssemblyForServices(System.Reflection.Assembly)"/>
        /// </summary>
        /// <remarks>
        /// By default, the assembly that the HttpApplication lives in will be scanned.
        /// </remarks>
        /// <param name="assembly">Service assembly.</param>
        /// <returns>Current builder instance.</returns>
        public DreamApplicationConfigurationBuilder WithApplicationAssembly(Assembly assembly) {
            _assembly = assembly;
            return this;
        }

        /// <summary>
        /// Provide an assembly scanning filter.
        /// </summary>
        /// <param name="filter">Filter callback.</param>
        /// <returns>Current builder instance.</returns>
        public DreamApplicationConfigurationBuilder WithFilteredAssemblyServices(Func<Type, bool> filter) {
            if(_assembly == null) {
                throw new ArgumentException("Builder does not have an assembly to scan");
            }
            WithServiceConfiguration(builder => builder.ScanAssemblyForServices(_assembly, filter));
            return this;
        }

        /// <summary>
        /// Set the path prefix for the application.
        /// </summary>
        /// <param name="prefix">Path prefix.</param>
        /// <returns>Current builder instance.</returns>
        public DreamApplicationConfigurationBuilder WithPathPrefix(string prefix) {
            _configuration.Prefix = prefix;
            return this;
        }

        /// <summary>
        /// Create the new <see cref="DreamApplication"/> inside the <see cref="HttpApplication"/>.
        /// </summary>
        /// <returns>Application instance.</returns>
        public DreamApplication CreateApplication() {
            return DreamApplication.Create(Build());
        }

        internal DreamApplicationConfiguration Build() {
            if(_serviceRegistrationBuilder == null) {
                WithFilteredAssemblyServices(t => true);
            }
            _configuration.Script = _serviceRegistrationBuilder.Build();
            return _configuration;
        }
    }
}