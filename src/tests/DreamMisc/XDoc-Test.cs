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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Tasking;

using NUnit.Framework;

namespace MindTouch.Xml.Test {

    [TestFixture]
    public class XDocTest {

        //-- Fields ---
        XDoc _doc;

        //--- Methods ---
        [SetUp]
        public void Init() {
            _doc = new XDoc("doc")
                .Attr("source", "http://www.mindtouch.com")
                .Value("Hello ")
                .Start("bold")
                    .Attr("style", "blinking")
                    .Value("World")
                .End()
                .Value("!")
                .Start("br").End()
                .Start("bold")
                    .Value("Cool")
                .End()
                .Start("span")
                    .Value("Ce\u00e7i est \"une\" id\u00e9e")
                .End()
                .Start("struct")
                    .Start("name").Value("John").End()
                    .Start("last").Value("Doe").End()
                .End();
        }

        [Test]
        public void XmlSerialization1() {
            Test("xml serialization", _doc.ToString(), "<doc source=\"http://www.mindtouch.com\">Hello <bold style=\"blinking\">World</bold>!<br /><bold>Cool</bold><span>Ce\u00e7i est \"une\" id\u00e9e</span><struct><name>John</name><last>Doe</last></struct></doc>");
        }

        [Test]
        public void ElementCount() {
            Test("element count", _doc["bold"].ListLength.ToString(), "2");
        }

        [Test]
        public void ElementAccessIndexed() {
            Test("element access (indexed)", _doc[4].Contents, "Cool");
        }

        [Test]
        public void ElementAccessXPathFirst() {
            Test("element access (xpath, first)", _doc["bold"].Contents, "World");
        }

        [Test]
        public void ElementAccessXPathSecond() {
            Test("element access (xpath, second)", _doc["bold"].Next.Contents, "Cool");
        }

        [Test]
        public void ElementAccessXPathWithIndex() {
            Test("element access (xpath with index)", _doc["bold[2]"].Contents, "Cool");
        }

        [Test]
        public void JsonSerialization() {
            Test("json serialization", JsonUtil.ToJson(_doc), "{\"@source\":\"http://www.mindtouch.com\",\"#text\":[\"Hello \",\"!\"],\"bold\":[{\"@style\":\"blinking\",\"#text\":\"World\"},\"Cool\"],\"br\":\"\",\"span\":\"Ce\\u00e7i est \\\"une\\\" id\\u00e9e\",\"struct\":{\"last\":\"Doe\",\"name\":\"John\"}}");
        }

        [Test]
        public void JsonpSerialization1() {
            Test("json serialization", JsonUtil.ToJsonp(_doc), "({\"doc\":{\"@source\":\"http://www.mindtouch.com\",\"#text\":[\"Hello \",\"!\"],\"bold\":[{\"@style\":\"blinking\",\"#text\":\"World\"},\"Cool\"],\"br\":\"\",\"span\":\"Ce\\u00e7i est \\\"une\\\" id\\u00e9e\",\"struct\":{\"last\":\"Doe\",\"name\":\"John\"}}})");
        }

        [Test]
        public void JsonpSerialization2() {
            XDoc doc = new XDoc("test");
            doc.Value("<tag>\"text\"</tag>");
            string text = JsonUtil.ToJsonp(doc);
            Assert.AreEqual("({\"test\":\"<tag>\\\"text\\\"</tag>\"})", text);
        }

        [Test]
        public void PhpSerialization1() {
            XDoc doc = new XDoc("test");
            doc.Value("<tag>\"text\"</tag>");
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
            MemoryStream stream = new MemoryStream();
            PhpUtil.WritePhp(doc, stream, encoding);
            byte[] text = stream.ToArray();
            Assert.IsTrue(ArrayUtil.Compare(encoding.GetBytes("a:1:{s:4:\"test\";s:17:\"<tag>\"text\"</tag>\";}"), text) == 0);
        }

        [Test]
        public void PhpSerialization2() {
            XDoc doc = new XDoc("test");
            doc.Value("<tag>text\ntext</tag>");
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
            MemoryStream stream = new MemoryStream();
            PhpUtil.WritePhp(doc, stream, encoding);
            byte[] text = stream.ToArray();
            Assert.IsTrue(ArrayUtil.Compare(encoding.GetBytes("a:1:{s:4:\"test\";s:20:\"<tag>text\ntext</tag>\";}"), text) == 0);
        }

        [Test]
        public void PhpSerialization3() {
            XDoc doc = new XDoc("test");
            doc.Value("<tag>ö</tag>");
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
            MemoryStream stream = new MemoryStream();
            PhpUtil.WritePhp(doc, stream, encoding);
            byte[] text = stream.ToArray();
            Assert.IsTrue(ArrayUtil.Compare(encoding.GetBytes("a:1:{s:4:\"test\";s:12:\"<tag>ö</tag>\";}"), text) == 0);
        }

        [Test]
        public void XmlAdd1() {
            _doc.Add(new XDoc("subdoc").Start("tag").Value("value").End());
            Test("xml add doc", _doc.ToString(), "<doc source=\"http://www.mindtouch.com\">Hello <bold style=\"blinking\">World</bold>!<br /><bold>Cool</bold><span>Ce\u00e7i est \"une\" id\u00e9e</span><struct><name>John</name><last>Doe</last></struct><subdoc><tag>value</tag></subdoc></doc>");
        }

        [Test]
        public void XmlAdd2() {
            _doc["bold"].Start("italic").Value(1).End().Start("underline").Value(2).End();
            Test("xml add elements", _doc.ToString(), "<doc source=\"http://www.mindtouch.com\">Hello <bold style=\"blinking\">World<italic>1</italic><underline>2</underline></bold>!<br /><bold>Cool</bold><span>Ce\u00e7i est \"une\" id\u00e9e</span><struct><name>John</name><last>Doe</last></struct></doc>");
        }

