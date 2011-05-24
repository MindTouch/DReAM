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

using System.IO;
using MindTouch.Collections;

namespace MindTouch.IO {

    /// <summary>
    /// Defines a serializer that can convert a type to and from a Stream.
    /// </summary>
    /// <remarks>
    /// Exists primarily for use with <see cref="TransactionalQueue{T}"/>
    /// </remarks>
    /// <typeparam name="T">Any type</typeparam>
    public interface IQueueItemSerializer<T> {

        //--- Methods ---

        /// <summary>
        /// Convert type T to a <see cref="Stream"/>.
        /// </summary>
        /// <remarks>
        /// It is assumed that the returned stream is read from beginning to end and contains only the binary data for the serialized item.
        /// </remarks>
        /// <param name="item">An instance of type T</param>
        /// <returns>A <see cref="Stream"/> containing the binary formatted type T</returns>
        Stream ToStream(T item);

        /// <summary>
        /// Create an instance of type T from a <see cref="Stream"/>.
        /// </summary>
        /// <remarks>
        /// It is assumed that the provided stream is read from beginning to end and contains only the binary data for the item to be deserialized.
        /// </remarks>
        /// <param name="stream">A <see cref="Stream"/> contained the binary formatted type T</param>
        /// <returns>A new instance of type T</returns>
        T FromStream(Stream stream);
    }
}