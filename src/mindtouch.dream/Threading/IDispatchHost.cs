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

using MindTouch.Tasking;

namespace MindTouch.Threading {

    /// <summary>
    /// IDispatchHost interface provides methods for requesting new work-items and processing them.
    /// </summary>
    internal interface IDispatchHost {

        //--- Properties ---

        /// <summary>
        /// Number of items that are waiting to be dispatched.
        /// </summary>
        long PendingWorkItemCount { get; }

        /// <summary>
        /// Minimum number of threads used by the dispatch host.
        /// </summary>
        int MinThreadCount { get; }

        /// <summary>
        /// Maximum number of threads used by the dispatch host.
        /// </summary>
        int MaxThreadCount { get; }

        /// <summary>
        /// Current number of threads used by the dispatch host.
        /// </summary>
        int ThreadCount { get; }

        //--- Mehods ---

        /// <summary>
        /// Invoked by an assigned DispatchThread when request a new work-item.
        /// </summary>
        /// <param name="thread">DispatchThread requesting the work-item.</param>
        /// <param name="result">Result object on which to submit the work-item.</param>
        void RequestWorkItem(DispatchThread thread, Result<DispatchWorkItem> result);

        /// <summary>
        /// Invoked by DispatchThreadScheduler to indicate that dispatch host should request additional threads if needed.
        /// </summary>
        /// <param name="reason">Reason for invoking this method (used for debug output).</param>
        void IncreaseThreadCount(string reason);

        /// <summary>
        /// Invoked by DispatchThreadScheduler to indicate that dispatch host should neither request new threads, nor release threads that are in use.
        /// </summary>
        /// <param name="reason">Reason for invoking this method (used for debug output).</param>
        void MaintainThreadCount(string reason);

        /// <summary>
        /// Invoked by DispatchThreadScheduler to indicate that dispatch host should release threads if possible.
        /// </summary>
        /// <param name="reason">Reason for invoking this method (used for debug output).</param>
        void DecreaseThreadCount(string reason);
    }
}