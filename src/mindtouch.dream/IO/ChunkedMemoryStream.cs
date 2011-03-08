/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2011 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * This file is dual licensded under Apache License 2.0 and MIT X11.
 * See respective sections below.
 * 
 * Apache License 2.0
 * ------------------
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
 * 
 * MIT X11
 * -------
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.IO;

namespace MindTouch.IO {

    /// <summary>
    /// Provides an altnerate implementation of <see cref="MemoryStream"/> that keeps its internal data buffers in several chunks to avoid
    /// memory fragmenation.
    /// </summary>
    public class ChunkedMemoryStream : Stream {
        
        //--- Constants ---
        private const int CHUNK_SHIFT = 14;
        private const int CHUNK_SIZE = 1 << CHUNK_SHIFT;

        //--- Fields ---
        private long _length;
        private long _position;
        private byte[] _buffer;
        private readonly int _bufferIndex;
        private int _bufferCount;
        private byte[][] _chunks;
        private bool _closed;
        private readonly bool _fixed;
        private readonly bool _readonly;
        private readonly bool _publiclyVisible;
        
        //--- Constructors ---

        /// <summary>
        /// Create a writeable and resizable memory stream
        /// </summary>
        public ChunkedMemoryStream() : this(0) { }

        /// <summary>
        /// Create a writeable and resizable memory stream with an initial capacity
        /// </summary>
        /// <param name="capacity">Minimum of available bytes pre-allocated.</param>
        public ChunkedMemoryStream(int capacity) {
            if(capacity < 0) {
                throw new ArgumentOutOfRangeException("capacity"); 
            }
            _buffer = new byte[capacity];
            _publiclyVisible = true;
        }

        /// <summary>
        /// Initializes a new non-resizable instance based on the specified byte array.
        /// </summary>
        /// <remarks>
        /// When the memory stream is initialized with a byte buffer, the following behavior should be excepted:
        /// <list type="bullet">
        /// <item>The stream cannot be resized.</item>
        /// <item>The stream is writeable.</item>
        /// <item><see cref="GetBuffer"/> throws <see cref="UnauthorizedAccessException"/>.</item>
        /// <item><see cref="SetLength"/> throws <see cref="NotSupportedException"/> if trying to make the stream
        /// larger than the initial buffer size.</item>
        /// <item>Attempting to change <see cref="Capacity"/> throws <see cref="NotSupportedException"/>.</item>
        /// </list>
        /// </remarks>
        /// <param name="buffer">Backing buffer to use.</param>
        public ChunkedMemoryStream(byte[] buffer) : this(buffer, true) { }

        /// <summary>
        /// Initializes a new non-resizable instance based on the specified byte array with the <see cref="CanWrite"/> property set as specified.
        /// </summary>
        /// <remarks>
        /// When the memory stream is initialized with this constructor, the following behavior should be excepted:
        /// <list type="bullet">
        /// <item>The stream cannot be resized.</item>
        /// <item>The stream may be writeable, as indicated by <see cref="CanWrite"/>.</item>
        /// <item><see cref="GetBuffer"/> throws <see cref="UnauthorizedAccessException"/>.</item>
        /// <item><see cref="SetLength"/> throws <see cref="NotSupportedException"/> if trying to make the stream
        /// larger than the initial buffer size.</item>
        /// <item>Attempting to change <see cref="Capacity"/> throws <see cref="NotSupportedException"/>.</item>
        /// </list>
        /// </remarks>
        /// <param name="buffer"> The array of unsigned bytes from which to create the current stream.</param>
        /// <param name="writable">The setting of the <see cref="CanWrite"/> property, which determines whether the stream supports writing.</param>
        public ChunkedMemoryStream(byte[] buffer, bool writable) {
            if(buffer == null) {
                throw new ArgumentNullException("buffer");
            }
            _buffer = buffer;
            _bufferIndex = 0;
            _bufferCount = buffer.Length;
            _length = _bufferCount;
            _fixed = true;
            _readonly = !writable;
        }

        /// <summary>
        /// Initializes a new non-resizable instance based on the specified region (index) of a byte array.
        /// </summary>
        /// <remarks>
        /// When the memory stream is initialized with a byte buffer, the following behavior should be excepted:
        /// <list type="bullet">
        /// <item>The stream cannot be resized.</item>
        /// <item>The stream is writeable.</item>
        /// <item><see cref="GetBuffer"/> throws <see cref="UnauthorizedAccessException"/>.</item>
        /// <item><see cref="SetLength"/> throws <see cref="NotSupportedException"/> if trying to make the stream
        /// larger than the initial buffer size.</item>
        /// <item>Attempting to change <see cref="Capacity"/> throws <see cref="NotSupportedException"/>.</item>
        /// </list>
        /// </remarks>
        /// <param name="buffer"> The array of unsigned bytes from which to create the current stream.</param>
        /// <param name="index">The index into buffer at which the stream begins.</param>
        /// <param name="count">The length of the stream in bytes.</param>
        public ChunkedMemoryStream(byte[] buffer, int index, int count) : this(buffer, index, count, true, false) { }

