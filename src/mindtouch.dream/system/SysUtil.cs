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

using System.Globalization;
using System.Reflection;
using System.Xml;
using MindTouch.Collections;
using MindTouch.Dream;

namespace System {

    /// <summary>
    /// Static utility class containing base Class library level extension and helper methods as well as System properties.
    /// </summary>
    public static class SysUtil {

        //--- Class Fields ---

        /// <summary>
        /// Global-use XmlNameTable
        /// </summary>
        public static readonly XmlNameTable NameTable;

        /// <summary>
        /// Global switch for determining whether Async I/O should be used when calling Asychronous Method Pattern I/O methods.
        /// </summary>
        /// <remarks>
        /// The value of this field is set by the "async-io" configuration key, if present.
        /// Mono does not implement asychronous I/O, instead creating blocking threads in the default .NET threadpool for performaing "async" work.
        /// By default, if the configuration key is not set and <see cref="IsMono"/> is defined, <see cref="UseAsyncIO"/> is set to <see langword="False"/>,
        /// otherwise it is <see langword="True"/>.
        /// </remarks>
        public static readonly bool UseAsyncIO = true;

        private static readonly bool _isMono;
        private static Converter<Exception, Exception> _prepareExceptionForRethrow;

        //--- Class Constructor ---
        static SysUtil() {
            _isMono = (Type.GetType("Mono.Runtime") != null);
            if(!bool.TryParse(System.Configuration.ConfigurationManager.AppSettings["async-io"], out UseAsyncIO)) {
                UseAsyncIO = !_isMono;
            }

            // intiailzie xml nametable
            int capacity;
            if(int.TryParse(System.Configuration.ConfigurationManager.AppSettings["xmlnametable-capacity"], out capacity)) {
                NameTable = new LockFreeXmlNameTable(capacity);
            } else {

                // use default capacity for nametable
                NameTable = new LockFreeXmlNameTable();
            }
        }

        //--- Class Properties ---

        /// <summary>
        /// <see langword="True"/> if the program is running under the Mono runtime.
        /// </summary>
        public static bool IsMono { get { return _isMono; } }

        /// <summary>
        /// <see langword="True"/> if the program is running on a Unix platform.
        /// </summary>
        public static bool IsUnix {
            get {

                // NOTE: taken from http://www.mono-project.com/FAQ:_Technical#Compatibility

                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 128);
            }
        }

        //--- Extension Methods ---

        /// <summary>
        /// Checks if an object is of a given type or interface.
        /// </summary>
        /// <typeparam name="T">Type or interface to check.</typeparam>
        /// <param name="instance">Object to check.</param>
        /// <returns><see langword="True"/> if the object implements the interface or derives from type.</returns>
        public static bool IsA<T>(this object instance) {
            return instance == null ? false : IsA<T>(instance.GetType());
        }

        /// <summary>
        /// Checks if the type is of a given type or interface.
        /// </summary>
        /// <typeparam name="T">Type or interface to check.</typeparam>
        /// <param name="type">Type to check.</param>
        /// <returns><see langword="True"/> if the Type implements the interface or derives from type.</returns>
        public static bool IsA<T>(this Type type) {
            if(type == null) {
                return false;
            }
            var t = typeof(T);
            return t.IsAssignableFrom(type);
        }

        /// <summary>
        /// Rethrow an existing exception.
        /// </summary>
        /// <remarks>
        /// Rethrow claims to return an exception so that it can be used as the argument to <see langword="throw"/> in a <see langword="try"/> block.
        /// This usage allows the compiler to see that the code cannot return from the block calling Rethrow, which is otherwise not discoverable.
        /// </remarks>
        /// <param name="exception">Exception to rethrow.</param>
        /// <returns>This method will never return an exception, since it always throws internally.</returns>
        public static Exception Rethrow(this Exception exception) {
            if(exception == null) {
                throw new ArgumentNullException("exception");
            }

            // check if we need to first create the runtime-appropriate delegate to preserve the stack-trace for a rethrow.
            if(_prepareExceptionForRethrow == null) {

                // Hack the stack trace so it appears to have been preserved.
                // If we can't hack the stack trace, then there's not much we can do as anything we
                // choose will alter the semantics of test execution.
                if(IsMono) {
                    MethodInfo method = typeof(Exception).GetMethod("FixRemotingException", BindingFlags.Instance | BindingFlags.NonPublic);
                    _prepareExceptionForRethrow = delegate(Exception e) { return (Exception)method.Invoke(e, null); };
                } else {
                    MethodInfo method = typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);
                    _prepareExceptionForRethrow = delegate(Exception e) {
                        method.Invoke(e, null);
                        return e;
                    };
                }

                // just in case the internal methods name have changed!
                if(_prepareExceptionForRethrow == null) {
                    _prepareExceptionForRethrow = delegate(Exception e) { return e; };
                }
            }

