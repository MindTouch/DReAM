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
using System.Collections.Specialized;
using NUnit.Framework;

namespace MindTouch.Dream.Test {
    [TestFixture]
    public class DreamHeadersTests {

        [Test]
        public void Can_parse_bad_namevaluecollection_from_HttpContext() {
            var collections = new NameValueCollection();
            collections.Add("Cookie", "__utma=134392366.2030730348.1275932450.1276553042.1276556836.19; __utmz=134392366.1276207881.9.3.utmcsr=developer.mindtouch.com|utmccn=(referral)|utmcmd=referral|utmcct=/User:arnec/bugs; _mkto_trk=id:954-WGP-507&token:_mch-mindtouch.com-1270756717014-83706; WRUID=0; __kti=1274382964652");
            collections.Add("Cookie", "http%3A%2F%2Fwww.mindtouch.com%2F");
            collections.Add("Cookie", "; __ktv=2f4-f02d-634b-51e2128b724d7c2; __qca=P0-2102347259-1274460371553; PHPSESSID=307e779182909ab37932b4dffe77c40a; __utmc=134392366; __kts=1274382964673,http%3A%2F%2Fwww.mindtouch.com%2F,; __ktt=631f-d0a2-648e-e0b128b724d7c2; authtoken=\"1_634121336269193470_4254e33b49bc1ee0a72c5716200e296b\"; __utmb=134392366.6.10.1276556836");
            Assert.AreEqual(3, collections.GetValues("Cookie").Length);
            var headers = new DreamHeaders(collections);
            var cookies = headers.Cookies;
            Assert.AreEqual(13, cookies.Count);
            Assert.AreEqual("__utma", cookies[0].Name);
            Assert.AreEqual("134392366.2030730348.1275932450.1276553042.1276556836.19", cookies[0].Value);
            Assert.AreEqual("__utmz", cookies[1].Name);
            Assert.AreEqual("134392366.1276207881.9.3.utmcsr=developer.mindtouch.com|utmccn=(referral)|utmcmd=referral|utmcct=/User:arnec/bugs", cookies[1].Value);
            Assert.AreEqual("_mkto_trk", cookies[2].Name);
            Assert.AreEqual("id:954-WGP-507&token:_mch-mindtouch.com-1270756717014-83706", cookies[2].Value);
            Assert.AreEqual("WRUID", cookies[3].Name);
            Assert.AreEqual("0", cookies[3].Value);
            Assert.AreEqual("__kti", cookies[4].Name);
            Assert.AreEqual("1274382964652,http%3A%2F%2Fwww.mindtouch.com%2F,", cookies[4].Value);
            Assert.AreEqual("__ktv", cookies[5].Name);
            Assert.AreEqual("2f4-f02d-634b-51e2128b724d7c2", cookies[5].Value);
            Assert.AreEqual("__qca", cookies[6].Name);
            Assert.AreEqual("P0-2102347259-1274460371553", cookies[6].Value);
            Assert.AreEqual("PHPSESSID", cookies[7].Name);
            Assert.AreEqual("307e779182909ab37932b4dffe77c40a", cookies[7].Value);
            Assert.AreEqual("__utmc", cookies[8].Name);
            Assert.AreEqual("134392366", cookies[8].Value);
            Assert.AreEqual("__kts", cookies[9].Name);
            Assert.AreEqual("1274382964673,http%3A%2F%2Fwww.mindtouch.com%2F,", cookies[9].Value);
            Assert.AreEqual("__ktt", cookies[10].Name);
            Assert.AreEqual("631f-d0a2-648e-e0b128b724d7c2", cookies[10].Value);
            Assert.AreEqual("authtoken", cookies[11].Name);
            Assert.AreEqual("1_634121336269193470_4254e33b49bc1ee0a72c5716200e296b", cookies[11].Value);
            Assert.AreEqual("__utmb", cookies[12].Name);
            Assert.AreEqual("134392366.6.10.1276556836", cookies[12].Value);
        }

        [Test]
        public void Preserve_order_of_hosts_in_forwarded_for_header() {
            // X-Forwarded-For
            var collections = new NameValueCollection();
            collections.Add("X-Forwarded-For", "a, b, c");
            collections.Add("X-Forwarded-For", "d, e");
            collections.Add("X-Forwarded-For", "f, g, h");
            var headers = new DreamHeaders(collections);
            var values = headers.ForwardedFor;
            Assert.AreEqual(new []{ "a", "b", "c", "d", "e", "f", "g", "h" }, values);
        }
    }
}
