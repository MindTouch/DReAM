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
    internal class SkipWriteStream : Stream {

        //--- Fields ---
        private readonly Stream _stream;
        private int _bytesToSkip;

        //--- Constructors ---
        internal SkipWriteStream(Stream stream, int bytesToSkip) {
            if(stream == null) {
                throw new ArgumentNullException("stream");
            }
            if(bytesToSkip < 0) {
                throw new ArgumentException("number must greater or equal to zero", "bytesToSkip");
            }
            _stream = stream;
            _bytesToSkip = bytesToSkip;
        }

        //--- Properties ---
        public override bool CanRead { get { return _stream.CanRead; } }
        public override bool CanSeek { get { return _stream.CanSeek; } }
        public override bool CanWrite { get { return _stream.CanWrite; } }
        public override long Length { get { return _stream.Length; } }

        public override long Position {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        //--- Methods ---
        public override void Flush() {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value) {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            if(_bytesToSkip > 0) {
                if(count > _bytesToSkip) {
                    _stream.Write(buffer, offset + _bytesToSkip, count - _bytesToSkip);
                }
                _bytesToSkip -= count;
            } else {
                _stream.Write(buffer, offset, count);
            }
        }
    }
}
