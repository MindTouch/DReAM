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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Autofac;
using Autofac.Builder;
using MindTouch.Security.Cryptography;
using MindTouch.Tasking;
using MindTouch.Web;

namespace MindTouch.Dream {

    /// <summary>
    /// Provides request context information for <see cref="DreamFeature"/> request processing.
    /// </summary>
    public class DreamContext : ITaskLifespan {

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog();
        private static int _contextIdCounter = 0;

        //--- Class Properties ---

        /// <summary>
        /// Singleton accessor for the current request context.
        /// </summary>
        /// <remarks> Will throw an <see cref="DreamContextAccessException"/> if there is no context defined.</remarks>
        /// <exception cref="DreamContextAccessException">Thrown if no context is defined in the current environment or the context has been disposed.</exception>
        public static DreamContext Current {
            get {
                TaskEnv current = TaskEnv.CurrentOrNull;
                if(current == null) {
                    throw new DreamContextAccessException("DreamContext.Current is not set because there is no task environment.");
                }
                DreamContext context = current.GetState<DreamContext>();
                if(context == null) {
                    throw new DreamContextAccessException("DreamContext.Current is not set because the current task environment does not contain a reference.");
                }
                if(context._isTaskDisposed) {
                    throw new DreamContextAccessException("DreamContext.Current is not set because the current context is already disposed.");

                }
                return context;
            }
        }

        /// <summary>
        /// Singleton accessor to current request context or <see langword="null"/>, if none is defined.
        /// </summary>
        public static DreamContext CurrentOrNull {
            get {
                TaskEnv current = TaskEnv.CurrentOrNull;
                if(current == null) {
                    return null;
                }
                DreamContext context = current.GetState<DreamContext>();
                if(context == null) {
                    return null;
                }
                if(context._isTaskDisposed) {
                    _log.Warn("requested already disposed context via CurrentOrNull, returning null");
                    return null;
                }
                return context;
            }
        }

        //--- Fields ---

        /// <summary>
        /// Dream environment.
        /// </summary>
        public readonly IDreamEnvironment Env;

        /// <summary>
        /// Unique Identifier of the request.
        /// </summary>
        public readonly int ID;
        
        /// <summary>
        /// Request Http Verb.
        /// </summary>
        public readonly string Verb;

        /// <summary>
        /// Request Uri.
        /// </summary>
        public readonly XUri Uri;

        /// <summary>
        /// Dream feature responsible for handling the request.
        /// </summary>
        public readonly DreamFeature Feature;

        /// <summary>
        /// Incoming request message.
        /// </summary>
        public readonly DreamMessage Request;

        // TODO (arnec): StartTime should eventually be mirroed with EndTime and a Duration attribute on features
        //               for now it's just the time the request started that should be used instead of DateTime.UtcNow
        /// <summary>
        /// Time the request started.
        /// </summary>
        public readonly DateTime StartTime;

        /// <summary>
        /// Caching information for request.
        /// </summary>
        public Tuplet<object, TimeSpan> CacheKeyAndTimeout;

        private readonly XUri _publicUri;
        private readonly string[] _suffixes;
        private readonly Dictionary<string, string[]> _pathParams;
        private readonly Dictionary<string, string> _license;
        private readonly Func<IContainer> _requestContainerFactory;
        private XUri _publicUriOverride;
        private XUri _serverUri;
        private Hashtable _state;
        private System.Diagnostics.StackTrace _stackTrace = DebugUtil.GetStackTrace();
        private CultureInfo _culture;
        private bool _isTaskDisposed;
        private IContainer _container;
        private TaskEnv _ownerEnv;

        //--- Constructors ---

