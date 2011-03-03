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
using log4net;

namespace MindTouch.Tasking {
    using Yield = IEnumerator<IYield>;

    /// <summary>
    /// Provides Extension methods on <see cref="AResult"/>, <see cref="Result"/> and <see cref="Result{T}"/> for working with them in
    /// the context of <see cref="Coroutine"/> execution.
    /// </summary>
    public static class CoroutineUtil {

        //--- Types ---
        private class SetAndContinue<T> : IYield, IContinuation {

            //--- Fields ---
            private readonly Result<T> _result;
            private readonly Action<T> _callback;
            private IContinuation _continuation;
            private Exception _exception;

            //--- Constructors ---
            public SetAndContinue(Result<T> result, Action<T> callback) {
                _result = result;
                _callback = callback;
            }

            //--- Properties ---
            public Exception Exception { get { return _exception ?? _result.Exception; } }

            //--- Methods ---
            public bool CanContinueImmediately(IContinuation continuation) {
                _continuation = continuation;

                // check if we can resume immediately; if not, use this object as the continuation
                bool result = ((IYield)_result).CanContinueImmediately(this);

                // check if we have a value for our callback
                if(result && _result.HasValue) {
                    try {
                        _callback(_result.Value);
                    } catch(Exception e) {
                        _exception = e;
                    }
                }
                return result;
            }

            public void Continue() {

                // check if we have a value for our callback
                if(_result.HasValue) {
                    try {
                        _callback(_result.Value);
                    } catch(Exception e) {
                        _exception = e;
                    }
                }

                // continue coroutine
                _continuation.Continue();
            }
        }

        private class LogExceptionAndContinue : IYield, IContinuation {

            //--- Fields ---
            private readonly AResult _result;
            private readonly ILog _log;
            private readonly string _message;
            private IContinuation _continuation;

            //--- Constructors ---
            public LogExceptionAndContinue(AResult result, ILog log, string message) {
                _result = result;
                _log = log;
                _message = message ?? "unhandled exception occurred in coroutine";
            }

            //--- Properties ---
            public Exception Exception { get { return _result.Exception; } }

            //--- Methods ---
            public bool CanContinueImmediately(IContinuation continuation) {
                _continuation = continuation;

                // check if we can resume immediately; if not, use this object as the continuation
                bool result = ((IYield)_result).CanContinueImmediately(this);

                // check if we have an exception to log
                if(result && _result.HasException) {
                    _log.Warn(_message, _result.Exception);
                }
                return result;
            }

            public void Continue() {

                // check if we have an exception to log
                if(_result.HasException) {
                    _log.Warn(_message, _result.Exception);
                }

                // continue coroutine
                _continuation.Continue();
            }
        }

        private class EnumerateAll : IYield, IContinuation {

            //--- Fields ---
            private readonly IEnumerator<IYield> _results;
            private IContinuation _continuation;

            //--- Constructors ---
            public EnumerateAll(IEnumerator<IYield> results) {
                _results = results;
            }

            //--- Properties ---
            public Exception Exception { get { return null; } }

            //--- Methods ---
            public bool CanContinueImmediately(IContinuation continuation) {
                _continuation = continuation;

                // loop over each IYield instance and check if we need to continue later
                while(_results.MoveNext()) {
                    if(!_results.Current.CanContinueImmediately(this)) {
                        return false;
                    }
                }

                // all IYield instances completed; time to dispose of the enumerator
                _results.Dispose();
                return true;
            }

            public void Continue() {

                // loop over remaining IYield instance and check if we need to continue later
                while(_results.MoveNext()) {
                    if(!_results.Current.CanContinueImmediately(this)) {
                        return;
                    }
                }

                // all IYield instances completed; time to dispose of the enumerator
                _results.Dispose();

                // continue coroutine
                _continuation.Continue();
            }
        }

        //--- Constants ---
        private const string COROUTINE_KEY = "coroutine";
        
        //--- AResult/Result/Result<T> Extension Methods ---

        /// <summary>
        /// Sets the current coroutine's behavior to catch its exception so that the invoking context can examine the exception, rather than it
        /// bubbling up the Coroutine stack.
        /// </summary>
        /// <remarks>
        /// The <see cref="Coroutine.Invoke"/> analog to calling a method with a try/catch around it.
        /// </remarks>
        /// <typeparam name="T">Type of AResult derivative value.</typeparam>
        /// <param name="result">Coroutine synchronization handle.</param>
        /// <returns>Coroutine synchronization handle.</returns>
        public static T Catch<T>(this T result) where T : AResult {

            // Note (arnec): this overload exists, because without it yield on async methods (not Coroutine.Invoke'd)
            // require the result type T to be specified because it can't infer it.
            // I.e. method Result<Foo> Method(Result<Foo> result) requires a .Catch<Foo>() instead of .Catch() without
            // this overloaded extension.
            Coroutine.Current.Mode = CoroutineExceptionHandlingMode.CatchOnce;
            return result;
        }

        /// <summary>
        /// Sets the current's coroutine behavior to catch its exception so that the invoking context can examine the exception, rather than it
        /// bubbling up the Coroutine stack.
        /// </summary>
        /// <remarks>
        /// The <see cref="Coroutine.Invoke"/> analog to calling a method with a try/catch around it.
        /// </remarks>
        /// <param name="result">Coroutine synchronization handle.</param>
        /// <returns>Coroutine synchronization handle.</returns>
        public static Result Catch(this Result result) {
            Coroutine.Current.Mode = CoroutineExceptionHandlingMode.CatchOnce;
            return result;
        }

