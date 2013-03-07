/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2013 MindTouch, Inc.
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
using System.Web;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream.Http {
    using Yield = IEnumerator<IYield>;


    /// <summary>
    /// Provides an <see cref="IHttpHandler"/> implementation to load Dream inside of IIS.
    /// </summary>
    public class HttpHandler : IHttpHandler, IPlugEndpoint {

        // NOTE (steveb): we only allow one environment to exist at once (see bug 5520)

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static readonly object SyncRoot = new object();
        private static IDreamEnvironment _env;
        private static XUri _uri;
        private static int _minSimilarity;
        private string _dreamInParamAuthtoken;

        //--- Constructors ---

        /// <summary>
        /// Create new handler instance
        /// </summary>
        public HttpHandler() {
            if(_env == null) {
                lock(SyncRoot) {
                    if(_env == null) {
                        _log.InfoMethodCall("Startup");
                        try {
                            _log.InfoMethodCall("ctor: initializing HttpHandler");
                            NameValueCollection settings = System.Configuration.ConfigurationManager.AppSettings;

                            // determine storage locations
                            string basePath = HttpContext.Current.ApplicationInstance.Server.MapPath("~");
                            string storagePath = settings["storage-dir"] ?? settings["service-dir"];
                            if(string.IsNullOrEmpty(storagePath)) {
                                storagePath = Path.Combine(basePath, "storage");
                            } else if(!Path.IsPathRooted(storagePath)) {
                                storagePath = Path.Combine(basePath, storagePath);
                            }

                            // read configuration
                            string apikey = settings["apikey"];
                            _uri = new XUri(settings["public-uri"] ?? settings["root-uri"] ?? "http://localhost/@api");
                            _minSimilarity = _uri.MaxSimilarity;
                            _dreamInParamAuthtoken = settings["dream.in.authtoken"];

                            // start dreamhost
                            XDoc config = new XDoc("config")
                                .Elem("guid", settings["guid"])
                                .Elem("uri.public", _uri.ToString())
                                .Elem("storage-dir", storagePath)
                                .Elem("host-path", settings["host-path"])
                                .Elem("connect-limit", settings["connect-limit"])
                                .Elem("apikey", apikey);
                            IDreamEnvironment env = new DreamHostService();
                            env.Initialize(config);

                            // load assemblies in 'services' folder
                            string servicesFolder = settings["service-dir"] ?? Path.Combine("bin", "services");
                            if(!Path.IsPathRooted(servicesFolder)) {
                                servicesFolder = Path.Combine(basePath, servicesFolder);
                            }
                            _log.DebugFormat("examining services directory '{0}'", servicesFolder);
                            if(Directory.Exists(servicesFolder)) {
                                Plug host = env.Self.With("apikey", apikey);
                                foreach(string file in Directory.GetFiles(servicesFolder, "*.dll")) {
                                    string assembly = Path.GetFileNameWithoutExtension(file);
                                    _log.DebugFormat("attempting to load '{0}'", assembly);

                                    // register assembly blueprints
                                    DreamMessage response = host.At("load").With("name", assembly).Post(new Result<DreamMessage>(TimeSpan.MaxValue)).Wait();
                                    if(!response.IsSuccessful) {
                                        _log.WarnFormat("DreamHost: ERROR: assembly '{0}' failed to load", file);
                                    }
                                }
                            } else {
                                _log.WarnFormat("DreamHost: WARN: no services directory '{0}'", servicesFolder);
                            }


                            // execute script
                            string scriptFilename = settings["script"];
                            if(!string.IsNullOrEmpty(scriptFilename)) {
                                string filename = scriptFilename;
                                if(!Path.IsPathRooted(filename)) {
                                    filename = Path.Combine(basePath, filename);
                                }

                                // execute xml script file
                                XDoc script = XDocFactory.LoadFrom(filename, MimeType.XML);
                                Plug host = env.Self.With("apikey", apikey);
                                host.At("execute").Post(script);
                            }

                            // register plug factory for this uri
                            Plug.AddEndpoint(this);

                            // set _env variable so other constructors don't initialize it anymore
                            _env = env;
                        } catch(Exception e) {
                            _log.ErrorExceptionMethodCall(e, "ctor");
                            throw;
                        }
                    }
                }
            }
        }

        //--- Properties ---
        bool IHttpHandler.IsReusable { get { return true; } }

        //--- Methods ---
        void IHttpHandler.ProcessRequest(HttpContext httpContext) {
            var key = new object();
            DreamMessage request = null;
            try {
                string verb = httpContext.Request.HttpMethod;
                XUri requestUri = HttpUtil.FromHttpContext(httpContext);
                _env.AddActivityDescription(key, string.Format("Incoming: {0} {1}", verb, requestUri.ToString()));
                _log.DebugMethodCall("ProcessRequest", verb, requestUri);

                // create request message
                request = new DreamMessage(DreamStatus.Ok, new DreamHeaders(httpContext.Request.Headers), MimeType.New(httpContext.Request.ContentType), httpContext.Request.ContentLength, httpContext.Request.InputStream);
                DreamUtil.PrepareIncomingMessage(request, httpContext.Request.ContentEncoding, string.Format("{0}://{1}{2}", httpContext.Request.Url.Scheme, httpContext.Request.Url.Authority, httpContext.Request.ApplicationPath), httpContext.Request.UserHostAddress, httpContext.Request.UserAgent);
                requestUri = requestUri.AuthorizeDreamInParams(request, _dreamInParamAuthtoken);

                // process message
                Result<DreamMessage> response = _env.SubmitRequestAsync(verb, requestUri, httpContext.User, request, new Result<DreamMessage>(TimeSpan.MaxValue)).Block();
                request.Close();
                DreamMessage item = response.HasException ? DreamMessage.InternalError(response.Exception) : response.Value;

                // set status
                if(_log.IsDebugEnabled) {
                    _log.DebugMethodCall("ProcessRequest[Status]", item.Status, String.Format("{0}{1}", httpContext.Request.Url.GetLeftPart(UriPartial.Authority), httpContext.Request.RawUrl).Replace("/index.aspx", "/"));
                }
                httpContext.Response.StatusCode = (int)item.Status;

                // remove internal headers
                item.Headers.DreamTransport = null;
                item.Headers.DreamPublicUri = null;

                // create stream for response (this will force the creation of the 'Content-Length' header as well)
                Stream stream = item.ToStream();

                // copy headers
                foreach(KeyValuePair<string, string> pair in item.Headers) {
                    _log.TraceMethodCall("ProcessRequest[Header]", pair.Key, pair.Value);
                    httpContext.Response.AppendHeader(pair.Key, pair.Value);
                }

                // add set-cookie headers to response
                if(item.HasCookies) {
                    foreach(DreamCookie cookie in item.Cookies) {
                        httpContext.Response.AppendHeader(DreamHeaders.SET_COOKIE, cookie.ToSetCookieHeader());
                    }
                }

                // send message stream
                long size = item.ContentLength;
                if(((size == -1) || (size > 0)) && (stream != Stream.Null)) {
                    stream.CopyToStream(httpContext.Response.OutputStream, size, new Result<long>(TimeSpan.MaxValue)).Wait();
                }
                item.Close();
            } catch(Exception ex) {
                _log.ErrorExceptionMethodCall(ex, "CommonRequestHandler");
                if(request != null) {
                    request.Close();
                }
                if(httpContext != null) {
                    httpContext.Response.Close();
                }
            } finally {
                _env.RemoveActivityDescription(key);
            }
        }

        //--- Interface Methods ---
        int IPlugEndpoint.GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            normalized = uri;
            int similarity = uri.Similarity(_uri);
            return (similarity >= _minSimilarity) ? similarity : 0;
        }

        Yield IPlugEndpoint.Invoke(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
            Result<DreamMessage> res;
            yield return res = _env.SubmitRequestAsync(verb, uri, null, request, new Result<DreamMessage>(response.Timeout));
            response.Return(res);
        }
    }
}
