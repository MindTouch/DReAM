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
using log4net;
using MindTouch.Collections;
using MindTouch.Tasking;

namespace MindTouch.Threading {
    internal sealed class DispatchThread {

        //--- Class Fields ---
        [ThreadStatic]
        public static DispatchThread CurrentThread;
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        public static bool TryQueueWorkItem(IDispatchQueue queue, Action callback) {
            DispatchThread current = CurrentThread;
            if((current != null) && ReferenceEquals(Async.CurrentDispatchQueue, queue)) {

                // NOTE (steveb): next call can never fail since we're calling the queue work-item method of the current thread
                current.QueueWorkItem(callback);
                return true;
            }
            return false;
        }

        //--- Fields ---
        private readonly WorkStealingDeque<Action> _inbox = new WorkStealingDeque<Action>();
        private readonly int _id;
        private volatile IDispatchHost _host;
        private IDispatchQueue _queue;

        //--- Constructors ---
        internal DispatchThread() {

            // create new thread
            var thread = Async.MaxStackSize.HasValue
                ? new Thread(DispatchLoop, Async.MaxStackSize.Value) { IsBackground = true }
                : new Thread(DispatchLoop) { IsBackground = true };

            //  assign ID
            _id = thread.ManagedThreadId;
            thread.Name = "DispatchThread #" + _id;
            _log.DebugFormat("DispatchThread #{0} created", _id);

            // kick-off new thread
            thread.Start();
        }

        //--- Properties ---
        public int Id { get { return _id; } }
        public int PendingWorkItemCount { get { return _inbox.Count; } }
        public IDispatchQueue DispatchQueue { get { return _queue; } }

        public IDispatchHost Host {
            get { return _host; }
            internal set {

                // check if host is being set or cleared
                if(value != null) {
                    if(_host != null) {
                        throw new InvalidOperationException(string.Format("DispatchThread #{0} already assigned to {1}", _id, _host));
                    }
#if EXTRA_DEBUG
                    _log.DebugFormat("DispatchThread #{0} assigned to {1}", _id, value);
#endif
                } else {
#if EXTRA_DEBUG
                    if(_host != null) {
                        _log.DebugFormat("DispatchThread #{0} unassigned from {1}", _id, _host);
                    }
#endif
                }

                // update state
                _host = value;
            }
        }

        //--- Methods ---
        public bool TryStealWorkItem(out Action callback) {
            return _inbox.TrySteal(out callback);
        }

        public void EvictWorkItems() {

            // clear CurrentThread to avoid having the evicted items being immediately added back again to the thread (i.e. infinite loop)
            CurrentThread = null;
            try {
                Action item;

                // remove up to 100 items from the current threads work queue
                for(int i = 0; (i < 100) && _inbox.TryPop(out item); ++i) {
                    _queue.QueueWorkItem(item);
                }
            } finally {

                // restore CurrentThread before we exit
                CurrentThread = this;
            }
        }

        private void QueueWorkItem(Action callback) {

            // NOTE (steveb): this method MUST be called from the dispatcher's associated thread

            // increase work-item counter and enqueue item
            _inbox.Push(callback);
        }

        private void DispatchLoop() {

            // set thread-local self-reference
            CurrentThread = this;

            // begin thread loop
            try {
                while(true) {

                    // check if queue has a work-item
                    Action callback;
                    if(!_inbox.TryPop(out callback)) {
                        var result = new Result<DispatchWorkItem>(TimeSpan.MaxValue);

                        // reset the dispatch queue for this thread
                        Async.CurrentDispatchQueue = null;

                        // check if thread is associated with a host already
                        if(_host == null) {

                            // NOTE (steveb): this is a brand new thread without a host yet

                            // return the thread to the dispatch scheduler
                            DispatchThreadScheduler.ReleaseThread(this, result);
                        } else {

                            // request another work-item
                            _host.RequestWorkItem(this, result);
                        }

                        // block until a work item is available
                        result.Block();

                        // check if we received a work item or an exception to shutdown
                        if(result.HasException && (result.Exception is DispatchThreadShutdownException)) {

                            // time to shut down
                            _log.DebugFormat("DispatchThread #{0} destroyed", _id);
                            return;
                        }
                        callback = result.Value.WorkItem;
                        _queue = result.Value.DispatchQueue;

                        // TODO (steveb): handle the weird case where _queue is null

                        // set the dispatch queue for this thread
                        Async.CurrentDispatchQueue = _queue;
                    }

                    // execute work-item
                    if(callback != null) {
                        try {
                            callback();
                        } catch(Exception e) {
                            _log.Warn("an unhandled exception occurred while executing the work-item", e);
                        }
                    }
                }
            } catch(Exception e) {

                // something went wrong that shouldn't have!
                _log.ErrorExceptionMethodCall(e, string.Format("DispatchLoop #{0}: FATAL ERROR", _id));

                // TODO (steveb): tell _host about untimely exit; post items in queue to host inbox (o/w we lose them)

            } finally {
                CurrentThread = null;
            }
        }
    }
}
