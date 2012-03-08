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
using System.IO;
using System.Net;
using System.Net.Sockets;
using Autofac;
using MindTouch.Dream.Http;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream {
    using Yield = IEnumerator<IYield>;

    /// <summary>
    /// Provides a hosting environment for <see cref="IDreamService"/> based services.
    /// </summary>
    public class DreamHost : IDisposable {

        //--- Constants ---

        /// <summary>
        /// Default dream host port: 8081
        /// </summary>
        public const int DEFAULT_PORT = 8081;

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        private static XUri MakeUri(IPAddress address, int port) {
            switch(address.AddressFamily) {
            case System.Net.Sockets.AddressFamily.InterNetwork:
                return new XUri(String.Format("http://{0}:{1}/", address, port));
            case System.Net.Sockets.AddressFamily.InterNetworkV6:
                return new XUri(String.Format("http://[{0}]:{1}/", address, port));
            }
            return null;
        }

        //--- Fields ---
        private IDreamEnvironment _env;
        private Plug _host;
        private List<HttpTransport> _transports = new List<HttpTransport>();
        private bool _disposed;
        private string _dreamInParamAuthtoken;

        //--- Constructors ---

        /// <summary>
        /// Create a new host with default settings.
        /// </summary>
        public DreamHost() : this(new XDoc("config"), null) { }

        /// <summary>
        /// Create a new host with provided configuration.
        /// </summary>
        /// <param name="config">Host configuration.</param>
        public DreamHost(XDoc config) : this(config, null) { }

        /// <summary>
        /// Create a new host with provided configuration and an Inversion of Control container.
        /// </summary>
        /// <remarks>
        /// The IoC container is also injected into default activator, so that <see cref="IDreamService"/> instances
        /// can be resolved from the container. The host configuration is provided to the container as a typed parameter.
        /// </remarks>
        /// <param name="config">Host configuration.</param>
        /// <param name="container">IoC Container.</param>
        public DreamHost(XDoc config, IContainer container) {
            if(config == null) {
                throw new ArgumentNullException("config");
            }

            // read host settings
            string appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);
            int limit = config["connect-limit"].AsInt ?? 0;
            int httpPort = config["http-port"].AsInt ?? DEFAULT_PORT;
            AuthenticationSchemes authenticationScheme = AuthenticationSchemes.Anonymous;
            string authShemes = config["authentication-shemes"].AsText;
            if(!String.IsNullOrEmpty(authShemes)) {
                try {
                    authenticationScheme = (AuthenticationSchemes)Enum.Parse(typeof(AuthenticationSchemes), authShemes, true);
                } catch(Exception e) {
                    _log.Warn(String.Format("invalid authetication scheme specified :{0}", authShemes), e);
                }
            }

            // get the authtoken for whitelisting dream.in.* query args
            _dreamInParamAuthtoken = config["dream.in.authtoken"].AsText;

            // read ip-addresses
            var addresses = new List<string>();
            foreach(XDoc ip in config["host|ip"]) {
                addresses.Add(ip.AsText);
            }
            if(addresses.Count == 0) {

                // if no addresses were supplied listen to all
                addresses.Add("*:" + httpPort);
            }

            // use default servername
            XUri publicUri = config["uri.public"].AsUri;
            if(publicUri == null) {

                // backwards compatibility
                publicUri = config["server-name"].AsUri;
                if(publicUri == null) {
                    foreach(IPAddress addr in Dns.GetHostAddresses(Dns.GetHostName())) {
                        if(addr.AddressFamily == AddressFamily.InterNetwork) {
                            XUri.TryParse("http://" + addr, out publicUri);
                        }
                    }
                    if(publicUri == null) {
                        // failed to get an address out of dns, fall back to localhost
                        XUri.TryParse("http://localhost", out publicUri);
                    }
                }
                publicUri = publicUri.AtPath(config["server-path"].AsText ?? config["path-prefix"].AsText ?? string.Empty);
            }

            // create environment and initialize it
            _env = new DreamHostService(container);
            try {

                // initialize environment
                string apikey = config["apikey"].AsText ?? StringUtil.CreateAlphaNumericKey(32);
                XDoc serviceConfig = new XDoc("config");
                var storageType = config["storage/@type"].AsText ?? "local";
                if("s3".EqualsInvariant(storageType)) {
                    serviceConfig.Add(config["storage"]);
                } else {
                    serviceConfig.Elem("storage-dir", config["storage-dir"].AsText ?? config["service-dir"].AsText ?? appDirectory);
                }
                serviceConfig.Elem("apikey", apikey);
                serviceConfig.Elem("uri.public", publicUri);
                serviceConfig.Elem("connect-limit", limit);
                serviceConfig.Elem("guid", config["guid"].AsText);
                serviceConfig.AddAll(config["components"]);
                var memorize = config["memorize-aliases"];
                if(!memorize.IsEmpty) {
                    serviceConfig.Elem("memorize-aliases", memorize.AsBool);
                }
                _env.Initialize(serviceConfig);

                // initialize host plug
                _host = _env.Self.With("apikey", apikey);

                // load assemblies in 'services' folder
                string servicesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "services");
                if(Directory.Exists(servicesFolder)) {

                    // Note (arnec): Deprecated, but the suggested alternative really doesn't apply since we don't want to
                    // load services into a separate appdomain.
#pragma warning disable 618,612
                    AppDomain.CurrentDomain.AppendPrivatePath("services");
#pragma warning restore 618,612
                    foreach(string file in Directory.GetFiles(servicesFolder, "*.dll")) {

                        // register assembly blueprints
                        DreamMessage response = _host.At("load").With("name", Path.GetFileNameWithoutExtension(file)).Post(new Result<DreamMessage>(TimeSpan.MaxValue)).Wait();
                        if(!response.IsSuccessful) {
                            _log.WarnFormat("DreamHost: ERROR: assembly '{0}' failed to load", file);
                        }
                    }
                }

                // add acccess-points
                AddListener(new XUri(String.Format("http://{0}:{1}/", "localhost", httpPort)), authenticationScheme);

                // check if user prescribed a set of IP addresses to use
                if(addresses != null) {

                    // listen to custom addresses (don't use the supplied port info, we expect that to be part of the address)
                    foreach(string address in addresses) {
                        if(!StringUtil.EqualsInvariantIgnoreCase(address, "localhost")) {
                            AddListener(new XUri(String.Format("http://{0}/", address)), authenticationScheme);
                        }
                    }
                } else {

                    // add listeners for all known IP addresses
                    foreach(IPAddress address in Dns.GetHostAddresses(Dns.GetHostName())) {
                        XUri uri = MakeUri(address, httpPort);
                        if(uri != null) {
                            AddListener(uri, authenticationScheme);
                            try {
                                foreach(string alias in Dns.GetHostEntry(address).Aliases) {
                                    AddListener(new XUri(String.Format("http://{0}:{1}/", alias, httpPort)), authenticationScheme);
                                }
                            } catch { }
                        }
                    }
                }
            } catch(Exception e) {
                if((e is HttpListenerException) && e.Message.EqualsInvariant("Access is denied")) {
                    _log.ErrorExceptionMethodCall(e, "ctor", "insufficient privileges to create HttpListener, make sure the application runs with Administrator rights");
                } else {
                    _log.ErrorExceptionMethodCall(e, "ctor");
                }
                try {
                    _env.Deinitialize();
                } catch { }
                throw;
            }
        }

        /// <summary>
        /// Finalizer to clean-up an undisposed host.
        /// </summary>
        ~DreamHost() {
            Dispose(false);
        }

        //--- Properties ---

        /// <summary>
        /// Http location of host <see cref="IDreamService"/>.
        /// </summary>
        public Plug Self { get { return _host; } }

        /// <summary>
        /// Global Id used for local:// uri's
        /// </summary>
        public Guid GlobalId { get { return _env.GlobalId; } }

        /// <summary>
        /// <see langword="True"/> if the host is running.
        /// </summary>
        public bool IsRunning { get { return _env.IsRunning; } }

        /// <summary>
        /// Root local:// uri for this host.
        /// </summary>
        public XUri LocalMachineUri { get { return _env.LocalMachineUri; } }

        /// <summary>
        /// Current host activity.
        /// </summary>
        public Tuplet<DateTime, string>[] ActivityMessages { get { return _env.ActivityMessages; } }

        //--- Methods ---

        /// <summary>
        /// Execute a set of scripts against the host.
        /// </summary>
        /// <remarks>
        /// This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="scripts">Scripts document.</param>
        /// <param name="path">Host path to post the scripts against.</param>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public void RunScripts(XDoc scripts, string path) {
            foreach(XDoc script in scripts["script | config"]) {
                RunScript(script, path);
            }
        }

        /// <summary>
        /// Execute a script against the host.
        /// </summary>
        /// <remarks>
        /// This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="script">Script document.</param>
        /// <param name="path">Host path to post the script against.</param>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public void RunScript(XDoc script, string path) {

            // check if a filename was provided
            string filename = script["@src"].AsText;
            if(filename != null) {

                // check if filename is relative
                if(!Path.IsPathRooted(filename) && (path != null)) {
                    filename = Path.Combine(path, filename);
                }

                // attempt to load script file
                if(!File.Exists(filename)) {
                    throw new FileNotFoundException(string.Format("script not found: {0}", filename));
                }
                script = XDocFactory.LoadFrom(filename, MimeType.XML);
            }

            // execute script
            if(script == null) {
                throw new Exception(string.Format("invalid script: {0}", script.AsText));
            }

            // convert <config> element into a <script> element
            if(script.HasName("config")) {
                XDoc doc = new XDoc("script");
                doc.Start("action").Attr("verb", "POST").Attr("path", "/host/services");
                doc.Add(script);
                doc.End();
                script = doc;
            }

            // execute script
            _host.At("execute").Post(script);
        }

        /// <summary>
        /// Block the current thread until the host shuts down.
        /// </summary>
        /// <remarks>
        /// This call does not initiate a shut down.
        /// </remarks>
        public void WaitUntilShutdown() {
            _env.WaitUntilShutdown();
        }

        /// <summary>
        /// Add a host activity.
        /// </summary>
        /// <param name="key">Activity key.</param>
        /// <param name="description">Description of activity.</param>
        public void AddActivityDescription(object key, string description) {
            _env.AddActivityDescription(key, description);
        }

        /// <summary>
        /// Remove a host activity.
        /// </summary>
        /// <param name="key">Activity key.</param>
        public void RemoveActivityDescription(object key) {
            _env.RemoveActivityDescription(key);
        }

        /// <summary>
        /// Update the host's info message.
        /// </summary>
        /// <param name="source">Message source.</param>
        /// <param name="message">Info message.</param>
        public void UpdateInfoMessage(string source, string message) {
            _env.UpdateInfoMessage(source, message);
        }

        /// <summary>
        /// Shut down and clean up the host's resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void AddListener(XUri uri, AuthenticationSchemes authenticationSheme) {
            switch(uri.Scheme.ToLowerInvariant()) {
            case Scheme.HTTP:
            case Scheme.HTTPS: {
                    HttpTransport transport = new HttpTransport(_env, uri, authenticationSheme, _dreamInParamAuthtoken);
                    transport.Startup();
                    _transports.Add(transport);
                }
                break;
            default:
                throw new ArgumentException("unsupported scheme: " + uri.Scheme);
            }
        }

        private void Dispose(bool disposing) {
            if(_disposed) {
                return;
            }
            _disposed = true;
            if(disposing) {

                // shutdown the environment
                if(_env != null) {
                    try {
                        _env.Deinitialize();
                    } catch(Exception e) {
                        _log.ErrorExceptionMethodCall(e, "Destroy");
                    }
                }

                // stop transports
                if(_transports != null) {
                    foreach(Http.HttpTransport transport in _transports) {
                        if(transport != null) {
                            try {
                                transport.Shutdown();
                            } catch(Exception e) {
                                _log.ErrorExceptionMethodCall(e, "Destroy");
                            }
                        }
                    }
                    _transports.Clear();
                }
            }
            _host = null;
            _env = null;
            _transports = null;
        }
    }
}
