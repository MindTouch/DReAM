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
using System.IO;
using System.Linq;
using System.Threading;
using MindTouch.Collections;
using MindTouch.Dream;
using MindTouch.IO;
using MindTouch.Tasking;

namespace MindTouch.Cache {


    /// <summary>
    /// Main-memory implementation of <see cref="IKeyValueCache"/>.
    /// </summary>
    /// <remarks>
    /// Even though the values cached remain in process, they are stored as serialized data not as references. Instances can be created via
    /// <see cref="KeyValueCacheFactory.Create"/>.
    /// </remarks>
    public class InMemoryKeyValueCache : IKeyValueCache {

        //--- Types ---
        private struct Entry {

            //--- Fields ---
            public readonly int Size;
            public readonly Stream Stream;

            //--- Constructors ---
            public Entry(int size, Stream stream) {
                Size = size;
                Stream = stream;
            }
        }

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly ExpiringDictionary<string, Entry> _cache;
        private readonly TaskTimer _flushTimer;
        private readonly ISerializer _serializer;
        private readonly int _maxSize;
        private readonly object _flushLock = new object();
        private int _currentSize;
        private bool _isDisposed;

        //--- Constructors ---
        internal InMemoryKeyValueCache(ISerializer serializer, int maxSize, TaskTimerFactory timerFactory) {
            _serializer = serializer;
            _maxSize = maxSize;
            _flushTimer = timerFactory.New(TimeSpan.FromSeconds(1), Flush, null, TaskEnv.None);
            _cache = new ExpiringDictionary<string, Entry>(timerFactory);
        }

        //--- Properties ---

        /// <summary>
        /// Maximum number of bytes used for cache values
        /// </summary>
        public int MemoryCapacity { get { return _maxSize; } }

        /// <summary>
        /// Number of bytes currently used by cache values
        /// </summary>
        public int MemorySize { get { return _currentSize; } }

        //--- Methods ---

        /// <summary>
        /// Manually force excess memory collection
        /// </summary>
        public void Flush() {
            if(_currentSize >= _maxSize) {
                lock(_flushLock) {
                    if(_currentSize >= _maxSize) {
                        var entries = _cache.ToArray();

                        // first delete all the ones without a timeout
                        foreach(var entry in entries.Where(x => x.TTL > TimeSpan.Zero).OrderBy(x => x.When)) {
                            Delete(entry.Key);

                            // Note (arnec): when we do memory clean-up we try to get at least 10% head room, so we don't constantly run into Flush churn
                            if(_currentSize < _maxSize * 0.9) {
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void Flush(TaskTimer flushTimer) {
            if(_isDisposed) {
                return;
            }
            Flush();
            flushTimer.Change(TimeSpan.FromSeconds(1), TaskEnv.None);
        }

        /// <summary>
        /// Delete a value from the cache.
        /// </summary>
        /// <param name="key">Key to identify the value by.</param>
        /// <returns><see langword="True"/> if the value was deleted.</returns>
        public bool Delete(string key) {
            Entry entry;
            var success = _cache.TryDelete(key, out entry);
            if(success) {
                Interlocked.Add(ref _currentSize, -entry.Size);
            }
            return success;
        }

        /// <summary>
        /// Try to retrieve a value from the cache.
        /// </summary>
        /// <typeparam name="T">Type the value to be returned.</typeparam>
        /// <param name="key">Key to identify the value by.</param>
        /// <param name="value">Output slot for value, if it was retrieved. Must be deserializable by the cache.</param>
        /// <returns><see langword="True"/> if the value was returned.</returns>
        public bool TryGet<T>(string key, out T value) {
            var entry = _cache[key];
            if(entry == null) {
                value = default(T);
                return false;
            }
            entry.Value.Stream.Position = 0;
            value = _serializer.Deserialize<T>(entry.Value.Stream);
            return true;
        }

        /// <summary>
        /// Add a value to the cache.
        /// </summary>
        /// <typeparam name="T">Type the value to be stored.</typeparam>
        /// <param name="key">Key to identify the value by.</param>
        /// <param name="value">Value to be stored. Must be serializable by the cache.</param>
        /// <param name="ttl">Maximum time for the value to live in the cache.</param>
        public void Set<T>(string key, T value, TimeSpan ttl) {
            if(ttl == TimeSpan.MinValue) {
                ttl = TimeSpan.MaxValue;
            }
            Stream stream = new ChunkedMemoryStream();
            _serializer.Serialize(stream, value);
            int size = (int)stream.Length;
            Entry oldValue;
            int oldSize = 0;
            if(_cache.TrySet(key, new Entry(size, stream), ttl, out oldValue)) {
                oldSize = oldValue.Size;
            }

            // Meh, race condition... does it matter?
            Interlocked.Add(ref _currentSize, size - oldSize);
        }

        /// <summary>
        /// Clear the entire cache immediately.
        /// </summary>
        public void Clear() {
            _cache.Clear();
        }

        /// <summary>
        /// Dispose all values currently in the cache.
        /// </summary>
        public void Dispose() {
            _isDisposed = true;
            _flushTimer.Change(TimeSpan.MaxValue, TaskEnv.None);
            _cache.Dispose();
        }

    }
}
