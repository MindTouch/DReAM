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

using MindTouch.Tasking;

namespace MindTouch.Cache {

    /// <summary>
    /// Provides a factory for the <see cref="InMemoryKeyValueCache"/> implementation of <see cref="IKeyValueCache"/>.
    /// </summary>
    public class InMemoryKeyValueCacheFactory : KeyValueCacheFactory {

        //--- Constants ---

        /// <summary>
        /// Default maximum memory size (in bytes) for the cache: 10MB.
        /// </summary>
        public const int DEFAULT_MAX_SIZE = 10 * 1024 * 1024;


        //--- Constructors ---

        /// <summary>
        /// Create a new factory instance.
        /// </summary>
        /// <param name="timer">Timer factory to provide to the cache instance.</param>
        public InMemoryKeyValueCacheFactory(TaskTimerFactory timer) : this(DEFAULT_MAX_SIZE, timer) { }
        
        /// <summary>
        /// Create a new factory instance.
        /// </summary>
        /// <param name="maxSize">Maximum number of bytes of memory the cache should use for stored items</param>
        /// <param name="timer">Timer factory to provide to the cache instance.</param>
        public InMemoryKeyValueCacheFactory(int maxSize, TaskTimerFactory timer) : base(timer, (s, t) => new InMemoryKeyValueCache(s, maxSize, t)) { }


    }
}