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
using System.Text;

using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream.Http {
    using Yield = IEnumerator<IYield>;

    internal class HttpPlugEndpoint : IPlugEndpoint {

        //--- Types ---
        internal class ActivityState {

            //--- Fields ---
            private object _key = new object();
            private Queue<string> _messages = new Queue<string>();
            private IDreamEnvironment _env;
            private string _verb;
            private string _uri;

            //--- Constructors ---
            internal ActivityState(IDreamEnvironment env, string verb, string uri) {
                _env = env;
                _verb = verb;
                _uri = uri;
            }

            //--- Messages ---
            internal void Message(string message) {
                lock(this) {
                    if(message != null) {
                        if(_messages != null) {
                            _messages.Enqueue(message);
                            if(_messages.Count > 10) {
                                _messages.Dequeue();
                            }
                            _env.AddActivityDescription(_key, string.Format("Outgoing: {0} {1} [{2}]", _verb, _uri, string.Join(" -> ", _messages.ToArray())));
                        }
                    } else {
                        _messages = null;
                        _env.RemoveActivityDescription(_key);
                    }
                }
            }
        }

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog();
        private static Dictionary<Guid, List<Result<DreamMessage>>> _requests = new Dictionary<Guid, List<Result<DreamMessage>>>();

        //--- Class Constructor ---
        static HttpPlugEndpoint() {
#if ALLOW_EXPIRED_HTTPS_CERTS
            try {
                ServicePointManager.ServerCertificateValidationCallback = delegate(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
                    return true;
                };
            } catch(Exception e) {
                LogUtils.LogError(_log, e, "class ctor");
            }
#endif
        }

        //--- Methods ---
        public int GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            normalized = uri;
            switch(uri.Scheme.ToLowerInvariant()) {
            case "http":
            case "https":
                return 1;
            case "ext-http":
                normalized = normalized.WithScheme("http");
                return int.MaxValue;
            case "ext-https":
                normalized = normalized.WithScheme("https");
                return int.MaxValue;
            default:
                return 0;
            }
        }

        public Yield Invoke(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
            Result<DreamMessage> res;

            // register activity
            DreamContext context = DreamContext.CurrentOrNull;
            Action<string> activity;
            if(context != null) {
                activity = new ActivityState(context.Env, verb, uri.ToString()).Message;
            } else {
                activity = delegate(string message) { };
            }
            activity("pre Invoke");
            yield return res = Coroutine.Invoke(HandleInvoke, activity, plug, verb, uri, request, new Result<DreamMessage>(response.Timeout)).Catch();
            activity("post Invoke");

            // unregister activity
            activity(null);

            // return response
            response.Return(res);
        }

        private Yield HandleInvoke(Action<string> activity, Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
            Result<IAsyncResult> async;

            // remove internal headers
            request.Headers.DreamTransport = null;

            // set request headers
            request.Headers.Host = uri.Host;
            if(request.Headers.UserAgent == null) {
                request.Headers.UserAgent = "Dream/" + DreamUtil.DreamVersion;
            }

            // add cookies to request
            if(request.HasCookies) {
                request.Headers[DreamHeaders.COOKIE] = DreamCookie.RenderCookieHeader(request.Cookies);
            }

            // check if we can pool the request with an existing one
            if((plug.Credentials == null) && StringUtil.ContainsInvariantIgnoreCase(verb, "GET")) {

                // create the request hashcode
                StringBuilder buffer = new StringBuilder();
                buffer.AppendLine(uri.ToString());
                foreach(KeyValuePair<string, string> header in request.Headers) {
                    buffer.Append(header.Key).Append(": ").Append(header.Value).AppendLine();
                }
                Guid hash = new Guid(StringUtil.ComputeHash(buffer.ToString()));

                // check if an active connection exists
                Result<DreamMessage> relay = null;
                lock(_requests) {
                    List<Result<DreamMessage>> pending;
                    if(_requests.TryGetValue(hash, out pending)) {
                        relay = new Result<DreamMessage>(response.Timeout);
                        pending.Add(relay);
                    } else {
                        pending = new List<Result<DreamMessage>>();
                        pending.Add(response);
                        _requests[hash] = pending;
                    }
                }

                // check if we're pooling a request
                if(relay != null) {

                    // wait for the relayed response
                    yield return relay;
                    response.Return(relay);
                    yield break;
                } else {

                    // NOTE (steveb): we use TaskEnv.Instantaneous so that we don't exit the current stack frame before we've executed the continuation;
                    //                otherwise, we'll trigger an exception because our result object may not be set.

                    // create new handler to multicast the response to the relays
                    response = new Result<DreamMessage>(response.Timeout, TaskEnv.Instantaneous);
                    response.WhenDone(_ => {
                        List<Result<DreamMessage>> pending;
                        lock(_requests) {
                            _requests.TryGetValue(hash, out pending);
                            _requests.Remove(hash);
                        }

                        // this check should never fail!
                        if(response.HasException) {

                            // send the exception to all relays
                            foreach(Result<DreamMessage> result in pending) {
                                result.Throw(response.Exception);
                            }
                        } else {
                            DreamMessage original = response.Value;

                            // only memorize the message if it needs to be cloned
                            if(pending.Count > 1) {

                                // clone the message to all relays
                                original.Memorize(new Result()).Wait();
                                foreach(var result in pending) {
                                    result.Return(original.Clone());
                                }
                            } else {

                                // relay the original message
                                pending[0].Return(original);
                            }
                        }
                    });
                }
            }

            // initialize request
            activity("pre WebRequest.Create");
            var httpRequest = (HttpWebRequest)WebRequest.Create(uri.ToUri());
            activity("post WebRequest.Create");
            httpRequest.Method = verb;
            httpRequest.Timeout = System.Threading.Timeout.Infinite;
            httpRequest.ReadWriteTimeout = System.Threading.Timeout.Infinite;

            // Note (arnec): httpRequest AutoRedirect is disabled because Plug is responsible for it (this allows redirects to follow
            // the appropriate handler instead staying stuck in http end point land
            httpRequest.AllowAutoRedirect = false;

            // Note from http://support.microsoft.com/kb/904262
            // The HTTP request is made up of the following parts:
            // 1.   Sending the request is covered by using the HttpWebRequest.Timeout method.
            // 2.   Getting the response header is covered by using the HttpWebRequest.Timeout method.
            // 3.   Reading the body of the response is not covered by using the HttpWebResponse.Timeout method. In ASP.NET 1.1 and in later versions, reading the body of the response 
            //      is covered by using the HttpWebRequest.ReadWriteTimeout method. The HttpWebRequest.ReadWriteTimeout method is used to handle cases where the response headers are 
            //      retrieved in a timely manner but where the reading of the response body times out.

            httpRequest.KeepAlive = false;
            httpRequest.ProtocolVersion = System.Net.HttpVersion.Version10;

            // TODO (steveb): set default proxy
            //httpRequest.Proxy = WebProxy.GetDefaultProxy();
            //httpRequest.Proxy.Credentials = CredentialCache.DefaultCredentials;

            // set credentials
            if(plug.Credentials != null) {
                httpRequest.Credentials = plug.Credentials;
                httpRequest.PreAuthenticate = true;
            } else if(!string.IsNullOrEmpty(uri.User) || !string.IsNullOrEmpty(uri.Password)) {
                httpRequest.Credentials = new NetworkCredential(uri.User ?? string.Empty, uri.Password ?? string.Empty);
                httpRequest.PreAuthenticate = true;

                // Note (arnec): this manually adds the basic auth header, so it can authorize
                // in a single request without requiring challenge 
                var authbytes = Encoding.ASCII.GetBytes(string.Concat(uri.User ?? string.Empty, ":", uri.Password ?? string.Empty));
                var base64 = Convert.ToBase64String(authbytes);
                httpRequest.Headers.Add(DreamHeaders.AUTHORIZATION, "Basic " + base64);
            }

            // add request headres
            foreach(KeyValuePair<string, string> header in request.Headers) {
                HttpUtil.AddHeader(httpRequest, header.Key, header.Value);
            }

            // send message stream
            if((request.ContentLength != 0) || (verb == Verb.POST)) {
                async = new Result<IAsyncResult>();
                try {
                    activity("pre BeginGetRequestStream");
                    httpRequest.BeginGetRequestStream(async.Return, null);
                    activity("post BeginGetRequestStream");
                } catch(Exception e) {
                    activity("pre HandleResponse 1");
                    if(!HandleResponse(activity, e, null, response)) {
                        _log.ErrorExceptionMethodCall(e, "HandleInvoke@BeginGetRequestStream", verb, uri);
                        try {
                            httpRequest.Abort();
                        } catch { }
                    }
                    yield break;
                }
                activity("pre yield BeginGetRequestStream");
                yield return async.Catch();
                activity("post yield BeginGetRequestStream");

                // send request
                Stream outStream;
                try {
                    activity("pre EndGetRequestStream");
                    outStream = httpRequest.EndGetRequestStream(async.Value);
                    activity("pre EndGetRequestStream");
                } catch(Exception e) {
                    activity("pre HandleResponse 2");
                    if(!HandleResponse(activity, e, null, response)) {
                        _log.ErrorExceptionMethodCall(e, "HandleInvoke@EndGetRequestStream", verb, uri);
                        try {
                            httpRequest.Abort();
                        } catch { }
                    }
                    yield break;
                }

                // copy data
                using(outStream) {
                    Result<long> res;
                    activity("pre yield CopyStream");
                    yield return res = request.ToStream().CopyTo(outStream, request.ContentLength, new Result<long>(TimeSpan.MaxValue)).Catch();
                    activity("post yield CopyStream");
                    if(res.HasException) {
                        activity("pre HandleResponse 3");
                        if(!HandleResponse(activity, res.Exception, null, response)) {
                            _log.ErrorExceptionMethodCall(res.Exception, "HandleInvoke@Async.CopyStream", verb, uri);
                            try {
                                httpRequest.Abort();
                            } catch { }
                        }
                        yield break;
                    }
                }
            }
            request = null;

            // wait for response
            async = new Result<IAsyncResult>(response.Timeout);
            try {
                activity("pre BeginGetResponse");
                httpRequest.BeginGetResponse(async.Return, null);
                activity("post BeginGetResponse");
            } catch(Exception e) {
                activity("pre HandleResponse 4");
                if(!HandleResponse(activity, e, null, response)) {
                    _log.ErrorExceptionMethodCall(e, "HandleInvoke@BeginGetResponse", verb, uri);
                    try {
                        httpRequest.Abort();
                    } catch { }
                }
                yield break;
            }
            activity("pre yield BeginGetResponse");
            yield return async.Catch();
            activity("post yield BeginGetResponse");

            // check if an error occurred
            if(async.HasException) {
                activity("pre HandleResponse 5");
                if(!HandleResponse(activity, async.Exception, null, response)) {
                    _log.ErrorExceptionMethodCall(async.Exception, "HandleInvoke@BeginGetResponse", verb, uri);
                    try {
                        httpRequest.Abort();
                    } catch { }
                }
                yield break;
            } else {

                // handle response
                try {
                    HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.EndGetResponse(async.Value);
                    activity("pre HandleResponse 6");
                    if(!HandleResponse(activity, null, httpResponse, response)) {
                        try {
                            httpRequest.Abort();
                        } catch { }
                    }
                } catch(Exception e) {
                    activity("pre HandleResponse 7");
                    if(!HandleResponse(activity, e, null, response)) {
                        _log.ErrorExceptionMethodCall(e, "HandleInvoke@EndGetResponse", verb, uri);
                        try {
                            httpRequest.Abort();
                        } catch { }
                    }
                    yield break;
                }
            }
        }

        private bool HandleResponse(Action<string> activity, Exception exception, HttpWebResponse httpResponse, Result<DreamMessage> response) {
            if(exception != null) {
                if(exception is WebException) {
                    activity("pre WebException");
                    httpResponse = (HttpWebResponse)((WebException)exception).Response;
                    activity("post WebException");
                } else {
                    activity("pre HttpWebResponse close");
                    try {
                        httpResponse.Close();
                    } catch { }
                    activity("HandleResponse exit 1");
                    response.Return(new DreamMessage(DreamStatus.UnableToConnect, null, new XException(exception)));
                    return false;
                }
            }

            // check if a response was obtained, otherwise fail
            if(httpResponse == null) {
                activity("HandleResponse exit 2");
                response.Return(new DreamMessage(DreamStatus.UnableToConnect, null, new XException(exception)));
                return false;
            }

            // determine response type
            MimeType contentType;
            Stream stream;
            HttpStatusCode statusCode = httpResponse.StatusCode;
            WebHeaderCollection headers = httpResponse.Headers;
            long contentLength = httpResponse.ContentLength;

            if(!string.IsNullOrEmpty(httpResponse.ContentType)) {
                contentType = new MimeType(httpResponse.ContentType);
                activity("pre new BufferedStream");
                stream = new BufferedStream(httpResponse.GetResponseStream());
                activity("post new BufferedStream");
            } else {

                // TODO (arnec): If we get a response with a stream, but no content-type, we're currently dropping the stream. Might want to revisit that.
                _log.DebugFormat("response ({0}) has not content-type and content length of {1}", statusCode, contentLength);
                contentType = null;
                stream = Stream.Null;
                httpResponse.Close();
            }

            // encapsulate the response in a dream message
            activity("HandleResponse exit 3");
            response.Return(new DreamMessage((DreamStatus)(int)statusCode, new DreamHeaders(headers), contentType, contentLength, stream));
            return true;
        }
    }
}