        [Test]
        public void XmlClone1() {
            XDoc doc = _doc.Clone();
            Test("xml serialization", doc.ToString(), "<doc source=\"http://www.mindtouch.com\">Hello <bold style=\"blinking\">World</bold>!<br /><bold>Cool</bold><span>Ce\u00e7i est \"une\" id\u00e9e</span><struct><name>John</name><last>Doe</last></struct></doc>");
        }

        [Test]
        public void XmlClone2() {
            XDoc doc = _doc["struct"].Clone();
            Test("xml clone", doc.ToString(), "<struct><name>John</name><last>Doe</last></struct>");
        }

        [Test]
        public void XmlEndAll1() {
            XDoc doc = _doc["struct"].EndAll();
            Test("xml end all", doc.Name, "struct");
        }

        [Test]
        public void XmlEndAll2() {
            XDoc doc = _doc["struct"].Start("outer").Start("inner").EndAll();
            Test("xml end all", doc.Name, "struct");
        }

        [Test]
        public void XmlEnd() {
            XDoc doc = _doc["struct"].Start("outer").Start("inner").End().End();
            Test("xml end all", doc.Name, "struct");
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void XmlEnd_Fail() {
            XDoc doc = _doc["struct"].Start("outer").Start("inner").End().End().End();
        }

        [Test]
        public void XmlAddBefore() {
            XDoc doc = new XDoc("root").Start("first").End().Start("second").End().Start("third").End();
            doc["second"].AddBefore(new XDoc("node"));
            Test("xml add before", doc.ToString(), "<root><first /><node /><second /><third /></root>");
        }

        [Test]
        public void XmlAddAfter() {
            XDoc doc = new XDoc("root").Start("first").End().Start("second").End().Start("third").End();
            doc["second"].AddAfter(new XDoc("node"));
            Test("xml add before", doc.ToString(), "<root><first /><second /><node /><third /></root>");
        }

        [Test]
        public void VersitSerialization() {
            XDoc cal = new XDoc("icalendar");
            cal.Start("vcalendar");
            cal.Start("prodid").Value("-//MindTouch//Deki//Calendar//EN").End();
            cal.Start("version").Value("1.0").End();
            cal.Start("vevent");
            cal.Start("dtstart").Value(new DateTime(2006, 9, 1, 16, 30, 0, DateTimeKind.Utc)).End();
            cal.Start("dtend").Value(new DateTime(2006, 9, 1, 17, 15, 0, DateTimeKind.Utc)).End();
            cal.Start("location").Attr("encoding", "QUOTED-PRINTABLE").Value("Joe's Office").End();
            cal.Start("uid").Value("264665-2").End();
            cal.Start("categories").Attr("encoding", "QUOTED-PRINTABLE").Value("Meeting").End();
            cal.Start("summary").Attr("encoding", "QUOTED-PRINTABLE").Value("Meeting: status & pizza").End();
            cal.Start("description").Value("Please show-up on time.\n\nPizzas will be ordered after meeting.").End();
            cal.Start("priority").Value("2").End();
            cal.End();
            cal.End();

            string text = VersitUtil.ToVersit(cal);
            string expected =
                "BEGIN:VCALENDAR\r\n" +
                "PRODID:-//MindTouch//Deki//Calendar//EN\r\n" +
                "VERSION:1.0\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DTSTART:20060901T163000Z\r\n" +
                "DTEND:20060901T171500Z\r\n" +
                "LOCATION;ENCODING=QUOTED-PRINTABLE:Joe's Office\r\n" +
                "UID:264665-2\r\n" +
                "CATEGORIES;ENCODING=QUOTED-PRINTABLE:Meeting\r\n" +
                "SUMMARY;ENCODING=QUOTED-PRINTABLE:Meeting: status & pizza\r\n" +
                "DESCRIPTION:Please show-up on time.\\n\\nPizzas will be ordered after meeting.\r\n" +
                "PRIORITY:2\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n";

            Assert.AreEqual(expected, text, "versit did not match");
        }

        [Test]
        public void VersitDeserialization() {
            string versit =
                "BEGIN:VCALENDAR\r\n" +
                "PRODID:-//MindTouch//Deki//Calendar//EN\r\n" +
                "VERSION:1.0\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DTSTART:20060902T063000Z\r\n" +
                "DTEND:20060902T071500Z\r\n" +
                "LOCATION;ENCODING=QUOTED-PRINTABLE:Joe's Office\r\n" +
                "UID:264665-2\r\n" +
                "CATEGORIES;ENCODING=QUOTED-PRINTABLE:Meeting\r\n" +
                "SUMMARY;ENCODING=QUOTED-PRINTABLE:Meeting: status & pizza\r\n" +
                "DESCRIPTION:Please show-up on time.\\n\\n\r\n" +
                " Pizzas will be ordered after meeting.\r\n" +
                "PRIORITY:2\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n";

            string text = VersitUtil.FromVersit(versit, "icalendar").ToString();
            string expected = "<icalendar><vcalendar><prodid>-//MindTouch//Deki//Calendar//EN</prodid><version>1.0</version><vevent><dtstart>2006-09-02T06:30:00Z</dtstart><dtend>2006-09-02T07:15:00Z</dtend><location encoding=\"QUOTED-PRINTABLE\">Joe's Office</location><uid>264665-2</uid><categories encoding=\"QUOTED-PRINTABLE\">Meeting</categories><summary encoding=\"QUOTED-PRINTABLE\">Meeting: status &amp; pizza</summary><description>Please show-up on time.\n\nPizzas will be ordered after meeting.</description><priority>2</priority></vevent></vcalendar></icalendar>";

            Assert.AreEqual(expected, text, "versit-xml did not match");
        }

        [Test]
        public void XDocFactory_parse_empty_XML() {
            string xml = "";
            XDoc doc = XDocFactory.From(xml, MimeType.XML);
            Assert.IsTrue(doc.IsEmpty);
        }

        [Test]
        public void XDocFactory_parse_non_XML() {
            string xml = "hello";
            XDoc doc = XDocFactory.From(xml, MimeType.XML);
            Assert.IsTrue(doc.IsEmpty);
        }

        [Test]
        public void XDocFactory_parse_valid_XML() {
            string xml = "<root>mixed<inner>text</inner></root>";
            XDoc doc = XDocFactory.From(xml, MimeType.XML);
            Assert.IsFalse(doc.IsEmpty);
            Assert.AreEqual(xml, doc.ToString());
        }

        [Test]
        public void XDocFactory_parse_XHTML_with_entity_codes() {
            string xhtml = "<html><body>&Omega; &alefsym; &Yuml; &euro; &copy; &oslash; &nbsp;</body></html>";
            XDoc doc = XDocFactory.From(xhtml, MimeType.HTML);
            Assert.AreNotEqual(null, doc, "could not load XHTML document");

            // check if entities were converted
            string text = doc["body"].AsText;
            byte[] values = new byte[] { 206, 169, 32, 226, 132, 181, 32, 197, 184, 32, 226, 130, 172, 32, 194, 169, 32, 195, 184, 32, 194, 160 };
            Assert.IsTrue(ArrayUtil.Compare(values, Encoding.UTF8.GetBytes(text)) == 0, "incorrect entity encoding");
        }

        [Test]
        public void XDoc_render_XML() {
            XDoc doc = new XDoc("html").Start("body").Start("a").Attr("href", "http://foo.bar?q=1&p=2").Value("test").End().End();
            string test = doc.ToString();
            Assert.AreEqual("<html><body><a href=\"http://foo.bar?q=1&amp;p=2\">test</a></body></html>", test);
        }

        [Test]
        public void XDocFactory_parse_HTML_with_entity_codes_and_render_as_XHTML() {
            string xhtml = "<html><body>&Omega; &alefsym; &Yuml; &euro; &copy; &oslash; &nbsp;</body></html>";
            XDoc doc = XDocFactory.From(xhtml, MimeType.HTML);

            // check if entities were converted
            string text = doc["body"].ToXHtml();
            Assert.AreEqual("<body>&Omega; &alefsym; &Yuml; &euro; &copy; &oslash; &nbsp;</body>", text);
        }

        [Test]
        public void XDocFactory_parse_HTML_with_entity_codes_and_render_as_XHTML_without_entities() {
            string xhtml = "<html><body>&Omega; &alefsym; &Yuml; &euro; &copy; &oslash; &nbsp;</body></html>";
            XDoc doc = XDocFactory.From(xhtml, MimeType.HTML);

            // check if entities were converted
            string text = doc["body"].ToXHtml(false);
            Assert.AreEqual("<body>&#937; &#8501; &#376; &#8364; &#169; &#248; &#160;</body>", text);
        }

        [Test, Ignore]
        public void HtmlPerf() {
            System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
            s.Start();
            int count = 1000;
            for(int i = 0; i < count; ++i) {
                string xhtml = "<html><body><br></br></body></html>";
                XDoc doc = XDocFactory.From(xhtml, MimeType.HTML);
            }
            s.Stop();
            Console.WriteLine("XDocFactory.From: total {0} secs, average {1} microseconds", s.Elapsed.TotalSeconds, (s.Elapsed.TotalSeconds / count) * 1000000);
        }

        [Test]
        public void XPost() {
            XDoc doc = new XDoc("test");
            doc.InsertValueAt("", "text");
            doc.InsertValueAt("@id", "123");
            doc.InsertValueAt("foo/a", "a");
            doc.InsertValueAt("foo/b", "b");
            doc.InsertValueAt("foo/@key", "value");
            doc.InsertValueAt("foo[3]/c", "c");
            doc.InsertValueAt("bar[3]/d", "d");
            doc.InsertValueAt("foo[5]/e", "e");
            doc.InsertValueAt("foo[5]/f", "f");

            string text = doc.ToString();
            string expected = "<test id=\"123\">text<foo key=\"value\"><a>a</a><b>b</b></foo><foo /><foo><c>c</c></foo><bar /><bar /><bar><d>d</d></bar><foo /><foo><e>e</e><f>f</f></foo></test>";

            Assert.AreEqual(expected, text, "xpost-xml did not match");
        }

        [Test]
        public void XmlContents() {
            XDoc doc = new XDoc("test");
            doc.Value("<tag>text</tag>");
            string text = doc.Contents;
            Assert.AreEqual("<tag>text</tag>", text);
        }

        [Test]
        public void XmlAsText1() {
            XDoc doc = new XDoc("test");
            string text = doc["foo"].AsText;
            Assert.AreEqual(null, text);
        }

        [Test]
        public void XmlAsText2() {
            XDoc doc = new XDoc("test").Start("aaa").Value("1").End().Start("zzz").Value("2").End();
            string text = doc["foo"].AsText;
            Assert.AreEqual(null, text);
        }

        [Test]
        public void XmlAsText3() {
            XDoc doc = new XDoc("test").Start("aaa").Value("1").End().Elem("zzz");
            string text = doc["zzz"].AsText;
            Assert.AreEqual(string.Empty, text);
        }

        [Test]
        public void XmlAsInnerText() {
            XDoc doc = new XDoc("test").Elem("a", "1").Elem("a", "2").Elem("a", "3").Elem("a", "4").Elem("a", "5");
            string text = doc.AsInnerText;
            Assert.AreEqual("12345", text);
        }

        [Test]
        public void XmlAsUri() {
            XUri uri = new XUri("http://foo.com/bar");
            XDoc doc = new XDoc("test").Elem("uri", uri.ToString());
            Assert.AreEqual(uri.ToString(), doc["uri"].AsText);
            Assert.AreEqual(uri, doc["uri"].AsUri);
        }

        [Test]
        public void XmlAsUriWithDreamContext() {
            DreamHostInfo hostInfo = DreamTestHelper.CreateRandomPortHost();
            MockServiceInfo mock = MockService.CreateMockService(hostInfo);
            mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                XUri uri = mock.AtLocalMachine.Uri;
                XDoc doc = new XDoc("test").Elem("uri", uri);
                Assert.AreEqual(uri.AsPublicUri().ToString(), doc["uri"].AsText);
                Assert.AreEqual(uri, doc["uri"].AsUri);
                response.Return(DreamMessage.Ok(doc));
            };
            DreamMessage result = mock.AtLocalMachine.PostAsync().Wait();
            Assert.IsTrue(result.IsSuccessful, "failure in service");
            Assert.AreEqual(mock.AtLocalHost.Uri.WithoutQuery(), result.ToDocument()["uri"].AsUri);
        }

