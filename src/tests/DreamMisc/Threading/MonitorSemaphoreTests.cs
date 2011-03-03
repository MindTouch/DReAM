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
using MindTouch.Threading;
using NUnit.Framework;

namespace MindTouch.Dream.Test.Threading {

    [TestFixture]
    public class MonitorSemaphoreTests {

        [Test]
        public void Signal_then_wait() {
            MonitorSemaphore monitor = new MonitorSemaphore();
            monitor.Signal();
            var timeout = monitor.Wait(TimeSpan.FromSeconds(1));
            
            Assert.IsTrue(timeout, "timeout should not have occurred");
        }

        [Test]
        public void Signal_signal_then_wait_wait_wait() {
            MonitorSemaphore monitor = new MonitorSemaphore();
            monitor.Signal();
            monitor.Signal();
            var timeout = monitor.Wait(TimeSpan.FromSeconds(1));            
            Assert.IsTrue(timeout, "timeout 1 should not have occurred");

            timeout = monitor.Wait(TimeSpan.FromSeconds(1));
            Assert.IsTrue(timeout, "timeout 2 should not have occurred");

            timeout = monitor.Wait(TimeSpan.FromSeconds(1));
            Assert.IsFalse(timeout, "timeout 3 should have occurred");
        }
        
        [Test]
        public void Wait_then_timeout() {
            MonitorSemaphore monitor = new MonitorSemaphore();
            var timeout = monitor.Wait(TimeSpan.FromSeconds(0.1));

            Assert.IsFalse(timeout, "timeout should have occurred");
        }

        [Test]
        public void Wait_then_Signal() {
            MonitorSemaphore monitor = new MonitorSemaphore();
            new Thread(() => {
                Thread.Sleep(100);
                monitor.Signal();
            }).Start();
            var timeout = monitor.Wait(TimeSpan.FromSeconds(3));

            Assert.IsTrue(timeout, "timeout should not have occurred");
        }
    }
}
