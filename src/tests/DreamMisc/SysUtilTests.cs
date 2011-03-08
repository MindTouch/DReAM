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
using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class SysUtilTests {

        public enum Castable {
            V1,
            V2
        }

        [Test]
        public void Can_cast_byte_to_enum() {
            var x = (byte)Castable.V2;
            var y = SysUtil.ChangeType(x, typeof(Castable));
            Assert.AreEqual(Castable.V2, y);
        }

        [Test]
        public void Can_cast_string_to_enum() {
            var x = Castable.V2.ToString();
            var y = SysUtil.ChangeType(x, typeof(Castable));
            Assert.AreEqual(Castable.V2, y);
        }

        [Test]
        public void Can_parse_double_in_german_locale() {

            // set foreign locale
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-de");
            // parse number
            var value = SysUtil.ChangeType<double>(".999");
            Assert.AreEqual(0.999, value);
        }

        [Test]
        public void Can_parse_double_in_us_locale() {

            // set foreign locale
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");

            // parse number
            var value = SysUtil.ChangeType<double>(".999");
            Assert.AreEqual(0.999, value);
        }

        [Test]
        public void Can_parse_float_in_us_locale() {

            // set foreign locale
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");

            // parse number
            var value = SysUtil.ChangeType<float>(".999");
            Assert.AreEqual(0.999f, value);
        }

        [Test]
        public void Can_parse_float_in_german_locale() {

            // set foreign locale
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-de");
            // parse number
            var value = SysUtil.ChangeType<float>(".999");
            Assert.AreEqual(0.999f, value);
        }

        [Test]
        public void Can_parse_decimal_in_german_locale() {

            // set foreign locale
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-de");
            // parse number
            var value = SysUtil.ChangeType<decimal>(".999");
            Assert.AreEqual(0.999d, value);
        }

        [Test]
        public void Can_parse_decimal_in_us_locale() {

            // set foreign locale
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");

            // parse number
            var value = SysUtil.ChangeType<decimal>(".999");
            Assert.AreEqual(0.999d, value);
        }
    }
}