        /// <summary>
        /// Initializes a new instance based on the specified region of a byte array, with the <see cref="CanWrite"/> 
        /// property set as specified, and the ability to call <see cref="GetBuffer"/> set as specified.
        /// </summary>
        /// <remarks>
        /// When the memory stream is initialized with a byte buffer, the following behavior should be excepted:
        /// <list type="bullet">
        /// <item>The stream cannot be resized.</item>
        /// <item><see cref="GetBuffer"/> throws <see cref="UnauthorizedAccessException"/>.</item>
        /// <item><see cref="SetLength"/> throws <see cref="NotSupportedException"/> if trying to make the stream
        /// larger than the initial buffer size.</item>
        /// <item>Attempting to change <see cref="Capacity"/> throws <see cref="NotSupportedException"/>.</item>
        /// </list>
        /// </remarks>
        /// <param name="buffer"> The array of unsigned bytes from which to create the current stream.</param>
        /// <param name="index">The index into buffer at which the stream begins.</param>
        /// <param name="count">The length of the stream in bytes.</param>
        /// <param name="writable">The setting of the <see cref="CanWrite"/> property, which determines whether the stream supports writing.</param>
        public ChunkedMemoryStream(byte[] buffer, int index, int count, bool writable) : this(buffer, index, count, writable, false) { }

        /// <summary>
        /// Initializes a new instance based on the specified region of a byte array, with the <see cref="CanWrite"/> 
        /// property set as specified, and the ability to call <see cref="GetBuffer"/> set as specified.
        /// </summary>
        /// 
        /// <remarks>
        /// When the memory stream is initialized with a byte buffer, the following behavior should be excepted:
        /// <list type="bullet">
        /// <item>The stream cannot be resized.</item>
        /// <item><see cref="GetBuffer"/> throws <see cref="UnauthorizedAccessException"/>.</item>
        /// <item><see cref="SetLength"/> throws <see cref="NotSupportedException"/> if trying to make the stream
        /// larger than the initial buffer size.</item>
        /// <item>Attempting to change <see cref="Capacity"/> throws <see cref="NotSupportedException"/>.</item>
        /// </list>
        /// </remarks>
        /// <param name="buffer"> The array of unsigned bytes from which to create the current stream.</param>
        /// <param name="index">The index into buffer at which the stream begins.</param>
        /// <param name="count">The length of the stream in bytes.</param>
        /// <param name="writable">The setting of the <see cref="CanWrite"/> property, which determines whether the stream supports writing.</param>
        /// <param name="publiclyVisible">
        /// <see langword="True"/> to enable <see cref="GetBuffer"/>, which returns the unsigned byte array from which
        /// the stream was created; otherwise, false.
        /// </param>
        public ChunkedMemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible) {
            if(buffer == null) {
                throw new ArgumentNullException("buffer");
            }
            if((index < 0) || (index >= buffer.Length)) {
                throw new ArgumentOutOfRangeException("index");
            }
            if((count < 0) || (index >= (buffer.Length - count))) {
                throw new ArgumentOutOfRangeException("count");
            }
            _buffer = buffer;
            _bufferIndex = index;
            _bufferCount = count;
            _length = _bufferCount - _bufferIndex;
            _fixed = true;
            _readonly = !writable;
            _publiclyVisible = publiclyVisible;
        }

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if the stream is not closed.
        /// </summary>
        public override bool CanRead { get { return !_closed; } }
        
        /// <summary>
        /// <see langword="True"/> if the stream is not closed.
        /// </summary>
        public override bool CanSeek { get { return !_closed; } }

        /// <summary>
        /// <see langword="True"/> if the stream is not closed and isn't a read-only stream.
        /// </summary>
        public override bool CanWrite { get { return !_closed && !_readonly; } }

