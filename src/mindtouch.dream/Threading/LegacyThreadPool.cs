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
using MindTouch.Tasking;

namespace MindTouch.Threading {

    /// <summary>
    /// LegacyThreadPool is a singleton class that provides an IDispatchQueue interface to the System.Threading.ThreadPool class.
    /// </summary>
    public sealed class LegacyThreadPool : IDispatchQueue {

        //--- Claas Fields ---

        /// <summary>
        /// Unique instance for interacting with System.Threading.ThreadPool class using the IDispatchQueue interface.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown when System.Threading.ThreadPool cannot accept the work-item.</exception>
        public static readonly LegacyThreadPool Instance = new LegacyThreadPool();

        //--- Constructors ---
        private LegacyThreadPool() { }

        //--- Methods ---

        /// <summary>
        /// Adds an item to the thread pool.  This method is implemented using System.Threading.ThreadPool::UnsafeQueueUserWorkItem().
        /// </summary>
        /// <param name="callback">Item to add to the thread pool.</param>
        public void QueueWorkItem(Action callback) {
            ThreadPool.UnsafeQueueUserWorkItem(delegate {
                Async.CurrentDispatchQueue = this;
                try {
                    callback();
                } finally {
                    Async.CurrentDispatchQueue = null;
                }
            }, null);
        }

        /// <summary>
        /// Adds an item to the thread pool.  This method is implemented using System.Threading.ThreadPool::UnsafeQueueUserWorkItem().
        /// </summary>
        /// <param name="callback">Item to add to the thread pool.</param>
        /// <returns>True if the work-item was enqueud, false otherwise.</returns>
        public bool TryQueueWorkItem(Action callback) {
            try {
                QueueWorkItem(callback);
            } catch(NotSupportedException) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Convert the dispatch queue into a string.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString() {
            return "LegacyThreadPool";
        }
    }
}
