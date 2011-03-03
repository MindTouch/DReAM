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
using MindTouch.IO;

namespace MindTouch.Cache {

    /// <summary>
    /// Common contract for <see cref="IKeyValueCache"/> factories. Meant to unify creation of cache instances and serializer registration.
    /// </summary>
    /// <remarks>
    /// See <see cref="KeyValueCacheFactory"/> as a common base for building a factory, although it can also be used for construction of
    /// <see cref="IKeyValueCache"/> implementation without subtyping it.
    /// </remarks>
    public interface IKeyValueCacheFactory {

        //--- Properties ---

        /// <summary>
        /// All serializers registered with this factory.
        /// </summary>
        IEnumerable<Tuplet<ISerializer,Type>> Serializers { get; }

        /// <summary>
        /// Default serializer to fire for all types lacking an explicit serializer.
        /// </summary>
        ISerializer DefaultSerializer { get; set; }

        //--- Methods ---
        
        /// <summary>
        /// Register a serializer for a specific type.
        /// </summary>
        /// <typeparam name="T">Type the serializer is responsible for.</typeparam>
        /// <param name="serializer">A serializer instance.</param>
        void SetSerializer<T>(ISerializer serializer);

        /// <summary>
        /// Register a serializer for a specific type.
        /// </summary>
        /// <param name="serializer">A serializer instance.</param>
        /// <param name="type">Type the serializer is responsible for.</param>
        void SetSerializer(ISerializer serializer, Type type);

        /// <summary>
        /// Remove a serializer for a specific type.
        /// </summary>
        /// <typeparam name="T">Type the serializer is responsible for.</typeparam>
        /// <returns><see langword="True"/> if a serializer for the specified type was registered.</returns>
        bool RemoveSerializer<T>();

        /// <summary>
        /// Remove a serializer for a specific type.
        /// </summary>
        /// <param name="type">Type the serializer is responsible for.</param>
        /// <returns><see langword="True"/> if a serializer for the specified type was registered.</returns>
        bool RemoveSerializer(Type type);

        /// <summary>
        /// Create a new <see cref="IKeyValueCache"/> instance.
        /// </summary>
        /// <remarks>
        /// The cache is assumed to be provided all registered serializers and their type mappings. It is also assumed that serializers
        /// added to the factory after the instance is created are not visible to the instance.
        /// </remarks>
        /// <returns>A new cache instance.</returns>
        IKeyValueCache Create();
    }
}