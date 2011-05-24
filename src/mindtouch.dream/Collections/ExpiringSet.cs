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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MindTouch.Dream;
using MindTouch.Tasking;

namespace MindTouch.Collections {

    // Note (arnec): This class allows deep access into its data storage and internals and is only supposed to be used
    // as the internal storage of a set that properly encapsulates it via its public interface.
    internal class ExpiringSet<TKey, TValue> : IDisposable {

        //--- Types ---
        public class Entry {

            //--- Fields ---
            public DateTime When;
            public TimeSpan TTL;
            public TKey Key;
            public TValue Value;
            public bool Removed;

            //--- Constructors ---
            public Entry(TKey key, TValue value, DateTime when, TimeSpan ttl) {
                Key = key;
                Value = value;
                When = when;
                TTL = ttl;
            }

            //--- Methods ---
            public void Release() {
                Removed = true;
                Key = default(TKey);
                Value = default(TValue);
            }
        }

        public class ExpiredArgs : EventArgs {
            public readonly IEnumerable<Entry> Entries;
            public ExpiredArgs(IEnumerable<Entry> entries) {
                Entries = entries;
            }
        }

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly TaskTimerFactory _taskTimerFactory;
        private readonly bool _autoRefresh;
        private readonly List<Entry> _orderedExpirations = new List<Entry>();
        private readonly Dictionary<TKey, Entry> _expirationLookup = new Dictionary<TKey, Entry>();
        private TaskTimer _expireTimer;

        //--- Events ---
        public event EventHandler CollectionChanged;
        public event EventHandler<ExpiredArgs> EntriesExpired;

        //--- Constructors ---
        public ExpiringSet(TaskTimerFactory taskTimerFactory, bool autoRefresh) {
            _taskTimerFactory = taskTimerFactory;
            _autoRefresh = autoRefresh;
            _expireTimer = _taskTimerFactory.New(DateTime.MaxValue, OnExpire, null, TaskEnv.None);
        }

        //--- Properties ---
        public Entry this[TKey key] {
            get {
                lock(_expirationLookup) {
                    Entry entry;
                    if(_expirationLookup.TryGetValue(key, out entry)) {
                        if(_autoRefresh) {
                            RefreshExpiration(entry);
                        }
                        return entry;
                    }
                }
                return null;
            }
        }

        //--- Methods ---
        public void RefreshExpiration(TKey key) {
            lock(_expirationLookup) {
                Entry entry;
                if(_expirationLookup.TryGetValue(key, out entry)) {
                    RefreshExpiration(entry);
                }
            }
        }

        public bool TryDelete(TKey key, out TValue value) {
            lock(_expirationLookup) {
                var entry = this[key];
                if(entry == null) {
                    value = default(TValue);
                    return false;
                }
                value = entry.Value;
                _expirationLookup.Remove(key);

                // marking entry as removed, will be pruned from _orderedExpirations when expiration is attempted
                entry.Release();
            }
            OnCollectionChange();
            return true;
        }


        public IEnumerable<Entry> GetEntries() {
            lock(_expirationLookup) {
                return _expirationLookup.Values.ToArray();
            }
        }

        public void Dispose() {
            lock(_expirationLookup) {
                _expireTimer.Change(DateTime.MaxValue, TaskEnv.None);
                _expirationLookup.Clear();
            }
        }

        public void RefreshExpiration(Entry entry) {
            lock(_expirationLookup) {
                var when = DateTime.UtcNow + entry.TTL;

                // Note (arnec): Only refreshing on half second resolution w/ autoRefresh, since it could cause a lot of refresh churn  
                if(_autoRefresh && Math.Abs(entry.When.Subtract(when).TotalMilliseconds) <= 500) {
                    return;
                }
                entry.When = when;
                CheckTimer(when);
            }
            OnCollectionChange();
        }

        public bool SetExpiration(TKey key, TValue value, DateTime when, TimeSpan ttl, bool create) {
            TValue oldValue;
            return SetExpiration(key, value, when, ttl, create, out oldValue);
        }

        public bool SetExpiration(TKey key, TValue value, DateTime when, TimeSpan ttl, bool create, out TValue oldValue) {
            Entry entry;
            var existed = false;
            lock(_expirationLookup) {
                if(!_expirationLookup.TryGetValue(key, out entry)) {
                    oldValue = default(TValue);
                    if(!create) {
                        return existed;
                    }
                    entry = new Entry(key, value, when, ttl);
                    _expirationLookup[key] = entry;
                    _orderedExpirations.Add(entry);
                } else {
                    oldValue = entry.Value;
                    entry.Value = value;
                    entry.When = when;
                    entry.TTL = ttl;
                    existed = true;
                }
                CheckTimer(when);
            }
            OnCollectionChange();
            return existed;
        }


        public void Clear() {
            lock(_expirationLookup) {
                foreach(var entry in _expirationLookup.Values) {
                    entry.Release();
                }
                _expirationLookup.Clear();
                _expireTimer.Change(TimeSpan.MaxValue, TaskEnv.None);
            }
            OnCollectionChange();
        }

        private void OnExpire(TaskTimer timer) {
            var now = DateTime.UtcNow;
            List<Entry> expirations = null;
            lock(_expirationLookup) {

                // Note (arnec): Sort is cheap here since we're generally dealing with an already sorted or mostly sorted set at this point
                _orderedExpirations.Sort((a, b) => a.When.CompareTo(b.When));
                while(_orderedExpirations.Count > 0) {
                    var entry = _orderedExpirations[0];
                    if(entry.Removed) {

                        // this item was already removed from the set, and no longer requires expiration, but does need removal from ordered set
                        _orderedExpirations.RemoveAt(0);
                        continue;
                    }
                    if(entry.When > now) {
                        _expireTimer.Change(entry.When, TaskEnv.New());
                        break;
                    }
                    entry.Removed = true;
                    _expirationLookup.Remove(entry.Key);
                    _orderedExpirations.RemoveAt(0);
                    _log.DebugFormat("expired item with key '{0}'", entry.Key);
                    if(EntriesExpired == null) {
                        continue;
                    }
                    if(expirations == null) {
                        expirations = new List<Entry>();
                    }
                    expirations.Add(entry);
                }
            }
            if(expirations == null) {
                return;
            }
            OnEntriesExpired(expirations);
            OnCollectionChange();
        }

        private void OnEntriesExpired(IEnumerable<Entry> expirations) {
            if(EntriesExpired != null) {
                EntriesExpired(this, new ExpiredArgs(expirations));
            }
        }

        private void OnCollectionChange() {
            if(CollectionChanged != null) {
                CollectionChanged(this, EventArgs.Empty);
            }
        }

        private void CheckTimer(DateTime when) {
            if(_expireTimer.Status == TaskTimerStatus.Done || _expireTimer.When > when) {
                _expireTimer.Change(when, TaskEnv.New());
            }
        }
    }
}
