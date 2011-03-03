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
using System.Reflection;
using System.Text;

namespace MindTouch.Tasking {
    using Yield = IEnumerator<IYield>;

    internal enum CoroutineExceptionHandlingMode {
        Unwind,
        CatchOnce
    }

    /// <summary>
    /// Signature for a no argument Coroutine.
    /// </summary>
    /// <typeparam name="TResult">Type of synchronization handle. Expected to be subclass of <see cref="AResult"/>.</typeparam>
    /// <param name="result">
    /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
    /// </param>
    /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
    public delegate Yield CoroutineHandler<TResult>(TResult result);

    /// <summary>
    /// Signature for a one argument Coroutine.
    /// </summary>
    /// <typeparam name="T1">Type of the first argument.</typeparam>
    /// <typeparam name="TResult">Type of synchronization handle. Expected to be subclass of <see cref="AResult"/>.</typeparam>
    /// <param name="arg1">First coroutine argument.</param>
    /// <param name="result">
    /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
    /// </param>
    /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
    public delegate Yield CoroutineHandler<T1, TResult>(T1 arg1, TResult result);

    /// <summary>
    /// Signature for a two argument Coroutine.
    /// </summary>
    /// <typeparam name="T1">Type of the first argument.</typeparam>
    /// <typeparam name="T2">Type of the second argument.</typeparam>
    /// <typeparam name="TResult">Type of synchronization handle. Expected to be subclass of <see cref="AResult"/>.</typeparam>
    /// <param name="arg1">First coroutine argument.</param>
    /// <param name="arg2">Second coroutine argument.</param>
    /// <param name="result">
    /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
    /// </param>
    /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
    public delegate Yield CoroutineHandler<T1, T2, TResult>(T1 arg1, T2 arg2, TResult result);

