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
using System.Diagnostics;

using MindTouch.Collections;
using MindTouch.Tasking;
using MindTouch.Threading.Timer;

namespace MindTouch.Threading {
    internal static class DispatchThreadScheduler {

        //--- Constants ---
        private static readonly TimeSpan IDLE_TIME_LIMIT = TimeSpan.FromSeconds(6);
        private static readonly TimeSpan SATURATION_TIME_LIMIT = TimeSpan.FromSeconds(3);
        private const float CPU_MAINTAIN_THRESHOLD = 90.0f;
        private const float CPU_SATURATION_THRESHOLD = 98.0f;

        //--- Class Fields ---
        private static readonly object _syncRoot = new object();
        private static int _allocatedThreads;
        private static TimeSpan _idleTime = TimeSpan.Zero;
        private static TimeSpan _saturationTime = TimeSpan.Zero;
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static readonly LockFreeItemConsumerQueue<KeyValuePair<DispatchThread, Result<DispatchWorkItem>>> _threadReserve = new LockFreeItemConsumerQueue<KeyValuePair<DispatchThread, Result<DispatchWorkItem>>>();
        private static readonly int _maxThreads;
        private static readonly int _threadReserveCount;
        private static readonly int _minThreadReserveCount;
        private static readonly Action<int> _createThreads = CreateThreads;
        private static readonly IList<IDispatchHost> _hosts = new List<IDispatchHost>();
        private static readonly PerformanceCounter _cpus;
        private static DateTime _nextCpuCounterRead = DateTime.MinValue;
        private static float _lastCpuValue;

        //--- Class Constructors ---
        static DispatchThreadScheduler() {

            // initialize performance counter to read how busy the CPU is
            try {
                _cpus = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpus.NextValue();
            } catch(Exception e) {
                _log.Warn("Unable to read the performance counter Processor/% Processor Time. Likely cause is a corrupted counter. This can be fixed by running 'lodctr /r'. Please refer to http://technet.microsoft.com/en-us/library/bb490926.aspx before attempting this fix", e);
                _cpus = null;
            }

            // read system wide settings
            _threadReserveCount = ReadAppSetting("reserved-dispatch-threads", 20);
            _minThreadReserveCount = ReadAppSetting("min-reserved-dispatch-threads", _threadReserveCount / 2);

            // TODO (steveb): we should base this on available memory (e.g. total_memory / 2 / 1MB_stack_size_per_thread)
            _maxThreads = ReadAppSetting("max-dispatch-threads", 1000);


            // add maintenance callback
            GlobalClock.AddCallback("DispatchThreadScheduler", Tick);

            // add initial reserve of threads
            CreateThreads(Math.Min(_threadReserveCount, _maxThreads));
        }

        //--- Class Properties ---
        public static int MaxThreadCount { get { return _maxThreads; } }
        public static int AvailableThreadCount { get { return _maxThreads - _allocatedThreads; } }

        private static float ProcessorLoad {
            get {

                // Note (arnec): If _cpus is null, initializing the performance counter failed and we're flying blind. Hopefully the
                // user considered the suggestion from the WARN message we provided.
                if(_cpus == null) {
                    return 0;
                }
                DateTime now = DateTime.UtcNow;
                if(now > _nextCpuCounterRead) {
                    _nextCpuCounterRead = now.AddSeconds(0.25);
                    _lastCpuValue = _cpus.NextValue();
                }
                return _lastCpuValue;
            }
        }

        //--- Class Methods ---
        public static void RequestThread(int minRequired, Action<KeyValuePair<DispatchThread, Result<DispatchWorkItem>>> callback) {
            int consume;
            int create;
            lock(_syncRoot) {

                // reset idle time
                _idleTime = TimeSpan.Zero;

                // check if a guaranteed number of threads were requested
                if(minRequired > 0) {

                    // check if we can supply the guaranteed number of threads; otherwise fail
                    if(_allocatedThreads + minRequired > _maxThreads) {
                        throw new InsufficientResourcesException("unable to obtain minium required threads");
                    }

                    // consume all requested thread from thread reserve
                    consume = minRequired;

                    // recreate all requested threads sicne we won't see them back again
                    create = minRequired;
                } else {

                    // only consume one thread from thread reserve
                    consume = 1;

                    // only create new threads if the thread reserve has fallen below a minium level
                    int count = _threadReserve.ItemCount;
                    if((count < _minThreadReserveCount) && (_allocatedThreads < MaxThreadCount)) {
                        create = 1;
                    } else {
                        create = 0;
                    }
                }

                // update number of allocated threads
                _allocatedThreads += create;

                // check if max number of threads has been reached
                if(_allocatedThreads >= _maxThreads) {
                    _log.WarnFormat("RequestThread: reached max threads for app domain (max: {0}, allocated: {1})", _maxThreads, _allocatedThreads);
                }
            }

            // assign requested threads
            for(int i = 0; i < consume; ++i) {
                if(!_threadReserve.TryEnqueue(callback)) {
                    throw new NotSupportedException("TryEnqueue failed");
                }
            }

            // create new threads to replenish thread reserve
            CreateThreadAsync(create);
        }

