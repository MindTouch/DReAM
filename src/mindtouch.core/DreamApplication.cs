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
using System.IO;
using System.Web;
using System.Web.Routing;
using log4net;
using MindTouch.Dream.Http;
using MindTouch.Tasking;

namespace MindTouch.Dream {

    /// <summary>
    /// Container for embedding the Dream hosting environment inside of an ASP.NET <see cref="HttpApplication"/>.
    /// </summary>
    public class DreamApplication {

        //--- Constants ---
        private const string ENV_CONFIG_KEY = "DreamApplicationEnvironment";

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---

        /// <summary>
        /// Initialize the Dream hosting environment for a given <see cref="HttpApplication"/>.
        /// </summary>
        /// <remarks>
        /// There can only be a single <see cref="DreamApplication"/> in any one <see cref="HttpApplication"/>.
        /// </remarks>
        /// <param name="application">Application to embed the hosting environment.</param>
        /// <returns>Created instance.</returns>
        public static DreamApplication CreateInHttpApplication(HttpApplication application) {
            var path = HttpContext.Current.Server.MapPath("~");
            return Create(DreamApplicationConfigurationBuilder
                .FromAppSettings(path)
                .ForHttpApplication(application)
                .Build()
            );
        }

        internal static DreamApplication Create(DreamApplicationConfiguration configuration) {
            var environment = new DreamApplication(configuration);
            HttpContext.Current.Application[ENV_CONFIG_KEY] = environment;
            return environment;
        }

        //--- Class Properties ---

        /// <summary>
        /// The application instance attached to the current HttpContext, if any.
        /// </summary>
        public static DreamApplication Current { get { return (DreamApplication)HttpContext.Current.Application[ENV_CONFIG_KEY]; } }

        //--- Fields ---
        private readonly DreamApplicationConfiguration _appConfig;
        private readonly IDreamEnvironment _env;
        private readonly Plug _self;

        //--- Constructors ---
        private DreamApplication(DreamApplicationConfiguration appConfig) {
            _appConfig = appConfig;
            _env = new DreamHostService();
            Initialize();
            RegisterDefaultRoute();
            _self = Plug.New(_env.Self.Uri.AtAbsolutePath("/"));
        }

        //--- Properties ---

        /// <summary>
        /// The application specific base Uri for the current request.
        /// </summary>
        public XUri RequestBaseUri { get { return GetRequestBaseUri(HttpContext.Current.Request); } }

        /// <summary>
        /// Local Plug to the Application environment.
        /// </summary>
        public Plug Self { get { return _self; } }

        internal DreamApplicationConfiguration AppConfig { get { return _appConfig; } }

        //--- Methods ---

        /// <summary>
        /// Application specific base uri for a given request
        /// </summary>
        /// <param name="request">HttpRequest instance.</param>
        /// <returns>Base Uri.</returns>
        public XUri GetRequestBaseUri(HttpRequest request) {
            var transport = new XUri(request.Url).WithoutPathQueryFragment().AtAbsolutePath(request.ApplicationPath);
            var prefix = _appConfig.Prefix;
            if(!string.IsNullOrEmpty(prefix)) {
                transport = transport.At(prefix);
            }
            return transport;
        }

        private void Initialize() {
            _log.InfoMethodCall("Startup");
            try {
                _log.Info("initializing DreamApplication");
                _env.Initialize(_appConfig.HostConfig);

                // load assemblies in 'services' folder
                _log.DebugFormat("examining services directory '{0}'", _appConfig.ServicesDirectory);
                var host = _env.Self.With("apikey", _appConfig.Apikey);
                if(Directory.Exists(_appConfig.ServicesDirectory)) {
                    foreach(var file in Directory.GetFiles(_appConfig.ServicesDirectory, "*.dll")) {
                        var assembly = Path.GetFileNameWithoutExtension(file);
                        _log.DebugFormat("attempting to load '{0}'", assembly);

                        // register assembly blueprints
                        var response = host.At("load").With("name", assembly).Post(new Result<DreamMessage>(TimeSpan.MaxValue)).Wait();
                        if(!response.IsSuccessful) {
                            _log.WarnFormat("DreamHost: ERROR: assembly '{0}' failed to load", file);
                        }
                    }
                } else {
                    _log.WarnFormat("DreamHost: WARN: no services directory '{0}'", _appConfig.ServicesDirectory);
                }

                // execute script
                if(_appConfig.Script != null && !_appConfig.Script.IsEmpty) {
                    host.At("execute").Post(_appConfig.Script);
                }
            } catch(Exception e) {
                _log.ErrorExceptionMethodCall(e, "ctor");
                throw;
            }
        }

        private void RegisterDefaultRoute() {
            RouteTable.Routes.Add(
                new Route(
                    string.IsNullOrEmpty(AppConfig.Prefix) ? "{*all}" : AppConfig.Prefix + "/{*all}",
                    new RouteValueDictionary { { "controller", "qwewqewqeqweq" } }, // A random and unlikely controller name
                    new RouteValueDictionary { { "controller", "qwewqewqeqweq" } }, // that is also required to match, to circumvent matching by GetVirtualPath
                    new DreamRouteHandler(this, _env)
                )
            );
        }
    }
}