    /// <summary>
    /// Signature for a three argument Coroutine.
    /// </summary>
    /// <typeparam name="T1">Type of the first argument.</typeparam>
    /// <typeparam name="T2">Type of the second argument.</typeparam>
    /// <typeparam name="T3">Type of the third argument.</typeparam>
    /// <typeparam name="TResult">Type of synchronization handle. Expected to be subclass of <see cref="AResult"/>.</typeparam>
    /// <param name="arg1">First coroutine argument.</param>
    /// <param name="arg2">Second coroutine argument.</param>
    /// <param name="arg3">Third coroutine argument.</param>
    /// <param name="result">
    /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
    /// </param>
    /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
    public delegate Yield CoroutineHandler<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3, TResult result);

    /// <summary>
    /// Signature for a four argument Coroutine.
    /// </summary>
    /// <typeparam name="T1">Type of the first argument.</typeparam>
    /// <typeparam name="T2">Type of the second argument.</typeparam>
    /// <typeparam name="T3">Type of the third argument.</typeparam>
    /// <typeparam name="T4">Type of the fourth argument.</typeparam>
    /// <typeparam name="TResult">Type of synchronization handle. Expected to be subclass of <see cref="AResult"/>.</typeparam>
    /// <param name="arg1">First coroutine argument.</param>
    /// <param name="arg2">Second coroutine argument.</param>
    /// <param name="arg3">Third coroutine argument.</param>
    /// <param name="arg4">Fourth coroutine argument.</param>
    /// <param name="result">
    /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
    /// </param>
    /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
    public delegate Yield CoroutineHandler<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, TResult result);

    /// <summary>
    /// Signature for a five argument Coroutine.
    /// </summary>
    /// <typeparam name="T1">Type of the first argument.</typeparam>
    /// <typeparam name="T2">Type of the second argument.</typeparam>
    /// <typeparam name="T3">Type of the third argument.</typeparam>
    /// <typeparam name="T4">Type of the fourth argument.</typeparam>
    /// <typeparam name="T5">Type of the fifth argument.</typeparam>
    /// <typeparam name="TResult">Type of synchronization handle. Expected to be subclass of <see cref="AResult"/>.</typeparam>
    /// <param name="arg1">First coroutine argument.</param>
    /// <param name="arg2">Second coroutine argument.</param>
    /// <param name="arg3">Third coroutine argument.</param>
    /// <param name="arg4">Fourth coroutine argument.</param>
    /// <param name="arg5">Fifth coroutine argument.</param>
    /// <param name="result">
    /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
    /// </param>
    /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
    public delegate Yield CoroutineHandler<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, TResult result);

    /// <summary>
    /// Signature for a six argument Coroutine.
    /// </summary>
    /// <typeparam name="T1">Type of the first argument.</typeparam>
    /// <typeparam name="T2">Type of the second argument.</typeparam>
    /// <typeparam name="T3">Type of the third argument.</typeparam>
    /// <typeparam name="T4">Type of the fourth argument.</typeparam>
    /// <typeparam name="T5">Type of the fifth argument.</typeparam>
    /// <typeparam name="T6">Type of the sixth argument.</typeparam>
    /// <typeparam name="TResult">Type of synchronization handle. Expected to be subclass of <see cref="AResult"/>.</typeparam>
    /// <param name="arg1">First coroutine argument.</param>
    /// <param name="arg2">Second coroutine argument.</param>
    /// <param name="arg3">Third coroutine argument.</param>
    /// <param name="arg4">Fourth coroutine argument.</param>
    /// <param name="arg5">Fifth coroutine argument.</param>
    /// <param name="arg6">Sixth coroutine argument.</param>
    /// <param name="result">
    /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
    /// </param>
    /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
    public delegate Yield CoroutineHandler<T1, T2, T3, T4, T5, T6, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, TResult result);

    /// <summary>
    /// Signature for a seven argument Coroutine.
    /// </summary>
    /// <typeparam name="T1">Type of the first argument.</typeparam>
    /// <typeparam name="T2">Type of the second argument.</typeparam>
    /// <typeparam name="T3">Type of the third argument.</typeparam>
    /// <typeparam name="T4">Type of the fourth argument.</typeparam>
    /// <typeparam name="T5">Type of the fifth argument.</typeparam>
    /// <typeparam name="T6">Type of the sixth argument.</typeparam>
    /// <typeparam name="T7">Type of the seventh argument.</typeparam>
    /// <typeparam name="TResult">Type of synchronization handle. Expected to be subclass of <see cref="AResult"/>.</typeparam>
    /// <param name="arg1">First coroutine argument.</param>
    /// <param name="arg2">Second coroutine argument.</param>
    /// <param name="arg3">Third coroutine argument.</param>
    /// <param name="arg4">Fourth coroutine argument.</param>
    /// <param name="arg5">Fifth coroutine argument.</param>
    /// <param name="arg6">Sixth coroutine argument.</param>
    /// <param name="arg7">Seventh coroutine argument.</param>
    /// <param name="result">
    /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
    /// </param>
    /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
    public delegate Yield CoroutineHandler<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, TResult result);

    /// <summary>
    /// Provides an execution framework for a special type of methods, called Coroutines.
    /// </summary>
    /// <remarks>
    /// Coroutines are methods that can yield their execution by using the <see langword="yield"/> keyword. To be a coroutine,
    /// a method's signature must match one of the <see cref="CoroutineHandler{TResult}"/> delegates, i.e. it will always have a
    /// return type of <see cref="IEnumerator{T}"/> and the last argument is always a subclass of <see cref="AResult"/>.
    /// </remarks>
    [Serializable]
    public sealed class Coroutine : IContinuation {

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog();

        [ThreadStatic]
        private static Coroutine _current;

        //--- Class Properties ---

        /// <summary>
        /// The current <see cref="Coroutine"/> instance, if executing in a coroutine contex.
        /// </summary>
        public static Coroutine Current {
            get { return _current; }
            private set { _current = value; }
        }

        //--- Class Methods ---
 
        /// <summary>
        /// Invoke a no argument Coroutine. Static shortcut for <see cref="Invoke"/> member.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callee">Method to invoke as coroutine.</param>
        /// <param name="result">
        /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
        /// </param>
        /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
        public static TResult Invoke<TResult>(CoroutineHandler<TResult> callee, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(result));
            return result;
        }

        /// <summary>
        /// Invoke a one argument Coroutine. Static shortcut for <see cref="Invoke"/> member.
        /// </summary>
        /// <typeparam name="T1">Type of the first argument.</typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callee">Method to invoke as coroutine.</param>
        /// <param name="arg1">First coroutine argument.</param>
        /// <param name="result">
        /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
        /// </param>
        /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
        public static TResult Invoke<T1, TResult>(CoroutineHandler<T1, TResult> callee, T1 arg1, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, result));
            return result;
        }

        /// <summary>
        /// Invoke a two argument Coroutine. Static shortcut for <see cref="Invoke"/> member.
        /// </summary>
        /// <typeparam name="T1">Type of the first argument.</typeparam>
        /// <typeparam name="T2">Type of the second argument.</typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callee">Method to invoke as coroutine.</param>
        /// <param name="arg1">First coroutine argument.</param>
        /// <param name="arg2">Second coroutine argument.</param>
        /// <param name="result">
        /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
        /// </param>
        /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
        public static TResult Invoke<T1, T2, TResult>(CoroutineHandler<T1, T2, TResult> callee, T1 arg1, T2 arg2, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, arg2, result));
            return result;
        }

        /// <summary>
        /// Invoke a three argument Coroutine. Static shortcut for <see cref="Invoke"/> member.
        /// </summary>
        /// <typeparam name="T1">Type of the first argument.</typeparam>
        /// <typeparam name="T2">Type of the second argument.</typeparam>
        /// <typeparam name="T3">Type of the third argument.</typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callee">Method to invoke as coroutine.</param>
        /// <param name="arg1">First coroutine argument.</param>
        /// <param name="arg2">Second coroutine argument.</param>
        /// <param name="arg3">Third coroutine argument.</param>
        /// <param name="result">
        /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
        /// </param>
        /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
        public static TResult Invoke<T1, T2, T3, TResult>(CoroutineHandler<T1, T2, T3, TResult> callee, T1 arg1, T2 arg2, T3 arg3, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, arg2, arg3, result));
            return result;
        }

        /// <summary>
        /// Invoke a four argument Coroutine. Static shortcut for <see cref="Invoke"/> member.
        /// </summary>
        /// <typeparam name="T1">Type of the first argument.</typeparam>
        /// <typeparam name="T2">Type of the second argument.</typeparam>
        /// <typeparam name="T3">Type of the third argument.</typeparam>
        /// <typeparam name="T4">Type of the fourth argument.</typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callee">Method to invoke as coroutine.</param>
        /// <param name="arg1">First coroutine argument.</param>
        /// <param name="arg2">Second coroutine argument.</param>
        /// <param name="arg3">Third coroutine argument.</param>
        /// <param name="arg4">Fourth coroutine argument.</param>
        /// <param name="result">
        /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
        /// </param>
        /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
        public static TResult Invoke<T1, T2, T3, T4, TResult>(CoroutineHandler<T1, T2, T3, T4, TResult> callee, T1 arg1, T2 arg2, T3 arg3, T4 arg4, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, arg2, arg3, arg4, result));
            return result;
        }

        /// <summary>
        /// Invoke a five argument Coroutine. Static shortcut for <see cref="Invoke"/> member.
        /// </summary>
        /// <typeparam name="T1">Type of the first argument.</typeparam>
        /// <typeparam name="T2">Type of the second argument.</typeparam>
        /// <typeparam name="T3">Type of the third argument.</typeparam>
        /// <typeparam name="T4">Type of the fourth argument.</typeparam>
        /// <typeparam name="T5">Type of the fifth argument.</typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callee">Method to invoke as coroutine.</param>
        /// <param name="arg1">First coroutine argument.</param>
        /// <param name="arg2">Second coroutine argument.</param>
        /// <param name="arg3">Third coroutine argument.</param>
        /// <param name="arg4">Fourth coroutine argument.</param>
        /// <param name="arg5">Fifth coroutine argument.</param>
        /// <param name="result">
        /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
        /// </param>
        /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
        public static TResult Invoke<T1, T2, T3, T4, T5, TResult>(CoroutineHandler<T1, T2, T3, T4, T5, TResult> callee, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, arg2, arg3, arg4, arg5, result));
            return result;
        }

        /// <summary>
        /// Invoke a six argument Coroutine. Static shortcut for <see cref="Invoke"/> member.
        /// </summary>
        /// <typeparam name="T1">Type of the first argument.</typeparam>
        /// <typeparam name="T2">Type of the second argument.</typeparam>
        /// <typeparam name="T3">Type of the third argument.</typeparam>
        /// <typeparam name="T4">Type of the fourth argument.</typeparam>
        /// <typeparam name="T5">Type of the fifth argument.</typeparam>
        /// <typeparam name="T6">Type of the sixth argument.</typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callee">Method to invoke as coroutine.</param>
        /// <param name="arg1">First coroutine argument.</param>
        /// <param name="arg2">Second coroutine argument.</param>
        /// <param name="arg3">Third coroutine argument.</param>
        /// <param name="arg4">Fourth coroutine argument.</param>
        /// <param name="arg5">Fifth coroutine argument.</param>
        /// <param name="arg6">Sixth coroutine argument.</param>
        /// <param name="result">
        /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
        /// </param>
        /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
        public static TResult Invoke<T1, T2, T3, T4, T5, T6, TResult>(CoroutineHandler<T1, T2, T3, T4, T5, T6, TResult> callee, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, arg2, arg3, arg4, arg5, arg6, result));
            return result;
        }

        /// <summary>
        /// Invoke a  seven argument Coroutine. Static shortcut for <see cref="Invoke"/> member.
        /// </summary>
        /// <typeparam name="T1">Type of the first argument.</typeparam>
        /// <typeparam name="T2">Type of the second argument.</typeparam>
        /// <typeparam name="T3">Type of the third argument.</typeparam>
        /// <typeparam name="T4">Type of the fourth argument.</typeparam>
        /// <typeparam name="T5">Type of the fifth argument.</typeparam>
        /// <typeparam name="T6">Type of the sixth argument.</typeparam>
        /// <typeparam name="T7">Type of the seventh argument.</typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callee">Method to invoke as coroutine.</param>
        /// <param name="arg1">First coroutine argument.</param>
        /// <param name="arg2">Second coroutine argument.</param>
        /// <param name="arg3">Third coroutine argument.</param>
        /// <param name="arg4">Fourth coroutine argument.</param>
        /// <param name="arg5">Fifth coroutine argument.</param>
        /// <param name="arg6">Sixth coroutine argument.</param>
        /// <param name="arg7">Seventh coroutine argument.</param>
        /// <param name="result">
        /// The result instance to be returned by the call to <see cref="Coroutine.Invoke"/> that executes this coroutine.
        /// </param>
        /// <returns>Iterator used by <see cref="Coroutine"/>.</returns>
        public static TResult Invoke<T1, T2, T3, T4, T5, T6, T7, TResult>(CoroutineHandler<T1, T2, T3, T4, T5, T6, T7, TResult> callee, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, arg2, arg3, arg4, arg5, arg6, arg7, result));
            return result;
        }

        //--- Fields ---
        private readonly object _method;
        private Yield _iterator;
        private Coroutine _outer;
        private readonly AResult _result;
        private CoroutineExceptionHandlingMode _mode = CoroutineExceptionHandlingMode.Unwind;

        //--- Constructors ---

        /// <summary>
        /// Create a new Coroutine instance from a delegate.
        /// </summary>
        /// <remarks>
        /// Consider using one of the static Invoke methods instead.
        /// </remarks>
        /// <param name="callee">Delegate to method to be invoked as a coroutine.</param>
        /// <param name="result">Synchronization handle.</param>
        public Coroutine(Delegate callee, AResult result) {
            if(callee == null) {
                throw new ArgumentNullException("callee");
            }
            if(result == null) {
                throw new ArgumentNullException("result");
            }
            _method = callee;
            _result = result;
        }

        /// <summary>
        /// Create a new Coroutine instance from a method
        /// </summary>
        /// <param name="method">Info for method to be invoked as a coroutine.</param>
        /// <param name="result">Synchronization handle.</param>
        public Coroutine(MethodInfo method, AResult result) {
            if(method == null) {
                throw new ArgumentNullException("method");
            }
            if(result == null) {
                throw new ArgumentNullException("result");
            }
            _method = method;
            _result = result;
        }

        //--- Properties ---
        
        /// <summary>
        /// Method that this Coroutine is executing. 
        /// </summary>
        public MethodInfo Method {
            get {
                if(_method is MethodInfo) {
                    return (MethodInfo)_method;
                }
                return ((Delegate)_method).Method;
            }
        }

        /// <summary>
        /// Full name of the method being called as a coroutine.
        /// </summary>
        public string FullName {
            get {
                bool first;
                var result = new StringBuilder();

                // build full name of method including declaring type
                MethodInfo method = Method;
                Type declaring = method.DeclaringType;
                if(declaring != null) {
                    result.Append(declaring.FullName.Replace('+', '.'));
                    result.Append(".");
                }
                result.Append(method.Name);

                // add generic parameters to method name if any
                if(method.IsGenericMethod) {
                    result.Append("[");
                    first = true;
                    foreach(var generic in method.GetGenericArguments()) {
                        if(!first) {
                            result.Append(",");
                        } else {
                            first = false;
                        }
                        result.Append(generic.Name);
                    }
                    result.Append("]");
                }

                // add parameter types and names
                result.Append("(");
                first = true;
                foreach(var param in method.GetParameters()) {
                    if(!first) {
                        result.Append(", ");
                    } else {
                        first = false;
                    }
                    var paramType = param.ParameterType;
                    if(paramType != null) {
                        result.Append(param.ParameterType.Name);
                    } else {
                        result.Append("<UnknownType>");
                    }
                    result.Append(" ");
                    result.Append(param.Name);
                }
                result.Append(")");
                return result.ToString();
            }
        }

        internal CoroutineExceptionHandlingMode Mode {
            get { return _mode; }
            set { _mode = value; }
        }

        //--- Methods ---

        /// <summary>
        /// Get a coroutine specific stacktrace, i.e. following the coroutine invocation stack rather than the thread call stack.
        /// </summary>
        /// <returns></returns>
        public ICollection<Coroutine> GetStackTrace() {
            var result = new List<Coroutine>();
            for(var current = this; current != null; current = current._outer) {
                result.Add(current);
            }
            return result;
        }

        /// <summary>
        /// Invoke the coroutine.
        /// </summary>
        /// <param name="invocation"></param>
        public void Invoke(Func<Yield> invocation) {
            if(_iterator != null) {
                throw new InvalidOperationException("coroutine has already been invoked");
            }
            if(invocation == null) {
                throw new ArgumentNullException("invocation");
            }
            Exception exception = null;

            // store current environment
            _outer = Current;
            try {

                // set coroutine environment
                Current = this;

                // invoke first stage of coroutine
                _iterator = invocation();

                // iterate over all stages of the coroutine
                while(_iterator.MoveNext()) {
                    var current = _iterator.Current;

                    // check if we need to wait for an asynchronous callback to continue
                    if(!current.CanContinueImmediately(this)) {

                        // time to bail out; the iterator will continue where we left off when it is ready to proceed
                        return;
                    }

                    // check if we have an exception to handle
                    var e = current.Exception;
                    if(e != null) {
                        if(_mode == CoroutineExceptionHandlingMode.Unwind) {

                            // make the exception propagate out
                            exception = e;
                            break;
                        }
                        if(_mode == CoroutineExceptionHandlingMode.CatchOnce) {

                            // let this exception be returned the the invoker
                            _mode = CoroutineExceptionHandlingMode.Unwind;
                        }
                    }
                }
            } catch(Exception e) {
                exception = e;
            } finally {

                // restore previous environment
                Current = _outer;
            }

            // invoke completion
            Completion(exception);

            // coroutine is finished; we need to dispose of it
            Dispose();
        }

        /// <summary>
        /// Continue execution of a suspended coroutine.
        /// </summary>
        public void Continue() {
            if(_iterator == null) {
                throw new InvalidOperationException("coroutine has not been invoked yet");
            }
            Exception exception = null;

            // store current environment
            var previous = Current;
            try {

                // set coroutine environment
                Current = this;

                // iterate over all stages of the coroutine
                while(true) {

                    // check if we have an exception to handle
                    var e = _iterator.Current.Exception;
                    if(e != null) {
                        if(_mode == CoroutineExceptionHandlingMode.Unwind) {

                            // make the exception propagate out
                            exception = e;
                            break;
                        }
                        if(_mode == CoroutineExceptionHandlingMode.CatchOnce) {

                            // let this exception be returned the the invoker
                            _mode = CoroutineExceptionHandlingMode.Unwind;
                        }
                    }

                    // continue with execution of the coroutine
                    if(!_iterator.MoveNext()) {
                        break;
                    }

                    // check if we need to wait for an asynchronous callback to continue
                    if(!_iterator.Current.CanContinueImmediately(this)) {

                        // time to bail out; the iterator will continue where we left off when it is ready to proceed
                        return;
                    }
                }
            } catch(Exception e) {
                exception = e;
            } finally {

                // restore previous environment
                Current = previous;
            }

            // invoke completion
            Completion(exception);

            // coroutine is finished; we need to dispose of it
            Dispose();
        }

        private void Dispose() {
            if(_iterator != null) {
                try {
                    _iterator.Dispose();
                } catch(Exception e) {
                    _log.WarnExceptionMethodCall(e, "Dispose");
                } finally {
                    _iterator = null;
                }
            }
        }

        private void Completion(Exception exception) {

            // NOTE (steveb): result.Return()/Throw() must be called by the coroutine; 
            //      if the coroutine omits to call it, we will throw an exception 
            //      since 'result' will not be marked as finished

            if(exception != null) {
                try {

                    // try to forward exception to recipient
                    exception.SetCoroutineInfo(this);
                    _result.Throw(exception);
                } catch {

                    // result object was already set
                    _log.ErrorExceptionFormat(exception, "Completion(): unhandled exception in {0} coroutine [1]", FullName);
                }
            } else if(!_result.HasFinished) {
                try {
                    exception = new CoroutineMissingResultException();
                    exception.SetCoroutineInfo(this);

                    // operation completed, but result hasn't been set
                    _result.Throw(exception);

                    // always log the fact that a coroutine failed to set a result; this is a pretty bad situation!
                    _log.Error("Completion() failed", exception);
                } catch {

                    // result object must have been set in the meantime
                    if(exception != null) {
                        _log.ErrorExceptionFormat(exception, "Completion(): unhandled exception in {0} coroutine [2]", FullName);
                    } else {
                        _log.ErrorFormat("Coroutine.Completion(): missing result caused unhandled exception in {0} coroutine [3]", FullName);
                    }
                }
            }
        }
    }
}
