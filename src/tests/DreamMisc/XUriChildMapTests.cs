/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
using MindTouch.Dream;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class XUriChildMapTests {

        [Test]
        public void GetAllMatches() {
            XUriChildMap<string> map = new XUriChildMap<string>();
            map.Add(new XUri("channel:///*"), "all");
            map.Add(new XUri("channel:///deki/*"), "deki");
            map.Add(new XUri("channel:///deki/pages/*"), "pages");
            map.Add(new XUri("channel:///deki/pages/create"), "create");
            map.Add(new XUri("channel:///deki/pages/mod"), "mod");
            map.Add(new XUri("channel:///deki/pages/delete"), "delete");
            map.Add(new XUri("channel:///deki/users/*"), "users");
            map.Add(new XUri("channel:///deki/pages/*"), "pagesandusers");
            map.Add(new XUri("channel:///deki/users/*"), "pagesandusers");
            map.Add(new XUri("channel:///deki/pages/mod/*"), "mod2");

            string[] expected = new string[] { "all", "deki", "pages", "create", "pagesandusers" };
            IEnumerable<string> matches = map.GetMatches(new XUri("channel:///deki/pages/create"));
            CheckExpectations(expected, matches);

            expected = new string[] { "all", "deki", "pages", "mod", "mod2", "pagesandusers" };
            matches = map.GetMatches(new XUri("channel:///deki/pages/mod"));
            CheckExpectations(expected, matches);

            expected = new string[] { "all", "deki", "users", "pagesandusers" };
            matches = map.GetMatches(new XUri("channel:///deki/users/create"));
            CheckExpectations(expected, matches);

            expected = new string[] { "all", "deki", "pages", "pagesandusers" };
            matches = map.GetMatches(new XUri("channel:///deki/pages"));
            CheckExpectations(expected, matches);
        }

        [Test]
        public void GetFilteredMatches() {
            XUriChildMap<string> map = new XUriChildMap<string>();
            map.Add(new XUri("channel:///*"), "all");
            map.Add(new XUri("channel:///deki/*"), "deki");
            map.Add(new XUri("channel:///deki/pages/*"), "pages");
            map.Add(new XUri("channel:///deki/pages/create"), "create");
            map.Add(new XUri("channel:///deki/pages/mod"), "mod");
            map.Add(new XUri("channel:///deki/pages/delete"), "delete");
            map.Add(new XUri("channel:///deki/users/*"), "users");
            map.Add(new XUri("channel:///deki/pages/*"), "pagesandusers");
            map.Add(new XUri("channel:///deki/users/*"), "pagesandusers");
            map.Add(new XUri("channel:///deki/pages/mod/*"), "mod2");
            List<string> filter = new List<string>();
            filter.Add("deki");
            filter.Add("users");
            filter.Add("pagesandusers");

            string[] expected = new string[] { "deki", "pagesandusers" };
            IEnumerable<string> matches = map.GetMatches(new XUri("channel:///deki/pages/create"),filter);
            CheckExpectations(expected, matches);

            expected = new string[] { "deki", "pagesandusers" };
            matches = map.GetMatches(new XUri("channel:///deki/pages/mod"), filter);
            CheckExpectations(expected, matches);

            expected = new string[] { "deki", "users", "pagesandusers" };
            matches = map.GetMatches(new XUri("channel:///deki/users/create"), filter);
            CheckExpectations(expected, matches);

            expected = new string[] { "deki", "pagesandusers" };
            matches = map.GetMatches(new XUri("channel:///deki/pages"), filter);
            CheckExpectations(expected, matches);
        }

        [Test]
        public void GetWildcardHostMatches() {
            XUriChildMap<string> map = new XUriChildMap<string>();
            map.Add(new XUri("channel://*/*"), "all");
            map.Add(new XUri("channel://deki1/deki/*"), "deki1");
            map.Add(new XUri("channel://deki2/deki/*"), "deki2");
            map.Add(new XUri("channel://*/deki/pages/*"), "allpages");

            string[] expected = new string[] { "all", "deki1", "allpages" };
            IEnumerable<string> matches = map.GetMatches(new XUri("channel://deki1/deki/pages/create"));
            CheckExpectations(expected, matches);

            expected = new string[] { "all", "deki1" };
            matches = map.GetMatches(new XUri("channel://deki1/deki/comments/mod"));
            CheckExpectations(expected, matches);

            expected = new string[] { "all", "deki2", };
            matches = map.GetMatches(new XUri("channel://deki2/deki/comments/create"));
            CheckExpectations(expected, matches);

        }

        [Test]
        public void GetMatchesRespectsScheme() {
            XUriChildMap<string> map = new XUriChildMap<string>(false);
            map.Add(new XUri("http://*/y/*"), "http1");
            map.Add(new XUri("https://*/y/*"), "https1");
            map.Add(new XUri("http://x/y/*"), "http2");
            map.Add(new XUri("https://x/y/*"), "https2");
            map.Add(new XUri("http://x/y/z"), "http3");
            map.Add(new XUri("https://x/y/z"), "https3");

            string[] expected = new string[] { "http1", "http2", "http3" };
            IEnumerable<string> matches = map.GetMatches(new XUri("http://x/y/z"));
            CheckExpectations(expected, matches);

            expected = new string[] { "https1", "https2", "https3", };
            matches = map.GetMatches(new XUri("https://x/y/z"));
            CheckExpectations(expected, matches);

            expected = new string[] { "http1", "http2", };
            matches = map.GetMatches(new XUri("http://x/y/y"));
            CheckExpectations(expected, matches);

            expected = new string[] { "https1", "https2" };
            matches = map.GetMatches(new XUri("https://x/y/y"));
            CheckExpectations(expected, matches);

            expected = new string[] { "http1", };
            matches = map.GetMatches(new XUri("http://y/y/z"));
            CheckExpectations(expected, matches);

            expected = new string[] {  "https1" };
            matches = map.GetMatches(new XUri("https://y/y/z"));
            CheckExpectations(expected, matches);

        }

        [Test]
        public void GetMatchesIgnoringScheme() {
            XUriChildMap<string> map = new XUriChildMap<string>(true);
            map.Add(new XUri("http://*/y/*"), "http1");
            map.Add(new XUri("https://*/y/*"), "https1");
            map.Add(new XUri("http://x/y/*"), "http2");
            map.Add(new XUri("https://x/y/*"), "https2");
            map.Add(new XUri("http://x/y/z"), "http3");
            map.Add(new XUri("https://x/y/z"), "https3");

            string[] expected = new string[] { "http1", "https1", "http2", "https2", "http3", "https3", };
            IEnumerable<string> matches = map.GetMatches(new XUri("http://x/y/z"));
            CheckExpectations(expected, matches);

            expected = new string[] { "http1", "https1", "http2", "https2" };
            matches = map.GetMatches(new XUri("https://x/y/y"));
            CheckExpectations(expected, matches);

            expected = new string[] { "http1", "https1" };
            matches = map.GetMatches(new XUri("foo://y/y/z"));
            CheckExpectations(expected, matches);

        }

        private void CheckExpectations(string[] expected, IEnumerable<string> matches) {
            List<string> m = new List<string>(matches);
            Assert.AreEqual(expected.Length, m.Count);
            foreach(string e in expected) {
                Assert.Contains(e, m);
            }
        }
    }

}