        /// <summary>
        /// Create instance.
        /// </summary>
        /// <param name="env">Dream Environment.</param>
        /// <param name="verb">Http request verb.</param>
        /// <param name="uri">Request Uri.</param>
        /// <param name="feature">Request handling feature.</param>
        /// <param name="publicUri">Public Uri for incoming request.</param>
        /// <param name="serverUri">Server Uri for Dream Host.</param>
        /// <param name="request">Request message.</param>
        /// <param name="culture">Request Culture.</param>
        /// <param name="requestContainerFactory">Factory delegate to create a request container on demand.</param>
        public DreamContext(IDreamEnvironment env, string verb, XUri uri, DreamFeature feature, XUri publicUri, XUri serverUri, DreamMessage request, CultureInfo culture, Func<IContainer> requestContainerFactory) {
            if(env == null) {
                throw new ArgumentNullException("env");
            }
            if(verb == null) {
                throw new ArgumentNullException("verb");
            }
            if(uri == null) {
                throw new ArgumentNullException("uri");
            }
            if(feature == null) {
                throw new ArgumentNullException("feature");
            }
            if(publicUri == null) {
                throw new ArgumentNullException("publicUri");
            }
            if(request == null) {
                throw new ArgumentNullException("request");
            }
            if(culture == null) {
                throw new ArgumentNullException("culture");
            }
            if(requestContainerFactory == null) {
                throw new ArgumentNullException("requestContainerFactory");
            }
            this.ID = System.Threading.Interlocked.Increment(ref _contextIdCounter);
            this.Env = env;
            this.Verb = verb;
            this.Uri = uri;
            this.Feature = feature;
            this.Feature.ExtractArguments(this.Uri, out _suffixes, out _pathParams);
            this.ServerUri = serverUri;
            this.Request = request;
            this.StartTime = DateTime.UtcNow;
            _publicUri = publicUri;
            _culture = culture;
            _requestContainerFactory = requestContainerFactory;

            // get service license
            _license = CheckServiceLicense();
        }

        private DreamContext(IDreamEnvironment env, string verb, XUri uri, DreamFeature feature, XUri publicUri, XUri serverUri, DreamMessage request, CultureInfo culture, Func<IContainer> requestContainerFactory, Dictionary<string, string> license) {
            if(env == null) {
                throw new ArgumentNullException("env");
            }
            if(verb == null) {
                throw new ArgumentNullException("verb");
            }
            if(uri == null) {
                throw new ArgumentNullException("uri");
            }
            if(feature == null) {
                throw new ArgumentNullException("feature");
            }
            if(publicUri == null) {
                throw new ArgumentNullException("publicUri");
            }
            if(request == null) {
                throw new ArgumentNullException("request");
            }
            if(culture == null) {
                throw new ArgumentNullException("culture");
            }
            if(requestContainerFactory == null) {
                throw new ArgumentNullException("requestContainerFactory");
            }
            this.ID = System.Threading.Interlocked.Increment(ref _contextIdCounter);
            this.Env = env;
            this.Verb = verb;
            this.Uri = uri;
            this.Feature = feature;
            this.Feature.ExtractArguments(this.Uri, out _suffixes, out _pathParams);
            this.ServerUri = serverUri;
            this.Request = request;
            this.StartTime = DateTime.UtcNow;
            _publicUri = publicUri;
            _culture = culture;
            _requestContainerFactory = requestContainerFactory;
            _license = license;
        }

        //--- Properties ---

        /// <summary>
        /// Dream Service handling the request.
        /// </summary>
        public IDreamService Service { get { return Feature.Service; } }

        /// <summary>
        /// <see langword="True"/> if the context has state attached to it.
        /// </summary>
        public bool HasState { get { return _state != null; } }

        /// <summary>
        /// Service license for this request.
        /// </summary>
        public Dictionary<string, string> ServiceLicense { get { return _license; } }

        /// <summary>
        /// <see langword="True"/> if the underlying Task environment has disposed this context.
        /// </summary>
        public bool IsTaskEnvDisposed { get { return _isTaskDisposed; } }

