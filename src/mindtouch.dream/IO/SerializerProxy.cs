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
using System.IO;

namespace MindTouch.IO {
    
    /// <summary>
    /// Provides an aggregator of serializer instances and type mappings to allow a collection of separate <see cref="ISerializer"/>
    /// instances to function as a single serializer.
    /// </summary>
    public class SerializerProxy : ISerializer {

        //--- Fields ---
        private readonly ISerializer _defaultSerializer;
        private readonly Dictionary<Type, ISerializer> _serializers;

        //--- Constructors ---

        /// <summary>
        /// Create a new proxy serializer.
        /// </summary>
        /// <param name="defaultSerializer">Serializer to use for all types without an explicit type mapping.</param>
        /// <param name="serializers">Map of types and responsible serializers.</param>
        public SerializerProxy(ISerializer defaultSerializer, Dictionary<Type, ISerializer> serializers) {
            _defaultSerializer = defaultSerializer;
            _serializers = serializers;
        }

        //--- Methods ---
        private ISerializer GetSerializer<T>() {
            ISerializer serializer;
            return _serializers.TryGetValue(typeof(T), out serializer) ? serializer : _defaultSerializer;
        }

        //--- ISerializer Members ---
        T ISerializer.Deserialize<T>(Stream stream) {
            return GetSerializer<T>().Deserialize<T>(stream);
        }

        void ISerializer.Serialize<T>(Stream stream, T data) {
            GetSerializer<T>().Serialize(stream, data);
        }

    }
}