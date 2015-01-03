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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using MindTouch.Security.Cryptography;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

using Autofac;

namespace MindTouch.Dream {
    using Yield = IEnumerator<IYield>;

    /// <summary>
    /// Base class for easily creating <see cref="IDreamService"/> implementations.
    /// </summary>
    [DreamServiceConfig("uri.self", "uri", "Uri for current service (provided by Host).")]
    [DreamServiceConfig("uri.log", "uri?", "Uri for logging service.")]
    [DreamServiceConfig("apikey", "string?", "Key to access protected features of the service.")]
    [DreamServiceConfig("service-license", "string?", "signed service-license")]
    public abstract class DreamService : IDreamService {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static readonly Dictionary<Type, XDoc> _blueprints = new Dictionary<Type, XDoc>();

        //--- Class Methods ---

        /// <summary>
        /// Create a service blueprint from reflection and attribute meta-data for an <see cref="IDreamService"/> implementation.
        /// </summary>
        /// <param name="type">Type of examine.</param>
        /// <returns>Xml formatted blueprint.</returns>
        public static XDoc CreateServiceBlueprint(Type type) {
            if(type == null) {
                throw new ArgumentNullException("type");
            }
            XDoc result;
            lock(_blueprints) {
                if(_blueprints.TryGetValue(type, out result)) {
                    return result;
                }
            }
            result = new XDoc("blueprint");

            // load assembly
            Dictionary<string, string> assemblySettings = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] assemblyParts = type.Assembly.FullName.Split(',');
            foreach(string parts in assemblyParts) {
                string[] assign = parts.Trim().Split(new char[] { '=' }, 2);
                if(assign.Length == 2) {
                    assemblySettings[assign[0].Trim()] = assign[1].Trim();
                }
            }
            result.Start("assembly");
            foreach(KeyValuePair<string, string> entry in assemblySettings) {
                result.Attr(entry.Key, entry.Value);
            }
            result.Value(assemblyParts[0]);
            result.End();
            result.Elem("class", type.FullName);

            // retrieve DreamService attribute on class definition
            DreamServiceAttribute serviceAttrib = (DreamServiceAttribute)Attribute.GetCustomAttribute(type, typeof(DreamServiceAttribute), false);
            result.Elem("name", serviceAttrib.Name);
            result.Elem("copyright", serviceAttrib.Copyright);
            result.Elem("info", serviceAttrib.Info);

            // retrieve DreamServiceUID attributes
            foreach(XUri sid in serviceAttrib.GetSIDAsUris()) {
                result.Elem("sid", sid);
            }

            // check if service has blueprint settings
            foreach(DreamServiceBlueprintAttribute blueprintAttrib in Attribute.GetCustomAttributes(type, typeof(DreamServiceBlueprintAttribute), true)) {
                result.InsertValueAt(blueprintAttrib.Name, blueprintAttrib.Value);
            }

            // check if service has configuration information
            DreamServiceConfigAttribute[] configAttributes = (DreamServiceConfigAttribute[])Attribute.GetCustomAttributes(type, typeof(DreamServiceConfigAttribute), true);
            if(!ArrayUtil.IsNullOrEmpty(configAttributes)) {
                result.Start("configuration");
                foreach(DreamServiceConfigAttribute configAttr in configAttributes) {
                    result.Start("entry")
                        .Elem("name", configAttr.Name)
                        .Elem("valuetype", configAttr.ValueType)
                        .Elem("description", configAttr.Description)
                    .End();
                }
                result.End();
            }

