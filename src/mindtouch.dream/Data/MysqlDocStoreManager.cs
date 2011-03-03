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

using MindTouch.Collections;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Data {

    /// <summary>
    /// Index information entity.
    /// </summary>
    public class IndexInfo {

        /// <summary>
        /// Index name.
        /// </summary>
        public string Name;

        /// <summary>
        /// Database table name.
        /// </summary>
        public string Table;

        /// <summary>
        /// Indexed XPath expression.
        /// </summary>
        public string XPath;
    }

    /// <summary>
    /// Provides an indexing service for use by a <see cref="MysqlDocStore"/> instances.
    /// </summary>
    public interface IMysqlDocStoreIndexer {

        //--- Properties ---

        /// <summary>
        /// Get the collection configuration.
        /// </summary>
        XDoc Config { get; }

        /// <summary>
        /// Get the collection name.
        /// </summary>
        string Name { get; }

        //--- Methods ---

        /// <summary>
        /// Queue a document for indexing.
        /// </summary>
        /// <param name="id">Primary key of document.</param>
        /// <param name="revision">Revision of document.</param>
        /// <param name="doc">Document to be indexed.</param>
        void QueueUpdate(int id, int revision, XDoc doc);

        /// <summary>
        /// Queue a document for removal from all indicies.
        /// </summary>
        /// <param name="id">Primary key of document.</param>
        void QueueDelete(int id);

        /// <summary>
        /// Get the index information for a key.
        /// </summary>
        /// <param name="keyName">Name of index key.</param>
        /// <returns></returns>
        IndexInfo GetIndexInfo(string keyName);
    }

    /// <summary>
    /// Provides an implemenation of <see cref="IMysqlDocStoreIndexer"/>
    /// </summary>
    public class MysqlDocStoreManager : IMysqlDocStoreIndexer {

        //--- Types ---
        private class WorkItem {
            public readonly IndexInfo Index;
            public readonly int Id;
            public readonly int Revision;
            public readonly XDoc Doc;
            public WorkItem(IndexInfo index, int id, int revision, XDoc doc) {
                Index = index;
                Id = id;
                Revision = revision;
                Doc = doc;
            }
        }

        //--- Class Fields ---
        private readonly IDataCatalog _catalog;
        private readonly XDoc _config;
        private readonly string _name;
        private readonly string _indexLookupTable;
        private readonly IDictionary<string, string> _namespaceMap = new Dictionary<string, string>();
        private readonly ProcessingQueue<WorkItem> _processingQueue;
        private Dictionary<string, IndexInfo> _indicies = new Dictionary<string, IndexInfo>();

        //--- Constructors ---

        /// <summary>
        /// Create a new storage manager.
        /// </summary>
        /// <param name="catalog">Database catalog to use.</param>
        /// <param name="config">Collection and index configuration.</param>
        public MysqlDocStoreManager(IDataCatalog catalog, XDoc config) {
            _catalog = catalog;
            _config = config;
            _name = "docstore_" + _config["name"].AsText;
            _indexLookupTable = _name + "_indicies";
            if(string.IsNullOrEmpty(_name)) {
                throw new ArgumentException("Missing name for store table");
            }
            if(_catalog == null) {
                throw new ArgumentException("Missing DataCatalog");
            }
            _namespaceMap.Add(new KeyValuePair<string, string>("docstore", "mindtouch.dream.docstore"));
            foreach(XDoc doc in _config["namespaces/namespace"]) {
                _namespaceMap.Add(new KeyValuePair<string, string>(doc["@prefix"].AsText, doc["@urn"].AsText));
            }
            _processingQueue = new ProcessingQueue<WorkItem>(Update, 5);

            // create storage & index lookup tables if required
            _catalog.NewQuery(string.Format(@"
CREATE TABLE IF NOT EXISTS {0} (
    id int primary key auto_increment not null,
    revision int not null default 1,
    doc_id varchar(255) unique not null, 
    doc text not null )", _name))
                .Execute();
            _catalog.NewQuery(string.Format(@"
CREATE TABLE IF NOT EXISTS {0} (
  idx_name varchar(255) primary key not null,
  idx_xpath text not null )", _indexLookupTable))
                .Execute();

            RefreshIndicies();
        }

        //--- Properties ---

        /// <summary>
        /// Get the collection configuration.
        /// </summary>
        public XDoc Config { get { return _config; } }

        /// <summary>
        /// Get the collection name.
        /// </summary>
        public string Name { get { return _name; } }

        /// <summary>
        /// Get all Indicies defined for this indexer.
        /// </summary>
        public IEnumerable<IndexInfo> Indicies {
            get {
                RefreshIndicies();
                List<IndexInfo> indicies = new List<IndexInfo>();
                foreach(IndexInfo info in _indicies.Values) {
                    indicies.Add(info);
                }
                return indicies;
            }
        }

        //--- Methods ---

        /// <summary>
        /// Manually add an index.
        /// </summary>
        /// <param name="keyName">Name of the index.</param>
        /// <param name="xpath">XPath expression to index.</param>
        public void AddIndex(string keyName, string xpath) {

            // make sure index doesn't already exist
            if(_indicies.ContainsKey(keyName)) {
                RefreshIndicies();
                if(_indicies.ContainsKey(keyName)) {
                    return;
                }
            }

            // TODO: need to sanity check keyName
            IndexInfo info = new IndexInfo();
            info.Name = keyName;
            info.Table = _name + "_idx_" + keyName;
            info.XPath = xpath;
            try {
                _catalog.NewQuery(string.Format("INSERT INTO {0} VALUES (?KEY, ?XPATH)", _indexLookupTable))
                    .With("KEY", keyName)
                    .With("XPATH", xpath)
                    .Execute();
                _catalog.NewQuery(string.Format(@"
CREATE TABLE {0} (
  ref_id int not null,
  ref_revision int not null,
  idx_value varchar(255),
  key(ref_id),
  key(idx_value(40)) );", info.Table))
                .Execute();
            } catch(Exception e) {

                // Note: need to do this by reflection magic, because Dream doesn't take DB dependencies at the
                // dll level
                if(StringUtil.EqualsInvariant(e.GetType().ToString(), "MySql.Data.MySqlClient.MySqlException")) {
                    try {
                        int errorNumber = (int)e.GetType().GetProperty("Number").GetValue(e, null);

                        // trap for duplicate key or existing table collisions
                        if(errorNumber == 1062 || errorNumber == 1050) {
                            return;
                        }
                    } catch { }
                }
                throw;
            }
            _indicies[keyName] = info;
            BuildIndex(info);
        }

        /// <summary>
        /// Modify an existing index.
        /// </summary>
        /// <param name="keyName">Name of existing index.</param>
        /// <param name="xpath">New XPath expression.</param>
        public void ChangeIndex(string keyName, string xpath) {

            // make sure index exists
            IndexInfo info = GetIndexInfo(keyName);
            if(info == null) {
                AddIndex(keyName, xpath);
                return;
            }
            _catalog.NewQuery(string.Format("UPDATE {0} SET idx_xpath = ?XPATH where idx_name = ?KEY", _indexLookupTable))
                .With("KEY", keyName)
                .With("XPATH", xpath)
                .Execute();
            RefreshIndicies();
            RebuildIndex(keyName);
        }

        /// <summary>
        /// Drop an an index.
        /// </summary>
        /// <param name="keyName">Name of the index.</param>
        public void RemoveIndex(string keyName) {

            // make sure index exists
            IndexInfo info = GetIndexInfo(keyName);
            if(info == null) {
                return;
            }
            _catalog.NewQuery(string.Format("DELETE FROM {0} WHERE idx_name = ?KEY; DROP TABLE {1};", _indexLookupTable, info.Table))
                .With("KEY", info.Name)
                .Execute();
        }

        /// <summary>
        /// Rebuild all values in an index.
        /// </summary>
        /// <param name="keyName">Name of the index.</param>
        public void RebuildIndex(string keyName) {

            // make sure index exists
            IndexInfo info = GetIndexInfo(keyName);
            if(info == null) {
                throw new ArgumentException(string.Format("No index exists for key '{0}'", keyName));
            }
            _catalog.NewQuery(string.Format("TRUNCATE TABLE {0}", info.Table)).Execute();
            BuildIndex(info);
        }

        /// <summary>
        /// Queue a document for indexing.
        /// </summary>
        /// <param name="id">Primary key of document.</param>
        /// <param name="revision">Revision of document.</param>
        /// <param name="doc">Document to be indexed.</param>
        public void QueueUpdate(int id, int revision, XDoc doc) {
            Map(doc);
            foreach(IndexInfo index in Indicies) {

                // TODO (arnec): what to do when enqueue fails...
                _processingQueue.TryEnqueue(new WorkItem(index, id, revision, doc));
            }
        }

        /// <summary>
        /// Queue a document for removal from all indicies.
        /// </summary>
        /// <param name="id">Primary key of document.</param>
        public void QueueDelete(int id) {
            foreach(IndexInfo index in Indicies) {

                // TODO (arnec): what to do when enqueue fails...
                _processingQueue.TryEnqueue(new WorkItem(index, id, 0, null));
            }
        }

        /// <summary>
        /// Get the index information for a key.
        /// </summary>
        /// <param name="keyName">Name of index key.</param>
        /// <returns></returns>
        public IndexInfo GetIndexInfo(string keyName) {
            IndexInfo info;
            if(!_indicies.TryGetValue(keyName, out info)) {
                RefreshIndicies();
                if(!_indicies.TryGetValue(keyName, out info)) {
                    return null;
                }
            }
            return info;
        }

        /// <summary>
        /// Drop the entire data store.
        /// </summary>
        /// <param name="catalog"></param>
        /// <param name="name"></param>
        public static void DropDataStore(IDataCatalog catalog, string name) {
            string tables = string.Empty;
            catalog.NewQuery("SHOW TABLES LIKE ?PREFIX")
                .With("PREFIX", name + "_store%")
                .Execute(delegate(IDataReader reader) {
                while(reader.Read()) {
                    if(tables != string.Empty) {
                        tables += ", ";
                    }
                    tables += reader.GetString(0);
                }
            });
            if(tables != string.Empty) {
                catalog.NewQuery(string.Format("DROP TABLE IF EXISTS {0}", tables)).Execute();
            }
        }

        private void RefreshIndicies() {
            lock(_indicies) {
                Dictionary<string, IndexInfo> indicies = new Dictionary<string, IndexInfo>();
                _catalog.NewQuery(string.Format(@"SELECT idx_name, idx_xpath FROM {0}", _indexLookupTable))
                    .Execute(delegate(IDataReader reader) {
                    while(reader.Read()) {
                        IndexInfo index = new IndexInfo();
                        index.Name = reader.GetString(0);
                        index.Table = _name + "_idx_" + index.Name;
                        index.XPath = reader.GetString(1);
                        indicies.Add(index.Name, index);
                    }
                });
                _indicies = indicies;
            }
        }

        private void BuildIndex(IndexInfo info) {
            _catalog.NewQuery(string.Format("SELECT id, revision, doc FROM {0}", _name)).Execute(delegate(IDataReader reader) {
                while(reader.Read()) {
                    int id = reader.GetInt32(0);
                    int revision = reader.GetInt32(1);
                    XDoc doc = XDocFactory.From(reader.GetString(2), MimeType.TEXT_XML);
                    QueueSingleIndexUpdate(info, id, revision, doc);
                }
            });
        }

        private void QueueSingleIndexUpdate(IndexInfo index, int id, int revision, XDoc doc) {
            Map(doc);

            // TODO (arnec): what to do when enqueue fails...
            _processingQueue.TryEnqueue(new WorkItem(index, id, revision, doc));
        }

        private void Update(WorkItem workItem) {
            if(workItem.Doc == null) {

                // delete
                _catalog.NewQuery(string.Format("DELETE FROM {0} WHERE ref_id = ?REFID", workItem.Index.Table))
                     .With("REFID", workItem.Id)
                     .Execute();
            } else {

                // index update
                foreach(XDoc x in workItem.Doc[workItem.Index.XPath]) {
                    string value = x.AsText;
                    if(string.IsNullOrEmpty(value)) {
                        continue;
                    }
                    _catalog.NewQuery(string.Format("INSERT INTO {0} VALUES (?REFID,?REVISION, ?VALUE)", workItem.Index.Table))
                          .With("REFID", workItem.Id)
                          .With("REVISION", workItem.Revision)
                          .With("VALUE", value)
                          .Execute();
                }

                // remove old entries
                _catalog.NewQuery(string.Format("DELETE FROM {0} WHERE ref_id = ?REFID AND ref_revision < ?REVISION", workItem.Index.Table))
                     .With("REFID", workItem.Id)
                     .With("REVISION", workItem.Revision)
                     .Execute();
            }
        }

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
