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

namespace MindTouch.Cache {

    /// <summary>
    /// Provides extension methods to attach additional getters and setter to any implemenation of <see cref="IKeyValueCache"/>.
    /// </summary>
    public static class KeyValueCacheEx {

        //--- Extension Methods ---

        /// <summary>
        /// Retrieve a value from the cache.
        /// </summary>
        /// <typeparam name="T">Type the value to be returned.</typeparam>
        /// <param name="cache">The cache instance to operate on.</param>
        /// <param name="key">Key to identify the value by.</param>
        /// <returns>The cached value or <see langword="null"/> if the value is not in cache.</returns>
        public static T Get<T>(this IKeyValueCache cache, string key) {
            T ret;
            cache.TryGet(key, out ret);
            return ret;
        }

        /// <summary>
        /// Retrieve a value from the cache.
        /// </summary>
        /// <typeparam name="T">Type the value to be returned.</typeparam>
        /// <param name="cache">The cache instance to operate on.</param>
        /// <param name="key">Key to identify the value by.</param>
        /// <param name="def"></param>
        /// <returns>The cached value or the method's default value if the value is not in cache.</returns>
        public static T Get<T>(this IKeyValueCache cache, string key, T def) {
            T ret;
            return cache.TryGet(key, out ret) ? ret : def;
        }

        /// <summary>
        /// Store a value in the cache.
        /// </summary>
        /// <remarks>
        /// This method will set the value and leave expiration up to the cache implementation.
        /// </remarks>
        /// <typeparam name="T">Type the value to be stored.</typeparam>
        /// <param name="cache">The cache instance to operate on.</param>
        /// <param name="key">Key to identify the value by.</param>
        /// <param name="val">Value to be stored.</param>
        public static void Set<T>(this IKeyValueCache cache, string key, T val) {
            cache.Set(key, val, TimeSpan.MinValue);
        }

        /// <summary>
        /// Store a value in the cache.
        /// </summary>
        /// <typeparam name="T">Type the value to be stored.</typeparam>
        /// <param name="cache">The cache instance to operate on.</param>
        /// <param name="key">Key to identify the value by.</param>
        /// <param name="val">Value to be stored.</param>
        /// <param name="expires">Absolute time at which the value should no longer be accessible from the cache.</param>
        public static void Set<T>(this IKeyValueCache cache, string key, T val, DateTime expires) {
            cache.Set(key, val, expires.ToSafeUniversalTime().Subtract(DateTime.UtcNow));
        }
    }
}