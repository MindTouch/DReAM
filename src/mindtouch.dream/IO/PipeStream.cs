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
using System.Threading;

namespace MindTouch.IO {
    internal class PipeStreamBuffer {

        //--- Fields ---
        private byte[] _buffer;
        private int _readPosition = 0;
        private int _writePosition = 0;
        private bool _full = false;
        private Semaphore _readCount;
        private ManualResetEvent _readerClosed = new ManualResetEvent(false);
        private Semaphore _writeCount;
        private ManualResetEvent _writerClosed = new ManualResetEvent(false);

        //--- Constructors ---
        public PipeStreamBuffer() : this(16384) { }

        public PipeStreamBuffer(int size) {
            if(size <= 0) {
                throw new ArgumentException("size");
            }
            _buffer = new byte[size];
            _readCount = new Semaphore(0, size);
            _writeCount = new Semaphore(size, size);
        }

        //--- Properties ---
        public int MaxWriteCount {
            get {
                if(_full) {
                    return 0;
                } else if(_readPosition < _writePosition) {
                    return _buffer.Length - _writePosition + _readPosition;
                } else if(_readPosition > _writePosition) {
                    return _readPosition - _writePosition;
                } else {
                    return _buffer.Length;
                }
            }
        }

        public int MaxReadCount {
            get {
                if(_full) {
                    return _buffer.Length;
                } else if(_readPosition < _writePosition) {
                    return _writePosition - _readPosition;
                } else if(_readPosition > _writePosition) {
                    return _buffer.Length - _readPosition + _writePosition;
                } else {
                    return 0;
                }
            }
        }

        //--- Methods ---
        public void CloseWriter() {
            _writerClosed.Set();
        }

        public void CloseReader() {
            _readerClosed.Set();
        }

        public void Write(byte[] buffer, int offset, int count) {
            if((offset + count) > buffer.Length) {
                throw new ArgumentOutOfRangeException("offset+count");
            }
            if(offset < 0) {
                throw new ArgumentOutOfRangeException("offset");
            }
            if(count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }
            if(count == 0) {
                return;
            }

            // copy as much data as possible
            for(int i = 0; i < count; ++i) {
                switch(WaitHandle.WaitAny(new WaitHandle[] { _readerClosed, _writeCount })) {
                case 0:
                    throw new IOException("data closed");
                case 1:
                    lock(this) {
                        _buffer[_writePosition++] = buffer[offset++];
                        if(_writePosition == _buffer.Length) {
                            _writePosition = 0;
                        }
                        _full = (_writePosition == _readPosition);
                    }
                    _readCount.Release();
                    break;
                default:
                    throw new InvalidOperationException("unexpected");
                }
            }
        }

        public int Read(byte[] buffer, int offset, int count) {
            if((offset + count) > buffer.Length) {
                throw new ArgumentOutOfRangeException("offset+count");
            }
            if(offset < 0) {
                throw new ArgumentOutOfRangeException("offset");
            }
            if(count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }
            if(count == 0) {
                return 0;
            }

            // copy as much data as possible
            int result = 0;
            for(int i = 0; i < count; ++i) {
                switch(WaitHandle.WaitAny(new WaitHandle[] { _writerClosed, _readCount })) {
                case 0:
                    if(!_readCount.WaitOne(0, false)) {
                        return result;
                    }
                    goto case 1;
                case 1:
                    lock(this) {
                        ++result;
                        buffer[offset++] = _buffer[_readPosition++];
                        if(_readPosition == _buffer.Length) {
                            _readPosition = 0;
                        }
                        _full = false;
                    }
                    _writeCount.Release();
                    break;
                default:
                    throw new InvalidOperationException("unexpected");
                }
            }
            return result;
        }
    }

    internal class PipeStreamWriter : Stream {

        //--- Fields ---
        private PipeStreamBuffer _buffer;

        //--- Constructors ---
        internal PipeStreamWriter(PipeStreamBuffer buffer) {
            if(buffer == null) {
                throw new ArgumentNullException("buffer");
            }
            _buffer = buffer;
        }

        //--- Properties ---
        public override bool CanRead {
            get { return false; }
        }

        public override bool CanSeek {
            get { return false; }
        }

        public override bool CanWrite {
            get { return true; }
        }

        public override long Length {
            get { return 0; }
        }

        public override long Position {
            get { return 0; }
            set { throw new NotSupportedException(); }
        }

        //--- Methds ---
        public override void Close() {
            _buffer.CloseWriter();
            _buffer = null;
            base.Close();
        }

        public override void Flush() {

            // TODO: wait until all bytes have been read
        }

        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException();
        }

        public override void SetLength(long value) {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            _buffer.Write(buffer, offset, count);
        }
    }

    internal class PipeStreamReader : Stream {

        //--- Fields ---
        private PipeStreamBuffer _buffer;

        //--- Constructors ---
        internal PipeStreamReader(PipeStreamBuffer buffer) {
            if(buffer == null) {
                throw new ArgumentNullException("buffer");
            }
            _buffer = buffer;
        }

        //--- Properties ---
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override bool CanRead { get { return true; } }

        public override long Position {
            get { return 0; }
            set { throw new NotSupportedException(); }
        }

        public override long Length {
            get { return _buffer.MaxReadCount; }
        }

        //--- Methods ---
        public override void Close() {
            _buffer.CloseReader();
            _buffer = null;
            base.Close();
        }

        public override void Flush() {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException();
        }

        public override void SetLength(long value) {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return _buffer.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotSupportedException();
        }
    }
}