        /// <summary>
        /// Change the capacity of the stream. If the stream was created from a byte array, this will throw <see cref="NotSupportedException"/>.
        /// </summary>
        public long Capacity {
            get {
                int result = _bufferCount;
                if(_chunks != null) {
                    result += _chunks.Length * CHUNK_SIZE;
                }
                return result;
            }
            set {

                // check if there's anything to do
                if(value == Capacity) {
                    return;
                }

                // check if the buffer has fixed size
                if(_fixed) {
                    throw new NotSupportedException("Cannot expand this ChunkedMemoryStream");
                }

                // check if the new capicity is valid
                if(value < _length) {
                    throw new ArgumentOutOfRangeException("value", string.Format("New capacity cannot be negative or less than the current length {0} {1}", value, _length));
                }

                // reallocate buffer
                ConsolidateChunks(value);
            }
        }

        /// <summary>
        /// Length of stream.
        /// </summary>
        public override long Length {
            get {
                EnsureOpen();
                return _length;
            }
        }

        /// <summary>
        /// Current position in the stream.
        /// </summary>
        public override long Position {
            get {
                EnsureOpen();
                return _position;
            }
            set {
                EnsureOpen();
                if(value < 0) {
                    throw new ArgumentOutOfRangeException("value");
                }
                _position = value;
            }
        }

        //--- Methods ---

        /// <summary>
        /// This override of <see cref="Stream.Flush"/> is a no-op, except for the case that the stream has been closed in which case it
        /// will throw an <see cref="ObjectDisposedException"/>. 
        /// </summary>
        public override void Flush() {
            EnsureOpen();
        }

        /// <summary>
        /// Retrieve the internal chunked buffer representation as a single byte array. Only works if buffer was created with publicly
        /// visible option, otherwise throws <see cref="UnauthorizedAccessException"/>.
        /// </summary>
        /// <returns>A byte array of all stream bytes.</returns>
        public byte[] GetBuffer() {
            if(!_publiclyVisible) {
                throw new UnauthorizedAccessException();
            }

            // check if we need to consolidate the chunks
            if(_chunks != null) {
                ConsolidateChunks(Capacity);
            }
            return _buffer;
        }

        /// <summary>
        ///  Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">
        /// An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset 
        /// and (offset + count - 1) replaced by the bytes read from the current source.
        /// </param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes 
        /// are not currently available, or zero (0) if the end of the stream has been reached.
        /// </returns>
        public override int Read(byte[] buffer, int offset, int count)  {
            EnsureOpen();
            if(buffer == null) {
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0) {
                throw new ArgumentOutOfRangeException("offset");
            }
            if(count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }
            if(count > (buffer.Length - offset)) {
                throw new ArgumentException("count exceeds remaining bytes in buffer", "count");
            }

            // check if there's anything to do
            if((_position >= _length) || (count == 0)) {
                return 0;
            }

            // determine starting chunk
            int chunkOffset;
            int index = GetChunkIndex(_position, out chunkOffset);
            var result = count = Math.Min(count, (int)(_length - _position));
            _position += count;

            // copy bytes from each chunk
            while(count > 0) {
                int chunkCount;
                var chunk = GetChunk(index++, out chunkCount);
                var length = Math.Min(chunkCount - chunkOffset, count);
                Array.Copy(chunk, chunkOffset, buffer, offset, length);
                count -= length;
                offset += length;
                chunkOffset = 0;
            }
            return result;
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin) {
            EnsureOpen();
            long position;
            switch(origin) {
            case SeekOrigin.Begin:
                position = offset;
                break;
            case SeekOrigin.Current:
                position = _position + offset;
                break;
            case SeekOrigin.End:
                position = _length + offset;
                break;
            default:
                throw new NotImplementedException(string.Format("unknown SeekOrigin.{0}", origin));
            }
            if(position < 0) {
                throw new ArgumentException("final offset cannot be negative", "offset");
            }
            return _position = position;
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value) {
            EnsureOpen();
            if(_fixed && (value > Capacity)) {
                throw new NotSupportedException("Expanding this ChunkedMemoryStream is not supported"); 
            }
            if(_readonly) {
                throw new NotSupportedException ("Cannot write to this ChunkedMemoryStream"); 
            }
            if(value < 0) {
                throw new ArgumentOutOfRangeException("value");
            }

            // make sure we have capacity to accomodate the new length
            EnsureCapacity(value);

            // determine last chunk
            int chunkOffset;
            int index = GetChunkIndex(value, out chunkOffset);
            if((_chunks != null) && (index < _chunks.Length)) {

                // check if chunk needs to be partially cleared
                if(chunkOffset > 0) {
                    var chunk = _chunks[index];
                    if(chunk != null) {
                        Array.Clear(chunk, chunkOffset, CHUNK_SIZE - chunkOffset);
                    }

                    // skip this chunk for deletion
                    ++index;
                }

                // clear chunks past length
                for(int i = index; i < _chunks.Length; ++i) {
                    _chunks[i] = null;
                }
            }

            // reset length and position
            _length = value;
            _position = Math.Min(_position, _length);
        }

