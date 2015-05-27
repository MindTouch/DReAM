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
    public class RemoveCssClassTests {

        //--- Methods ---

        [Test]
        public void Removing_null_css_class_does_nothing() {

            // setup
            var doc = new XDoc("div").Attr("class", "test");

            // test
            doc.RemoveCssClass(null);

            // validation
            Assert.AreEqual("test", doc["@class"].AsText, "class not set to expected value");
        }

        [Test]
        public void Removing_empty_css_class_does_nothing() {

            // setup
            var doc = new XDoc("div").Attr("class", "test");

            // test
            doc.AddCssClass("");

            // validation
            Assert.AreEqual("test", doc["@class"].AsText, "class not set to expected value");
        }

        [Test]
        public void Removing_css_class_removes_empty_class_attribute() {

            // setup
            var doc = new XDoc("div").Attr("class", "test");

            // test
            doc.RemoveCssClass("test");

            // validation
            Assert.IsFalse(doc.HasAttr("class"), "class attribute was not removed");
        }

        [Test]
        public void Removing_duplicate_css_class_removes_empty_class_attribute() {

            // setup
            var doc = new XDoc("div").Attr("class", "test test");

            // test
            doc.RemoveCssClass("test");

            // validation
            Assert.IsFalse(doc.HasAttr("class"), "class attribute was not removed");
        }

        [Test]
        public void Removing_css_class_updates_class_attribute_when_attribute_starts_with_same_value() {

            // setup
            var doc = new XDoc("div").Attr("class", "test bar");

            // test
            doc.RemoveCssClass("test");

            // validation
            Assert.AreEqual("bar", doc["@class"].AsText, "class not set to expected value");
        }

        [Test]
        public void Removing_css_class_updates_class_attribute_when_attribute_ends_with_same_value() {

            // setup
            var doc = new XDoc("div").Attr("class", "foo test");

            // test
            doc.RemoveCssClass("test");

            // validation
            Assert.AreEqual("foo", doc["@class"].AsText, "class not set to expected value");
        }

        [Test]
        public void Removing_css_class_updates_class_attribute_when_attribute_contains_same_value() {

            // setup
            var doc = new XDoc("div").Attr("class", "foo test bar");

            // test
            doc.RemoveCssClass("test");

            // validation
            Assert.AreEqual("foo bar", doc["@class"].AsText, "class not set to expected value");
        }

        [Test]
        public void Removing_duplicate_css_class_removes_all_occurrences() {

            // setup
            var doc = new XDoc("div").Attr("class", "test foo test bar test");

            // test
            doc.RemoveCssClass("test");

            // validation
            Assert.AreEqual("foo bar", doc["@class"].AsText, "class not set to expected value");
        }
    }
}
