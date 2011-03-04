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
using System.Linq;
using log4net;
using MindTouch.Collections;

namespace MindTouch.IO {
    /// <summary>
    /// A record store with semantics for sequentially writing and retrieving records, similar to a queue, except that a read removes the
    /// head next record, but does not delete it until delete is called, allowing for transactional dequeuing of records.
    /// </summary>
    /// <remarks>
    /// This implementation is backed by a series of binary files, rolling over into a new file when the maxFileBytes constructor parameters is
    /// exceeded. A file in the sequence is deleted when all records within that file are marked as deleted, allowing more granular storage
    /// recovery than <see cref="SingleFileQueueStream"/>. Exists primarily for use with <see cref="TransactionalQueue{T}"/>. This
    /// class is not threadsafe.
    /// </remarks>
    public class MultiFileQueueStream : IQueueStream {

        //--- Types ---
        private class QueueStreamHandle : IQueueStreamHandle {

            //--- Fields ---
            public readonly long Position;
            public readonly int Id;
            public readonly int Length;

            //--- Constructors ---
            public QueueStreamHandle(int id, long position, int length) {
                Position = position;
                Id = id;
                Length = length;
            }
        }

        private class QueueFileInfo {

            //--- Fields ---
            public readonly int Id;
            public readonly Stream Stream;

            //--- Constructors ---
            public QueueFileInfo(int id, string filename) {
                Id = id;
                Stream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            }
        }

        //--- Constants ---
        private const long DEFAULT_MAX_FILE_BYTES = 10 * 1024 * 1024;
        private const int LENGTH_SIZE = sizeof(int);
        private const int HEADER_SIZE = 4 + LENGTH_SIZE;

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();
        private static readonly byte[] RecordMarker = new byte[] { 0, 0, 255, 1 };
        private static readonly byte[] DeletedMarker = new byte[] { 0, 0, 1, 255 };

        //--- Fields ---
        private readonly Dictionary<int, QueueFileInfo> _files = new Dictionary<int, QueueFileInfo>();
        private readonly string _storageRoot;
        private readonly long _maxFileBytes;
        private readonly Dictionary<int, HashSet<QueueStreamHandle>> _recordMap = new Dictionary<int, HashSet<QueueStreamHandle>>();
        private readonly Queue<QueueStreamHandle> _recordQueue = new Queue<QueueStreamHandle>();
        private QueueFileInfo _head;
        private bool _isDisposed;

        //--- Constructors ---
        /// <summary>
        /// Create a new instance using the default maximum file bytes
        /// </summary>
        /// <param name="storageRoot">Path to directory in which the queue streamm will keep its data</param>
        public MultiFileQueueStream(string storageRoot) : this(storageRoot, DEFAULT_MAX_FILE_BYTES) { }

        /// <summary>
        /// Create a new instance 
        /// </summary>
        /// <param name="storageRoot">Path to directory in which the queue streamm will keep its data</param>
        /// <param name="maxFileBytes">maximum number of bytes to keep in any one data file. This affects space reclamation on fragmented queues</param>
        public MultiFileQueueStream(string storageRoot, long maxFileBytes) {
            _storageRoot = storageRoot;
            _maxFileBytes = maxFileBytes;
            Initialize();
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
            if(!_isDisposed) {
                _isDisposed = true;
                foreach(var fileInfo in _files.Values) {
                    fileInfo.Stream.Close();
                    fileInfo.Stream.Dispose();
                }
                _files.Clear();
            }
        }

        /// <summary>
        /// Append the given stream as a new record. It is assumed that the stream's position is set to the appropriate position.
        /// </summary>
        /// <param name="stream">A stream containng the record to be stored</param>
        /// <param name="length">Number of bytes to read from the stream</param>
        public void AppendRecord(Stream stream, long length) {
            EnsureInstanceNotDisposed();
            _head.Stream.Seek(0, SeekOrigin.End);
            var position = _head.Stream.Position;
            _head.Stream.Write(RecordMarker, 0, RecordMarker.Length);
            var lengthBytes = BitConverter.GetBytes((int)length);
            _head.Stream.Write(lengthBytes, 0, lengthBytes.Length);
            var copied = stream.CopyTo(_head.Stream, length);
            if(copied != length) {

                // unable to read as many bytes as we expected before EOF
                throw new IOException(string.Format("expected to read {0} bytes from stream only able to read {1}", length, copied));
            }
            AddHandle(_head.Id, position, (int)length);
            if(_head.Stream.Position < _maxFileBytes) {
                return;
            }
            CreateHead(_head.Id + 1);
        }

