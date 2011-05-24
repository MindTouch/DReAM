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

using NUnit.Framework;

namespace MindTouch.Dream.Test {
    
    [TestFixture]
    public class CultureUtilTests {
        private CultureInfo _default = CultureInfo.GetCultureInfo("de-de");

        [Test]
        public void Should_get_default_on_null_culture() {
            Assert.AreEqual(_default,CultureUtil.GetNonNeutralCulture(null,_default));
        }

        [Test]
        public void Should_get_null_from_null_language_string() {
            Assert.IsNull(CultureUtil.GetNonNeutralCulture(null));
        }

        [Test]
        public void Should_get_null_from_empty_language_string() {
            Assert.IsNull(CultureUtil.GetNonNeutralCulture(string.Empty));
        }

        [Test]
        public void Should_get_en_us_from_en() {
            Assert.AreEqual(CultureInfo.GetCultureInfo("en-us"),CultureUtil.GetNonNeutralCulture(CultureInfo.GetCultureInfo("en"),_default));
        }
    }
}
