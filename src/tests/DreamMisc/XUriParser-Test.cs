/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2013 MindTouch, Inc.
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
using System.Diagnostics;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class XUriParserTest {

        // MISSING TESTS FOR:
        // * user name with encoding
        // * password with encoding
        // * test trailing double-slash: //
        // * fragment with encoding
        // * query key with encoding
        // * query value with encoding

        //--- Class Methods ---
        private static XUri TryParse(string text) {
            string scheme;
            string user;
            string password;
            string hostname;
            int port;
            bool usesDefaultPort;
            bool trailingSlash;
            string[] segements;
            string query;
            string fragment;
            if(!XUriParser.TryParse(text, out scheme, out user, out password, out hostname, out port, out usesDefaultPort, out segements, out trailingSlash, out query, out fragment)) {
                Assert.Fail("failed to parse uri: {0}", text);
            }
            var result = new XUri(scheme, user, password, hostname, port, segements, trailingSlash, null, fragment);
            if(query != null) {
                result = result.WithQuery(query);
            }
            return result;
        }

        private void AssertParse(string text, string scheme = null, string user = null, string password = null, string hostname = null, int? port = null, bool? usesDefaultPort = null, string[] segments = null, bool? trailingSlash = null, KeyValuePair<string, string>[] @params = null, string fragment = null) {

            // setup
            Action<XUri, string> assert = (uri, suffix) => {
                Assert.AreEqual(scheme, uri.Scheme, string.Format("scheme ({0})", suffix));
                Assert.AreEqual(hostname, uri.Host, string.Format("hostname ({0})", suffix));
                Assert.AreEqual(port, uri.Port, string.Format("port ({0})", suffix));
                Assert.AreEqual(usesDefaultPort, uri.UsesDefaultPort, string.Format("usesDefaultPort ({0})", suffix));
                Assert.AreEqual(user, uri.User, string.Format("user ({0})", suffix));
                Assert.AreEqual(password, uri.Password, string.Format("password ({0})", suffix));
                Assert.AreEqual(segments, uri.Segments, string.Format("segments ({0})", suffix));
                Assert.AreEqual(trailingSlash, uri.TrailingSlash, string.Format("trailingSlash ({0})", suffix));
                Assert.AreEqual(@params, uri.Params, string.Format("query ({0})", suffix));
                Assert.AreEqual(fragment, uri.Fragment, string.Format("fragment ({0})", suffix));
                Assert.AreEqual(text, uri.ToString(), string.Format("ToString() ({0})", suffix));
            };

            // setup
            var uriOriginal = XUri.TryParse(text);
            var uriNew = TryParse(text);

            // test
            assert(uriOriginal, "original");
            assert(uriNew, "new");
        }

        //--- Methods ---

        [Test]
        public void TestUriConstructor1() {
            const string original = "http://domain.org/";
            AssertParse(original, 
                scheme: "http",
                hostname: "domain.org",
                port: 80,
                usesDefaultPort: true,
                segments: new string[0], 
                trailingSlash: true
            );
        }

        [Test]
        public void TestUriConstructor2() {
            const string original = "http://domain.org:81";
            AssertParse(original,
                scheme: "http",
                hostname: "domain.org",
                port: 81,
                usesDefaultPort: false,
                segments: new string[0],
                trailingSlash: false
            );
        }

        [Test]
        public void TestUriConstructor3() {
            const string original = "http://[2001:0db8:85a3:08d3:1319:8a2e:0370:7344]/";
            AssertParse(original,
                scheme: "http",
                hostname: "[2001:0db8:85a3:08d3:1319:8a2e:0370:7344]",
                port: 80,
                usesDefaultPort: true,
                segments: new string[0],
                trailingSlash: true
            );
        }

        [Test]
        public void TestUriConstructor4() {
            const string original = "http://[2001:0db8:85a3:08d3:1319:8a2e:0370:7344]:81/";
            AssertParse(original,
                scheme: "http",
                hostname: "[2001:0db8:85a3:08d3:1319:8a2e:0370:7344]",
                port: 81,
                usesDefaultPort: false,
                segments: new string[0],
                trailingSlash: true
            );
        }

        [Test]
        public void TestUriConstructor5() {
            const string original = "http://user:password@domain.org";
            AssertParse(original,
                scheme: "http",
                user: "user",
                password: "password",
                hostname: "domain.org",
                port: 80,
                usesDefaultPort: true,
                segments: new string[0],
                trailingSlash: false
            );
        }

        [Test]
        public void TestUriConstructor6() {
            const string original = "http://user:password@domain.org:81/";
            AssertParse(original,
                scheme: "http",
                user: "user",
                password: "password",
                hostname: "domain.org",
                port: 81,
                usesDefaultPort: false,
                segments: new string[0],
                trailingSlash: true
            );
        }

        [Test]
        public void TestUriConstructor7() {
            const string original = "http://user:password@domain.org:81/path";
            AssertParse(original,
                scheme: "http",
                user: "user",
                password: "password",
                hostname: "domain.org",
                port: 81,
                usesDefaultPort: false,
                segments: new[] { "path" },
                trailingSlash: false
            );
        }

        [Test]
        public void TestUriConstructor8() {
            const string original = "http://user:password@domain.org:81/path//";
            AssertParse(original,
                scheme: "http",
                user: "user",
                password: "password",
                hostname: "domain.org",
                port: 81,
                usesDefaultPort: false,
                segments: new[] { "path", "/" },
                trailingSlash: false
            );
        }

        [Test]
        public void TestUriConstructor8_1() {
            const string original = "http://user:password@domain.org:81//";
            AssertParse(original,
                scheme: "http",
                user: "user",
                password: "password",
                hostname: "domain.org",
                port: 81,
                usesDefaultPort: false,
                segments: new[] { "/" },
                trailingSlash: false
            );
        }

        [Test]
        public void TestUriConstructor9() {
            const string original = "http://user:password@domain.org:81/path/foo%20bar/path//@blah";
            AssertParse(original,
                scheme: "http",
                user: "user",
                password: "password",
                hostname: "domain.org",
                port: 81,
                usesDefaultPort: false,
                segments: new[] { "path", "foo%20bar", "path", "/@blah" },
                trailingSlash: false
            );
        }

        [Test]
        public void TestUriConstructor10() {
            const string original = "http://user:password@domain.org:81/path/foo%20bar/path//@blah?ready&set=&go=foo/bar";
            AssertParse(original,
                scheme: "http",
                user: "user",
                password: "password",
                hostname: "domain.org",
                port: 81,
                usesDefaultPort: false,
                segments: new[] { "path", "foo%20bar", "path", "/@blah" },
                trailingSlash: false,
                @params: new[] {
                    new KeyValuePair<string, string>("ready", null), 
                    new KeyValuePair<string, string>("set", ""), 
                    new KeyValuePair<string, string>("go", "foo/bar")
                }
            );
        }

        [Test]
        public void TestUriConstructor11() {
            const string original = "http://user:password@domain.org:81/path/foo%20bar/path//@blah#yo";
            AssertParse(original,
                scheme: "http",
                user: "user",
                password: "password",
                hostname: "domain.org",
                port: 81,
                usesDefaultPort: false,
                segments: new[] { "path", "foo%20bar", "path", "/@blah" },
                trailingSlash: false,
                fragment: "yo"
            );
        }

        [Test]
        public void TestUriConstructor12() {
            const string original = "http://user:password@domain.org:81/path/foo%20bar/path//@blah/?ready&set=&go=foo/bar#yo";
            AssertParse(original,
                scheme: "http",
                user: "user",
                password: "password",
                hostname: "domain.org",
                port: 81,
                usesDefaultPort: false,
                segments: new[] { "path", "foo%20bar", "path", "/@blah" },
                trailingSlash: true,
                @params: new[] {
                    new KeyValuePair<string, string>("ready", null), 
                    new KeyValuePair<string, string>("set", ""), 
                    new KeyValuePair<string, string>("go", "foo/bar")
                },
                fragment: "yo"
            );
        }

        [Test]
        public void TestUriConstructor13() {
            const string original = "ftp://ftp.is.co.za/rfc/rfc1808.txt";
            AssertParse(original,
                scheme: "ftp",
                hostname: "ftp.is.co.za",
                port: 21,
                usesDefaultPort: true,
                segments: new[] { "rfc", "rfc1808.txt" },
                trailingSlash: false
            );
        }

        [Test]
        public void TestUriConstructor14() {
            const string original = "http://www.ietf.org/rfc/rfc2396.txt";
            AssertParse(original,
                scheme: "http",
                hostname: "www.ietf.org",
                port: 80,
                usesDefaultPort: true,
                segments: new[] { "rfc", "rfc2396.txt" },
                trailingSlash: false
            );
        }

        [Test, Ignore("doesn't pass the original test either")]
        public void TestUriConstructor15() {
            const string original = "ldap://[2001:db8::7]/c=GB?objectClass?one";
            AssertParse(original,
                scheme: "ldap",
                hostname: "[2001:db8::7]",
                port: -1,
                usesDefaultPort: true,
                segments: new[] { "c=GB" },
                trailingSlash: false,
                @params: new[] {
                    new KeyValuePair<string, string>("objectClass?one", null)
                }
            );
        }

        [Test]
        public void TestUriConstructor16() {
            const string original = "telnet://192.0.2.16:80/";
            AssertParse(original,
                scheme: "telnet",
                hostname: "192.0.2.16",
                port: 80,
                usesDefaultPort: false,
                segments: new string[0],
                trailingSlash: true
            );
        }

        [Test]
        public void TestUriConstructor17() {
            const string original = "ftp://cnn.example.com&story=breaking_news@10.0.0.1/top_story.htm#";
            AssertParse(original,
                scheme: "ftp",
                user: "cnn.example.com&story=breaking_news",
                hostname: "10.0.0.1",
                port: 21,
                usesDefaultPort: true,
                segments: new[] { "top_story.htm" },
                trailingSlash: false,
                fragment: ""
            );
        }

        [Test]
        public void TestUriConstructor18() {
            const string original = "http://domain.org/?";
            AssertParse(original,
                scheme: "http",
                hostname: "domain.org",
                port: 80,
                usesDefaultPort: true,
                segments: new string[0],
                trailingSlash: true,
                @params: new KeyValuePair<string, string>[0]
            );
        }

        [Test]
        public void TestUriConstructor19() {
            const string original = "http://domain.org?";
            AssertParse(original,
                scheme: "http",
                hostname: "domain.org",
                port: 80,
                usesDefaultPort: true,
                segments: new string[0],
                trailingSlash: false,
                @params: new KeyValuePair<string, string>[0]
            );
        }

        [Test]
        public void TestUriConstructor20() {
            const string original = "http://www.ietf.org/rfc;15/rfc2396.txt";
            AssertParse(original,
                scheme: "http",
                hostname: "www.ietf.org",
                port: 80,
                usesDefaultPort: true,
                segments: new[] { "rfc;15", "rfc2396.txt" },
                trailingSlash: false
            );
        }

        [Test]
        public void TestUriConstructor21() {
            const string original = "http://www.ietf.org/rfc;15/rfc2396.txt;";
            AssertParse(original,
                scheme: "http",
                hostname: "www.ietf.org",
                port: 80,
                usesDefaultPort: true,
                segments: new[] { "rfc;15", "rfc2396.txt;" },
                trailingSlash: false
            );
        }

        [Test]
        public void TestUriConstructor22() {
            const string original = "http://www.ietf.org/;15/rfc2396.txt;";
            AssertParse(original,
                scheme: "http",
                hostname: "www.ietf.org",
                port: 80,
                usesDefaultPort: true,
                segments: new[] { ";15", "rfc2396.txt;" },
                trailingSlash: false
            );
        }

        [Test]
        public void TestUriConstructor23() {
            const string original = "http:///path";
            AssertParse(original,
                scheme: "http",
                hostname: "",
                port: 80,
                usesDefaultPort: true,
                segments: new[] { "path" },
                trailingSlash: false
            );
        }

        [Test]
        public void TestUriConstructor24() {
            const string original = "http://host/seg^ment?qu^ery=a|b^c#fo|o#b^ar";
            AssertParse(original,
                scheme: "http",
                hostname: "host",
                port: 80,
                usesDefaultPort: true,
                segments: new[] { "seg^ment" },
                trailingSlash: false,
                @params: new[] { new KeyValuePair<string, string>("qu^ery", "a|b^c") },
                fragment: "fo|o#b^ar"
            );
        }

        [Test]
        public void Can_parse_square_brackets_in_query() {
            Assert.IsNotNull(XUri.TryParse("http://host/foo?bar[123]=abc"));
        }

        [Test]
        public void Can_parse_square_brackets_in_segment() {
            Assert.IsNotNull(XUri.TryParse("http://host/foo/[123]/bar"));
        }

        [Test]
        public void Can_parse_square_brackets_in_fragment() {
            Assert.IsNotNull(XUri.TryParse("http://host/foo#[bar]"));
        }

        [Test]
        public void Square_brackets_in_parsed_query_are_encoded_on_render() {
            Assert.AreEqual("http://host/foo?bar%5B123%5D=abc",new XUri("http://host/foo?bar[123]=abc").ToString());
        }

        [Test]
        public void Can_parse_curly_brackets_in_query() {
            Assert.IsNotNull(XUri.TryParse("http://test.com/AllItems.aspx?RootFolder={xyz}"));
        }

        [Test]
        public void Can_parse_curly_brackets_in_segment() {
            Assert.IsNotNull(XUri.TryParse("http://test.com/{xyz}/foo"));
        }

        [Test]
        public void Can_parse_curly_brackets_in_fragment() {
            Assert.IsNotNull(XUri.TryParse("http://test.com/foo#{xyz}"));
        }

        [Test]
        public void Curly_brackets_in_parsed_query_are_encoded_on_render() {
            Assert.AreEqual("http://test.com/AllItems.aspx?RootFolder=%7Bxyz%7D", new XUri("http://test.com/AllItems.aspx?RootFolder={xyz}").ToString());
        }

        [Test]
        public void TestXUriFromUriConstruction() {
            var evilSegments = new[] {

                // Escaped version of "Iñtërnâtiônàlizætiøn" (should look similar to "Internationalization" but with extended characteres)
                "I\u00f1t\u00ebrn\u00e2ti\u00f4n\u00e0liz\u00e6ti\u00f8n",
                "A%4b",
                "A^B"
            };
            foreach(var evil in evilSegments) {
                var original = new Uri("http://foo/" + evil);
                var fromDecoded = new Uri(original.ToString());
                var uri1 = new XUri(original);
                var uri2 = new XUri(fromDecoded);
                // just making sure they actually parse
            }
        }

        [Test]
        public void TestXUriFromUriConstruction2() {
            var evilSegments = new[] {

                // Escaped version of "Iñtërnâtiônàlizætiøn" (should look similar to "Internationalization" but with extended characteres)
                "I\u00f1t\u00ebrn\u00e2ti\u00f4n\u00e0liz\u00e6ti\u00f8n",
                "A%4b"
            };
            foreach(var evil in evilSegments) {
                var original = new XUri("http://" + evil);
                var fromDecoded = new XUri(original.ToString());
                var uri1 = new XUri(original);
                var uri2 = new XUri(fromDecoded);
                // just making sure they actually parse
            }
        }

        [Test]
        public void TestTryParse() {
            Assert.IsFalse(XUri.TryParse("htt;//") != null);
        }

        [Test, Ignore]
        public void ParsePerformance() {
            const string uri = "http://user:password@domain.org:81/path/foo%20bar/path//@blah/?ready&set=&go=foo/bar#yo";
            const int WARMUP = 1000;
            const int PERF_LOOPS = 500000;

            // test original parsing code
            for(var i = 0; i < WARMUP; ++i) {
                XUri.TryParse(uri);
            }
            var swOriginal = Stopwatch.StartNew();
            for(var i = 0; i < PERF_LOOPS; ++i) {
                XUri.TryParse(uri);
            }
            swOriginal.Stop();

            // test new parsing code
            for(var i = 0; i < WARMUP; ++i) {
                TryParse(uri);
            }
            var swNew = Stopwatch.StartNew();
            for(var i = 0; i < PERF_LOOPS; ++i) {
                TryParse(uri);
            }
            swNew.Stop();

            // show result
            Console.WriteLine("original: {0:#,##0}ms", swOriginal.ElapsedMilliseconds);
            Console.WriteLine("new     : {0:#,##0}ms", swNew.ElapsedMilliseconds);
        }
    }
}
