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

using System.IO;
using MindTouch.Collections;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.IO {

    /// <summary>
    /// <see cref="XDoc"/> to and from <see cref="Stream"/> serializer
    /// </summary>
    /// <remarks>
    /// Exists primarily for use with <see cref="TransactionalQueue{T}"/>
    /// </remarks>
    public class XDocQueueItemSerializer : IQueueItemSerializer<XDoc> {

        //--- Methods ---

        /// <summary>
        /// Serialize an <see cref="XDoc"/> to a <see cref="Stream"/>
        /// </summary> 
        /// <param name="doc">An <see cref="XDoc"/> document</param>
        /// <returns>A <see cref="Stream"/></returns>
        public Stream ToStream(XDoc doc) {
            var data = new ChunkedMemoryStream();
            doc.WriteTo(data);
            data.Position = 0;
            return data;
        }

        /// <summary>
        /// Create an instance of <see cref="XDoc"/> from a binary <see cref="Stream"/>
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/></param>
        /// <returns>An <see cref="XDoc"/> document</returns>
        public XDoc FromStream(Stream stream) {
            return XDocFactory.From(stream, MimeType.XML);
        }
    }
}