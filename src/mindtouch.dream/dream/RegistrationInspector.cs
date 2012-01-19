/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2010 MindTouch, Inc.
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
using Autofac;

namespace MindTouch.Dream {

    /// <summary>
    /// This type is used by <see cref="DreamService.InitializeLifetimeScope"/> to let the service determine whether a type is already registered
    /// for it's <see cref="ILifetimeScope"/>
    /// </summary>
    public interface IRegistrationInspector {

        //--- Methods ---

        /// <summary>
        /// Check if a type is already registered
        /// </summary>
        /// <typeparam name="T">Type to check</typeparam>
        /// <returns>True if the type is already registered</returns>
        bool IsRegistered<T>();

        /// <summary>
        /// Check if a type is already registered
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if the type is already registered</returns>
        bool IsRegistered(Type type);
    }
    
    internal class RegistrationInspector : IRegistrationInspector {
        
        //--- Fields ---
        private readonly IContainer _container;
        private readonly HashSet<Type> _knownTypes = new HashSet<Type>();

        //--- Constructors ---
        public RegistrationInspector(IContainer container) {
            _container = container;
        }

        //--- Methods ---
        public bool IsRegistered<T>() {
            return IsRegistered(typeof(T));
        }

        public bool IsRegistered(Type type) {
            return _knownTypes.Contains(type) || _container.IsRegistered(type);
        }

        public void Register(IEnumerable<Type> types) {
            foreach(var type in types) {
                _knownTypes.Add(type);
            }
        }
    }
}