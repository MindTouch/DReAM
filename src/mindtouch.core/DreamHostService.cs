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

// ReSharper disable SuggestUseVarKeywordEverywhere
// ReSharper disable SuggestUseVarKeywordEvident

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Autofac;
using Autofac.Builder;

using MindTouch.Collections;
using MindTouch.Dream.IO;
using MindTouch.Tasking;
using MindTouch.Threading;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream {
    using Yield = IEnumerator<IYield>;
    using DreamFeatureCoroutineHandler = CoroutineHandler<DreamContext, DreamMessage, Result<DreamMessage>>;

    [DreamService("MindTouch Dream Host", "Copyright (c) 2006-2014 MindTouch, Inc.",
        SID = new[] { 
            "sid://mindtouch.com/2007/03/dream/host",
            "http://services.mindtouch.com/dream/stable/2007/03/host" 
        }
    )]
    [DreamServiceConfig("storage-dir", "string?", "Rooted path to the folder for service storage.")]
    [DreamServiceConfig("host-path", "string?", "Path to host service.")]
    [DreamServiceConfig("uri.public", "string?", "Public server URI with path")]
    [DreamServiceConfig("connect-limit", "int?", "Max. number of simultaneous connections")]
    [DreamServiceConfig("guid", "string?", "Globally unique host identity string")]
    internal class DreamHostService : DreamService, IDreamEnvironment, IPlugEndpoint {

        //--- Constants ---
        public const string SOURCE_HOST = "host";
        public static readonly TimeSpan MAX_REQUEST_TIME = TimeSpan.FromSeconds(2 * 60 * 60);

        //--- Types ---
        internal class ServiceEntry {

            //--- Fields ---
            internal readonly IDreamService Service;
            internal readonly XUri Uri;
            internal readonly XUri Owner;
            internal readonly XUri SID;
            internal readonly XDoc Blueprint;

            //--- Constructors ---
            internal ServiceEntry(IDreamService service, XUri uri, XUri owner, XUri sid, XDoc blueprint) {
                this.Service = service;
                this.Uri = uri;
                this.Owner = owner;
                this.SID = sid;
                this.Blueprint = blueprint;
            }
        }

        private sealed class DreamActivityDescription : IDreamActivityDescription {

            //--- Fields ---
            private readonly DateTime _created = DateTime.UtcNow;
            private string _description;
            private readonly Dictionary<IDreamActivityDescription, DreamActivityDescription> _activities;

            //--- Constructors ---
            internal DreamActivityDescription(Dictionary<IDreamActivityDescription, DreamActivityDescription> activities) {
                _activities = activities;
                lock(_activities) {
                    _activities[this] = this;
                }
            }

            //--- Properties ---
            public DateTime Created { get { return _created; } }

            public string Description {
                get {
                    return _description;
                }
                set {
                    _description = value;
                }
            }

            //--- Methods ----
            public void Dispose() {
                lock(_activities) {
                    _activities.Remove(this);
                }
            }
        }

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static readonly Dictionary<Type, ILookup<string, MethodInfo>> _methodInfoCache = new Dictionary<Type, ILookup<string, MethodInfo>>();

        //--- Class Constructors ---
        static DreamHostService() {
            Environment.SetEnvironmentVariable("DreamHost", Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName));
        }

        //--- Class Methods ---
        private static string EncodedServicePath(XUri uri) {
            string result = uri.Path;
            if(result.Length == 0) {
                result = "/";
            }
            result = System.Xml.XmlConvert.EncodeLocalName(result);
            return result;
        }

        private static Yield PrologueDreamIn(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string root = context.GetParam(DreamInParam.ROOT, "doc");

            // check if we need to change the message format
            string format = context.GetParam(DreamInParam.FORMAT, null);
            if(format != null) {
                switch(format.ToLowerInvariant()) {
                case "json":
                case "jsonp":
                    request = DreamMessage.NotImplemented("json(p) input format not supported");
                    break;
                case "php":
                    request = DreamMessage.NotImplemented("php input format not supported");
                    break;
                case "xpost":
                    if(request.ContentType.Match(MimeType.FORM_URLENCODED)) {
                        XDoc doc = XPostUtil.FromXPathValuePairs(XUri.ParseParamsAsPairs(request.ToText()), root);
                        request = new DreamMessage(request.Status, request.Headers, doc);
                    }
                    break;
                case "versit":
                    if(!request.ContentType.Match(MimeType.XML)) {
                        XDoc doc = VersitUtil.FromVersit(request.ToTextReader().ReadToEnd(), root);
                        request = new DreamMessage(request.Status, request.Headers, doc);
                    }
                    break;
                case "html":
                    if(!request.ContentType.Match(MimeType.XML)) {
                        XDoc doc = XDocFactory.From(request.ToTextReader(), MimeType.HTML);
                        request = new DreamMessage(request.Status, request.Headers, doc);
                    }
                    break;
                case "xspan": {
                        XDoc doc = XSpanUtil.FromXSpan(request.ToDocument());
                        request = new DreamMessage(request.Status, request.Headers, doc);
                    }
                    break;
                case "xhtml":
                    if(request.ContentType.Match(MimeType.XHTML)) {
                        request.Headers.ContentType = MimeType.XML;
                    }
                    break;
                case "xml":
                    break;
                default:
                    request = DreamMessage.BadRequest(string.Format("{0} input format not supported", format));
                    break;
                }
            } else if("base64".EqualsInvariantIgnoreCase(request.Headers.ContentEncoding)) {
                byte[] bytes = Convert.FromBase64String(request.ToText());
                request = new DreamMessage(request.Status, request.Headers, request.ContentType, bytes);
                request.Headers.ContentEncoding = null;
            }
            response.Return(request);
            yield break;
        }

        private static Yield EpilogueDreamOut(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // NOTE (steveb): standard epilogue is applied to all responses, not just successful ones.

            if(request.IsSuccessful) {

                // select result fragment (if appropriate)
                string xpath = context.GetParam(DreamOutParam.SELECT, string.Empty);
                if(request.HasDocument && (xpath != string.Empty)) {
                    XDoc doc = request.ToDocument()[xpath];
                    request = new DreamMessage(request.Status, request.Headers, doc);
                }
            }

            // change response format
            IDreamResponseFormatter formatter = null;
            string format = context.GetParam(DreamOutParam.FORMAT, null);
            if(format != null) {
                switch(format.ToLowerInvariant()) {
                case "json": {
                        string callback = context.GetParam(DreamOutParam.CALLBACK, "");
                        string prefix = context.GetParam(DreamOutParam.PREFIX, "");
                        string postfix = context.GetParam(DreamOutParam.POSTFIX, "");
                        formatter = new DreamResponseJsonFormatter(callback, prefix, postfix);
                    }
                    break;
                case "jsonp":
                    formatter = new DreamResponseJsonpFormatter(context.GetParam(DreamOutParam.PREFIX, ""));
                    break;
                case "xhtml":
                    formatter = new DreamResponseXHtmlFormatter();
                    break;
                case "xspan":
                    formatter = new DreamResponseXSpanFormatter();
                    break;
                case "php":
                    formatter = new DreamResponsePhpFormatter();
                    break;
                case "versit":
                    formatter = new DreamResponseVersitFormatter();
                    break;
                case "xml":
                    break;
                default:
                    request = DreamMessage.BadRequest(string.Format("{0} output format not supported", format));
                    break;
                }
            }

            // apply formatter to all XML messages
            if((formatter != null) && request.HasDocument && request.ContentType.IsXml) {
                Stream stream = formatter.Format(request.ToDocument());
                request = new DreamMessage(request.Status, request.Headers, formatter.GetContentType(request.ToDocument()), stream.Length, stream);
            }

            // override content-type
            string type = context.GetParam(DreamOutParam.TYPE, null);
            if(type != null) {
                request.Headers[DreamHeaders.CONTENT_TYPE] = type;
            }

            // set content-disposition
            string saveas = context.GetParam(DreamOutParam.SAVEAS, null);
            if(saveas != null) {
                request.Headers[DreamHeaders.CONTENT_DISPOSITION] = "attachment; filename=\"" + saveas + "\";";
            }

            // increse hit counter
            context.Feature.IncreaseHitCounter();
            response.Return(request);
            yield break;
        }

        private static void PopulateActivities(XDoc doc, XUri self, DateTime now, IDreamActivityDescription[] activities) {
            doc.Attr("count", activities.Length).Attr("href", self.At("status", "activities"));
            foreach(var description in activities) {
                doc.Start("description").Attr("created", description.Created).Attr("age", (now - description.Created).TotalSeconds).Value(description.Description).End();
            }
        }

        private static ILookup<string, MethodInfo> GetMethodInfos(Type type) {
            ILookup<string, MethodInfo> result;
            lock(_methodInfoCache) {
                if(_methodInfoCache.TryGetValue(type, out result)) {
                    return result;
                }
            }
            result = (from m in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod) where (m.GetCustomAttributes(typeof(DreamFeatureAttribute), false).Length > 0) select m).ToLookup(methodInfo => methodInfo.Name);
            lock(_methodInfoCache) {
                _methodInfoCache[type] = result;
            }
            return result;
        } 

        //--- Fields ---
        private readonly IContainer _container;
        private readonly ILifetimeScope _hostLifetimeScope;
        private readonly Dictionary<IDreamService, ILifetimeScope> _serviceLifetimeScopes = new Dictionary<IDreamService, ILifetimeScope>();
        private readonly XUriMap<XUri> _aliases = new XUriMap<XUri>();
        private readonly Dictionary<IDreamActivityDescription, DreamActivityDescription> _activities = new Dictionary<IDreamActivityDescription, DreamActivityDescription>();
        private readonly Dictionary<string, Tuplet<int, string>> _infos = new Dictionary<string, Tuplet<int, string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Type> _registeredTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
        private readonly Dictionary<string, ServiceEntry> _services = new Dictionary<string, ServiceEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<XUri>> _requests = new Dictionary<string, List<XUri>>();
        private readonly DateTime _created = DateTime.UtcNow;
        private IServiceActivator _serviceActivator;
        private DreamFeatureStage[] _defaultPrologues;
        private DreamFeatureStage[] _defaultEpilogues;
        private Guid _id;
        private bool _running;
        private DreamFeatureDirectory _features = new DreamFeatureDirectory();
        private Dictionary<string, XDoc> _blueprints;
        private string _storageType;
        private XDoc _storageConfig;
        private string _storagePath;
        private Plug _storage;
        private ManualResetEvent _shutdown;
        private XUri _localMachineUri;
        private XUri _publicUri;
        private int _connectionCounter;
        private int _connectionLimit;
        private long _requestCounter;
        private int _reentrancyLimit;
        private string _rootRedirect;
        private string _debugMode;
        private bool _memorizeAliases;
        private volatile ProcessingQueue<Action<Action>> _requestQueue;

        //--- Constructors ---
        public DreamHostService() : this(null) { }

        public DreamHostService(IContainer container) {
            _container = (container ?? new ContainerBuilder().Build(ContainerBuildOptions.Default));
            _hostLifetimeScope = _container.BeginLifetimeScope(DreamContainerScope.Host);
        }

        //--- Properties ---
        public Guid GlobalId { get { return _id; } }
        public bool IsRunning { get { return _running; } }
        public bool IsDebugEnv {
            get {
                switch(_debugMode) {
                case "on":
                case "true":
                    return true;
                case "debugger-only":
                    return Debugger.IsAttached;
                }
                return false;
            }
        }

        public XUri LocalMachineUri { get { return _localMachineUri; } }

        public IDreamActivityDescription[] ActivityMessages {
            get {
                lock(_activities) {
                    return _activities.Values.ToArray();
                }
            }
        }

        //--- Features ---
        [DreamFeature("GET:version", "Retrieve version information about Dream assemblies.")]
        public Yield GetVersions(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc result = new XDoc("versions");
            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                AssemblyName name = assembly.GetName();
                result.Start("assembly")
                    .Attr("name", name.Name)
                    .Elem("AssemblyVersion", name.Version.ToString())
                    .Elem("BuildDate", assembly.GetBuildDate());
                var svnRevision = assembly.GetAttribute<SvnRevisionAttribute>();
                if(svnRevision != null) {
                    result.Elem("SvnRevision", svnRevision.Revision);
                }
                var svnBranch = assembly.GetAttribute<SvnBranchAttribute>();
                if(svnBranch != null) {
                    result.Elem("SvnBranch", svnBranch.Branch);
                }
                var gitRevision = assembly.GetAttribute<GitRevisionAttribute>();
                if(gitRevision != null) {
                    result.Elem("GitRevision", gitRevision.Revision);
                }
                var gitBranch = assembly.GetAttribute<GitBranchAttribute>();
                if(gitBranch != null) {
                    result.Elem("GitBranch", gitBranch.Branch);
                }
                var gitUri = assembly.GetAttribute<GitUriAttribute>();
                if(gitUri != null) {
                    result.Elem("GitUri", gitUri.Uri);
                }
                result.End();
            }
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:blueprints", "Retrieve list of all blueprints")]
        public Yield GetAllBlueprints(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc result = new XDoc("list");
            Dictionary<string, XDoc> blueprints = _blueprints;
            if(blueprints != null) {
                lock(blueprints) {
                    foreach(XDoc entry in blueprints.Values) {
                        result.Add(entry);
                    }
                }
            }
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:blueprints/{sid-or-typename}", "Retrieve a blueprint")]
        public Yield GetBlueprints(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string sid = context.GetSuffix(0, UriPathFormat.Original);
            XDoc result = null;
            Dictionary<string, XDoc> blueprints = _blueprints;
            if(blueprints != null) {
                lock(blueprints) {
                    blueprints.TryGetValue(sid, out result);
                }
            }
            if(result != null) {
                response.Return(DreamMessage.Ok(result));
            } else {
                response.Return(DreamMessage.NotFound(string.Format("could not find blueprint for {0}", sid)));
            }
            yield break;
        }

        [DreamFeature("GET:resources/{resource}", "Retrieve embedded resource")]
        public Yield GetErrorXsl(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string resource = context.GetParam("resource");
            Plug plug = Plug.New(string.Format("resource://mindtouch.core/MindTouch.Dream.resources.host.{0}", resource)).With(DreamOutParam.TYPE, MimeType.FromFileExtension(resource).FullType);
            yield return context.Relay(plug, request, response);
        }

        [DreamFeature("POST:blueprints", "Add a service blueprint. (requires API key)")]
        internal Yield RegisterServiceType(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // process request
            XDoc blueprint = request.ToDocument();

            // validate assembly
            string typeName = blueprint["class"].Contents;
            string assemblyName = blueprint["assembly"].Contents;
            if(string.IsNullOrEmpty(assemblyName)) {
                response.Return(DreamMessage.BadRequest("missing assembly name"));
                yield break;
            }
            if(string.IsNullOrEmpty(typeName)) {
                response.Return(DreamMessage.BadRequest("missing class name"));
                yield break;
            }
            Assembly assembly = Assembly.Load(assemblyName);
            if(assembly == null) {
                _log.WarnMethodCall("register: missing assembly", blueprint);
                response.Return(DreamMessage.BadRequest("assembly not found"));
                yield break;
            }

            // validate type
            Type type = assembly.GetType(typeName, false);
            if(type == null) {
                _log.WarnMethodCall("register: class not found", blueprint);
                response.Return(DreamMessage.NotFound("type not found"));
                yield break;
            }
            RegisterBlueprint(blueprint, type);
            response.Return(DreamMessage.Ok());
        }

        [DreamFeature("DELETE:blueprints/{sid-or-typename}", "Remove a service blueprint. (requires API key)")]
        internal Yield DeleteServiceType(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // process request
            string sid = context.GetParam("sid-or-typename");
            _log.DebugMethodCall("unregister", sid);
            Dictionary<string, XDoc> blueprints = _blueprints;
            if(blueprints != null) {
                lock(blueprints) {
                    blueprints.Remove(sid);
                }
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("POST:load", "Load an assembly and register all contained services. (requires API key)")]
        [DreamFeatureParam("name", "string", "Name of assembly to load")]
        internal Yield LoadAssembly(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // validate assembly
            string assemblyName = context.GetParam("name");
            Assembly assembly = Assembly.Load(assemblyName);
            if(assembly == null) {
                _log.WarnMethodCall("register: missing assembly", assemblyName);
                response.Return(DreamMessage.BadRequest("assembly not found"));
                yield break;
            }

            // enumerate all types in assembly
            Type[] types;
            try {
                types = assembly.GetTypes();
            } catch(ReflectionTypeLoadException e) {
                _log.WarnFormat("register: unable to load assembly '{0}':", assemblyName);
                foreach(Exception loaderException in e.LoaderExceptions) {
                    _log.WarnFormat("Loader Exception: '{0}':", loaderException.Message);
                }
                throw;
            }
            foreach(Type t in types) {
                object[] dsa = t.GetCustomAttributes(typeof(DreamServiceAttribute), false);
                if(dsa.Length > 0) {
                    RegisterBlueprint(null, t);
                }
            }
            response.Return(DreamMessage.Ok());
        }

        [DreamFeature("GET:services", "Retrieve list of running services. (requires API key)")]
        internal Yield GetServices(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc result = new XDoc("services");
            lock(_services) {
                result.Attr("count", _services.Count);
                foreach(KeyValuePair<string, ServiceEntry> entry in _services) {
                    result.Start("service");
                    result.Elem("path", entry.Key);
                    result.Elem("uri", entry.Value.Uri);
                    if(entry.Value.Owner != null) {
                        result.Elem("uri.owner", entry.Value.Owner);
                    }
                    if(entry.Value.SID != null) {
                        result.Elem("sid", entry.Value.SID);
                    }
                    result.Elem("type", entry.Value.Service.GetType().FullName);
                    result.End();
                }
            }
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("POST:services", "Start a service instance. (requires API key)")]
        internal Yield PostServices(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // process request
            XDoc blueprint = null;
            Type type;
            XDoc config = request.ToDocument();
            string path = config["path"].AsText;
            XUri sid = config["sid"].AsUri;
            string typeName = config["class"].AsText;
            _log.InfoMethodCall("start", path, (sid != null) ? (object)sid : (object)(typeName ?? "<unknown>"));

            if(path == null) {
                response.Return(DreamMessage.BadRequest("path missing"));
                yield break;
            }

            // special case during boot-strapping
            if(sid == null) {

                // let's first try just to load the type
                Debug.Assert(typeName != null, "typeName != null");
                type = Type.GetType(typeName, false);
                if(type == null) {

                    // let's try to find the type amongst the loaded assemblies
                    foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                        type = assembly.GetType(typeName, false);
                        if(type != null) {
                            break;
                        }
                    }
                    if(type == null) {
                        response.Return(DreamMessage.BadRequest("type not found"));
                        yield break;
                    }
                }
                blueprint = CreateServiceBlueprint(type);
                RegisterBlueprint(null, type);
            } else {

                // validate blueprints
                Dictionary<string, XDoc> blueprints = _blueprints;
                if(blueprints != null) {
                    lock(blueprints) {
                        blueprints.TryGetValue(XUri.EncodeSegment(sid.ToString()), out blueprint);
                    }
                }

                // if blueprint wasn't found, try finding it by type name
                if((blueprint == null) && (blueprints != null) && (blueprints.Count == 0)) {
                    type = CoreUtil.FindBuiltInTypeBySID(sid);
                    if(type != null) {
                        blueprint = CreateServiceBlueprint(type);
                    }
                }

                // check if blueprint was found
                if(blueprint == null) {
                    _log.WarnMethodCall("start: blueprint not found", sid);
                    response.Return(DreamMessage.BadRequest("blueprint not found"));
                    yield break;
                }

                // find type
                Assembly assembly = Assembly.Load(blueprint["assembly"].AsText);
                type = assembly.GetType(blueprint["class"].AsText);
            }

            // instantiate service
            IDreamService service = _serviceActivator.Create(config, type);

            // start service
            XDoc doc = null;
            yield return Coroutine.Invoke(StartService, service, blueprint, path, config, new Result<XDoc>()).Set(v => doc = v);
            response.Return(DreamMessage.Ok(doc));
        }

        [DreamFeature("POST:stop", "Stop a service instance. (requires API key)")]
        internal Yield PostStopService(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // process request
            XDoc doc = request.ToDocument();
            StopService(doc["uri"].AsUri);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("POST:execute", "Execute an XML script. (requires API key)")]
        internal Yield ExecuteScript(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // process request
            XDoc script = request.ToDocument();
            XDoc reply = CoreUtil.ExecuteScript(Env, request.Headers, script);
            response.Return(DreamMessage.Ok(reply));
            yield break;
        }

        [DreamFeature("*:test", "Test communication with Host service.")]
        [DreamFeatureParam("status", "int?", "Response status code to reply with (default = 200)")]
        [DreamFeatureParam("cookie", "string?", "Include Set-Cookie in response")]
        public Yield GetTest(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            int status = context.GetParam("status", (int)DreamStatus.Ok);
            XDoc doc = new XMessage(request);
            doc["status"].ReplaceValue(status.ToInvariantString());
            doc.Elem("verb", context.Verb);
            DreamMessage reply = new DreamMessage((DreamStatus)status, null, doc);
            string cookieValue = context.GetParam("cookie", null);
            if(cookieValue != null) {
                reply.Cookies.Add(DreamCookie.NewSetCookie("test-cookie", cookieValue, Self.Uri, DateTime.UtcNow.AddHours(1.0)));
            }
            if(context.Verb.EqualsInvariant("HEAD")) {
                reply = new DreamMessage(reply.Status, null, MimeType.XML, new byte[0]);
                reply.Headers.ContentLength = doc.ToString().Length;
                response.Return(reply);
            } else {
                response.Return(reply);
            }
            yield break;
        }

        [DreamFeature("POST:convert", "Convert a document to another format. (requires API key)")]
        internal Yield PostConvert(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            response.Return(DreamMessage.Ok(request.ContentType, request.ToBytes()));
            yield break;
        }

        [DreamFeature("GET:status", "Show system status")]
// ReSharper disable UnusedMember.Local
        private Yield GetStatus(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
// ReSharper restore UnusedMember.Local
            DateTime now = DateTime.UtcNow;
            XDoc result = new XDoc("status");
            XUri self = Self.Uri.With("apikey", context.GetParam("apikey", null));

            // host information
            double age = Math.Max((now - _created).TotalSeconds, 1);
            result.Start("host").Attr("created", _created).Attr("age", age).Attr("requests", _requestCounter).Attr("rate", _requestCounter / age);
            result.Elem("uri.local", _localMachineUri.ToString());
            result.Elem("uri.public", _publicUri);

            // host/aliases
            result.Start("aliases").Attr("count", _aliases.Count).Attr("href", self.At("status", "aliases")).End();

            // connections
            result.Start("connections").Attr("count", _connectionCounter).Attr("pending", (_requestQueue != null) ? _requestQueue.Count : 0).Attr("limit", _connectionLimit).End();

            // activities
            var activities = ActivityMessages;
            result.Start("activities");
            PopulateActivities(result, self, now, activities);
            result.End();

            // infos
            lock(_infos) {
                result.Start("infos").Attr("count", _infos.Count);
                foreach(KeyValuePair<string, Tuplet<int, string>> entry in _infos) {
                    result.Start("info").Attr("source", entry.Key).Attr("hits", entry.Value.Item1).Attr("rate", entry.Value.Item1 / age).Value(entry.Value.Item2).End();
                }
                result.End();
            }

            // host/services information
            result.Start("services").Attr("count", _services.Count).Attr("href", self.At("services")).End();

            // host/features
            result.Start("features").Attr("href", self.At("status", "features")).End();

            // end host information
            result.End();

            // system information
            result.Start("system");

            // system/memory information
            result.Elem("memory.used", GC.GetTotalMemory(false));

            // system/thread information
            int workerThreads;
            int completionThreads;
            int dispatcherThreads;
            AsyncUtil.GetAvailableThreads(out workerThreads, out completionThreads, out dispatcherThreads);
            int maxWorkerThreads;
            int maxCompletionThreads;
            int maxDispatcherThreads;
            AsyncUtil.GetMaxThreads(out maxWorkerThreads, out maxCompletionThreads, out maxDispatcherThreads);
            result.Elem("workerthreads.max", maxWorkerThreads);
            result.Elem("workerthreads.used", maxWorkerThreads - workerThreads);
            result.Elem("completionthreads.max", maxCompletionThreads);
            result.Elem("completionthreads.used", maxCompletionThreads - completionThreads);
            result.Elem("dispatcherthreads.max", maxDispatcherThreads);
            result.Elem("dispatcherthreads.used", maxDispatcherThreads - dispatcherThreads);

            // timer information
            var taskTimerStats = TaskTimerFactory.GetStatistics();
            result.Start("timers.queued").Attr("href", self.At("status", "timers")).Value(taskTimerStats.QueuedTimers).End();
            result.Start("timers.pending").Attr("href", self.At("status", "timers")).Value(taskTimerStats.PendingTimers).End();
            result.Elem("timers.counter", taskTimerStats.Counter);
            result.Elem("timers.last", taskTimerStats.Last);

            // rendez-vous events 
            result.Start("async").Attr("count", RendezVousEvent.PendingCounter + AResult.PendingCounter);
            lock(RendezVousEvent.Pending) {
                foreach(var entry in RendezVousEvent.Pending.Values) {
                    result.Start("details");
                    if(entry.Key != null) {
                        var dc = entry.Key.GetState<DreamContext>();
                        if(dc != null) {
                            result.Elem("verb", dc.Verb);
                            result.Elem("uri", dc.Uri);
                        }
                    }
                    if(entry.Value != null) {
                        result.Start("stacktrace");
                        XException.AddStackTrace(result, entry.Value.ToString());
                        result.End();
                    }
                    result.End();
                }
            }
            result.End();

            // xml name table stats
            LockFreeXmlNameTable table = SysUtil.NameTable as LockFreeXmlNameTable;
            if(table != null) {
                int capacity;
                int entries;
                long bytes;
                int[] distribution;
                double expected;
                table.GetStats(out capacity, out entries, out bytes, out distribution, out expected);
                result.Start("xmlnametable").Attr("href", self.At("status", "xmlnametable"));
                result.Elem("capacity", capacity.ToString("#,##0"));
                result.Elem("bytes", bytes.ToString("#,##0"));
                result.Elem("entries", entries.ToString("#,##0"));
                result.Elem("expected-comparisons", expected);
                result.Elem("distribution", "[" + string.Join("; ", Array.ConvertAll(distribution, i => i.ToString("#,##0"))) + "]");
                result.End();
            }

            // replicate app settings
            result.Start("app-settings");
            foreach(var key in System.Configuration.ConfigurationManager.AppSettings.AllKeys) {
                result.Start("entry").Attr("key", key).Attr("value", System.Configuration.ConfigurationManager.AppSettings[key]).End();
            }
            result.End();

            // end of </system>
            result.End();

            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:status/threads", "Show information about all threads")]
// ReSharper disable UnusedMember.Local
        private XDoc GetThreads() {
// ReSharper restore UnusedMember.Local
            XDoc result = new XDoc("threads");
            var threadinfos = AsyncUtil.Threads;
            result.Attr("count", threadinfos.Count());
            foreach(var threadinfo in threadinfos) {
                result.Start("thread");
                result.Attr("name", threadinfo.Thread.Name);
                result.Attr("id", threadinfo.Thread.ManagedThreadId);
                result.Attr("state", threadinfo.Thread.ThreadState.ToString());
                result.Attr("priority", threadinfo.Thread.Priority.ToString());
                if(threadinfo.Info != null) {
                    result.Attr("info", threadinfo.Info.ToString());
                }
                result.End();
            }
            return result;
        }

        [DreamFeature("GET:status/aliases", "Show system aliases")]
// ReSharper disable UnusedMember.Local
        private Yield GetStatusAliases(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
// ReSharper restore UnusedMember.Local
            XDoc result = new XDoc("aliases");

            // host/aliases
            lock(_aliases) {
                result.Attr("count", _aliases.Count);
                foreach(XUri uri in _aliases.Keys) {
                    result.Elem("uri.alias", uri.ToString());
                }
            }
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("POST:status/aliases", "Add a system alias")]
        internal Yield PostStatusAlias(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            lock(_aliases)
                foreach(var alias in request.ToDocument()["uri.alias"]) {
                    var uriText = alias.AsText;
                    if(string.IsNullOrEmpty(uriText)) {
                        continue;
                    }
                    var uri = new XUri(uriText);
                    _aliases[uri] = uri;
                }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:status/timers", "Show system timers")]
// ReSharper disable UnusedMember.Local
        private Yield GetTimers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
// ReSharper restore UnusedMember.Local
            var factories = TaskTimerFactory.Factories;
            var result = new XDoc("timers").Attr("count.factories", factories.Length);
            var allTimers = 0;
            foreach(var factory in factories) {
                result.Start("factory")
                    .Attr("nextmaintenace", factory.NextMaintenance.ToString())
                    .Attr("type.owner", factory.OwnerType.ToString());
                var service = factory.Owner as IDreamService;
                if(service != null) {
                    result.Attr("uri.owner", service.Self);
                }
                if(factory.IsAbandoned) {
                    result.Attr("abandoned", true);
                }
                var next = factory.Next;
                var now = DateTime.UtcNow;
                var timerCount = 0;
                if(next != null) {
                    timerCount++;
                    result.Start("timer").Attr("status", next.Status.ToString()).Attr("when", (next.When - now).ToString());
                    var handler = next.Handler;
                    if(handler != null) {
                        var method = handler.Method == null ? "unknown" : handler.Method.ToString();
                        var target = handler.Target == null ? "unknown" : handler.Target.ToString();
                        result.Attr("handler", method).Attr("target", target);
                    }
                    result.End();
                }
                var pending = factory.Pending.ToArray();
                timerCount += pending.Length;
                allTimers += timerCount;
                result.Attr("count", timerCount);
                foreach(var timer in pending) {
                    result.Start("timer").Attr("status", timer.Status.ToString()).Attr("when", (timer.When - now).ToString());
                    var handler = timer.Handler;
                    if(handler != null) {
                        var method = handler.Method == null ? "unknown" : handler.Method.ToString();
                        var target = handler.Target == null ? "unknown" : handler.Target.ToString();
                        result.Attr("handler", method).Attr("target", target);
                    }
                    result.End();
                }
                result.End();
            }
            result.Attr("count.timers", allTimers);
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:status/activities", "Show system activities")]
// ReSharper disable UnusedMember.Local
        private Yield GetStatusActiities(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
// ReSharper restore UnusedMember.Local
            DateTime now = DateTime.UtcNow;

            // host/aliases
            XUri self = Self.Uri.With("apikey", context.GetParam("apikey", null));
            var activities = ActivityMessages;
            var result = new XDoc("activities");
            PopulateActivities(result, self, now, activities);
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:status/features", "Show system features")]
// ReSharper disable UnusedMember.Local
        private Yield GetStatusFeatures(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
// ReSharper restore UnusedMember.Local
            XDoc result;
            lock(_features) {
                result = _features.ListAll();
            }
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:status/xmlnametable", "Show entries in XmlNameTable")]
// ReSharper disable UnusedMember.Local
        private Yield GetStatusXmlNameTable(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
// ReSharper restore UnusedMember.Local
            var result = new XDoc("xmlnametable");
            var table = SysUtil.NameTable as LockFreeXmlNameTable;
            if(table != null) {
                var entries = table.GetEntries();
                result.Attr("count", entries.Length);
                foreach(var entry in entries) {
                    result.Elem("entry", entry);
                }

            }
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // initialize fields
            _blueprints = new Dictionary<string, XDoc>(StringComparer.Ordinal);

            // create sub-services
            if(_storageType.EqualsInvariant("s3")) {
                yield return CreateService(
                        "$store",
                        "sid://mindtouch.com/2010/10/dream/s3.storage.private",
                        new XDoc("config")
                            .Elem("folder", _storagePath)
                            .Elem("private-root", true)
                            .AddNodes(_storageConfig),
                        new Result<Plug>()).Set(v => _storage = v);
            } else {
                yield return CreateService(
                        "$store",
                        "sid://mindtouch.com/2007/07/dream/storage.private",
                        new XDoc("config")
                            .Elem("folder", _storagePath)
                            .Elem("private-root", true),
                        new Result<Plug>()).Set(v => _storage = v);
            }

            // NOTE (steveb): now that we have created our basic infrastructure, it's time to fill-in the parts we skipped
            DreamMessage msg = null;
            yield return Self.At("load").With("name", "mindtouch.core").Post(new Result<DreamMessage>(TimeSpan.MaxValue)).Set(v => msg = v);
            if(!msg.IsSuccessful) {
                throw new Exception("unexpected failure loading mindtouch.core");
            }
            yield return Self.At("load").With("name", "mindtouch.dream").Post(new Result<DreamMessage>(TimeSpan.MaxValue)).Set(v => msg = v);
            if(!msg.IsSuccessful) {
                throw new Exception("unexpected failure loading mindtouch.core");
            }
            result.Return();
        }

        protected override Yield Stop(Result result) {
            if(!IsRunning) {
                result.Return();
                yield break;
            }
            try {

                // BUG #810: announce to all root-level services that we're shutting down

                // dismiss all pending requests
                _requestQueue = null;

                // shutdown all services, except host and sub-services (the latter should be cleaned-up by their owners)
                _log.Debug("Stopping stand-alone services");
                Dictionary<string, ServiceEntry> services;
                lock(_services) {
                    services = new Dictionary<string, ServiceEntry>(_services);
                }
                foreach(KeyValuePair<string, ServiceEntry> entry in services) {
                    if((entry.Value.Owner == null) && !(ReferenceEquals(this, entry.Value.Service))) {
                        StopService(entry.Value.Service.Self);
                    }
                }

                // now destroy support services
                _log.Debug("Stopping host");
            } catch(Exception ex) {
                _log.ErrorExceptionMethodCall(ex, "Stop: host failed to deinitialize");
            }

            // stop storage service
            if(_storage != null) {
                yield return _storage.Delete(new Result<DreamMessage>(TimeSpan.MaxValue)).CatchAndLog(_log);
                _storage = null;
            }

            // check if any inner services failed to stop
            foreach(KeyValuePair<string, ServiceEntry> entry in _services) {
                _log.WarnMethodCall("Stop: service did not shutdown", entry.Key);
            }

            // invoke base.Stop
            yield return Coroutine.Invoke(base.Stop, new Result()).CatchAndLog(_log);

            // deinitialize fields
            _blueprints = null;
            _registeredTypes.Clear();
            _infos.Clear();
            _activities.Clear();
            _features = new DreamFeatureDirectory();
            _services.Clear();
            _aliases.Clear();

            // mark host as not running
            Plug.RemoveEndpoint(this);
            _running = false;
            _shutdown.Set();
            result.Return();
        }

        public void Initialize(XDoc config) {
            if(_running) {
                _log.WarnMethodCall("Initialize: host already initailized");
                throw new InvalidOperationException("already initialized");
            }
            try {

                // initialize container
                var containerConfig = config["components"];
                if(!containerConfig.IsEmpty) {
                    _log.Debug("registering host level module");
                    var builder = new ContainerBuilder();
                    builder.RegisterModule(new XDocAutofacContainerConfigurator(containerConfig, DreamContainerScope.Host));
                    builder.Update(_container);
                }

                // make sure we have an IServiceActivator
                if(!_container.IsRegistered<IServiceActivator>()) {
                    var builder = new ContainerBuilder();
                    builder.RegisterType<DefaultServiceActivator>().As<IServiceActivator>();
                    builder.Update(_container);
                }
                _serviceActivator = _container.Resolve<IServiceActivator>();
                _running = true;
                _shutdown = new ManualResetEvent(false);
                _rootRedirect = config["root-redirect"].AsText;
                _debugMode = config["debug"].AsText.IfNullOrEmpty("false").ToLowerInvariant();
                _memorizeAliases = config["memorize-aliases"].AsBool ?? true;

                // add default prologues/epilogues
                _defaultPrologues = new[] { 
                    new DreamFeatureStage("dream.in.*", PrologueDreamIn, DreamAccess.Public)
                };
                _defaultEpilogues = new[] { 
                    new DreamFeatureStage("dream.out.*", EpilogueDreamOut, DreamAccess.Public) 
                };

                // initialize identity
                _id = !config["guid"].IsEmpty ? new Guid(config["guid"].AsText) : Guid.NewGuid();
                _localMachineUri = new XUri(string.Format("local://{0}", _id.ToString("N")));
                _aliases[_localMachineUri] = _localMachineUri;

                // initialize environment
                string path = config["host-path"].AsText ?? "host";
                _publicUri = config["uri.public"].AsUri ?? new XUri("http://localhost:8081");
                _aliases[_publicUri] = _publicUri;
                _connectionLimit = config["connect-limit"].AsInt ?? 0;
                if(_connectionLimit < 0) {

                    // determine connection limit based on max thread count
                    int maxThreads;
                    int maxPorts;
                    int maxDispatchers;
                    AsyncUtil.GetMaxThreads(out maxThreads, out maxPorts, out maxDispatchers);
                    if(maxDispatchers > 0) {
                        _connectionLimit = maxDispatchers + _connectionLimit;
                    } else {
                        _connectionLimit = maxThreads + _connectionLimit;
                    }
                }
                _reentrancyLimit = config["reentrancy-limit"].AsInt ?? 20;
                _storageType = config["storage/@type"].AsText ?? "local";
                if("s3".EqualsInvariant(_storageType)) {
                    _storagePath = config["storage/root"].AsText ?? "";
                    _storageConfig = config["storage"].Clone();
                } else {
                    _storagePath = config["storage/path"].AsText ?? config["storage-dir"].AsText ?? config["service-dir"].AsText;
                    if(!Path.IsPathRooted(_storagePath)) {
                        throw new ArgumentException("missing or invalid storage-dir");
                    }
                }

                // log initialization settings
                _log.DebugMethodCall("Initialize: guid", _id);
                _log.DebugMethodCall("Initialize: apikey", config["apikey"].AsText ?? "(auto)");
                _log.DebugMethodCall("Initialize: uri.public", _publicUri);
                _log.DebugMethodCall("Initialize: storage-type", _storageType);
                _log.DebugMethodCall("Initialize: storage-dir", _storagePath);
                _log.DebugMethodCall("Initialize: host-path", path);
                _log.DebugMethodCall("Initialize: connect-limit", _connectionLimit);

                // add path & type information
                config = config.Root;
                config.Elem("path", path);
                config.Elem("class", GetType().FullName);
                config.Elem("sid", "sid://mindtouch.com/2007/03/dream/host");

                // set root-uri
                Plug.AddEndpoint(this);

                // check if we need to fill in the TYPE information using the type
                XDoc blueprint = CreateServiceBlueprint(GetType());

                // start service
                if(_connectionLimit > 0) {
                    _requestQueue = new ProcessingQueue<Action<Action>>(RequestQueueCallback, _connectionLimit);
                }
                Coroutine.Invoke(StartService, this, blueprint, path, config, new Result<XDoc>()).Wait();
            } catch {
                _running = false;
                _shutdown.Set();
                throw;
            }
        }

        public void Deinitialize() {
            try {
                if(IsRunning) {
                    Self.WithTimeout(TimeSpan.MaxValue).With("apikey", PrivateAccessKey).At("@config").Delete();
                }
            } finally {
                lock(_serviceLifetimeScopes) {
                    foreach(var serviceContainer in _serviceLifetimeScopes.Values) {
                        serviceContainer.Dispose();
                    }
                    _serviceLifetimeScopes.Clear();
                }
                _hostLifetimeScope.Dispose();
            }
        }

        public void WaitUntilShutdown() {
            _shutdown.WaitOne();
        }

        public IDreamActivityDescription CreateActivityDescription() {
            return new DreamActivityDescription(_activities);
        }

        public void UpdateInfoMessage(string source, string message) {
            lock(_infos) {
                Tuplet<int, string> info;
                if(!_infos.TryGetValue(source, out info)) {
                    info = new Tuplet<int, string>(0, null);
                    _infos[source] = info;
                }
                ++info.Item1;
                info.Item2 = message;
            }
        }

        public Result<DreamMessage> SubmitRequestAsync(string verb, XUri uri, IPrincipal user, DreamMessage request, Result<DreamMessage> response) {

            // check if no user is specified
            if(user == null) {

                // check if a context already exists
                DreamContext context = DreamContext.CurrentOrNull;
                if(context != null) {

                    // inherit the current user principal
                    user = context.User;
                }
            }
            var requestQueue = _requestQueue;
            if(requestQueue != null) {
                if(!requestQueue.TryEnqueue(completion => SubmitRequestAsync(verb, uri, user, request, response, completion))) {
                	throw new ShouldNeverHappenException();
                }
                return response;
            }
            return SubmitRequestAsync(verb, uri, user, request, response, () => {});
        }

        private Result<DreamMessage> SubmitRequestAsync(string verb, XUri uri, IPrincipal user, DreamMessage request, Result<DreamMessage> response, Action completion) {
            if(string.IsNullOrEmpty(verb)) {
                throw new ArgumentNullException("verb");
            }
            if(uri == null) {
                throw new ArgumentNullException("uri");
            }
            if(request == null) {
                throw new ArgumentNullException("request");
            }
            if(response == null) {
                throw new ArgumentNullException("response");
            }

            // ensure environment is still running
            if(!IsRunning) {
                response.Return(DreamMessage.InternalError("host not running"));
                return response;
            }

            try {
                Interlocked.Increment(ref _requestCounter);

                // check if we were not able to begin processing the request
                DreamMessage failed = BeginRequest(completion, uri, request);
                if(failed != null) {
                    response.Return(failed);
                    EndRequest(completion, uri, request);
                    return response;
                }

                // check if 'verb' is overwritten by a processing parameter
                verb = verb.ToUpperInvariant();
                string requestedVerb = (uri.GetParam(DreamInParam.VERB, null) ?? request.Headers.MethodOverride ?? verb).ToUpperInvariant();
                if(
                    verb.EqualsInvariant(Verb.POST) || (
                        verb.EqualsInvariant(Verb.GET) && (
                            requestedVerb.EqualsInvariant(Verb.OPTIONS) ||
                            requestedVerb.EqualsInvariant(Verb.HEAD)
                        )
                    )
                ) {
                    verb = requestedVerb;
                }

                // check if an origin was specified
                request.Headers.DreamOrigin = uri.GetParam(DreamInParam.ORIGIN, request.Headers.DreamOrigin);

                // check if a public uri is supplied
                XUri publicUri = XUri.TryParse(uri.GetParam(DreamInParam.URI, null) ?? request.Headers.DreamPublicUri);
                XUri transport = XUri.TryParse(request.Headers.DreamTransport) ?? uri.WithoutCredentialsPathQueryFragment();
                if(publicUri == null) {

                    // check if request is local
                    if(transport.Scheme.EqualsInvariantIgnoreCase("local")) {

                        // local:// uris with no public-uri specifier default to the configured public-uri
                        publicUri = _publicUri;
                    } else {

                        // check if the request was forwarded through Apache mod_proxy
                        string proxyOverride = uri.GetParam(DreamInParam.HOST, null);
                        if(string.IsNullOrEmpty(proxyOverride)) {
                            proxyOverride = request.Headers.ForwardedHost;
                        }
                        string serverPath = string.Join("/", transport.Segments);
                        if(proxyOverride != null) {

                            // request used an override, append path of public-uri
                            serverPath = string.Join("/", _publicUri.Segments);
                        }

                        // set the uri scheme based-on the incoming scheme and the override header
                        string scheme = transport.Scheme;
                        if("On".EqualsInvariantIgnoreCase(request.Headers.FrontEndHttps ?? "")) {
                            scheme = Scheme.HTTPS;
                        }
                        scheme = uri.GetParam(DreamInParam.SCHEME, scheme);

                        // set the host port
                        string hostPort = proxyOverride ?? request.Headers.Host ?? uri.HostPort;
                        publicUri = new XUri(string.Format("{0}://{1}", scheme, hostPort)).AtPath(serverPath);
                    }
                    request.Headers.DreamPublicUri = publicUri.ToString();
                }

                // set host header
                request.Headers.Host = publicUri.HostPort;

                // convert incoming uri to local://
                XUri localFeatureUri = uri.ChangePrefix(uri.WithoutPathQueryFragment(), _localMachineUri);

                // check if path begins with public uri path
                if((transport.Segments.Length > 0) && localFeatureUri.PathStartsWith(transport.Segments)) {
                    localFeatureUri = localFeatureUri.WithoutFirstSegments(transport.Segments.Length);
                }

                // check if the path is the application root and whether we have special behavior for that
                if(localFeatureUri.Path.IfNullOrEmpty("/") == "/") {
                    if(!string.IsNullOrEmpty(_rootRedirect)) {
                        localFeatureUri = localFeatureUri.AtAbsolutePath(_rootRedirect);
                    } else if(IsDebugEnv) {
                        localFeatureUri = localFeatureUri.AtAbsolutePath("/host/services");
                    }
                }

                // find the requested feature
                List<DreamFeature> features;
                lock(_features) {
                    features = _features.Find(localFeatureUri);
                }
                DreamFeature feature = null;
                if(features != null) {

                    // TODO (steveb): match the incoming mime-type to the feature's acceptable mime-types (mime-type overloading)

                    // match the request verb to the feature verb
                    foreach(DreamFeature entry in features) {
                        if((entry.Verb == "*") || entry.Verb.EqualsInvariant(verb)) {
                            feature = entry;
                            break;
                        }
                    }

                    // check if this is an OPTIONS request and there is no defined feature for it
                    if(verb.EqualsInvariant(Verb.OPTIONS) && ((feature == null) || feature.Verb.EqualsInvariant("*"))) {

                        // list all allowed methods
                        List<string> methods = new List<string>();
                        foreach(DreamFeature entry in features) {
                            if(!methods.Contains(entry.Verb)) {
                                methods.Add(entry.Verb);
                            }
                        }
                        methods.Sort(StringComparer.Ordinal.Compare);
                        DreamMessage result = DreamMessage.Ok();
                        result.Headers.Allow = string.Join(", ", methods.ToArray());
                        response.Return(result);

                        // decrease counter for external requests
                        EndRequest(completion, uri, request);
                        return response;
                    }
                }

                // check if a feature was found
                if(feature == null) {
                    DreamMessage result;

                    // check if any feature was found
                    if((features == null) || (features.Count == 0)) {
                        string msg = verb + " URI: " + uri.ToString(false) + " LOCAL: " + localFeatureUri.ToString(false) + " PUBLIC: " + publicUri + " TRANSPORT: " + transport;
                        _log.WarnMethodCall("ProcessRequest: feature not found", msg);
                        result = DreamMessage.NotFound("resource not found");
                    } else {
                        string msg = verb + " " + uri.ToString(false);
                        _log.WarnMethodCall("ProcessRequest: method not allowed", msg);
                        List<string> methods = new List<string>();
                        foreach(DreamFeature entry in features) {
                            if(!methods.Contains(entry.Verb)) {
                                methods.Add(entry.Verb);
                            }
                        }
                        methods.Sort(StringComparer.Ordinal.Compare);
                        result = DreamMessage.MethodNotAllowed(methods.ToArray(), "allowed methods are " + string.Join(", ", methods.ToArray()));
                    }
                    response.Return(result);

                    // decrease counter for external requests
                    EndRequest(completion, uri, request);
                    return response;
                }

                // add uri to aliases list
                if(_memorizeAliases) {
                    lock(_aliases) {
                        _aliases[transport] = transport;
                        _aliases[publicUri] = publicUri;
                    }
                }

                // create context
                DreamContext context = new DreamContext(this, verb, localFeatureUri, feature, publicUri, _publicUri, request, CultureInfo.InvariantCulture, GetRequestLifetimeScopeFactory(feature.Service));

                // attach request id to the context
                context.SetState(DreamHeaders.DREAM_REQUEST_ID, request.Headers.DreamRequestId);

                // add user to context
                context.User = user;

                // build linked-list of feature calls
                var chain = new Result<DreamMessage>(TimeSpan.MaxValue, TaskEnv.Current).WhenDone(result => {

                    // extract message
                    DreamMessage message;
                    if(result.HasValue) {
                        message = result.Value;
                    } else if(result.Exception is DreamAbortException) {
                        message = ((DreamAbortException)result.Exception).Response;
                    } else if(result.Exception is DreamCachedResponseException) {
                        message = ((DreamCachedResponseException)result.Exception).Response;
                    } else {
                        _log.ErrorExceptionFormat(response.Exception, "Failed Feature '{0}' Chain [{1}:{2}]: {3}",
                            feature.MainStage.Name,
                            verb,
                            localFeatureUri.Path,
                            response.Exception.Message
                        );
                        message = DreamMessage.InternalError(result.Exception);
                    }

                    // decrease counter for external requests
                    EndRequest(completion, uri, request);

                    // need to manually dispose of the context, since we're already attaching and detaching it by hand to TaskEnvs throughout the chain
                    if(response.IsCanceled) {
                        _log.DebugFormat("response for '{0}' has already returned", context.Uri.Path);
                        response.ConfirmCancel();
                        ((ITaskLifespan)context).Dispose();
                    } else {
                        ((ITaskLifespan)context).Dispose();
                        response.Return(message);
                    }
                });
                for(int i = feature.Stages.Length - 1; i >= 0; --i) {
                    var link = new DreamFeatureChain(feature.Stages[i], i == feature.MainStageIndex, context, chain, (i > 0) ? feature.Stages[i - 1].Name : "first");
                    chain = new Result<DreamMessage>(TimeSpan.MaxValue, TaskEnv.Current).WhenDone(link.Handler);
                }

                // kick-off new task
                AsyncUtil.Fork(
                    () => chain.Return(request),
                    TaskEnv.New(TimerFactory),
                    new Result(TimeSpan.MaxValue, response.Env).WhenDone(res => {
                        if(!res.HasException) {
                            return;
                        }
                        _log.ErrorExceptionFormat(res.Exception, "handler for {0}:{1} failed", context.Verb, context.Uri.ToString(false));
                        ((ITaskLifespan)context).Dispose();

                        // forward exception to recipient
                        response.Throw(res.Exception);

                        // decrease counter for external requests
                        EndRequest(completion, uri, request);
                    })
                );
            } catch(Exception e) {
                response.Throw(e);
                EndRequest(completion, uri, request);
            }
            return response;
        }

        public ILifetimeScope CreateServiceLifetimeScope(IDreamService service, Action<IContainer, ContainerBuilder> registrationCallback) {
            lock(_serviceLifetimeScopes) {
                if(_serviceLifetimeScopes.ContainsKey(service)) {
                    throw new InvalidOperationException(string.Format("LifetimeScope for service  '{0}' at '{1}' has already been created.", service, service.Self.Uri));
                }
                var serviceLifetimeScope = _hostLifetimeScope.BeginLifetimeScope(DreamContainerScope.Service, b => registrationCallback(_container, b));
                _serviceLifetimeScopes[service] = serviceLifetimeScope;
                return serviceLifetimeScope;
            }
        }

        public void DisposeServiceContainer(IDreamService service) {
            lock(_serviceLifetimeScopes) {
                ILifetimeScope serviceLifetimeScope;
                if(!_serviceLifetimeScopes.TryGetValue(service, out serviceLifetimeScope)) {
                    _log.WarnFormat("LifetimeScope for service '{0}' at '{1}' already gone.", service, service.Self.Uri.ToString(false));
                    return;
                }
                serviceLifetimeScope.Dispose();
                _serviceLifetimeScopes.Remove(service);
            }
        }

        private DreamMessage BeginRequest(Action completion, XUri uri, DreamMessage request) {
            if(completion != null) {
                Interlocked.Increment(ref _connectionCounter);
            }

            // check if request is new or basd on an existing request
            var id = request.Headers.DreamRequestId;
            if(string.IsNullOrEmpty(id)) {

                // assign a new request id
                id = Guid.NewGuid().ToString("B");
                request.Headers.DreamRequestId = id;
                lock(_requests) {
                    _requests[id] = new List<XUri> { uri };
                }
            } else {

                // check if reentrant request limit has been reached
                lock(_requests) {
                    List<XUri> requests;
                    if(!_requests.TryGetValue(id, out requests)) {
                        requests = new List<XUri>();
                        _requests[id] = requests;
                    }
                    if(requests.Count >= _reentrancyLimit) {
                        return new DreamMessage(DreamStatus.ServiceUnavailable, null, MimeType.TEXT, "The request exceeded the reentrancy limit for the server.");
                    }
                    requests.Add(uri);
                }
            }
            return null;
        }

        private void EndRequest(Action completion, XUri uri, DreamMessage request) {

            // decrease reentrancy request limit
            var id = request.Headers.DreamRequestId;
            if(!string.IsNullOrEmpty(id)) {
                lock(_requests) {
                    List<XUri> requests;
                    if(_requests.TryGetValue(id, out requests)) {
                        requests.RemoveLast(uri);
                        if(requests.Count == 0) {
                            _requests.Remove(id);
                        }
                    }
                }
            }
            if(completion != null) {
                Interlocked.Decrement(ref _connectionCounter);
                completion();
            }
        }

        private Func<Action<ContainerBuilder>, ILifetimeScope> GetRequestLifetimeScopeFactory(IDreamService service) {
            return buildAction => {
                ILifetimeScope serviceLifetimeScope;
                lock(_serviceLifetimeScopes) {
                    if(!_serviceLifetimeScopes.TryGetValue(service, out serviceLifetimeScope)) {
                        throw new InvalidOperationException(string.Format("Cannot create a request container for service  '{0}' at '{1}'. This error  normally occurs if DreamContext.Container is invoked in Service Start or Shutdown", service, service.Self.Uri));
                    }
                }
                var requestLifetimeScope = serviceLifetimeScope.BeginLifetimeScope(DreamContainerScope.Request, buildAction);
                return requestLifetimeScope;
            };
        }

        private Yield StartService(IDreamService service, XDoc blueprint, string path, XDoc config, Result<XDoc> result) {
            Result<DreamMessage> r;
            path = path.ToLowerInvariant();
            if(_services.ContainsKey(path)) {
                string message = string.Format("conflicting uri: {0}", path);
                _log.Warn(message);
                throw new ArgumentException(message);
            }

            // TODO (steveb): validate all fields in the blueprint (presence & validity)

            // add fresh information
            Type type = service.GetType();
            XUri sid = config["sid"].AsUri ?? blueprint["sid"].AsUri;
            XUri owner = config["uri.owner"].AsUri;

            // create directory of service features
            DreamFeatureDirectory features = CreateServiceFeatureDirectory(service, blueprint, config);
            XUri uri = config["uri.self"].AsUri;

            // now that we have the uri, we can add the storage information (if we already started the host storage service!)
            Plug serviceStorage = null;
            if(_storage != null) {
                var encodedPath = EncodedServicePath(uri);

                // check if private storage is requested
                if(!blueprint["setup/private-storage"].IsEmpty) {

                    // set storage configuration
                    // TODO (arnec): currently new private services are rooted inside shared private service, which means they
                    // could be accessed by the shared private users
                    if(_storageType.EqualsInvariant("s3")) {

                        // Note (arnec): For S3 we can't use Path.Combine, since it might use a different separator from '/'
                        var servicePath = new StringBuilder(_storagePath);
                        if(!servicePath[servicePath.Length - 1].Equals('/')) {
                            servicePath.Append("/");
                        }
                        if(encodedPath[0].Equals('/')) {
                            servicePath.Append(encodedPath.Substring(1));
                        } else {
                            servicePath.Append(encodedPath);
                        }
                        yield return CreateService(
                            "private-storage/" + encodedPath,
                            "sid://mindtouch.com/2010/10/dream/s3.storage.private",
                            new XDoc("config")
                                .Elem("folder", servicePath.ToString())
                                .AddNodes(_storageConfig),
                            new Result<Plug>()).Set(v => serviceStorage = v);

                    } else {
                        var servicePath = Path.Combine(_storagePath, encodedPath);
                        yield return CreateService(
                            "private-storage/" + encodedPath,
                            "sid://mindtouch.com/2007/07/dream/storage.private",
                            new XDoc("config").Elem("folder", servicePath),
                            new Result<Plug>()).Set(v => serviceStorage = v);
                    }
                    config.Elem("uri.storage", serviceStorage.Uri);
                    var cookies = Cookies;
                    lock(cookies) {
                        foreach(var cookie in cookies.Fetch(serviceStorage.Uri)) {
                            config.Add(cookie.AsSetCookieDocument);
                        }
                    }
                } else {
                    // use central private storage
                    config.Elem("uri.storage", _storage.Uri.At(encodedPath));

                    // get central storage's internal access key
                    DreamCookieJar cookies = Cookies;
                    lock(cookies) {
                        foreach(DreamCookie cookie in cookies.Fetch(_storage.Uri)) {
                            config.Add(cookie.AsSetCookieDocument);
                        }
                    }

                }
            }

            // check if we're bootstrapping (i.e. starting ourself!)
            if(Self != null) {

                // add 'internal' access key
                config.Add(DreamCookie.NewSetCookie("service-key", InternalAccessKey, Self.Uri).AsSetCookieDocument);
            }

            // initialize service
            try {
                service.Initialize(this, blueprint);
            } catch {
                string message = string.Format("StartService: service initialization failed ({0} : {1})", path, sid);
                _log.Warn(message);
                throw new DreamBadRequestException(message);
            }

            // activate features
            lock(_features) {
                _features.Add(uri.Segments, 0, features);
            }

            // start service
            yield return r = Plug.New(uri).At("@config").Put(config, new Result<DreamMessage>(TimeSpan.MaxValue));
            XDoc resultDoc;
            if(r.Value.IsSuccessful) {

                // report service as started
                lock(_services) {

                    // TODO (steveb): this operation may fail if two services attempt to register at the same uri; in which case the service should be stopped.
                    _services.Add(uri.Path.ToLowerInvariant(), new ServiceEntry(service, uri, owner, sid, blueprint));
                }
                resultDoc = r.Value.ToDocument();
            } else {
                StopService(uri);
                if(serviceStorage != null) {
                    StopService(serviceStorage);
                }

                // deactivate features
                lock(_features) {
                    _features.Remove(uri);
                }
                _log.ErrorExceptionMethodCall(null, "StartService", (sid != null) ? (object)sid : (object)type.FullName);
                string message = string.Format("service initialization failed: {0} ({1})", uri, sid);
                _log.Warn(message);
                throw new DreamAbortException(r.Value);
            }
            _log.DebugFormat("StartService: service started: {0} ({1})", uri, sid);
            result.Return(resultDoc);
        }

        private void StopService(XUri uri) {
            string path = uri.Path.ToLowerInvariant();

            // remove service from services table
            ServiceEntry service;
            lock(_services) {
                if(_services.TryGetValue(path, out service)) {
                    _services.Remove(path);
                }
            }
            if(_log.IsDebugEnabled) {
                string sid = "<UNKNOWN>";
                string type = "<UNKNOWN>";
                if(service != null) {
                    sid = service.SID.ToString();
                    if(service.Service != null) {
                        type = service.Service.GetType().ToString();
                    }
                }
                _log.DebugMethodCall("stop", path, sid, type);
            }

            // deactivate service
            DreamMessage deleteResponse = Plug.New(uri).At("@config").Delete(new Result<DreamMessage>(TimeSpan.MaxValue)).Wait();
            if(!deleteResponse.IsSuccessful) {
                _log.InfoMethodCall("StopService: Delete failed", uri.ToString(false), deleteResponse.Status);
            }

            // deactivate features
            lock(_features) {
                _features.Remove(uri);
            }

            // check for private-storage service
            if((service != null) && !service.Blueprint["setup/private-storage"].IsEmpty) {
                StopService(Self.At("private-storage", EncodedServicePath(service.Uri)));
            }

            // check for any lingering child services
            List<ServiceEntry> entries;
            lock(_services) {
                entries = _services.Values.Where(entry => uri == entry.Owner).ToList();
            }
            foreach(ServiceEntry entry in entries) {
                _log.WarnMethodCall("StopService: child service was not shutdown properly", entry.Service.Self.Uri.ToString(false));
                StopService(entry.Service.Self);
            }
        }

        private void RegisterBlueprint(XDoc blueprint, Type type) {

            // check if type has already been registers
            Debug.Assert(type != null, "type != null");
            Debug.Assert(type.AssemblyQualifiedName != null, "type.AssemblyQualifiedName != null");
            if(_registeredTypes.ContainsKey(type.AssemblyQualifiedName)) {
                return;
            }
            if(!type.IsA<IDreamService>()) {
                throw new DreamAbortException(DreamMessage.BadRequest("class is not derived from IDreamService"));
            }
            if(type.IsAbstract || type.IsValueType || type.IsInterface) {
                throw new DreamAbortException(DreamMessage.BadRequest("class is not an instantiatable"));
            }
            ConstructorInfo ctor = type.GetConstructor(Type.EmptyTypes);
            if(ctor == null) {
                throw new DreamAbortException(DreamMessage.BadRequest("class is missing a default constructror"));
            }

            // check if we need to fill in the TYPE information using the type
            if((blueprint == null) || (blueprint["name"].IsEmpty)) {
                blueprint = CreateServiceBlueprint(type);
            }

            // BUG #809: let's check all fields in the service blueprint (presence & validity)

            // register blueprint under SIDs
            Dictionary<string, XDoc> blueprints = _blueprints;
            if(blueprints != null) {
                lock(blueprints) {
                    foreach(XDoc sid in blueprint["sid"]) {
                        XUri uri = sid.AsUri;
                        _log.DebugMethodCall("register", uri);
                        ProxyPlugEndpoint.Add(uri, blueprint);
                        blueprints[XUri.EncodeSegment(uri.ToString())] = blueprint;
                    }
                }
            } else {
                throw new InvalidOperationException("host is not initialized");
            }

            // add type to registered type list
            lock(_registeredTypes) {
                _registeredTypes[type.AssemblyQualifiedName] = type;
            }
        }

        private DreamFeatureDirectory CreateServiceFeatureDirectory(IDreamService service, XDoc blueprint, XDoc config) {
            Type type = service.GetType();
            string path = config["path"].Contents.ToLowerInvariant();

            // add transport information
            XUri serviceUri = LocalMachineUri.AtAbsolutePath(path);
            config.Root.Elem("uri.self", serviceUri.ToString());

            // compile list of active service features, combined by suffix
            int serviceUriSegmentsLength = serviceUri.Segments.Length;
            DreamFeatureDirectory directory = new DreamFeatureDirectory();
            var methodInfos = GetMethodInfos(type);
            foreach(XDoc featureBlueprint in blueprint["features/feature"]) {
                string methodName = featureBlueprint["method"].Contents;
                string pattern = featureBlueprint["pattern"].AsText;

                // TODO (steveb): we should be a little more discerning here as this might trigger false positives
                bool atConfig = pattern.ContainsInvariantIgnoreCase("@config");

                // locate method
                var methods = methodInfos[methodName];
                if(methods.Count() > 1) {
                    var found = string.Join(", ", methods.Select(m => m.DeclaringType.FullName + "!" + m.Name + "(" + string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name).ToArray()) + ")").ToArray());
                    throw new MissingMethodException(string.Format("found multiple definitions for {0}: {1}", methodName, found));
                }
                if(methods.None()) {
                    throw new MissingMethodException(string.Format("could not find {0} in class {1}", methodName, type.FullName));
                }
                MethodInfo method = methods.First();

                // determine access level
                DreamAccess access;
                switch(featureBlueprint["access"].AsText) {
                case null:
                case "public":
                    access = DreamAccess.Public;
                    break;
                case "internal":
                    access = DreamAccess.Internal;
                    break;
                case "private":
                    access = DreamAccess.Private;
                    break;
                default:
                    throw new NotSupportedException(string.Format("access level is not supported ({0})", methodName));
                }

                // parse pattern string
                string[] parts = pattern.Split(new[] { ':' }, 2);
                string verb = parts[0].Trim();
                string signature = parts[1].Trim();
                if(signature.Length == 0) {
                    signature = string.Empty;
                }

                // add feature prologues
                List<DreamFeatureStage> stages = new List<DreamFeatureStage>();
                stages.AddRange(_defaultPrologues);
                if(!atConfig) {
                    DreamFeatureStage[] custom = service.Prologues;
                    if(!ArrayUtil.IsNullOrEmpty(custom)) {
                        stages.AddRange(custom);
                    }
                }

                // add feature handler
                int mainStageIndex = stages.Count;
                stages.Add(new DreamFeatureStage(service, method, access));

                // add feature epilogues
                if(!atConfig) {
                    DreamFeatureStage[] custom = service.Epilogues;
                    if(!ArrayUtil.IsNullOrEmpty(custom)) {
                        stages.AddRange(custom);
                    }
                }
                stages.AddRange(_defaultEpilogues);

                // create dream feature and add to service directory
                var paramAttributes = method.GetCustomAttributes(typeof(DreamFeatureParamAttribute), false).Cast<DreamFeatureParamAttribute>().ToArray();
                DreamFeature feature = new DreamFeature(service, serviceUri, mainStageIndex, stages.ToArray(), verb, signature, paramAttributes);
                directory.Add(feature.PathSegments, serviceUriSegmentsLength, feature);
            }
            return directory;
        }

        private void RequestQueueCallback(Action<Action> action, Action completion) {

            // check if host was stopped
            if(_requestQueue == null) {
                completion();
                return;
            }
            action(completion);
        }

        //--- Interface Methods ---
        int IPlugEndpoint.GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            int result;
            XUri prefix;
            lock(_aliases) {
                _aliases.TryGetValue(uri, out prefix, out result);
            }

            // check if we found a match
            if(prefix != null) {
                normalized = uri.ChangePrefix(prefix, _localMachineUri);

                // if 'dream.in.uri' is not set, set it
                if((normalized.GetParam(DreamInParam.URI, null) == null) && !prefix.Scheme.EqualsInvariant("local")) {
                    normalized = normalized.With(DreamInParam.URI, prefix.ToString());
                }
            } else {
                normalized = null;
            }
            return (result > 0) ? result + Plug.BASE_ENDPOINT_SCORE : 0;
        }

        Yield IPlugEndpoint.Invoke(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
            UpdateInfoMessage(SOURCE_HOST, null);
            Result<DreamMessage> res = new Result<DreamMessage>(response.Timeout, TaskEnv.New());
            SubmitRequestAsync(verb, uri, null, request, res, null);
            yield return res;
            response.Return(res);
        }
    }
}
