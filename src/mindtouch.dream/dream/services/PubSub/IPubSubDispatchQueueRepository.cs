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
using MindTouch.Tasking;

namespace MindTouch.Dream.Services.PubSub {

    /// <summary>
    /// Repository for dispatch queues by subscription.
    /// </summary>
    [Obsolete("The PubSub subsystem has been deprecated and will be removed in v3.0")]
    public interface IPubSubDispatchQueueRepository : IDisposable {

        //--- Properties ---

        /// <summary>
        /// Retrieve the queue for a subscription.
        /// </summary>
        /// <param name="set"><see cref="PubSubSubscriptionSet"/> identifying the dispatch queue.</param>
        /// <returns>Returns the dispatch queue or null, if there is no queue for that set.</returns>
        IPubSubDispatchQueue this[PubSubSubscriptionSet set] { get; }

        //--- Methods ---

        /// <summary>
        /// Retrieve all sets that the repository has loaded from its optional storage. This will only return data before <see cref="InitializeRepository"/> is called, after which the sets will be initialized with queues.
        /// </summary>
        /// <returns></returns>
        IEnumerable<PubSubSubscriptionSet> GetUninitializedSets();

        /// <summary>
        /// Attach the dequeue handler to use for constructing dispatch queues. Calling this will also initalize queues for all sets found in <see cref="GetUninitializedSets"/>.
        /// </summary>
        /// <param name="dequeueHandler">Callback for items to be dispatched.</param>
        void InitializeRepository(Func<DispatchItem, Result<bool>> dequeueHandler);

        /// <summary>
        /// Register or update an existing set and its queue.
        /// </summary>
        /// <param name="set">The set to register.</param>
        void RegisterOrUpdate(PubSubSubscriptionSet set);

        /// <summary>
        /// Delete a set and its queue. Has no effect if the set isn't registered.
        /// </summary>
        /// <param name="set">The set to be deleted.</param>
        void Delete(PubSubSubscriptionSet set);
    }
}