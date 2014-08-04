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
using System.IO;
using System.Net;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Web;

namespace MindTouch.Dream.Http {
    using Yield = IEnumerator<IYield>;

    internal class HttpTransport : IPlugEndpoint {

        //--- Types ---
        internal class ActivityState {

            //--- Fields ---
            private readonly IDreamActivityDescription _activity;
            private Queue<string> _messages = new Queue<string>();
            private readonly IDreamEnvironment _env;
            private readonly string _verb;
            private readonly string _uri;
            private readonly string _hostname;

            //--- Constructors ---
            internal ActivityState(IDreamEnvironment env, string verb, string uri, string hostname) {
                _env = env;
                _activity = env.CreateActivityDescription();
                _verb = verb;
                _uri = uri;
                _hostname = hostname;
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
                            _activity.Description = string.Format("Incoming ({2}): {0} {1} [{3}]", _verb, _uri, _hostname, string.Join(" -> ", _messages.ToArray()));
                        }
                    } else {
                        _messages = null;
                        _activity.Dispose();
                    }
                }
            }
        }

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        public readonly string ServerSignature;
        private readonly IDreamEnvironment _env;
        private XUri _uri;
        private readonly int _minSimilarity;
        private HttpListener _listener;
        private readonly string _sourceInternal;
        private readonly string _sourceExternal;
        private readonly AuthenticationSchemes _authenticationSheme;
        private readonly string _dreamInParamAuthtoken;

        //--- Constructors ---
        public HttpTransport(IDreamEnvironment env, XUri uri, AuthenticationSchemes authenticationSheme, string dreamInParamAuthtoken) {
            if(env == null) {
                throw new ArgumentNullException("env");
            }
            if(uri == null) {
                throw new ArgumentNullException("uri");
            }
            _env = env;
            _uri = uri.WithoutCredentialsPathQueryFragment();
            _minSimilarity = _uri.MaxSimilarity;
            _sourceInternal = _uri + " (internal)";
            _sourceExternal = _uri.ToString();
            _authenticationSheme = authenticationSheme;
            _dreamInParamAuthtoken = dreamInParamAuthtoken;
            this.ServerSignature = "Dream-HTTPAPI/" + DreamUtil.DreamVersion;
        }

        //--- Methods ---
        public void Startup() {
            _log.InfoMethodCall("Startup", _uri);

            // create listener and make it listen to the uri
            _listener = new HttpListener {
                IgnoreWriteExceptions = true, 
                AuthenticationSchemes = _authenticationSheme
            };
            _listener.Prefixes.Add(_uri.ToString());
            try {
                _listener.Start();
            } catch(Exception x) {
                _log.WarnExceptionFormat(x, "Unable to start listening on '{0}'", _uri);
                throw;
            }
            _listener.BeginGetContext(RequestHandler, _listener);

            // register plug factory for this uri
            Plug.AddEndpoint(this);
        }

        public void Shutdown() {
            _log.InfoMethodCall("Shutdown", _uri);
            Plug.RemoveEndpoint(this);
            _uri = null;

            // BUGBUGBUG (arnec): the following line may throw an exception on shutdown and
            // there is no way of differentiating it from other failures, so we log just in case.
            try {
                _listener.Stop();
            } catch(Exception e) {
                _log.Debug("Shutdown", e);
            }
        }

        private void RequestHandler(IAsyncResult ar) {
            HttpListenerContext httpContext = null;
            HttpListener listener = (HttpListener)ar.AsyncState;

            // try to finish getting the current context
            try {
                httpContext = listener.EndGetContext(ar);
            } catch(Exception e) {
                _log.WarnExceptionFormat(e, "unable to finish acquiring the request context, unable to handle request");
            }

            // start listening for next request
            if(!listener.IsListening) {
                _log.Debug("dropping out of request handler, since the listener is no longer listening");
                return;
            }
            try {
                listener.BeginGetContext(RequestHandler, listener);
            } catch(Exception e) {
                _log.WarnExceptionFormat(e, "unable to re-aquire context, dropping out of request handler");
                return;
            }

            // if we didn't succeed in ending the GetContext call, drop out 
            if(httpContext == null) {
                return;
            }
            Action<string> activity = null;
            DreamMessage request = null;
            try {

                // finish listening for current context
                string[] prefixes = new string[listener.Prefixes.Count];
                listener.Prefixes.CopyTo(prefixes, 0);
                XUri requestUri = HttpUtil.FromHttpContext(httpContext);
                _log.DebugMethodCall("RequestHandler", httpContext.Request.HttpMethod, requestUri);

                // create request message
                request = new DreamMessage(DreamStatus.Ok, new DreamHeaders(httpContext.Request.Headers), MimeType.New(httpContext.Request.ContentType), httpContext.Request.ContentLength64, httpContext.Request.InputStream);
                Debug.Assert(httpContext.Request.RemoteEndPoint != null, "httpContext.Request.RemoteEndPoint != null");
                DreamUtil.PrepareIncomingMessage(request, httpContext.Request.ContentEncoding, prefixes[0], httpContext.Request.RemoteEndPoint.ToString(), httpContext.Request.UserAgent);
                requestUri = requestUri.AuthorizeDreamInParams(request, _dreamInParamAuthtoken);

                // check if the request was forwarded through Apache mod_proxy
                string hostname = requestUri.GetParam(DreamInParam.HOST, null) ?? request.Headers.ForwardedHost ?? request.Headers.Host ?? requestUri.HostPort;
                activity = new ActivityState(_env, httpContext.Request.HttpMethod, httpContext.Request.Url.ToString(), hostname).Message;
                activity("RequestHandler");

                // process message
                _env.UpdateInfoMessage(_sourceExternal, null);
                string verb = httpContext.Request.HttpMethod;
                _env.SubmitRequestAsync(verb, requestUri, httpContext.User, request, new Result<DreamMessage>(TimeSpan.MaxValue))
                    .WhenDone(result => Coroutine.Invoke(ResponseHandler, request, result, httpContext, activity, new Result(TimeSpan.MaxValue)));
            } catch(Exception ex) {
                _log.ErrorExceptionMethodCall(ex, "RequestHandler");
                if(request != null) {
                    request.Close();
                }
                try {
                    DreamMessage response = DreamMessage.InternalError(ex);
                    httpContext.Response.StatusCode = (int)response.Status;
                    Stream stream = response.ToStream();
                    httpContext.Response.Headers.Clear();
                    foreach(KeyValuePair<string, string> pair in response.Headers) {
                        HttpUtil.AddHeader(httpContext.Response, pair.Key, pair.Value);
                    }
                    httpContext.Response.KeepAlive = false;
                    long size = response.ContentLength;
                    if(((size == -1) || (size > 0)) && (stream != Stream.Null)) {
                        CopyStream(message => { }, stream, httpContext.Response.OutputStream, size, new Result<long>(DreamHostService.MAX_REQUEST_TIME)).Block();
                    }
                    httpContext.Response.OutputStream.Flush();
                } catch {
                    httpContext.Response.StatusCode = (int)DreamStatus.InternalError;
                }
                httpContext.Response.Close();
                if(activity != null) {
                    activity(null);
                }
            }
        }

        private Yield ResponseHandler(DreamMessage request, Result<DreamMessage> response, HttpListenerContext httpContext, Action<string> activity, Result result) {
            DreamMessage item = null;
            request.Close();
            try {
                activity("begin ResponseHandler");
                item = response.HasException ? DreamMessage.InternalError(response.Exception) : response.Value;

                // set status
                _log.TraceMethodCall("ResponseHandler: Status", item.Status, httpContext.Request.HttpMethod, httpContext.Request.Url);
                httpContext.Response.StatusCode = (int)item.Status;

                // remove internal headers
                item.Headers.DreamTransport = null;
                item.Headers.DreamPublicUri = null;

                // add out-going headers
                if(item.Headers.Server == null) {
                    item.Headers.Server = ServerSignature;
                }

                // create stream for response (this will force the creation of the 'Content-Length' header as well)
                Stream stream = item.ToStream();

                // copy headers
                httpContext.Response.Headers.Clear();
                foreach(KeyValuePair<string, string> pair in item.Headers) {
                    _log.TraceMethodCall("SendHttpResponse: Header", pair.Key, pair.Value);
                    HttpUtil.AddHeader(httpContext.Response, pair.Key, pair.Value);
                }

                // add set-cookie headers to response
                if(item.HasCookies) {
                    foreach(DreamCookie cookie in item.Cookies) {
                        httpContext.Response.Headers.Add(DreamHeaders.SET_COOKIE, cookie.ToSetCookieHeader());
                    }
                }

                // disable keep alive behavior
                httpContext.Response.KeepAlive = false;

                // send message stream
                long size = item.ContentLength;
                if(((size == -1) || (size > 0)) && (stream != Stream.Null)) {
                    activity(string.Format("pre CopyStream ({0} bytes)", size));
                    yield return CopyStream(activity, stream, httpContext.Response.OutputStream, size, new Result<long>(DreamHostService.MAX_REQUEST_TIME)).CatchAndLog(_log);
                    activity("post CopyStream");
                }
                activity("pre Flush");
                httpContext.Response.OutputStream.Flush();
                activity("post Flush");
                result.Return();
                activity("end ResponseHandler");
            } finally {
                activity(null);
                if(item != null) {
                    item.Close();
                }
                httpContext.Response.Close();
            }
        }

        private Result<int> Read(Action<string> activity, Stream stream, byte[] buffer, int offset, int count, Result<int> result) {

            // asynchronously execute read operation
            Result<IAsyncResult> inner = new Result<IAsyncResult>(TimeSpan.MaxValue);
            inner.WhenDone(_unused => {
                try {
                    activity(string.Format("pre {0}!EndRead", stream.GetType().FullName));
                    int readCount = stream.EndRead(inner.Value);
                    activity("post EndRead");
                    result.Return(readCount);
                } catch(Exception e) {
                    activity("throw Read 1");
                    result.Throw(e);
                }
            });
            try {
                activity(string.Format("pre {0}!BeginRead", stream.GetType().FullName));
                stream.BeginRead(buffer, offset, count, inner.Return, stream);
                activity("post BeginRead");
            } catch(Exception e) {
                activity("throw Read 2");
                result.Throw(e);
            }

            // return yield handle
            return result;
        }

        private Result Write(Action<string> activity, Stream stream, byte[] buffer, int offset, int count, Result result) {

            // asynchronously execute read operation
            Result<IAsyncResult> inner = new Result<IAsyncResult>(TimeSpan.MaxValue);
            inner.WhenDone(_unused => {
                try {
                    activity(string.Format("pre {0}!EndWrite", stream.GetType().FullName));
                    stream.EndWrite(inner.Value);
                    activity("post EndWrite");
                    result.Return();
                } catch(Exception e) {
                    activity("throw Write 1");
                    result.Throw(e);
                }
            });
            try {
                activity(string.Format("pre {0}!BeginWrite", stream.GetType().FullName));
                stream.BeginWrite(buffer, offset, count, inner.Return, stream);
                activity("post BeginWrite");
            } catch(Exception e) {
                activity("throw Write 2");
                result.Throw(e);
            }

            // return yield handle
            return result;
        }

        private Result<long> CopyStream(Action<string> activity, Stream source, Stream target, long length, Result<long> result) {

            // NOTE (steveb): intermediary copy steps already have a timeout operation, no need to limit the duration of the entire copy operation
            if((source == Stream.Null) || (length == 0)) {
                activity("return CopyStream 1");
                result.Return(0);
            } else if(!SysUtil.UseAsyncIO || (source.IsStreamMemorized() && target.IsStreamMemorized())) {

                // source & target are memory streams; let's do the copy inline as fast as we can
                byte[] buffer = new byte[StreamUtil.BUFFER_SIZE];
                long total = 0;
                while(length != 0) {
                    long count = source.Read(buffer, 0, buffer.Length);
                    if(count == 0) {
                        break;
                    }
                    target.Write(buffer, 0, (int)count);
                    total += count;
                    length -= count;
                }
                activity("return CopyStream 2");
                result.Return(total);
            } else {

                // use new task environment so we don't copy the task state over and over again
                TaskEnv.ExecuteNew(() => {
                    activity("pre CopyStream_Handler");
                    Coroutine.Invoke(CopyStream_Handler, activity, source, target, length, result);
                    activity("post CopyStream_Handler");
                });
            }
            return result;
        }

        private Yield CopyStream_Handler(Action<string> activity, Stream source, Stream target, long length, Result<long> result) {
            byte[] readBuffer = new byte[StreamUtil.BUFFER_SIZE];
            byte[] writeBuffer = new byte[StreamUtil.BUFFER_SIZE];
            long total = 0;
            int zero_read_counter = 0;
            Result write = null;

            // NOTE (steveb): we stop when we've read the expected number of bytes and the length was non-negative, 
            //                otherwise we stop when we can't read anymore bytes.

            while(length != 0) {

                // read first
                long count = (length >= 0) ? Math.Min(length, readBuffer.LongLength) : readBuffer.LongLength;
                if(source.IsStreamMemorized()) {
                    activity("pre Stream.Read");
                    count = source.Read(readBuffer, 0, (int)count);
                    activity("post Stream.Read");

                    // check if we failed to read
                    if(count == 0) {
                        break;
                    }
                } else {
                    activity("pre Read");
                    yield return Read(activity, source, readBuffer, 0, (int)count, new Result<int>()).Set(v => count = v);
                    activity("post Read");

                    // check if we failed to read
                    if(count == 0) {

                        // let's abort after 10 tries to read more data
                        if(++zero_read_counter > 10) {
                            break;
                        }
                        continue;
                    }
                    zero_read_counter = 0;
                }
                total += count;
                length -= count;

                // swap buffers
                byte[] tmp = writeBuffer;
                writeBuffer = readBuffer;
                readBuffer = tmp;

                // write second
                if((target == Stream.Null) || target.IsStreamMemorized()) {
                    activity("post Stream.Write");
                    target.Write(writeBuffer, 0, (int)count);
                    activity("post Stream.Write");
                } else {
                    if(write != null) {
                        activity("pre yield write 1");
                        yield return write.Catch();
                        activity("post yield write 1");
                        write.Confirm();
                    }
                    activity("pre Write");
                    write = Write(activity, target, writeBuffer, 0, (int)count, new Result());
                    activity("post Write");
                }
            }
            if(write != null) {
                activity("pre yield write 2");
                yield return write.Catch();
                activity("post yield write 2");
                write.Confirm();
            }

            // return result
            activity("return CopyStream_Handler");
            result.Return(total);
        }

        //--- Interface Methods ---
        int IPlugEndpoint.GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            normalized = uri;
            int similarity = uri.Similarity(_uri);
            return (similarity >= _minSimilarity) ? similarity : 0;
        }

        Yield IPlugEndpoint.Invoke(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
            _env.UpdateInfoMessage(_sourceInternal, null);
            Result<DreamMessage> res = new Result<DreamMessage>(response.Timeout);
            _env.SubmitRequestAsync(verb, uri, null, request, res);
            yield return res;
            response.Return(res);
        }
    }
}
