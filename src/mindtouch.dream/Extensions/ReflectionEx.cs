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
using System.Reflection;

namespace MindTouch.Extensions {

    /// <summary>
    /// Provides extension methods relating to <see cref="MethodInfo"/>.
    /// </summary>
    public static class ReflectionEx {

        //--- Extension Methods ---

        /// <summary>
        /// Invokes the method or constructor represented by the current instance, using the specified parameters.  Automatically rethrows the inner exception of a <see cref="TargetInvocationException"/>.
        /// </summary>
        /// <param name="method">Method or constructor to invoke.</param>
        /// <param name="subject">The object on which to invoke the method or constructor. If a method is static, this argument is ignored. If the a constructor is static, this argument must be null.</param>
        /// <param name="arguments">An argument list for the invoked method or constructor.</param>
        /// <returns>Returns the value returned by the invoked method or constructor.</returns>
        public static object InvokeWithRethrow(this MethodInfo method, object subject, object[] arguments) {
            try {
                return method.Invoke(subject, arguments);
            } catch(TargetInvocationException e) {
                if(e.InnerException != null) {
                    throw e.InnerException.Rethrow();
                }
                throw;
            }
        }
    }
}
