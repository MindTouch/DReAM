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

using System.Globalization;

namespace System {

    /// <summary>
    /// Static utility class containing extension and helper methods for working with <see cref="DateTime"/>.
    /// </summary>
    public static class DateTimeUtil {

		//--- Class Fields ---

        /// <summary>
        /// The Unix Epoch time, i.e. seconds since January 1, 1970 (UTC).
        /// </summary>
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        //--- Class Properties ---

        /// <summary>
        /// Get today's date in UTC timezone.
        /// </summary>
        public static DateTime UtcToday {
            get {
                DateTime result = DateTime.Today.ToUniversalTime();
                result = result.AddHours(-result.Hour);
                return result;
            }
        }

        //--- Extension Methods ---

        /// <summary>
        /// Safely get the UTC <see cref="DateTime"/> for a given input.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="DateTime.ToUniversalTime"/>, this method will consider <see cref="DateTimeKind.Unspecified"/> source dates to 
        /// originate in the universal time zone.
        /// </remarks>
        /// <param name="date">Source date</param>
        /// <returns>Date in the UTC timezone.</returns>
        public static DateTime ToSafeUniversalTime(this DateTime date) {
            if(date != DateTime.MinValue && date != DateTime.MaxValue) {
                switch(date.Kind) {
                case DateTimeKind.Unspecified:
                    date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, DateTimeKind.Utc);
                    break;
                case DateTimeKind.Local:
                    date = date.ToUniversalTime();
                    break;
                }
            }
            return date;
        }

        /// <summary>
        /// Remove the millisecond component from a date.
        /// </summary>
        /// <param name="date">Source date.</param>
        /// <returns>DateTime with milliseconds truncated.</returns>
        public static DateTime WithoutMilliseconds(this DateTime date) {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);
        }

        /// <summary>
        /// Remove the millisecond component from a timespan.
        /// </summary>
        /// <param name="timeSpan">Source timeSpan.</param>
        /// <returns>TimeSpan with milliseconds truncated.</returns>
        public static TimeSpan WithoutMilliseconds(this TimeSpan timeSpan) {
            return TimeSpan.FromSeconds(Math.Floor(timeSpan.TotalSeconds));
        }

        /// <summary>
        /// Get the utc-based unix epoch time.
        /// </summary>
        /// <param name="date">Source date.</param>
        /// <returns>Seconds since January 1, 1970 (UTC).</returns>
        public static uint ToEpoch(this DateTime date) {
            return (uint)date.ToSafeUniversalTime().Subtract(Epoch).TotalSeconds;
        }

        //--- Class Methods ---

        /// <summary>
        /// Get a DateTime instance from utc-based unix epoch time.
        /// </summary>
        /// <param name="secondsSinceEpoch">Seconds since January 1, 1970 (UTC).</param>
        /// <returns>DateTime instance.</returns>
        public static DateTime FromEpoch(uint secondsSinceEpoch) {
            return Epoch.AddSeconds(secondsSinceEpoch);
        }

        /// <summary>
        /// Parse a date using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="value">Source datetime string.</param>
        /// <returns>DateTime</returns>
        public static DateTime ParseInvariant(string value) {
            return DateTime.Parse(value, CultureInfo.InvariantCulture.DateTimeFormat);
        }

        /// <summary>
        /// Parse a date using <see cref="CultureInfo.InvariantCulture"/> and an exact date format.
        /// </summary>
        /// <param name="value">Source datetime string.</param>
        /// <param name="format">DateTime format string.</param>
        /// <returns>DateTime</returns>
        public static DateTime ParseExactInvariant(string value, string format) {
            return DateTime.ParseExact(value, format, CultureInfo.InvariantCulture.DateTimeFormat);
        }

        /// <summary>
        /// Try to parse a date using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="value">Source datetime string.</param>
        /// <param name="date">Output location</param>
        /// <returns><see langword="True"/> if a date was successfully parsed.</returns>
        public static bool TryParseInvariant(string value, out DateTime date) {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal, out date);
        }

        /// <summary>
        /// Try to parse a date using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="value">Source datetime string.</param>
        /// <param name="format">DateTime format string.</param>
        /// <param name="date">Output location</param>
        /// <returns><see langword="True"/> if a date was successfully parsed.</returns>
        public static bool TryParseExactInvariant(string value, string format, out DateTime date) {
            return DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal, out date);
        }
    }
}
