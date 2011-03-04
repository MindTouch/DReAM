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
using System.IO;

namespace MindTouch.IO {

#pragma warning disable 661,660
    /// <summary>
    /// Value wrapper for Records returned by <see cref="IQueueStream.ReadNextRecord"/> 
    /// </summary>
    public struct QueueStreamRecord {
#pragma warning restore 661,660

        //--- Class Fields ---
        /// <summary>
        /// Empty record instance
        /// </summary>
        public static readonly QueueStreamRecord Empty = new QueueStreamRecord(null,null);

        //--- Fields ---
        /// <summary>
        /// Stream of the record
        /// </summary>
        public readonly Stream Stream;
        /// <summary>
        /// Handle of record used by <see cref="IQueueStream.DeleteRecord"/> to identify the record to delete
        /// </summary>
        /// <remarks>
        /// The Handle is a separate object so that the caller does not have to hold on to a reference to the record stream between use of the stream
        /// deletion of the record
        /// </remarks>
        public readonly IQueueStreamHandle Handle;

        //--- Constructors ---
        /// <summary>
        /// Create a new record
        /// </summary>
        /// <remarks>
        /// Only used by implementors of <see cref="IQueueStream"/>
        /// </remarks>
        /// <param name="stream">A stream containing the bytes of only this record. Should be positioned at the 0 byte</param>
        /// <param name="handle">An implementation of <see cref="IQueueStreamHandle"/> used by the creator of this instance to track the record in question</param>
        public QueueStreamRecord(Stream stream, IQueueStreamHandle handle) {
            Stream = stream;
            Handle = handle;
        }

        //--- Operators ---

        // Note (arnec): Overriding == & != but not Equals() or GetHashCode() because == and != do not work by default for struct comparison
        /// <summary>
        /// This class provides a custom == operator in order to support simple <see cref="ValueType.Equals(object)"/> comparison, which is not
        /// supported by default by structs
        /// </summary>
        /// <param name="a">left hand side of equality comparison</param>
        /// <param name="b">right hand side of equality comparison</param>
        /// <returns><see langword="true"/> if both records contain the same values</returns>
        public static bool operator ==(QueueStreamRecord a, QueueStreamRecord b) {
            return a.Equals(b);
        }

        /// <summary>
        /// This class provides a custom == operator in order to support simple <see cref="ValueType.Equals(object)"/> negation, which is not
        /// supported by default by structs
        /// </summary>
        /// <param name="a">left hand side of equality comparison</param>
        /// <param name="b">right hand side of equality comparison</param>
        /// <returns><see langword="true"/> if both records contain different values</returns>
        public static bool operator !=(QueueStreamRecord a, QueueStreamRecord b) {
            return !a.Equals(b);
        }
    }
}