        /// <summary>
        /// Uri by which the host is known publicly in the context of this request.
        /// </summary>
        public XUri PublicUri {
            get {
                return _publicUriOverride ?? _publicUri;
            }
        }

        /// <summary>
        /// Culture of the request.
        /// </summary>
        public CultureInfo Culture {
            get {
                return _culture;
            }
            set {
                if(value == null) {
                    throw new NullReferenceException("value");
                }
                _culture = value;
            }
        }

        /// <summary>
        /// Uri the Dream Host is registered for.
        /// </summary>
        public XUri ServerUri {
            get {
                return _serverUri;
            }
            set {
                if(_serverUri != null) {
                    throw new Exception("server uri already set");
                }
                if(value == null) {
                    throw new ArgumentNullException("value");
                }
                _serverUri = value;
            }
        }

        /// <summary>
        /// User, if any, authenticated for this request.
        /// </summary>
        public IPrincipal User {
            get {
                return GetState<IPrincipal>();
            }
            set {
                SetState(value);
            }
        }

        /// <summary>
        /// Request Inversion of Control container.
        /// </summary>
        public IContainer Container {
            get {
                if(_container == null ) {
                    _container = _requestContainerFactory();
                    var builder = new ContainerBuilder();
                    builder.Register(this).ExternallyOwned();
                    builder.Build(_container);
                }
                return _container;
            }
        }

        /// <summary>
        /// Request State.
        /// </summary>
        private Hashtable State {
            get {
                if(_state == null) {
                    _state = new Hashtable();
                }
                return _state;
            }
        }

        //--- Methods ---

        /// <summary>
        /// Attach the context to the current context.
        /// </summary>
        /// <remarks>
        /// Throws <see cref="DreamContextAccessException"/> if the context is already attached to
        /// a task environemnt of the task environment already has a context.
        /// </remarks>
        /// <exception cref="DreamContextAccessException">Context is either attached to a <see cref="TaskEnv"/> or the current <see cref="TaskEnv"/>
        /// already has a context attached.</exception>
        public void AttachToCurrentTaskEnv() {
            lock(this) {
                var env = TaskEnv.Current;
                if(env.GetState<DreamContext>() != null) {
                    throw new DreamContextAccessException("tried to attach dreamcontext to env that already has a dreamcontext");
                }
                if(_ownerEnv != null && _ownerEnv == env) {
                    throw new DreamContextAccessException("tried to re-attach dreamcontext to env it is already attached to");
                }
                if(_ownerEnv != null) {
                    throw new DreamContextAccessException("tried to attach dreamcontext to an env, when it already is attached to another");
                }
                _ownerEnv = env;
                env.SetState(this);
            }
        }

        /// <summary>
        /// Detach the context from the its task environment.
        /// </summary>
        /// <remarks>
        /// Must be done in the context's task environment.
        /// </remarks>
        public void DetachFromTaskEnv() {
            lock(this) {
                if(TaskEnv.CurrentOrNull != _ownerEnv) {
                    _log.Warn("detaching context in env other than owning end");
                }
                _ownerEnv.RemoveState(this);
                _ownerEnv = null;
            }    
        }

        /// <summary>
        /// Override the <see cref="PublicUri"/> for this request.
        /// </summary>
        /// <param name="publicUri">Publicly accessible Uri.</param>
        public void SetPublicUriOverride(XUri publicUri) {
            _publicUriOverride = publicUri;
        }

        /// <summary>
        /// Remove any <see cref="PublicUri"/> override.
        /// </summary>
        public void ClearPublicUriOverride() {
            _publicUriOverride = null;
        }

        /// <summary>
        /// Number of suffixes for this feature path.
        /// </summary>
        /// <returns></returns>
        public int GetSuffixCount() {
            EnsureFeatureIsSet();
            return _suffixes.Length;
        }

