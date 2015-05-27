/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */

using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouchTest.Xml.XDocUtilTests {

    [TestFixture]
    public class AddCssClassTests {

        //--- Methods ---

        [Test]
        public void Adding_null_css_class_does_nothing() {

            // setup
            var doc = new XDoc("div").Attr("class", "test");

            // test
            doc.AddCssClass(null);

            // validation
            Assert.AreEqual("test", doc["@class"].AsText, "class not set to expected value");
        }

        [Test]
        public void Adding_empty_css_class_does_nothing() {

            // setup
            var doc = new XDoc("div").Attr("class", "test");

            // test
            doc.AddCssClass("");

            // validation
            Assert.AreEqual("test", doc["@class"].AsText, "class not set to expected value");
        }

        [Test]
        public void Adding_css_class_adds_missing_class_attribute() {

            // setup
            var doc = new XDoc("div");

            // test
            doc.AddCssClass("test");

            // validation
            Assert.AreEqual("test", doc["@class"].AsText, "class not set to expected value");
        }

        [Test]
        public void Adding_css_class_appends_to_existing_class_attribute() {

            // setup
            var doc = new XDoc("div").Attr("class", "foo");

            // test
            doc.AddCssClass("test");

            // validation
            Assert.AreEqual("foo test", doc["@class"].AsText, "class not set to expected value");
        }

        [Test]
        public void Adding_duplicate_css_class_does_nothing_when_attribute_is_set_to_same_value() {

            // setup
            var doc = new XDoc("div").Attr("class", "test");

            // test
            doc.AddCssClass("test");

            // validation
            Assert.AreEqual("test", doc["@class"].AsText, "class not set to expected value");
        }

        [Test]
        public void Adding_duplicate_css_class_does_nothing_when_attribute_starts_with_same_value() {

            // setup
            var doc = new XDoc("div").Attr("class", "test bar");

            // test
            doc.AddCssClass("test");

            // validation
            Assert.AreEqual("test bar", doc["@class"].AsText, "class not set to expected value");
        }

        [Test]
        public void Adding_duplicate_css_class_does_nothing_when_attribute_ends_with_same_value() {

            // setup
            var doc = new XDoc("div").Attr("class", "foo test");

            // test
            doc.AddCssClass("test");

            // validation
            Assert.AreEqual("foo test", doc["@class"].AsText, "class not set to expected value");
        }

        [Test]
        public void Adding_duplicate_css_class_does_nothing_when_attribute_contains_same_value() {

            // setup
            var doc = new XDoc("div").Attr("class", "foo test bar");

            // test
            doc.AddCssClass("test");

            // validation
            Assert.AreEqual("foo test bar", doc["@class"].AsText, "class not set to expected value");
        }
    }
}
