/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
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

using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class XUriMapTests {

        [Test]
        public void Exact_Match() {
            XUriMap<string> map = new XUriMap<string>();
            string key = "foo/bar";
            map[new XUri("test://foo/bar")] = key;
            string match;
            int similarity;
            XUri test = new XUri("test://foo/bar");
            map.TryGetValue(test, out match, out similarity);
            Assert.AreEqual(key, match);
            Assert.AreEqual(test.MaxSimilarity, similarity);
        }

        [Test]
        public void Partial_Match() {
            XUriMap<string> map = new XUriMap<string>();
            string key = "foo/bar";
            map[new XUri("test://foo/bar")] = key;
            string match;
            int similarity;
            XUri test = new XUri("test://foo/bar/baz");
            map.TryGetValue(test, out match, out similarity);
            Assert.AreEqual(key, match);
            Assert.AreEqual(test.MaxSimilarity - 1, similarity);
        }

        [Test]
        public void Schemes_should_be_differentiated() {
            XUriMap<string> map = new XUriMap<string>();
            map[new XUri("test1://foo/bar/boo")] = "t1/foo/bar";
            map[new XUri("test2://foo/bar/baz")] = "t2/foo/bar";
            string match;
            int similarity;
            map.TryGetValue(new XUri("test2://foo/bar/boo"), out match, out similarity);
            Assert.IsNull(match,match);
        }

        [Test]
        public void Get_best_match() {
            XUriMap<string> map = new XUriMap<string>();
            string foobarbaz = "foobarbaz";
            map[new XUri("test://foo")] = "foo";
            map[new XUri("test://foo/bar")] = "foobar";
            map[new XUri("test://foo/bar/baz")] = foobarbaz;
            map[new XUri("test://foo/baz")] = "foobaz";
            map[new XUri("test://foo/bar/baz/beep/sdfdsfsd")] = "dfdsfdsfsdf";
            map[new XUri("test://foo/baz/beep/bar")] = "foobazbeepbar";
            string match;
            int similarity;
            XUri test = new XUri("test://foo/bar/baz/beep");
            map.TryGetValue(test, out match, out similarity);
            Assert.AreEqual(foobarbaz, match);
            Assert.AreEqual(test.MaxSimilarity - 1, similarity);
        }

        [Test]
        public void Get_all_matches() {
            XUriMap<XUri> map = new XUriMap<XUri>();
            List<XUri> expected = new List<XUri>();
            expected.Add(Add(map, new XUri("channel:///deki/pages/tweak/wobble")));
            expected.Add(Add(map, new XUri("channel:///deki/pages/tweak/wonk")));
            expected.Add(Add(map, new XUri("channel:///deki/pages/mod")));
            expected.Add(Add(map, new XUri("channel:///deki/pages/create")));
            expected.Add(Add(map, new XUri("channel:///deki/pages/delete")));
            Add(map, new XUri("channel:///deki/users/create"));
            Add(map, new XUri("channel:///dream/pages"));
            IEnumerable<XUri> matches = map.GetValues(new XUri("channel:///deki/pages/*"));
            List<XUri> matchList = new List<XUri>(matches);
            Assert.AreEqual(expected.Count, matchList.Count);
            foreach(XUri match in matchList) {
                Assert.Contains(match,expected);
            }
        }

        [Test]
        public void Get_exact_match_only_from_GetValues() {
            XUriMap<XUri> map = new XUriMap<XUri>();
            List<XUri> expected = new List<XUri>();
            Add(map, new XUri("channel:///deki/pages"));
            Add(map, new XUri("channel:///deki/pages/mod"));
            Add(map, new XUri("channel:///deki/pages/create"));
            Add(map, new XUri("channel:///deki/pages/delete"));
            IEnumerable<XUri> matches = map.GetValues(new XUri("channel:///deki/pages/mod"));
            List<XUri> matchList = new List<XUri>(matches);
            Assert.AreEqual(1, matchList.Count);
        }

        [Test]
        public void Fail_to_get_exact_match_only_from_GetValues() {
            XUriMap<XUri> map = new XUriMap<XUri>();
            List<XUri> expected = new List<XUri>();
            Add(map, new XUri("channel:///deki/pages"));
            Add(map, new XUri("channel:///deki/pages/mod"));
            IEnumerable<XUri> matches = map.GetValues(new XUri("channel:///deki/pages/mod/foo"));
            List<XUri> matchList = new List<XUri>(matches);
            Assert.AreEqual(0, matchList.Count);
        }

        private XUri Add(XUriMap<XUri> map, XUri uri) {
            map.Add(uri, uri);
            return uri;
        }
    }
}
