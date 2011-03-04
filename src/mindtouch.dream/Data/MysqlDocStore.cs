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
using System.Data;
using System.Linq;

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Data {

    /// <summary>
    /// Provides and implementation of <see cref="IDocStore"/> using Mysql as its persistence engine.
    /// </summary>
    public class MysqlDocStore : IDocStore {

        //--- Types ---
        struct QueryItem {
            public int Id;
            public int Revision;
            public string Doc;
        }

        //--- Class Fields ---
        private readonly IDataCatalog _catalog;
        private readonly IMysqlDocStoreIndexer _indexer;
        private readonly IDictionary<string, string> _namespaceMap = new Dictionary<string, string>();
        private readonly string _idXPath;
        private readonly string _name;

        //--- Constructors ---

        /// <summary>
        /// Create a new storage instance.
        /// </summary>
        /// <param name="catalog">DataCatalog to use for collection storage.</param>
        /// <param name="indexer">Indexing service.</param>
        public MysqlDocStore(IDataCatalog catalog, IMysqlDocStoreIndexer indexer) {
            _catalog = catalog;
            _indexer = indexer;
            _name = _indexer.Name;
            _idXPath = _indexer.Config["id-xpath"].AsText ?? "@id";
            _namespaceMap.Add(new KeyValuePair<string, string>("docstore", "mindtouch.dream.docstore"));
            foreach(XDoc doc in _indexer.Config["namespaces/namespace"]) {
                _namespaceMap.Add(new KeyValuePair<string, string>(doc["@prefix"].AsText, doc["@urn"].AsText));
            }
        }

        //--- Properties ---

        /// <summary>
        /// Provides queryable access to the document storage.
        /// </summary>
        public IQueryable<XDoc> AsQueryable { get { return YieldAll(-1, -1).AsQueryable(); } }

        //--- Methods ---

        /// <summary>
        /// Store a document in the collection.
        /// </summary>
        /// <param name="doc">Document to store.</param>
        /// <param name="force"><see langword="True"/> if the write should proceed even if optimistic locking meta-data indicates the document is older than the document already stored.</param>
        /// <returns><see langword="True"/> if the action completed successfully.</returns>
        public bool Put(XDoc doc, bool force) {
            Map(doc);
            string docid = doc[_idXPath].AsText;
            string revisionClause = string.Empty;
            XDoc revisionAttr = doc["@docstore:revision"];
            if(!revisionAttr.IsEmpty) {
                if(!force) {
                    int? rev = revisionAttr.AsInt;
                    if(rev.HasValue) {
                        int pk = doc["@docstore:id"].AsInt ?? 0;
                        revisionClause = string.Format("AND id = {0} AND revision = {1}", pk, rev.Value);
                    }
                }

                // if we have docstore specific tags, we need to remove them before storage, but don't want to alter the doc
                // that was given to us
                doc = doc.Clone();
                Map(doc);
                doc["@docstore:revision"].Remove();
                doc["@docstore:id"].Remove();
            }
            if(string.IsNullOrEmpty(docid)) {
                throw new ArgumentException(string.Format("Document does not contain a valid value at '{0}'", _idXPath));
            }
            int rowsAffected = 0;
            int id = 0;
            int revision = 0;

            // try update first, check for rows affected
            _catalog.NewQuery(string.Format(@"
UPDATE {0} SET id = (@id := id), doc = ?DOC, revision = (@revision := revision + 1) WHERE doc_id = ?DOCID {1};
SELECT ROW_COUNT(), @id, @revision;", _name, revisionClause))
                    .With("DOCID", docid)
                    .With("DOC", doc.ToString())
                .Execute(delegate(IDataReader reader) {
                while(reader.Read()) {
                    rowsAffected = reader.GetInt32(0);
                    if(rowsAffected == 0) {
                        continue;
                    }

                    // Note (arnec): have to fetch as string and convert to int, because @variables in mysql
                    // are already returned as byte arrays representing strings
                    id = Convert.ToInt32(reader.GetString(1));
                    revision = Convert.ToInt32(reader.GetString(2));
                }
            });
            bool wroteData = (rowsAffected > 0);

            // if there was a revisionClause it's always an update, so we can skip the next block
            if(string.IsNullOrEmpty(revisionClause) && rowsAffected == 0) {

                // no row updated, try insert
                try {
                    id = _catalog.NewQuery(string.Format(@"
INSERT INTO {0} (doc_id,doc) VALUES (?DOCID,?VALUE);
SELECT last_insert_id();", _name))
                             .With("DOCID", docid)
                             .With("VALUE", doc.ToString()).ReadAsInt() ?? 0;
                    revision = 1;
                } catch(Exception e) {

                    // Note: need to do this by reflection magic, because Dream doesn't take DB dependencies at the dll level
                    bool isDuplicate = false;
                    if(StringUtil.EqualsInvariant(e.GetType().ToString(), "MySql.Data.MySqlClient.MySqlException")) {
                        try {
                            int errorNumber = (int)e.GetType().GetProperty("Number").GetValue(e, null);

                            // trap for duplicate key collisions
                            if(errorNumber == 1062) {
                                isDuplicate = true;
                            }
                        } catch { }
                        if(!isDuplicate) {
                            throw;
                        }
                    }
                }
                if(id == 0) {

                    // insert failed, try update once more
                    _catalog.NewQuery(string.Format(@"
UPDATE {0} SET id = (@id := id), doc = ?DOC, revision = (@revision := revision + 1) WHERE doc_id = ?DOCID;
SELECT @id, @revision;", _name))
                        .With("DOCID", docid)
                        .With("DOC", doc.ToString())
                        .Execute(delegate(IDataReader reader) {
                        while(reader.Read()) {

                            // Note (arnec): have to fetch as string and convert to int, because @variables in mysql
                            // are already returned as byte arrays representing strings
                            id = Convert.ToInt32(reader.GetString(0));
                            revision = Convert.ToInt32(reader.GetString(1));
                        }
                    });
                }
                wroteData = true;
            }

            if(wroteData) {
                _indexer.QueueUpdate(id, revision, doc);
            }
            return wroteData;
        }

        /// <summary>
        /// Delete a document from the collection.
        /// </summary>
        /// <param name="docId">Unique identifier of the document.</param>
        /// <returns><see langword="True"/> if the there was a document to be deleted.</returns>
        public bool Delete(string docId) {
            int? id = _catalog.NewQuery(string.Format("SELECT id FROM {0} WHERE doc_id = ?KEY", _name)).With("KEY", docId).ReadAsInt();
            if(!id.HasValue) {
                return false;
            }
            int? affected = _catalog.NewQuery(string.Format("DELETE FROM {0} WHERE id = ?ID; SELECT ROW_COUNT();", _name)).With("ID", id.Value).ReadAsInt();
             if( !affected.HasValue || affected.Value == 0 ) {
                return false;
            }
           _indexer.QueueDelete(id.Value);
            return true;
        }

        /// <summary>
        /// Retrieve a document by its unique identifier.
        /// </summary>
        /// <param name="docId">Unique identifier of the document.</param>
        /// <returns>Document instance.</returns>
        public XDoc Get(string docId) {
            XDoc doc = null;
            _catalog.NewQuery(string.Format("SELECT id, revision, doc FROM {0} WHERE doc_id = ?KEY", _name))
                .With("KEY", docId)
                .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    doc = XDocFactory.From(dr.GetString(2), MimeType.TEXT_XML);
                    Map(doc);
                    int id = dr.GetInt32(0);
                    int revision = dr.GetInt32(1);
                    doc.Attr("docstore:revision", revision);
                    doc.Attr("docstore:id", id);
                }
            });
            return doc;
        }

        /// <summary>
        /// Get a subset of documents from the collection, ordered by insertion order.
        /// </summary>
        /// <param name="offset">Offset into collection.</param>
        /// <param name="limit">Maximum number of documents to retrieve.</param>
        /// <returns>Enumerable of matching documents.</returns>
        public IEnumerable<XDoc> Get(int offset, int limit) {
            return YieldAll(limit, offset);
        }

        /// <summary>
        /// Query the collection based on an indexed key in the document.
        /// </summary>
        /// <param name="keyName">Name of the index key.</param>
        /// <param name="keyValue">Value of the key.</param>
        /// <returns>Enumerable of matching documents.</returns>
        public IEnumerable<XDoc> Get(string keyName, string keyValue) {
            return Get(keyName, keyValue, -1, -1);
        }

        /// <summary>
        /// Query the collection based on an indexed key in the document.
        /// </summary>
        /// <param name="keyName">Name of the index key.</param>
        /// <param name="keyValue">Value of the key.</param>
        /// <param name="offset">Offset into collection.</param>
        /// <param name="limit">Maximum number of documents to retrieve.</param>
        /// <returns>Enumerable of matching documents.</returns>
        public IEnumerable<XDoc> Get(string keyName, string keyValue, int offset, int limit) {
            var info = _indexer.GetIndexInfo(keyName);
            if(info == null) {
                throw new ArgumentException(string.Format("No key '{0}' defined", keyName), "keyName");
            }
            var docs = new List<XDoc>();
            _catalog.NewQuery(string.Format("SELECT id, revision, doc FROM {0} LEFT JOIN {1} ON {0}.id = {1}.ref_id WHERE {1}.idx_value = ?KEY ORDER BY {0}.id{2}",
                    _name, info.Table, GetLimitAndOffsetClause(limit,offset)))
                .With("KEY", keyValue)
                .Execute(delegate(IDataReader reader) {
                while(reader.Read()) {
                    XDoc doc = XDocFactory.From(reader.GetString(2), MimeType.TEXT_XML);
                    Map(doc);
                    foreach(XDoc match in doc[info.XPath]) {
                        if(StringUtil.EqualsInvariant(match.AsText, keyValue)) {
                            int id = reader.GetInt32(0);
                            int revision = reader.GetInt32(1);
                            doc.Attr("docstore:revision", revision);
                            doc.Attr("docstore:id", id);
                            docs.Add(doc);
                            break;
                        }
                    }
                }
            });
            return docs;
        }

        private IEnumerable<XDoc> YieldAll(int limit, int offset) {
            var docResults = new List<QueryItem>();
            _catalog.NewQuery(string.Format("SELECT id, revision, doc FROM {0} ORDER BY {0}.id{1}", _name, GetLimitAndOffsetClause(limit, offset)))
                .Execute(delegate(IDataReader dr) {
                    while(dr.Read()) {
                        var docResult = new QueryItem {
                                                          Id = dr.GetInt32(0),
                                                          Revision = dr.GetInt32(1),
                                                          Doc = dr.GetString(2)
                                                      };
                        docResults.Add(docResult);
                    }
                });
            foreach(var docResult in docResults) {
                XDoc doc = XDocFactory.From(docResult.Doc, MimeType.TEXT_XML);
                Map(doc);
                doc.Attr("docstore:revision", docResult.Revision);
                doc.Attr("docstore:id", docResult.Id);
                yield return doc;
            }
        }

        private string GetLimitAndOffsetClause(int limit, int offset) {
            return (limit >= 0 ? " LIMIT " + limit : "") + (offset >= 0 ? " OFFSET " + offset : "");
        }

        // TODO (arnec): does it make sense to factor this into the indexer or not have the indexer require it?
        private void Map(XDoc doc) {
            if(_namespaceMap == null) {
                return;
            }
            foreach(KeyValuePair<string, string> kvp in _namespaceMap) {
                doc.UsePrefix(kvp.Key, kvp.Value);
            }
        }
    }
}
