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
using System.Text;

namespace MindTouch.IO {

    /// <summary>
    /// Provides a wrapping reader around any <see cref="TextReader"/> implementation to trim leading and trailing whitespace.
    /// </summary>
    public class TrimmingTextReader : TextReader {

        //--- Constants ---
        private const int BUFFER_SIZE = 256;

        //--- Fields ---
        private readonly TextReader _original;
        private readonly StringBuilder _buffer;
        private int _position;
        private bool _endOfStream = false;

        //--- Constructors ---

        /// <summary>
        /// Wrap a <see cref="TextReader"/> with the trimming reader.
        /// </summary>
        /// <param name="original">Original text reader instance.</param>
        public TrimmingTextReader(TextReader original) {
            if(original == null) {
                throw new ArgumentNullException("original");
            }
            _buffer = new StringBuilder();
            _original = original;

            // skip leading whitespace characters
            int ch = _original.Peek();
            if(ch == 0xFEFF) {

                // skip BOM character
                _original.Read();
                ch = _original.Peek();
            }
            for(; (ch >= 0) && char.IsWhiteSpace((char)ch); ch = _original.Peek()) {
                _original.Read();
            }
        }

        //--- Methods ---
        /// <summary>
        /// Override of <see cref="TextReader.Dispose(bool)"/> that also disposes any wrapped <see cref="TextReader"/> instance.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing) {
            try {
                if(disposing) {
                    _original.Dispose();
                }
            } finally {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Reads the next character without changing the state of the reader or the character source.
        /// Returns the next available character without actually reading it from the input stream.
        /// </summary>
        /// <returns>
        /// An integer representing the next character to be read, or -1 if no more characters are available or the stream does not support seeking.
        /// </returns>
        public override int Peek() {
            int ch;

            // check if we can peek a character from the buffer
            if(_position < _buffer.Length) {
                ch = _buffer[_position];
            } else {

                // accumulate whitespace characters until we determine if we have reached the end of the reader or not
                for(ch = _original.Peek(); (ch >= 0) && char.IsWhiteSpace((char)ch); ch = _original.Peek()) {
                    _buffer.Append((char)_original.Read());
                }

                // check if we found a non-whitespace character
                if(ch >= 0) {

                    // check if we buffered any characters
                    if(_position < _buffer.Length) {
                        ch = _buffer[_position];
                    }
                } else if(_buffer.Length > 0) {

                    // reset the buffer
                    _buffer.Length = 0;
                    _position = 0;
                }
            }
            return ch;
        }

        /// <summary>
        /// Reads a buffer of characters from the reader.
        /// </summary>
        /// <param name="buffer">Character buffer to fill.</param>
        /// <param name="index">Index into buffer to start filling at.</param>
        /// <param name="count">Maximum number of characters to read.</param>
        /// <returns>Number of characters actually read. 0 if the reader has reached the end of the underlying reader.</returns>
        public override int Read(char[] buffer, int index, int count) {

            // try to service the request from the buffer
            if(_position < _buffer.Length) {
                return CopyStringBuffer(buffer, index, count);
            }

            // we're done, don't bother checking again
            if(_endOfStream) {
                return 0;
            }

            // read from original reader
            var read = _original.Read(buffer, index, count);

            // if we read nothing just pass it through
            if(read == 0) {
                _endOfStream = true;
                return 0;
            }

            // if the last read character is not a whitespace, just pass it through
            if(!char.IsWhiteSpace(buffer[read - 1])) {
                return read;
            }

            // fill the buffer until we find the end of the file or a trailing non-whitespace
            _buffer.Append(buffer, index, read);
            var innerBuffer = new char[BUFFER_SIZE];
            while(true) {
                read = _original.Read(innerBuffer, 0, BUFFER_SIZE);

                // read no bytes, nothing more to get from original
                if(read == 0) {
                    _endOfStream = true;
                    break;
                }
                _buffer.Append(innerBuffer, 0, read);

                // if the last character is not a whitespace, we can drop out of the read loop,
                // since we don't have trailing whitespace mucking up the buffer
                if(!char.IsWhiteSpace(innerBuffer[read - 1])) {
                    break;
                }
            }
            return CopyStringBuffer(buffer, index, count);
        }

        /// <summary>
        /// Reads the next character from the input stream and advances the character position by one character.
        /// </summary>
        /// <returns>
        /// The next character from the input stream, or -1 if no more characters are available. The default implementation returns -1.
        /// </returns>
        public override int Read() {
            int ch = -1;

            // check if we can read a character from the buffer
            if(_position < _buffer.Length) {
                ch = _buffer[_position++];

                // check if we have exhausted the buffer or reached the end of the reader
                if(_position == _buffer.Length) {

                    // reset the buffer
                    _buffer.Length = 0;
                    _position = 0;
                }
            } else if(!_endOfStream) {

                // accumulate characters until we determine if we have reached the end of the reader or not
                do {
                    ch = _original.Read();
                    _buffer.Append((char)ch);
                } while(ch >= 0 && char.IsWhiteSpace((char)ch));

                // check if we reached the end
                if(ch == -1) {
                    _endOfStream = true;
                    if(_buffer.Length > 0) {

                        // reset the buffer
                        _buffer.Length = 0;
                        _position = 0;
                    }
                } else {

                    // return the current position from the buffer (there'll be at least one char in there)
                    ch = _buffer[_position++];

                    // check if we have exhausted the buffer or reached the end of the reader
                    if(_position == _buffer.Length) {

                        // reset the buffer
                        _buffer.Length = 0;
                        _position = 0;
                    }
                }
            }
            return ch;
        }

        /// <summary>
        /// Reads all characters from the current position to the end of the TextReader and returns them as one string.
        /// </summary>
        /// <returns>A string containing all characters from the current position to the end of the TextReader.</returns>
        public override string ReadToEnd() {
        
            // NOTE (steveb): Mono 2.8.2 does not implement TextReader.ReadToEnd() properly (see https://bugzilla.novell.com/show_bug.cgi?id=655934);
            //                once fixed, this code can be removed.
        
            var result = new StringBuilder();
            for(var c = Read(); c >= 0; c = Read()) {
                result.Append((char)c);
            }
            return result.ToString();
        }
        
        /// <summary>
        /// Reads a line of characters from the current stream and returns the data as a string.
        /// </summary>
        /// <returns>The next line from the input stream, or null if all characters have been read.</returns>
        public override string ReadLine() {
        
            // NOTE (steveb): Mono 2.8.2 does not implement TextReader.ReadLine() properly (see https://bugzilla.novell.com/show_bug.cgi?id=655934);
            //                once fixed, this code can be removed.
        
            StringBuilder result = null;
            for(var c = Read(); c >= 0; c = Read()) {
        
                // lazy initialize string buffer so we can detect the case where we had already reached the end of the reader
                result = result ?? new StringBuilder();
        
                // check simple character line ending
                if(c == '\r') {
                    if(Peek() == '\n') {
                        Read();
                    }
                    break;
                } else if(c == '\n') {
                    break;
                } else {
                    result.Append((char)c);

                    // check if buffered sequence matches Environment.NewLine
                    if(result.Length >= Environment.NewLine.Length) {
                        var match = true;
                        for(int resultIndex = result.Length - 1, newlineIndex = Environment.NewLine.Length - 1; newlineIndex >= 0 && match; --resultIndex, --newlineIndex) {
                            match = (result[resultIndex] == Environment.NewLine[newlineIndex]);
                        }
                        if(match) {
                            result.Remove(result.Length - Environment.NewLine.Length, Environment.NewLine.Length);
                            break;
                        }
                    }
                }
            }
            return (result != null) ? result.ToString() : null;
        }

        private int CopyStringBuffer(char[] buffer, int index, int count) {
            var lengthOfBuffer = _buffer.Length;
            if(_endOfStream) {

                // find last non-whitespace
                for(int i = _buffer.Length - 1; i >= 0; i--) {
                    if(!char.IsWhiteSpace(_buffer[i])) {
                        lengthOfBuffer = i + 1;
                        break;
                    }
                }
            }
            var copyCount = Math.Min(count, lengthOfBuffer - _position);
            _buffer.CopyTo(_position, buffer, index, copyCount);
            _position += copyCount;

            // if position has caught up with the buffer, reset the buffer
            if(_position == lengthOfBuffer) {

                // reset the buffer
                _buffer.Length = 0;
                _position = 0;
            }
            return copyCount;
        }
    }
}
