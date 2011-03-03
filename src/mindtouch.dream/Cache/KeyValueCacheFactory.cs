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
using System.Linq;
using System.Text;
using MindTouch.IO;
using MindTouch.Tasking;

namespace MindTouch.Cache {

    /// <summary>
    /// Provides a common <see cref="IKeyValueCacheFactory"/> that can be used for subclassing or directly by instantiating it with an
    /// appropriate instance creation function.
    /// </summary>
    /// <remarks>
    /// <see cref="IKeyValueCache"/> are created via a creation function that is provided the factory's <see cref="TaskTimerFactory"/> and
    /// an <see cref="ISerializer"/> instance that encapsulates all registered serializer via the <see cref="SerializerProxy"/> aggregator,
    /// allowing the <see cref="IKeyValueCache"/> implementation to function against a single serializer without having to determine the right
    /// serializer for a given type.
    /// </remarks>
    public class KeyValueCacheFactory : IKeyValueCacheFactory {

        //--- Fields ---
        private readonly TaskTimerFactory _timer;
        private readonly Func<ISerializer, TaskTimerFactory, IKeyValueCache> _factoryMethod;
        private readonly Dictionary<Type, ISerializer> _serializers = new Dictionary<Type, ISerializer>();

        // TODO (arnec): build a serializer that can serialize basic types and only falls back on BinaryFormatter on type miss
        private ISerializer _defaultSerializer = new BinaryFormatterSerializer();

        //--- Constructors ---
        
        /// <summary>
        /// Create a new factory instance.
        /// </summary>
        /// <param name="timer">The timer factory to be provided to cache instances.</param>
        /// <param name="factoryMethod">A function for creating a new cache instance from an <see cref="ISerializer"/> instance and timer factory.</param>
        public KeyValueCacheFactory(TaskTimerFactory timer, Func<ISerializer, TaskTimerFactory, IKeyValueCache> factoryMethod) {
            _timer = timer;
            _factoryMethod = factoryMethod;
        }

        //--- Properties ---

        /// <summary>
        /// All serializers registered with this factory.
        /// </summary>
        public IEnumerable<Tuplet<ISerializer, Type>> Serializers {
            get {
                foreach(var kvp in _serializers) {
                    yield return new Tuplet<ISerializer, Type>(kvp.Value, kvp.Key);
                }
                yield return new Tuplet<ISerializer, Type>(_defaultSerializer, typeof(object));
            }
        }

        /// <summary>
        /// Default serializer to fire for all types lacking an explicit serializer.
        /// </summary>
        /// <remarks>
        /// At creation time, this is set to an instance of <see cref="BinaryFormatterSerializer"/>, providing the ability to
        /// serialize any <see cref="SerializableAttribute"/> marked instances.
        /// </remarks>
        public ISerializer DefaultSerializer {
            get { return _defaultSerializer; }
            set {
                if(value == null) {
                    throw new ArgumentNullException();
                }
                _defaultSerializer = value;
            }
        }

        //--- Methods ---

        /// <summary>
        /// Register a serializer for a specific type.
        /// </summary>
        /// <typeparam name="T">Type the serializer is responsible for.</typeparam>
        /// <param name="serializer">A serializer instance.</param>
        public void SetSerializer<T>(ISerializer serializer) {
            _serializers[typeof(T)] = serializer;
        }

        /// <summary>
        /// Register a serializer for a specific type.
        /// </summary>
        /// <param name="serializer">A serializer instance.</param>
        /// <param name="type">Type the serializer is responsible for.</param>
        public void SetSerializer(ISerializer serializer, Type type) {
            _serializers[type] = serializer;
        }

        /// <summary>
        /// Remove a serializer for a specific type.
        /// </summary>
        /// <typeparam name="T">Type the serializer is responsible for.</typeparam>
        /// <returns><see langword="True"/> if a serializer for the specified type was registered.</returns>
        public bool RemoveSerializer<T>() {
            return _serializers.Remove(typeof(T));
        }

        /// <summary>
        /// Remove a serializer for a specific type.
        /// </summary>
        /// <param name="type">Type the serializer is responsible for.</param>
        /// <returns><see langword="True"/> if a serializer for the specified type was registered.</returns>
        public bool RemoveSerializer(Type type) {
            return _serializers.Remove(type);
        }

        /// <summary>
        /// Create a new <see cref="IKeyValueCache"/> instance.
        /// </summary>
        /// <returns>A new cache instance.</returns>
        public IKeyValueCache Create() {
            return _factoryMethod(new SerializerProxy(_defaultSerializer, _serializers), _timer);
        }
    }
}
