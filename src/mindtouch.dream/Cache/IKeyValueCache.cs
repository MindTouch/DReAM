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
using MindTouch.IO;

namespace MindTouch.Cache {

    /// <summary>
    /// Public contract for key/value cache's to be offered by Dream.
    /// </summary>
    /// <remarks>
    /// <p>It should be excepted that any value stored in the <see cref="IKeyValueCache"/> may be discarded at any time. A Time-to-live
    /// does not provide any guarantee that the value will remain in the cache that long, it merely means that after that time the value
    /// will not be returned by the cache.</p>
    /// <p>
    /// All values stored in the cache are stored as binary copies, not as references. The cache will never hold on to a reference. This does
    /// mean that all values need to be serializable by the cache. It is is assumed that cache's are constructed via an implementation of 
    /// <see cref="IKeyValueCacheFactory"/>, which provides a mechanism for registering <see cref="ISerializer"/> instances for this purpose.
    /// </p>
    /// </remarks>
    public interface IKeyValueCache : IDisposable {

        //--- Methods ---

        /// <summary>
        /// Delete a value from the cache.
        /// </summary>
        /// <param name="key">Key to identify the value by.</param>
        /// <returns><see langword="True"/> if the value was deleted.</returns>
        bool Delete(string key);

        /// <summary>
        /// Try to retrieve a value from the cache.
        /// </summary>
        /// <typeparam name="T">Type the value to be returned.</typeparam>
        /// <param name="key">Key to identify the value by.</param>
        /// <param name="value">Output slot for value, if it was retrieved. Must be deserializable by the cache.</param>
        /// <returns><see langword="True"/> if the value was returned.</returns>
        bool TryGet<T>(string key, out T value);

        /// <summary>
        /// Add a value to the cache.
        /// </summary>
        /// <remarks>
        /// The time-to-live only guarantees that the value will no longer be retrievable after the specified time. It doesn not mean that the
        /// value is guaranteed to be in the cache for the specified time. A ttl of <see cref="TimeSpan.MinValue"/> leaves disposal of
        /// the value up to the cache implementation.
        /// </remarks>
        /// <typeparam name="T">Type the value to be stored.</typeparam>
        /// <param name="key">Key to identify the value by.</param>
        /// <param name="value">Value to be stored. Must be serializable by the cache.</param>
        /// <param name="ttl">Maximum time-to-live for the value in the cache. A ttl of <see cref="TimeSpan.MinValue"/> leaves disposal of
        /// the value up to the cache implementation.</param>
        void Set<T>(string key, T value, TimeSpan ttl);

        /// <summary>
        /// Clear the cache. 
        /// </summary>
        /// <remarks>Not guaranteed to be atomic.</remarks>
        void Clear();
    }
}