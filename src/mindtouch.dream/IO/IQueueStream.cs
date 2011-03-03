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
    /// <summary>
    /// A record store with semantics for sequentially writing and retrieving records, similar to a queue, except that a read removes the
    /// head next record, but does not delete it until delete is called, allowing for transactional dequeuing of records.
    /// </summary>
    /// <seealso cref="SingleFileQueueStream"/>
    /// <seealso cref="MultiFileQueueStream"/>
    public interface IQueueStream : IDisposable {

        //--- Properties ---

        /// <summary>
        /// Total number of records unread.
        /// </summary>
        int UnreadCount { get; }
        
        //--- Methods ---
        /// <summary>
        /// Append the given stream as a new record. It is assumed that the stream's position is set to the appropriate position.
        /// </summary>
        /// <param name="stream">A stream containng the record to be stored</param>
        /// <param name="length">Number of bytes to read from the stream</param>
        void AppendRecord(Stream stream, long length);
        
        /// <summary>
        /// Permanently remove a record from the stream, identified by the <see cref="IQueueStreamHandle"/> attached to the <see cref="QueueStreamRecord"/>
        /// returned by <see cref="ReadNextRecord"/>.
        /// </summary>
        /// <param name="location">
        /// The handle attached to the <see cref="QueueStreamRecord"/> returned by <see cref="ReadNextRecord"/>
        /// </param>
        void DeleteRecord(IQueueStreamHandle location);

        /// <summary>
        /// Read the next unread record from the stream. Will return <see cref="QueueStreamRecord.Empty"/> if there is no next record in the stream.
        /// </summary>
        /// <returns></returns>
        QueueStreamRecord ReadNextRecord();

        /// <summary>
        /// Truncate the QueueStream and drop all records.
        /// </summary>
        void Truncate();
    }

    /// <summary>
    /// Handle for a record returned by <see cref="IQueueStream"/> for later deletion of that record. The handle has not behavior
    /// associated with it, but serves simply as an identifier for the originating stream.
    /// </summary>
    public interface IQueueStreamHandle { }
}