        /// <summary>
        /// Sets the current coroutine's behavior to catch its exception so that the invoking context can examine the exception, rather than it
        /// bubbling up the Coroutine stack.
        /// </summary>
        /// <remarks>
        /// The <see cref="Coroutine.Invoke"/> analog to calling a method with a try/catch around it.
        /// </remarks>
        /// <typeparam name="T">Type of result's value.</typeparam>
        /// <param name="result">Coroutine synchronization handle.</param>
        /// <returns>Coroutine synchronization handle.</returns>
        public static Result<T> Catch<T>(this Result<T> result) {
            Coroutine.Current.Mode = CoroutineExceptionHandlingMode.CatchOnce;
            return result;
        }

        /// <summary>
        /// Sets the current coroutine's behavior to catch any exception thrown by a coroutine and log the exception,
        /// but continue with the caller's context.
        /// </summary>
        /// <param name="result">Coroutine synchronization handle.</param>
        /// <param name="log">Logger instance to use for the exception logging.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
        public static IYield CatchAndLog(this AResult result, ILog log) {
            return CatchAndLog(result, log, null);
        }

        /// <summary>
        /// Sets the current coroutine's behavior to catch any exception thrown by a coroutine and log the exception,
        /// but continue with the caller's context.
        /// </summary>
        /// <param name="result">Coroutine synchronization handle.</param>
        /// <param name="log">Logger instance to use for the exception logging.</param>
        /// <param name="message">Additional message to log along with exception</param>
        /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
        public static IYield CatchAndLog(this AResult result, ILog log, string message) {
            if(result == null) {
                throw new ArgumentNullException("result");
            }
            if(log == null) {
                throw new ArgumentNullException("log");
            }
            Coroutine.Current.Mode = CoroutineExceptionHandlingMode.CatchOnce;
            return new LogExceptionAndContinue(result, log, message);
        }

        /// <summary>
        /// Sets a callback for capturing the result's value.
        /// </summary>
        /// <remarks>
        /// Using the callback allows the value to be captured into a local variable rather than having to define
        /// a result instance before invoking the coroutine that would only be used to extract the value from after invocation.</remarks>
        /// <typeparam name="T">Type of result's value.</typeparam>
        /// <param name="result">Coroutine synchronization handle.</param>
        /// <param name="callback">Callback action to invoke on successful completion of the synchronization handle.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
        public static IYield Set<T>(this Result<T> result, Action<T> callback) {
            if(result == null) {
                throw new ArgumentNullException("result");
            }
            if(callback == null) {
                throw new ArgumentNullException("callback");
            }
            return new SetAndContinue<T>(result, callback);
        }

        /// <summary>
        /// Yield execution of the current coroutine untill all the results have completed.
        /// </summary>
        /// <typeparam name="TResult">Type of the result value.</typeparam>
        /// <param name="list">List of result instances to yield execution to.</param>
        /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
        public static IYield Join<TResult>(this IEnumerable<TResult> list) where TResult : AResult {
            return new EnumerateAll(list.Cast<IYield>().GetEnumerator());
        }

        //--- Exception Extension Methods ---

        /// <summary>
        /// Extract the coroutine context from an exception that was thrown as part of an invoke.
        /// </summary>
        /// <param name="exception">Exception to examine.</param>
        /// <returns>The coroutine instance if the exception was thrown in the context of a coroutine, <see langword="null"/> otherwise.</returns>
        public static Coroutine GetCoroutineInfo(this Exception exception) {
            if((exception == null) || (exception.Data == null)) {
                return null;
            }
            return exception.Data[COROUTINE_KEY] as Coroutine;
        }

        /// <summary>
        /// Get the coroutine invocation stack trace, including exception stack traces, and values of inner exceptions' coroutine stack traces.
        /// </summary>
        /// <param name="exception">Exception to examine.</param>
        /// <returns>
        /// A string representation of stack trace if the exception was thrown in the context of a coroutine, <see langword="null"/> otherwise.
        /// </returns>
        public static string GetCoroutineStackTrace(this Exception exception) {
            if(exception == null) {
                throw new ArgumentNullException("exception");
            }

            // render exception message
            var result = new StringBuilder();
            result.Append(exception.GetType().FullName);
            var message = exception.Message;
            if(!string.IsNullOrEmpty(message)) {
                result.Append(": ");
                result.Append(message);
            }

            // check if there is an inner exception to render
            if(exception.InnerException != null) {
                result.Append(" ---> ");
                result.AppendLine(exception.InnerException.GetCoroutineStackTrace());
                result.Append("   --- End of inner exception stack trace ---");
            }

            // render exception stack trace
            result.AppendLine();
            result.Append(exception.StackTrace);

            // check if exception has a coroutine stack trace associated with it
            var coroutine = exception.GetCoroutineInfo();
            if(coroutine != null) {
                result.AppendLine();
                result.AppendLine("   --- End of exception stack trace ---");

                // render coroutine stack trace
                foreach(var entry in coroutine.GetStackTrace()) {
                    result.AppendLine(string.Format("   at {0}", entry.FullName));
                }
                result.Append("   --- End of coroutine stack trace ---");
            }
            return result.ToString();
        }

        internal static void SetCoroutineInfo(this Exception exception) {
            exception.SetCoroutineInfo(Coroutine.Current);
        }

        internal static void SetCoroutineInfo(this Exception exception, Coroutine coroutine) {
            if((coroutine != null) && (exception.Data != null) && (exception.Data[COROUTINE_KEY] == null)) {
                exception.Data[COROUTINE_KEY] = coroutine;
            }
        }
    }
}