        [Test]
        public void Filter() {
            XDoc doc = new XDoc("test").Elem("a", "1").Elem("a", "2").Elem("a", "3").Elem("a", "4").Elem("a", "5");
            doc.Filter(delegate(XDoc item) { return (item.AsInt ?? 0) % 2 == 0; });
            string text = doc.ToString();
            Assert.AreEqual("<test><a>2</a><a>4</a></test>", text);
        }

        [Test]
        public void Sort() {
            XDoc doc = new XDoc("test").Elem("a", "4").Elem("a", "2").Elem("a", "1").Elem("a", "3").Elem("a", "5");
            doc.Sort(delegate(XDoc left, XDoc right) { return (left.AsInt ?? 0) - (right.AsInt ?? 0); });
            string text = doc.ToString();
            Assert.AreEqual("<test><a>1</a><a>2</a><a>3</a><a>4</a><a>5</a></test>", text);
        }

        [Test]
        public void Replace1() {
            XDoc doc = new XDoc("test").Start("aaa").Value("1").End().Start("bbb").Value("2").End().Start("ccc").Value("3").End();
            doc["bbb"].ReplaceValue("0");
            Assert.AreEqual("0", doc["bbb"].Contents);
        }

        [Test]
        public void Replace2() {
            XDoc doc = new XDoc("test").Start("aaa").Value("1").End().Start("bbb").Value("2").End().Start("ccc").Value("3").End();
            doc["bbb"].Replace("empty");
            Assert.AreEqual("<test><aaa>1</aaa>empty<ccc>3</ccc></test>", doc.ToString());
        }

