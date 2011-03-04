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
using MindTouch.Xml;

namespace MindTouch.Dream.Services.PubSub {

    /// <summary>
    /// Event processed and sent by the <see cref="IPubSubDispatcher"/>.
    /// </summary>
    public class DispatcherEvent {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly DreamMessage _message;

        /// <summary>
        /// Event Id.
        /// </summary>
        public readonly string Id;

        /// <summary>
        /// Event Channel.
        /// </summary>
        public readonly XUri Channel;

        /// <summary>
        /// Optional Event Resource.
        /// </summary>
        public readonly XUri Resource;

        /// <summary>
        /// List of origins of the event.
        /// </summary>
        public readonly XUri[] Origins;

        /// <summary>
        /// List of recipients of the event derived from matched subscriptions.
        /// </summary>
        public readonly DispatcherRecipient[] Recipients;

        /// <summary>
        /// List of Pub Sub services this event has passed through.
        /// </summary>
        public readonly XUri[] Via;

        //--- Constructors ---

        /// <summary>
        /// Create a new event from a dream message.
        /// </summary>
        /// <param name="message">Message to parse.</param>
        public DispatcherEvent(DreamMessage message) {

            // sanity check the input
            string[] origins = message.Headers.DreamEventOrigin;
            if(origins.Length == 0) {
                throw new DreamBadRequestException(string.Format("message must specify at least one DreamEventOrigin header"));
            }
            if(string.IsNullOrEmpty(message.Headers.DreamEventChannel)) {
                throw new DreamBadRequestException("message must have exactly one DreamEventChannel header");
            }

            // parse message
            _message = message.Clone();
            Channel = new XUri(_message.Headers.DreamEventChannel);
            if(!string.IsNullOrEmpty(_message.Headers.DreamEventResource)) {
                Resource = new XUri(_message.Headers.DreamEventResource);
            }
            List<XUri> originList = new List<XUri>();
            foreach(string origin in origins) {
                originList.Add(new XUri(origin));
            }
            Origins = originList.ToArray();
            List<DispatcherRecipient> recipientList = new List<DispatcherRecipient>();
            foreach(string recipient in _message.Headers.DreamEventRecipients) {
                recipientList.Add(new DispatcherRecipient(new XUri(recipient)));
            }
            Recipients = recipientList.ToArray();
            List<XUri> viaList = new List<XUri>();
            foreach(string via in _message.Headers.DreamEventVia) {
                viaList.Add(new XUri(via));
            }
            Via = viaList.ToArray();
            // attach an Id, if one does not exist
            Id = message.Headers.DreamEventId;
            if(string.IsNullOrEmpty(Id)) {
                Id = Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Create a new event from an Xml payload
        /// </summary>
        /// <param name="data">Event payload.</param>
        /// <param name="channel">Event channel.</param>
        /// <param name="resource">Event resource.</param>
        /// <param name="origins"> Event origins.</param>
        public DispatcherEvent(XDoc data, XUri channel, XUri resource, params XUri[] origins)
            : this(DreamMessage.Ok(data), channel, resource, origins) {
        }

        /// <summary>
        /// Create a new event from a message body payload.
        /// </summary>
        /// <remarks>
        /// This constructor assumes that the message provided was newly created for the event and belongs to the event.
        /// </remarks>
        /// <param name="message">Event payload.</param>
        /// <param name="channel">Event channel.</param>
        /// <param name="resource">Event resource.</param>
        /// <param name="origins"> Event origins.</param>
        public DispatcherEvent(DreamMessage message, XUri channel, XUri resource, params XUri[] origins) {
            if(resource == null && (origins == null || origins.Length == 0)) {
                throw new ArgumentException("Event must have at least one origin Uri");
            }
            _message = message;
            Id = Guid.NewGuid().ToString();
            Channel = channel;
            Resource = resource;
            if(origins == null || origins.Length == 0) {
                origins = new XUri[] { Resource };
            }
            Origins = origins;
            Recipients = new DispatcherRecipient[0];
            Via = new XUri[0];
        }

        private DispatcherEvent(DispatcherEvent ev, XUri[] via, DispatcherRecipient[] recipients) {
            _message = ev._message;
            Id = ev.Id;
            Channel = ev.Channel;
            Resource = ev.Resource;
            Origins = ev.Origins;
            Recipients = recipients;
            Via = via;
        }

        //--- Properties ---

        /// <summary>
        /// Does the message have a document payload?
        /// </summary>
        public bool HasDocument { get { return _message.HasDocument; } }

        //--- Methods ---

        /// <summary>
        /// Generate the message Xml envelope.
        /// </summary>
        /// <returns>Xml envelope instance.</returns>
        public XDoc GetEventEnvelope() {
            return new XDoc("event").Attr("id", Id);
        }

        /// <summary>
        /// Determine whether an event has visited a specific uri on its transit.
        /// </summary>
        /// <param name="via">Uri to check</param>
        /// <returns><see langword="True"/> if the Uri was part of the message's transit path.</returns>
        public bool HasVisited(XUri via) {
            return Array.Find(Via, delegate(XUri uri) { return uri == via; }) != null;
        }

        /// <summary>
        /// Get the message payload as a document.
        /// </summary>
        /// <returns>Xml document.</returns>
        public XDoc AsDocument() {
            return _message.ToDocument();
        }

        /// <summary>
        /// Create a new <see cref="DreamMessage"/> instance containing payload and envelope.
        /// </summary>
        /// <returns></returns>
        public DreamMessage AsMessage() {
            DreamHeaders headers = new DreamHeaders();
            headers.DreamEventId = Id;
            if(Resource != null) {
                headers.DreamEventResource = Resource.ToString();
            }
            headers.DreamEventChannel = Channel.ToString();
            string[] origin = new string[Origins.Length];
            for(int i = 0; i < Origins.Length; i++) {
                origin[i] = Origins[i].ToString();
            }
            headers.DreamEventOrigin = origin;
            string[] recipients = new string[Recipients.Length];
            for(int i = 0; i < Recipients.Length; i++) {
                recipients[i] = Recipients[i].ToString();
            }
            headers.DreamEventRecipients = recipients;
            string[] via = new string[Via.Length];
            for(int i = 0; i < Via.Length; i++) {
                via[i] = Via[i].ToString();
            }
            headers.DreamEventVia = via;

            // if our message has a document as content, we can skip the whole stream business
            if(_message.HasDocument) {
                return new DreamMessage(DreamStatus.Ok, headers, _message.ToDocument());
            }

            // AsBytes will create the byte array only once, so we can call this multiple times without penalty
            byte[] bytes = _message.ToBytes();
            return DreamMessage.Ok(_message.ContentType, bytes);
        }

        /// <summary>
        /// Add a uri to the message's transit path.
        /// </summary>
        /// <param name="via"></param>
        /// <returns></returns>
        public DispatcherEvent WithVia(XUri via) {
            XUri[] newVia = new XUri[Via.Length + 1];
            Array.Copy(Via, newVia, Via.Length);
            newVia[Via.Length] = via;
            return new DispatcherEvent(this, newVia, this.Recipients);
        }

        /// <summary>
        /// Add recipients to the message.
        /// </summary>
        /// <param name="replace">If <see langword="True"/>, the provide list replaces any existing recipients.</param>
        /// <param name="recipients">Zero or more recipients.</param>
        /// <returns>New event instance.</returns>
        public DispatcherEvent WithRecipient(bool replace, params DispatcherRecipient[] recipients) {
            if(replace) {
                return new DispatcherEvent(this, Via, recipients);
            }
            DispatcherRecipient[] newRecipients = new DispatcherRecipient[Recipients.Length + recipients.Length];
            Array.Copy(Recipients, newRecipients, Recipients.Length);
            Array.Copy(recipients, 0, newRecipients, Recipients.Length, recipients.Length);
            return new DispatcherEvent(this, Via, newRecipients);
        }
    }
}
