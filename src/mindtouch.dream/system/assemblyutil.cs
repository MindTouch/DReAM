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

using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace System {

    /// <summary>
    /// Utility class containing Extension methods for inspecting Assemblies.
    /// </summary>
    public static class AssemblyUtil {

        //--- Constants ---
        private const int PE_HEADER_OFFSET = 60;
        private const int LINKER_TIMESTAMP_OFFSET = 8;

        //--- Class Fields ---
        private static readonly DateTime _date1970 = new DateTime(1970, 1, 1, 0, 0, 0);
        private static readonly Dictionary<string, DateTime> _buildDateCache = new Dictionary<string, DateTime>();

        //--- Extension Methods ---

        /// <summary>
        /// Get the build date of an assembly by inspecting the assembly file header.
        /// </summary>
        /// <param name="assembly">Assembly to inspect</param>
        /// <returns><see cref="DateTime"/> when then assembly was built.</returns>
        public static DateTime GetBuildDate(this Assembly assembly) {
            try {
                DateTime buildDate;
                if(!_buildDateCache.TryGetValue(assembly.Location, out buildDate)) {
                    byte[] b = new byte[2048];
                    using(Stream s = new FileStream(assembly.Location, FileMode.Open, FileAccess.Read)) {
                        s.Read(b, 0, 2048);
                    }
                    int i = BitConverter.ToInt32(b, PE_HEADER_OFFSET);
                    int secondsSince1970 = BitConverter.ToInt32(b, i + LINKER_TIMESTAMP_OFFSET);
                    buildDate = _date1970.AddSeconds(secondsSince1970);
                    _buildDateCache[assembly.Location] = buildDate;
                }
                return buildDate;
            } catch {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Get an assembly attribute by type.
        /// </summary>
        /// <typeparam name="T">Type of the assembly attribute to retrieve.</typeparam>
        /// <param name="assembly">Assembly to inspect.</param>
        /// <returns>Instance of the specified assembly attribute, or null if the attribute is not found.</returns>
        public static T GetAttribute<T>(this Assembly assembly) where T : Attribute {
            object[] attr = assembly.GetCustomAttributes(typeof(T), false);
            if(attr == null || attr.Length == 0) {
                return null;
            }
            return (T)attr[0];
        }
    }
}
