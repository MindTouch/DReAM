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
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;

using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream {
    using Yield = IEnumerator<IYield>;

    /// <summary>
    /// Provides a contract for intercepting and modifying <see cref="Plug"/> requests and responses in the invocation pipeline.
    /// </summary>
    /// <param name="verb">Verb of the intercepted invocation.</param>
    /// <param name="uri">Uri of the intercepted invocation.</param>
    /// <param name="normalizedUri">Normalized version of the uri of the intercepted invocation.</param>
    /// <param name="message">Message of the intercepted invocation.</param>
    /// <returns>The message to return as the result of the interception.</returns>
    public delegate DreamMessage PlugHandler(string verb, XUri uri, XUri normalizedUri, DreamMessage message);

    /// <summary>
    /// Provides a fluent, immutable interface for building request/response invocation  against a resource. Mostly used as an interface
    /// for making Http requests, but can be extended for any resource that can provide request/response semantics.
    /// </summary>
    public class Plug {

        //--- Constants ---

        /// <summary>
        /// Default number of redirects plug uses when no value is specified.
        /// </summary>
        public const ushort DEFAULT_MAX_AUTO_REDIRECTS = 50;

        /// <summary>
        /// Base score normal priorty <see cref="IPlugEndpoint"/> implementations should use to signal a successful match.
        /// </summary>
        public const int BASE_ENDPOINT_SCORE = int.MaxValue / 2;
        
        /// <summary>
        /// Default timeout of 60 seconds for <see cref="Plug"/> invocations.
        /// </summary>
        public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(60);

        //--- Class Fields ---

        /// <summary>
        /// Default, shared cookie jar for all plugs.
        /// </summary>
        public static DreamCookieJar GlobalCookies = new DreamCookieJar();

        private static log4net.ILog _log = LogUtils.CreateLog();
        private static List<IPlugEndpoint> _endpoints = new List<IPlugEndpoint>();

        //--- Class Constructors ---
        static Plug() {

            // let's find all IPlugEndpoint derived, concrete classes
            foreach(Type type in typeof(Plug).Assembly.GetTypes()) {
                if(typeof(IPlugEndpoint).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && !type.IsGenericTypeDefinition) {
                    ConstructorInfo ctor = type.GetConstructor(System.Type.EmptyTypes);
                    if(ctor != null) {
                        AddEndpoint((IPlugEndpoint)ctor.Invoke(null));
                    }
                }
            }
        }

        //--- Class Operators ---

        /// <summary>
        /// Implicit conversion operator for casting a <see cref="Plug"/> to a <see cref="XUri"/>.
        /// </summary>
        /// <param name="plug">Plug instance to convert.</param>
        /// <returns>New uri instance.</returns>
        public static implicit operator XUri(Plug plug) {
            return (plug != null) ? plug.Uri : null;
        }

        //--- Class Methods ---

        /// <summary>
        /// Create a new <see cref="Plug"/> instance from a uri string.
        /// </summary>
        /// <param name="uri">Uri string.</param>
        /// <returns>New plug instance.</returns>
        public static Plug New(string uri) {
            return New(uri, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Create a new <see cref="Plug"/> instance from a uri string.
        /// </summary>
        /// <param name="uri">Uri string.</param>
        /// <param name="timeout">Invocation timeout.</param>
        /// <returns>New plug instance.</returns>
        public static Plug New(string uri, TimeSpan timeout) {
            if(uri != null) {
                return new Plug(new XUri(uri), timeout, null, null, null, null, null, DEFAULT_MAX_AUTO_REDIRECTS);
            }
            return null;
        }

        /// <summary>
        /// Create a new <see cref="Plug"/> instance from a <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">Uri instance.</param>
        /// <returns>New plug instance.</returns>
        public static Plug New(Uri uri) {
            return New(uri, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Create a new <see cref="Plug"/> instance from a <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">Uri instance.</param>
        /// <param name="timeout">Invocation timeout.</param>
        /// <returns>New plug instance.</returns>
        public static Plug New(Uri uri, TimeSpan timeout) {
            if(uri != null) {
                return new Plug(new XUri(uri), timeout, null, null, null, null, null, DEFAULT_MAX_AUTO_REDIRECTS);
            }
            return null;
        }

        /// <summary>
        /// Create a new <see cref="Plug"/> instance from a <see cref="XUri"/>.
        /// </summary>
        /// <param name="uri">Uri instance.</param>
        /// <returns>New plug instance.</returns>
        public static Plug New(XUri uri) {
            return New(uri, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Create a new <see cref="Plug"/> instance from a <see cref="XUri"/>.
        /// </summary>
        /// <param name="uri">Uri instance.</param>
        /// <param name="timeout">Invocation timeout.</param>
        /// <returns>New plug instance.</returns>
        public static Plug New(XUri uri, TimeSpan timeout) {
            if(uri != null) {
                return new Plug(uri, timeout, null, null, null, null, null, DEFAULT_MAX_AUTO_REDIRECTS);
            }
            return null;
        }

        /// <summary>
        /// Manually add a plug endpoint for handling invocations.
        /// </summary>
        /// <param name="endpoint">Factory instance to add.</param>
        public static void AddEndpoint(IPlugEndpoint endpoint) {
            lock(_endpoints) {
                _endpoints.Add(endpoint);
            }
        }

        /// <summary>
        /// Manually remove a plug endpoint from the handler pool.
        /// </summary>
        /// <param name="endpoint">Factory instance to remove.</param>
        public static void RemoveEndpoint(IPlugEndpoint endpoint) {
            lock(_endpoints) {
                _endpoints.Remove(endpoint);
            }
        }

        /// <summary>
        /// Blocks on a Plug synchronization handle to wait for it ti complete and confirm that it's a non-error response.
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="result">Plug synchronization handle.</param>
        /// <returns>Successful reponse message.</returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public static DreamMessage WaitAndConfirm(Result<DreamMessage> result) {

            // NOTE (steveb): we don't need to set a time-out since 'Memorize()' already guarantees eventual termination

            result.Wait().Memorize(new Result(TimeSpan.MaxValue)).Wait();
            DreamMessage message = result.Value;
            if(!message.IsSuccessful) {
                throw new DreamResponseException(message);
            }
            return message;
        }

        private static DreamMessage PreProcess(string verb, XUri uri, XUri normalizedUri, DreamHeaders headers, DreamCookieJar cookies, DreamMessage message) {

            // check if plug is running in the context of a service
            DreamContext context = DreamContext.CurrentOrNull;
            if(context != null) {

                // set request id header
                message.Headers.DreamRequestId = context.GetState<string>(DreamHeaders.DREAM_REQUEST_ID);

                // set dream service header
                if(context.Service.Self != null) {
                    message.Headers.DreamService = context.AsPublicUri(context.Service.Self).ToString();
                }

                // check if uri is local://
                if(normalizedUri.Scheme.EqualsInvariant("local")) {
                    DreamUtil.AppendHeadersToInternallyForwardedMessage(context.Request, message);
                }
            }

            if(cookies != null) {
                lock(cookies) {
                    message.Cookies.AddRange(cookies.Fetch(uri));
                }
            }

            // transfer plug headers
            message.Headers.AddRange(headers);
            return message;
        }

        private static DreamMessage PostProcess(string verb, XUri uri, XUri normalizedUri, DreamHeaders headers, DreamCookieJar cookies, DreamMessage message) {

            // check if we received cookies
            if(message.HasCookies) {
                DreamContext context = DreamContext.CurrentOrNull;

                // add matching cookies to service or to global cookie jar
                if(cookies != null) {
                    lock(cookies) {
                        if(!StringUtil.EqualsInvariant(uri.Scheme, "local") && StringUtil.EqualsInvariant(normalizedUri.Scheme, "local")) {

                            // need to translate cookies as they leave the dreamcontext
                            cookies.Update(DreamCookie.ConvertToPublic(message.Cookies), uri);
                        } else {
                            cookies.Update(message.Cookies, uri);
                        }
                    }
                }
            }
            return message;
        }

        private static int FindPlugEndpoint(XUri uri, out IPlugEndpoint match, out XUri normalizedUri) {
            match = null;
            normalizedUri = null;

            // determine which plug factory has the best match
            int maxScore = 0;
            lock(_endpoints) {

                // loop over all plug factories to determine best transport mechanism
                foreach(IPlugEndpoint factory in _endpoints) {
                    XUri newNormalizedUri;
                    int score = factory.GetScoreWithNormalizedUri(uri, out newNormalizedUri);
                    if(score > maxScore) {
                        maxScore = score;
                        normalizedUri = newNormalizedUri;
                        match = factory;
                    }
                }
            }
            return maxScore;
        }

        //--- Fields ---

        /// <summary>
        /// Uri of the instance.
        /// </summary>
        public readonly XUri Uri;

        /// <summary>
        /// Timeout for invocation.
        /// </summary>
        public readonly TimeSpan Timeout;

        /// <summary>
        /// If not null, the creditials to use for the invocation.
        /// </summary>
        public readonly ICredentials Credentials;

        // BUGBUGBUG (steveb): _headers needs to be read-only
        private readonly DreamHeaders _headers;

        // BUGBUGBUG (steveb): _preHandlers, _postHandlers need to be read-only
        private readonly List<PlugHandler> _preHandlers;
        private readonly List<PlugHandler> _postHandlers;

        private readonly DreamCookieJar _cookieJarOverride = null;
        private readonly ushort _maxAutoRedirects = 0;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="uri">Uri to the resource to make the request against.</param>
        /// <param name="timeout">Invocation timeout.</param>
        /// <param name="headers">Header collection for request.</param>
        /// <param name="preHandlers">Optional pre-invocation handlers.</param>
        /// <param name="postHandlers">Optional post-invocation handlers.</param>
        /// <param name="credentials">Optional request credentials.</param>
        /// <param name="cookieJarOverride">Optional cookie jar to override global jar shared by <see cref="Plug"/> instances.</param>
        /// <param name="maxAutoRedirects">Maximum number of redirects to follow, 0 if non redirects should be followed.</param>
        public Plug(XUri uri, TimeSpan timeout, DreamHeaders headers, List<PlugHandler> preHandlers, List<PlugHandler> postHandlers, ICredentials credentials, DreamCookieJar cookieJarOverride, ushort maxAutoRedirects) {
            if(uri == null) {
                throw new ArgumentNullException("uri");
            }
            this.Uri = uri;
            this.Timeout = timeout;
            this.Credentials = credentials;
            _headers = headers;
            _preHandlers = preHandlers;
            _postHandlers = postHandlers;
            _cookieJarOverride = cookieJarOverride;
            _maxAutoRedirects = maxAutoRedirects;
        }

        //--- Properties ---

        /// <summary>
        /// Request header collection.
        /// </summary>
        public DreamHeaders Headers { get { return _headers; } }

        /// <summary>
        /// Pre-invocation handlers.
        /// </summary>
        public PlugHandler[] PreHandlers { get { return (_preHandlers != null) ? _preHandlers.ToArray() : null; } }

        /// <summary>
        /// Post-invocation handlers.
        /// </summary>
        public PlugHandler[] PostHandlers { get { return (_postHandlers != null) ? _postHandlers.ToArray() : null; } }

        /// <summary>
        /// Cookie jar for the request.
        /// </summary>
        public DreamCookieJar CookieJar {
            get {

                // Note (arnec): In order for the override to not block the environment, we always run this logic to get at the
                // plug's cookie jar rather than assigning the resulting value to _cookieJarOverride
                if(_cookieJarOverride != null) {
                    return _cookieJarOverride;
                }
                DreamContext context = DreamContext.CurrentOrNull;
                return ((context != null) && (context.Service.Cookies != null)) ? context.Service.Cookies : GlobalCookies;
            }
        }

        /// <summary>
        /// True if this plug will automatically follow redirects (301,302 &amp; 307).
        /// </summary>
        public bool AutoRedirect { get { return _maxAutoRedirects > 0; } }

        /// <summary>
        /// Maximum number of redirect to follow before giving up.
        /// </summary>
        public ushort MaxAutoRedirects { get { return _maxAutoRedirects; } }

        //--- Methods ---

        /// <summary>
        /// Create a copy of the instance with new path segments appended to its Uri.
        /// </summary>
        /// <param name="segments">Segements to add.</param>
        /// <returns>New instance.</returns>
        public Plug At(params string[] segments) {
            if(segments.Length == 0) {
                return this;
            }
            return new Plug(Uri.At(segments), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a path/query/fragement appended to its Uri.
        /// </summary>
        /// <param name="path">Path/Query/fragment string.</param>
        /// <returns>New instance.</returns>
        public Plug AtPath(string path) {
            return new Plug(Uri.AtPath(path), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, string value) {
            return new Plug(Uri.With(key, value), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, bool value) {
            return new Plug(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, int value) {
            return new Plug(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, long value) {
            return new Plug(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, decimal value) {
            return new Plug(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, double value) {
            return new Plug(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, DateTime value) {
            return new Plug(Uri.With(key, value.ToUniversalTime().ToString("R")), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with additional query parameters.
        /// </summary>
        /// <param name="args">Array of query key/value pairs.</param>
        /// <returns>New instance.</returns>
        public Plug WithParams(KeyValuePair<string, string>[] args) {
            return new Plug(Uri.WithParams(args), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with the provided querystring added.
        /// </summary>
        /// <param name="query">Query string.</param>
        /// <returns>New instance.</returns>
        public Plug WithQuery(string query) {
            return new Plug(Uri.WithQuery(query), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with parameters from another uri added.
        /// </summary>
        /// <param name="uri">Uri to extract parameters from.</param>
        /// <returns>New instance.</returns>
        public Plug WithParamsFrom(XUri uri) {
            return new Plug(Uri.WithParamsFrom(uri), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with the given credentials
        /// </summary>
        /// <remarks>
        /// Using the user/password signature will always try to send a basic auth header. If negotiation of auth method is desired
        /// (i.e. digest auth may be an option), use <see cref="WithCredentials(System.Net.ICredentials)"/> instead.
        /// </remarks>
        /// <param name="user">User.</param>
        /// <param name="password">Password.</param>
        /// <returns>New instance.</returns>
        public Plug WithCredentials(string user, string password) {
            return new Plug(Uri.WithCredentials(user, password), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with the given credentials
        /// </summary>
        /// <param name="credentials">Credential instance.</param>
        /// <returns>New instance.</returns>
        public Plug WithCredentials(ICredentials credentials) {
            return new Plug(Uri, Timeout, _headers, _preHandlers, _postHandlers, credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with credentials removed.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithoutCredentials() {
            return new Plug(Uri.WithoutCredentials(), Timeout, _headers, _preHandlers, _postHandlers, null, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with an override cookie jar.
        /// </summary>
        /// <param name="cookieJar">Cookie jar to use.</param>
        /// <returns>New instance.</returns>
        public Plug WithCookieJar(DreamCookieJar cookieJar) {
            return new Plug(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, cookieJar, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with any override cookie jar removed.
        /// </summary>
        /// <remarks>Will fall back on <see cref="DreamContext"/> or global jar.</remarks>
        /// <returns>New instance.</returns>
        public Plug WithoutCookieJar() {
            return new Plug(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, null, MaxAutoRedirects);
        }

        /// <summary>
        /// Turn on auto redirect behavior with the <see cref="DEFAULT_MAX_AUTO_REDIRECTS"/> number of redirects to follow.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithAutoRedirects() {
            return new Plug(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, DEFAULT_MAX_AUTO_REDIRECTS);
        }

        /// <summary>
        /// Turn on auto redirect behavior with the specified number of redirects.
        /// </summary>
        /// <param name="maxRedirects">Maximum number of redirects to follow before giving up.</param>
        /// <returns>New instance.</returns>
        public Plug WithAutoRedirects(ushort maxRedirects) {
            return new Plug(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, maxRedirects);
        }

        /// <summary>
        /// Turn off auto-redirect behavior.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithoutAutoRedirects() {
            return new Plug(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, 0);
        }

        /// <summary>
        /// Create a copy of the instance with a header added.
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value.</param>
        /// <returns>New instance.</returns>
        public Plug WithHeader(string name, string value) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }
            if(value == null) {
                throw new ArgumentNullException("value");
            }
            DreamHeaders newHeaders = new DreamHeaders(_headers);
            newHeaders.Add(name, value);
            return new Plug(Uri, Timeout, newHeaders, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a header collection added.
        /// </summary>
        /// <param name="headers">Header collection</param>
        /// <returns>New instance.</returns>
        public Plug WithHeaders(DreamHeaders headers) {
            if(headers != null) {
                DreamHeaders newHeaders = new DreamHeaders(_headers);
                newHeaders.AddRange(headers);
                return new Plug(Uri, Timeout, newHeaders, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
            }
            return this;
        }

        /// <summary>
        /// Create a copy of the instance with a header removed.
        /// </summary>
        /// <param name="name">Name of the header to remove.</param>
        /// <returns>New instance.</returns>
        public Plug WithoutHeader(string name) {
            DreamHeaders newHeaders = null;
            if(_headers != null) {
                newHeaders = new DreamHeaders(_headers);
                newHeaders.Remove(name);
                if(newHeaders.Count == 0) {
                    newHeaders = null;
                }
            }
            return new Plug(Uri, Timeout, newHeaders, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with all headers removed.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithoutHeaders() {
            return new Plug(Uri, Timeout, null, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a pre-invocation handler added.
        /// </summary>
        /// <param name="preHandlers">Pre-invocation handler.</param>
        /// <returns>New instance.</returns>
        public Plug WithPreHandler(params PlugHandler[] preHandlers) {
            List<PlugHandler> list = (_preHandlers != null) ? new List<PlugHandler>(_preHandlers) : new List<PlugHandler>();
            list.AddRange(preHandlers);
            return new Plug(Uri, Timeout, _headers, list, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a post-invocation handler added.
        /// </summary>
        /// <param name="postHandlers">Post-invocation handler.</param>
        /// <returns>New instance.</returns>
        public Plug WithPostHandler(params PlugHandler[] postHandlers) {
            List<PlugHandler> list = new List<PlugHandler>(postHandlers);
            if(_postHandlers != null) {
                list.AddRange(_postHandlers);
            }
            return new Plug(Uri, Timeout, _headers, _preHandlers, list, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with all handlers removed.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithoutHandlers() {
            return new Plug(Uri, Timeout, _headers, null, null, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a new timeout.
        /// </summary>
        /// <param name="timeout">Invocation timeout.</param>
        /// <returns>New instance.</returns>
        public Plug WithTimeout(TimeSpan timeout) {
            return new Plug(Uri, timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a trailing slash.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithTrailingSlash() {
            return new Plug(Uri.WithTrailingSlash(), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance without a trailing slash.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithoutTrailingSlash() {
            return new Plug(Uri.WithoutTrailingSlash(), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Turn on double-encoding of segments when the Plug's <see cref="Uri"/> is converted to a <see cref="System.Uri"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithSegmentDoubleEncoding() {
            return new Plug(Uri.WithSegmentDoubleEncoding(), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Turn off double-encoding of segments when the Plug's <see cref="Uri"/> is converted to a <see cref="System.Uri"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithoutSegmentDoubleEncoding() {
            return new Plug(Uri.WithoutSegmentDoubleEncoding(), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Provide a string representation of the Uri of the instance.
        /// </summary>
        /// <returns>Uri string.</returns>
        public override string ToString() {
            return Uri.ToString();
        }

        #region --- Blocking Methods ---

        /// <summary>
        /// Blocking version of <see cref="Post(MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage Post() {
            return WaitAndConfirm(Invoke(Verb.POST, DreamMessage.Ok(XDoc.Empty), new Result<DreamMessage>(TimeSpan.MaxValue)));
        }

        /// <summary>
        /// Blocking version of <see cref="Post(MindTouch.Xml.XDoc,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="doc"></param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage Post(XDoc doc) {
            return WaitAndConfirm(Invoke(Verb.POST, DreamMessage.Ok(doc), new Result<DreamMessage>(TimeSpan.MaxValue)));
        }

        /// <summary>
        /// Blocking version of <see cref="Post(MindTouch.Dream.DreamMessage,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="message"></param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage Post(DreamMessage message) {
            return WaitAndConfirm(Invoke(Verb.POST, message, new Result<DreamMessage>(TimeSpan.MaxValue)));
        }

        /// <summary>
        /// Blocking version of <see cref="PostAsForm(MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage PostAsForm() {
            DreamMessage message = DreamMessage.Ok(Uri.Params);
            XUri uri = Uri.WithoutParams();
            return WaitAndConfirm(new Plug(uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects).Invoke(Verb.POST, message, new Result<DreamMessage>(TimeSpan.MaxValue)));
        }

        /// <summary>
        /// Blocking version of <see cref="Put(MindTouch.Xml.XDoc,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="doc"></param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage Put(XDoc doc) {
            return WaitAndConfirm(Invoke(Verb.PUT, DreamMessage.Ok(doc), new Result<DreamMessage>(TimeSpan.MaxValue)));
        }

        /// <summary>
        /// Blocking version of <see cref="Put(MindTouch.Dream.DreamMessage,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="message"></param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage Put(DreamMessage message) {
            return WaitAndConfirm(Invoke(Verb.PUT, message, new Result<DreamMessage>(TimeSpan.MaxValue)));
        }

        /// <summary>
        /// Blocking version of <see cref="Get(MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage Get() {
            return WaitAndConfirm(Invoke(Verb.GET, DreamMessage.Ok(), new Result<DreamMessage>(TimeSpan.MaxValue)));
        }

        /// <summary>
        /// Blocking version of <see cref="Get(MindTouch.Dream.DreamMessage,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="message"></param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage Get(DreamMessage message) {
            return WaitAndConfirm(Invoke(Verb.GET, message, new Result<DreamMessage>(TimeSpan.MaxValue)));
        }

        /// <summary>
        /// Blocking version of <see cref="Head(MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage Head() {
            return WaitAndConfirm(Invoke(Verb.HEAD, DreamMessage.Ok(), new Result<DreamMessage>(TimeSpan.MaxValue)));
        }

        /// <summary>
        /// Blocking version of <see cref="Options(MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage Options() {
            return WaitAndConfirm(Invoke(Verb.OPTIONS, DreamMessage.Ok(), new Result<DreamMessage>(TimeSpan.MaxValue)));
        }

        /// <summary>
        /// Blocking version of <see cref="Delete(MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage Delete() {
            return WaitAndConfirm(Invoke(Verb.DELETE, DreamMessage.Ok(), new Result<DreamMessage>(TimeSpan.MaxValue)));
        }

        /// <summary>
        /// Blocking version of <see cref="Delete(MindTouch.Xml.XDoc,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="doc"></param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage Delete(XDoc doc) {
            return WaitAndConfirm(Invoke(Verb.DELETE, DreamMessage.Ok(doc), new Result<DreamMessage>(TimeSpan.MaxValue)));
        }

        /// <summary>
        /// Blocking version of <see cref="Delete(MindTouch.Dream.DreamMessage,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="message"></param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage Delete(DreamMessage message) {
            return WaitAndConfirm(Invoke(Verb.DELETE, message, new Result<DreamMessage>(TimeSpan.MaxValue)));
        }

        /// <summary>
        /// Blocking version of <see cref="Invoke(string,MindTouch.Dream.DreamMessage,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="verb"></param>
        /// <param name="message"></param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage Invoke(string verb, DreamMessage message) {
            return WaitAndConfirm(Invoke(verb, message, new Result<DreamMessage>(TimeSpan.MaxValue)));
        }
        #endregion

        #region --- Iterative Methods ---

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb and an empty message.
        /// </summary>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> Post(Result<DreamMessage> result) {
            return Invoke(Verb.POST, DreamMessage.Ok(), result);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb.
        /// </summary>
        /// <param name="doc">Document to send.</param>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> Post(XDoc doc, Result<DreamMessage> result) {
            return Invoke(Verb.POST, DreamMessage.Ok(doc), result);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> Post(DreamMessage message, Result<DreamMessage> result) {
            return Invoke(Verb.POST, message, result);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb with <see cref="Verb.GET"/> query arguments converted as form post body.
        /// </summary>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> PostAsForm(Result<DreamMessage> result) {
            DreamMessage message = DreamMessage.Ok(Uri.Params);
            XUri uri = Uri.WithoutParams();
            return new Plug(uri, Timeout, Headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects).Invoke(Verb.POST, message, result);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.PUT"/> verb.
        /// </summary>
        /// <param name="doc">Document to send.</param>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> Put(XDoc doc, Result<DreamMessage> result) {
            return Invoke(Verb.PUT, DreamMessage.Ok(doc), result);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.PUT"/> verb.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> Put(DreamMessage message, Result<DreamMessage> result) {
            return Invoke(Verb.PUT, message, result);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.GET"/> verb and no message body.
        /// </summary>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> Get(Result<DreamMessage> result) {
            return Invoke(Verb.GET, DreamMessage.Ok(), result);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.GET"/> verb.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> Get(DreamMessage message, Result<DreamMessage> result) {
            return Invoke(Verb.GET, message, result);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.HEAD"/> verb and no message body.
        /// </summary>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> Head(Result<DreamMessage> result) {
            return Invoke(Verb.HEAD, DreamMessage.Ok(), result);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.OPTIONS"/> verb and no message body.
        /// </summary>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> Options(Result<DreamMessage> result) {
            return Invoke(Verb.OPTIONS, DreamMessage.Ok(), result);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.DELETE"/> verb and no message body.
        /// </summary>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> Delete(Result<DreamMessage> result) {
            return Invoke(Verb.DELETE, DreamMessage.Ok(), result);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.DELETE"/> verb.
        /// </summary>
        /// <param name="doc">Document to send.</param>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> Delete(XDoc doc, Result<DreamMessage> result) {
            return Invoke(Verb.DELETE, DreamMessage.Ok(doc), result);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.DELETE"/> verb.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> Delete(DreamMessage message, Result<DreamMessage> result) {
            return Invoke(Verb.DELETE, message, result);
        }

        /// <summary>
        /// Invoke the plug.
        /// </summary>
        /// <param name="verb">Request verb.</param>
        /// <param name="request">Request message.</param>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> Invoke(string verb, DreamMessage request, Result<DreamMessage> result) {

            // Note (arnec): Plug never throws, so we remove the timeout from the result (if it has one), 
            // and pass it into our coroutine manually.
            var timeout = result.Timeout;
            if(timeout != TimeSpan.MaxValue) {
                result.Timeout = TimeSpan.MaxValue;
            }
            return Coroutine.Invoke(Invoke_Helper, verb, request, timeout, result);
        }

        private Yield Invoke_Helper(string verb, DreamMessage request, TimeSpan timeout, Result<DreamMessage> response) {
            DreamMessage message = null;
            var hasTimeout = timeout != TimeSpan.MaxValue;
            var requestTimer = Stopwatch.StartNew();
            yield return InvokeEx(verb, request, new Result<DreamMessage>(timeout)).Set(v => message = v);
            requestTimer.Stop();
            if(hasTimeout) {
                timeout = timeout - requestTimer.Elapsed;
            }
            Result memorize;
            yield return memorize = message.Memorize(new Result(timeout)).Catch();
            if(memorize.HasException) {
                var status = DreamStatus.ResponseFailed;
                if(memorize.HasTimedOut) {
                    status = DreamStatus.ResponseDataTransferTimeout;
                }
                response.Return(new DreamMessage(status, null, new XException(memorize.Exception)));
            } else {
                response.Return(message);
            }
        }

        /// <summary>
        /// Invoke the plug, but leave the stream unread so that the returned <see cref="DreamMessage"/> can be streamed.
        /// </summary>
        /// <param name="verb">Request verb.</param>
        /// <param name="request">Request message.</param>
        /// <param name="result">The <see cref="Result{DreamMessage}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public Result<DreamMessage> InvokeEx(string verb, DreamMessage request, Result<DreamMessage> result) {
            if(verb == null) {
                throw new ArgumentNullException("verb");
            }
            if(request == null) {
                throw new ArgumentNullException("request");
            }
            if(request.Status != DreamStatus.Ok) {
                throw new ArgumentException("request status must be 200 (Ok)");
            }
            if(result == null) {
                throw new ArgumentNullException("response");
            }

            // determine which factory has the best match
            IPlugEndpoint match;
            XUri normalizedUri;
            FindPlugEndpoint(Uri, out match, out normalizedUri);

            // check if we found a match
            if(match == null) {
                request.Close();
                result.Return(new DreamMessage(DreamStatus.NoEndpointFound, null, XDoc.Empty));
                return result;
            }

            // add matching cookies from service or from global cookie jar
            DreamCookieJar cookies = CookieJar;

            // prepare request
            try {
                request = PreProcess(verb, Uri, normalizedUri, _headers, cookies, request);

                // check if custom pre-processing handlers are registered
                if(_preHandlers != null) {
                    foreach(var handler in _preHandlers) {
                        request = handler(verb, Uri, normalizedUri, request) ?? new DreamMessage(DreamStatus.RequestIsNull, null, XDoc.Empty);
                        if(request.Status != DreamStatus.Ok) {
                            result.Return(request);
                            return result;
                        }
                    }
                }
            } catch(Exception e) {
                request.Close();
                result.Return(new DreamMessage(DreamStatus.RequestFailed, null, new XException(e)));
                return result;
            }

            // Note (arnec): Plug never throws, so we usurp the passed result if it has a timeout
            // setting the result timeout on inner result manually
            var outerTimeout = result.Timeout;
            if(outerTimeout != TimeSpan.MaxValue) {
                result.Timeout = TimeSpan.MaxValue;
            }

            // if the governing result has a shorter timeout than the plug, it superceeds the plug timeout
            var timeout = outerTimeout < Timeout ? outerTimeout : Timeout;

            // prepare response handler
            var inner = new Result<DreamMessage>(timeout, TaskEnv.None).WhenDone(
                v => {
                    try {
                        var message = PostProcess(verb, Uri, normalizedUri, _headers, cookies, v);

                        // check if custom post-processing handlers are registered
                        if((message.Status == DreamStatus.MovedPermanently ||
                            message.Status == DreamStatus.Found ||
                            message.Status == DreamStatus.TemporaryRedirect) &&
                           AutoRedirect &&
                           request.IsCloneable
                        ) {
                            var redirectPlug = new Plug(message.Headers.Location, Timeout, Headers, null, null, null, CookieJar, (ushort)(MaxAutoRedirects - 1));
                            var redirectMessage = request.Clone();
                            request.Close();
                            redirectPlug.InvokeEx(verb, redirectMessage, new Result<DreamMessage>()).WhenDone(result.Return);
                        } else {
                            request.Close();
                            if(_postHandlers != null) {
                                foreach(var handler in _postHandlers) {
                                    message = handler(verb, Uri, normalizedUri, message) ?? new DreamMessage(DreamStatus.ResponseIsNull, null, XDoc.Empty);
                                }
                            }
                            result.Return(message);
                        }
                    } catch(Exception e) {
                        request.Close();
                        result.Return(new DreamMessage(DreamStatus.ResponseFailed, null, new XException(e)));
                    }
                },
                e => {

                    // an exception occurred somewhere during processing (not expected, but it could happen)
                    request.Close();
                    var status = DreamStatus.RequestFailed;
                    if(e is TimeoutException) {
                        status = DreamStatus.RequestConnectionTimeout;
                    }
                    result.Return(new DreamMessage(status, null, new XException(e)));
                }
            );

            // invoke message handler
            Coroutine.Invoke(match.Invoke, this, verb, normalizedUri, request, inner);
            return result;
        }
        #endregion

        #region --- Obsolete Methods ---

        /// <summary>
        /// This method is deprecated. Please use <see cref="WithParams(System.Collections.Generic.KeyValuePair{string,string}[])"/> instead.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [Obsolete("Please use 'WithParams(KeyValuePair<string, string>[] args)' instead")]
        public Plug WithParams(NameValueCollection args) {
#pragma warning disable 618
            return new Plug(Uri.WithParams(args), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
#pragma warning restore 618
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="WithHeaders(MindTouch.Dream.DreamHeaders)"/> instead.
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        [Obsolete("Please use 'WithHeaders(DreamHeaders)' instead")]
        public Plug WithHeaders(NameValueCollection headers) {
            if(headers != null) {
                DreamHeaders newHeaders = new DreamHeaders(_headers);
                newHeaders.AddRange(headers);
                return new Plug(Uri, Timeout, newHeaders, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
            }
            return this;
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="Post(MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <returns></returns>
        [Obsolete("PostAsync() is obsolete. Please use Post(Result<DreamMessage>) instead.")]
        public Result<DreamMessage> PostAsync() {
            return Invoke(Verb.POST, DreamMessage.Ok(), new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="Post(MindTouch.Xml.XDoc,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        [Obsolete("PostAsync(XDoc) is obsolete. Please use Post(XDoc, Result<DreamMessage>) instead.")]
        public Result<DreamMessage> PostAsync(XDoc doc) {
            return Invoke(Verb.POST, DreamMessage.Ok(doc), new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="Post(MindTouch.Dream.DreamMessage,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [Obsolete("PostAsync(DreamMessage) is obsolete. Please use Post(DreamMessage, Result<DreamMessage>) instead.")]
        public Result<DreamMessage> PostAsync(DreamMessage message) {
            return Invoke(Verb.POST, message, new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="PostAsForm(MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <returns></returns>
        [Obsolete("PostAsFormAsync() is obsolete. Please use PostAsForm(Result<DreamMessage>) instead.")]
        public Result<DreamMessage> PostAsFormAsync() {
            DreamMessage message = DreamMessage.Ok(Uri.Params);
            XUri uri = Uri.WithoutParams();
            return new Plug(uri, Timeout, Headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects).Invoke(Verb.POST, message, new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="Put(MindTouch.Xml.XDoc,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        [Obsolete("PutAsync(XDoc) is obsolete. Please use Put(XDoc, Result<DreamMessage>) instead.")]
        public Result<DreamMessage> PutAsync(XDoc doc) {
            return Invoke(Verb.PUT, DreamMessage.Ok(doc), new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="Put(MindTouch.Dream.DreamMessage,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [Obsolete("PutAsync(DreamMessage) is obsolete. Please use Put(DreamMessage, Result<DreamMessage>) instead.")]
        public Result<DreamMessage> PutAsync(DreamMessage message) {
            return Invoke(Verb.PUT, message, new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="Get(MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <returns></returns>
        [Obsolete("GetAsync() is obsolete. Please use Get(Result<DreamMessage>) instead.")]
        public Result<DreamMessage> GetAsync() {
            return Invoke(Verb.GET, DreamMessage.Ok(), new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="Get(MindTouch.Dream.DreamMessage,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [Obsolete("GetAsync(DreamMessage) is obsolete. Please use Get(DreamMessage, Result<DreamMessage>) instead.")]
        public Result<DreamMessage> GetAsync(DreamMessage message) {
            return Invoke(Verb.GET, message, new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="Head(MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <returns></returns>
        [Obsolete("HeadAsync() is obsolete. Please use Head(Result<DreamMessage>) instead.")]
        public Result<DreamMessage> HeadAsync() {
            return Invoke(Verb.HEAD, DreamMessage.Ok(), new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="Options(MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <returns></returns>
        [Obsolete("OptionsAsync() is obsolete. Please use Options(Result<DreamMessage>) instead.")]
        public Result<DreamMessage> OptionsAsync() {
            return Invoke(Verb.OPTIONS, DreamMessage.Ok(), new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="Delete(MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <returns></returns>
        [Obsolete("DeleteAsync() is obsolete. Please use Delete(Result<DreamMessage>) instead.")]
        public Result<DreamMessage> DeleteAsync() {
            return Invoke(Verb.DELETE, DreamMessage.Ok(), new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="Delete(MindTouch.Xml.XDoc,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        [Obsolete("DeleteAsync(XDoc) is obsolete. Please use Delete(XDoc, Result<DreamMessage>) instead.")]
        public Result<DreamMessage> DeleteAsync(XDoc doc) {
            return Invoke(Verb.DELETE, DreamMessage.Ok(doc), new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="Delete(MindTouch.Dream.DreamMessage,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [Obsolete("DeleteAsync(DreamMessage) is obsolete. Please use Delete(DreamMessage, Result<DreamMessage>) instead.")]
        public Result<DreamMessage> DeleteAsync(DreamMessage message) {
            return Invoke(Verb.DELETE, message, new Result<DreamMessage>(TimeSpan.MaxValue));
        }

        /// <summary>
        /// This method is deprecated. Please use <see cref="Invoke(string,MindTouch.Dream.DreamMessage,MindTouch.Tasking.Result{MindTouch.Dream.DreamMessage})"/> instead.
        /// </summary>
        /// <param name="verb"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [Obsolete("InvokeAsync(verb, DreamMessage) is obsolete. Please use Invoke(string, DreamMessage, Result<DreamMessage>) instead.")]
        public Result<DreamMessage> InvokeAsync(string verb, DreamMessage request) {

            // NOTE (steveb): no need to set time-outs since 'Invoke()' will ensure that a timeout occurs

            return Invoke(verb, request, new Result<DreamMessage>(TimeSpan.MaxValue));
        }
        #endregion
    }
}
