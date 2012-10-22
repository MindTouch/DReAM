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
using System.Net;

using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class CookieTests {

        [Test]
        public void ParseCookies1() {
            List<DreamCookie> cookies = DreamCookie.ParseCookieHeader("$Version=\"1\"; Customer=\"WILE_E_COYOTE\"; $Path=\"/acme\"");
            Assert.AreEqual(1, cookies.Count, "failed to parse cookies");
            Assert.AreEqual("Customer", cookies[0].Name, "bad cookie name");
            Assert.AreEqual("WILE_E_COYOTE", cookies[0].Value, "bad cookie value");
            Assert.AreEqual("/acme", cookies[0].Path, "bad cookie path");
        }

        [Test]
        public void ParseCookies2() {
            List<DreamCookie> cookies = DreamCookie.ParseCookieHeader("Customer=\"WILE_E_COYOTE\"; $Path=\"/acme\"");
            Assert.AreEqual(1, cookies.Count, "failed to parse cookies");
            Assert.AreEqual("Customer", cookies[0].Name, "bad cookie name");
            Assert.AreEqual("WILE_E_COYOTE", cookies[0].Value, "bad cookie value");
            Assert.AreEqual("/acme", cookies[0].Path, "bad cookie path");
        }

        [Test]
        public void ParseCookies3() {
            List<DreamCookie> cookies = DreamCookie.ParseCookieHeader("$Version=\"1\"; Customer=\"WILE_E_COYOTE\"; $Path=\"/acme\", Part_Number=\"Rocket_Launcher_0001\"; $Path=\"/acme\", Shipping=\"FedEx\"; $Path=\"/acme\"");
            Assert.AreEqual(3, cookies.Count, "failed to parse cookies");
            Assert.AreEqual("Customer", cookies[0].Name, "bad cookie 0 name");
            Assert.AreEqual("Part_Number", cookies[1].Name, "bad cookie 1 name");
            Assert.AreEqual("Shipping", cookies[2].Name, "bad cookie 2 name");
            Assert.AreEqual("WILE_E_COYOTE", cookies[0].Value, "bad cookie 0 value");
            Assert.AreEqual("Rocket_Launcher_0001", cookies[1].Value, "bad cookie 1 value");
            Assert.AreEqual("FedEx", cookies[2].Value, "bad cookie 2 value");
            Assert.AreEqual("/acme", cookies[0].Path, "bad cookie 0 path");
            Assert.AreEqual("/acme", cookies[1].Path, "bad cookie 1 path");
            Assert.AreEqual("/acme", cookies[2].Path, "bad cookie 2 path");
        }

        [Test]
        public void ParseCookies4() {
            List<DreamCookie> result = DreamCookie.ParseCookieHeader("$Version=\"1\"; Customer=\"WILE_E_COYOTE\"; $Path=\"/acme\", Part_Number=\"Rocket_Launcher_0001\"; $Path=\"/acme\"");
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Customer", result[0].Name);
            Assert.AreEqual("WILE_E_COYOTE", result[0].Value);
            Assert.AreEqual("/acme", result[0].Path);
            Assert.AreEqual("Part_Number", result[1].Name);
            Assert.AreEqual("Rocket_Launcher_0001", result[1].Value);
            Assert.AreEqual("/acme", result[1].Path);
        }

        [Test]
        public void ParseCookies5() {
            List<DreamCookie> result = DreamCookie.ParseCookieHeader("Customer=WILE_E_COYOTE; $Path=\"/acme\", Part_Number=\"Rocket \\\"Launcher\\\" 0001\"; $Path=\"/acme\"");
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Customer", result[0].Name);
            Assert.AreEqual("WILE_E_COYOTE", result[0].Value);
            Assert.AreEqual("/acme", result[0].Path);
            Assert.AreEqual("Part_Number", result[1].Name);
            Assert.AreEqual("Rocket \"Launcher\" 0001", result[1].Value);
            Assert.AreEqual("/acme", result[1].Path);
        }

        [Test]
        public void ParseCookies6() {
            List<DreamCookie> cookies = DreamCookie.ParseCookieHeader("$Version=\"1\", Customer=\"WILE_E_COYOTE\"; $Path=\"/acme\"");
            Assert.AreEqual(1, cookies.Count, "failed to parse cookies");
            Assert.AreEqual("Customer", cookies[0].Name, "bad cookie name");
            Assert.AreEqual("WILE_E_COYOTE", cookies[0].Value, "bad cookie value");
            Assert.AreEqual("/acme", cookies[0].Path, "bad cookie path");
        }

        [Test]
        public void ParseCookies7() {
            List<DreamCookie> cookies = new List<DreamCookie>();
            cookies.Add(new DreamCookie("Customer", "WILE_E_COYOTE", new XUri("http://localhost/acme")));
            string header = DreamCookie.RenderCookieHeader(cookies);
            cookies = DreamCookie.ParseCookieHeader(header);
            Assert.AreEqual(1, cookies.Count, "failed to parse cookies");
            Assert.AreEqual("Customer", cookies[0].Name, "bad cookie name");
            Assert.AreEqual("WILE_E_COYOTE", cookies[0].Value, "bad cookie value");
            Assert.AreEqual("/acme", cookies[0].Path, "bad cookie path");
        }

        [Test]
        public void ParseBadCookie1() {
            var cookies = DreamCookie.ParseCookieHeader("foo=\"bar\"; [index.php]scayt_verLang=5; authtoken=\"1234\"");
            Assert.AreEqual(3, cookies.Count, "Failed to parse cookies, wrong number of resulting cookies");
            Assert.AreEqual(cookies[0].Name, "foo", "bad cookie name");
            Assert.AreEqual(cookies[0].Value, "bar", "bad cookie value");
            Assert.AreEqual(cookies[2].Name, "authtoken", "bad cookie name");
            Assert.AreEqual(cookies[2].Value, "1234", "bad cookie value");
        }

        [Test]
        public void ParseBadCookie2() {
            var cookies = DreamCookie.ParseCookieHeader("  foo=\"bar\"; lithiumLogin:successfactors=~2acHBr09HxytcqIXV~eVqhSr8s74VfDTjhQ8XU615EaYeGn-7OdDSN70BshVnsYG71yPbJvKPoZzHl05KP; authtoken=\"1234\"  ");
            Assert.AreEqual(3, cookies.Count, "Failed to parse cookies, wrong number of resulting cookies");
            Assert.AreEqual(cookies[0].Name, "foo", "bad cookie name");
            Assert.AreEqual(cookies[0].Value, "bar", "bad cookie value");
            Assert.AreEqual(cookies[2].Name, "authtoken", "bad cookie name");
            Assert.AreEqual(cookies[2].Value, "1234", "bad cookie value");
        }

        [Test]
        public void ParseBadCookie3() {
            var cookies = DreamCookie.ParseCookieHeader("  foo=\"bar\", lithiumLogin:successfactors=~2acHBr09HxytcqIXV~eVqhSr8s74VfDTjhQ8XU615EaYeGn-7OdDSN70BshVnsYG71yPbJvKPoZzHl05KP; authtoken=\"1234\"  ");
            Assert.AreEqual(3, cookies.Count, "Failed to parse cookies, wrong number of resulting cookies");
            Assert.AreEqual(cookies[0].Name, "foo", "bad cookie name");
            Assert.AreEqual(cookies[0].Value, "bar", "bad cookie value");
            Assert.AreEqual(cookies[2].Name, "authtoken", "bad cookie name");
            Assert.AreEqual(cookies[2].Value, "1234", "bad cookie value");
        }

        [Test]
        public void ParseBadCookie4() {
            var cookies = DreamCookie.ParseCookieHeader("  foo=\"bar\"; hel,lo=\"wo,~rld\"; authtoken=\"1234\"  ");
            Assert.AreEqual(4, cookies.Count, "Failed to parse cookies, wrong number of resulting cookies");
            Assert.AreEqual(cookies[0].Name, "foo", "bad cookie name");
            Assert.AreEqual(cookies[0].Value, "bar", "bad cookie value");
            Assert.AreEqual(cookies[1].Name, "hel", "bad cookie name");
            Assert.IsNull(cookies[1].Value, null, "bad cookie value");
            Assert.AreEqual(cookies[2].Name, "lo", "bad cookie name");
            Assert.AreEqual(cookies[2].Value, "wo,~rld", "bad cookie value");
            Assert.AreEqual(cookies[3].Name, "authtoken", "bad cookie name");
            Assert.AreEqual(cookies[3].Value, "1234", "bad cookie value");
        }

        [Test]
        public void ParseBadCookie5() {
            var cookies = DreamCookie.ParseCookieHeader("  foo=\"bar\", hello=wo;;rld; authtoken=\"1234\"  ");
            Assert.AreEqual(4, cookies.Count, "Failed to parse cookies, wrong number of resulting cookies");
            Assert.AreEqual(cookies[0].Name, "foo", "bad cookie name");
            Assert.AreEqual(cookies[0].Value, "bar", "bad cookie value");
            Assert.AreEqual(cookies[3].Name, "authtoken", "bad cookie name");
            Assert.AreEqual(cookies[3].Value, "1234", "bad cookie value");
        }

        [Test]
        public void ParseCookies_with_unquoted_values() {
            List<DreamCookie> result = DreamCookie.ParseCookieHeader("__utma=134392366.697651776.1256325927.1256943466.1256946079.27; __utmz=134392366.1256946079.27.2.utmcsr=developer.mindtouch.com|utmccn=(referral)|utmcmd=referral|utmcct=/User:SteveB/Bugs; LOOPFUSE=78fe6a69-de6f-494f-9cf1-7e4fbe7a1c38; __kti=1256208055528,http%3A%2F%2Fwww.mindtouch.com%2F,; __ktv=9f88-8fb4-514a-a2cb1247bd5bce8; _mkto_trk=id:954-WGP-507&token:_mch-mindtouch.com-1256705011527-41439; __utma=249966356.478917817.1256718580.1256718580.1256946891.2; __utmz=249966356.1256946891.2.2.utmcsr=bugs.developer.mindtouch.com|utmccn=(referral)|utmcmd=referral|utmcct=/view.php; __utmc=134392366; __utmb=134392366.7.10.1256946079; PHPSESSID=bed8f2d85712b33f1a3804856045b374; __utmb=249966356.1.10.1256946891; __utmc=249966356; __kts=1256946891198,http%3A%2F%2Fcampaign.mindtouch.com%2FEvents%2FSharepoint,http%3A%2F%2Fbugs.developer.mindtouch.com%2Fview.php%3Fid%3D7255; __ktt=c2e3-a511-ce1b-438b124a7df79be,abc,");
            Assert.AreEqual(15, result.Count);
            Assert.AreEqual("__utma", result[0].Name);
            Assert.AreEqual("134392366.697651776.1256325927.1256943466.1256946079.27", result[0].Value);
            Assert.AreEqual("__utmz", result[1].Name);
            Assert.AreEqual("134392366.1256946079.27.2.utmcsr=developer.mindtouch.com|utmccn=(referral)|utmcmd=referral|utmcct=/User:SteveB/Bugs", result[1].Value);
            Assert.AreEqual("LOOPFUSE", result[2].Name);
            Assert.AreEqual("78fe6a69-de6f-494f-9cf1-7e4fbe7a1c38", result[2].Value);
            Assert.AreEqual("__kti", result[3].Name);
            Assert.AreEqual("1256208055528,http%3A%2F%2Fwww.mindtouch.com%2F,", result[3].Value);
            Assert.AreEqual("__ktv", result[4].Name);
            Assert.AreEqual("9f88-8fb4-514a-a2cb1247bd5bce8", result[4].Value);
            Assert.AreEqual("_mkto_trk", result[5].Name);
            Assert.AreEqual("id:954-WGP-507&token:_mch-mindtouch.com-1256705011527-41439", result[5].Value);
            Assert.AreEqual("__utma", result[6].Name);
            Assert.AreEqual("249966356.478917817.1256718580.1256718580.1256946891.2", result[6].Value);
            Assert.AreEqual("__utmz", result[7].Name);
            Assert.AreEqual("249966356.1256946891.2.2.utmcsr=bugs.developer.mindtouch.com|utmccn=(referral)|utmcmd=referral|utmcct=/view.php", result[7].Value);
            Assert.AreEqual("__utmc", result[8].Name);
            Assert.AreEqual("134392366", result[8].Value);
            Assert.AreEqual("__utmb", result[9].Name);
            Assert.AreEqual("134392366.7.10.1256946079", result[9].Value);
            Assert.AreEqual("PHPSESSID", result[10].Name);
            Assert.AreEqual("bed8f2d85712b33f1a3804856045b374", result[10].Value);
            Assert.AreEqual("__utmb", result[11].Name);
            Assert.AreEqual("249966356.1.10.1256946891", result[11].Value);
            Assert.AreEqual("__utmc", result[12].Name);
            Assert.AreEqual("249966356", result[12].Value);
            Assert.AreEqual("__kts", result[13].Name);
            Assert.AreEqual("1256946891198,http%3A%2F%2Fcampaign.mindtouch.com%2FEvents%2FSharepoint,http%3A%2F%2Fbugs.developer.mindtouch.com%2Fview.php%3Fid%3D7255", result[13].Value);
            Assert.AreEqual("__ktt", result[14].Name);
            Assert.AreEqual("c2e3-a511-ce1b-438b124a7df79be,abc,", result[14].Value);
        }

        [Test]
        public void ParseCookies_with_unquoted_values2() {
            List<DreamCookie> result = DreamCookie.ParseCookieHeader("__utma=134392366.2030730348.1275932450.1276553042.1276556836.19; __utmz=134392366.1276207881.9.3.utmcsr=developer.mindtouch.com|utmccn=(referral)|utmcmd=referral|utmcct=/User:arnec/bugs; _mkto_trk=id:954-WGP-507&token:_mch-mindtouch.com-1270756717014-83706; WRUID=0; __kti=1274382964652,http%3A%2F%2Fwww.mindtouch.com%2F,; __ktv=2f4-f02d-634b-51e2128b724d7c2; __qca=P0-2102347259-1274460371553; PHPSESSID=307e779182909ab37932b4dffe77c40a; __utmc=134392366; __kts=1274382964673,http%3A%2F%2Fwww.mindtouch.com%2F,; __ktt=631f-d0a2-648e-e0b128b724d7c2; authtoken=\"1_634121336269193470_4254e33b49bc1ee0a72c5716200e296b\"; __utmb=134392366.6.10.1276556836");
            Assert.AreEqual(13, result.Count);
            Assert.AreEqual("__utma", result[0].Name);
            Assert.AreEqual("134392366.2030730348.1275932450.1276553042.1276556836.19", result[0].Value);
            Assert.AreEqual("__utmz", result[1].Name);
            Assert.AreEqual("134392366.1276207881.9.3.utmcsr=developer.mindtouch.com|utmccn=(referral)|utmcmd=referral|utmcct=/User:arnec/bugs", result[1].Value);
            Assert.AreEqual("_mkto_trk", result[2].Name);
            Assert.AreEqual("id:954-WGP-507&token:_mch-mindtouch.com-1270756717014-83706", result[2].Value);
            Assert.AreEqual("WRUID", result[3].Name);
            Assert.AreEqual("0", result[3].Value);
            Assert.AreEqual("__kti", result[4].Name);
            Assert.AreEqual("1274382964652,http%3A%2F%2Fwww.mindtouch.com%2F,", result[4].Value);
            Assert.AreEqual("__ktv", result[5].Name);
            Assert.AreEqual("2f4-f02d-634b-51e2128b724d7c2", result[5].Value);
            Assert.AreEqual("__qca", result[6].Name);
            Assert.AreEqual("P0-2102347259-1274460371553", result[6].Value);
            Assert.AreEqual("PHPSESSID", result[7].Name);
            Assert.AreEqual("307e779182909ab37932b4dffe77c40a", result[7].Value);
            Assert.AreEqual("__utmc", result[8].Name);
            Assert.AreEqual("134392366", result[8].Value);
            Assert.AreEqual("__kts", result[9].Name);
            Assert.AreEqual("1274382964673,http%3A%2F%2Fwww.mindtouch.com%2F,", result[9].Value);
            Assert.AreEqual("__ktt", result[10].Name);
            Assert.AreEqual("631f-d0a2-648e-e0b128b724d7c2", result[10].Value);
            Assert.AreEqual("authtoken", result[11].Name);
            Assert.AreEqual("1_634121336269193470_4254e33b49bc1ee0a72c5716200e296b", result[11].Value);
            Assert.AreEqual("__utmb", result[12].Name);
            Assert.AreEqual("134392366.6.10.1276556836", result[12].Value);
        }

        [Test]
        public void Parse_SetCookie() {
            List<DreamCookie> result = DreamCookie.ParseSetCookieHeader("Customer=\"WILE_E_COYOTE\"; Version=\"1\"; Path=\"/acme\", Part_Number=\"Rocket_Launcher_0001\"; Version=\"1\"; Path=\"/acme\"");
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Customer", result[0].Name);
            Assert.AreEqual("WILE_E_COYOTE", result[0].Value);
            Assert.AreEqual(1, result[0].Version);
            Assert.AreEqual("/acme", result[0].Path);
            Assert.AreEqual(false, result[0].HttpOnly);
            Assert.AreEqual("Part_Number", result[1].Name);
            Assert.AreEqual("Rocket_Launcher_0001", result[1].Value);
            Assert.AreEqual(1, result[1].Version);
            Assert.AreEqual("/acme", result[1].Path);
            Assert.AreEqual(false, result[1].HttpOnly);
        }

        [Test]
        public void Parse_SetCookie_with_HttpOnly() {
            List<DreamCookie> result = DreamCookie.ParseSetCookieHeader("Customer=\"WILE_E_COYOTE\"; Version=\"1\"; Path=\"/acme\"; HttpOnly, Part_Number=\"Rocket_Launcher_0001\"; Version=\"1\"; Path=\"/acme\"; HttpOnly");
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Customer", result[0].Name);
            Assert.AreEqual("WILE_E_COYOTE", result[0].Value);
            Assert.AreEqual(1, result[0].Version);
            Assert.AreEqual(true, result[0].HttpOnly);
            Assert.AreEqual("/acme", result[0].Path);
            Assert.AreEqual("Part_Number", result[1].Name);
            Assert.AreEqual("Rocket_Launcher_0001", result[1].Value);
            Assert.AreEqual(1, result[1].Version);
            Assert.AreEqual("/acme", result[1].Path);
            Assert.AreEqual(true, result[1].HttpOnly);
        }

        [Test]
        public void Parse_5_cookies_separated_by_semicolon() {
            List<DreamCookie> cookies = DreamCookie.ParseCookieHeader("authtoken=\"3_633644459231333750_74b1192b1846f065523d01ac18c772c5\"; PHPSESSID=34c4b18a50a91dd99adb1ed1e6b570cb; __utma=14492279.2835659202033839600.1228849092.1228849092.1228849092.1; __utmc=14492279; __utmz=14492279.1228849092.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none)");
            Assert.AreEqual(5, cookies.Count);
            Assert.AreEqual("authtoken", cookies[0].Name);
            Assert.AreEqual("3_633644459231333750_74b1192b1846f065523d01ac18c772c5", cookies[0].Value);
            Assert.AreEqual("PHPSESSID", cookies[1].Name);
            Assert.AreEqual("34c4b18a50a91dd99adb1ed1e6b570cb", cookies[1].Value);
        }

        [Test]
        public void Parse_2_cookies_separated_by_semicolon() {
            List<DreamCookie> cookies = DreamCookie.ParseCookieHeader("PHPSESSID=663e17bc2eaef4e355c6e6fe1bb86c04; authtoken=1_633644446772281250_c3dd88ad4539197ef12f3614e91fec8f");
            Assert.AreEqual(2, cookies.Count);
            Assert.AreEqual("PHPSESSID", cookies[0].Name);
            Assert.AreEqual("663e17bc2eaef4e355c6e6fe1bb86c04", cookies[0].Value);
            Assert.AreEqual("authtoken", cookies[1].Name);
            Assert.AreEqual("1_633644446772281250_c3dd88ad4539197ef12f3614e91fec8f", cookies[1].Value);
        }

        [Test]
        public void Parse_cookie_without_path_or_domain() {
            List<DreamCookie> cookies = DreamCookie.ParseCookieHeader("foo=\"bar\"");
            Assert.IsNull(cookies[0].Path);
            Assert.IsNull(cookies[0].Domain);
        }

        [Test]
        public void Parse_cookie_sample_from_wikipedia() {
            List<DreamCookie> cookies = DreamCookie.ParseSetCookieHeader("RMID=732423sdfs73242; expires=Fri, 31-Dec-2010 23:59:59 GMT; path=/; domain=.example.net; HttpOnly");
            Assert.AreEqual(1, cookies.Count);
            Assert.AreEqual("RMID", cookies[0].Name);
            Assert.AreEqual("732423sdfs73242", cookies[0].Value);
            DateTime expires = DateTimeUtil.ParseExactInvariant("Fri, 31-Dec-2010 23:59:59 GMT", "ddd, dd-MMM-yyyy HH:mm:ss 'GMT'");
            Assert.AreEqual(expires, cookies[0].Expires);
            Assert.AreEqual("/", cookies[0].Path);

            // TODO (steveb): it seems wrong that we check for 'example.net' instead of '.example.net'
            Assert.AreEqual("example.net", cookies[0].Domain);
            Assert.AreEqual(true, cookies[0].HttpOnly);
        }

        [Test]
        public void GetCookie_with_no_path() {
            List<DreamCookie> cookies = new List<DreamCookie>();
            cookies.Add(new DreamCookie("foo", "bar", null));
            DreamCookie c = DreamCookie.GetCookie(cookies, "foo");
            Assert.IsNotNull(c);
            Assert.AreEqual("bar", c.Value);
        }

        [Test]
        public void SetCookie_without_uri() {
            DreamCookie cookie = DreamCookie.NewSetCookie("foo", "bar", null);
            Assert.IsNull(cookie.Path);
            Assert.IsNull(cookie.Domain);
            string cookieHeader = cookie.ToSetCookieHeader();
            Assert.IsFalse(cookieHeader.Contains("Path"));
            Assert.IsFalse(cookieHeader.Contains("Domain"));
        }

        [Test]
        public void Render_SetCookie_header_for_hostname_does_not_start_with_dot() {
            string header = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://myhost/x/y/z")).ToSetCookieHeader();
            Assert.IsTrue(header.Contains("Path=/x/y/z"), "path is bad: " + header);
            Assert.IsFalse(header.Contains("Domain"), "domain found: " + header);
        }

        [Test]
        public void Render_SetCookie_header_for_localhost_does_not_start_with_dot() {
            string header = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://localhost/x/y/z")).ToSetCookieHeader();
            Assert.IsTrue(header.Contains("Path=/x/y/z"), "path is bad: " + header);
            Assert.IsFalse(header.Contains("Domain"), "domain found: " + header);
        }

        [Test]
        public void Render_SetCookie_header_for_domainname_does_start_with_dot() {
            string header = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://foo.com/x/y/z")).ToSetCookieHeader();
            Assert.IsTrue(header.Contains("Path=/x/y/z"), "path is bad: " + header);
            Assert.IsTrue(header.Contains("Domain=.foo.com"), "domain is bad: " + header);

        }

        [Test]
        public void Render_SetCookied_with_HttpOnly() {
            string header = DreamCookie.NewSetCookie("foo", "bar", new XUri("http://foo.com/x/y/z"), DateTime.MaxValue, false, null, null, true).ToSetCookieHeader();
            Assert.IsTrue(header.Contains("; HttpOnly"), "missing HttpOnly");
        }

        [Test]
        public void SetCookie_to_Xml_and_back() {
            DreamCookie cookie1 = DreamCookie.NewSetCookie(
                "foo",
                "bar",
                new XUri("http://abc.com/path"),
                DateTime.Now.AddDays(1),
                true,
                "blah blah",
                new XUri("http://comment.com/blah"));
            XDoc cookieDoc = cookie1.AsSetCookieDocument;
            DreamCookie cookie2 = DreamCookie.ParseSetCookie(cookieDoc);
            Assert.AreEqual(cookie1.Comment, cookie2.Comment);
            Assert.AreEqual(cookie1.CommentUri, cookie2.CommentUri);
            Assert.AreEqual(cookie1.Domain, cookie2.Domain);
            Assert.AreEqual(cookie1.Expired, cookie2.Expired);
            Assert.AreEqual(cookie1.Expires, cookie2.Expires);
            Assert.AreEqual(cookie1.Name, cookie2.Name);
            Assert.AreEqual(cookie1.Path, cookie2.Path);
            Assert.AreEqual(cookie1.Secure, cookie2.Secure);
            Assert.AreEqual(cookie1.Uri, cookie2.Uri);
            Assert.AreEqual(cookie1.Value, cookie2.Value);
        }

        [Test]
        public void SetCookie_to_header_and_back() {
            DreamCookie cookie1 = DreamCookie.NewSetCookie(
                "foo",
                "bar",
                new XUri("http://abc.com/path"),
                DateTime.Now.AddDays(1),
                true,
                "blah blah",
                new XUri("http://comment.com/blah"));
            string header = cookie1.ToSetCookieHeader();
            List<DreamCookie> cookies = DreamCookie.ParseSetCookieHeader(header);
            Assert.AreEqual(1, cookies.Count);
            DreamCookie cookie2 = cookies[0];
            Assert.AreEqual(cookie1.Comment, cookie2.Comment);
            Assert.AreEqual(cookie1.CommentUri, cookie2.CommentUri);
            Assert.AreEqual(cookie1.Domain, cookie2.Domain);
            Assert.AreEqual(cookie1.Expires, cookie2.Expires);
            Assert.AreEqual(cookie1.Expired, cookie2.Expired);
            Assert.AreEqual(cookie1.Name, cookie2.Name);
            Assert.AreEqual(cookie1.Path, cookie2.Path);
            Assert.AreEqual(cookie1.Secure, cookie2.Secure);
            Assert.AreEqual(cookie1.Uri, cookie2.Uri);
            Assert.AreEqual(cookie1.Value, cookie2.Value);
        }

        [Test]
        public void Create_SetCookie_header() {
            DreamCookie setcookie = DreamCookie.NewSetCookie("test", "123", new XUri("http://bar.com/foo"));
            Assert.AreEqual("/foo", setcookie.Path);
            Assert.AreEqual("bar.com", setcookie.Domain);
            string setcookieString = setcookie.ToSetCookieHeader();
            Assert.AreEqual("test=\"123\"; Domain=.bar.com; Version=1; Path=/foo", setcookieString);
            List<DreamCookie> cookies = DreamCookie.ParseSetCookieHeader(setcookieString);
            Assert.AreEqual(1, cookies.Count);
            Assert.AreEqual("/foo", cookies[0].Path);
            Assert.AreEqual("bar.com", cookies[0].Domain);
        }

        [Test]
        public void Fetch_cookie_from_jar() {
            DreamCookie setcookie = DreamCookie.NewSetCookie("test", "123", new XUri("http://bar.com/foo"));
            List<DreamCookie> cookies = new List<DreamCookie>();
            cookies.Add(setcookie);
            DreamCookieJar jar = new DreamCookieJar();
            jar.Update(cookies, null);
            List<DreamCookie> cookies2 = jar.Fetch(new XUri("http://bar.com/foo/baz"));
            Assert.AreEqual(1, cookies2.Count);
            Assert.AreEqual("/foo", cookies2[0].Path);
            Assert.AreEqual("bar.com", cookies2[0].Domain);
        }

        [Test]
        public void Set_Fetch_cookie_from_jar_for_https() {
            List<DreamCookie> setcookies = new List<DreamCookie>();
            setcookies.Add(DreamCookie.NewSetCookie("authtoken", "1_633698885517217440_64e1d64e732341bde1797f20fe2ab824", new XUri("http:///"), DateTime.UtcNow.AddDays(2)));
            DreamCookieJar jar = new DreamCookieJar();
            jar.Update(setcookies,new XUri("https://admin:pass@wikiaddress//@api/deki/users/authenticate"));
            List<DreamCookie> cookies = jar.Fetch(new XUri("https://wikiaddress//@api/deki/Pages/home/files,subpages"));
            Assert.AreEqual(1,cookies.Count);
            Assert.AreEqual("/", cookies[0].Path);
            Assert.AreEqual("1_633698885517217440_64e1d64e732341bde1797f20fe2ab824", cookies[0].Value);
            Assert.AreEqual("authtoken", cookies[0].Name);
        }

        [Test]
        public void Fetch_cookie_from_dreamcontext_jar_with_local_uri() {
            using(DreamHostInfo hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config").Elem("uri.public", "/foo/bar"))) {
                MockServiceInfo mock = MockService.CreateMockService(hostInfo);
                List<DreamCookie> cookies = null;
                mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                    context.Service.Cookies.Update(new DreamCookie("public", "bar", mock.AtLocalHost.Uri), null);
                    context.Service.Cookies.Update(new DreamCookie("local", "baz", mock.AtLocalMachine.Uri), null);
                    cookies = context.Service.Cookies.Fetch(mock.AtLocalHost.Uri);
                    response2.Return(DreamMessage.Ok());
                };
                mock.AtLocalHost.Post();
                bool foundPublic = false;
                bool foundLocal = false;
                foreach(DreamCookie cookie in cookies) {
                    switch(cookie.Name) {
                    case "public":
                        foundPublic = true;
                        break;
                    case "local":
                        foundLocal = true;
                        break;
                    }
                }
                Assert.IsTrue(foundPublic, "didn't find public");
                Assert.IsTrue(foundLocal, "didn't find local");
            }
        }

        [Test]
        public void SetCookie_header_passed_between_services_uses_local_uri() {
            using(DreamHostInfo hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                MockServiceInfo mock1 = MockService.CreateMockService(hostInfo);
                MockServiceInfo mock2 = MockService.CreateMockService(hostInfo);
                List<DreamCookie> cookies = null;
                mock1.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                    DreamMessage r = Plug.New(mock2.AtLocalMachine).Post(DreamMessage.Ok());
                    cookies = context.Service.Cookies.Fetch(mock2.AtLocalMachine);
                    Assert.AreEqual(1, cookies.Count);
                    Assert.AreEqual(mock2.AtLocalMachine.Uri, cookies[0].Uri);
                    response2.Return(DreamMessage.Ok());
                };
                mock2.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                    DreamMessage msg = DreamMessage.Ok();
                    msg.Cookies.Add(DreamCookie.NewSetCookie("foo", "bar", context.Uri));
                    response2.Return(msg);
                };
                mock1.AtLocalHost.Post();
                Assert.AreEqual(1, cookies.Count);
            }
        }

        [Test]
        public void SetCookie_header_passed_from_service_over_webrequest_uses_public_uri() {
            using(DreamHostInfo hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                MockServiceInfo mock = MockService.CreateMockService(hostInfo);
                mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                    DreamMessage msg = DreamMessage.Ok();
                    msg.Cookies.Add(DreamCookie.NewSetCookie("foo", "bar", context.Uri));
                    response2.Return(msg);
                };
                XUri localUri = mock.AtLocalHost.Uri;
                HttpWebRequest r = WebRequest.Create(localUri.ToString()) as HttpWebRequest;
                CookieContainer cookies = new CookieContainer();
                r.CookieContainer = cookies;
                r.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)r.GetResponse();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual(1, response.Cookies.Count);
                Assert.AreEqual("localhost", response.Cookies[0].Domain);
                Assert.AreEqual(mock.AtLocalHost.Uri.Path, response.Cookies[0].Path);
            }
        }

        [Test]
        public void SetCookie_header_passed_from_service_to_external_uses_public_uri() {
            using(DreamHostInfo hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                MockServiceInfo mock = MockService.CreateMockService(hostInfo);
                mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                    DreamMessage msg = DreamMessage.Ok();
                    msg.Cookies.Add(DreamCookie.NewSetCookie("foo", "bar", context.Uri));
                    response2.Return(msg);
                };
                mock.AtLocalHost.Post();
                List<DreamCookie> cookies = Plug.GlobalCookies.Fetch(mock.AtLocalHost);
                Assert.AreEqual(1, cookies.Count);
                Assert.AreEqual(mock.AtLocalHost.Uri, cookies[0].Uri);
            }
        }

        [Test]
        public void SetCookie_header_passed_between_services_uses_local_uri_on_host_non_root_public_uri() {
            using(DreamHostInfo hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config").Elem("uri.public", "/foo/bar"))) {
                MockServiceInfo mock1 = MockService.CreateMockService(hostInfo);
                MockServiceInfo mock2 = MockService.CreateMockService(hostInfo);
                List<DreamCookie> cookies = null;
                mock1.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                    DreamMessage r = Plug.New(mock2.AtLocalMachine).Post(DreamMessage.Ok());
                    cookies = context.Service.Cookies.Fetch(mock2.AtLocalMachine);
                    Assert.AreEqual(1, cookies.Count);
                    Assert.AreEqual(mock2.AtLocalMachine.Uri, cookies[0].Uri);
                    response2.Return(DreamMessage.Ok());
                };
                mock2.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                    DreamMessage msg = DreamMessage.Ok();
                    msg.Cookies.Add(DreamCookie.NewSetCookie("foo", "bar", context.Uri));
                    response2.Return(msg);
                };
                mock1.AtLocalHost.Post();
                Assert.AreEqual(1, cookies.Count);
            }
        }

        [Test]
        public void SetCookie_header_passed_from_service_to_external_uses_public_uri_on_host_non_root_public_uri() {
            using(DreamHostInfo hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config").Elem("uri.public", "/foo/bar"))) {
                MockServiceInfo mock = MockService.CreateMockService(hostInfo);
                mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                    DreamMessage msg = DreamMessage.Ok();
                    msg.Cookies.Add(DreamCookie.NewSetCookie("foo", "bar", context.Uri));
                    response2.Return(msg);
                };
                mock.AtLocalHost.Post();
                List<DreamCookie> cookies = Plug.GlobalCookies.Fetch(mock.AtLocalHost);
                Assert.AreEqual(1, cookies.Count);
                Assert.AreEqual(mock.AtLocalHost.Uri, cookies[0].Uri);
            }
        }

        [Test]
        public void SetCookie_header_passed_from_service_to_external_but_rooted_at_external() {
            using(DreamHostInfo hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config").Elem("uri.public", "/foo/bar"))) {
                MockServiceInfo mock = MockService.CreateMockService(hostInfo);
                mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                    DreamMessage msg = DreamMessage.Ok();
                    msg.Cookies.Add(DreamCookie.NewSetCookie("foo", "bar", context.Uri.AsPublicUri().WithoutPathQueryFragment()));
                    response2.Return(msg);
                };
                mock.AtLocalHost.Post();
                List<DreamCookie> cookies = Plug.GlobalCookies.Fetch(mock.AtLocalHost);
                Assert.AreEqual(1, cookies.Count);
                Assert.AreEqual("/", cookies[0].Path);
            }
        }

    }
}
