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

namespace MindTouch.Threading {

    /// <summary>
    /// Value wrapper for work-itenms issued returned by <see cref="IDispatchHost"/>.
    /// </summary>
    internal struct DispatchWorkItem {

        //--- Fields ---

        /// <summary>
        /// Work-item callback.
        /// </summary>
        public readonly Action WorkItem;

        /// <summary>
        /// Dispatch queue associated with work-item.
        /// </summary>
        public readonly IDispatchQueue DispatchQueue;

        //--- Constructors ---

        /// <summary>
        /// Create new work-item instance.
        /// </summary>
        /// <param name="workitem">Work-item callback.</param>
        /// <param name="queue">Dispatch queue associated with work-item.</param>
        public DispatchWorkItem(Action workitem, IDispatchQueue queue) {
            this.WorkItem = workitem;
            this.DispatchQueue = queue;
        }
    }
}