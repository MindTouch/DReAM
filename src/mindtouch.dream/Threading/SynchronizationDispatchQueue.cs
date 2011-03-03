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

namespace MindTouch.Threading {

    /// <summary>
    /// Provides an implementation of <see cref="IDispatchQueue"/> that immediately dispatches against a <see cref="SynchronizationContext"/>.
    /// </summary>
    public class SynchronizationDispatchQueue : IDispatchQueue {

        //--- Fields ---

        /// <summary>
        /// The synchronization context of this dispatch queue.
        /// </summary>
        public readonly SynchronizationContext Context;

        //--- Constructors ---

        /// <summary>
        /// Create a new dispatch queue for a given synchronization context.
        /// </summary>
        /// <param name="context">Context to post work items against.</param>
        public SynchronizationDispatchQueue(SynchronizationContext context) {
            Context = context;
        }

        //--- Methods ---

        /// <summary>
        /// Adds a work-item to the thread pool.  Depending on the implementation, this method may block the invoker
        /// or throw an exception if the thread pool cannot accept more items.
        /// </summary>
        /// <param name="callback">Item to add to the thread pool.</param>
        public void QueueWorkItem(Action callback) {
            Context.Post(_ => callback(), null);
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
            return string.Format("SynchronizationDispatchQueue (context: {0})", Context);
        }
    }
}