            // check if there is a stack-trace to preserve
            if(exception.StackTrace != null) {
                throw _prepareExceptionForRethrow(exception);
            } else {
                throw exception;
            }
        }

        //--- Class Methods ---

        /// <summary>
        /// Wrapper on top of <see cref="Convert.ChangeType(object,System.Type)"/> to add handling of MindTouch Dream base types.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="type">Type to convert value into.</param>
        /// <returns>Value converted to type, if possible.</returns>
        /// <exception cref="InvalidCastException"></exception>
        public static object ChangeType(object value, Type type) {
            if(type == null) {
                throw new ArgumentNullException("type");
            }

            // check if this is a noop
            if(type == typeof(object)) {
                return value;
            }

            // check if target type is real number, if so use culture-invariant parsing
            switch(Type.GetTypeCode(type)) {
            case TypeCode.Single:
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            case TypeCode.Double:
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            case TypeCode.Decimal:
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }

            // check if target type is nullable
            Type nullableType = Nullable.GetUnderlyingType(type);
            if(nullableType != null) {
                if(value == null) {
                    return null;
                }
                type = nullableType;
            }

            // check if type is enum and value is a value type
            if(type.IsEnum) {
                var valueType = value.GetType();
                if(valueType.IsValueType && (value is Byte || value is Int32 || value is SByte || value is Int16 || value is Int64 || value is UInt16 || value is UInt32 || value is UInt64)) {
                    return Enum.ToObject(type, value);
                }
            }

            // check if value is string
            if(value is string) {
                if(type == typeof(XUri)) {

                    // target type is XUri
                    XUri result = XUri.TryParse((string)value);
                    if(result == null) {
                        throw new InvalidCastException();
                    }
                    return result;
                }
                if(type.IsEnum) {

                    // target type is enum
                    try {
                        return Enum.Parse(type, (string)value, true);
                    } catch {
                        throw new InvalidCastException();
                    }
                }
            } else if((value is XUri) && (type == typeof(string))) {
                return value.ToString();
            }

            // convert type
            return Convert.ChangeType(value, type);
        }

        /// <summary>
        /// Generic version of <see cref="ChangeType"/> to allow type change without having to cast the output to the desired type.
        /// </summary>
        /// <typeparam name="T">Type to convert the value into.</typeparam>
        /// <param name="value">Value to convert.</param>
        /// <returns>Converted value, if possible.</returns>
        /// <exception cref="InvalidCastException"></exception>
        public static T ChangeType<T>(object value) {
            return (T)ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Excecute an atomic compare and exchange operation and determine whether it succeeded or not.
        /// </summary>
        /// <typeparam name="T">Type of value do perform compare and exchange on. Must be a class.</typeparam>
        /// <param name="location">The reference location of the value to be changed.</param>
        /// <param name="oldValue">The current value to expect for the swap to succeed.</param>
        /// <param name="newValue">The new value to swap into place, as long as location is still equal to the old value.</param>
        /// <returns><see langword="True"/>If the operation successfully replaced old value with new value.</returns>
        public static bool CAS<T>(ref T location, T oldValue, T newValue) where T : class {
            return ReferenceEquals(System.Threading.Interlocked.CompareExchange(ref location, newValue, oldValue), oldValue);
        }

        /// <summary>
        /// Try to parse a case-insensitive string into an enum value.
        /// </summary>
        /// <typeparam name="T">Enum type to convert into.</typeparam>
        /// <param name="text">Text value to parse.</param>
        /// <param name="value">Parsed enum value.</param>
        /// <returns><see langword="True"/> if an enum value could be parsed.</returns>
        public static bool TryParseEnum<T>(string text, out T value) {
            try {
                value = (T)Enum.Parse(typeof(T), text, true);
                return true;
            } catch {
                value = default(T);
                return false;
            }
        }

        /// <summary>
        /// Try to parse a case-insensitive string into an enum value.
        /// </summary>
        /// <typeparam name="T">Enum type to convert into.</typeparam>
        /// <param name="text">Text value to parse.</param>
        /// <returns>Parsed enum value or null.</returns>
        public static T? TryParseEnum<T>(string text) where T : struct {
            try {
                return (T)Enum.Parse(typeof(T), text, true);
            } catch {
                return null;
            }
        }

        /// <summary>
        /// Parse a case-insensitive string into an enum value.
        /// </summary>
        /// <typeparam name="T">Enum type to convert into.</typeparam>
        /// <param name="text">Text value to parse.</param>
        /// <returns>Parsed enum value.</returns>
        public static T ParseEnum<T>(string text) {
            return (T)Enum.Parse(typeof(T), text, true);
        }
    }
}