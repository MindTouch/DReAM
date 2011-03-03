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

using log4net;
using MindTouch.Dream;
using MindTouch.Tasking;

namespace System {

    /// <summary>
    /// Static utility class containing extension and helper methods for Debug instrumentation.
    /// </summary>
    public static class DebugUtil {

        //--- Class Fields ---
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static TimeSpan _collectInterval = TimeSpan.Zero;
        private static readonly TaskTimer _collectTimer = TaskTimerFactory.Default.New(delegate(TaskTimer taskTimer) {
            double memBefore = (double)GC.GetTotalMemory(false) / 1024 / 1024;
            double memAfter = (double)GC.GetTotalMemory(true) / 1024 / 1024;
            _log.DebugFormat("forced GC: {0:0.00}MB before / {1:0.00}MB after", memBefore, memAfter);
            taskTimer.Change(_collectInterval, TaskEnv.New());
        },null);

        /// <summary>
        /// Global flag to signal whether stack traces should be captured.
        /// </summary>
        public static bool CaptureStackTrace = false;

        //--- Class Methods ---

        /// <summary>
        /// Set an interval at which Garbage collection should be forced.
        /// </summary>
        /// <param name="t">Interval length.</param>
        public static void SetCollectionInterval(TimeSpan t) {
            _collectInterval = t;
            _collectTimer.Change(t, TaskEnv.New());
            _log.DebugFormat("set collection interval to {0:0} seconds",t.TotalSeconds);
        }

        /// <summary>
        /// Get the current StackTrace.
        /// </summary>
        /// <returns>Currently applicable StackTrace.</returns>
        public static System.Diagnostics.StackTrace GetStackTrace() {
            if(CaptureStackTrace) {
                return new System.Diagnostics.StackTrace(1, true);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Wrap a stopwatch aroudn the execution of an action.
        /// </summary>
        /// <param name="handler">The action to be timed.</param>
        /// <returns>Time elapsed during the handler's execution.</returns>
        public static TimeSpan Stopwatch(Action handler) {
            var s = new Diagnostics.Stopwatch();
            s.Start();
            handler();
            s.Stop();
            return s.Elapsed;
        }
    }
}