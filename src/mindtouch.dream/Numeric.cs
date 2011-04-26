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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MindTouch {
    public static class Numeric {

        //--- Extension Methods ---

        /// <summary>
        /// Safely convert an unsigned integer to an int. If the unsigned integer is greater than int.MaxValue, int.MaxValue is returned.
        /// </summary>
        /// <param name="value">Unsigned integer</param>
        /// <returns>Integer</returns>
        public static int ToInt(this uint value) {
            return (value > int.MaxValue) ? int.MaxValue : (int)value;
        }

        /// <summary>
        /// Safely convert an long integer to an int. If the long integer is greater than int.MaxValue, int.MaxValue is returned.
        /// </summary>
        /// <param name="value">long integer</param>
        /// <returns>Integer</returns>
        public static int ToInt(this long value) {
            return (value > int.MaxValue)
                ? int.MaxValue
                : (value < int.MinValue)
                    ? int.MinValue
                    : (int)value;
        }

        /// <summary>
        /// Safely convert an unsigned long integer to an int. If the unsigned long integer is greater than int.MaxValue, int.MaxValue is returned.
        /// </summary>
        /// <param name="value">Unsigned integer</param>
        /// <returns>Integer</returns>
        public static int ToInt(this ulong value) {
            return (value > int.MaxValue) ? int.MaxValue : (int)value;
        }

        /// <summary>
        /// Safely convert an unsigned long integer to an unsigned integer. If the unsigned long integer is greater than uint.MaxValue, uint.MaxValue is returned.
        /// </summary>
        /// <param name="value">Unsigned long integer</param>
        /// <returns>Unsigned Integer</returns>
        public static uint ToUInt(this ulong value) {
            return (value > uint.MaxValue) ? uint.MaxValue : (uint)value;
        }

        /// <summary>
        /// Safely convert an unsigned long integer to an int. If the unsigned integer is greater than long.MaxValue, long.MaxValue is returned.
        /// </summary>
        /// <param name="value">Unsigned long integer</param>
        /// <returns>Long Integer</returns>
        public static long ToLong(this ulong value) {
            return (value > long.MaxValue) ? long.MaxValue : (long)value;
        }

        /// <summary>
        /// Safely add two integers, remaining bounded by int.MinValue and int.MaxValue.
        /// </summary>
        /// <param name="a">left hand side of addition</param>
        /// <param name="b">right hand side of addtion</param>
        /// <returns>Signed integer</returns>
        public static int SafeAdd(this int a, int b) {
            var x = (long)a + b;
            if(x > int.MaxValue) {
                return int.MaxValue;
            }
            if(x < int.MinValue) {
                return int.MinValue;
            }
            return (int)x;
        }

        /// <summary>
        /// Safely add two unsigned integers, remaining bounded by uint.MinValue and uint.MaxValue.
        /// </summary>
        /// <param name="a">left hand side of addition</param>
        /// <param name="b">right hand side of addtion</param>
        /// <returns>Unsigned integer</returns>
        public static uint SafeAdd(this uint a, uint b) {
            var x = (ulong)a + b;
            if(x > uint.MaxValue) {
                return uint.MaxValue;
            }
            return (uint)x;
        }


        /// <summary>
        /// Convert a sequence of numbers into a commma separated string
        /// </summary>
        /// <param name="ids">Sequence of numbers</param>
        /// <returns>Comma separated string</returns>
        public static string ToCommaDelimitedString(this IEnumerable<int> ids) {
            return CreateCommaDelimitedString(ids);
        }

        /// <summary>
        /// Convert a sequence of numbers into a commma separated string
        /// </summary>
        /// <param name="ids">Sequence of numbers</param>
        /// <returns>Comma separated string</returns>
        public static string ToCommaDelimitedString(this IEnumerable<uint> ids) {
            return CreateCommaDelimitedString(ids);
        }

        /// <summary>
        /// Convert a sequence of numbers into a commma separated string
        /// </summary>
        /// <param name="ids">Sequence of numbers</param>
        /// <returns>Comma separated string</returns>
        public static string ToCommaDelimitedString(this IEnumerable<long> ids) {
            return CreateCommaDelimitedString(ids);
        }

        /// <summary>
        /// Convert a sequence of numbers into a commma separated string
        /// </summary>
        /// <param name="ids">Sequence of numbers</param>
        /// <returns>Comma separated string</returns>
        public static string ToCommaDelimitedString(this IEnumerable<ulong> ids) {
            return CreateCommaDelimitedString(ids);
        }

        // Note (arnec): ToCommaDelimitedString is not generic, since there is no way to add a numeric-only constraint
        private static string CreateCommaDelimitedString<T>(this IEnumerable<T> ids) {
            StringBuilder builder = null;
            foreach(var id in ids) {
                if(builder == null) {
                    builder = new StringBuilder();
                } else {
                    builder.Append(",");
                }
                builder.Append(id);
            }
            return builder == null ? null : builder.ToString();
        }

        /// <summary>
        /// Convert a comma-separated string of numbers into a sequence of numbers.
        /// </summary>
        /// <param name="commaDelimited">comma delimited string</param>
        /// <returns>Sequence of numbers or an empty sequence, if the string was empty</returns>
        public static IEnumerable<uint> CommaDelimitedToUInt(this string commaDelimited) {
            return string.IsNullOrEmpty(commaDelimited)
                ? new uint[0]
                : commaDelimited.Split(',').Select(x => uint.Parse(x)).ToArray();
        }

        /// <summary>
        /// Convert a comma-separated string of numbers into a sequence of numbers.
        /// </summary>
        /// <param name="commaDelimited">comma delimited string</param>
        /// <returns>Sequence of numbers or an empty sequence, if the string was empty</returns>
        public static IEnumerable<int> CommaDelimitedToInt(this string commaDelimited) {
            return string.IsNullOrEmpty(commaDelimited)
                ? new int[0]
                : commaDelimited.Split(',').Select(x => int.Parse(x)).ToArray();
        }

        /// <summary>
        /// Convert a comma-separated string of numbers into a sequence of numbers.
        /// </summary>
        /// <param name="commaDelimited">comma delimited string</param>
        /// <returns>Sequence of numbers or an empty sequence, if the string was empty</returns>
        public static IEnumerable<ulong> CommaDelimitedToULong(this string commaDelimited) {
            return string.IsNullOrEmpty(commaDelimited)
                ? new ulong[0]
                : commaDelimited.Split(',').Select(x => ulong.Parse(x)).ToArray();
        }

        /// <summary>
        /// Convert a comma-separated string of numbers into a sequence of numbers.
        /// </summary>
        /// <param name="commaDelimited">comma delimited string</param>
        /// <returns>Sequence of numbers or an empty sequence, if the string was empty</returns>
        public static IEnumerable<long> CommaDelimitedToLong(this string commaDelimited) {
            return string.IsNullOrEmpty(commaDelimited)
                ? new long[0]
                : commaDelimited.Split(',').Select(x => long.Parse(x)).ToArray();
        }
    }
}