        /// <summary>
        /// Read the next unread record from the stream. Will return <see cref="QueueStreamRecord.Empty"/> if there is no next record in the stream.
        /// </summary>
        /// <returns></returns>
        public QueueStreamRecord ReadNextRecord() {
            EnsureInstanceNotDisposed();
            if(_recordQueue.Count == 0) {
                return QueueStreamRecord.Empty;
            }
            var next = _recordQueue.Dequeue();
            var streamInfo = GetStreamInfoFromHandle(next);
            if(streamInfo == null) {
                EnsureInstanceNotDisposed();
                throw new InvalidOperationException("unable to access stream for head file");
            }
            streamInfo.Stream.Seek(next.Position + HEADER_SIZE, SeekOrigin.Begin);
            var data = new ChunkedMemoryStream();
            var copied = streamInfo.Stream.CopyTo(data, next.Length);
            if(copied != next.Length) {
                _log.WarnFormat("reached EOF in the middle of the record");
                return QueueStreamRecord.Empty;
            }
            data.Seek(0, SeekOrigin.Begin);
            return new QueueStreamRecord(data, next);
        }

        /// <summary>
        /// Truncate the QueueStream and drop all records.
        /// </summary>
        public void Truncate() {
            EnsureInstanceNotDisposed();
            _log.DebugFormat("truncating queuestream at {0}",_storageRoot);
            _recordMap.Clear();
            _recordQueue.Clear();
            foreach(var file in _files.Values.ToArray()) {
                RemoveFile(file);
            }
            CreateHead(1);
        }

        /// <summary>
        /// Permanently remove a record from the stream, identified by the <see cref="IQueueStreamHandle"/> attached to the <see cref="QueueStreamRecord"/>
        /// returned by <see cref="ReadNextRecord"/>.
        /// </summary>
        /// <param name="location">The handle attached to the <see cref="QueueStreamRecord"/>
        /// returned by <see cref="ReadNextRecord"/></param>
        public void DeleteRecord(IQueueStreamHandle location) {
            EnsureInstanceNotDisposed();
            if(!(location is QueueStreamHandle)) {
                throw new ArgumentException("The provided location handle is invalid for this stream", "location");
            }
            var handle = (QueueStreamHandle)location;
            var streamInfo = GetStreamInfoFromHandle(handle);
            if(streamInfo == null) {
                return;
            }
            streamInfo.Stream.Seek(handle.Position, SeekOrigin.Begin);
            streamInfo.Stream.Write(DeletedMarker, 0, DeletedMarker.Length);
            RemoveHandle(handle, streamInfo);
        }

        private void EnsureInstanceNotDisposed() {
            if(_isDisposed) {
                throw new ObjectDisposedException("the queue has been disposed");
            }
        }

        private void Initialize() {
            if(!Directory.Exists(_storageRoot)) {
                Directory.CreateDirectory(_storageRoot);
            }
            var files = from filename in Directory.GetFiles(_storageRoot, "data_*.bin")
                        let parts = Path.GetFileNameWithoutExtension(filename).Split('_')
                        let id = int.Parse(parts[parts.Length - 1])
                        orderby id
                        select new QueueFileInfo(id, GetFilename(id));
            _log.DebugFormat("Queue storage usage for {0}", _storageRoot);
            foreach(var queueFileInfo in files) {
                if(_log.IsDebugEnabled) {
                    var finfo = new System.IO.FileInfo(GetFilename(queueFileInfo.Id));
                    _log.DebugFormat(" {0}: {1:0.00}KB", Path.GetFileName(GetFilename(queueFileInfo.Id)), (double)finfo.Length / 1024);
                }
                _head = queueFileInfo;
                _files.Add(queueFileInfo.Id, queueFileInfo);
                MapRecords(queueFileInfo);
            }
            if(_files.Count == 0) {
                CreateHead(1);
            }
        }

