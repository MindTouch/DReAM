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
    public class Dispatcher : IPubSubDispatcher {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly XUri _owner;
        private readonly DreamCookie _serviceKeySetCookie;
        private readonly Dictionary<XUri, int> _dispatchFailuresByDestination = new Dictionary<XUri, int>();
        private readonly ProcessingQueue<DispatcherEvent> _dispatchQueue;
        private Dictionary<XUri, List<PubSubSubscriptionSet>> _subscriptionsByDestination = new Dictionary<XUri, List<PubSubSubscriptionSet>>();
        private PubSubSubscriptionSet _combinedSet;
        private long _combinedSetVersion = 0;


        // NOTE (arnec): always lock _subscriptionsByOwner when accessing either _subscriptionsByOwner or _subscriptionByLocation
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
        protected Dictionary<DispatcherRecipient, List<XUri>> _destinationsByRecipient = new Dictionary<DispatcherRecipient, List<XUri>>();

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
        public Dispatcher(DispatcherConfig config) {
            _owner = config.ServiceUri.AsServerUri();
            _serviceKeySetCookie = config.ServiceAccessCookie;
            _combinedSet = new PubSubSubscriptionSet(_owner, 0, _serviceKeySetCookie);
            _dispatchQueue = new ProcessingQueue<DispatcherEvent>(DispatchFromQueue, 10);
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
                List<PubSubSubscriptionSet> sets = new List<PubSubSubscriptionSet>();
                foreach(PubSubSubscriptionSet set in _subscriptionsByOwner.Values) {
                    sets.Add(set);
                }
                return sets;
            }
        }

        /// <summary>
        /// Register a subscription set
        /// </summary>
        /// <param name="setDoc">Xml formatted subscription set.</param>
        /// <returns>Tuple of subscription set and <see langword="True"/> if the set was newly created, or <see langword="False"/> if the set existed (does not update the set).</returns>
        public Tuplet<PubSubSubscriptionSet, bool> RegisterSet(XDoc setDoc) {
            PubSubSubscriptionSet set = new PubSubSubscriptionSet(setDoc);
            foreach(DreamCookie cookie in set.Cookies) {
                _cookieJar.Update(cookie, null);
            }
            lock(_subscriptionsByOwner) {
                PubSubSubscriptionSet existing;
                if(_subscriptionsByOwner.TryGetValue(set.Owner, out existing)) {
                    return new Tuplet<PubSubSubscriptionSet, bool>(existing, true);
                }
                _subscriptionByLocation.Add(set.Location, set);
                _subscriptionsByOwner.Add(set.Owner, set);
                Update();
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
                                        ev.Resource);
            }
            DispatcherEvent dispatchEvent = ev.WithVia(_owner);
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
                        _log.DebugFormat("finished dispatch of event '{0}'", dispatchEvent.Id);
                    }
                });
        }

        private Yield Dispatch_Helper(DispatcherEvent dispatchEvent, Result result) {
            Dictionary<XUri, List<DispatcherRecipient>> listeningEndpoints = null;
            if(dispatchEvent.Recipients != null && dispatchEvent.Recipients.Length > 0) {
                yield return Coroutine.Invoke(GetListenersForRecipients, dispatchEvent, new Result<Dictionary<XUri, List<DispatcherRecipient>>>()).Set(v => listeningEndpoints = v);
            } else {
                yield return Coroutine.Invoke(GetListenersByChannelResourceMatch, dispatchEvent, new Result<Dictionary<XUri, List<DispatcherRecipient>>>()).Set(v => listeningEndpoints = v);
            }
            if(listeningEndpoints.Count == 0) {
                _log.DebugFormat("event '{0}' for resource '{1}' has no endpoints to dispatch to", dispatchEvent.Id, dispatchEvent.Resource);
            } else {
                _log.DebugFormat("event '{0}' for resource '{1}' ready for dispatch with {2} endpoint(s)", dispatchEvent.Id, dispatchEvent.Resource, listeningEndpoints.Count);
            }
            foreach(KeyValuePair<XUri, List<DispatcherRecipient>> destination in listeningEndpoints) {
                var uri = destination.Key;
                DispatcherEvent subEvent = null;
                yield return Coroutine.Invoke(DetermineRecipients, dispatchEvent, destination.Value.ToArray(), new Result<DispatcherEvent>()).Set(v => subEvent = v);
                if(subEvent == null) {
                    _log.DebugFormat("no recipient union for event '{0}' and {1}", dispatchEvent.Id, uri);
                    continue;
                }
                _log.DebugFormat("dispatching event '{0}' to {1}", subEvent.Id, uri);
                Plug p = Plug.New(uri);
                p = p.WithCookieJar(_cookieJar);
                Result<DreamMessage> response = p.Post(subEvent.AsMessage(), new Result<DreamMessage>(TimeSpan.MaxValue));
                response.WhenDone(r => DispatchCompletion_Helper(uri, r));
            }
            result.Return();
            yield break;
        }

        /// <summary>
        /// Override hook for modifying the selection of event listeners based on channel and resource matches.
        /// </summary>
        /// <param name="ev">Event to be dispatched.</param>
        /// <param name="result">The <see cref="Result"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle for the action's execution.</returns>
        protected virtual Yield GetListenersByChannelResourceMatch(DispatcherEvent ev, Result<Dictionary<XUri, List<DispatcherRecipient>>> result) {

            // dispatch to all subscriptions that listen for this event and its contents
            _log.Debug("trying dispatch based on channel matches");
            ICollection<PubSubSubscription> listeningSubs = null;
            lock(_channelMap) {
                if(ev.Resource != null) {
                    listeningSubs = _resourceMap.GetMatches(ev.Resource);
                }
                listeningSubs = _channelMap.GetMatches(ev.Channel, listeningSubs);
            }
            Dictionary<XUri, List<DispatcherRecipient>> listeners = new Dictionary<XUri, List<DispatcherRecipient>>();
            foreach(PubSubSubscription sub in listeningSubs) {
                List<DispatcherRecipient> recipients;
                if(!listeners.TryGetValue(sub.Destination, out recipients)) {
                    recipients = new List<DispatcherRecipient>();
                    listeners.Add(sub.Destination, recipients);
                }
                foreach(DispatcherRecipient recipient in sub.Recipients) {
                    if(!recipients.Contains(recipient)) {
                        recipients.Add(recipient);
                    }
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
        protected virtual Yield GetListenersForRecipients(DispatcherEvent ev, Result<Dictionary<XUri, List<DispatcherRecipient>>> result) {

            //if the event has recipients attached, do subscription lookup by recipients
            _log.Debug("trying dispatch based on event recipient list event");
            lock(_destinationsByRecipient) {
                Dictionary<XUri, List<DispatcherRecipient>> listeners = new Dictionary<XUri, List<DispatcherRecipient>>();
                foreach(DispatcherRecipient recipient in ev.Recipients) {
                    List<XUri> destinations;
                    if(_destinationsByRecipient.TryGetValue(recipient, out destinations)) {
                        foreach(XUri destination in destinations) {
                            List<DispatcherRecipient> recipients;
                            if(!listeners.TryGetValue(destination, out recipients)) {
                                recipients = new List<DispatcherRecipient>();
                                listeners.Add(destination, recipients);
                            }
                            if(!recipients.Contains(recipient)) {
                                recipients.Add(recipient);
                            }
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
        /// <param name="recipients">List of proposed recipients.</param>
        /// <param name="result">The <see cref="Result"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle for the action's execution.</returns>
        protected virtual Yield DetermineRecipients(DispatcherEvent ev, DispatcherRecipient[] recipients, Result<DispatcherEvent> result) {
            if(ev.Recipients.Length == 0) {

                //if the event has no reciepient list, anyone can receive it
                result.Return(ev);
                yield break;
            }
            recipients = ArrayUtil.Intersect(ev.Recipients, recipients);
            result.Return(recipients.Length == 0 ? null : ev.WithRecipient(true, recipients));
            yield break;
        }

        private void DispatchCompletion_Helper(XUri destination, Result<DreamMessage> result) {
            if(result.Value.IsSuccessful || result.Value.Status == DreamStatus.NotModified) {

                // if the post was a success, or didn't affect a change, clear any failure count
                lock(_dispatchFailuresByDestination) {
                    if(_log.IsDebugEnabled) {
                        if(_dispatchFailuresByDestination.ContainsKey(destination)) {
                            _log.Debug("zeroing out existing error count");
                        }
                    }
                    _dispatchFailuresByDestination.Remove(destination);
                }
            } else {

                // post was a failure, increase consecutive failures
                if(_log.IsWarnEnabled) {
                    _log.WarnFormat("event dispatch to '{0}' failed: {1} - {2}", destination, result.Value.Status, result.Value.ToText());
                }
                lock(_dispatchFailuresByDestination) {

                    // NOTE (arnec): using ContainsKey instead of TryGetValue, since we're incrementing a value type in place
                    if(!_dispatchFailuresByDestination.ContainsKey(destination)) {
                        _dispatchFailuresByDestination.Add(destination, 1);
                    } else {
                        _dispatchFailuresByDestination[destination]++;
                    }
                    List<PubSubSubscriptionSet> subscriptionSets;
                    lock(_subscriptionsByDestination) {
                        if(!_subscriptionsByDestination.TryGetValue(destination, out subscriptionSets)) {
                            return;
                        }
                    }
                    foreach(PubSubSubscriptionSet set in subscriptionSets) {
                        _log.DebugFormat("failure {0} out of {1} for {2}",
                                                _dispatchFailuresByDestination[destination],
                                                set.MaxFailures,
                                                destination);

                        // kick out a subscription set if one of its subscriptions fails too many times
                        if(_dispatchFailuresByDestination[destination] > set.MaxFailures) {
                            _log.DebugFormat("exceeded max failures, kicking set at '{0}'", set.Location);
                            RemoveSet(set.Location);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Replace an existing set.
        /// </summary>
        /// <param name="location">Set resource location uri postfix.</param>
        /// <param name="setDoc">New set document.</param>
        /// <returns>Updated set.</returns>
        public PubSubSubscriptionSet ReplaceSet(string location, XDoc setDoc) {
            lock(_subscriptionsByOwner) {
                PubSubSubscriptionSet oldSet;
                if(!_subscriptionByLocation.TryGetValue(location, out oldSet)) {
                    return null;
                }
                PubSubSubscriptionSet set = oldSet.Derive(setDoc);
                if(set == oldSet) {
                    return oldSet;
                }
                foreach(DreamCookie cookie in set.Cookies) {
                    _cookieJar.Update(cookie, null);
                }
                if(set.Owner != oldSet.Owner) {
                    _log.WarnFormat("subscription set owner mispatch: {0} vs. {1}", oldSet.Owner, set.Owner);
                    throw new ArgumentException("owner of new set does not match existing owner");
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
                Update();
                return result;
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
                XUriChildMap<PubSubSubscription> tempChannelMap = new XUriChildMap<PubSubSubscription>();
                XUriChildMap<PubSubSubscription> tempResourceMap = new XUriChildMap<PubSubSubscription>(true);
                Dictionary<DispatcherRecipient, List<XUri>> tempRecipients = new Dictionary<DispatcherRecipient, List<XUri>>();
                Dictionary<XUri, List<PubSubSubscriptionSet>> tempSubs = new Dictionary<XUri, List<PubSubSubscriptionSet>>();
                List<PubSubSubscription> allSubsList = new List<PubSubSubscription>();
                foreach(PubSubSubscriptionSet set in _subscriptionsByOwner.Values) {
                    foreach(PubSubSubscription sub in set.Subscriptions) {
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
                        foreach(DispatcherRecipient recipient in sub.Recipients) {
                            List<XUri> destinations;
                            if(!tempRecipients.TryGetValue(recipient, out destinations)) {
                                destinations = new List<XUri>();
                                tempRecipients.Add(recipient, destinations);
                            }
                            if(!destinations.Contains(sub.Destination)) {
                                destinations.Add(sub.Destination);
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
                lock(_destinationsByRecipient) {
                    _destinationsByRecipient = tempRecipients;
                }
            }
            return allSubs;
        }
    }
}
