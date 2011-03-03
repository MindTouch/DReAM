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

namespace MindTouch.Dream {

    /// <summary>
    /// Singleton ownership scope of items resolved from a container. 
    /// </summary>
    /// <remarks>
    /// Dream uses a nested hierarchy of one host container, per service containers and per request (per service) containers.
    /// The singleton ownership and lifespan of an instance resolved at from any container will goverened by its scope,
    /// rather than the container. The only exception are <see cref="Factory"/> scoped instances which are created new on each resolution,
    /// while their lifespan is governed by the container they were resolved from.
    /// </remarks>
    public enum DreamContainerScope {

        /// <summary>
        /// Singleton belonging to the host container.
        /// </summary>
        Host,

        /// <summary>
        /// Singleton belonging to the current service's container.
        /// </summary>
        Service,

        /// <summary>
        /// Singleton belonging to the current request's container.
        /// </summary>
        Request,

        /// <summary>
        /// Per call instance.
        /// </summary>
        Factory
    }
}