        [Test]
        public void Replace3() {
            XDoc doc = new XDoc("test").Start("aaa").Value("1").End().Start("bbb").Value("2").End().Start("ccc").Value("3").End();
            doc["bbb"].Replace(new XDoc("ddd").Value("0"));
            Assert.AreEqual("<test><aaa>1</aaa><ddd>0</ddd><ccc>3</ccc></test>", doc.ToString());
        }

        [Test]
        public void Replace4() {
            XDoc doc = new XDoc("test").Start("aaa").Value("1").End().Start("bbb").Value("2").End().Start("ccc").Value("3").End();
            object value = "empty";
            doc["bbb"].Replace(value);
            Assert.AreEqual("<test><aaa>1</aaa>empty<ccc>3</ccc></test>", doc.ToString());
        }

        [Test]
        public void Replace5() {
            XDoc doc = new XDoc("test").Start("aaa").Value("1").End().Start("bbb").Value("2").End().Start("ccc").Value("3").End();
            object value = new XDoc("ddd").Value("0");
            doc["bbb"].Replace(value);
            Assert.AreEqual("<test><aaa>1</aaa><ddd>0</ddd><ccc>3</ccc></test>", doc.ToString());
        }

        [Test]
        public void Replace6() {
            XDoc doc = new XDoc("test").Start("aaa").Value("1").End().Start("bbb").Value("2").End().Start("ccc").Value("3").End();
            object value = new XDoc("ddd").Value("0");
            doc.Replace(value);
            Assert.AreEqual("<ddd>0</ddd>", doc.ToString());
        }

        [Test]
        public void Rename1() {
            XDoc doc = new XDoc("test").Attr("attr", 1).Start("aaa").Value("1").End().Start("bbb").Attr("attr", 2).Value("2").End().Start("ccc").Value("3").End();
            doc["bbb"].Rename("foo");
            Assert.AreEqual("<test attr=\"1\"><aaa>1</aaa><foo attr=\"2\">2</foo><ccc>3</ccc></test>", doc.ToString());
        }

        [Test]
        public void Rename2() {
            XDoc doc = new XDoc("test").Attr("attr", 1).Start("aaa").Value("1").End().Start("bbb").Attr("attr", 2).Value("2").End().Start("ccc").Value("3").End();
            doc.Rename("foo");
            Assert.AreEqual("<foo attr=\"1\"><aaa>1</aaa><bbb attr=\"2\">2</bbb><ccc>3</ccc></foo>", doc.ToString());
        }

        [Test]
        public void InFrontNodes() {
            XDoc doc = new XDoc("test").Attr("attr", 1).Start("aaa").Value("1").End();
            doc.AddNodesInFront(new XDoc("bbb").Attr("attr", 2).Value("start").Elem("ccc", "inner").Value("end"));
            Assert.AreEqual("<test attr=\"1\">start<ccc>inner</ccc>end<aaa>1</aaa></test>", doc.ToString());
        }

