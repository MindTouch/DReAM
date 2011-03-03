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

using MindTouch.Data;
using MindTouch.Dream.Test.Data;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [Ignore("needs completed Catalog mocks")]
    [TestFixture]
    public class MysqlDocStoreManagerTests {
        private MockDataCatalog _catalog;
        private string testStore = "teststore";

        [SetUp]
        public void Setup() {
            _catalog = new MockDataCatalog();
        }

        [TearDown]
        public void Teardown() {
            _catalog.Verify();
        }

        [Test]
        public void Can_create_store_tables() {

            //--- Arrange ---
            // set up DataCatalog expectations
            _catalog.ExpectNewQuery(@"
CREATE TABLE IF NOT EXISTS teststore_store (
    id int primary key auto_increment not null,
    revision int not null default 1,
    doc_id varchar(255) unique not null, 
    doc text not null )", 1).Execute();
            _catalog.ExpectNewQuery(@"
CREATE TABLE IF NOT EXISTS teststore_store_indicies (
  idx_name varchar(255) primary key not null,
  idx_xpath text not null )", 1).Execute();
            _catalog.ExpectNewQuery(@"SELECT idx_name, idx_xpath FROM teststore_store_indicies",1)
                .Execute(new MockDataCatalog.MockDataReader(new[]{"idx_name","idx_xpath"},new object[0][]));
            //--- Act ---
            MysqlDocStoreManager manager = new MysqlDocStoreManager(_catalog, new XDoc("config").Elem("name", testStore).Elem("id-xpath", "@id"));

            //--- Assert ---
            // check datacatalog expectations were met
            Assert.IsTrue(_catalog.Verify(), _catalog.VerificationFailure);
        }

        [Test]
        public void Can_wipe_store_tables() {

            //--- Arrange ---
            // set up DataCatalog expectations

            //--- Act ---
            MysqlDocStoreManager.DropDataStore(_catalog, testStore);

            //--- Assert ---
            // check datacatalog expectations were met
        }

        [Test]
        public void Indicies_are_reflected_in_Indicies_property() {

            //--- Arrange ---
            // set up DataCatalog expectations

            //--- Act ---
            MysqlDocStoreManager manager = new MysqlDocStoreManager(_catalog, new XDoc("config").Elem("name", testStore).Elem("id-xpath", "@id"));
            List<IndexInfo> indicies = new List<IndexInfo>(manager.Indicies);

            //--- Assert ---
            // check datacatalog expectations were met

            // check Indicies return value
            Assert.AreEqual(2, indicies.Count);
            bool foundFoo = false;
            bool foundBar = false;
            foreach(IndexInfo info in indicies) {
                if(info.Name == "foo")
                    foundFoo = true;
                if(info.Name == "bar")
                    foundBar = true;
            }
            Assert.IsTrue(foundBar);
            Assert.IsTrue(foundFoo);
        }

        [Test]
        public void Can_create_indicies() {

            //--- Arrange ---
            // set up DataCatalog expectations

            //--- Act ---
            MysqlDocStoreManager manager = new MysqlDocStoreManager(_catalog, new XDoc("config").Elem("name", testStore).Elem("id-xpath", "@id"));
            manager.AddIndex("foo", "/foo");
            manager.AddIndex("bar", "/bar");

            //--- Assert ---
            // check datacatalog expectations were met
        }

        [Test]
        public void Indicies_are_wiped_on_data_store_drop() {

            //--- Arrange ---
            // set up DataCatalog expectations

            //--- Act ---
            MysqlDocStoreManager.DropDataStore(_catalog, testStore);

            //--- Assert ---
            // check datacatalog expectations were met
        }

        [Test]
        public void Can_remove_indicies() {

            //--- Arrange ---
            // set up DataCatalog expectations

            //--- Act ---
            MysqlDocStoreManager manager = new MysqlDocStoreManager(_catalog, new XDoc("config").Elem("name", testStore).Elem("id-xpath", "@id"));
            manager.RemoveIndex("foo");
            manager.RemoveIndex("bar");
            List<IndexInfo> indicies = new List<IndexInfo>(manager.Indicies);

            //--- Assert ---
            // check datacatalog expectations were met

            // check the Indicies property is empty
            Assert.AreEqual(0, indicies.Count);
        }

        [Test]
        public void Can_get_index_info_by_key_name() {

            //--- Arrange ---
            // set up DataCatalog expectations

            //--- Act ---
            MysqlDocStoreManager manager = new MysqlDocStoreManager(_catalog, new XDoc("config").Elem("name", testStore).Elem("id-xpath", "@id"));
            IndexInfo info = manager.GetIndexInfo("foo");

            //--- Assert ---
            // check datacatalog expectations were met

            // check indexinfo
            Assert.IsNotNull(info);
            Assert.AreEqual("foo", info.Name);
        }

        [Test]
        public void Can_build_index_for_document() {

            //--- Arrange ---
/*
            int id1 = 1;
            XDoc doc1 = new XDoc("x").Attr("id", "a").Elem("foo", "x");
            int id2 = 2;
            XDoc doc2 = new XDoc("x").Attr("id", "b").Elem("foo", "b1").Elem("foo", "b2");
*/

            // set up DataCatalog expectations

            //--- Act ---
            MysqlDocStoreManager manager = new MysqlDocStoreManager(_catalog, new XDoc("config").Elem("name", testStore).Elem("id-xpath", "@id"));
            manager.AddIndex("x", "foo");

            //--- Assert ---
            // verify that addindex called routines to build indicies off existing docs
        }

        [Test]
        public void Can_rebuild_index_for_document() {

            //--- Arrange ---
/*
            int id1 = 1;
            XDoc doc1 = new XDoc("x").Attr("id", "a").Elem("foo", "x");
            int id2 = 2;
            XDoc doc2 = new XDoc("x").Attr("id", "b").Elem("foo", "b1").Elem("foo", "b2");
*/
            // set up DataCatalog expectations

            //--- Act ---
            MysqlDocStoreManager manager = new MysqlDocStoreManager(_catalog, new XDoc("config").Elem("name", testStore).Elem("id-xpath", "@id"));
            manager.RebuildIndex("x");

            //--- Assert ---
            // verify that rebuild called routines to build indicies off existing docs
        }

        [Test]
        public void Can_delete_indexed_items() {

            //--- Arrange ---
            int id1 = 1;
            XDoc doc1 = new XDoc("x").Attr("id", "a").Elem("foo", "x");
            int id2 = 2;
            XDoc doc2 = new XDoc("x").Attr("id", "b").Elem("foo", "b1").Elem("foo", "b2");

            // set up DataCatalog expectations

            //--- Act ---
            MysqlDocStoreManager manager = new MysqlDocStoreManager(_catalog, new XDoc("config").Elem("name", testStore).Elem("id-xpath", "@id"));
            manager.QueueDelete(id1);
            manager.QueueDelete(id2);


            //--- Assert ---
            // verify that queued deletes remove index values
        }

        [Test]
        public void Can_update_items() {

            //--- Arrange ---
            // set up DataCatalog expectations

            //--- Act ---
            MysqlDocStoreManager manager = new MysqlDocStoreManager(_catalog, new XDoc("config").Elem("name", testStore).Elem("id-xpath", "@id"));
            manager.QueueUpdate(1, 2, new XDoc("x").Elem("foo", "a"));

            //--- Assert ---
            // verify that queued update updates index
        }
    }
}
