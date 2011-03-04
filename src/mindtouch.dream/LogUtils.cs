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
using System.Collections;
using System.IO;
using System.Text;

using log4net;
using log4net.Core;
using log4net.ObjectRenderer;

using MindTouch.Tasking;

namespace MindTouch {

    /// <summary>
    /// Provides extension and static helper methods for working with a log4Net <see cref="ILog"/> instance.
    /// </summary>
    public static class LogUtils {

        //--- Constants ---
        private static readonly Type _type = typeof(LogUtils);

        //--- Extension Methods ---

        /// <summary>
        /// Short-cut for <see cref="ILogger.IsEnabledFor"/> with <see cref="Level.Trace"/>.
        /// </summary>
        /// <param name="log">Logger instance.</param>
        /// <returns><see langword="True"/> if the the logger is running trace loggging.</returns>
        public static bool IsTraceEnabled(this ILog log) {
            return log.Logger.IsEnabledFor(Level.Trace);
        }

        /// <summary>
        /// Log a method call at <see cref="Level.Trace"/> level.
        /// </summary>
        /// <param name="log">Logger instance.</param>
        /// <param name="method">Method name.</param>
        /// <param name="args">Method arguments.</param>
        public static void TraceMethodCall(this ILog log, string method, params object[] args) {
            if(log.IsTraceEnabled()) {
                log.Logger.Log(_type, Level.Trace, string.Format("{0}{1}", method, Render(args)), null);
            }
        }

        /// <summary>
        /// Log an exception in a specific method at <see cref="Level.Trace"/> level.
        /// </summary>
        /// <param name="log">Logger instance.</param>
        /// <param name="exception">Exception that triggered this log call.</param>
        /// <param name="method">Method name.</param>
        /// <param name="args">Method arguments.</param>
        public static void TraceExceptionMethodCall(this ILog log, Exception exception, string method, params object[] args) {
            if(log.IsTraceEnabled()) {
                log.Logger.Log(_type, Level.Trace, string.Format("{0}{1}", method, Render(args)), exception);
            }
        }

        /// <summary>
        /// Log a formatted string at <see cref="Level.Trace"/> level.
        /// </summary>
        /// <param name="log">Logger instance.</param>
        /// <param name="format">A format string.</param>
        /// <param name="args">Format string parameters.</param>
        public static void TraceFormat(this ILog log, string format, params object[] args) {
            if(log.IsTraceEnabled()) {
                log.Logger.Log(_type, Level.Trace, string.Format(format, args), null);
            }
        }

        /// <summary>
        /// Log a method call at <see cref="Level.Debug"/> level.
        /// </summary>
        /// <param name="log">Logger instance.</param>
        /// <param name="method">Method name.</param>
        /// <param name="args">Method arguments.</param>
        public static void DebugMethodCall(this ILog log, string method, params object[] args) {
            if(log.IsDebugEnabled) {
                log.DebugFormat("{0}{1}", method, Render(args));
            }
        }

        /// <summary>
        /// Log an exception in a specific method at <see cref="Level.Debug"/> level.
        /// </summary>
        /// <param name="log">Logger instance.</param>
        /// <param name="exception">Exception that triggered this log call.</param>
        /// <param name="method">Method name.</param>
        /// <param name="args">Method arguments.</param>
        public static void DebugExceptionMethodCall(this ILog log, Exception exception, string method, params object[] args) {
            if(log.IsDebugEnabled) {
                log.Debug(string.Format("{0}{1}", method, Render(args)), exception);
            }
        }

        /// <summary>
        /// Log an exception with a formatted text message.
        /// </summary>
        /// <param name="log">Logger instance.</param>
        /// <param name="exception">Exception that triggered this log call.</param>
        /// <param name="message">Message format string.</param>
        /// <param name="args">Format arguments.</param>
        public static void DebugFormat(this ILog log, Exception exception, string message, params object[] args) {
            if(log.IsDebugEnabled) {
                log.Debug(string.Format(message,args),exception);
            }
        }

        /// <summary>
        /// Log a method call at <see cref="Level.Info"/> level.
        /// </summary>
        /// <param name="log">Logger instance.</param>
        /// <param name="method">Method name.</param>
        /// <param name="args">Method arguments.</param>
        public static void InfoMethodCall(this ILog log, string method, params object[] args) {
            if(log.IsInfoEnabled) {
                log.InfoFormat("{0}{1}", method, Render(args));
            }
        }

        /// <summary>
        /// Log a method call at <see cref="Level.Warn"/> level.
        /// </summary>
        /// <param name="log">Logger instance.</param>
        /// <param name="method">Method name.</param>
        /// <param name="args">Method arguments.</param>
        public static void WarnMethodCall(this ILog log, string method, params object[] args) {
            if(log.IsWarnEnabled) {
                log.WarnFormat("{0}{1}", method, Render(args));
            }
        }

