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

using System.Data;
using MindTouch.Data;
using MindTouch.Dream.Test.Data;

using NUnit.Framework;

namespace MindTouch.Dream.Test {
    [TestFixture]
    public class MockDataCatalogTests {
        private MockDataCatalog _mockCatalog;
        private IDataCatalog _catalog;

        [SetUp]
        public void Setup() {
            _catalog = _mockCatalog = new MockDataCatalog();
        }

        [TearDown]
        public void Teardown() {
            _mockCatalog.Verify();
        }

        [Test]
        public void Can_execute_query_without_return() {
            _mockCatalog.ExpectNewQuery("blah", 1).Execute();
            _catalog.NewQuery("blah").Execute();
            Assert.IsTrue(_mockCatalog.Verify(), _mockCatalog.VerificationFailure);
        }

        [Test]
        public void Whitespace_in_query_is_not_signifcant() {
            _mockCatalog.ExpectNewQuery(@"
blah    blah  
 blah
 ", 1).Execute();
            _catalog.NewQuery("blah blah blah").Execute();
            Assert.IsTrue(_mockCatalog.Verify(), _mockCatalog.VerificationFailure);
        }

        [Test]
        public void Can_read_string() {
            _mockCatalog.ExpectNewQuery("foo", 1).With("key1", "val1").WithExpectedReturnValue("bar").Read();
            Assert.AreEqual("bar", _catalog.NewQuery("foo").With("key1", "val1").Read());
        }

        [Test]
        public void Can_read_string_exactly_expected_times() {
            _mockCatalog.ExpectNewQuery("foo", 2).With("key1", "val1").WithExpectedReturnValue("bar").Read();
            Assert.AreEqual("bar", _catalog.NewQuery("foo").With("key1", "val1").Read());
            Assert.IsFalse(_mockCatalog.Verify());
            Assert.AreEqual("bar", _catalog.NewQuery("foo").With("key1", "val1").Read());
            Assert.IsTrue(_mockCatalog.Verify());
            try {
                _catalog.NewQuery("foo").With("key1", "val1").Read();
            } catch(AssertionException) {
                return;
            }
            Assert.Fail("query should have thrown");
        }

        [Test]
        public void Can_read_different_strings_for_different_args() {
            _mockCatalog.ExpectNewQuery("foo", 1).With("key1", "barVal").WithExpectedReturnValue("bar").Read();
            _mockCatalog.ExpectNewQuery("foo", 1).With("key1", "bazVal").WithExpectedReturnValue("baz").Read();
            Assert.AreEqual("bar", _catalog.NewQuery("foo").With("key1", "barVal").Read());
            Assert.AreEqual("baz", _catalog.NewQuery("foo").With("key1", "bazVal").Read());
        }

        [Test]
        public void Can_create_MockDataReader_with_no_data() {
            var reader = new MockDataCatalog.MockDataReader(new[] { "foo", "bar" }, new object[0][]) as IDataReader;
            var i = 0;
            while(reader.Read()) {
                i++;
            }
            Assert.AreEqual(0, i);
        }

        [Test]
        public void Can_get_read_from_MockDataReader() {
            var reader = new MockDataCatalog.MockDataReader(
                new[] { "1", "2", "3" },
                new[] {
                          new object[] {"1.1", "1.2", "1.3"},
                          new object[] {"2.1", "2.2", "2.3"},
                          new object[] {"3.1", "3.2", "3.3"},
                      }) as IDataReader;
            var i = 0;
            while(reader.Read()) {
                i++;
                for(var j = 0; j < reader.FieldCount; j++) {
                    var field = reader.GetName(j);
                    Assert.AreEqual(string.Format("{0}.{1}", i, field), reader[field].ToString());
                    Assert.AreEqual(string.Format("{0}.{1}", i, j + 1), reader[j].ToString());
                }
            }
            Assert.AreEqual(3, i);
        }

        [Test]
        public void Can_read_from_DataReader() {
            _mockCatalog.ExpectNewQuery("foo", 1).Execute(
                new MockDataCatalog.MockDataReader(
                    new[] { "foo", "bar" },
                    new[] { 
                        new object[]{"1.1","1.2","1.3"},
                        new object[]{"2.1","2.2","2.3"},
                        new object[]{"3.1","3.2","3.3"},
                    }));
        }
    }
}
