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

using System.Collections.Generic;
using System.Linq;

using MindTouch.Xml;

namespace MindTouch.Data {

    /// <summary>
    /// Provides access to a persistently stored collection of documents.
    /// </summary>
    public interface IDocStore {

        //--- Properties ---

        /// <summary>
        /// Provides queryable access to the document storage.
        /// </summary>
        IQueryable<XDoc> AsQueryable { get; }

        //--- Methods ---

        /// <summary>
        /// Store a document in the collection.
        /// </summary>
        /// <param name="doc">Document to store.</param>
        /// <param name="force"><see langword="True"/> if the write should proceed even if optimistic locking meta-data indicates the document is older than the document already stored.</param>
        /// <returns><see langword="True"/> if the action completed successfully.</returns>
        bool Put(XDoc doc, bool force);

        /// <summary>
        /// Delete a document from the collection.
        /// </summary>
        /// <param name="docId">Unique identifier of the document.</param>
        /// <returns><see langword="True"/> if the there was a document to be deleted.</returns>
        bool Delete(string docId);

        /// <summary>
        /// Retrieve a document by its unique identifier.
        /// </summary>
        /// <param name="docId">Unique identifier of the document.</param>
        /// <returns>Document instance.</returns>
        XDoc Get(string docId);

        /// <summary>
        /// Get a subset of documents from the collection, ordered by insertion order.
        /// </summary>
        /// <param name="offset">Offset into collection.</param>
        /// <param name="limit">Maximum number of documents to retrieve.</param>
        /// <returns>Enumerable of matching documents.</returns>
        IEnumerable<XDoc> Get(int offset, int limit);

        /// <summary>
        /// Query the collection based on an indexed key in the document.
        /// </summary>
        /// <param name="keyName">Name of the index key.</param>
        /// <param name="keyValue">Value of the key.</param>
        /// <returns>Enumerable of matching documents.</returns>
        IEnumerable<XDoc> Get(string keyName, string keyValue);

        /// <summary>
        /// Query the collection based on an indexed key in the document.
        /// </summary>
        /// <param name="keyName">Name of the index key.</param>
        /// <param name="keyValue">Value of the key.</param>
        /// <param name="offset">Offset into collection.</param>
        /// <param name="limit">Maximum number of documents to retrieve.</param>
        /// <returns>Enumerable of matching documents.</returns>
        IEnumerable<XDoc> Get(string keyName, string keyValue, int offset, int limit);
    }
}