        /// <summary>
        /// Log an exception in a specific method at <see cref="Level.Warn"/> level.
        /// </summary>
        /// <param name="log">Logger instance.</param>
        /// <param name="exception">Exception that triggered this log call.</param>
        /// <param name="method">Method name.</param>
        /// <param name="args">Method arguments.</param>
        public static void WarnExceptionMethodCall(this ILog log, Exception exception, string method, params object[] args) {
            if(log.IsWarnEnabled) {
                log.Warn(string.Format("{0}{1}", method, Render(args)), exception);
            }
        }

        /// <summary>
        /// Log a formatted string message about an exception at <see cref="Level.Warn"/> level.
        /// </summary>
        /// <param name="log">Logger instance.</param>
        /// <param name="exception">Exception that triggered this log call.</param>
        /// <param name="format">A format string.</param>
        /// <param name="args">Format string parameters.</param>
        public static void WarnExceptionFormat(this ILog log, Exception exception, string format, params object[] args) {
            if(log.IsWarnEnabled) {
                log.Warn(string.Format(format, args), exception);
            }
        }

        /// <summary>
        /// Log an exception in a specific method at <see cref="Level.Error"/> level.
        /// </summary>
        /// <param name="log">Logger instance.</param>
        /// <param name="exception">Exception that triggered this log call.</param>
        /// <param name="method">Method name.</param>
        /// <param name="args">Method arguments.</param>
        public static void ErrorExceptionMethodCall(this ILog log, Exception exception, string method, params object[] args) {
            if(log.IsErrorEnabled) {
                log.Error(string.Format("{0}{1}", method, Render(args)), exception);
            }
        }

        /// <summary>
        /// Log a formatted string message about an exception at <see cref="Level.Error"/> level.
        /// </summary>
        /// <param name="log">Logger instance.</param>
        /// <param name="exception">Exception that triggered this log call.</param>
        /// <param name="format">A format string.</param>
        /// <param name="args">Format string parameters.</param>
        public static void ErrorExceptionFormat(this ILog log, Exception exception, string format, params object[] args) {
            if(log.IsErrorEnabled) {
                log.Error(string.Format(format, args), exception);
            }
        }

        //--- Class Methods ---

        /// <summary>
        /// Create an <see cref="ILog"/> instance for the enclosing type.
        /// </summary>
        /// <returns></returns>
        public static ILog CreateLog() {
            var frame = new System.Diagnostics.StackFrame(1, false);
            var type = frame.GetMethod().DeclaringType;
            return LogManager.GetLogger(type);
        }

        /// <summary>
        /// This methods is deprecated please use <see cref="CreateLog()"/>instead.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [Obsolete("This methods is deprecated please use LogUtils.CreateLog() instead.")]
        public static ILog CreateLog<T>() {
            return LogManager.GetLogger(typeof(T));
        }

        /// <summary>
        /// Create an <see cref="ILog"/> instance for a given type.
        /// </summary>
        /// <param name="classType">Type of class to create the logger for</param>
        /// <returns></returns>
        public static ILog CreateLog(Type classType) {
            return LogManager.GetLogger(classType);
        }

        private static string Render(ICollection args) {
            if((args == null) || (args.Count == 0)) {
                return string.Empty;
            }
            var builder = new StringBuilder();
            Render(builder, args);
            return builder.ToString();
        }

        private static void Render(StringBuilder builder, object arg) {
            if(arg is ICollection) {
                builder.Append("(");
                RenderCollection(builder, (ICollection)arg);
                builder.Append(")");
            } else {
                builder.Append(arg);
            }
        }

        private static void RenderCollection(StringBuilder builder, ICollection args) {
            if(args is IDictionary) {
                var dict = (IDictionary)args;
                var first = true;
                foreach(var key in dict.Keys) {

                    // append ',' if need be
                    if(!first) {
                        builder.Append(", ");
                    }
                    first = false;

                    // append item in collection
                    try {
                        var arg = dict[key];
                        builder.Append(key);
                        builder.Append("=");
                        Render(builder, arg);
                    } catch { }
                }
            } else {
                var first = true;
                foreach(var arg in args) {

                    // append ',' if need be
                    if(!first) {
                        builder.Append(", ");
                    }
                    first = false;

                    // append item in collection
                    try {
                        Render(builder, arg);
                    } catch { }
                }
            }
        }
    }
}

namespace  MindTouch.Logging {
    internal class ExceptionRenderer : IObjectRenderer {

        //--- Methods ---
        public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer) {
        	if(obj is Exception) {
                writer.Write(((Exception)obj).GetCoroutineStackTrace());
            }
        }
    }
}