            // retrieve DreamFeature attributes on method definitions
            result.Start("features");
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach(MethodInfo method in methods) {

                // retrieve feature description
                Attribute[] featureAttributes = Attribute.GetCustomAttributes(method, typeof(DreamFeatureAttribute), false);
                if(featureAttributes.Length == 0) {
                    continue;
                }
                if(method.IsGenericMethod || method.IsGenericMethodDefinition) {
                    throw new NotSupportedException(string.Format("generic methods are not supported ({0})", method.Name));
                }

                // determine access level
                string access;
                if(method.IsPublic) {
                    access = "public";
                } else if(method.IsAssembly) {
                    access = "internal";
                } else if(method.IsPrivate || method.IsFamily) {
                    access = "private";
                } else {
                    throw new NotSupportedException(string.Format("access level is not supported ({0})", method.Name));
                }

                // retrieve feature parameter descriptions, filters, prologues, and epilogues
                Attribute[] paramAttributes = Attribute.GetCustomAttributes(method, typeof(DreamFeatureParamAttribute), false);
                Attribute[] statusAttributes = Attribute.GetCustomAttributes(method, typeof(DreamFeatureStatusAttribute), false);
                foreach(DreamFeatureAttribute featureAttrib in featureAttributes) {
                    result.Start("feature");
                    result.Elem("obsolete", featureAttrib.Obsolete);
                    result.Elem("pattern", featureAttrib.Pattern);
                    result.Elem("description", featureAttrib.Description);
                    result.Elem("hidden", featureAttrib.Hidden);
                    result.Elem("method", method.Name);

                    // add parameter descriptions (as seen on the method definition)
                    foreach(DreamFeatureParamAttribute paramAttrib in paramAttributes) {
                        result.Start("param");
                        result.Elem("name", paramAttrib.Name);
                        if(!string.IsNullOrEmpty(paramAttrib.ValueType)) {
                            result.Elem("valuetype", paramAttrib.ValueType);
                        }
                        result.Elem("description", paramAttrib.Description);
                        result.End();
                    }

                    // add status codes
                    foreach(DreamFeatureStatusAttribute paramAttrib in statusAttributes) {
                        result.Start("status");
                        result.Attr("value", (int)paramAttrib.Status);
                        result.Value(paramAttrib.Description);
                        result.End();
                    }

                    // add access level
                    result.Elem("access", access);
                    result.End();
                }
            }
            result.End();
            result.EndAll();
            lock(_blueprints) {
                _blueprints[type] = result;
            }
            return result;
        }

        //--- Fields ---
        private IDreamEnvironment _env;
        private XDoc _config = XDoc.Empty;
        private Plug _self;
        private Plug _storage;
        private Plug _owner;
        private XDoc _blueprint;
        private readonly DreamCookieJar _cookies = new DreamCookieJar();
        private string _privateAccessKey;
        private string _internalAccessKey;
        private string _apikey;
        private string _license;
        private TaskTimerFactory _timerFactory;

        //--- Constructors ---

        /// <summary>
        /// Base constructor, initializing private and internal access keys.
        /// </summary>
        protected DreamService() {

            // generate access keys
            _privateAccessKey = StringUtil.CreateAlphaNumericKey(32);
            _internalAccessKey = StringUtil.CreateAlphaNumericKey(32);
        }

        //--- Properties ---

        /// <summary>
        /// Authentication realm for service (default: dream).
        /// </summary>
        public virtual string AuthenticationRealm { get { return "dream"; } }

        /// <summary>
        /// Service configuration.
        /// </summary>
        public XDoc Config { get { return _config; } }

        /// <summary>
        /// <see cref="Plug"/> for hosting environment.
        /// </summary>
        public Plug Env { get { return _env.Self; } }

        /// <summary>
        /// Service <see cref="Plug"/>.
        /// </summary>
        public Plug Self { get { return _self; } }

        /// <summary>
        /// <see cref="Plug"/> for service that created this service.
        /// </summary>
        public Plug Owner { get { return _owner; } }

        /// <summary>
        /// <see cref="Plug"/> for Storage Service.
        /// </summary>
        public Plug Storage { get { return _storage; } }

        /// <summary>
        /// Service Identifier used to create this instance.
        /// </summary>
        public XUri SID { get { return _config["sid"].AsUri ?? _config["class"].AsUri; } }

        /// <summary>
        /// Service blueprint.
        /// </summary>
        public XDoc Blueprint { get { return _blueprint; } }

        /// <summary>
        /// Service cookie jar.
        /// </summary>
        public DreamCookieJar Cookies { get { return _cookies; } }

        /// <summary>
        /// Service license (if one exists).
        /// </summary>
        public string ServiceLicense { get { return _license; } }

        /// <summary>
        /// Prologue request stages to be executed before a Feature is executed.
        /// </summary>
        public virtual DreamFeatureStage[] Prologues { get { return null; } }

        /// <summary>
        /// Epilogue request stages to be executed after a Feature has completed.
        /// </summary>
        public virtual DreamFeatureStage[] Epilogues { get { return null; } }