        private string GetFilename(int id) {
            return Path.Combine(_storageRoot, "data_" + id + ".bin");
        }

        private void MapRecords(QueueFileInfo queueFileInfo) {
            if(queueFileInfo.Stream.Length == 0) {
                return;
            }
            queueFileInfo.Stream.Position = 0;
            var header = new byte[RecordMarker.Length];
            while(true) {
                var position = queueFileInfo.Stream.Position;
                var missedHeader = false;
                int read;
                var isDeleted = false;
                while(true) {
                    read = queueFileInfo.Stream.Read(header, 0, header.Length);
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
                    queueFileInfo.Stream.Position = ++position;
                    missedHeader = true;
                }
                if(missedHeader) {
                    _log.Warn("missed expected header and skipped corrupt data");
                }
                var lengthBytes = new byte[LENGTH_SIZE];
                read = queueFileInfo.Stream.Read(lengthBytes, 0, LENGTH_SIZE);
                if(read < LENGTH_SIZE) {

                    // end of file? WTF? ok, bail
                    _log.Warn("reached end of file trying to read the number of bytes in the next record, skipping record");
                    queueFileInfo.Stream.Seek(0, SeekOrigin.Begin);
                    return;
                }
                var length = BitConverter.ToInt32(lengthBytes, 0);
                if(length <= 0) {
                    _log.Warn("illegal record length, must be in corrupted record, skipping record");
                    queueFileInfo.Stream.Position = ++position;
                    continue;
                }
                if(!isDeleted) {
                    position = queueFileInfo.Stream.Position - HEADER_SIZE;
                    AddHandle(queueFileInfo.Id, position, length);
                }
                if(queueFileInfo.Stream.Seek(length, SeekOrigin.Current) == queueFileInfo.Stream.Length) {
                    break;
                }
            }
            queueFileInfo.Stream.Seek(0, SeekOrigin.Begin);
        }

        private QueueFileInfo GetStreamInfoFromHandle(QueueStreamHandle handle) {
            QueueFileInfo queueFileInfo;
            _files.TryGetValue(handle.Id, out queueFileInfo);
            return queueFileInfo;
        }

        private void AddHandle(int id, long position, int length) {
            var handle = new QueueStreamHandle(id, position, length);
            _recordQueue.Enqueue(handle);
            HashSet<QueueStreamHandle> handles;
            if(!_recordMap.TryGetValue(id, out handles)) {
                handles = new HashSet<QueueStreamHandle>();
                _recordMap.Add(id, handles);
            }
            handles.Add(handle);
        }

        private void CreateHead(int nextId) {
            var newHead = new QueueFileInfo(nextId, GetFilename(nextId));
            _files.Add(nextId, newHead);
            _head = newHead;
        }

        private void RemoveHandle(QueueStreamHandle handle, QueueFileInfo queueFileInfo) {
            HashSet<QueueStreamHandle> handles;
            if(!_recordMap.TryGetValue(handle.Id, out handles)) {
                return;
            }
            handles.Remove(handle);
            if(handles.Count != 0) {
                return;
            }
            _recordMap.Remove(handle.Id);
            if(queueFileInfo == _head) {

                // tail caught up with head, so let's truncate the file
                queueFileInfo.Stream.SetLength(0);
            } else {

                // no undeleted records in non-head file, remove file
                RemoveFile(queueFileInfo);
            }
            if(_head.Id == 1 || _recordQueue.Count > 0) {
                return;
            }
            if(_recordMap.Count > 0) {
                return;
            }

            // the head is not the first file and there are no remaining items, so we can reset the head to the first file
            RemoveFile(_head);
            CreateHead(1);
        }

        private void RemoveFile(QueueFileInfo queueFileInfo) {
            queueFileInfo.Stream.Close();
            queueFileInfo.Stream.Dispose();
            var filename = GetFilename(queueFileInfo.Id);
            try {
                File.Delete(filename);
            } catch(Exception e) {
                _log.Warn(string.Format("unable to delete disposed file '{0}'", filename), e);
            }
            _files.Remove(queueFileInfo.Id);
        }
    }
}