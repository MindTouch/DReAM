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
using MindTouch.Tasking;

namespace MindTouch.Collections {
    /// <summary>
    /// Represents a dictionary of key/value pairs with an expiration time.
    /// </summary>
    /// <remarks>
    /// Values inserted into this this set will expire after some set time (which may be updated and reset), automatically removing the value
    /// from the set and firing a notification event <see cref="EntryExpired"/>. The dictionary may optionally be configured to extend a pair's
    /// expiration on access, including accessing the entry via the instances iterator.
    /// </remarks>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue">The type of elements in the list.</typeparam>
    public class ExpiringDictionary<TKey, TValue> : IEnumerable<ExpiringDictionary<TKey, TValue>.Entry>, IDisposable {

        //--- Types
        /// <summary>
        /// A wrapper class containing meta data about values stored in the <see cref="ExpiringDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <remarks>
        /// The meta-data is created on demand, i.e. it will not reflect changes in <see cref="When"/> or <see cref="TTL"/> that happen
        /// after it is retrieved from the set.
        /// </remarks>
        public class Entry {

            //--- Class Methods ---
            internal static Entry FromExpiringSetEntry(ExpiringSet<TKey, TValue>.Entry entry) {
                return new Entry(entry.Key, entry.Value, entry.When, entry.TTL);
            }

            //--- Fields ---

            /// <summary>
            /// The absolute time at which the entry will expire.
            /// </summary>
            public readonly DateTime When;

            /// <summary>
            /// The time-to-live of the entry at insertion time.
            /// </summary>
            public readonly TimeSpan TTL;

            /// <summary>
            /// The key stored in the dictionary.
            /// </summary>
            public readonly TKey Key;

            /// <summary>
            /// The value stored in the dictionary.
            /// </summary>
            public readonly TValue Value;

            //--- Constructors ---
            private Entry(TKey key, TValue value, DateTime when, TimeSpan ttl) {
                Key = key;
                Value = value;
                When = when;
                TTL = ttl;
            }
        }

        //--- Events ---

        /// <summary>
        /// Fired for every entry that expires.
        /// </summary>
        public event EventHandler<ExpirationArgs<TKey, TValue>> EntryExpired;

        /// <summary>
        /// Fired any time a value is added, removed or experiation is changed.
        /// </summary>
        public event EventHandler CollectionChanged;

        //--- Fields ---
        private readonly ExpiringSet<TKey, TValue> _set;

        //--- Constructors ---

        /// <summary>
        /// Create a new hashset
        /// </summary>
        /// <param name="taskTimerFactory">The timer factory to create the set's timer from</param>
        public ExpiringDictionary(TaskTimerFactory taskTimerFactory) : this(taskTimerFactory, false) { }

        /// <summary>
        /// Create a new hashset
        /// </summary>
        /// <param name="taskTimerFactory">The timer factory to create the set's timer from</param>
        /// <param name="autoRefresh"><see langword="True"/> if accessing an entry should extend the expiration time by the time-to-live</param>
        public ExpiringDictionary(TaskTimerFactory taskTimerFactory, bool autoRefresh) {
            _set = new ExpiringSet<TKey, TValue>(taskTimerFactory, autoRefresh);
            _set.CollectionChanged += OnCollectionChanged;
            _set.EntriesExpired += OnEntriesExpired;
        }

        //--- Properties ---

        /// <summary>
        /// Get the <see cref="Entry"/> meta-data container for the given value.
        /// </summary>
        /// <remarks>
        /// This access will refresh the entry's expiration, if the set was created with the 'autoRefresh' flag.
        /// </remarks>
        /// <param name="key">The identifying key.</param>
        /// <returns>Meta-data for specified value. Returns <see langword="null"/> if the value is not stored in the set.</returns>
        public Entry this[TKey key] {
            get {
                var entry = _set[key];
                return entry == null ? null : Entry.FromExpiringSetEntry(entry);
            }
        }

        //--- Methods ---

        /// <summary>
        /// Clear the entire set immediately
        /// </summary>
        public void Clear() {
            _set.Clear();
        }

        /// <summary>
        /// Update the value of an entry.
        /// </summary>
        /// <param name="key">The identifying key.</param>
        /// <param name="value">The keyed value.</param>
        /// <returns><see langword="True"/> if an entry existed for the given key.</returns>
        public bool Update(TKey key, TValue value) {
            var entry = _set[key];
            if(entry == null) {
                return false;
            }
            entry.Value = value;
            return true;
        }

        /// <summary>
        /// Add or update a key/value pair.
        /// </summary>
        /// <param name="key">The identifying key.</param>
        /// <param name="value">The keyed value.</param>
        /// <param name="ttl">The time-to-live from right now</param>
        public void Set(TKey key, TValue value, TimeSpan ttl) {
            var when = ttl == TimeSpan.MaxValue ? DateTime.MaxValue : DateTime.UtcNow + ttl;
            _set.SetExpiration(key, value, when, ttl, true);
        }

