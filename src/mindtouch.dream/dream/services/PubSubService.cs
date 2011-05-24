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
using Autofac;
using Autofac.Builder;
using log4net;

using MindTouch.Dream.Services.PubSub;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Publication and Subscription Service", "Copyright (c) 2006-2011 MindTouch, Inc.",
        SID = new string[] { "sid://mindtouch.com/dream/2008/10/pubsub" }
    )]
    internal class PubSubService : DreamService {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        protected IPubSubDispatcher _dispatcher;

        //--- Features ---
        [DreamFeature("POST:publish", "Publish an event")]
        internal Yield PublishEvent(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            DispatcherEvent ev;
            try {
                ev = new DispatcherEvent(request);
                _log.DebugFormat("{0} received event '{1}'", this.Self.Uri, ev.Id);
                if(ev.Channel.Scheme == "pubsub") {
                    response.Return(DreamMessage.Forbidden("events published into this service cannot be of scheme 'pubsub'"));
                    yield break;
                }
                _dispatcher.Dispatch(ev);
                response.Return(DreamMessage.Ok(ev.GetEventEnvelope()));
            } catch(Exception e) {
                response.Return(DreamMessage.BadRequest(e.Message));
            }
            yield break;
        }

        [DreamFeature("POST:subscribers", "Intialize a set of subscriptions")]
        internal Yield CreateSubscriptionSet(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc subscriptionSet = request.ToDocument();
            Tuplet<PubSubSubscriptionSet, bool> set = _dispatcher.RegisterSet(subscriptionSet);
            XUri locationUri = Self.At("subscribers", set.Item1.Location).Uri.AsPublicUri();
            DreamMessage msg = null;
            if(set.Item2) {

                // existing subs cause a Conflict with ContentLocation of the sub
                msg = DreamMessage.Conflict("The specified owner already has a registered subscription set");
                msg.Headers.ContentLocation = locationUri;
            } else {

                // new subs cause a Created with Location of the sub, plus XDoc containing the location
                XDoc responseDoc = new XDoc("subscription-set")
                    .Elem("uri.location", locationUri)
                    .Elem("access-key", set.Item1.AccessKey);
                msg = DreamMessage.Created(locationUri, responseDoc);
                msg.Headers.Location = locationUri.With("access-key", set.Item1.AccessKey);
            }
            response.Return(msg);
            yield break;
        }

        [DreamFeature("GET:subscribers", "Get the combined subscription set for this service")]
        internal Yield GetCombinedSubscribeSet(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            response.Return(DreamMessage.Ok(_dispatcher.CombinedSet.AsDocument()));
            yield break;
        }

        [DreamFeature("GET:diagnostics/subscriptions", "Diagnostic: Get all subscription sets registered with this service")]
        protected Yield DiagnosticGetAllSubscriptionSets(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc sets = new XDoc("subscription-sets");
            foreach(PubSubSubscriptionSet set in _dispatcher.GetAllSubscriptionSets()) {
                sets.Add(set.AsDocument());
            }
            response.Return(DreamMessage.Ok(sets));
            yield break;
        }

        [DreamFeature("GET:subscribers/{id}", "Intialize a set of subscriptions")]
        protected Yield GetSubscribeSet(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PubSubSubscriptionSet set = _dispatcher[context.GetParam("id")];
            if(set == null) {
                response.Return(DreamMessage.NotFound("There is no subscription set at this location"));
            } else {
                response.Return(DreamMessage.Ok(set.AsDocument()));
            }
            yield break;
        }

        [DreamFeature("POST:subscribers/{id}", "Replace subscription set (should only be used for chaining)")]
        [DreamFeature("PUT:subscribers/{id}", "Replace subscription set")]
        protected Yield ReplaceSubscribeSet(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            try {
                string setId = context.GetParam("id");
                _log.DebugFormat("Updating set {0}", setId);
                if(!string.IsNullOrEmpty(request.Headers.DreamEventId)) {
                    _log.DebugFormat("'{0}' update is event: {1} - {2}", setId, request.Headers.DreamEventChannel, request.Headers.DreamEventId);
                }
                XDoc subscriptionDocument = request.ToDocument();
                PubSubSubscriptionSet set = _dispatcher.ReplaceSet(setId, subscriptionDocument);
                if(set != null) {
                    long? version = subscriptionDocument["@version"].AsLong;
                    if(version.HasValue && version.Value <= set.Version) {
                        _log.DebugFormat("set not modified: {0}", setId);
                        response.Return(DreamMessage.NotModified());
                    } else {
                        if(version.HasValue) {
                            _log.DebugFormat("Updating set '{0}' from version {1} to {2}", setId, set.Version, version);
                        } else {
                            _log.DebugFormat("Updating set '{0}'", setId);
                        }
                        response.Return(DreamMessage.Ok());
                    }
                } else {
                    _log.DebugFormat("no such set: {0}", setId);
                    response.Return(DreamMessage.NotFound("There is no subscription set at this location"));
                }
            } catch(ArgumentException e) {
                response.Return(DreamMessage.Forbidden(e.Message));
            }
            yield break;
        }

        [DreamFeature("DELETE:subscribers/{id}", "Remove subscription set")]
        protected Yield RemoveSubscribeSet(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string id = context.GetParam("id");
            _dispatcher.RemoveSet(id);
            DreamMessage msg = DreamMessage.Ok();
            response.Return(msg);
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, IContainer container, Result result) {
            yield return Coroutine.Invoke(base.Start, config, container, new Result());
            _log.DebugFormat("starting {0}", Self.Uri);

            // make sure we have an IPubSubDispatcher registered
            if(!container.IsRegistered<IPubSubDispatcher>()) {
                var builder = new ContainerBuilder();
                builder.Register<Dispatcher>().As<IPubSubDispatcher>().ServiceScoped();
                builder.Build(container);
            }

            // initialize dispatcher
            _dispatcher = container.Resolve<IPubSubDispatcher>(TypedParameter.From(new DispatcherConfig {
                ServiceUri = Self, 
                ServiceAccessCookie = DreamCookie.NewSetCookie("service-key", InternalAccessKey, Self.Uri),
                ServiceCookies = Cookies,
                ServiceConfig = config                                                                                          
            }));

            // check for upstream chaining
            if(!config["upstream"].IsEmpty) {
                XDoc combinedset = _dispatcher.CombinedSet.AsDocument();

                // we've been provided 1 or more upstream pubsub services that we need to subscribe to
                foreach(XDoc upstream in config["upstream/uri"]) {
                    int retry = 0;
                    while(true) {
                        retry++;
                        _log.DebugFormat("setting up upstream chain to {0} (attempt {1})", upstream, retry);
                        XUri upstreamUri = upstream.AsUri;

                        // subscribe with an empty set, since there are no child subs at Start, but we need a place to subscribe updates on
                        XDoc emptySub = new XDoc("subscription-set").Elem("uri.owner", Self.Uri);
                        Result<DreamMessage> upstreamResult;
                        yield return upstreamResult = Plug.New(upstreamUri).Post(emptySub, new Result<DreamMessage>(TimeSpan.MaxValue));
                        if(upstreamResult.Value.IsSuccessful) {
                            XUri location = new XUri(upstreamResult.Value.Headers.Location).WithoutQuery();
                            string accessKey = upstreamResult.Value.ToDocument()["access-key"].AsText;

                            // subscribe the resulting location to our pubsub:///* changes
                            XDoc subscribeToChanges = new XDoc("subscription-set")
                                .Elem("uri.owner", upstreamUri.WithScheme("upstream"))
                                .Start("subscription")
                                .Attr("id", "1")
                                .Elem("channel", "pubsub://*/*")
                                .Add(DreamCookie.NewSetCookie("access-key", accessKey, location).AsSetCookieDocument)
                                .Start("recipient").Elem("uri", upstreamResult.Value.Headers.Location).End()
                                .End();
                            _dispatcher.RegisterSet(subscribeToChanges);
                            break;
                        }
                        _log.WarnFormat("unable to subscribe to upstream pubsub (attempt {0}): {1}", retry, upstreamResult.Value.Status);
                        if(retry >= 3) {
                            _log.WarnFormat("giving up on upstream chaining to {0}", upstream);
                            break;
                        }
                        yield return Async.Sleep(TimeSpan.FromMilliseconds(500));
                        continue;
                    }
                }
            }
            if(!config["downstream"].IsEmpty) {

                // we've been provided 1 or more downstream pubsub services that we need to get to subscribe to us
                foreach(XDoc downstream in config["downstream/uri"]) {
                    int retry = 0;
                    while(true) {
                        retry++;
                        _log.DebugFormat("setting up downstream chain to {0} (attempt {1})", downstream, retry);
                        Result<DreamMessage> downstreamResult;
                        yield return downstreamResult = Plug.New(downstream.AsUri).Get(new Result<DreamMessage>(TimeSpan.MaxValue));
                        if(downstreamResult.Value.IsSuccessful) {
                            XDoc downstreamSet = downstreamResult.Value.ToDocument();
                            Tuplet<PubSubSubscriptionSet, bool> set = _dispatcher.RegisterSet(downstreamSet);
                            XUri locationUri = Self.At("subscribers", set.Item1.Location).Uri;
                            XUri featureUri = Self.At("subscribers").Uri;
                            _log.DebugFormat("downstream chain to {0} registered {1}", downstream, set.Item1.Location);
                            XDoc subscribeToChanges = new XDoc("subscription-set")
                                .Elem("uri.owner", Self.Uri)
                                .Start("subscription")
                                .Attr("id", "1")
                                .Elem("channel", "pubsub://*/*")
                                .Add(DreamCookie.NewSetCookie("access-key", set.Item1.AccessKey, featureUri).AsSetCookieDocument)
                                .Start("recipient").Elem("uri", locationUri).End()
                                .End();
                            yield return downstreamResult = Plug.New(downstream.AsUri).Post(subscribeToChanges, new Result<DreamMessage>(TimeSpan.MaxValue));
                            if(downstreamResult.Value.IsSuccessful) {
                                break;
                            }
                            _log.WarnFormat("unable to subscribe to downstream pubsub (attempt {0}): {1}", retry, downstreamResult.Value.Status);
                        } else {
                            _log.WarnFormat("unable to retrieve downstream set (attempt {0}): {1}", retry, downstreamResult.Value.Status);
                        }
                        if(retry >= 3) {
                            _log.WarnFormat("giving up on downstream chaining to {0}", downstream);
                            break;
                        }
                        yield return Async.Sleep(TimeSpan.FromMilliseconds(500));
                    }
                }
            }
            result.Return();
        }

        protected override Yield Stop(Result result) {

            // TODO (arnec): need to clean up upstream and downstream subscriptions
            _dispatcher = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        public override DreamAccess DetermineAccess(DreamContext context, DreamMessage request) {
            if(context.Feature.Signature.StartsWith("subscribers/")) {
                string id = context.GetParam("id", null);
                PubSubSubscriptionSet set = _dispatcher[id];
                if(set != null) {
                    string accessKey = context.GetParam("access-key", null);
                    if(string.IsNullOrEmpty(accessKey)) {
                        DreamCookie cookie = DreamCookie.GetCookie(request.Cookies, "access-key");
                        if(cookie != null) {
                            accessKey = cookie.Value;
                        }
                    }
                    if(StringUtil.EqualsInvariant(set.AccessKey, accessKey)) {
                        return DreamAccess.Private;
                    }
                    _log.DebugFormat("no matching access-key in query or cookie for location '{0}'", id);
                } else {
                    _log.DebugFormat("no subscription set for location '{0}'", id);
                }
            }
            return base.DetermineAccess(context, request);
        }
    }
}
