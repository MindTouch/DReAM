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
using log4net;
using MindTouch.Xml;

namespace MindTouch.Dream.Services.PubSub {
    
    /// <summary>
    /// A <see cref="DispatcherEvent"/> recipient.
    /// </summary>
    public class DispatcherRecipient : IComparable<DispatcherRecipient> {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---

        /// <summary>
        /// Canonical uri describing the recipient.
        /// </summary>
        public readonly XUri Uri;

        /// <summary>
        /// Meta data about the recipient.
        /// </summary>
        public readonly XDoc Doc;

        //--- Constructors ---

        /// <summary>
        /// Create a recipient from a recipient document.
        /// </summary>
        /// <param name="recipientDoc">Recipient Xml document.</param>
        public DispatcherRecipient(XDoc recipientDoc) {
            Uri = recipientDoc["uri"].AsUri;
            if(Uri == null) {
                throw new ArgumentException("A recipient must have a uri");
            }
            Doc = recipientDoc;
        }

        /// <summary>
        /// Create a recipient, given its canonical uri.
        /// </summary>
        /// <param name="uri">Recipient uri.</param>
        public DispatcherRecipient(XUri uri) {
            Uri = uri;
            Doc = new XDoc("recipient").Elem("uri", uri);
        }

        //--- Methods ---
        int IComparable<DispatcherRecipient>.CompareTo(DispatcherRecipient other) {
            return ToString().CompareTo(other.ToString());
        }

        /// <summary>
        /// Override of default implementation for comparison purposes.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Override of default implementation for comparison purposes.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            if(obj is DispatcherRecipient) {
                return ToString().Equals(obj.ToString());
            }
            return base.Equals(obj);
        }

        /// <summary>
        /// Override of default implementation for display purposes.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return Uri.ToString();
        }

        /// <summary>
        /// Convert instance to Xml document representation.
        /// </summary>
        /// <returns>New Recipient document.</returns>
        public XDoc AsDocument() {
            return Doc;
        }
    }
}
