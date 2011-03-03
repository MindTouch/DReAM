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
using System.Collections.Generic;
using System.Threading;
using log4net;
using MindTouch.Collections;
using MindTouch.Tasking;

namespace MindTouch.Threading {

    /// <summary>
    /// ElasticPriorityThreadPool provides a thread pool that can have a variable number of threads going from a minimum number of reserved
    /// threads to a maximum number of parallel threads.
    /// </summary>
    /// <remarks>
    /// The threads are obtained from the DispatchThreadScheduler and shared across all other clients of the DispatchThreadScheduler.
    /// Obtained threads are released automatically if the thread pool is idle for long enough.  Reserved threads are never released.
    /// </remarks>
    public class ElasticPriorityThreadPool : IDispatchHost, IDisposable {

        //--- Constants ---

        /// <summary>
        /// Maximum number of threads that can be reserved by a single instance.
        /// </summary>
        public const int MAX_RESERVED_THREADS = 1000;

        //--- Types ---
        private class PrioritizedThreadPool : IDispatchQueue {

            //--- Fields ---
            private readonly int _priority;
            private readonly ElasticPriorityThreadPool _pool;

            //--- Constructors ---
            public PrioritizedThreadPool(int priority, ElasticPriorityThreadPool pool) {
                _priority = priority;
                _pool = pool;
            }

            //--- Properties ---
            public int Priority { get { return _priority; } }

            //--- Methods ---
            public void QueueWorkItem(Action callback) {
                if(!TryQueueWorkItem(callback)) {
                    throw new NotSupportedException("TryQueueWorkItem failed");
                }
            }

            public bool TryQueueWorkItem(Action callback) {
                return _pool.TryQueueWorkItem(_priority, callback);
            }
        }

        //--- Class Fields ---
        private static int _instanceCounter;
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly object _syncRoot = new object();
        private readonly int _id = Interlocked.Increment(ref _instanceCounter);
        private readonly IThreadsafePriorityQueue<Action> _inbox;
        private readonly PrioritizedThreadPool[] _prioritizedInbox;
        private readonly IThreadsafeStack<KeyValuePair<DispatchThread, Result<DispatchWorkItem>>> _reservedThreads = new LockFreeStack<KeyValuePair<DispatchThread, Result<DispatchWorkItem>>>();
        private readonly int _minReservedThreads;
        private readonly int _maxParallelThreads;
        private int _threadCount;
        private int _threadVelocity;
        private DispatchThread[] _activeThreads;
        private bool _disposed;

        //--- Constructors ---

        /// <summary>
        /// Creates a new ElasticPriorityThreadPool instance.
        /// </summary>
        /// <param name="minReservedThreads">Minium number of threads to reserve for the thread pool.</param>
        /// <param name="maxParallelThreads">Maximum number of parallel threads used by the thread pool.</param>
        /// <param name="maxPriority">Maximum priority number (inclusive upper bound).</param>
        /// <exception cref="InsufficientResourcesException">The ElasticPriorityThreadPool instance was unable to obtain the minimum reserved threads.</exception>
        public ElasticPriorityThreadPool(int minReservedThreads, int maxParallelThreads, int maxPriority) {
            _minReservedThreads = Math.Max(0, Math.Min(minReservedThreads, MAX_RESERVED_THREADS));
            _maxParallelThreads = Math.Max(Math.Max(1, minReservedThreads), Math.Min(maxParallelThreads, int.MaxValue));
            _inbox = new LockFreePriorityQueue<Action>(maxPriority);
            _prioritizedInbox = new PrioritizedThreadPool[maxPriority];
            for(int i = 0; i < maxPriority; ++i) {
                _prioritizedInbox[i] = new PrioritizedThreadPool(i, this);
            }

            // initialize reserved threads
            _activeThreads = new DispatchThread[Math.Min(_maxParallelThreads, Math.Max(_minReservedThreads, Math.Min(16, _maxParallelThreads)))];
            if(_minReservedThreads > 0) {
                DispatchThreadScheduler.RequestThread(_minReservedThreads, AddThread);
            }
            DispatchThreadScheduler.RegisterHost(this);
            _log.DebugFormat("Create @{0}", this);
        }

        //--- Properties ---

        /// <summary>
        /// Number of minimum reserved threads.
        /// </summary>
        public int MinReservedThreads { get { return _disposed ? 0 : _minReservedThreads; } }

        /// <summary>
        /// Number of maxium parallel threads.
        /// </summary>
        public int MaxParallelThreads { get { return _maxParallelThreads; } }

        /// <summary>
        /// Number of threads currently used.
        /// </summary>
        public int ThreadCount { get { return _threadCount; } }

