/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
using System.Threading;
using MindTouch.Dream;
using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class LockTests {

        [Test]
        public void WhenDone_is_called_when_lock_is_acquired() {
            var l = new Lock<int>(1);
            var first = l.Capture(new Result());
            var second = l.Capture(new Result());
            Assert.IsTrue(first.HasValue);
            Assert.IsFalse(second.HasValue);
            var reset = new ManualResetEvent(false);
            second.WhenDone( _ => reset.Set());
            l.Release();
            Assert.IsTrue(reset.WaitOne(TimeSpan.FromSeconds(10),true));
            Assert.IsTrue(second.HasValue);
            l.Release();
        }

        [Test]
        public void Only_one_thread_can_acquire_the_lock_at_a_time() {
            var l = new Lock<int>(1);
            var trigger = new ManualResetEvent(false);
            var first = Async.Fork(() =>
            {
                trigger.WaitOne();
                Thread.Sleep(200);
                l.Capture(new Result()).Wait();
            }, new Result());
            Result secondInternal = null;
            var second = Async.Fork(() =>
            {
                trigger.WaitOne();
                secondInternal = l.Capture(new Result());
            }, new Result());
            trigger.Set();
            Assert.IsFalse(first.HasValue);
            second.Wait();
            l.Release();
            Assert.IsTrue(secondInternal.HasValue);
            Assert.IsFalse(first.HasValue);
            secondInternal.Wait();
            first.Wait();
            Assert.IsTrue(first.HasValue);
        }
    }
}