        /// <summary>
        /// Get a suffix.
        /// </summary>
        /// <param name="index">Index of path suffix.</param>
        /// <param name="format">Uri path format.</param>
        /// <returns>Suffix.</returns>
        public string GetSuffix(int index, UriPathFormat format) {
            EnsureFeatureIsSet();
            string suffix = _suffixes[index];
            switch(format) {
            case UriPathFormat.Original:
                return suffix;
            case UriPathFormat.Decoded:
                return XUri.Decode(suffix);
            case UriPathFormat.Normalized:
                return XUri.Decode(suffix).ToLowerInvariant();
            default:
                throw new ArgumentException("format");
            }
        }

        /// <summary>
        /// Get all suffixes.
        /// </summary>
        /// <param name="format">Uri path format for suffixes.</param>
        /// <returns>Array of suffixes.</returns>
        public string[] GetSuffixes(UriPathFormat format) {
            EnsureFeatureIsSet();
            string[] result = new string[_suffixes.Length];
            switch(format) {
            case UriPathFormat.Original:
                for(int i = 0; i < result.Length; ++i) {
                    result[i] = _suffixes[i];
                }
                break;
            case UriPathFormat.Decoded:
                for(int i = 0; i < result.Length; ++i) {
                    result[i] = XUri.Decode(_suffixes[i]);
                }
                break;
            case UriPathFormat.Normalized:
                for(int i = 0; i < result.Length; ++i) {
                    result[i] = XUri.Decode(_suffixes[i]).ToLowerInvariant();
                }
                break;
            default:
                throw new ArgumentException("format");
            }
            return result;
        }

        /// <summary>
        /// Request parameters.
        /// </summary>
        /// <remarks>
        /// Parameters refers to both query and path parameters.
        /// </remarks>
        /// <returns>Array parameter key/value pairs.</returns>
        public KeyValuePair<string, string>[] GetParams() {
            EnsureFeatureIsSet();
            if(Uri.Params != null) {
                int count = _pathParams.Count + Uri.Params.Length;
                List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>(count);
                foreach(KeyValuePair<string, string[]> pair in _pathParams) {
                    foreach(string value in pair.Value) {
                        result.Add(new KeyValuePair<string, string>(pair.Key, value));
                    }
                }
                foreach(KeyValuePair<string, string> pair in Uri.Params) {
                    result.Add(pair);
                }
                return result.ToArray();
            } else {
                return new KeyValuePair<string, string>[0];
            }
        }

        /// <summary>
        /// Get all values for a named parameter.
        /// </summary>
        /// <param name="key"><see cref="DreamFeatureParamAttribute"/> name.</param>
        /// <returns>Text values of parameter.</returns>
        /// <exception cref="DreamAbortException">Throws if parameter does not exist.</exception>
        public string[] GetParams(string key) {
            EnsureFeatureIsSet();
            if(key == null) {
                throw new ArgumentNullException("key");
            }
            string[] values;
            if(!_pathParams.TryGetValue(key, out values) || (values == null)) {
                values = Uri.GetParams(key);
            }
            return values ?? new string[0];
        }

        /// <summary>
        /// Get a named parameter.
        /// </summary>
        /// <remarks>
        /// Will throw <see cref="DreamAbortException"/> if the named parameter does not exist.
        /// </remarks>
        /// <param name="key"><see cref="DreamFeatureParamAttribute"/> name.</param>
        /// <returns>Text value of parameter.</returns>
        /// <exception cref="DreamAbortException">Throws if parameter does not exist.</exception>
        public string GetParam(string key) {
            EnsureFeatureIsSet();
            if(key == null) {
                throw new ArgumentNullException("key");
            }
            string result;
            string[] values;
            _pathParams.TryGetValue(key, out values);
            if((values != null) && (values.Length > 0)) {
                result = values[0];
            } else {
                result = Uri.GetParam(key, null);
            }
            if(result == null) {
                throw new DreamAbortException(DreamMessage.BadRequest(string.Format("missing feature parameter '{0}'", key)));
            }
            return result;
        }

