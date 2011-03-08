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
using Autofac;
using MindTouch.Xml;

namespace MindTouch.Dream {

    /// <summary>
    /// Provides a mechanism for instantiating <see cref="IDreamService"/> instances.
    /// </summary>
    public interface IServiceActivator {

        /// <summary>
        /// Create a new <see cref="IDreamService"/> instance.
        /// </summary>
        /// <param name="config">Service configuration that will later be used to initialize the instance.</param>
        /// <param name="type">Type of the <see cref="IDreamService"/> implemntor to instantiate.</param>
        /// <returns>A service instance.</returns>
        IDreamService Create(XDoc config, Type type);
    }

    internal class DefaultServiceActivator : IServiceActivator {
        private readonly IContainer _container;

        public DefaultServiceActivator(IContainer container) {
            _container = container;
        }

        public IDreamService Create(XDoc config, Type type) {
            object service;
            if(!_container.TryResolve(type, out service, TypedParameter.From(config))) {
                service = Activator.CreateInstance(type);
            }
            return (IDreamService)service;
        }
    }
}
