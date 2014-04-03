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
using System;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    /// <summary>
    /// Helper methods for Nunit assertions.
    /// </summary>
    public static class AssertEx {

        //--- Class Methods ---

        /// <summary>
        /// Compare two timespans with a minor degree of millisecond fuzzyness.
        /// </summary>
        /// <param name="expected">Expected timespan</param>
        /// <param name="actual">Actual timespan</param>
        public static void AreEqual(TimeSpan expected, TimeSpan actual) {
            AreEqual(expected, actual, null);
        }

        /// <summary>
        /// Compare two timespans with a minor degree of millisecond fuzzyness.
        /// </summary>
        /// <param name="expected">Expected timespan</param>
        /// <param name="actual">Actual timespan</param>
        /// <param name="message">Error message to display should the assertion fail.</param>
        public static void AreEqual(TimeSpan expected, TimeSpan actual, string message) {

            // NOTE (steveb): on Mono, TimeSpan's are not precise enough to use Equal
            const double margin = 0.01;

            if(Math.Abs(expected.TotalSeconds - actual.TotalSeconds) > margin) {
                Assert.AreEqual(expected, actual, message);
            }
        }
    }
}

