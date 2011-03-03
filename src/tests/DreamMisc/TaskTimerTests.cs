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
using System.Threading;
using MindTouch;
using MindTouch.Dream;
using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class TaskTimerTests {

        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        
        [Test]
        public void TaskTimer_Shutdown() {
            var shouldFire = new ManualResetEvent(false);
            var neverFires = new ManualResetEvent(false);
            TaskTimer.New(
                TimeSpan.FromSeconds(2),
                delegate(TaskTimer tt) {
                    _log.DebugFormat("this task timer should never have fired");
                    neverFires.Set();
                },
                null,
                TaskEnv.None);
            TaskTimer.New(
                TimeSpan.FromSeconds(1),
                delegate(TaskTimer tt) {
                    _log.DebugFormat("this task timer should fire before we try shutdown");
                    shouldFire.Set();
                },
                null,
                TaskEnv.None);
            _log.DebugFormat("waiting for first task");
            Assert.IsTrue(shouldFire.WaitOne(2000,false));
            _log.DebugFormat("starting shutdown");
            TaskTimer.Shutdown();
            _log.DebugFormat("shutdown complete");
            Assert.IsFalse(neverFires.WaitOne(2000, false));
        }
    }
}