        /// <summary>
        /// Get a named parameter.
        /// </summary>
        /// <typeparam name="T">Type to convert parameter to.</typeparam>
        /// <param name="key"><see cref="DreamFeatureParamAttribute"/> name.</param>
        /// <returns>Parameter value converted to requested type.</returns>
        public T GetParam<T>(string key) {
            string result = GetParam(key);
            try {
                return (T)SysUtil.ChangeType(result, typeof(T));
            } catch {
                throw new DreamAbortException(DreamMessage.BadRequest(string.Format("invalid value for feature parameter '{0}'", key)));
            }
        }

        /// <summary>
        /// Get a named parameter.
        /// </summary>
        /// <param name="key"><see cref="DreamFeatureParamAttribute"/> name.</param>
        /// <param name="def">Default value to return in case parameter is not defined.</param>
        /// <returns>Text value of parameter</returns>
        public string GetParam(string key, string def) {
            EnsureFeatureIsSet();
            if(key == null) {
                throw new ArgumentNullException("key");
            }
            string result;
            string[] values;
            _pathParams.TryGetValue(key, out values);
            if((values != null) && (values.Length > 0)) {
                result = values[0];
            } else {
                result = Uri.GetParam(key, null);
            }
            return result ?? def;
        }

        /// <summary>
        /// Get a named parameter.
        /// </summary>
        /// <typeparam name="T">Type to convert parameter to.</typeparam>
        /// <param name="key"><see cref="DreamFeatureParamAttribute"/> name.</param>
        /// <param name="def">Default value to return in case parameter is not defined.</param>
        /// <returns>Parameter value converted to requested type.</returns>
        public T GetParam<T>(string key, T def) where T : struct {
            string result = GetParam(key, null);
            if(result != null) {
                try {
                    return (T)SysUtil.ChangeType<T>(result);
                } catch {
                    throw new DreamAbortException(DreamMessage.BadRequest(string.Format("invalid value for feature parameter '{0}'", key)));
                }
            }
            return def;
        }

        /// <summary>
        /// Relay a request to another service using the current query parameters, service cookies and verb.
        /// </summary>
        /// <remarks>
        /// Must be yielded by a coroutine or invoked with <see cref="Coroutine.Invoke"/>.
        /// </remarks>
        /// <param name="plug">Location of relay recipient.</param>
        /// <param name="request">Request message to relay.</param>
        /// <param name="response">The <see cref="Result{DreamMessage}"/> instance this coroutine will use as a synchronization handle.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/> execution environment.</returns>
        public IYield Relay(Plug plug, DreamMessage request, Result<DreamMessage> response) {
            return Relay(plug, Verb, request, response);
        }

        /// <summary>
        /// Relay a request to another service using the current query parameters and service cookies.
        /// </summary>
        /// <remarks>
        /// Must be yielded by a coroutine or invoked with <see cref="Coroutine.Invoke"/>.
        /// </remarks>
        /// <param name="plug">Location of relay recipient.</param>
        /// <param name="verb">Http verb to use for relay.</param>
        /// <param name="request">Request message to relay.</param>
        /// <param name="response">The <see cref="Result{DreamMessage}"/> instance this coroutine will use as a synchronization handle.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/> execution environment.</returns>
        public IYield Relay(Plug plug, string verb, DreamMessage request, Result<DreamMessage> response) {

            // combine query parameters of current request with new target URI, then append suffix segments
            Result<DreamMessage> inner = new Result<DreamMessage>(response.Timeout);
            Result result = new Result(TimeSpan.MaxValue);
            Plug.New(plug.Uri.WithParamsFrom(Uri)).InvokeEx(verb, request, inner).WhenDone(delegate {
                response.Return(inner);
                result.Return();
            });
            return result;
        }