        /// <summary>
        /// Add or update a key/value pair.
        /// </summary>
        /// <param name="key">The identifying key.</param>
        /// <param name="value">The keyed value.</param>
        /// <param name="ttl">The time-to-live from right now</param>
        /// <param name="oldValue">Previous value stored at the key location</param>
        /// <returns><see langword="True"/> if an entry existed for the given key.</returns>
        public bool TrySet(TKey key, TValue value, TimeSpan ttl, out TValue oldValue) {
            var when = ttl == TimeSpan.MaxValue ? DateTime.MaxValue : DateTime.UtcNow + ttl;
            return _set.SetExpiration(key, value, when, ttl, true, out oldValue);
        }

        /// <summary>
        /// Add or update a key/value pair.
        /// </summary>
        /// <param name="key">The identifying key.</param>
        /// <param name="value">The keyed value.</param>
        /// <param name="when">The absolute expiration time of the pair.</param>
        public void Set(TKey key, TValue value, DateTime when) {
            var ttl = when - DateTime.UtcNow;
            _set.SetExpiration(key, value, when, ttl, true);
        }

        /// <summary>
        /// Add or update a key/value pair.
        /// </summary>
        /// <param name="key">The identifying key.</param>
        /// <param name="value">The keyed value.</param>
        /// <param name="oldValue">Previous value stored at the key location</param>
        /// <param name="when">The absolute expiration time of the pair.</param>
        public bool TrySet(TKey key, TValue value, DateTime when, out TValue oldValue) {
            var ttl = when - DateTime.UtcNow;
            return _set.SetExpiration(key, value, when, ttl, true, out oldValue);
        }

        /// <summary>
        /// Set the expiration for a key/value pair.
        /// </summary>
        /// <param name="key">The identifying key.</param>
        /// <param name="ttl">Time-to-live for the entry.</param>
        /// <returns><see langword="True"/> if an entry existed for the given key.</returns>
        public bool SetExpiration(TKey key, TimeSpan ttl) {
            var when = DateTime.UtcNow + ttl;
            return _set.SetExpiration(key, default(TValue), when, ttl, false);
        }

        /// <summary>
        /// Set the expiration for a key/value pair.
        /// </summary>
        /// <param name="key">The identifying key.</param>
        /// <param name="when">The absolute expiration time of the pair.</param>
        /// <returns><see langword="True"/> if an entry existed for the given key.</returns>
        public bool SetExpiration(TKey key, DateTime when) {
            var ttl = when - DateTime.UtcNow;
            return _set.SetExpiration(key, default(TValue), when, ttl, false);
        }

        /// <summary>
        /// Remove a value from the set.
        /// </summary>
        /// <param name="key">The identifying key.</param>
        /// <returns><see langword="True"/> if an entry existed for the given key.</returns>
        public bool Delete(TKey key) {
            TValue value;
            return TryDelete(key, out value);
        }

        /// <summary>
        /// Try to delete a key and receive back its old value on success.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        /// <param name="value">Output slot of the original value on successful deletion.</param>
        /// <returns><see langword="True"/> if an entry existed for the given key.</returns>
        public bool TryDelete(TKey key, out TValue value) {
            var success = _set.TryDelete(key, out value);
            return success;
        }
        /// <summary>
        /// Extend an entry's expiration by its time-to-live.
        /// </summary>
        /// <param name="key">The identifying key.</param>
        public void RefreshExpiration(TKey key) {
            _set.RefreshExpiration(key);
        }

        /// <summary>
        /// Dispose the set, releasing all entries.
        /// </summary>
        public void Dispose() {
            _set.CollectionChanged -= OnCollectionChanged;
            _set.EntriesExpired -= OnEntriesExpired;
            _set.Dispose();
        }

        private void OnEntriesExpired(object sender, ExpiringSet<TKey, TValue>.ExpiredArgs e) {
            if(EntryExpired == null) {
                return;
            }
            foreach(var entry in e.Entries) {
                EntryExpired(this, new ExpirationArgs<TKey, TValue>(Entry.FromExpiringSetEntry(entry)));
            }
        }

        private void OnCollectionChanged(object sender, EventArgs e) {
            if(CollectionChanged != null) {
                CollectionChanged(this, EventArgs.Empty);
            }
        }

        //--- IEnumerable Members ---
        IEnumerator<Entry> IEnumerable<Entry>.GetEnumerator() {
            foreach(var entry in _set.GetEntries()) {
                yield return Entry.FromExpiringSetEntry(entry);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable<Entry>)this).GetEnumerator();
        }
    }

    /// <summary>
    /// Event argument for <see cref="ExpiringDictionary{TKey,TValue}.EntryExpired"/> event.
    /// </summary>
    /// <typeparam name="TKey">Type of the key stored in the <see cref="ExpiringDictionary{TKey,TValue}"/> that fired the event.</typeparam>
    /// <typeparam name="TValue">Type of the value stored in the <see cref="ExpiringDictionary{TKey,TValue}"/> that fired the event.</typeparam>
    public class ExpirationArgs<TKey, TValue> : EventArgs {

        //--- Fields ---

        /// <summary>
        /// Entry that expired.
        /// </summary>
        public readonly ExpiringDictionary<TKey, TValue>.Entry Entry;

        //--- Constructors ---
        internal ExpirationArgs(ExpiringDictionary<TKey, TValue>.Entry entry) {
            Entry = entry;
        }
    }


}
