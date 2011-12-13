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

namespace MindTouch.Dream.Services.PubSub {

    /// <summary>
    /// Serializable container for an <see cref="DispatcherEvent"/> for a specific <see cref="PubSubSubscriptionSet"/> identified by its location.
    /// </summary>
    [Obsolete("The PubSub subsystem has been deprecated and will be removed in v3.0")]
    public class DispatchItem {

        //--- Fields ---

        /// <summary>
        /// Uri the event will be dispatched to.
        /// </summary>
        public readonly XUri Uri;

        /// <summary>
        /// Event to dispatch.
        /// </summary>
        public readonly DispatcherEvent Event;

        /// <summary>
        /// Location string identifying the <see cref="PubSubSubscriptionSet"/> the event is being dispatched for.
        /// </summary>
        public readonly string Location;

        //--- Constructors ---

        /// <summary>
        /// Create a new dispatch item
        /// </summary>
        /// <param name="uri">Uri the event will be dispatched to.</param>
        /// <param name="event">Event to dispatch.</param>
        /// <param name="location">Location string identifying the <see cref="PubSubSubscriptionSet"/> the event is being dispatched for.</param>
        public DispatchItem(XUri uri, DispatcherEvent @event, string location) {
            Uri = uri;
            Event = @event;
            Location = location;
        }
    }
}