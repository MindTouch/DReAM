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

namespace MindTouch.Extensions.Time {

    /// <summary>
    /// Provides extension methods relating to <see cref="TimeSpan"/> and <see cref="DateTime"/>.
    /// </summary>
    public static class TimeEx {

        //--- Extension Methods ---

        /// <summary>
        /// Get a timespan of the hours specified.
        /// </summary>
        /// <param name="hours">Number of hours.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Hours(this int hours) {
            return TimeSpan.FromHours(hours);
        }

        /// <summary>
        /// Get a timespan of the hours specified.
        /// </summary>
        /// <param name="hours">Number of hours.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Hours(this uint hours) {
            return TimeSpan.FromHours(hours);
        }

        /// <summary>
        /// Get a timespan of the hours specified.
        /// </summary>
        /// <param name="hours">Number of hours.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Hours(this long hours) {
            return TimeSpan.FromHours(hours);
        }

        /// <summary>
        /// Get a timespan of the hours specified.
        /// </summary>
        /// <param name="hours">Number of hours.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Hours(this ulong hours) {
            return TimeSpan.FromHours(hours);
        }

        /// <summary>
        /// Get a timespan of the hours specified.
        /// </summary>
        /// <param name="hours">Number of hours.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Hours(this double hours) {
            return TimeSpan.FromHours(hours);
        }

        /// <summary>
        /// Get a timespan of the minutes specified.
        /// </summary>
        /// <param name="minutes">Number of minutes.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Minutes(this int minutes) {
            return TimeSpan.FromMinutes(minutes);
        }

        /// <summary>
        /// Get a timespan of the minutes specified.
        /// </summary>
        /// <param name="minutes">Number of minutes.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Minutes(this uint minutes) {
            return TimeSpan.FromMinutes(minutes);
        }

        /// <summary>
        /// Get a timespan of the minutes specified.
        /// </summary>
        /// <param name="minutes">Number of minutes.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Minutes(this long minutes) {
            return TimeSpan.FromMinutes(minutes);
        }

        /// <summary>
        /// Get a timespan of the minutes specified.
        /// </summary>
        /// <param name="minutes">Number of minutes.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Minutes(this ulong minutes) {
            return TimeSpan.FromMinutes(minutes);
        }

        /// <summary>
        /// Get a timespan of the minutes specified.
        /// </summary>
        /// <param name="minutes">Number of minutes.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Minutes(this double minutes) {
            return TimeSpan.FromMinutes(minutes);
        }

        /// <summary>
        /// Get a timespan of the seconds specified.
        /// </summary>
        /// <param name="seconds">Number of seconds.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Seconds(this int seconds) {
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// Get a timespan of the seconds specified.
        /// </summary>
        /// <param name="seconds">Number of seconds.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Seconds(this uint seconds) {
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// Get a timespan of the seconds specified.
        /// </summary>
        /// <param name="seconds">Number of seconds.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Seconds(this long seconds) {
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// Get a timespan of the seconds specified.
        /// </summary>
        /// <param name="seconds">Number of seconds.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Seconds(this ulong seconds) {
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// Get a timespan of the seconds specified.
        /// </summary>
        /// <param name="seconds">Number of seconds.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Seconds(this double seconds) {
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// Get a timespan of the milliseconds specified.
        /// </summary>
        /// <param name="milliseconds">Number of milliseconds.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Milliseconds(this int milliseconds) {
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        /// <summary>
        /// Get a timespan of the milliseconds specified.
        /// </summary>
        /// <param name="milliseconds">Number of milliseconds.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Milliseconds(this uint milliseconds) {
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        /// <summary>
        /// Get a timespan of the milliseconds specified.
        /// </summary>
        /// <param name="milliseconds">Number of milliseconds.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Milliseconds(this long milliseconds) {
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        /// <summary>
        /// Get a timespan of the milliseconds specified.
        /// </summary>
        /// <param name="milliseconds">Number of milliseconds.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Milliseconds(this ulong milliseconds) {
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        /// <summary>
        /// Get a timespan of the milliseconds specified.
        /// </summary>
        /// <param name="milliseconds">Number of milliseconds.</param>
        /// <returns>A TimeSpan instance.</returns>
        public static TimeSpan Milliseconds(this double milliseconds) {
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        /// <summary>
        /// Get a DateTime instance from utc-based unix epoch time.
        /// </summary>
        /// <param name="secondsSinceEpoch">Seconds since January 1, 1970 (UTC).</param>
        /// <returns>DateTime instance.</returns>
        public static DateTime Epoch(int secondsSinceEpoch) {
            return DateTimeUtil.Epoch.AddSeconds(secondsSinceEpoch);
        }

        /// <summary>
        /// Get a DateTime instance from utc-based unix epoch time.
        /// </summary>
        /// <param name="secondsSinceEpoch">Seconds since January 1, 1970 (UTC).</param>
        /// <returns>DateTime instance.</returns>
        public static DateTime Epoch(uint secondsSinceEpoch) {
            return DateTimeUtil.Epoch.AddSeconds(secondsSinceEpoch);
        }

        /// <summary>
        /// Get a DateTime instance from utc-based unix epoch time.
        /// </summary>
        /// <param name="secondsSinceEpoch">Seconds since January 1, 1970 (UTC).</param>
        /// <returns>DateTime instance.</returns>
        public static DateTime Epoch(long secondsSinceEpoch) {
            return DateTimeUtil.Epoch.AddSeconds(secondsSinceEpoch);
        }

        /// <summary>
        /// Get a DateTime instance from utc-based unix epoch time.
        /// </summary>
        /// <param name="secondsSinceEpoch">Seconds since January 1, 1970 (UTC).</param>
        /// <returns>DateTime instance.</returns>
        public static DateTime Epoch(ulong secondsSinceEpoch) {
            return DateTimeUtil.Epoch.AddSeconds(secondsSinceEpoch);
        }

        /// <summary>
        /// Get a DateTime instance from utc-based unix epoch time.
        /// </summary>
        /// <param name="secondsSinceEpoch">Seconds since January 1, 1970 (UTC).</param>
        /// <returns>DateTime instance.</returns>
        public static DateTime Epoch(double secondsSinceEpoch) {
            return DateTimeUtil.Epoch.AddSeconds(secondsSinceEpoch);
        }

    }
}
