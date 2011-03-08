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

using Autofac;
using Autofac.Registrars;

namespace MindTouch.Dream {

    /// <summary>
    /// Extension methods for defining Autofac registration scope
    /// </summary>
    public static class AutoFacEx {

        //--- Extension Methods ---

        /// <summary>
        /// Set the registered item's container resolution scope.
        /// </summary>
        /// <param name="registrar">Registrar instance.</param>
        /// <param name="scope">Container Resolution scope.</param>
        /// <returns>The modified registrar instance.</returns>
        public static IConcreteRegistrar InScope(this IConcreteRegistrar registrar, DreamContainerScope scope) {
            return scope == DreamContainerScope.Factory ? registrar.WithScope(InstanceScope.Factory) : registrar.InContext(scope);
        }

        /// <summary>
        /// Set the registered item's contrainer resolution scope to <see cref="DreamContainerScope.Host"/>
        /// </summary>
        /// <param name="registrar">Registrar instance.</param>
        /// <returns>The modified registrar instance.</returns>
        public static IConcreteRegistrar HostScoped(this IConcreteRegistrar registrar) {
            return registrar.InScope(DreamContainerScope.Host);
        }

        /// <summary>
        /// Set the registered item's contrainer resolution scope to <see cref="DreamContainerScope.Service"/>
        /// </summary>
        /// <param name="registrar">Registrar instance.</param>
        /// <returns>The modified registrar instance.</returns>
        public static IConcreteRegistrar ServiceScoped(this IConcreteRegistrar registrar) {
            return registrar.InScope(DreamContainerScope.Service);
        }

        /// <summary>
        /// Set the registered item's contrainer resolution scope to <see cref="DreamContainerScope.Request"/>
        /// </summary>
        /// <param name="registrar">Registrar instance.</param>
        /// <returns>The modified registrar instance.</returns>
        public static IConcreteRegistrar RequestScoped(this IConcreteRegistrar registrar) {
            return registrar.InScope(DreamContainerScope.Request);
        }

        /// <summary>
        /// Set the registered item's container resolution scope.
        /// </summary>
        /// <param name="registrar">Registrar instance.</param>
        /// <param name="scope">Container Resolution scope.</param>
        /// <returns>The modified registrar instance.</returns>
        public static IReflectiveRegistrar InScope(this IReflectiveRegistrar registrar, DreamContainerScope scope) {
            return scope == DreamContainerScope.Factory ? registrar.WithScope(InstanceScope.Factory) : registrar.InContext(scope);
        }

        /// <summary>
        /// Set the registered item's contrainer resolution scope to <see cref="DreamContainerScope.Host"/>
        /// </summary>
        /// <param name="registrar">Registrar instance.</param>
        /// <returns>The modified registrar instance.</returns>
        public static IReflectiveRegistrar HostScoped(this IReflectiveRegistrar registrar) {
            return registrar.InScope(DreamContainerScope.Host);
        }

        /// <summary>
        /// Set the registered item's contrainer resolution scope to <see cref="DreamContainerScope.Service"/>
        /// </summary>
        /// <param name="registrar">Registrar instance.</param>
        /// <returns>The modified registrar instance.</returns>
        public static IReflectiveRegistrar ServiceScoped(this IReflectiveRegistrar registrar) {
            return registrar.InScope(DreamContainerScope.Service);
        }

        /// <summary>
        /// Set the registered item's contrainer resolution scope to <see cref="DreamContainerScope.Request"/>
        /// </summary>
        /// <param name="registrar">Registrar instance.</param>
        /// <returns>The modified registrar instance.</returns>
        public static IReflectiveRegistrar RequestScoped(this IReflectiveRegistrar registrar) {
            return registrar.InScope(DreamContainerScope.Request);
        }
    }
}