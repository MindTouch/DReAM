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
    /// Provides an implementation of <see cref="IDispatchQueue"/> that immediately invokes the work item
    /// rather than queueing t for execution.
    /// </summary>
    public class ImmediateDispatchQueue : IDispatchQueue {

        //--- Class Fields ---

        /// <summary>
        /// Singleton instnace of the <see cref="ImmediateDispatchQueue"/>.
        /// </summary>
        public static readonly ImmediateDispatchQueue Instance = new ImmediateDispatchQueue();

        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        
        //--- Constructors ---
        private ImmediateDispatchQueue() { }

        //--- Methods ---

        /// <summary>
        /// Adds a work-item to the thread pool.  Depending on the implementation, this method may block the invoker
        /// or throw an exception if the thread pool cannot accept more items.
        /// </summary>
        /// <param name="callback">Item to add to the thread pool.</param>
        public void QueueWorkItem(Action callback) {
            try {
                callback();
            } catch(Exception e) {

                // log exception, but ignore it; outer task is immune to it
                _log.WarnExceptionMethodCall(e, "QueueWorkItem: unhandled exception in callback");
            }
        }

        /// <summary>
        /// In the context of this implementation, <see cref="TryQueueWorkItem"/> behaves identically to <see cref="QueueWorkItem"/>
        /// and will always return <see langword="True"/>.
        /// </summary>
        /// <param name="callback">Item to add to the thread pool.</param>
        /// <returns>Always returns <see langword="True"/>.</returns>
        public bool TryQueueWorkItem(Action callback) {
            QueueWorkItem(callback);
            return true;
        }

        /// <summary>
        /// Convert the dispatch queue into a string.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString() {
            return "ImmediateDispatchQueue";
        }
    }
}
