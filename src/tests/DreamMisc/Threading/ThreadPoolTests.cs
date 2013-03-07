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

using System;
using System.Diagnostics;
using System.Threading;
using log4net;
using MindTouch.Tasking;
using MindTouch.Threading;
using NUnit.Framework;

namespace MindTouch.Dream.Test.Threading {

    [Ignore("run tests manually")]
    [TestFixture]
    public class ThreadPoolTests {

        //--- Class Fields ---
        private static int _counter;
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Methods ---

        [SetUp]
        public void Setup() {
            _counter = 0;
        }

        [Test]
        public void ElasticThreadPool_Fibonacci_from_1_to_33_threads() {
            var throughputs = new TimeSpan[33];
            for(int i = 1; i < throughputs.Length; ++i) {
                using(var stp = new ElasticThreadPool(i, i)) {
                    int value;
                    TimeSpan elapsed;
                    FibonacciThreadPool(stp, 30, TimeSpan.Zero, out value, out elapsed);
                    Assert.AreEqual(832040, value);
                    throughputs[i] = elapsed;
                }
            }
            _log.Debug("--- Results ---");
            for(int i = 1; i < throughputs.Length; ++i) {
                _log.DebugFormat("{0,2}: {1}", i, throughputs[i]);
            }
        }

        [Test]
        public void ElasticThreadPool_Fibonacci_Min_0_Max_1() {
            var stp = new ElasticThreadPool(0, 1);
            int value;
            TimeSpan elapsed;

            FibonacciThreadPool(stp, 30, TimeSpan.Zero, out value, out elapsed);
            Assert.AreEqual(832040, value);

            _log.Debug("Time: " + elapsed);
            _log.Debug("Work items processed: " + _counter);
            stp.Dispose();
            Assert.AreEqual(0, stp.WorkItemCount, "WorkQueue items");
            Assert.AreEqual(0, stp.ThreadCount, "WorkQueue threads");
        }

        [Test]
        public void ElasticThreadPool_Fibonacci_Min_0_Max_4() {
            var stp = new ElasticThreadPool(0, 4);
            int value;
            TimeSpan elapsed;

            FibonacciThreadPool(stp, 30, TimeSpan.Zero, out value, out elapsed);
            Assert.AreEqual(832040, value);

            _log.Debug("Time: " + elapsed);
            _log.Debug("Work items processed: " + _counter);
            stp.Dispose();
            Assert.AreEqual(0, stp.WorkItemCount, "WorkQueue items");
            Assert.AreEqual(0, stp.ThreadCount, "WorkQueue threads");
        }

        [Test]
        public void ElasticThreadPool_Fibonacci_Min_0_Max_10() {
            var stp = new ElasticThreadPool(0, 10);
            int value;
            TimeSpan elapsed;

            FibonacciThreadPool(stp, 30, TimeSpan.Zero, out value, out elapsed);
            Assert.AreEqual(832040, value);

            _log.Debug("Time: " + elapsed);
            _log.Debug("Work items processed: " + _counter);
            stp.Dispose();
            Assert.AreEqual(0, stp.WorkItemCount, "WorkQueue items");
            Assert.AreEqual(0, stp.ThreadCount, "WorkQueue threads");
        }

        [Test]
        public void ElasticThreadPool_Fibonacci_Min_0_Max_30() {
            var stp = new ElasticThreadPool(0, 30);
            int value;
            TimeSpan elapsed;

            FibonacciThreadPool(stp, 30, TimeSpan.Zero, out value, out elapsed);
            Assert.AreEqual(832040, value);

            _log.Debug("Time: " + elapsed);
            _log.Debug("Work items processed: " + _counter);
            stp.Dispose();
            Assert.AreEqual(0, stp.WorkItemCount, "WorkQueue items");
            Assert.AreEqual(0, stp.ThreadCount, "WorkQueue threads");
        }

        [Test]
        public void ElasticThreadPool_Fibonacci_Min_1_Max_30() {
            var stp = new ElasticThreadPool(1, 30);
            int value;
            TimeSpan elapsed;

            FibonacciThreadPool(stp, 30, TimeSpan.Zero, out value, out elapsed);
            Assert.AreEqual(832040, value);

            _log.Debug("Time: " + elapsed);
            _log.Debug("Work items processed: " + _counter);
            stp.Dispose();
            Assert.AreEqual(0, stp.WorkItemCount, "WorkQueue items");
            Assert.AreEqual(0, stp.ThreadCount, "WorkQueue threads");
        }

