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

namespace MindTouch.IO {

    /// <summary>
    /// Provides a stream based contract for serializing types to binary and back.
    /// </summary>
    /// <remarks>
    /// This contract assumes that implementations will only read from and write to the stream and not try to alter it otherwise.
    /// </remarks>
    public interface ISerializer {

        //--- Methods ---

        /// <summary>
        /// Return an instance of the specified type by reading from the <see cref="Stream"/> at its current position.
        /// </summary>
        /// <remarks>
        /// The serializer must be smart enough to know when it has reached the end of the type's byte sequence, rather than reading
        /// until the end of stream.
        /// </remarks>
        /// <typeparam name="T">Type of the instance to deserialize.</typeparam>
        /// <param name="stream">Source stream.</param>
        /// <returns>Deserialized instance.</returns>
        T Deserialize<T>(Stream stream);

        /// <summary>
        /// Write the provided instance at the current position in the stream.
        /// </summary>
        /// <typeparam name="T">Type of the instance to serialize.</typeparam>
        /// <param name="stream">Destination stream.</param>
        /// <param name="data">Instance to be serialized.</param>
        void Serialize<T>(Stream stream, T data);
    }
}