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

namespace MindTouch.Dream {

    /// <summary>
    /// Provides Dream specific extension methods for <see cref="XUri"/>.
    /// </summary>
    public static class XUriEx {

        //--- Extension Methods ---

        /// <summary>
        /// Get a typed parameter from the Uri.
        /// </summary>
        /// <typeparam name="T">Type of the parameter.</typeparam>
        /// <param name="uri">Input Uri.</param>
        /// <param name="key">Parameter key.</param>
        /// <param name="def">Default value to return in case parameter does not exist.</param>
        /// <returns>Parameter value or default.</returns>
        public static T GetParam<T>(this XUri uri, string key, T def) {
            return GetParam<T>(uri, key, 0, def);
        }

        /// <summary>
        /// Get a typed parameter from the Uri.
        /// </summary>
        /// <typeparam name="T">Type of the parameter.</typeparam>
        /// <param name="uri">Input Uri.</param>
        /// <param name="key">Parameter key.</param>
        /// <param name="index">Parameter index.</param>
        /// <param name="def">Default value to return in case parameter does not exist.</param>
        /// <returns>Parameter value or default.</returns>
        public static T GetParam<T>(this XUri uri, string key, int index, T def) {
            string value = uri.GetParam(key, index, null);
            if(!string.IsNullOrEmpty(value)) {
                return (T)SysUtil.ChangeType(value, typeof(T));
            }
            return def;
        }

        /// <summary>
        /// Translate Uri to request context sensitive public uri.
        /// </summary>
        /// <param name="uri">Input uri.</param>
        /// <returns>Translated Uri or original instance, should translation not be possible.</returns>
        public static XUri AsPublicUri(this XUri uri) {
            var current = DreamContext.CurrentOrNull;
            if(current != null) {
                return current.AsPublicUri(uri);
            }
            return uri;
        }

        /// <summary>
        /// Translate Uri to request context sensitive local uri.
        /// </summary>
        /// <param name="uri">Input uri.</param>
        /// <returns>Translated Uri or original instance, should translation not be possible.</returns>
        public static XUri AsLocalUri(this XUri uri) {
            var current = DreamContext.CurrentOrNull;
            if(current != null) {
                return current.AsLocalUri(uri);
            }
            return uri;
        }

        /// <summary>
        /// Translate Uri to request context sensitive server relateive uri.
        /// </summary>
        /// <param name="uri">Input uri.</param>
        /// <returns>Translated Uri or original instance, should translation not be possible.</returns>
        public static XUri AsServerUri(this XUri uri) {
            var current = DreamContext.CurrentOrNull;
            if(current != null) {
                return current.AsServerUri(uri);
            }
            return uri;
        }
    }
}