        [Test]
        public void ElasticThreadPool_Fibonacci_Min_4_Max_30() {
            var stp = new ElasticThreadPool(4, 30);
            int value;
            TimeSpan elapsed;

            FibonacciThreadPool(stp, 30, TimeSpan.Zero, out value, out elapsed);
            Assert.AreEqual(832040, value);

            _log.Debug("Time: " + elapsed);
            _log.Debug("Work items processed: " + _counter);
            stp.Dispose();
            Assert.AreEqual(0, stp.WorkItemCount, "WorkQueue items");
            Assert.AreEqual(0, stp.ThreadCount, "WorkQueue threads");
        }

        [Test]
        public void ElasticThreadPool_Fibonacci_Min_0_Max_100_with_1ms_delay() {
            var stp = new ElasticThreadPool(0, 100);
            int value;
            TimeSpan elapsed;

            FibonacciThreadPool(stp, 25, TimeSpan.FromSeconds(0.001), out value, out elapsed);
            Assert.AreEqual(75025, value);

            _log.Debug("Time: " + elapsed);
            _log.Debug("Work items processed: " + _counter);
            stp.Dispose();
            Assert.AreEqual(0, stp.WorkItemCount, "WorkQueue items");
            Assert.AreEqual(0, stp.ThreadCount, "WorkQueue threads");
        }

        [Test]
        public void ElasticThreadPool_Multi_Fibonacci_Min_0_Max_30() {
            const int test = 4;

            // initialize data structures
            ElasticThreadPool[] stp = new ElasticThreadPool[test];
            Result<int>[] results = new Result<int>[test];
            for(int i = 0; i < test; ++i) {
                stp[i] = new ElasticThreadPool(0, 30);
                results[i] = new Result<int>(TimeSpan.MaxValue, TaskEnv.New(stp[i]));
            }

            // start test
            var sw = Stopwatch.StartNew();
            for(int i = 0; i < results.Length; ++i) {
                _log.DebugFormat("--- FIBONACCI KICK-OFF: {0}", i);
                Fibonacci(stp[i], 30, TimeSpan.Zero, results[i]);
            }
            results.Join(new Result(TimeSpan.MaxValue)).Wait();
            sw.Stop();
            TimeSpan elapsed = sw.Elapsed;

            // check results
            for(int i = 0; i < test; ++i) {
                Assert.AreEqual(832040, results[i].Value, "result {0} did not match", i);
            }
            _log.Debug("Time: " + elapsed);
            _log.Debug("Work items processed: " + _counter);
            for(int i = 0; i < test; ++i) {
                stp[i].Dispose();
                Assert.AreEqual(0, stp[i].WorkItemCount, "WorkQueue[{0}] items", i);
                Assert.AreEqual(0, stp[i].ThreadCount, "WorkQueue[{0}] threads", i);
            }
        }

        [Test]
        public void ElasticThreadPool_Multi_Fibonacci_Min_1_Max_30() {
            const int test = 4;

            // initialize data structures
            ElasticThreadPool[] stp = new ElasticThreadPool[test];
            Result<int>[] results = new Result<int>[test];
            for(int i = 0; i < test; ++i) {
                stp[i] = new ElasticThreadPool(1, 30);
                results[i] = new Result<int>(TimeSpan.MaxValue, TaskEnv.New(stp[i]));
            }

            // start test
            var sw = Stopwatch.StartNew();
            for(int i = 0; i < results.Length; ++i) {
                _log.DebugFormat("--- FIBONACCI KICK-OFF: {0}", i);
                Fibonacci(stp[i], 30, TimeSpan.Zero, results[i]);
            }
            results.Join(new Result(TimeSpan.MaxValue)).Wait();
            sw.Stop();
            TimeSpan elapsed = sw.Elapsed;

            // check results
            for(int i = 0; i < test; ++i) {
                Assert.AreEqual(832040, results[i].Value, "result {0} did not match", i);
            }
            _log.Debug("Time: " + elapsed);
            _log.Debug("Work items processed: " + _counter);
            for(int i = 0; i < test; ++i) {
                stp[i].Dispose();
                Assert.AreEqual(0, stp[i].WorkItemCount, "WorkQueue[{0}] items", i);
                Assert.AreEqual(0, stp[i].ThreadCount, "WorkQueue[{0}] threads", i);
            }
        }