        [Test]
        public void InsertNodes() {
            XDoc doc = new XDoc("test").Attr("attr", 1).Start("aaa").Value("1").End();
            doc.AddNodes(new XDoc("bbb").Attr("attr", 2).Value("start").Elem("ccc", "inner").Value("end"));
            Assert.AreEqual("<test attr=\"1\"><aaa>1</aaa>start<ccc>inner</ccc>end</test>", doc.ToString());
        }

        [Test]
        public void AppendNodes() {
            XDoc doc = new XDoc("test").Attr("attr", 1).Elem("aaa", "1").Elem("bbb", "2").Elem("ccc", "3");
            doc["bbb"].AddNodesAfter(new XDoc("zzz").Attr("attr", 2).Value("start").Elem("xxx", "inner").Value("end"));
            Assert.AreEqual("<test attr=\"1\"><aaa>1</aaa><bbb>2</bbb>start<xxx>inner</xxx>end<ccc>3</ccc></test>", doc.ToString());
        }

        [Test]
        public void AppendText() {
            XDoc doc = new XDoc("test").Attr("attr", 1).Elem("aaa", "1").Elem("bbb", "2").Elem("ccc", "3");
            doc["bbb"].AddAfter("zzz");
            Assert.AreEqual("<test attr=\"1\"><aaa>1</aaa><bbb>2</bbb>zzz<ccc>3</ccc></test>", doc.ToString());
        }

        [Test]
        public void PrependNodes() {
            XDoc doc = new XDoc("test").Attr("attr", 1).Elem("aaa", "1").Elem("bbb", "2").Elem("ccc", "3");
            doc["bbb"].AddNodesBefore(new XDoc("bbb").Attr("attr", 2).Value("start").Elem("xxx", "inner").Value("end"));
            Assert.AreEqual("<test attr=\"1\"><aaa>1</aaa>start<xxx>inner</xxx>end<bbb>2</bbb><ccc>3</ccc></test>", doc.ToString());
        }


        [Test]
        public void PrependText() {
            XDoc doc = new XDoc("test").Attr("attr", 1).Elem("aaa", "1").Elem("bbb", "2").Elem("ccc", "3");
            doc["bbb"].AddBefore("zzz");
            Assert.AreEqual("<test attr=\"1\"><aaa>1</aaa>zzz<bbb>2</bbb><ccc>3</ccc></test>", doc.ToString());
        }
        [Test]
        public void Parent1() {
            XDoc doc = new XDoc("test").Start("aaa").Value("1").End();
            Assert.IsTrue(doc.Parent.IsEmpty);
        }

        [Test]
        public void Parent2() {
            XDoc doc = new XDoc("test").Start("aaa").Value("1").End();
            doc["aaa"].Parent.Attr("attr", 1);
            Assert.AreEqual("<test attr=\"1\"><aaa>1</aaa></test>", doc.ToString());
        }

        [Test]
        public void Parent3() {
            XDoc doc = new XDoc("test").Start("aaa").Value("start").Elem("bbb").End();
            doc["aaa/bbb"].Parent.Value("end");
            Assert.AreEqual("<test><aaa>start<bbb />end</aaa></test>", doc.ToString());
        }
        [Test]
        public void ElementWithNamespace() {
            string namespaceUri = "http://purl.org/dc/elements/1.1/";
            XDoc doc = new XDoc("test");
            doc.UsePrefix("dc", namespaceUri);
            doc.Attr("xmlns:dc", namespaceUri);
            doc.Elem("dc:creator", "test");
            Assert.AreEqual(string.Format("<test xmlns:dc=\"{0}\"><dc:creator>test</dc:creator></test>", namespaceUri), doc.ToString());
        }
        [Test]
        public void AttributeWithNamespace() {
            string namespaceUri = "http://purl.org/dc/elements/1.1/";
            XDoc doc = new XDoc("test");
            doc.UsePrefix("dc", namespaceUri);
            doc.Attr("xmlns:dc", namespaceUri);
            doc.Start("creator").Attr("dc:test", namespaceUri).Value("test").End();
            Assert.AreEqual(string.Format("<test xmlns:dc=\"{0}\"><creator dc:test=\"{0}\">test</creator></test>", namespaceUri), doc.ToString());
        }

