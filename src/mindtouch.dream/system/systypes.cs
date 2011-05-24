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

namespace System {

    /// <summary>
    /// Equality computation delegate.
    /// </summary>
    /// <typeparam name="T">Type of the values to be compared</typeparam>
    /// <param name="left">Left-hand value</param>
    /// <param name="right">Right-hand value</param>
    /// <returns><see langword="True"/> if left and right are the same value as determined by the delegate implementation.</returns>
    public delegate bool Equality<T>(T left, T right);

    /// <summary>
    /// Encapsulates a method that has no parameters and returns a value of the type specified by the TResult parameter.
    /// </summary>
    /// <remarks>This definition extends the regular .NET Func definitions, which only cover 4 parameters.</remarks>
    /// <typeparam name="T1">The type of the first parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="TResult">The type of the return value of the method that this delegate encapsulates.</typeparam>
    /// <param name="item1">The first parameter of the method that this delegate encapsulates.</param>
    /// <param name="item2">The second parameter of the method that this delegate encapsulates.</param>
    /// <param name="item3">The third parameter of the method that this delegate encapsulates.</param>
    /// <param name="item4">The fourth parameter of the method that this delegate encapsulates.</param>
    /// <param name="item5">The fifth parameter of the method that this delegate encapsulates.</param>
    /// <returns>The return value of the method that this delegate encapsulates.</returns>
    public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5);

    /// <summary>
    /// Encapsulates a method that has no parameters and returns a value of the type specified by the TResult parameter.
    /// </summary>
    /// <remarks>This definition extends the regular .NET Func definitions, which only cover 4 parameters.</remarks>
    /// <typeparam name="T1">The type of the first parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="TResult">The type of the return value of the method that this delegate encapsulates.</typeparam>
    /// <param name="item1">The first parameter of the method that this delegate encapsulates.</param>
    /// <param name="item2">The second parameter of the method that this delegate encapsulates.</param>
    /// <param name="item3">The third parameter of the method that this delegate encapsulates.</param>
    /// <param name="item4">The fourth parameter of the method that this delegate encapsulates.</param>
    /// <param name="item5">The fifth parameter of the method that this delegate encapsulates.</param>
    /// <param name="item6">The sixth parameter of the method that this delegate encapsulates.</param>
    /// <returns>The return value of the method that this delegate encapsulates.</returns>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, TResult>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6);

    /// <summary>
    /// Encapsulates a method that has no parameters and returns a value of the type specified by the TResult parameter.
    /// </summary>
    /// <remarks>This definition extends the regular .NET Func definitions, which only cover 4 parameters.</remarks>
    /// <typeparam name="T1">The type of the first parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="TResult">The type of the return value of the method that this delegate encapsulates.</typeparam>
    /// <param name="item1">The first parameter of the method that this delegate encapsulates.</param>
    /// <param name="item2">The second parameter of the method that this delegate encapsulates.</param>
    /// <param name="item3">The third parameter of the method that this delegate encapsulates.</param>
    /// <param name="item4">The fourth parameter of the method that this delegate encapsulates.</param>
    /// <param name="item5">The fifth parameter of the method that this delegate encapsulates.</param>
    /// <param name="item6">The sixth parameter of the method that this delegate encapsulates.</param>
    /// <param name="item7">The seventh parameter of the method that this delegate encapsulates.</param>
    /// <returns>The return value of the method that this delegate encapsulates.</returns>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7);

    /// <summary>
    /// Encapsulates a method that has no parameters and returns a value of the type specified by the TResult parameter.
    /// </summary>
    /// <remarks>This definition extends the regular .NET Func definitions, which only cover 4 parameters.</remarks>
    /// <typeparam name="T1">The type of the first parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T2">The type of the second parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T3">The type of the third parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="T8">The type of the eighth parameter of the method that this delegate encapsulates.</typeparam>
    /// <typeparam name="TResult">The type of the return value of the method that this delegate encapsulates.</typeparam>
    /// <param name="item1">The first parameter of the method that this delegate encapsulates.</param>
    /// <param name="item2">The second parameter of the method that this delegate encapsulates.</param>
    /// <param name="item3">The third parameter of the method that this delegate encapsulates.</param>
    /// <param name="item4">The fourth parameter of the method that this delegate encapsulates.</param>
    /// <param name="item5">The fifth parameter of the method that this delegate encapsulates.</param>
    /// <param name="item6">The sixth parameter of the method that this delegate encapsulates.</param>
    /// <param name="item7">The seventh parameter of the method that this delegate encapsulates.</param>
    /// <param name="item8">The eighth parameter of the method that this delegate encapsulates.</param>
    /// <returns>The return value of the method that this delegate encapsulates.</returns>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8);
}
