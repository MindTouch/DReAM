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
    /// Represents a set of values with an expiration time.
    /// </summary>
    /// <remarks>
    /// Values inserted into this this set will expire after some set time (which may be updated and reset), automatically removing the value
    /// from the set and firing a notification event <see cref="EntryExpired"/>. The set may optionally be configured to extend a value's
    /// expiration on access, including accessing the entry via the instances iterator.
    /// </remarks>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class ExpiringHashSet<T> : IEnumerable<ExpiringHashSet<T>.Entry>, IDisposable {

        //--- Types ---

        /// <summary>
        /// A wrapper class containing meta data about values stored in the <see cref="ExpiringHashSet{T}"/>.
        /// </summary>
        /// <remarks>
        /// The meta-data is created on demand, i.e. it will not reflect changes in <see cref="When"/> or <see cref="TTL"/> that happen
        /// after it is retrieved from the set.
        /// </remarks>
        public class Entry {

            //--- Class Methods ---
            internal static Entry FromExpiringSetEntry(ExpiringSet<T,T>.Entry entry) {
                return new Entry(entry.Value, entry.When, entry.TTL);
            }

            //--- Fields ---

            /// <summary>
            /// The absolute time at which the entry will expire
            /// </summary>
            public readonly DateTime When;

            /// <summary>
            /// The time-to-live of the entry at insertion time
            /// </summary>
            public readonly TimeSpan TTL;

            /// <summary>
            /// The value stored in the set
            /// </summary>
            public readonly T Value;

            //--- Constructors ---
            private Entry(T value, DateTime when, TimeSpan ttl) {
                Value = value;
                When = when;
                TTL = ttl;
            }
        }

        //--- Events ---

        /// <summary>
        /// Fired for every entry that expires.
        /// </summary>
        public event EventHandler<ExpirationEventArgs<T>> EntryExpired;

        /// <summary>
        /// Fired any time a value is added, removed or experiation is changed.
        /// </summary>
        public event EventHandler CollectionChanged;

        //--- Fields ---
        private readonly ExpiringSet<T,T> _set;

        //--- Constructors ---

        /// <summary>
        /// Create a new hashset
        /// </summary>
        /// <param name="taskTimerFactory">The timer factory to create the set's timer from</param>
        public ExpiringHashSet(TaskTimerFactory taskTimerFactory) : this(taskTimerFactory,false) { }

        /// <summary>
        /// Create a new hashset
        /// </summary>
        /// <param name="taskTimerFactory">The timer factory to create the set's timer from</param>
        /// <param name="autoRefresh"><see langword="True"/> if accessing an entry should extend the expiration time by the time-to-live</param>
        public ExpiringHashSet(TaskTimerFactory taskTimerFactory, bool autoRefresh) {
            _set = new ExpiringSet<T, T>(taskTimerFactory, autoRefresh);
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
        /// <param name="value">Set value.</param>
        /// <returns>Meta-data for specified value. Returns <see langword="null"/> if the value is not stored in the set.</returns>
        public Entry this[T value] {
            get {
                var entry = _set[value];
                return entry == null ? null : Entry.FromExpiringSetEntry(entry);
            }
        }


        //--- Methods ---

        /// <summary>
        /// Add or update the expiration for an item
        /// </summary>
        /// <param name="value">The value in the set</param>
        /// <param name="ttl">The time-to-live from right now</param>
        public void SetExpiration(T value, TimeSpan ttl) {
            var when = DateTime.UtcNow + ttl;
            _set.SetExpiration(value, value, when, ttl, true);
        }

        /// <summary>
        /// Add or update the expiration for an item
        /// </summary>
        /// <param name="value">The value in the set</param>
        /// <param name="when">The absolute expiration time of the item</param>
        public void SetExpiration(T value, DateTime when) {
            var ttl = when - DateTime.UtcNow;
            _set.SetExpiration(value, value, when, ttl, true);
        }

        /// <summary>
        /// Add or update the expiration for an item
        /// </summary>
        /// <remarks>
        /// Thie overload takes both the when and ttl. The <paramref name="when"/> is authoritative for expiration,
        /// but <paramref name="ttl"/> is used for <see cref="RefreshExpiration"/>
        /// </remarks>
        /// <param name="value"></param>
        /// <param name="when"></param>
        /// <param name="ttl"></param>
        public void SetExpiration(T value, DateTime when, TimeSpan ttl) {
            _set.SetExpiration(value, value, when, ttl, true);
        }

        /// <summary>
        /// Obsolete: The method <see cref="RemoveExpiration"/> has been deprecated. Use <see cref="Delete"/> instead.
        /// </summary>
        /// <param name="value"></param>
        [Obsolete("The method RemoveExpiration(T value) has been deprecated. Use Delete(T value) instead.")]
        public void RemoveExpiration(T value) {
            Delete(value);
        }

        /// <summary>
        /// Remove a value from the set.
        /// </summary>
        /// <param name="value">Value to remove</param>
        /// <returns><see langword="True"/> if an entry existed for the given key.</returns>
        public bool Delete(T value) {
            T oldValue;
            return _set.TryDelete(value, out oldValue);
        }

        /// <summary>
        /// Extend an entry's expiration by its time-to-live.
        /// </summary>
        /// <param name="value">The value in the set</param>
        public void RefreshExpiration(T value) {
            _set.RefreshExpiration(value);
        }

        /// <summary>
        /// Clear the entire set immediately
        /// </summary>
        public void Clear() {
            _set.Clear();
        }

        /// <summary>
        /// Dispose the set, releasing all entries.
        /// </summary>
        public void Dispose() {
            _set.CollectionChanged -= OnCollectionChanged;
            _set.EntriesExpired -= OnEntriesExpired;
            _set.Dispose();
        }

        private void OnEntriesExpired(object sender, ExpiringSet<T, T>.ExpiredArgs e) {
            if(EntryExpired == null) {
                return;
            }
            foreach(var entry in e.Entries) {
                EntryExpired(this, new ExpirationEventArgs<T>(Entry.FromExpiringSetEntry(entry)));
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
    /// Event argument for <see cref="ExpiringHashSet{T}.EntryExpired"/> event.
    /// </summary>
    /// <typeparam name="T">Type of the value stored in the <see cref="ExpiringHashSet{T}"/> that fired the event.</typeparam>
    public class ExpirationEventArgs<T> : EventArgs {

        //--- Fields ---

        /// <summary>
        /// Entry that expired.
        /// </summary>
        public readonly ExpiringHashSet<T>.Entry Entry;

        //--- Constructors ----
        internal ExpirationEventArgs(ExpiringHashSet<T>.Entry entry) {
            Entry = entry;
        }
    }


}