        public static void ReleaseThread(DispatchThread thread, Result<DispatchWorkItem> result) {
            if(thread == null) {
                throw new ArgumentNullException("thread");
            }
            if(result == null) {
                throw new ArgumentNullException("result");
            }

            // unbind thread from current host
            thread.Host = null;

            // add thread to list of idle threads
            if(!_threadReserve.TryEnqueue(new KeyValuePair<DispatchThread, Result<DispatchWorkItem>>(thread, result))) {
                throw new NotSupportedException("TryEnqueue failed");
            }
        }

        public static void RegisterHost(IDispatchHost host) {
            lock(_hosts) {
                _hosts.Add(host);
            }
        }

        public static void UnregisterHost(IDispatchHost host) {
            lock(_hosts) {
                for(int i = 0; i < _hosts.Count; ++i) {
                    if(ReferenceEquals(_hosts[i], host)) {
                        _hosts.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        private static void Tick(DateTime now, TimeSpan elapsed) {
            lock(_syncRoot) {

                // check if resource manager has been idle for a while
                _idleTime += elapsed;
                if(_idleTime > IDLE_TIME_LIMIT) {
                    int count = _threadReserve.ItemCount;
                    if(count > _threadReserveCount) {
                        _log.DebugFormat("Tick: idling with excessive thread reserve (count: {0})", count);
                        _idleTime = TimeSpan.Zero;

                        // try discarding an idle thread
                        KeyValuePair<DispatchThread, Result<DispatchWorkItem>> entry;
                        if(_threadReserve.TryDequeue(out entry)) {
                            --_allocatedThreads;
                            entry.Value.Throw(new DispatchThreadShutdownException());
                        }
                    }
                }
            }

            // loop over hosts and determine if their target thread count needs to be adjusted
            lock(_hosts) {
                string reason = null;
                _saturationTime += elapsed;

                // check if the system is saturated with work
                float load = ProcessorLoad;
                if((load >= CPU_SATURATION_THRESHOLD) && (_saturationTime > SATURATION_TIME_LIMIT)) {

                    // TODO: be more selective on which host to decrease

                    // request threads to be discarded
                    foreach(IDispatchHost host in _hosts) {
                        long itemCount = host.PendingWorkItemCount;
                        int threadCount = host.ThreadCount;

                        // check if host is starving (which means it has items to process, but no threads)
                        if((itemCount > 0) && (threadCount == 0)) {

                            // take care of starving hosts (doesn't matter if we're saturated or not)
                            host.IncreaseThreadCount(string.Format("starving (items: {0})", itemCount));
                        } else if(threadCount > 1) {
                            if(reason == null) {
                                reason = string.Format("throttle down (cpu: {0}%, threads: {1})", load, threadCount);
                            }

                            // tell host to decrease thread count
                            host.DecreaseThreadCount(reason);
                        }
                    }
                } else if(load >= CPU_MAINTAIN_THRESHOLD) {

                    // reset saturation counter if we're running below saturation level
                    if(load < CPU_SATURATION_THRESHOLD) {
                        _saturationTime = TimeSpan.Zero;
                    }

                    // stop new threads from being requested
                    foreach(IDispatchHost host in _hosts) {
                        long itemCount = host.PendingWorkItemCount;
                        int threadCount = host.ThreadCount;

                        // check if host is starving (which means it has items to process, but no threads)
                        if((itemCount > 0) && (threadCount == 0)) {

                            // take care of starving hosts (doesn't matter if we're saturated or not)
                            host.IncreaseThreadCount(string.Format("starving (items: {0})", itemCount));
                        } else {
                            if(reason == null) {
                                reason = string.Format("maintain (cpu: {0}%)", load);
                            }

                            // tell host to decrease thread count
                            host.MaintainThreadCount(reason);
                        }
                    }
                } else {
                    _saturationTime = TimeSpan.Zero;

                    // allocate additional threads if we have capacity to do so
                    foreach(IDispatchHost host in _hosts) {
                        long itemCount = host.PendingWorkItemCount;

                        // check if host is starving (which means it has items to process, but no threads)
                        if((itemCount > 0) && (host.ThreadCount == 0)) {

                            // take care of starving hosts (doesn't matter if we're saturated or not)
                            host.IncreaseThreadCount(string.Format("starving (items: {0})", itemCount));
                        } else if(itemCount > 0) {
                            if(reason == null) {
                                reason = string.Format("throttle up (cpu: {0}%, items: {1})", load, itemCount);
                            }

                            // tell host to increase thread count
                            host.IncreaseThreadCount(reason);
                        }
                    }
                }
            }
        }

        private static void CreateThreadAsync(int count) {
            if(count > 0) {
                _log.DebugFormat("request {0} threads to be created", count);
                _createThreads.BeginInvoke(count, CreateThreadDone, null);
            }
        }

        private static void CreateThreads(int count) {
            for(int i = 0; i < count; ++i) {
                try {
                    new DispatchThread();
                } catch(Exception e) {
                    _log.Error("CreateThreads", e);
                }
            }
        }

        private static void CreateThreadDone(IAsyncResult result) {
            _createThreads.EndInvoke(result);
        }

        private static int ReadAppSetting(string key, int def) {
            int result;
            if(!int.TryParse(System.Configuration.ConfigurationManager.AppSettings[key], out result)) {
                return def;
            }
            return result;
        }
    }
}