        /// <summary>
        /// Number of items pending for execution.
        /// </summary>
        public int WorkItemCount {
            get {
                int result = _inbox.Count;
                DispatchThread[] threads = _activeThreads;
                foreach(DispatchThread thread in threads) {
                    if(thread != null) {
                        result += thread.PendingWorkItemCount;
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Max priority for work items.
        /// </summary>
        public int MaxPriority { get { return _inbox.MaxPriority; } }

        /// <summary>
        /// Accessor for prioritized dispatch queue.
        /// </summary>
        /// <param name="priority">Dispatch queue priority level (between 0 and MaxPriority).</param>
        /// <returns>Prioritized dispatch queue.</returns>
        public IDispatchQueue this[int priority] {
            get {
                if(_disposed) {
                    throw new ObjectDisposedException("ElasticPriorityThreadPool has already been disposed");
                }
                return _prioritizedInbox[priority];
            }
        }

        //--- Methods ---

        /// <summary>
        /// Shutdown the ElasticThreadPool instance.  This method blocks until all pending items have finished processing.
        /// </summary>
        public void Dispose() {
            if(!_disposed) {
                _disposed = true;
                _log.DebugFormat("Dispose @{0}", this);

                // TODO (steveb): make dispose more reliable
                // 1) we can't wait indefinitively!
                // 2) we should progressively sleep longer and longer to avoid unnecessary overhead
                // 3) this pattern feels useful enough to be captured into a helper method

                // wait until all threads have been decommissioned
                while(ThreadCount > 0) {
                    Thread.Sleep(100);
                }

                // discard all reserved threads
                KeyValuePair<DispatchThread, Result<DispatchWorkItem>> reserved;
                while(_reservedThreads.TryPop(out reserved)) {
                    DispatchThreadScheduler.ReleaseThread(reserved.Key, reserved.Value);
                }
                DispatchThreadScheduler.UnregisterHost(this);
            }
        }

        /// <summary>
        /// Convert the dispatch queue into a string.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString() {
            return string.Format("ElasticPriorityThreadPool @{0} (current: {1}, reserve: {5}, velocity: {2}, min: {3}, max: {4}, items: {6}, max-priority: {7})", _id, _threadCount, _threadVelocity, _minReservedThreads, _maxParallelThreads, _reservedThreads.Count, WorkItemCount, _inbox.MaxPriority);
        }

        private bool TryQueueWorkItem(int priority, Action callback) {
            if(_disposed) {
                throw new ObjectDisposedException("ElasticThreadPool has already been disposed");
            }

            // check if we can enqueue work-item into current dispatch thread
            IDispatchQueue queue = this[priority];
            if(DispatchThread.TryQueueWorkItem(queue, callback)) {
                return true;
            }

            // check if there are available threads to which the work-item can be given to
            KeyValuePair<DispatchThread, Result<DispatchWorkItem>> entry;
            if(_reservedThreads.TryPop(out entry)) {
                lock(_syncRoot) {
                    RegisterThread("new item", entry.Key);
                }

                // found an available thread, let's resume it with the work-item
                entry.Value.Return(new DispatchWorkItem(callback, queue));
                return true;
            }

            // no threads available, keep work-item for later
            if(!_inbox.TryEnqueue(priority, callback)) {
                return false;
            }

            // check if we need to request a thread to kick things off
            if(ThreadCount == 0) {
                ((IDispatchHost)this).IncreaseThreadCount("request first thread");
            }
            return true;
        }

        private void AddThread(KeyValuePair<DispatchThread, Result<DispatchWorkItem>> keyvalue) {
            DispatchThread thread = keyvalue.Key;
            Result<DispatchWorkItem> result = keyvalue.Value;
            if(_threadVelocity >= 0) {
                lock(_syncRoot) {
                    _threadVelocity = 0;

                    // check if an item is available for dispatch
                    int priority;
                    Action callback;
                    if(TryRequestItem(null, out priority, out callback)) {
                        RegisterThread("new thread", thread);

                        // dispatch work-item
                        result.Return(new DispatchWorkItem(callback, this[priority]));
                        return;
                    }
                }
            }

            // we have no need for this thread
            RemoveThread("insufficient work for new thread", thread, result);
        }

        private void RemoveThread(string reason, DispatchThread thread, Result<DispatchWorkItem> result) {
            if(thread == null) {
                throw new ArgumentNullException("thread");
            }
            if(result == null) {
                throw new ArgumentNullException("result");
            }
            if(thread.PendingWorkItemCount != 0) {
                throw new ArgumentException(string.Format("thread #{1} still has work-items in queue (items: {0})", thread.PendingWorkItemCount, thread.Id), "thread");
            }

            // remove thread from list of allocated threads
            lock(_syncRoot) {
                _threadVelocity = 0;
                UnregisterThread(reason, thread);
            }

            // check if we can put thread into the reserved list
            if(_reservedThreads.Count < MinReservedThreads) {
                if(!_reservedThreads.TryPush(new KeyValuePair<DispatchThread, Result<DispatchWorkItem>>(thread, result))) {
                    throw new NotSupportedException("TryPush failed");
                }
            } else {

                // return thread to resource manager
                DispatchThreadScheduler.ReleaseThread(thread, result);
            }
        }

        private bool TryRequestItem(DispatchThread thread, out int priority, out Action callback) {

            // check if we can find a work-item in the shared queue
            if(_inbox.TryDequeue(out priority, out callback)) {
                return true;
            }

            // try to steal a work-item from another thread; take a snapshot of all allocated threads (needed in case the array is copied for resizing)
            DispatchThread[] threads = _activeThreads;
            foreach(DispatchThread entry in threads) {

                // check if we can steal a work-item from this thread
                if((entry != null) && !ReferenceEquals(entry, thread) && entry.TryStealWorkItem(out callback)) {
                    priority = ((PrioritizedThreadPool)entry.DispatchQueue).Priority;
                    return true;
                }
            }

            // check again if we can find a work-item in the shared queue since trying to steal may have overlapped with the arrival of a new item
            if(_inbox.TryDequeue(out priority, out callback)) {
                return true;
            }
            return false;
        }

        private void RegisterThread(string reason, DispatchThread thread) {
            ++_threadCount;
            thread.Host = this;

            // find an empty slot in the array of all threads
            int index;
            for(index = 0; index < _activeThreads.Length; ++index) {

                // check if we found an empty slot
                if(_activeThreads[index] == null) {

                    // assign it to the found slot and stop iterating
                    _activeThreads[index] = thread;
                    break;
                }
            }

            // check if we need to grow the array
            if(index == _activeThreads.Length) {

                // make room to add a new thread by doubling the array size and copying over the existing entries
                DispatchThread[] newArray = new DispatchThread[2 * _activeThreads.Length];
                Array.Copy(_activeThreads, newArray, _activeThreads.Length);

                // assign new thread
                newArray[index] = thread;

                // update instance field
                _activeThreads = newArray;
            }
#if EXTRA_DEBUG
            _log.DebugFormat("AddThread: {1} - {0}", this, reason);
#endif
        }

        private void UnregisterThread(string reason, DispatchThread thread) {
            thread.Host = null;

            // find thread and remove it
            for(int i = 0; i < _activeThreads.Length; ++i) {
                if(ReferenceEquals(_activeThreads[i], thread)) {
                    --_threadCount;
                    _activeThreads[i] = null;
#if EXTRA_DEBUG
                    _log.DebugFormat("RemoveThread: {1} - {0}", this, reason);
#endif
                    break;
                }
            }
        }

        //--- IDispatchHost Members ---
        long IDispatchHost.PendingWorkItemCount { get { return WorkItemCount; } }
        int IDispatchHost.MinThreadCount { get { return MinReservedThreads; } }
        int IDispatchHost.MaxThreadCount { get { return _maxParallelThreads; } }

        void IDispatchHost.RequestWorkItem(DispatchThread thread, Result<DispatchWorkItem> result) {
            if(thread == null) {
                throw new ArgumentNullException("thread");
            }
            if(thread.PendingWorkItemCount > 0) {
                throw new ArgumentException(string.Format("thread #{1} still has work-items in queue (items: {0})", thread.PendingWorkItemCount, thread.Id), "thread");
            }
            if(!ReferenceEquals(thread.Host, this)) {
                throw new InvalidOperationException(string.Format("thread is allocated to another queue: received {0}, expected: {1}", thread.Host, this));
            }
            if(result == null) {
                throw new ArgumentNullException("result");
            }

            // check if we need to decommission threads without causing starvation
            if(_threadVelocity < 0) {
                RemoveThread("system saturation", thread, result);
                return;
            }

            // check if we found a work-item
            int priority;
            Action callback;
            if(TryRequestItem(thread, out priority, out callback)) {

                // dispatch work-item
                result.Return(new DispatchWorkItem(callback, this[priority]));
            } else {

                // relinquich thread; it's not required anymore
                RemoveThread("insufficient work", thread, result);
            }
        }

        void IDispatchHost.IncreaseThreadCount(string reason) {

            // check if thread pool is already awaiting another thread
            if(_threadVelocity > 0) {
                return;
            }
            lock(_syncRoot) {
                _threadVelocity = 1;

                // check if thread pool has enough threads
                if(_threadCount >= _maxParallelThreads) {
                    _threadVelocity = 0;
                    return;
                }
#if EXTRA_DEBUG
                _log.DebugFormat("IncreaseThreadCount: {1} - {0}", this, reason);
#endif
            }

            // check if there are threads in the reserve
            KeyValuePair<DispatchThread, Result<DispatchWorkItem>> reservedThread;
            if(_reservedThreads.TryPop(out reservedThread)) {
                AddThread(reservedThread);
            } else {
                DispatchThreadScheduler.RequestThread(0, AddThread);
            }
        }

        void IDispatchHost.MaintainThreadCount(string reason) {

            // check if thread pool is already trying to steady
            if(_threadVelocity == 0) {
                return;
            }
            lock(_syncRoot) {
                _threadVelocity = 0;
#if EXTRA_DEBUG
                _log.DebugFormat("MaintainThreadCount: {1} - {0}", this, reason);
#endif
            }
        }

        void IDispatchHost.DecreaseThreadCount(string reason) {

            // check if thread pool is already trying to discard thread
            if(_threadVelocity < 0) {
                return;
            }
            lock(_syncRoot) {
                _threadVelocity = -1;
#if EXTRA_DEBUG
                _log.DebugFormat("DecreaseThreadCount: {1} - {0}", this, reason);
#endif
            }
        }
    }
}