        /// <summary>
        /// Get a typed state variable
        /// </summary>
        /// <remarks>Since the type is used as the state key, can only contain one instance for this type. This call is thread-safe.</remarks>
        /// <typeparam name="T">Type of state variable.</typeparam>
        /// <returns>Instance or default for type.</returns>
        public T GetState<T>() {
            lock(State) {
                return (T)(State.ContainsKey(typeof(T)) ? State[typeof(T)] : default(T));
            }
        }

        /// <summary>
        /// Store a typed state variable.
        /// </summary>
        /// <remarks>Since the type is used as the state key, can only contain one instance for this type. This call is thread-safe.</remarks>
        /// <typeparam name="T">Type of state variable.</typeparam>
        /// <param name="value">Instance to store.</param>
        public void SetState<T>(T value) {
            lock(State) {
                State[typeof(T)] = value;
            }
        }

        /// <summary>
        /// Get a typed state variable by key.
        /// </summary>
        /// <remarks>This call is thread-safe.</remarks>
        /// <typeparam name="T">Type of state variable.</typeparam>
        /// <param name="key">State variable key.</param>
        /// <returns>Instance or default for type.</returns>
        public T GetState<T>(string key) {
            lock(State) {
                return (T)(State.ContainsKey(key) ? State[key] : default(T));
            }
        }

        /// <summary>
        /// Store a typed state variable by key.
        /// </summary>
        /// <remarks>This call is thread-safe.</remarks>
        /// <typeparam name="T">Type of state variable.</typeparam>
        /// <param name="key">State variable key.</param>
        /// <param name="value">Instance to store.</param>
        public void SetState<T>(string key, T value) {
            lock(State) {
                State[key] = value;
            }
        }

        /// <summary>
        /// Convert a Uri to a host local Uri, if possible.
        /// </summary>
        /// <remarks>
        /// Will return the original Uri if there is no local equivalent.
        /// </remarks>
        /// <param name="uri">Uri to convert.</param>
        /// <returns>Local Uri.</returns>
        public XUri AsLocalUri(XUri uri) {
            XUri result = uri;
            if(uri.Similarity(PublicUri) == PublicUri.MaxSimilarity) {
                result = uri.ChangePrefix(PublicUri, Env.LocalMachineUri);
            } else if((ServerUri != null) && (uri.Similarity(ServerUri) == ServerUri.MaxSimilarity)) {
                result = uri.ChangePrefix(ServerUri, Env.LocalMachineUri);
            }
            return result;
        }

        /// <summary>
        /// Convert a Uri to uri relative to the requests public uri, if possible.
        /// </summary>
        /// <remarks>
        /// Will return the original Uri if there is no public equivalent.
        /// </remarks>
        /// <param name="uri">Uri to convert.</param>
        /// <returns>Public Uri.</returns>
        public XUri AsPublicUri(XUri uri) {
            XUri result = uri;
            if(uri.Similarity(Env.LocalMachineUri) == Env.LocalMachineUri.MaxSimilarity) {
                result = uri.ChangePrefix(Env.LocalMachineUri, PublicUri);
            }
            return result;
        }

        /// <summary>
        /// Convert a Uri to uri relative to the server's public uri, if possible.
        /// </summary>
        /// <remarks>
        /// Will return the original Uri if there is no public equivalent.
        /// </remarks>
        /// <param name="uri">Uri to convert.</param>
        /// <returns>Public Uri.</returns>
        public XUri AsServerUri(XUri uri) {
            XUri result = uri;
            if((ServerUri != null) && (uri.Similarity(Env.LocalMachineUri) == Env.LocalMachineUri.MaxSimilarity)) {
                result = uri.ChangePrefix(Env.LocalMachineUri, ServerUri);
            }
            return result;
        }