        /// <summary>
        /// Exception translators given an opportunity to rewrite an exception before it is returned to the initiator of a request.
        /// </summary>
        public virtual ExceptionTranslator[] ExceptionTranslators { get { return null; } }

        /// <summary>
        /// Access Key for using internal features.
        /// </summary>
        protected string InternalAccessKey { get { return _internalAccessKey; } }

        /// <summary>
        /// Access Key for using any feature.
        /// </summary>
        protected string PrivateAccessKey { get { return _privateAccessKey; } }

        /// <summary>
        /// <see langword="True"/> if the service has been started.
        /// </summary>
        protected bool IsStarted { get { return !_config.IsEmpty; } }

        /// <summary>
        /// <see cref="TaskTimerFactory"/> associated with this service.
        /// </summary>
        /// <remarks>
        /// Governs the lifecycle of TaskTimers created during the lifecycle of the service.
        /// </remarks>
        protected TaskTimerFactory TimerFactory { get { return _timerFactory; } }

        //--- Features ---

        /// <summary>
        /// <see cref="DreamFeature"/> for retrieve the service configuration.
        /// </summary>
        /// <param name="context">Feature request context.</param>
        /// <param name="request">Request message.</param>
        /// <param name="response">Response synchronization handle.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/> to invoke the feature.</returns>
        [DreamFeature("GET:@config", "Retrieve service configuration")]
        protected virtual Yield GetConfig(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(IsStarted) {
                response.Return(DreamMessage.Ok(Config));
            } else {
                throw new DreamNotFoundException("service not started");
            }
            yield break;
        }

        /// <summary>
        /// <see cref="DreamFeature"/> for initializing the service with its configuration.
        /// </summary>
        /// <remarks>
        /// This feature is responsible for calling <see cref="Start(MindTouch.Xml.XDoc,MindTouch.Tasking.Result)"/>.
        /// </remarks>
        /// <param name="context">Feature request context.</param>
        /// <param name="request">Request message.</param>
        /// <param name="response">Response synchronization handle.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/> to invoke the feature.</returns>
        [DreamFeature("PUT:@config", "Initialize service")]
        protected virtual Yield PutConfig(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc config = request.ToDocument();
            if(config.Name != "config") {
                throw new DreamBadRequestException("bad document type");
            }
            if(IsStarted) {
                throw new DreamBadRequestException("service must be stopped first");
            }
            _timerFactory = TaskTimerFactory.Create(this);

            // configure service container
            var lifetimeScope = _env.CreateServiceLifetimeScope(this, (c, b) => PreInitializeLifetimeScope(c, b, config));

            // call container-less start (which contains shared start logic)
            yield return Coroutine.Invoke(Start, request.ToDocument(), new Result());

            // call start with container for sub-classes that want to resolve instances at service start
            yield return Coroutine.Invoke(Start, config, lifetimeScope, new Result());

            response.Return(DreamMessage.Ok(new XDoc("service-info")
                .Start("private-key")
                    .Add(DreamCookie.NewSetCookie("service-key", PrivateAccessKey, Self.Uri).AsSetCookieDocument)
                .End()
                .Start("internal-key")
                    .Add(DreamCookie.NewSetCookie("service-key", InternalAccessKey, Self.Uri).AsSetCookieDocument)
                .End()
               ));
        }

        private void PreInitializeLifetimeScope(IContainer rootContainer, ContainerBuilder lifetimeScopeBuilder, XDoc config) {
            var components = config["components"];
            lifetimeScopeBuilder.RegisterInstance(_timerFactory).ExternallyOwned();
            var registrationInspector = new RegistrationInspector(rootContainer);
            if(!components.IsEmpty) {
                _log.Debug("registering service level module");
                var module = new XDocAutofacContainerConfigurator(components, DreamContainerScope.Service);
                registrationInspector.Register(module);
                lifetimeScopeBuilder.RegisterModule(module);
            }
            InitializeLifetimeScope(registrationInspector, lifetimeScopeBuilder, config);
        }

        /// <summary>
        /// This method is called before the Start method and allows the service container to be modified before it is created.
        /// </summary>
        /// <param name="inspector">Utility class for determining what types have already been registered for use by the service scope</param>
        /// <param name="lifetimeScopeBuilder">Builder instance for registering new types</param>
        /// <param name="config">Same config document that is later passed to Start</param>
        protected virtual void InitializeLifetimeScope(IRegistrationInspector inspector, ContainerBuilder lifetimeScopeBuilder, XDoc config) { }

