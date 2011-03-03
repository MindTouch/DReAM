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
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream.Services.PubSub {

    /// <summary>
    /// Provides an object model for a pub sub subscription.
    /// </summary>
    public class PubSubSubscription {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---

        /// <summary>
        /// Merge two subscriptions on matchine channel and resource.
        /// </summary>
        /// <param name="channel">Common channel.</param>
        /// <param name="resource">Common resource.</param>
        /// <param name="owner">Common owner.</param>
        /// <param name="cookie">Subscription set cookie.</param>
        /// <param name="first">First subscription to merge.</param>
        /// <param name="second">Second subscription to merge.</param>
        /// <returns></returns>
        public static PubSubSubscription MergeForChannelAndResource(XUri channel, XUri resource, PubSubSubscriptionSet owner, DreamCookie cookie, PubSubSubscription first, PubSubSubscription second) {
            return new PubSubSubscription(channel, resource, owner, cookie, first, second);
        }

        //--- Fields ---

        /// <summary>
        /// Subscription Id.
        /// </summary>
        public readonly string Id;

        /// <summary>
        /// Subscription channels.
        /// </summary>
        public readonly XUri[] Channels;

        /// <summary>
        /// Subscription resources.
        /// </summary>
        public readonly XUri[] Resources;

        /// <summary>
        /// Subscription dispatch destination.
        /// </summary>
        public readonly XUri Destination;

        /// <summary>
        /// Subscription recipients.
        /// </summary>
        public readonly DispatcherRecipient[] Recipients;

        /// <summary>
        /// Subscription owner.
        /// </summary>
        public readonly PubSubSubscriptionSet Owner;

        /// <summary>
        /// Dispatch access cookie.
        /// </summary>
        public readonly DreamCookie Cookie;

        private readonly bool _isProxy;

        //--- Constructors ---
        private PubSubSubscription(XUri channel, XUri resource, PubSubSubscriptionSet owner, DreamCookie cookie, PubSubSubscription first, PubSubSubscription second) {
            if(channel == null) {
                throw new ArgumentNullException("channel");
            }
            Channels = new[] { channel };
            Resources = resource == null ? new XUri[0] : new[] { resource };
            Id = Guid.NewGuid().ToString();
            Owner = owner;
            Destination = Owner.Owner.At("publish");
            Cookie = cookie;
            Recipients = ArrayUtil.Union(first.Recipients, (second == null) ? new DispatcherRecipient[0] : second.Recipients);
            _isProxy = true;
        }


        /// <summary>
        /// Create a subscription from a subscription document.
        /// </summary>
        /// <param name="sub">Subscription document.</param>
        /// <param name="owner">Owning set.</param>
        public PubSubSubscription(XDoc sub, PubSubSubscriptionSet owner) {
            Owner = owner;
            // sanity check the input
            XDoc channels = sub["channel"];
            if(channels.IsEmpty) {
                throw new ArgumentException("<subscription> must have at least one <channel>");
            }
            XDoc filter = sub["filter"];
            if(filter.ListLength > 1) {
                throw new ArgumentException("<subscription> must have zero or one <filter>");
            }
            XDoc proxy = sub["uri.proxy"];
            if(proxy.ListLength > 1) {
                throw new ArgumentException("<subscription> must have zero or one <uri.proxy>");
            }
            XDoc recipients = sub["recipient"];
            if(recipients.IsEmpty) {
                throw new ArgumentException("<subscription> must have at least one valid <recipient>");
            }
            if(recipients.ListLength > 1 && proxy.ListLength == 0) {
                throw new ArgumentException("<subscription> must include <uri.proxy> if there is more than one <recipient>");
            }

            // create our internal representation
            try {
                Id = sub["@id"].Contents;
                if(string.IsNullOrEmpty(Id)) {
                    Id = Guid.NewGuid().ToString();
                }
                XDoc cookie = sub["set-cookie"];
                if(!cookie.IsEmpty) {
                    Cookie = DreamCookie.ParseSetCookie(cookie);
                }
                List<XUri> channelList = new List<XUri>();
                foreach(XDoc c in channels) {
                    channelList.Add(c.AsUri);
                }
                Channels = channelList.ToArray();
                List<XUri> resourceList = new List<XUri>();
                foreach(XDoc r in sub["uri.resource"]) {
                    resourceList.Add(r.AsUri);
                }
                Resources = resourceList.ToArray();
                if(proxy.IsEmpty) {
                    Destination = new DispatcherRecipient(recipients).Uri;
                } else {
                    Destination = proxy.AsUri;
                    _isProxy = true;
                }
                List<DispatcherRecipient> recipientList = new List<DispatcherRecipient>();
                foreach(XDoc recipient in recipients) {
                    recipientList.Add(new DispatcherRecipient(recipient));
                }
                Recipients = recipientList.ToArray();
            } catch(Exception e) {
                throw new ArgumentException("Unable to parse subscription: " + e.Message, e);
            }
        }

        //--- Methods ---

        /// <summary>
        /// Create a subscription document.
        /// </summary>
        /// <returns>Subscription Xml document.</returns>
        public XDoc AsDocument() {
            XDoc doc = new XDoc("subscription")
                      .Attr("id", Id);
            foreach(XUri channel in Channels) {
                doc.Elem("channel", channel);
            }
            if(Resources != null) {
                foreach(XUri resource in Resources) {
                    doc.Elem("uri.resource", resource);
                }
            }
            if(Cookie != null) {
                doc.Add(Cookie.AsSetCookieDocument);
            }
            if(_isProxy) {
                doc.Elem("uri.proxy", Destination);
            }
            foreach(DispatcherRecipient recipient in Recipients) {
                doc.Add(recipient.AsDocument());
            }
            return doc;
        }
    }
}
