/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2013 MindTouch, Inc.
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

using MindTouch.Collections;
using NUnit.Framework;

namespace MindTouch.Dream.Test.Collections {

    [TestFixture]
    public class WorkStealingDequeTests {

        //--- Methods ---
        [Test]
        public void New_Count() {
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);
        }

        [Test]
        public void New_TryPop_Count() {
            int value;
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);
        }

        [Test]
        public void New_TryPop_TryPop_Count() {
            int value;
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);
        }

        [Test]
        public void New_Push_Count() {
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            q.Push(42);
            Assert.AreEqual(1, q.Count);
        }

        [Test]
        public void New_Push_TryPop_Count() {
            int value;
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            q.Push(42);
            Assert.AreEqual(1, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(42, value);
            Assert.AreEqual(0, q.Count);
        }

        [Test]
        public void New_TryPop_Push_Count() {
            int value;
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);

            q.Push(42);
            Assert.AreEqual(1, q.Count);
        }

        [Test]
        public void New_TryPop_Push_TryPop_Count() {
            int value;
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);

            q.Push(42);
            Assert.AreEqual(1, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(42, value);
            Assert.AreEqual(0, q.Count);
        }

        [Test]
        public void New_TryPop_Push_TryPop_TryPop_Count() {
            int value;
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);

            q.Push(42);
            Assert.AreEqual(1, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(42, value);
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);
        }

        [Test]
        public void New_Push_x50_Count() {
            int value;
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            for(int i = 0; i < 50; ++i) {
                q.Push(100 - i);
            }
            Assert.AreEqual(50, q.Count);

            for(int i = 49; i >= 0; --i) {
                q.TryPop(out value);
                Assert.AreEqual(100 - i, value);
            }
            Assert.AreEqual(0, q.Count);
        }

        [Test]
        public void New_Push_x50_TryPop_x50_Count() {
            int value;
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            for(int i = 0; i < 50; ++i) {
                q.Push(100 - i);
            }
            Assert.AreEqual(50, q.Count);

            for(int i = 49; i >= 0; --i) {
                q.TryPop(out value);
                Assert.AreEqual(100 - i, value);
            }
            Assert.AreEqual(0, q.Count);
        }

        [Test]
        public void New_Push_x50_TryPop_x50_TryPop_Count() {
            int value;
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            for(int i = 0; i < 50; ++i) {
                q.Push(100 - i);
            }
            Assert.AreEqual(50, q.Count);

            for(int i = 49; i >= 0; --i) {
                q.TryPop(out value);
                Assert.AreEqual(100 - i, value);
            }
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);
        }

        [Test]
        public void New_TryPop_Push_x50_TryPop_x50_TryPop_Count() {
            int value;
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);

            for(int i = 0; i < 50; ++i) {
                q.Push(100 - i);
            }
            Assert.AreEqual(50, q.Count);

            for(int i = 49; i >= 0; --i) {
                q.TryPop(out value);
                Assert.AreEqual(100 - i, value);
            }
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);
        }

        [Test]
        public void New_TryPop_Push_x31_TryPop_x31_TryPop_Count() {
            int value;
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);

            for(int i = 0; i < 31; ++i) {
                q.Push(100 - i);
            }
            Assert.AreEqual(31, q.Count);

            for(int i = 30; i >= 0; --i) {
                q.TryPop(out value);
                Assert.AreEqual(100 - i, value);
            }
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);
        }

        [Test]
        public void New_TryPop_Push_x32_TryPop_x32_TryPop_Count() {
            int value;
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);

            for(int i = 0; i < 32; ++i) {
                q.Push(100 - i);
            }
            Assert.AreEqual(32, q.Count);

            for(int i = 31; i >= 0; --i) {
                q.TryPop(out value);
                Assert.AreEqual(100 - i, value);
            }
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);
        }

        [Test]
        public void New_TryPop_Push_x33_TryPop_x33_TryPop_Count() {
            int value;
            var q = new WorkStealingDeque<int>();
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);

            for(int i = 0; i < 33; ++i) {
                q.Push(100 - i);
            }
            Assert.AreEqual(33, q.Count);

            for(int i = 32; i >= 0; --i) {
                q.TryPop(out value);
                Assert.AreEqual(100 - i, value);
            }
            Assert.AreEqual(0, q.Count);

            q.TryPop(out value);
            Assert.AreEqual(0, q.Count);
        }
    }
}