        /// <summary>
        /// <see cref="DreamFeature"/> for deinitializing the service.
        /// </summary>
        /// <param name="context">Feature request context.</param>
        /// <param name="request">Request message.</param>
        /// <param name="response">Response synchronization handle.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/> to invoke the feature.</returns>
        [DreamFeature("DELETE:@config", "Deinitialize service")]
        protected virtual Yield DeleteConfig(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(IsStarted) {
                _timerFactory.Dispose();
                yield return Coroutine.Invoke(Stop, new Result());
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        /// <summary>
        /// <see cref="DreamFeature"/> for retrieving the service blueprint.
        /// </summary>
        /// <param name="context">Feature request context.</param>
        /// <param name="request">Request message.</param>
        /// <param name="response">Response synchronization handle.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/> to invoke the feature.</returns>
        [DreamFeature("GET:@blueprint", "Retrieve service blueprint")]
        public virtual Yield GetServiceBlueprint(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            response.Return(DreamMessage.Ok(Blueprint));
            yield break;
        }

        /// <summary>
        /// <see cref="DreamFeature"/> for retrieving the service description.
        /// </summary>
        /// <param name="context">Feature request context.</param>
        /// <param name="request">Request message.</param>
        /// <param name="response">Response synchronization handle.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/> to invoke the feature.</returns>
        [DreamFeature("GET:@about", "Retrieve service description")]
        [DreamFeatureParam("hidden", "bool?", "show internal, private, obsolete, and hidden features, as well as service configuration information (default: false)")]
        public virtual Yield GetServiceInfo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc blueprint = Blueprint;
            bool showHidden = context.GetParam("hidden", false);
            string title = blueprint["name"].AsText ?? "Service Blueprint";
            XDoc result = new XDoc("html").Attr("xmlns", "http://www.w3.org/1999/xhtml")
                .Start("head")
                    .Elem("title", title)
                    .Start("meta").Attr("http-equiv", "content-type").Attr("content", "text/html;charset=utf-8").End()
                    .Start("meta").Attr("http-equiv", "Content-Style-Type").Attr("content", "text/css").End()
                .End();
            if(blueprint.IsEmpty) {
                result.Elem("body", "Missing service blueprint");
            } else {
                result.Start("body")
                        .Elem("h1", title)
                        .Start("p")
                            .Value(blueprint["copyright"].Contents)
                            .Value(" ")
                            .Start("a").Attr("href", blueprint["info"].Contents).Value("(more)").End()
                            .Value(" ")
                            .Start("a").Attr("href", Self.Uri.At("@blueprint")).Value("(blueprint)").End()
                        .End();

                // only show configuration information if requested
                if(showHidden) {
                    XDoc config = blueprint["configuration"];
                    if(!config.IsEmpty) {
                        result.Elem("h2", "Configuration");
                        result.Start("ul");
                        foreach(XDoc entry in config["entry"]) {
                            result.Start("li");
                            if(entry["valuetype"].Contents != string.Empty) {
                                result.Value(string.Format("{0} = {1} : {2}", entry["name"].Contents, entry["valuetype"].Contents, entry["description"].Contents));
                            } else {
                                result.Value(string.Format("{0} : {1}", entry["name"].Contents, entry["description"].Contents));
                            }
                            result.End();
                        }
                        result.End();
                    }
                }

                // sort features by signature then verb
                blueprint["features"].Sort((first, second) => {
                    string[] firstPattern = first["pattern"].Contents.Split(new[] { ':' }, 2);
                    string[] secondPattern = second["pattern"].Contents.Split(new[] { ':' }, 2);
                    int cmp = firstPattern[1].CompareInvariantIgnoreCase(secondPattern[1]);
                    if(cmp != 0) {
                        return cmp;
                    }
                    return firstPattern[0].CompareInvariant(secondPattern[0]);
                });

                // display features
                XDoc features = blueprint["features/feature"];
                if(!features.IsEmpty) {
                    result.Elem("h2", "Features");
                    List<string> modifiers = new List<string>();
                    foreach(XDoc feature in features) {
                        modifiers.Clear();

                        // add modifiers
                        string modifier = feature["access"].AsText;
                        if(modifier != null) {

                            // don't show internal/private/hidden features
                            if(!showHidden) {
                                if(modifier != "public") {
                                    continue;
                                }
                                if((feature["hidden"].AsText ?? "false") != "false") {
                                    continue;
                                }
                            }
                            modifiers.Add(modifier);
                        }
                        modifier = feature["obsolete"].AsText;
                        if(modifier != null) {

                            // don't show obsolete features
                            if(!showHidden) {
                                continue;
                            }
                            modifiers.Add("OBSOLETE => " + modifier);
                        }
                        if(modifiers.Count > 0) {
                            modifier = " (" + string.Join(", ", modifiers.ToArray()) + ")";
                        } else {
                            modifier = string.Empty;
                        }

                        // check if feature has GET verb and no path parameters
                        string pattern = feature["pattern"].Contents;
                        if(pattern.StartsWithInvariantIgnoreCase(Verb.GET + ":") && (pattern.IndexOfAny(new[] { '{', '*', '?' }) == -1)) {
                            string[] parts = pattern.Split(new[] { ':' }, 2);
                            result.Start("h3")
                                .Start("a").Attr("href", context.AsPublicUri(Self.Uri.AtPath(parts[1])))
                                    .Value(feature["pattern"].Contents)
                                .End()
                                .Value(modifier)
                            .End();
                        } else {
                            result.Elem("h3", feature["pattern"].Contents + modifier);
                        }
                        result.Start("p")
                                .Value(feature["description"].Contents)
                                .Value(" ")
                                .Start("a").Attr("href", feature["info"].Contents).Value("(more)").End();
                        XDoc paramlist = feature["param"];
                        if(!paramlist.IsEmpty) {
                            result.Start("ul");
                            foreach(XDoc param in paramlist) {
                                result.Start("li");
                                if(param["valuetype"].Contents != string.Empty) {
                                    result.Value(string.Format("{0} = {1} : {2}", param["name"].Contents, param["valuetype"].Contents, param["description"].Contents));
                                } else {
                                    result.Value(string.Format("{0} : {1}", param["name"].Contents, param["description"].Contents));
                                }
                                result.End();
                            }
                            result.End();
                        }
                        result.End();
                    }
                }
            }
            response.Return(DreamMessage.Ok(MimeType.HTML, result.ToString()));
            yield break;
        }

        /// <summary>
        /// <see cref="DreamFeature"/> for deleting the service (separate from deinitializing it.)
        /// </summary>
        /// <param name="context">Feature request context.</param>
        /// <param name="request">Request message.</param>
        /// <param name="response">Response synchronization handle.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/> to invoke the feature.</returns>
        [DreamFeature("DELETE:", "Stop service")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Insufficient permission")]
        protected virtual Yield DeleteService(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            yield return Env.At("stop").Post(new XDoc("service").Elem("uri", Self), new Result<DreamMessage>(TimeSpan.MaxValue)).CatchAndLog(_log);
            response.Return(DreamMessage.Ok());
        }

        [DreamFeature("POST:@grants", "Adds a grant to this service for accessing another service.")]
        internal Yield PostGrant(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            lock(Cookies) {
                Cookies.Update(DreamCookie.ParseAllSetCookieNodes(request.ToDocument()), null);
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        //--- Methods ---

        /// <summary>
        /// Initialize a service instance.
        /// </summary>
        /// <param name="env">Host environment.</param>
        /// <param name="blueprint">Service blueprint.</param>
        public virtual void Initialize(IDreamEnvironment env, XDoc blueprint) {
            if(env == null) {
                throw new ArgumentNullException("env");
            }
            if(blueprint == null) {
                throw new ArgumentNullException("blueprint");
            }
            _env = env;
            _blueprint = blueprint;
        }

        /// <summary>
        /// Perform startup configuration of a service instance.
        /// </summary>
        /// <remarks>
        /// Should not be manually invoked and should only be overridden if <see cref="Start(MindTouch.Xml.XDoc,MindTouch.Tasking.Result)"/> isn't already overriden.
        /// </remarks>
        /// <param name="config">Service configuration.</param>
        /// <param name="serviceLifetimeScope">Service level IoC container</param>
        /// <param name="result">Synchronization handle for coroutine invocation.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/> execution environment.</returns>
        protected virtual Yield Start(XDoc config, ILifetimeScope serviceLifetimeScope, Result result) {
            result.Return();
            yield break;
        }

        /// <summary>
        /// Perform startup configuration of a service instance.
        /// </summary>
        /// <remarks>
        /// Should not be manually invoked and should only be overridden if <see cref="Start(MindTouch.Xml.XDoc,Autofac.ILifetimeScope,MindTouch.Tasking.Result)"/> isn't already overriden.
        /// </remarks>
        /// <param name="config">Service configuration.</param>
        /// <param name="result">Synchronization handle for coroutine invocation.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/> execution environment.</returns>
        protected virtual Yield Start(XDoc config, Result result) {
            Result<DreamMessage> res;

            // store configuration and uri
            _config = config;
            _self = Plug.New(config["uri.self"].AsUri);
            if(_self == null) {
                throw new ArgumentNullException("config", "Missing element'uri.self'");
            }
            _owner = Plug.New(config["uri.owner"].AsUri);

            // check for service access keys
            var internalAccessKey = config["internal-service-key"].AsText;
            if(!string.IsNullOrEmpty(internalAccessKey)) {
                _internalAccessKey = internalAccessKey;
            }
            var privateAccessKey = config["private-service-key"].AsText;
            if(!string.IsNullOrEmpty(privateAccessKey)) {
                _privateAccessKey = privateAccessKey;
            }

            // check for api-key settings
            _apikey = config["apikey"].AsText;

            // process 'set-cookie' entries
            var setCookies = DreamCookie.ParseAllSetCookieNodes(config["set-cookie"]);
            if(setCookies.Count > 0) {
                Cookies.Update(setCookies, null);
            }

            // grant private access key to self, host, and owner
            var privateAcccessCookie = DreamCookie.NewSetCookie("service-key", PrivateAccessKey, Self.Uri);
            Cookies.Update(privateAcccessCookie, null);
            yield return Env.At("@grants").Post(DreamMessage.Ok(privateAcccessCookie.AsSetCookieDocument), new Result<DreamMessage>(TimeSpan.MaxValue));
            if(Owner != null) {
                yield return res = Owner.At("@grants").Post(DreamMessage.Ok(privateAcccessCookie.AsSetCookieDocument), new Result<DreamMessage>(TimeSpan.MaxValue));
                if(!res.Value.IsSuccessful) {
                    throw new ArgumentException("unexpected failure setting grants on owner service");
                }
            }

            // check if this service requires a service-license to work
            if(this is IDreamServiceLicense) {
                string service_license = config["service-license"].AsText;
                if(string.IsNullOrEmpty(service_license)) {
                    throw new DreamAbortException(DreamMessage.LicenseRequired("service-license missing"));
                }

                // extract public RSA key for validation
                RSACryptoServiceProvider public_key = RSAUtil.ProviderFrom(GetType().Assembly);
                if(public_key == null) {
                    throw new DreamAbortException(DreamMessage.InternalError("service assembly invalid"));
                }

                // validate the service-license
                _license = null;
                Dictionary<string, string> values;
                try {

                    // parse service-license
                    values = HttpUtil.ParseNameValuePairs(service_license);
                    if(!Encoding.UTF8.GetBytes(service_license.Substring(0, service_license.LastIndexOf(','))).VerifySignature(values["dsig"], public_key)) {
                        throw new DreamAbortException(DreamMessage.InternalError("invalid service-license (1)"));
                    }

                    // check if the SID matches
                    string sid;
                    if(!values.TryGetValue("sid", out sid) || !SID.HasPrefix(XUri.TryParse(sid), true)) {
                        throw new DreamAbortException(DreamMessage.InternalError("invalid service-license (2)"));
                    }
                    _license = service_license;
                } catch(Exception e) {

                    // unexpected error, blame it on the license
                    if(e is DreamAbortException) {
                        throw;
                    }
                    throw new DreamAbortException(DreamMessage.InternalError("corrupt service-license (1)"));
                }

                // validate expiration date
                string expirationtext;
                if(values.TryGetValue("expire", out expirationtext)) {
                    try {
                        DateTime expiration = DateTime.Parse(expirationtext);
                        if(expiration < GlobalClock.UtcNow) {
                            _license = null;
                        }
                    } catch(Exception e) {
                        _license = null;

                        // unexpected error, blame it on the license
                        if(e is DreamAbortException) {
                            throw;
                        }
                        throw new DreamAbortException(DreamMessage.InternalError("corrupt service-license (2)"));
                    }
                }

                // check if a license was assigned
                if(_license == null) {
                    throw new DreamAbortException(DreamMessage.LicenseRequired("service-license has expired"));
                }
            } else {
                config["service-license"].RemoveAll();
            }

            // create built-in services
            _storage = Plug.New(config["uri.storage"].AsUri);

            // done
            _log.Debug("Start");
            result.Return();
        }

        /// <summary>
        /// Perform shutdown cleanup of a service instance.
        /// </summary>
        /// <remarks>
        /// Should not be manually invoked.
        /// </remarks>
        /// <param name="result">Synchronization handle for coroutine invocation.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/> execution environment.</returns>
        protected virtual Yield Stop(Result result) {

            // ungrant owner and parent, otherwise they end up with thousands of grants
            DreamCookie cookie = DreamCookie.NewSetCookie("service-key", string.Empty, Self.Uri, GlobalClock.UtcNow);

            // ignore if these operations fail since we're shutting down anyway
            yield return Env.At("@grants").Post(DreamMessage.Ok(cookie.AsSetCookieDocument), new Result<DreamMessage>(TimeSpan.MaxValue)).CatchAndLog(_log);
            if(Owner != null) {
                yield return Owner.At("@grants").Post(DreamMessage.Ok(cookie.AsSetCookieDocument), new Result<DreamMessage>(TimeSpan.MaxValue)).CatchAndLog(_log);
            }
            _log.Debug("Stop");

            // reset fields
            _self = null;
            _owner = null;
            _storage = null;
            _config = XDoc.Empty;
            _apikey = null;
            _cookies.Clear();
            _license = null;
            _env.DisposeServiceContainer(this);

            // we're done
            result.Return();
        }

        /// <summary>
        /// Create a service as a child of the current service.
        /// </summary>
        /// <param name="path">Relative path to locate new service at.</param>
        /// <param name="sid">Service Identifier.</param>
        /// <param name="config">Service configuration.</param>
        /// <param name="result">The result instance to be returned by this methods.</param>
        /// <returns>Synchronization handle for this method's invocation.</returns>
        protected Result<Plug> CreateService(string path, string sid, XDoc config, Result<Plug> result) {
            return Coroutine.Invoke(CreateService_Helper, path, sid, config ?? new XDoc("config"), result);
        }

        private Yield CreateService_Helper(string path, string sid, XDoc config, Result<Plug> result) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }
            if(sid == null) {
                throw new ArgumentNullException("sid");
            }
            if(config == null) {
                throw new ArgumentNullException("config");
            }

            // check if we have a licensekey for the requested SID
            string serviceLicense = TryGetServiceLicense(XUri.TryParse(sid) ?? XUri.Localhost);

            // add parameters to config document
            Plug plug = Self.AtPath(path);
            config.Root
                .Elem("path", plug.Uri.Path)
                .Elem("sid", sid)

                // add 'owner' and make sure we keep it in 'local://' format
                .Elem("uri.owner", Self.Uri.ToString())

                // add 'internal' access key
                .Add(DreamCookie.NewSetCookie("service-key", InternalAccessKey, Self.Uri).AsInternalSetCookieDocument)

                // add optional 'service-license' token
                .Elem("service-license", serviceLicense);

            // inject parent apikey if apikey not defined
            if(config["apikey"].IsEmpty) {
                config.Root.Elem("apikey", _apikey);
            }

            // post to host to create service
            Result<DreamMessage> res;
            yield return res = Env.At("services").Post(config, new Result<DreamMessage>(TimeSpan.MaxValue));
            if(!res.Value.IsSuccessful) {
                if(res.Value.HasDocument) {
                    string message = res.Value.ToDocument()[".//message"].AsText.IfNullOrEmpty("unknown error");
                    throw new DreamResponseException(res.Value, string.Format("unable to initialize service ({0})", message));
                }
                throw new DreamResponseException(res.Value, string.Format("unable to initialize service ({0})", res.Value.ToText()));
            }
            result.Return(plug);
            yield break;
        }

        /// <summary>
        /// No-op hook for retrieving a service license string from its Service Identifier uri.
        /// </summary>
        /// <param name="sid">Service Identifier.</param>
        /// <returns>Service license.</returns>
        protected virtual string TryGetServiceLicense(XUri sid) {
            return null;
        }

        /// <summary>
        /// Determine the access appropriate for an incoming request.
        /// </summary>
        /// <param name="context">Request context.</param>
        /// <param name="request">Request message.</param>
        /// <returns>Access level for request.</returns>
        public virtual DreamAccess DetermineAccess(DreamContext context, DreamMessage request) {
            DreamMessage message = request;

            // check if request has a service or api key
            string key = context.Uri.GetParam("apikey", message.Headers[DreamHeaders.DREAM_APIKEY]);
            if(key == null) {
                DreamCookie cookie = DreamCookie.GetCookie(message.Cookies, "service-key");
                if(cookie != null) {
                    key = cookie.Value;
                }
            }
            return DetermineAccess(context, key);
        }

        private Yield InServiceInvokeHandler(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            throw new InvalidOperationException("this feature should never be invoked");
        }

        /// <summary>
        /// Invoke an action in the context of a service feature.
        /// </summary>
        /// <remarks>
        /// Assumes that there exists a current <see cref="DreamContext"/> that belongs to a request to another feature of this service.
        /// </remarks>
        /// <param name="verb">Http verb.</param>
        /// <param name="path">Feature path.</param>
        /// <param name="handler">Action to perform in this context.</param>
        /// <returns>Exception thrown by handler or null.</returns>
        public Exception InvokeInServiceContext(string verb, string path, Action handler) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            if(string.IsNullOrEmpty(verb)) {
                throw new ArgumentNullException("verb");
            }
            if(path == null) {
                throw new ArgumentNullException("path");
            }

            // create new new environment for execution
            XUri uri = Self.AtPath(path);
            DreamContext current = DreamContext.Current;
            Exception e = TaskEnv.ExecuteNew(() => {
                DreamFeatureStage[] stages = new[] {
                    new DreamFeatureStage("InServiceInvokeHandler", InServiceInvokeHandler, DreamAccess.Private)
                };

                // BUGBUGBUG (steveb): when invoking a remote function this way, we're are not running the proloques and epilogues, which leads to errors;
                //  also dream-access attributes are being ignored (i.e. 'public' vs. 'private')
                DreamMessage message = DreamUtil.AppendHeadersToInternallyForwardedMessage(current.Request, DreamMessage.Ok());
                var context = current.CreateContext(verb, uri, new DreamFeature(this, Self, 0, stages, verb, path, Enumerable.Empty<DreamFeatureParamAttribute>()), message);
                context.AttachToCurrentTaskEnv();

                // pass along host and public-uri information
                handler();
            }, TimerFactory);
            return e;
        }

        /// <summary>
        /// Provides a hook for overriding what access level the current request should be granted.
        /// </summary>
        /// <param name="context">Request context.</param>
        /// <param name="key">Authorization key of request.</param>
        /// <returns>Access level granted to the request.</returns>
        protected virtual DreamAccess DetermineAccess(DreamContext context, string key) {
            if(_env.IsDebugEnv) {
                return DreamAccess.Private;
            }
            if(!string.IsNullOrEmpty(key) && (key.EqualsInvariant(_apikey))) {
                return DreamAccess.Private;
            }
            if(key == InternalAccessKey) {
                return DreamAccess.Internal;
            }
            if(key == PrivateAccessKey) {
                return DreamAccess.Private;
            }
            return DreamAccess.Public;
        }

        /// <summary>
        /// Provides a hook for overriding default authentication of incoming user credentials.
        /// </summary>
        /// <remarks>
        /// Overriding methods should throw <see cref="DreamAbortException"/> if the user cannot be authenticated.
        /// </remarks>
        /// <param name="context">Request context.</param>
        /// <param name="message">Request message.</param>
        /// <param name="username">Request user.</param>
        /// <param name="password">User password.</param>
        protected void Authenticate(DreamContext context, DreamMessage message, out string username, out string password) {
            if(!HttpUtil.GetAuthentication(context.Uri, message.Headers, out username, out password)) {
                throw new DreamAbortException(DreamMessage.AccessDenied(AuthenticationRealm, "authentication failed"));
            }
        }
    }
}
