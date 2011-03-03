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
using System.Diagnostics;
using MindTouch.Dream.Test.Mock;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class TimesTests {

        [Test]
        public void One_for_Never_is_TooMany() {
            var t = Times.Never();
            Assert.AreEqual(Times.Result.TooMany, t.Verify(1));
        }

        [Test]
        public void None_for_Never_is_Ok() {
            var t = Times.Never();
            Assert.AreEqual(Times.Result.Ok, t.Verify(0));
        }

        [Test]
        public void One_for_Once_is_Ok() {
            var t = Times.Once();
            Assert.AreEqual(Times.Result.Ok, t.Verify(1));
        }
        
        [Test]
        public void None_for_Once_is_TooFew() {
            var t = Times.Once();
            Assert.AreEqual(Times.Result.TooFew,t.Verify(0));
        }

        [Test]
        public void Two_for_Once_is_TooMany() {
            var t = Times.Once();
            Assert.AreEqual(Times.Result.TooMany, t.Verify(2));
        }

        [Test]
        public void None_for_AtMostOnce_is_Ok() {
            var t = Times.AtMostOnce();
            Assert.AreEqual(Times.Result.Ok, t.Verify(0));
        }

        [Test]
        public void Five_for_AtMost_5_is_Ok() {
            var t = Times.AtMost(5);
            Assert.AreEqual(Times.Result.Ok, t.Verify(5));
        }

        [Test]
        public void Four_for_AtMost_5_is_Ok() {
            var t = Times.AtMost(5);
            Assert.AreEqual(Times.Result.Ok, t.Verify(4));
        }

        [Test]
        public void Six_for_AtMost_5_is_TooMany() {
            var t = Times.AtMost(5);
            Assert.AreEqual(Times.Result.TooMany, t.Verify(6));
        }

        [Test]
        public void Five_for_AtLeast_5_is_Ok() {
            var t = Times.AtLeast(5);
            Assert.AreEqual(Times.Result.Ok, t.Verify(5));
        }

        [Test]
        public void Six_for_AtLeast_5_is_Ok() {
            var t = Times.AtLeast(5);
            Assert.AreEqual(Times.Result.Ok, t.Verify(6));
        }

        [Test]
        public void Four_for_AtLeast_5_is_TooFew() {
            var t = Times.AtLeast(5);
            Assert.AreEqual(Times.Result.TooFew, t.Verify(4));
        }

        [Test]
        public void Five_for_Exactly_5_is_Ok() {
            var t = Times.Exactly(5);
            Assert.AreEqual(Times.Result.Ok, t.Verify(5));
        }

        [Test]
        public void Six_for_Exactly_5_is_TooMany() {
            var t = Times.Exactly(5);
            Assert.AreEqual(Times.Result.TooMany, t.Verify(6));
        }

        [Test]
        public void Four_for_Exactly_5_is_TooFew() {
            var t = Times.Exactly(5);
            Assert.AreEqual(Times.Result.TooFew, t.Verify(4));
        }

        [Test]
        public void Never_waits_for_timeout() {
            var t = Times.Never();
            var stopwatch = Stopwatch.StartNew();
            Assert.AreEqual(Times.Result.Ok, t.Verify(0, TimeSpan.FromSeconds(1)));
            stopwatch.Stop();
            Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds,900);
        }

        [Test]
        public void Once_does_not_wait_for_timeout() {
            var t = Times.Once();
            var stopwatch = Stopwatch.StartNew();
            Assert.AreEqual(Times.Result.Ok, t.Verify(1, TimeSpan.FromSeconds(1)));
            stopwatch.Stop();
            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds,100);
        }

        [Test]
        public void TooFew_causes_timeout_wait() {
            var t = Times.AtLeast(3);
            var stopwatch = Stopwatch.StartNew();
            Assert.AreEqual(Times.Result.TooFew, t.Verify(2, TimeSpan.FromSeconds(1)));
            stopwatch.Stop();
            Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 900);
        }

        [Test]
        public void TooMany_does_not_cause_timeout_wait() {
            var t = Times.AtMost(3);
            var stopwatch = Stopwatch.StartNew();
            Assert.AreEqual(Times.Result.TooMany, t.Verify(4, TimeSpan.FromSeconds(1)));
            stopwatch.Stop();
            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, 100);
        }

    }
}