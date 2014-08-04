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
using MindTouch.Tasking;
using MindTouch.Web;

namespace MindTouch.Dream {
    using DreamFeatureCoroutineHandler = CoroutineHandler<DreamContext, DreamMessage, Result<DreamMessage>>;

    internal class DreamCachedResponseException : DreamException {

        //--- Fields ---
        public readonly DreamMessage Response;

        //--- Constructors ---
        public DreamCachedResponseException(DreamMessage response) {
            this.Response = response;
        }

        public DreamCachedResponseException(DreamMessage response, string message)
            : base(message) {
            this.Response = response;
        }

        public DreamCachedResponseException(DreamMessage response, string message, Exception innerException)
            : base(message, innerException) {
            this.Response = response;
        }
    }

    internal class DreamFeatureChain {

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        internal readonly bool MainStage;
        private DreamFeatureStage _stage;
        private DreamContext _context;
        private Result<DreamMessage> _response;
        private string _previousName;

        //--- Constructors ---
        internal DreamFeatureChain(DreamFeatureStage stage, bool mainStage, DreamContext context, Result<DreamMessage> response, string previousName) {
            if(context.IsTaskEnvDisposed) {
                throw new InvalidOperationException("cannot go to next feature state with disposed context");
            }
            this.MainStage = mainStage;
            _stage = stage;
            _context = context;
            _response = response;
            _previousName = previousName ?? "N/A";
        }

        //--- Properties ---
        internal string Name { get { return _stage.Name; } }

        //--- Methods ---
        internal void Handler(Result<DreamMessage> result) {
            DreamMessage request;

            // check if previous feature handler threw an exception
            try {
                if(result.HasException) {
                    DreamAbortException abort = result.Exception as DreamAbortException;
                    if(abort != null) {

                        // extract contained message
                        request = abort.Response;
                    } else {

                        // check if this is a cached response we need to forward
                        DreamCachedResponseException cached = result.Exception as DreamCachedResponseException;
                        if(cached != null) {
                            _response.Throw(cached);
                            return;
                        }

                        // convert exception into message
                        DreamMessage exceptionMessage = null;
                        ExceptionTranslator[] translators = _context.Feature.ExceptionTranslators;
                        if(translators != null) {
                            bool locallyAttachedContext = false;
                            try {
                                if(DreamContext.CurrentOrNull == null) {
                                    _context.AttachToCurrentTaskEnv();
                                    locallyAttachedContext = true;
                                }
                                foreach(var translate in translators) {
                                    exceptionMessage = translate(_context, result.Exception);
                                    if(exceptionMessage != null) {
                                        break;
                                    }
                                }
                            } finally {
                                if(locallyAttachedContext) {
                                    _context.DetachFromTaskEnv();
                                }
                            }
                        }
                        if(exceptionMessage == null) {
                            _log.ErrorExceptionFormat(result.Exception, "handler for {0}:{1} failed ({2})", _context.Verb, _context.Uri.ToString(false), _previousName);
                            exceptionMessage = DreamMessage.InternalError(result.Exception);
                        }
                        request = exceptionMessage;
                    }
                } else {
                    request = result.Value;
                }
            } catch(Exception e) {
                if(result.HasException) {
                    _log.ErrorExceptionFormat(result.Exception, "handler for {0}:{1} failed ({2}), cascading via processing exception '{3}'", _context.Verb, _context.Uri.ToString(false), _previousName, e);
                    request = DreamMessage.InternalError(result.Exception);
                } else {
                    _log.ErrorExceptionFormat(e, "handler for {0}:{1} failed completing stage '{2}'", _context.Verb, _context.Uri.ToString(false), _previousName);
                    request = DreamMessage.InternalError(e);
                }
            }

            // check if feature handler can handle this message
            if(!MainStage || request.IsSuccessful) {
                TaskEnv.ExecuteNew(() => Handler_DreamMessage(request));
            } else {

                // non-success messages skip the main stage
                _response.Return(request);
            }
        }

        private void Handler_DreamMessage(DreamMessage request) {
            var attachedToTaskEnv = false;
            try {

                // grabbing context from FeatureChain (must remove it again from env before env completes, so that it doesn't get disposed)
                if(_context.IsTaskEnvDisposed) {
                    throw new InvalidOperationException("cannot go to next feature state with disposed context");
                }
                _context.AttachToCurrentTaskEnv();
                attachedToTaskEnv = true;

                // check if request is authorized for service
                if((_stage.Access != DreamAccess.Public) && (_context.Feature.Service.Self != null) && (_stage.Access > _context.Feature.Service.DetermineAccess(_context, request))) {
                    request.Close();
                    _log.WarnFormat("access '{0}' to feature '{1}' denied for '{2}'",
                        _stage.Access,
                        _context.Uri.AsPublicUri(),
                        request.Headers.DreamService
                    );
                    if(_log.IsDebugEnabled) {
                        _log.DebugFormat("with service keys:");
                        foreach(DreamCookie c in request.Cookies) {
                            if(c.Name != "service-key") {
                                continue;
                            }
                            _log.DebugFormat("  path: {0}, key: {1}", c.Path, c.Value);
                        }
                    }

                    // removing context from env so that shared context is not disposed
                    _context.DetachFromTaskEnv();
                    attachedToTaskEnv = false;
                    _response.Return(DreamMessage.Forbidden("insufficient access privileges"));
                } else {

                    // invoke handler
                    Result<DreamMessage> inner = new Result<DreamMessage>(_response.Timeout, TaskEnv.Current).WhenDone(value => {

                        // removing context from env so that shared context is not disposed
                        _context.DetachFromTaskEnv();

                        // forward result to recipient
                        _response.Return(value);
                    }, exception => {

                        // removing context from env so that shared context is not disposed
                        _context.DetachFromTaskEnv();

                        // forward exception to recipient
                        _response.Throw(exception);
                    });
                    _stage.Invoke(_context, request, inner);
                }
            } catch(Exception ex) {
                if(attachedToTaskEnv) {
                    _context.DetachFromTaskEnv();
                }
                _response.Throw(ex);
            }
        }
    }
}
