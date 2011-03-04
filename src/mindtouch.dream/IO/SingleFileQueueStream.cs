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
using System.IO;
using log4net;
using MindTouch.Collections;

namespace MindTouch.IO {

    /// <summary>
    /// A record store with semantics for sequentially writing and retrieving records, similar to a queue, except that a read removes the
    /// head next record, but does not delete it until delete is called, allowing for transactional dequeuing of records.
    /// </summary>
    /// <remarks>
    /// This implementation is backed by a single binary stream, which keeps growing until all records are marked as deleted, at which time
    /// the stream is truncacted. Exists primarily for use with <see cref="TransactionalQueue{T}"/>. This class is not threadsafe.
    /// </remarks>
    public class SingleFileQueueStream : IQueueStream {

        //--- Types ---
        private class QueueStreamHandle : IQueueStreamHandle {

            //--- Fields ---
            public readonly long Position;
            public readonly long Generation;

            //--- Constructors ---
            public QueueStreamHandle(long position, long generation) {
                Position = position;
                Generation = generation;
            }
        }

        //--- Constants ---
        private const int LENGTH_SIZE = sizeof(int);
        private const int HEADER_SIZE = 4 + LENGTH_SIZE;

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();
        private static readonly byte[] RecordMarker = new byte[] { 0, 0, 255, 1 };
        private static readonly byte[] DeletedMarker = new byte[] { 0, 0, 1, 255 };

        //--- Fields ---
        private readonly Stream _stream;
        private readonly Dictionary<long, int> _recordMap = new Dictionary<long, int>();
        private readonly Queue<long> _recordQueue = new Queue<long>();
        private long _generation = 0;

