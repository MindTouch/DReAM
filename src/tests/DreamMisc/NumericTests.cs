/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
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
using System.Linq;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class NumericTests {

        [Test]
        public void ToInt_for_large_uint_gets_capped_at_max_value() {
            var large = (uint)int.MaxValue + 1000;
            Assert.Greater(large, int.MaxValue);
            Assert.AreEqual(int.MaxValue, large.ToInt());
        }

        [Test]
        public void ToInt_for_large_long_gets_capped_at_max_value() {
            var large = (long)int.MaxValue + 1000;
            Assert.Greater(large, int.MaxValue);
            Assert.AreEqual(int.MaxValue, large.ToInt());
        }

        [Test]
        public void ToInt_for_small_long_gets_capped_at_min_value() {
            var small = (long)int.MinValue - 1000;
            Assert.Less(small, int.MaxValue);
            Assert.AreEqual(int.MinValue, small.ToInt());
        }

        [Test]
        public void ToInt_for_large_ulong_gets_capped_at_max_value() {
            var large = (ulong)int.MaxValue + 1000;
            Assert.Greater(large, int.MaxValue);
            Assert.AreEqual(int.MaxValue, large.ToInt());
        }

        [Test]
        public void ToUInt_for_large_ulong_gets_capped_at_max_value() {
            var large = (ulong)uint.MaxValue + 1000;
            Assert.Greater(large, uint.MaxValue);
            Assert.AreEqual(uint.MaxValue, large.ToUInt());
        }

        [Test]
        public void SafeAdd_of_overflowing_result_caps_at_max_value() {
            var a = int.MaxValue - 1000;
            var b = 10000;
            Assert.AreEqual(int.MaxValue, a.SafeAdd(b));
        }

        [Test]
        public void SafeAdd_of_underflowing_result_caps_at_min_value() {
            var a = int.MinValue + 1000;
            var b = -10000;
            Assert.AreEqual(int.MinValue, a.SafeAdd(b));
        }

        [Test]
        public void ToCommaDelimitedString_of_empty_int_sequence_returns_null() {
            var s = new int[0];
            Assert.IsNull(s.ToCommaDelimitedString());
        }

        [Test]
        public void Can_convert_int_sequence_to_comma_delimited_string() {
            var s = new int[] { 1, -2, 3 };
            Assert.AreEqual("1,-2,3", s.ToCommaDelimitedString());
        }

        [Test]
        public void ToCommaDelimitedString_of_empty_uint_sequence_returns_null() {
            var s = new uint[0];
            Assert.IsNull(s.ToCommaDelimitedString());
        }

        [Test]
        public void Can_convert_uint_sequence_to_comma_delimited_string() {
            var s = new uint[] { 1, 2, 3 };
            Assert.AreEqual("1,2,3", s.ToCommaDelimitedString());
        }

        [Test]
        public void ToCommaDelimitedString_of_empty_long_sequence_returns_null() {
            var s = new long[0];
            Assert.IsNull(s.ToCommaDelimitedString());
        }

        [Test]
        public void Can_convert_long_sequence_to_comma_delimited_string() {
            var s = new long[] { 1, -2, 3 };
            Assert.AreEqual("1,-2,3", s.ToCommaDelimitedString());
        }

        [Test]
        public void ToCommaDelimitedString_of_empty_ulong_sequence_returns_null() {
            var s = new ulong[0];
            Assert.IsNull(s.ToCommaDelimitedString());
        }

        [Test]
        public void Can_convert_ulong_sequence_to_comma_delimited_string() {
            var s = new ulong[] { 1, 2, 3 };
            Assert.AreEqual("1,2,3", s.ToCommaDelimitedString());
        }

        [Test]
        public void CommaDelimitedToInt_returns_empty_set_for_null_string() {
            var s = ((string)null).CommaDelimitedToInt();
            Assert.IsNotNull(s);
            Assert.IsFalse(s.Any());
        }

        [Test]
        public void CommaDelimitedToInt_returns_empty_set_for_empty_string() {
            var s = string.Empty.CommaDelimitedToInt();
            Assert.IsNotNull(s);
            Assert.IsFalse(s.Any());
        }

        [Test]
        public void Can_convert_comma_deliminted_string_to_int_sequence() {
            var s = "1,-10,3";
            Assert.AreEqual(new int[] { 1, -10, 3 }, s.CommaDelimitedToInt().ToArray());
        }

        [Test]
        public void CommaDelimitedToUInt_returns_empty_set_for_null_string() {
            var s = ((string)null).CommaDelimitedToUInt();
            Assert.IsNotNull(s);
            Assert.IsFalse(s.Any());
        }

        [Test]
        public void CommaDelimitedToUInt_returns_empty_set_for_empty_string() {
            var s = string.Empty.CommaDelimitedToUInt();
            Assert.IsNotNull(s);
            Assert.IsFalse(s.Any());
        }

        [Test]
        public void Can_convert_comma_deliminted_string_to_uint_sequence() {
            var s = "1,10,3";
            Assert.AreEqual(new uint[] { 1, 10, 3 }, s.CommaDelimitedToUInt().ToArray());
        }

        [Test]
        public void CommaDelimitedToLong_returns_empty_set_for_null_string() {
            var s = ((string)null).CommaDelimitedToLong();
            Assert.IsNotNull(s);
            Assert.IsFalse(s.Any());
        }

        [Test]
        public void CommaDelimitedToLong_returns_empty_set_for_empty_string() {
            var s = string.Empty.CommaDelimitedToLong();
            Assert.IsNotNull(s);
            Assert.IsFalse(s.Any());
        }

        [Test]
        public void Can_convert_comma_deliminted_string_to_long_sequence() {
            var s = "1,-10,3";
            Assert.AreEqual(new long[] { 1, -10, 3 }, s.CommaDelimitedToLong().ToArray());
        }

        [Test]
        public void CommaDelimitedToULong_returns_empty_set_for_null_string() {
            var s = ((string)null).CommaDelimitedToULong();
            Assert.IsNotNull(s);
            Assert.IsFalse(s.Any());
        }

        [Test]
        public void CommaDelimitedToULong_returns_empty_set_for_empty_string() {
            var s = string.Empty.CommaDelimitedToULong();
            Assert.IsNotNull(s);
            Assert.IsFalse(s.Any());
        }

        [Test]
        public void Can_convert_comma_deliminted_string_to_ulong_sequence() {
            var s = "1,10,3";
            Assert.AreEqual(new ulong[] { 1, 10, 3 }, s.CommaDelimitedToULong().ToArray());
        }
    }
}
