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
using System.Web;
using System.Collections.Generic;
using System.IO;
using log4net;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Web;

namespace MindTouch.Dream.Http {
    /// <summary>
    /// Provides an <see cref="IHttpHandler"/> implementation to load Dream inside of IIS.
    /// </summary>
    public class RoutedHttpHandler : IHttpHandler {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        private readonly DreamApplication _handler;
        private readonly IDreamEnvironment _env;

        //--- Constructors ---

        /// <summary>
        /// Create new handler instance
        /// </summary>
        public RoutedHttpHandler(DreamApplication handler, IDreamEnvironment env) {
            _handler = handler;
            _env = env;
        }

        //--- Properties ---
        bool IHttpHandler.IsReusable { get { return true; } }

        //--- Methods ---
        void IHttpHandler.ProcessRequest(HttpContext httpContext) {
            var key = _env.CreateActivityDescription();
            DreamMessage request = null;
            try {
                string verb = httpContext.Request.HttpMethod;
                XUri requestUri = HttpUtil.FromHttpContext(httpContext);
                _env.AddActivityDescription(key, string.Format("Incoming: {0} {1}", verb, requestUri));
                _log.DebugMethodCall("ProcessRequest", verb, requestUri);

                // create request message
                request = new DreamMessage(DreamStatus.Ok, new DreamHeaders(httpContext.Request.Headers), MimeType.New(httpContext.Request.ContentType), httpContext.Request.ContentLength, httpContext.Request.InputStream);
                DreamUtil.PrepareIncomingMessage(request, httpContext.Request.ContentEncoding, string.Format("{0}://{1}{2}", httpContext.Request.Url.Scheme, httpContext.Request.Url.Authority, httpContext.Request.ApplicationPath), httpContext.Request.UserHostAddress, httpContext.Request.UserAgent);
                requestUri = requestUri.AuthorizeDreamInParams(request, _handler.AppConfig.DreamInParamAuthToken);

                // TODO (arnec): should this happen before PrepareIncomingMessage?
                request.Headers.DreamTransport = _handler.GetRequestBaseUri(httpContext.Request).ToString();

                // process message
                var response = _env.SubmitRequestAsync(verb, requestUri, httpContext.User, request, new Result<DreamMessage>(TimeSpan.MaxValue)).Block();
                request.Close();
                if(response.HasException) {
                    _log.ErrorExceptionFormat(response.Exception, "Request Failed [{0}:{1}]: {2}", verb, requestUri.Path, response.Exception.Message);
                }
                var item = response.HasException ? DreamMessage.InternalError(response.Exception) : response.Value;

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
    }
}
