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
using System.Linq;
using Autofac;
using Autofac.Builder;
using log4net;

using MindTouch.Dream.Services.PubSub;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;
using MindTouch.Extensions.Time;

namespace MindTouch.Dream.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Publication and Subscription Service", "Copyright (c) 2006-2011 MindTouch, Inc.",
        SID = new[] { "sid://mindtouch.com/dream/2008/10/pubsub" }
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
            var subscriptionSet = request.ToDocument();
            var location = request.Headers["X-Set-Location-Key"] ?? StringUtil.CreateAlphaNumericKey(8);
            var accessKey = request.Headers["X-Set-Access-Key"] ?? StringUtil.CreateAlphaNumericKey(8);
            var set = _dispatcher.RegisterSet(location, subscriptionSet, accessKey);
            var locationUri = Self.At("subscribers", set.Item1.Location).Uri.AsPublicUri();
            DreamMessage msg = null;
            if(set.Item2) {

                // existing subs cause a Conflict with ContentLocation of the sub
                msg = DreamMessage.Conflict("The specified owner or location already has a registered subscription set");
                msg.Headers.ContentLocation = locationUri;
            } else {

                // new subs cause a Created with Location of the sub, plus XDoc containing the location
                var responseDoc = new XDoc("subscription-set")
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
            var sets = new XDoc("subscription-sets");
            foreach(var set in _dispatcher.GetAllSubscriptionSets()) {
                sets.Add(set.AsDocument());
            }
            response.Return(DreamMessage.Ok(sets));
            yield break;
        }

        [DreamFeature("GET:subscribers/{location}", "Intialize a set of subscriptions")]
        protected Yield GetSubscribeSet(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var set = _dispatcher[context.GetParam("location")];
            response.Return(set == null ? DreamMessage.NotFound("There is no subscription set at this location") : DreamMessage.Ok(set.AsDocument()));
            yield break;
        }

        [DreamFeature("POST:subscribers/{location}", "Replace subscription set (should only be used for chaining)")]
        [DreamFeature("PUT:subscribers/{location}", "Replace subscription set")]
        protected Yield ReplaceSubscribeSet(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            DreamMessage responseMsg = null;
            try {
                var location = context.GetParam("location");
                if(!string.IsNullOrEmpty(request.Headers.DreamEventId)) {
                    _log.DebugFormat("'{0}' update is event: {1} - {2}", location, request.Headers.DreamEventChannel, request.Headers.DreamEventId);
                }
                var subscriptionDocument = request.ToDocument();
                var accessKey = request.Headers["X-Set-Access-Key"];
                var set = _dispatcher.ReplaceSet(location, subscriptionDocument, accessKey);
                _log.DebugFormat("Trying to update set {0}", location);
                if(set != null) {
                    var version = subscriptionDocument["@version"].AsLong;
                    if(version.HasValue && version.Value <= set.Version) {
                        _log.DebugFormat("set not modified: {0}", location);
                        responseMsg = DreamMessage.NotModified();
                    } else {
                        if(version.HasValue) {
                            _log.DebugFormat("Updating set '{0}' from version {1} to {2}", location, set.Version, version);
                        } else {
                            _log.DebugFormat("Updating set '{0}'", location);
                        }
                        responseMsg = DreamMessage.Ok();
                    }
                } else {
                    _log.DebugFormat("no such set: {0}", location);
                    responseMsg = DreamMessage.NotFound("There is no subscription set at this location");
                }
            } catch(ArgumentException e) {
                responseMsg = DreamMessage.Forbidden(e.Message);
            }
            response.Return(responseMsg);
            yield break;
        }
        [DreamFeature("DELETE:subscribers/{location}", "Remove subscription set")]
        protected Yield RemoveSubscribeSet(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var location = context.GetParam("location");
            _dispatcher.RemoveSet(location);
            var msg = DreamMessage.Ok();
            response.Return(msg);
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, IContainer container, Result result) {
            yield return Coroutine.Invoke(base.Start, config, container, new Result());
            _log.DebugFormat("starting {0}", Self.Uri);

            // make sure we have an IPubSubDispatcher registered
            ContainerBuilder builder = null;
            if(!container.IsRegistered<IPubSubDispatcher>()) {
                builder = new ContainerBuilder();
                builder.Register<Dispatcher>().As<IPubSubDispatcher>().ServiceScoped();
            }
            if(!container.IsRegistered<IPubSubDispatchQueueRepository>()) {
                var localQueuePath = config["queue-path"].AsText;
                builder = builder ?? new ContainerBuilder();
                var retryTime = (config["failed-dispatch-retry"].AsInt ?? 60).Seconds();
                if(string.IsNullOrEmpty(localQueuePath)) {
                    _log.Debug("no queue persistent path provided, using memory queues");
                    builder.Register(new MemoryPubSubDispatchQueueRepository(TimerFactory, retryTime))
                        .As<IPubSubDispatchQueueRepository>();
                } else {
                    builder.Register(new PersistentPubSubDispatchQueueRepository(localQueuePath, TimerFactory, retryTime))
                        .As<IPubSubDispatchQueueRepository>();
                }
            }
            if(builder != null) {
                builder.Build(container);
            }

            // initialize dispatcher
            _dispatcher = container.Resolve<IPubSubDispatcher>(
                TypedParameter.From(new DispatcherConfig {
                    ServiceUri = Self,
                    ServiceAccessCookie = DreamCookie.NewSetCookie("service-key", InternalAccessKey, Self.Uri),
                    ServiceCookies = Cookies,
                    ServiceConfig = config
                })
            );

            // check for upstream chaining
            if(!config["upstream"].IsEmpty) {
                _dispatcher.CombinedSet.AsDocument();

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
                            _dispatcher.RegisterSet(StringUtil.CreateAlphaNumericKey(8), subscribeToChanges, StringUtil.CreateAlphaNumericKey(8));
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
                            Tuplet<PubSubSubscriptionSet, bool> set = _dispatcher.RegisterSet(StringUtil.CreateAlphaNumericKey(8), downstreamSet, StringUtil.CreateAlphaNumericKey(8));
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
                var location = context.GetParam("location", null);
                var set = _dispatcher[location];
                if(set != null) {
                    var accessKey = context.GetParam("access-key", null);
                    if(string.IsNullOrEmpty(accessKey)) {
                        var cookie = DreamCookie.GetCookie(request.Cookies, "access-key");
                        if(cookie != null) {
                            accessKey = cookie.Value;
                        }
                    }
                    if(set.AccessKey.EqualsInvariant(accessKey)) {
                        return DreamAccess.Private;
                    }
                    _log.DebugFormat("no matching access-key in query or cookie for location '{0}'", location);
                } else {
                    _log.DebugFormat("no subscription set for location '{0}'", location);
                }
            }
            return base.DetermineAccess(context, request);
        }
    }
}