        //--- Constructors ---
        /// <summary>
        /// Create from file path. File will be opened for exclusive write and closed and disposed when the instance is disposed
        /// </summary>
        /// <param name="path">File path</param>
        public SingleFileQueueStream(string path) {
            if(!File.Exists(path)) {
                var dir = Path.GetDirectoryName(path);
                if(!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
            }
            _stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            MapRecords();
        }

        /// <summary>
        /// Create from an existing stream and the stream will be disposed when the instance is disposed.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> that supports reading, writing and seeking</param>
        public SingleFileQueueStream(Stream stream) {
            if(!stream.CanRead || !stream.CanSeek || !stream.CanWrite) {
                throw new ArgumentException("The stream must support reading, writing and seeking for use by the SingleFileQueueStream", "stream");
            }
            _stream = stream;
            MapRecords();
        }

        //--- Properties ---
        /// <summary>
        /// Count of all records not yet read in current session
        /// </summary>
        /// <remarks>
        /// Records that are read but not yet deleted are not reflected in Unreadcount
        /// </remarks>
        public int UnreadCount { get { return _recordQueue.Count; } }

        //--- Methods ---
        /// <summary>
        /// Dispose the instance per <see cref="System.IDisposable"/> pattern
        /// </summary>
        public void Dispose() {
            _stream.Close();
            _stream.Dispose();
        }

        /// <summary>
        /// Append the given stream as a new record. It is assumed that the stream's position is set to the appropriate position.
        /// </summary>
        /// <param name="stream">A stream containng the record to be stored</param>
        /// <param name="length">Number of bytes to read from the stream</param>
        public void AppendRecord(Stream stream, long length) {
            _stream.Seek(0, SeekOrigin.End);
            var position = _stream.Position;
            _stream.Write(RecordMarker, 0, RecordMarker.Length);
            var lengthBytes = BitConverter.GetBytes((int)length);
            _stream.Write(lengthBytes, 0, lengthBytes.Length);
            var copied = stream.CopyTo(_stream, length);
            if(copied != length) {

                // unable to read as many bytes as we expected before EOF
                throw new IOException(string.Format("expected to read {0} bytes from stream only able to read {1}", length, copied));
            }
            _recordQueue.Enqueue(position);
            _recordMap.Add(position, (int)length);
        }

        /// <summary>
        /// Permanently remove a record from the stream, identified by the <see cref="IQueueStreamHandle"/> attached to the <see cref="QueueStreamRecord"/>
        /// returned by <see cref="ReadNextRecord"/>.
        /// </summary>
        /// <param name="location">The handle returned by ReadNextRecord</param>
        public void DeleteRecord(IQueueStreamHandle location) {
            var handle = location as QueueStreamHandle;
            if(handle == null) {
                throw new ArgumentException("The provided location cursor is invalid for this stream", "location");
            }
            if(handle.Generation != _generation) {
                return;
            }
            if(!_recordMap.ContainsKey(handle.Position)) {
                return;
            }
            _stream.Position = handle.Position;
            _stream.Write(DeletedMarker, 0, DeletedMarker.Length);
            _recordMap.Remove(handle.Position);
            if(_recordMap.Count == 0) {

                // no undeleted records in file, truncate
                _stream.SetLength(0);
                _generation++;
            }
        }

        /// <summary>
        /// Read the next unread record from the stream. Will return <see cref="QueueStreamRecord.Empty"/> if there is no next record in the stream.
        /// </summary>
        /// <returns></returns>
        public QueueStreamRecord ReadNextRecord() {
            if(_recordQueue.Count == 0) {
                return QueueStreamRecord.Empty;
            }
            var next = _recordQueue.Dequeue();
            var recordLength = _recordMap[next];
            _stream.Seek(next + HEADER_SIZE, SeekOrigin.Begin);
            var data = new ChunkedMemoryStream();
            var copied = _stream.CopyTo(data, recordLength);
            if(copied != recordLength) {
                _log.WarnFormat("reached EOF in the middle of the record");
                return QueueStreamRecord.Empty;
            }
            data.Position = 0;
            return new QueueStreamRecord(data, new QueueStreamHandle(next, _generation));
        }

        /// <summary>
        /// Truncate the QueueStream and drop all records.
        /// </summary>
        public void Truncate() {
            _log.DebugFormat("truncating queue stream");
            _recordMap.Clear();
            _recordQueue.Clear();
            _stream.SetLength(0);
            _generation++;
        }

        private void MapRecords() {
            if(_stream.Length == 0) {
                return;
            }
            _stream.Position = 0;
            var header = new byte[RecordMarker.Length];
            while(true) {
                var position = _stream.Position;
                var missedHeader = false;
                int read;
                var isDeleted = false;
                while(true) {
                    read = _stream.Read(header, 0, header.Length);
                    if(read != header.Length) {

                        // end of file? WTF? ok, bail
                        _log.Warn("reached end of file trying to read the next record marker");
                        return;
                    }
                    if(ArrayUtil.Compare(header, RecordMarker) == 0) {
                        break;
                    }
                    if(ArrayUtil.Compare(header, DeletedMarker) == 0) {
                        isDeleted = true;
                        break;
                    }
                    _stream.Position = ++position;
                    missedHeader = true;
                }
                if(missedHeader) {
                    _log.Warn("missed expected header and skipped corrupt data");
                }
                var lengthBytes = new byte[LENGTH_SIZE];
                read = _stream.Read(lengthBytes, 0, LENGTH_SIZE);
                if(read < LENGTH_SIZE) {

                    // end of file? WTF? ok, bail
                    _log.Warn("reached end of file trying to read the number of bytes in the next record, skipping record");
                    _stream.Seek(0, SeekOrigin.Begin);
                    return;
                }
                var length = BitConverter.ToInt32(lengthBytes, 0);
                if(length <= 0) {
                    _log.Warn("illegal record length, must be in corrupted record, skipping record");
                    _stream.Position = ++position;
                    continue;
                }
                if(!isDeleted) {
                    position = _stream.Position - HEADER_SIZE;
                    _recordQueue.Enqueue(position);
                    _recordMap.Add(position, length);
                }
                if(_stream.Seek(length, SeekOrigin.Current) == _stream.Length) {
                    break;
                }
            }
            _stream.Seek(0, SeekOrigin.Begin);
        }
    }
}