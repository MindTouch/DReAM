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
    public class HasCssClassTests {

        //--- Methods ---

        [Test]
        public void Has_null_css_class_does_nothing() {

            // setup
            var doc = new XDoc("doc").Attr("class", "test");

            // test
            var found = doc.HasCssClass(null);

            // validation
            Assert.IsFalse(found, "css class should not have been found");
        }

        [Test]
        public void Has_empty_css_class_does_nothing() {

            // setup
            var doc = new XDoc("doc").Attr("class", "test");

            // test
            var found = doc.HasCssClass("");

            // validation
            Assert.IsFalse(found, "css class should not have been found");
        }

        [Test]
        public void Has_css_class_without_class_attribute() {

            // setup
            var doc = new XDoc("doc");

            // test
            var found = doc.HasCssClass("test");

            // validation
            Assert.IsFalse(found, "css class should not have been found");
        }

        [Test]
        public void Has_css_class_without_match() {

            // setup
            var doc = new XDoc("div").Attr("class", "foo");

            // test
            var found = doc.HasCssClass("test");

            // validation
            Assert.IsFalse(found, "css class should not have been found");
        }

        [Test]
        public void Has_css_class_matches_entire_value() {

            // setup
            var doc = new XDoc("div").Attr("class", "test");

            // test
            var found = doc.HasCssClass("test");

            // validation
            Assert.IsTrue(found, "css class should have been found");
        }

        [Test]
        public void Has_css_class_matches_start_of_value() {

            // setup
            var doc = new XDoc("div").Attr("class", "test bar");

            // test
            var found = doc.HasCssClass("test");

            // validation
            Assert.IsTrue(found, "css class should have been found");
        }

        [Test]
        public void Has_css_class_matches_end_of_value() {

            // setup
            var doc = new XDoc("div").Attr("class", "foo test");

            // test
            var found = doc.HasCssClass("test");

            // validation
            Assert.IsTrue(found, "css class should have been found");
        }

        [Test]
        public void Has_css_class_matches_inside_value() {

            // setup
            var doc = new XDoc("div").Attr("class", "foo test bar");

            // test
            var found = doc.HasCssClass("test");

            // validation
            Assert.IsTrue(found, "css class should have been found");
        }
    }
}
