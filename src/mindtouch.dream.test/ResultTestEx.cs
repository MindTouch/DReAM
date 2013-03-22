/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2013 MindTouch, Inc.
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
using MindTouch.Tasking;

namespace MindTouch.Dream.Test {

    /// <summary>
    /// Provides extension methods for writing mock objects returning <see cref="Result"/> instances easier.
    /// </summary>
    public static class ResultTestEx {

        //--- Extension Methods ---

        /// <summary>
        /// Set <see cref="Result.Return()"/> on a result instance, returning the instance.
        /// </summary>
        /// <param name="result">Result instance extension method is called on.</param>
        /// <returns>Same result instance extension method was called on.</returns>
        public static Result WithReturn(this Result result) {
            result.Return();
            return result;
        }

        /// <summary>
        /// Set a value via <see cref="Result{T}.Return(T)"/> on a result instance, returning the instance.
        /// </summary>
        /// <typeparam name="T">Type of value to set on result.</typeparam>
        /// <param name="result">Result instance extension method is called on.</param>
        /// <param name="value">Value to set on result</param>
        /// <returns>Same result instance extension method was called on.</returns>
        public static Result<T> WithReturn<T>(this Result<T> result, T value) {
            result.Return(value);
            return result;
        }

        /// <summary>
        /// Convert a value into a <see cref="Result{T}"/> instance containing that value.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="value">Value instance.</param>
        /// <returns>New result instance.</returns>
        public static Result<T> AsResult<T>(this T value) {
            return new Result<T>().WithReturn(value);
        }
    }
}
