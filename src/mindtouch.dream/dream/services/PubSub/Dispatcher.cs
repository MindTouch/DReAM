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
using System.Linq;
using log4net;
using MindTouch.Collections;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream.Services.PubSub {
    using Yield = IEnumerator<IYield>;

    /// <summary>
    /// Default implementation of <see cref="IPubSubDispatcher"/> with extension points for sub-classing.
    /// </summary>
    public class Dispatcher : IPubSubDispatcher, IDisposable {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly XUri _owner;
        private readonly DreamCookie _serviceKeySetCookie;
        private readonly Dictionary<string, int> _dispatchFailuresByLocation = new Dictionary<string, int>();
        private readonly ProcessingQueue<DispatcherEvent> _dispatchQueue;
        private readonly IPubSubDispatchQueue _defaultQueue;
        private readonly IPersistentPubSubDispatchQueueRepository _queueRepository;
        private Dictionary<XUri, List<PubSubSubscriptionSet>> _subscriptionsByDestination = new Dictionary<XUri, List<PubSubSubscriptionSet>>();
        private PubSubSubscriptionSet _combinedSet;
        private long _combinedSetVersion = 0;

        // NOTE (arnec): always lock _subscriptionsByOwner when accessing either _subscriptionsByOwner or _subscriptionByLocation or _queuesByLocation
        private readonly Dictionary<XUri, PubSubSubscriptionSet> _subscriptionsByOwner = new Dictionary<XUri, PubSubSubscriptionSet>();
        private readonly Dictionary<string, PubSubSubscriptionSet> _subscriptionByLocation = new Dictionary<string, PubSubSubscriptionSet>();

        /// <summary>
        /// Cookie Jar provide by the hosting pub sub service
        /// </summary>
        protected readonly DreamCookieJar _cookieJar = new DreamCookieJar();

        /// <summary>
        /// Collection of dispatch destinations by recipients.
        /// </summary>
        /// <remarks>
        /// Should not be modified by subclass.
        /// </remarks>
        protected Dictionary<DispatcherRecipient, List<PubSubSubscription>> _subscriptionsByRecipient = new Dictionary<DispatcherRecipient, List<PubSubSubscription>>();

        // NOTE (arnec): always lock _channelMap when accessing either _channelMap or _resourceMap

        /// <summary>
        /// Uri lookup table for subscriptions by channel uri.
        /// </summary>
        /// <remarks>
        /// Should not be modified by subclass. When <see cref="_channelMap"/> or <see cref="_resourceMap"/> is accessed a lock should always be
        /// taken on <see cref="_channelMap"/>.
        /// </remarks>
        protected XUriChildMap<PubSubSubscription> _channelMap = new XUriChildMap<PubSubSubscription>();

        /// <summary>
        /// Uri lookup table for subscriptions by resource uri.
        /// </summary>
        /// <remarks>
        /// Should not be modified by subclass. When <see cref="_channelMap"/> or <see cref="_resourceMap"/> is accessed a lock should always be
        /// taken on <see cref="_channelMap"/>.
        /// </remarks>
        protected XUriChildMap<PubSubSubscription> _resourceMap = new XUriChildMap<PubSubSubscription>(true);

        //--- Constructors ---

        /// <summary>
        /// Create a new dispatcher.
        /// </summary>
        /// <param name="config">Configuration instance injected from pub sub service.</param>
        /// <param name="queueRepository">Factory for dispatch queues used by persisted (i.e. expiring) subscriptions</param>
        public Dispatcher(DispatcherConfig config, IPersistentPubSubDispatchQueueRepository queueRepository) {
            _queueRepository = queueRepository;
            _owner = config.ServiceUri.AsServerUri();
            _serviceKeySetCookie = config.ServiceAccessCookie;
            _combinedSet = new PubSubSubscriptionSet(_owner, 0, _serviceKeySetCookie);
            _dispatchQueue = new ProcessingQueue<DispatcherEvent>(DispatchFromQueue, 10);
            _defaultQueue = new ImmediatePubSubDispatchQueue();
            _defaultQueue.SetDequeueHandler(TryDispatchItem);
            var pubSubSubscriptionSets = queueRepository.Initialize(TryDispatchItem);

            // Note (arnec): only invoking lock here, so that RegisterSet and Update don't do it over and over
            lock(_subscriptionsByOwner) {
                foreach(var set in pubSubSubscriptionSets) {
                    RegisterSet(set, true);
                    Update();
                }
            }
        }

        //--- Properties ---

        /// <summary>
        /// The combined set of pub sub subscriptions.
        /// </summary>
        public PubSubSubscriptionSet CombinedSet {
            get { return _combinedSet; }
        }

        /// <summary>
        /// Retrieve a subscription set by location key.
        /// </summary>
        /// <param name="location">Location postfix of subscription set resource uri on the pub sub service.</param>
        /// <returns></returns>
        public PubSubSubscriptionSet this[string location] {
            get {
                lock(_subscriptionsByOwner) {
                    PubSubSubscriptionSet result;
                    _subscriptionByLocation.TryGetValue(location, out result);
                    return result;
                }
            }
        }

        //--- Methods ---

        /// <summary>
        /// Retrieve all uncombined subscription sets.
        /// </summary>
        /// <returns>Enumerable of <see cref="PubSubSubscriptionSet"/>.</returns>
        public IEnumerable<PubSubSubscriptionSet> GetAllSubscriptionSets() {
            lock(_subscriptionsByOwner) {
                return _subscriptionsByOwner.Values.ToList();
            }
        }

        /// <summary>
        /// Register a subscription set
        /// </summary>
        /// <param name="location">location id.</param>
        /// <param name="setDoc">Xml formatted subscription set.</param>
        /// <param name="accessKey">secret key for accessing the set.</param>
        /// <returns>Tuple of subscription set and <see langword="True"/> if the set was newly created, or <see langword="False"/> if the set existed (does not update the set).</returns>
        public Tuplet<PubSubSubscriptionSet, bool> RegisterSet(string location, XDoc setDoc, string accessKey) {
            var set = new PubSubSubscriptionSet(setDoc, location, accessKey);
            return RegisterSet(set, false);
        }

        private Tuplet<PubSubSubscriptionSet, bool> RegisterSet(PubSubSubscriptionSet set, bool init) {
            foreach(var cookie in set.Cookies) {
                _cookieJar.Update(cookie, null);
            }
            lock(_subscriptionsByOwner) {
                PubSubSubscriptionSet existing;
                if(_subscriptionsByOwner.TryGetValue(set.Owner, out existing) || _subscriptionByLocation.TryGetValue(set.Location, out existing)) {
                    return new Tuplet<PubSubSubscriptionSet, bool>(existing, true);
                }
                _subscriptionByLocation.Add(set.Location, set);
                _subscriptionsByOwner.Add(set.Owner, set);
                if(set.HasExpiration && !init) {
                    _queueRepository.RegisterOrUpdate(set);
                }
                if(!init) {
                    Update();
                }
                return new Tuplet<PubSubSubscriptionSet, bool>(set, false);
            }
        }

        /// <summary>
        /// Dispatch an event against the registered subscriptions.
        /// </summary>
        /// <param name="ev">Dispatch event instance.</param>
        public void Dispatch(DispatcherEvent ev) {
            if(ev.HasVisited(_owner)) {

                // this event is in a dispatch loop, so we drop it
                if(_log.IsWarnEnabled) {
                    _log.WarnFormat("event for channel '{0}' already visited the service, dropping", ev.Channel);
                    if(_log.IsDebugEnabled) {
                        _log.Debug("  event origin:");
                        foreach(XUri origin in ev.Origins) {
                            _log.DebugFormat("    - {0}", origin);
                        }
                        _log.Debug("  event route:");
                        foreach(XUri via in ev.Via) {
                            _log.DebugFormat("    - {0}", via);
                        }
                    }
                }
                throw new DreamBadRequestException("Dispatch loop detected: The event has already been dispatched by this service");
            }
            if(_log.IsDebugEnabled) {
                _log.DebugFormat("Dispatcher '{0}' dispatching '{1}' on channel '{2}' with resource '{3}'",
                    _owner,
                    ev.Id,
                    ev.Channel,
                    ev.Resource
                );
            }
            var dispatchEvent = ev.WithVia(_owner);
            if(!_dispatchQueue.TryEnqueue(dispatchEvent)) {
                throw new InvalidOperationException(string.Format("Enqueue of '{0}' failed.", dispatchEvent.Id));
            }
        }

        private void DispatchFromQueue(DispatcherEvent dispatchEvent, Action completionCallback) {
            Coroutine.Invoke(Dispatch_Helper, dispatchEvent, new Result()).WhenDone(
                r => {
                    completionCallback();
                    if(r.HasException) {
                        _log.ErrorExceptionMethodCall(r.Exception, "AsyncDispatcher", "async queue processor encountered an error");
                    } else {
                        _log.DebugFormat("finished enqueuing dispatch of event '{0}'", dispatchEvent.Id);
                    }
                });
        }

        private Yield Dispatch_Helper(DispatcherEvent dispatchEvent, Result result) {
            Dictionary<XUri, List<PubSubSubscription>> listeningSets = null;
            if(dispatchEvent.Recipients != null && dispatchEvent.Recipients.Length > 0) {
                yield return Coroutine.Invoke(GetListenersForRecipients, dispatchEvent, new Result<Dictionary<XUri, List<PubSubSubscription>>>()).Set(v => listeningSets = v);
            } else {
                yield return Coroutine.Invoke(GetListenersByChannelResourceMatch, dispatchEvent, new Result<Dictionary<XUri, List<PubSubSubscription>>>()).Set(v => listeningSets = v);
            }
            if(listeningSets.Count == 0) {
                _log.DebugFormat("event '{0}' for resource '{1}' has no endpoints to dispatch to", dispatchEvent.Id, dispatchEvent.Resource);
            } else {
                _log.DebugFormat("event '{0}' for resource '{1}' ready for dispatch with {2} endpoint(s)", dispatchEvent.Id, dispatchEvent.Resource, listeningSets.Count);
            }
            foreach(var destination in listeningSets) {
                var uri = destination.Key;
                foreach(var sub in destination.Value) {
                    DispatcherEvent subEvent = null;
                    yield return Coroutine.Invoke(DetermineRecipients, dispatchEvent, destination.Value, new Result<DispatcherEvent>()).Set(v => subEvent = v);
                    if(subEvent == null) {
                        _log.DebugFormat("no recipient union for event '{0}' and {1}", dispatchEvent.Id, uri);
                        continue;
                    }
                    IPubSubDispatchQueue queue;
                    if(sub.Owner.HasExpiration) {
                        queue = _queueRepository[sub.Owner];
                        if(queue == null) {
                            _log.DebugFormat("unable to get dispatch queue for event '{0}'via location '{1}'", dispatchEvent.Id, sub.Owner.Location);
                            continue;
                        }
                    } else {
                        queue = _defaultQueue;
                    }
                    queue.Enqueue(new DispatchItem(uri, subEvent, sub.Owner.Location));
                }
            }
            result.Return();
            yield break;
        }

        private Result<bool> TryDispatchItem(DispatchItem item) {
            var result = new Result<bool>();
            _log.DebugFormat("dispatching event '{0}' to {1}", item.Event.Id, item.Uri);
            Plug.New(item.Uri)
                .WithCookieJar(_cookieJar)
                .Post(item.Event.AsMessage(), new Result<DreamMessage>(TimeSpan.MaxValue))
                .WhenDone(r => DispatchCompletion_Helper(item, r.Value, result));
            return result;
        }

        /// <summary>
        /// Override hook for modifying the selection of event listeners based on channel and resource matches.
        /// </summary>
        /// <param name="ev">Event to be dispatched.</param>
        /// <param name="result">The <see cref="Result"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle for the action's execution.</returns>
        protected virtual Yield GetListenersByChannelResourceMatch(DispatcherEvent ev, Result<Dictionary<XUri, List<PubSubSubscription>>> result) {

            // dispatch to all subscriptions that listen for this event and its contents
            _log.Debug("trying dispatch based on channel matches");
            ICollection<PubSubSubscription> listeningSubs = null;
            lock(_channelMap) {
                if(ev.Resource != null) {
                    listeningSubs = _resourceMap.GetMatches(ev.Resource);
                }
                listeningSubs = _channelMap.GetMatches(ev.Channel, listeningSubs);
            }
            var listeners = new Dictionary<XUri, List<PubSubSubscription>>();
            foreach(var sub in listeningSubs) {
                List<PubSubSubscription> subs;
                if(!listeners.TryGetValue(sub.Destination, out subs)) {
                    subs = new List<PubSubSubscription>();
                    listeners.Add(sub.Destination, subs);
                    subs.Add(sub);
                } else if(!subs.Contains(sub)) {
                    subs.Add(sub);
                }
            }
            result.Return(listeners);
            yield break;
        }

        /// <summary>
        /// Override hook for modifying the selection of event listeners based on recipients subscribed to the event.
        /// </summary>
        /// <param name="ev">Event to be dispatched.</param>
        /// <param name="result">The <see cref="Result"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle for the action's execution.</returns>
        protected virtual Yield GetListenersForRecipients(DispatcherEvent ev, Result<Dictionary<XUri, List<PubSubSubscription>>> result) {

            //if the event has recipients attached, do subscription lookup by recipients
            _log.Debug("trying dispatch based on event recipient list event");
            lock(_subscriptionsByRecipient) {
                var listeners = new Dictionary<XUri, List<PubSubSubscription>>();
                foreach(var recipient in ev.Recipients) {
                    List<PubSubSubscription> subscriptions;
                    if(!_subscriptionsByRecipient.TryGetValue(recipient, out subscriptions)) {
                        continue;
                    }
                    foreach(var sub in subscriptions) {
                        List<PubSubSubscription> subs;
                        if(!listeners.TryGetValue(sub.Destination, out subs)) {
                            subs = new List<PubSubSubscription>();
                            listeners.Add(sub.Destination, subs);
                            subs.Add(sub);
                        } else if(!subs.Contains(sub)) {
                            subs.Add(sub);
                        }
                    }
                }
                result.Return(listeners);
            }
            yield break;
        }


        /// <summary>
        /// Override hook for filtering recipients for an event.
        /// </summary>
        /// <param name="ev">Event to be dispatched.</param>
        /// <param name="subscriptions">List of matching subscriptions.</param>
        /// <param name="result">The <see cref="Result"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle for the action's execution.</returns>
        protected virtual Yield DetermineRecipients(DispatcherEvent ev, List<PubSubSubscription> subscriptions, Result<DispatcherEvent> result) {
            if(ev.Recipients.Length == 0) {

                //if the event has no reciepient list, anyone can receive it
                result.Return(ev);
                yield break;
            }
            var subscribers = subscriptions.SelectMany(x => x.Recipients).ToArray();
            var recipients = ArrayUtil.Intersect(ev.Recipients, subscribers);
            result.Return(recipients.Length == 0 ? null : ev.WithRecipient(true, recipients));
            yield break;
        }

        private void DispatchCompletion_Helper(DispatchItem destination, DreamMessage response, Result<bool> result) {
            // CLEANUP: proper access to lookup
            var set = _subscriptionByLocation[destination.Location];
            if(set.HasExpiration) {
                if(response.IsSuccessful || response.Status == DreamStatus.NotModified) {
                    result.Return(true);
                } else {
                    result.Return(false);
                }
            } else {
                if(response.IsSuccessful || response.Status == DreamStatus.NotModified) {
                    // if the post was a success, or didn't affect a change, clear any failure count
                    lock(_dispatchFailuresByLocation) {
                        if(_log.IsDebugEnabled) {
                            if(_dispatchFailuresByLocation.ContainsKey(destination.Location)) {
                                _log.Debug("zeroing out existing error count");
                            }
                        }
                        _dispatchFailuresByLocation.Remove(destination.Location);
                    }
                } else {

                    // post was a failure, increase consecutive failures
                    if(_log.IsWarnEnabled) {
                        _log.WarnFormat("event dispatch to '{0}' failed: {1} - {2}", destination, response.Status, response.ToText());
                    }
                    lock(_dispatchFailuresByLocation) {

                        // NOTE (arnec): using ContainsKey instead of TryGetValue, since we're incrementing a value type in place
                        if(!_dispatchFailuresByLocation.ContainsKey(destination.Location)) {
                            _dispatchFailuresByLocation.Add(destination.Location, 1);
                        } else {
                            _dispatchFailuresByLocation[destination.Location]++;
                        }
                        var failures = _dispatchFailuresByLocation[destination.Location];
                        _log.DebugFormat("failure {0} out of {1} for set at location {2}", failures, set.MaxFailures, destination.Location);

                        // kick out a subscription set if one of its subscriptions fails too many times
                        if(failures > set.MaxFailures) {
                            _log.DebugFormat("exceeded max failures, kicking set at '{0}'", set.Location);
                            RemoveSet(destination.Location);
                        }
                    }
                }

                // Note (arnec): non-expiring sets always "succeed" at dispatch since their queues are not kept around
                result.Return(true);
                return;

            }

        }

        /// <summary>
        /// Replace an existing set.
        /// </summary>
        /// <param name="location">Set resource location uri postfix.</param>
        /// <param name="setDoc">New set document.</param>
        /// <param name="accessKey"></param>
        /// <returns>Updated set.</returns>
        public PubSubSubscriptionSet ReplaceSet(string location, XDoc setDoc, string accessKey) {
            lock(_subscriptionsByOwner) {
                PubSubSubscriptionSet oldSet;
                if(!_subscriptionByLocation.TryGetValue(location, out oldSet)) {
                    return null;
                }
                PubSubSubscriptionSet set = oldSet.Derive(setDoc, accessKey);
                if(set == oldSet) {
                    return oldSet;
                }
                if(set.Owner != oldSet.Owner) {
                    _log.WarnFormat("subscription set owner mispatch: {0} vs. {1}", oldSet.Owner, set.Owner);
                    throw new ArgumentException("owner of new set does not match existing owner");
                }
                if(set.HasExpiration != oldSet.HasExpiration) {
                    _log.WarnFormat("attempted to change a subscription type (expiring vs. non-expiring)");
                    throw new ArgumentException("new set has different expiration type");
                }
                foreach(DreamCookie cookie in set.Cookies) {
                    _cookieJar.Update(cookie, null);
                }
                _subscriptionByLocation[location] = set;
                _subscriptionsByOwner[set.Owner] = set;
                Update();
                return set;
            }
        }

        /// <summary>
        /// Remove a set.
        /// </summary>
        /// <param name="location">Set resource location uri postfix.</param>
        /// <returns><see langword="True"/> if a set existed at the provided location.</returns>
        public bool RemoveSet(string location) {
            lock(_subscriptionsByOwner) {
                PubSubSubscriptionSet set;
                if(!_subscriptionByLocation.TryGetValue(location, out set)) {
                    return false;
                }
                bool result = _subscriptionsByOwner.Remove(set.Owner);
                _subscriptionByLocation.Remove(location);
                _queueRepository.Delete(set);
                Update();
                return result;
            }
        }

        public void Dispose() {
            lock(_subscriptionsByOwner) {
                _queueRepository.Dispose();
                _defaultQueue.Dispose();
            }
        }

        private void Update() {
            Async.Fork(Update_Helper, new Result());
        }

        private void Update_Helper() {
            lock(_subscriptionsByOwner) {
                PubSubSubscription[] allSubs = CalculateCombinedSubscriptions();
                _combinedSetVersion++;
                _combinedSet = new PubSubSubscriptionSet(_owner, _combinedSetVersion, _serviceKeySetCookie, allSubs);
            }

            // this is outside the lock, since it may be an expensive operation and shouldn't block lookups
            DispatcherEvent ev = new DispatcherEvent(_combinedSet.AsDocument(), new XUri("pubsub:///set/update"), null, _owner);
            _log.DebugFormat("updated combined set, dispatching as id '{0}'", ev.Id);
            Dispatch(ev);
            if(CombinedSetUpdated != null) {
                CombinedSetUpdated(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Event fired anytime the combined subscription set has changed.
        /// </summary>
        public event EventHandler<EventArgs> CombinedSetUpdated;

        /// <summary>
        /// Override hook for modifying/augmenting the calculation of the combined subscription set.
        /// </summary>
        /// <returns>Returns all subscription sets used to calculate the new combined set.</returns>
        protected virtual PubSubSubscription[] CalculateCombinedSubscriptions() {
            PubSubSubscription[] allSubs;
            lock(_subscriptionsByOwner) {
                var tempChannelMap = new XUriChildMap<PubSubSubscription>();
                var tempResourceMap = new XUriChildMap<PubSubSubscription>(true);
                var tempRecipients = new Dictionary<DispatcherRecipient, List<PubSubSubscription>>();
                var tempSubs = new Dictionary<XUri, List<PubSubSubscriptionSet>>();
                var allSubsList = new List<PubSubSubscription>();
                foreach(var set in _subscriptionsByOwner.Values) {
                    foreach(var sub in set.Subscriptions) {
                        tempChannelMap.AddRange(sub.Channels, sub);
                        if(sub.Resources != null && sub.Resources.Length > 0) {
                            tempResourceMap.AddRange(sub.Resources, sub);
                        } else {
                            tempResourceMap.Add(new XUri("x://*/*"), sub);
                        }
                        allSubsList.Add(sub);
                        List<PubSubSubscriptionSet> sets;
                        if(!tempSubs.TryGetValue(sub.Destination, out sets)) {
                            sets = new List<PubSubSubscriptionSet>();
                            tempSubs.Add(sub.Destination, sets);
                        }
                        if(!sets.Contains(set)) {
                            sets.Add(set);
                        }
                        foreach(var recipient in sub.Recipients) {
                            List<PubSubSubscription> destinations;
                            if(!tempRecipients.TryGetValue(recipient, out destinations)) {
                                destinations = new List<PubSubSubscription>();
                                tempRecipients.Add(recipient, destinations);
                            }
                            if(!destinations.Contains(sub)) {
                                destinations.Add(sub);
                            }
                        }
                    }
                }
                allSubs = allSubsList.ToArray();
                lock(_channelMap) {
                    _channelMap = tempChannelMap;
                    _resourceMap = tempResourceMap;
                }
                lock(_subscriptionsByDestination) {
                    _subscriptionsByDestination = tempSubs;
                }
                lock(_subscriptionsByRecipient) {
                    _subscriptionsByRecipient = tempRecipients;
                }
            }
            return allSubs;
        }
    }
}
