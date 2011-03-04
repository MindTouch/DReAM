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
using MindTouch.Xml;

namespace MindTouch.Dream.Services.PubSub {

    /// <summary>
    /// Contract for implementing an event dispatcher for the pub sub service.
    /// </summary>
    /// <remarks>
    /// The default implementation is <see cref="Dispatcher"/>.
    /// </remarks>
    public interface IPubSubDispatcher {

        //--- Properties ---

        /// <summary>
        /// The combined set of pub sub subscriptions.
        /// </summary>
        PubSubSubscriptionSet CombinedSet { get; }

        /// <summary>
        /// Retrieve a subscription set by location key.
        /// </summary>
        /// <param name="location">Location postfix of subscription set resource uri on the pub sub service.</param>
        /// <returns></returns>
        PubSubSubscriptionSet this[string location] { get; }

        //--- Methods ---

        /// <summary>
        /// Retrieve all uncombined subscription sets.
        /// </summary>
        /// <returns>Enumerable of <see cref="PubSubSubscriptionSet"/>.</returns>
        IEnumerable<PubSubSubscriptionSet> GetAllSubscriptionSets();

        /// <summary>
        /// Register a subscription set
        /// </summary>
        /// <param name="setDoc">Xml formatted subscription set.</param>
        /// <returns>Tuple of subscription set and <see langword="True"/> if the set was newly created, or <see langword="False"/> if the set existed (does not update the set).</returns>
        Tuplet<PubSubSubscriptionSet, bool> RegisterSet(XDoc setDoc);

        /// <summary>
        /// Dispatch an event against the registered subscriptions.
        /// </summary>
        /// <param name="ev">Dispatch event instance.</param>
        void Dispatch(DispatcherEvent ev);

        /// <summary>
        /// Replace an existing set.
        /// </summary>
        /// <param name="location">Set resource location uri postfix.</param>
        /// <param name="setDoc">New set document.</param>
        /// <returns>Updated set.</returns>
        PubSubSubscriptionSet ReplaceSet(string location, XDoc setDoc);

        /// <summary>
        /// Remove a set.
        /// </summary>
        /// <param name="location">Set resource location uri postfix.</param>
        /// <returns><see langword="True"/> if a set existed at the provided location.</returns>
        bool RemoveSet(string location);

        //--- Events ---

        /// <summary>
        /// Event fired anytime the combined subscription set has changed.
        /// </summary>
        event EventHandler<EventArgs> CombinedSetUpdated;
    }
}