        [Test]
        public void ElasticThreadPool_Multi_Staged_Fibonacci_Min_0_Max_30() {
            const int test = 4;

            // initialize data structures
            ElasticThreadPool[] stp = new ElasticThreadPool[test];
            Result<int>[] results = new Result<int>[test];
            for(int i = 0; i < test; ++i) {
                stp[i] = new ElasticThreadPool(0, 30);
                results[i] = new Result<int>(TimeSpan.MaxValue, TaskEnv.New(stp[i]));
            }

            // start test
            var sw = Stopwatch.StartNew();
            for(int i = 0; i < results.Length; ++i) {
                _log.DebugFormat("--- FIBONACCI KICK-OFF: {0}", i);
                Fibonacci(stp[i], 30, TimeSpan.Zero, results[i]);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            results.Join(new Result(TimeSpan.MaxValue)).Wait();
            sw.Stop();
            TimeSpan elapsed = sw.Elapsed;

            // check results
            for(int i = 0; i < test; ++i) {
                Assert.AreEqual(832040, results[i].Value, "result {0} did not match", i);
            }
            _log.Debug("Time: " + elapsed);
            _log.Debug("Work items processed: " + _counter);
            for(int i = 0; i < test; ++i) {
                stp[i].Dispose();
                Assert.AreEqual(0, stp[i].WorkItemCount, "WorkQueue[{0}] items", i);
                Assert.AreEqual(0, stp[i].ThreadCount, "WorkQueue[{0}] threads", i);
            }
        }

        [Test]
        public void ElasticThreadPool_Multi_Staged_Fibonacci_Min_1_Max_30() {
            const int test = 4;

            // initialize data structures
            ElasticThreadPool[] stp = new ElasticThreadPool[test];
            Result<int>[] results = new Result<int>[test];
            for(int i = 0; i < test; ++i) {
                stp[i] = new ElasticThreadPool(1, 30);
                results[i] = new Result<int>(TimeSpan.MaxValue, TaskEnv.New(stp[i]));
            }

            // start test
            var sw = Stopwatch.StartNew();
            for(int i = 0; i < results.Length; ++i) {
                _log.DebugFormat("--- FIBONACCI KICK-OFF: {0}", i);
                Fibonacci(stp[i], 30, TimeSpan.Zero, results[i]);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            results.Join(new Result(TimeSpan.MaxValue)).Wait();
            sw.Stop();
            TimeSpan elapsed = sw.Elapsed;

            // check results
            for(int i = 0; i < test; ++i) {
                Assert.AreEqual(832040, results[i].Value, "result {0} did not match", i);
            }
            _log.Debug("Time: " + elapsed);
            _log.Debug("Work items processed: " + _counter);
            for(int i = 0; i < test; ++i) {
                stp[i].Dispose();
                Assert.AreEqual(0, stp[i].WorkItemCount, "WorkQueue[{0}] items", i);
                Assert.AreEqual(0, stp[i].ThreadCount, "WorkQueue[{0}] threads", i);
            }
        }
        
        [Test]
        public void TestThreadPool_Legacy() {
            var stp = LegacyThreadPool.Instance;
            int value;
            TimeSpan elapsed;

            FibonacciThreadPool(stp, 30, TimeSpan.Zero, out value, out elapsed);
            Assert.AreEqual(832040, value);

            _log.Debug("Result: " + value);
            _log.Debug("Time: " + elapsed);
            _log.Debug("Work items processed: " + _counter);
        }

        private static void FibonacciThreadPool(IDispatchQueue stp, int n, TimeSpan delay, out int value, out TimeSpan elapsed) {
            _log.Debug("FibonacciThreadPool");

            var sw = Stopwatch.StartNew();
            value = Fibonacci(stp, n, delay, new Result<int>(TimeSpan.MaxValue, TaskEnv.New(stp))).Wait();
            sw.Stop();
            elapsed = sw.Elapsed;
        }

        private static Result<int> Fibonacci(IDispatchQueue stp, int n, TimeSpan delay, Result<int> result) {
            if(!ReferenceEquals(result.Env.DispatchQueue, stp)) {
                _log.Error(string.Format("ERROR: wrong task env {0}, expected {1}.", result.Env.DispatchQueue, stp));
            }
            stp.QueueWorkItem(delegate {
                Interlocked.Increment(ref _counter);
                switch(n) {
                case 0:
                    if(delay > TimeSpan.Zero) {
                        Thread.Sleep(delay);
                    }
                    result.Return(0);
                    break;
                case 1:
                    if(delay > TimeSpan.Zero) {
                        Thread.Sleep(delay);
                    }
                    result.Return(1);
                    break;
                default:
                    Result<int> a = Fibonacci(stp, n - 1, delay, new Result<int>(TimeSpan.MaxValue, TaskEnv.New(stp)));
                    Result<int> b = Fibonacci(stp, n - 2, delay, new Result<int>(TimeSpan.MaxValue, TaskEnv.New(stp)));
                    new AResult[] { a, b }.Join(new Result(TimeSpan.MaxValue, TaskEnv.New(stp))).WhenDone(_ => {
                        if(!ReferenceEquals(AsyncUtil.CurrentDispatchQueue, stp)) {
                            _log.Error(string.Format("ERROR: wrong queue {0}, expected {1}.", AsyncUtil.CurrentDispatchQueue, stp));
                        }
                        result.Return(a.Value + b.Value);
                    });
                    break;
                }
            });
            return result;
        }
    }
}