        /// <summary>
        /// Replace the context's own state with a clone of the state of another context.
        /// </summary>
        /// <param name="context"></param>
        public void CloneStateFromContext(DreamContext context) {
            if(context.HasState) {
                lock(context._state) {
                    var state = State;
                    foreach(DictionaryEntry entry in context._state) {
                        var cloneable = entry.Value as ITaskLifespan;
                        state[entry.Key] = (cloneable == null) ? entry.Value : cloneable.Clone();
                    }
                }
            }
        }

        internal DreamContext CreateContext(string verb, XUri uri, DreamFeature feature, DreamMessage message) {
            return new DreamContext(Env, verb, uri, feature, PublicUri, ServerUri, message, Culture, _requestContainerFactory, null);
        }

        private void EnsureFeatureIsSet() {
            if(Feature == null) {
                throw new InvalidOperationException("feature not set");
            }
        }

        private Dictionary<string, string> CheckServiceLicense() {

            // check request validity (unless it's for the @config uri, which is a special case)
            Dictionary<string, string> result = null;
            if((Feature.Service.Self != null) && (Feature.Service is IDreamServiceLicense) && !(Uri.LastSegment ?? string.Empty).EqualsInvariant("@config")) {
                string service_license = ((IDreamServiceLicense)Feature.Service).ServiceLicense;
                if(string.IsNullOrEmpty(service_license)) {
                    throw new DreamAbortException(DreamMessage.LicenseRequired("service-license missing"));
                }

                // extract public RSA key for validation
                RSACryptoServiceProvider public_key = RSAUtil.ProviderFrom(Feature.Service.GetType().Assembly);
                if(public_key == null) {
                    throw new DreamAbortException(DreamMessage.InternalError("service assembly invalid"));
                }

                // validate the service-license
                try {

                    // parse service-license
                    result = HttpUtil.ParseNameValuePairs(service_license);
                    if(!Encoding.UTF8.GetBytes(service_license.Substring(0, service_license.LastIndexOf(','))).VerifySignature(result["dsig"], public_key)) {
                        throw new DreamAbortException(DreamMessage.InternalError("invalid service-license"));
                    }
                } catch(Exception e) {

                    // unexpected error, blame it on the license
                    if(e is DreamAbortException) {
                        throw;
                    } else {
                        throw new DreamAbortException(DreamMessage.InternalError("corrupt service-license"));
                    }
                }

                // check license
                string text;
                if((!result.TryGetValue("licensee", out text) || string.IsNullOrEmpty(text)) && !result.ContainsKey("expire")) {

                    // unexpected error, blame it on the license
                    throw new DreamAbortException(DreamMessage.InternalError("corrupt service-license"));
                }

                // determine 'now' date-time
                DateTime now = DateTime.UtcNow;
                DateTime? request_date = Request.Headers.Date;
                if(request_date.HasValue) {
                    now = (request_date.Value > now) ? request_date.Value : now;
                }

                // check expiration
                DateTime expire;
                if(result.TryGetValue("expire", out text) && (!DateTimeUtil.TryParseInvariant(text, out expire) || (expire.ToUniversalTime() < now))) {
                    throw new DreamAbortException(DreamMessage.LicenseRequired("service-license has expired"));
                }
            }
            return result;
        }

        #region ITaskLifespan Members

        object ITaskLifespan.Clone() {
            var context = new DreamContext(Env, Verb, Uri, Feature, _publicUri, _serverUri, Request, _culture, _requestContainerFactory, _license);
            context.CloneStateFromContext(this);
            return context;
        }

        void ITaskLifespan.Dispose() {
            if(_isTaskDisposed) {
                _log.Warn("disposing already disposed context");
            }
            _isTaskDisposed = true;
            if(_container != null) {
                _container.Dispose();
                _container = null;
            }
            if(_state == null) {
                return;
            }
            lock(_state) {
                foreach(var item in _state.Values) {
                    var disposable = item as ITaskLifespan;
                    if(disposable == null) {
                        continue;
                    }
                    disposable.Dispose();
                }
                _state.Clear();
            }
        }
        #endregion
    }
}