        [Test]
        public void RemoveAll() {
            XDoc actual = new XDoc("test").Attr("attr", 1).Start("a").Attr("attr", 2).Start("aa").Attr("attr", 3).Attr("other", 4).Value("test").End().End();
            XDoc expected = new XDoc("test").Start("a").Start("aa").Attr("other", 4).Value("test").End().End();
            actual["//@attr"].RemoveAll();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void DocumentHashcodes() {
            XDoc x = new XDoc("foo")
                .Elem("a", "1")
                .Elem("b", "2")
                .Elem("a", "3");
            Assert.AreEqual(1, x["dfdfdf"].GetHashCode());
            List<XDoc> a = x["a"].ToList();
            Assert.AreEqual(2, a.Count);
            Assert.AreNotEqual(x.GetHashCode(), a[0].GetHashCode());
            Assert.AreEqual(a[0].GetHashCode(), a[1].GetHashCode());
        }

        [Test]
        public void DocumentEquality() {
            XDoc x = new XDoc("x").Elem("a", "1").Elem("b", "2");
            XDoc y = new XDoc("x").Elem("a", "1").Elem("b", "2");
            XDoc z = new XDoc("x").Elem("a", "1").Elem("b", "3");
            XDoc null1 = null;
            XDoc null2 = null;
            Assert.IsTrue(x.Equals(y));
            Assert.IsFalse(x.Equals(z));
            Assert.IsTrue(x == y);
            Assert.IsFalse(x == z);
            Assert.IsTrue(x != z);
            Assert.IsFalse(x != y);
            Assert.IsFalse(x.Equals(null1));
            Assert.IsFalse(x == null1);
            Assert.IsFalse(null1 == x);
            Assert.IsTrue(x != null1);
            Assert.IsTrue(null1 != x);
            Assert.IsTrue(null1 == null2);
            Assert.IsTrue(null1 == null);
            Assert.IsFalse(null1 != null2);
            Assert.IsFalse(null1 != null);
        }

        [Test]
        public void Namespaces() {
            XDoc doc = new XDoc("foo", "#foo");
            doc.UsePrefix("bar", "#bar");
            doc.Start("bar:bar").Attr("bar:attr1", "bar").Attr("attr2", "none").Attr("foo:attr3", "foo").End();
            doc.Elem("none", "none");
            Assert.AreEqual("<foo xmlns=\"#foo\"><bar:bar bar:attr1=\"bar\" attr2=\"none\" attr3=\"foo\" xmlns:bar=\"#bar\" /><none>none</none></foo>", doc.ToString());
        }

        [Test]
        public void ParseCDataToXml1() {
            string text = @"<html><body><script type=""text/javascript"">/*<![CDATA[*/var test = '<div>""test""</div>';/*]]>*/</script><p>test</p></body></html>";
            XDoc doc = XDocFactory.From(text, MimeType.HTML);

            Assert.AreEqual(@"<html><body><script type=""text/javascript""><![CDATA[var test = '<div>""test""</div>';]]></script><p>test</p></body></html>", doc.ToString());
        }

        [Test]
        public void ParseCDataToXml2() {
            string text = @"<html><body><script type=""text/javascript"">var test = '<div>""test""</div>';</script><p>test</p></body></html>";
            XDoc doc = XDocFactory.From(text, MimeType.HTML);

            Assert.AreEqual(@"<html><body><script type=""text/javascript""><![CDATA[var test = '<div>""test""</div>';]]></script><p>test</p></body></html>", doc.ToString());
        }

        [Test]
        public void ParseCDataToXhtml() {
            string text = @"<html><body><script type=""text/javascript"">/*<![CDATA[*/var test = '<div>""test""</div>';/*]]>*/</script><p>test</p></body></html>";
            XDoc doc = XDocFactory.From(text, MimeType.HTML);

            Assert.AreEqual(@"<html><body><script type=""text/javascript"">/*<![CDATA[*/var test = '<div>""test""</div>';/*]]>*/</script><p>test</p></body></html>", doc.ToXHtml());
        }

        [Test]
        public void ToKeyValuePairs() {
            XDoc doc = new XDoc("root");
            doc.Start("key1").Value("val1").End()
                .Elem("key2")
                .Start("key3").Value(string.Empty).End();
            KeyValuePair<string, string>[] pairs = doc.ToKeyValuePairs();
            Assert.IsTrue(Array.Exists<KeyValuePair<string, string>>(pairs, delegate(KeyValuePair<string, string> pair) { return pair.Key == "key1" && pair.Value == "val1"; }));
            Assert.IsTrue(Array.Exists<KeyValuePair<string, string>>(pairs, delegate(KeyValuePair<string, string> pair) { return pair.Key == "key2" && pair.Value == null; }));
            Assert.IsTrue(Array.Exists<KeyValuePair<string, string>>(pairs, delegate(KeyValuePair<string, string> pair) { return pair.Key == "key3" && pair.Value == string.Empty; }));
        }

        [Test]
        public void ToBytesVsWriteTo() {
            XDoc doc = new XDoc("test").Attr("attr", 1).Elem("aaa", "1").Elem("bbb", "2").Elem("ccc", "3");
            byte[] toBytes = doc.ToBytes();
            MemoryStream mem = new MemoryStream();
            doc.WriteTo(mem);
            byte[] writeTo = mem.ToArray();
            Assert.AreEqual(toBytes, writeTo);
        }

        [Test]
        public void VisitAll_select_nothing() {
            XDoc doc = new XDoc("test").Attr("attr", 1).Start("a").Attr("attr", 2).Start("aa").Attr("attr", 3).Attr("other", 4).Value("test").End().End();
            var count = (from x in doc["foo"].VisitAll() where x.HasName("#text") select x).Count();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void VisitAll_select_first_text_node() {
            XDoc doc = new XDoc("test").Attr("attr", 1).Start("a").Attr("attr", 2).Start("aa").Attr("attr", 3).Attr("other", 4).Value("test").End().End();
            var first = (from x in doc.VisitAll() where x.HasName("#text") select x).First();

            Assert.AreEqual("test", first.AsText);
        }

        [Test]
        public void VisitAll_select_nodes_with_other_attr() {
            XDoc doc = new XDoc("test").Attr("attr", 1).Start("a").Attr("attr", 2).Start("aa").Attr("attr", 3).Attr("other", 4).Value("test").End().End();
            var selection = (from x in doc.VisitAll() where x.HasAttr("other") select x);

            Assert.AreEqual(1, selection.Count());
            Assert.AreEqual("aa", selection.First().Name);
        }

        [Test]
        public void VisitOnly_select_nodes_with_other_attr() {
            XDoc doc = new XDoc("test").Attr("attr", 1).Start("a").Attr("attr", 2).Start("aa").Attr("attr", 3).Attr("other", 4).Value("test").End().End();
            var selection = (from x in doc.VisitOnly(x => x.HasName("test")) where x.HasAttr("attr") select x);

            Assert.AreEqual(2, selection.Count());
        }

        [Test]
        public void VisitAll_with_end_element_callback_calls_on_element_exit() {
            var doc = new XDoc("a").Start("b").Start("c").Elem("d").End().Elem("e").End().Elem("f");
            var builder = new StringBuilder();
            foreach(var node in doc.VisitAll(x => builder.Append(x.Name + "<"))) {
                builder.Append(">" + node.Name);
            }
            Assert.AreEqual(">a>b>c>dd<c<>ee<b<>ff<a<", builder.ToString());
        }

        [Test]
        public void VisitAll_with_end_element_callback_calls_on_element_exit_including_text_nodes() {
            var doc = new XDoc("a").Start("b").Elem("c", "Ct").Start("e").Attr("x", "y").Value("Et").End().End().Elem("f");
            var builder = new StringBuilder();
            foreach(var node in doc.VisitAll(x => builder.Append((x.IsText ? x.AsText : x.Name) + "<"))) {
                builder.Append(">" + (node.IsText ? node.AsText : node.Name));
            }
            Assert.AreEqual(">a>b>c>CtCt<c<>e>EtEt<e<b<>ff<a<", builder.ToString());
        }

        [Test]
        public void VisitAll_with_end_element_callback_calls_on_element_exit_including_Comment() {
            var doc = new XDoc("a").Start("b").Comment("foo").Elem("e").End().Elem("f");
            var builder = new StringBuilder();
            foreach(var node in doc.VisitAll(x => builder.Append((x.AsXmlNode is XmlComment ? "#" + x.AsXmlNode.Value : x.Name) + "<"))) {
                builder.Append(">" + (node.AsXmlNode is XmlComment ? "#" + node.AsXmlNode.Value : node.Name));
            }
            Assert.AreEqual(">a>b>#foo#foo<>ee<b<>ff<a<", builder.ToString());
        }

        [Test]
        public void VisitOnly_with_end_element_callback_only_calls_on_element_exit_of_visited_elements() {
            var doc = new XDoc("a").Start("b").Start("c").Elem("d").End().Elem("e").End().Elem("f");
            var builder = new StringBuilder();
            foreach(var node in doc.VisitOnly(x => x.Name != "c", x => builder.Append(x.Name + "<"))) {
                builder.Append(">" + node.Name);
            }
            Assert.AreEqual(">a>b>cc<>ee<b<>ff<a<", builder.ToString());
        }

        [Test]
        public void EnumerateChildrenPredicate_gets_called_before_parent_is_enumerated() {
            var doc = new XDoc("a").Start("b").Start("c").Elem("d").End().Elem("e").End().Elem("f");
            XmlNode current = null;
            foreach(var node in doc.VisitOnly(x => { current = x.AsXmlNode; return true; })) {
                Assert.AreEqual(current, node.AsXmlNode);
            }
        }

        [Test]
        public void VisitAll_stays_in_current_selection() {
            var doc = new XDoc("a").Start("b").Start("c").Elem("d").End().Elem("e").End().Elem("f");
            var builder = new StringBuilder();
            var subdoc = doc["b"];
            foreach(var node in subdoc.VisitAll(x => builder.Append(x.Name + "<"))) {
                builder.Append(">" + node.Name);
            }
            Assert.AreEqual(">b>c>dd<c<>ee<b<", builder.ToString());
        }

        [Test]
        public void Can_return_to_marker_node() {
            var doc = new XDoc("test")
                .Start("a");
            var marker = doc.AsXmlNode;
            doc.Start("b").Start("c").Start("d");
            Assert.AreEqual("d", doc.Name);
            doc.End(marker);
            Assert.AreEqual("a", doc.Name);
        }

        [Test]
        public void Marker_throws_at_document_root() {
            var doc = new XDoc("test")
                .Start("a");
            doc.Start("b").Start("c").Start("d");
            var marker = doc.AsXmlNode;
            doc.End();
            Assert.AreEqual("c", doc.Name);
            try {
                doc.End(marker);
            } catch(InvalidOperationException e) {
                if(e.Message.EqualsInvariant("xdoc is at root position")) {
                    return;
                }
                Assert.Fail("threw wrong InvalidOperationException:" + e.Message);
            }
            Assert.Fail("should have thrown an InvalidOperationException.");
        }

        [Test]
        public void AsText_on_whitespace_should_return_value() {
            var document = new XmlDocument();
            document.AppendChild(document.CreateElement("test"));
            var x = document.CreateElement("x");
            document.DocumentElement.AppendChild(x);
            x.AppendChild(document.CreateWhitespace(" "));
            var doc = new XDoc(document);
            Assert.AreEqual(" ", doc["x"][0].AsText);
        }

        [Test]
        public void AsText_on_significant_whitespace_should_return_value() {
            var document = new XmlDocument();
            document.AppendChild(document.CreateElement("test"));
            var x = document.CreateElement("x");
            document.DocumentElement.AppendChild(x);
            x.AppendChild(document.CreateSignificantWhitespace(" "));
            var doc = new XDoc(document);
            Assert.AreEqual(" ", doc["x"][0].AsText);
        }

        [Test]
        public void AsText_on_cdata_should_return_value() {
            var document = new XmlDocument();
            document.AppendChild(document.CreateElement("test"));
            var x = document.CreateElement("x");
            document.DocumentElement.AppendChild(x);
            x.AppendChild(document.CreateCDataSection("blah"));
            var doc = new XDoc(document);
            Assert.AreEqual("blah", doc["x"][0].AsText);
        }

        [Test]
        public void AsText_on_text_should_return_value() {
            var document = new XmlDocument();
            document.AppendChild(document.CreateElement("test"));
            var x = document.CreateElement("x");
            document.DocumentElement.AppendChild(x);
            x.AppendChild(document.CreateTextNode("blah"));
            var doc = new XDoc(document);
            Assert.AreEqual("blah", doc["x"][0].AsText);
        }

        [Test]
        public void AsText_on_element_with_one_whitespace_node_should_return_value() {
            var document = new XmlDocument();
            document.AppendChild(document.CreateElement("test"));
            var x = document.CreateElement("x");
            document.DocumentElement.AppendChild(x);
            x.AppendChild(document.CreateWhitespace(" "));
            var doc = new XDoc(document);
            Assert.AreEqual(" ", doc["x"].AsText);
        }

        [Test]
        public void AsText_on_element_with_one_significant_whitespace_node_should_return_value() {
            var document = new XmlDocument();
            document.AppendChild(document.CreateElement("test"));
            var x = document.CreateElement("x");
            document.DocumentElement.AppendChild(x);
            x.AppendChild(document.CreateSignificantWhitespace(" "));
            var doc = new XDoc(document);
            Assert.AreEqual(" ", doc["x"].AsText);
        }

        [Test]
        public void AsText_on_element_with_one_cdata_node_should_return_value() {
            var document = new XmlDocument();
            document.AppendChild(document.CreateElement("test"));
            var x = document.CreateElement("x");
            document.DocumentElement.AppendChild(x);
            x.AppendChild(document.CreateCDataSection("blah"));
            var doc = new XDoc(document);
            Assert.AreEqual("blah", doc["x"].AsText);
        }

        [Test]
        public void AsText_on_element_with_one_text_node_should_return_value() {
            var document = new XmlDocument();
            document.AppendChild(document.CreateElement("test"));
            var x = document.CreateElement("x");
            document.DocumentElement.AppendChild(x);
            x.AppendChild(document.CreateTextNode("blah"));
            var doc = new XDoc(document);
            Assert.AreEqual("blah", doc["x"].AsText);
        }

        [Test]
        public void AsText_on_element_concats_whitespace_text_significant_whitespace_and_CDATA() {
            var document = new XmlDocument();
            document.AppendChild(document.CreateElement("test"));
            var x = document.CreateElement("x");
            document.DocumentElement.AppendChild(x);
            x.AppendChild(document.CreateTextNode("foo"));
            x.AppendChild(document.CreateWhitespace(" "));
            x.AppendChild(document.CreateCDataSection("bar"));
            x.AppendChild(document.CreateSignificantWhitespace(" "));
            var doc = new XDoc(document);
            Assert.AreEqual("foo bar ", doc["x"].AsText);
        }


        [Test]
        public void AsText_on_attribute_should_return_value() {
            var doc = new XDoc("test").Attr("x", "blah");
            Assert.AreEqual("blah", doc["@x"].AsText);
        }

        [Test]
        public void Can_define_and_retrieve_CDATA() {
            var doc = new XDoc("test").Start("x").CDataSection("blah").End();
            Assert.AreEqual("blah", doc["x"][0].AsText);
        }

        [Test]
        public void Can_use_document_root_as_marker() {
            var doc = new XDoc("test");
            var marker = doc.AsXmlNode;
            doc.Start("a").Start("b").Start("c").Start("d");
            Assert.AreEqual("d", doc.Name);
            doc.End(marker);
            Assert.AreEqual("test", doc.Name);
        }

        [Test]
        public void New_empty_selection() {
            var docs = XDoc.CreateSelection();

            Assert.IsTrue(docs.IsEmpty);
        }

        [Test]
        public void New_selection_of_two_documents() {
            var docs = XDoc.CreateSelection(new XDoc("foo"), new XDoc("bar"));

            Assert.AreEqual(2, docs.ListLength);

            var enumerable = (IEnumerable<XDoc>)docs;
            Assert.AreEqual(2, docs.Count());
            Assert.AreEqual("foo", enumerable.First().Name);
            Assert.AreEqual("bar", enumerable.Skip(1).First().Name);
        }

        [Test]
        public void Constructor_with_namespace_and_no_prefix() {
            var doc = new XDoc(null, "foo", "namespace");

            Assert.AreEqual(string.Empty, doc.Prefix);
            Assert.AreEqual("foo", doc.Name);
            Assert.AreEqual("namespace", doc.NamespaceURI);
            Assert.AreEqual("<foo xmlns=\"namespace\" />", doc.ToString());
        }

        [Test]
        public void Constructor_with_namespace_and_prefix() {
            var doc = new XDoc("prefix", "foo", "namespace");

            Assert.AreEqual("prefix", doc.Prefix);
            Assert.AreEqual("foo", doc.Name);
            Assert.AreEqual("namespace", doc.NamespaceURI);
            Assert.AreEqual("<prefix:foo xmlns:prefix=\"namespace\" />", doc.ToString());
        }

        [Test]
        public void Constructor_copy() {
            var foo = new XDoc("foo");
            var bar = new XDoc(foo);

            Assert.AreEqual("foo", bar.Name);
        }

        [Test]
        public void ListLength_document_element_only() {
            var doc = new XDoc("foo");

            Assert.AreEqual(1, doc.ListLength);
        }

        [Test]
        public void FirstNext() {
            var docs = XDoc.CreateSelection(new XDoc("foo"), new XDoc("bar"));

            Assert.AreEqual("bar", docs.Next.Name);
            Assert.AreEqual("foo", docs.Next.First.Name);
        }

        [Test]
        public void AsInnerText_on_empty() {
            Assert.IsNull(XDoc.Empty.AsInnerText);
        }

        [Test]
        public void AttributeMustBeEncodedAsInvariant(){
            System.Globalization.CultureInfo old = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("FR-fr");
            XDoc result = new XDoc("test").Attr("testdouble",(double)1.5).Attr("testfloat",(float)1.15);
            Assert.AreEqual(1.5,result["@testdouble"].AsDouble);
            Assert.AreEqual(1.15F,result["@testfloat"].AsFloat);
            System.Threading.Thread.CurrentThread.CurrentCulture = old;
        }

        [TearDown]
        public void TearDown() {
        }

        void Test(string description, string received, string expected) {
            Assert.AreEqual(expected, received, description + "\nreceived:\t" + received + "\nexpected:\t" + expected);
        }
    }
}