        /// <summary>
        /// Similar to GetBuffer, but creates its output by reading from the internal representation, rather than
        /// consolidating the chunks into a single buffer first.
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray() {
            EnsureOpen();
            var result = new byte[Length];
            var position = _position;
            Position = 0;
            Read(result, 0, result.Length);
            Position = position;
            return result;
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count) {
            EnsureOpen();
            if(_readonly) {
                throw new NotSupportedException("Stream is not writable"); 
            }
            if(buffer == null) {
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0) {
                throw new ArgumentOutOfRangeException("offset");
            }
            if(count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }
            if(count > (buffer.Length - offset)) {
                throw new ArgumentException("count exceeds remaining bytes in buffer", "count");
            }

            // make sure we have capacity the write operation
            EnsureCapacity(_position + count);

            // determine starting chunk
            int chunkOffset;
            int index = GetChunkIndex(_position, out chunkOffset);
            _position += count;
            _length = Math.Max(_length, _position);

            // copy bytes into each chunk
            while(count > 0) {
                int chunkCount;
                var chunk = GetChunk(index++, out chunkCount);
                var length = Math.Min(chunkCount - chunkOffset, count);
                Array.Copy(buffer, offset, chunk, chunkOffset, length);
                count -= length;
                offset += length;
                chunkOffset = 0;
            }
        }

        /// <summary>
        /// Write the entire memory stream to another stream.
        /// </summary>
        /// <param name="stream">Target stream.</param>
        public void WriteTo(Stream stream) {
            long length = _length;
            int index = 0;
            while(length > 0) {
                int chunkCount;
                var chunk = GetChunk(index++, out chunkCount);
                var count = (int)Math.Min(length, chunkCount);
                stream.Write(chunk, 0, count);
                length -= count;
            }
        }

        /// <summary>
        /// Dispose of the stream's held resource and call the base class <see cref="Stream.Dispose(bool)"/>.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing) {
            try {
                if(disposing) {
                    _buffer = null;
                    _chunks = null;
                    _closed = true;
                }
            } finally {
                base.Dispose(disposing);
            }
        }

        private void ConsolidateChunks(long capacity) {
            var newBuffer = new byte[capacity];
            var position = _position;
            Position = 0;
            Read(newBuffer, 0, (int)_length);
            Position = position;
            _buffer = newBuffer;
            _chunks = null;
        }

        private void EnsureCapacity(long requested) {
            long capacity = Capacity;

            // check if we need to do anything
            if(requested <= capacity) {
                return;
            }

            // check if the buffer has fixed size
            if(_fixed) {
                throw new NotSupportedException("Cannot expand this ChunkedMemoryStream");
            }

            // the smallest buffer we allocate is 256 bytes
            if(requested < 256) {
                requested = 256;
            }

            // check if doubling the current capacity is good enough
            if(requested < capacity * 2) {
                requested = capacity * 2;
            }

            // limit the allocation size to that of a single chunk
            if(requested > CHUNK_SIZE) {
                requested = CHUNK_SIZE;
            }

            // check if we need to resize the buffer
            if(_buffer.Length != requested) {
                Array.Resize(ref _buffer, (int)requested);
                _bufferCount = _buffer.Length - _bufferIndex;
            }
        }

        private void EnsureOpen() {
            if(_closed) {
                throw new ObjectDisposedException("stream has already been closed or disposed");
            }
        }

        private byte[] GetChunk(int index, out int count) {
            if(index == 0) {
                count = _bufferCount;
                return _buffer;
            }

            // resize chunks array if need  be
            --index;
            if(_chunks == null) {
                int newSize;
                for(newSize = 1; newSize < index; newSize *= 2) { }
                _chunks = new byte[newSize][];
            } else if(index >= _chunks.Length) {
                int newSize;
                for(newSize = _chunks.Length * 2; newSize < index; newSize *= 2) { }
                Array.Resize(ref _chunks, newSize);
            }

            // allocate chunk if need be
            var result = _chunks[index] ?? (_chunks[index] = new byte[CHUNK_SIZE]);
            count = CHUNK_SIZE;
            return result;
        }

        private int GetChunkIndex(long position, out int offset) {

            // check if position falls inside the buffer
            if(position < (_buffer.Length - _bufferIndex)) {
                offset = (int)position + _bufferIndex;
                return 0;
            }

            // calculate chunk index and offset
            position -= _buffer.Length;
            var result = (int)(position >> CHUNK_SHIFT);
            offset = (int)(position % CHUNK_SIZE);
            return result + 1;
        }
    }
}
