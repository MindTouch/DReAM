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
using System.Collections.Generic;
using MindTouch.Dream.Test;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using NUnit.Framework;

namespace System.Collections.TimedAccumulatorTests {

    [TestFixture]
    public class Enqueue {

        //--- Methods ---

        [Test]
        public void Enqueuing_enough_items_dispatches_them() {
            
            // Arrange
            var list = new List<int>();
            var accumulator = new TimedAccumulator<int>(items => {
                lock(list) {
                    list.AddRange(items);
                }
            }, 3, 5.Seconds(), TaskTimerFactory.Default);

            // Act
            accumulator.Enqueue(1);
            accumulator.Enqueue(2);
            accumulator.Enqueue(3);

            // Assert
            Assert.AreEqual(3, list.Count, "accumulated items where not dispatched");
            Assert.AreEqual(1, list[0], "item[0] did not match");
            Assert.AreEqual(2, list[1], "item[1] did not match");
            Assert.AreEqual(3, list[2], "item[2] did not match");
        }

        [Test]
        public void Enqueuing_too_few_items_delays_dispatching_them() {

            // Arrange
            var list = new List<int>();
            var accumulator = new TimedAccumulator<int>(items => {
                lock(list) {
                    list.AddRange(items);
                }
            }, 3, 100.Milliseconds(), TaskTimerFactory.Default);

            // Act
            accumulator.Enqueue(1);

            // Assert
            Assert.IsTrue(Wait.For(() => {
                lock(list) {
                    return list.Count == 1;
                }
            }, 1.Seconds()), "accumulated items where not dispatched");
            Assert.AreEqual(1, list[0], "item[0] did not match");
        }

        [Test]
        public void Enqueuing_too_few_items_delays_dispatching_them_twice_in_a_row() {

            // Arrange
            var list = new List<int>();
            var accumulator = new TimedAccumulator<int>(items => {
                lock(list) {
                    list.AddRange(items);
                }
            }, 3, 100.Milliseconds(), TaskTimerFactory.Default);

            // Act 1
            accumulator.Enqueue(1);

            // Assert 1
            Assert.IsTrue(Wait.For(() => {
                lock(list) {
                    return list.Count == 1;
                }
            }, 1.Seconds()), "accumulated items where not dispatched (act 1)");
            Assert.AreEqual(1, list[0], "item[0] did not match (act 1)");
            list.Clear();

            // Act 2
            accumulator.Enqueue(2);

            // Assert 2
            Assert.IsTrue(Wait.For(() => {
                lock(list) {
                    return list.Count == 1;
                }
            }, 1.Seconds()), "accumulated items where not dispatched (act 2)");
            Assert.AreEqual(2, list[0], "item[0] did not match (act 2)");
        }

        [Test]
        public void Enqueuing_more_than_enough_items_dispatches_them_and_then_waits_to_dispatch_the_rest() {

            // Arrange
            var list = new List<int>();
            var accumulator = new TimedAccumulator<int>(items => {
                lock(list) {
                    list.AddRange(items);
                }
            }, 3, 100.Milliseconds(), TaskTimerFactory.Default);

            // Act
            accumulator.Enqueue(1);
            accumulator.Enqueue(2);
            accumulator.Enqueue(3);
            accumulator.Enqueue(4);
            accumulator.Enqueue(5);

            // Assert
            Assert.AreEqual(3, list.Count, "accumulated items where not dispatched");
            Assert.AreEqual(1, list[0], "item[0] did not match");
            Assert.AreEqual(2, list[1], "item[1] did not match");
            Assert.AreEqual(3, list[2], "item[2] did not match");
            Assert.IsTrue(Wait.For(() => {
                lock(list) {
                    return list.Count == 5;
                }
            }, 1.Seconds()), "accumulated items where not dispatched");
            Assert.AreEqual(4, list[3], "item[3] did not match");
            Assert.AreEqual(5, list[4], "item[4] did not match");
        }
    }
}
