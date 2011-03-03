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
    /// Provides a <see cref="TextReader"/> that automatically trims leading and trailing waitspace as it reads.
    /// </summary>
    public class TrimmingStringReader : TextReader {

        //--- Fields ---
        private string _text;
        private int _position;
        private readonly int _length;

        //--- Constructors ---

        /// <summary>
        /// Create a new text reader.
        /// </summary>
        /// <param name="text">Text to read.</param>
        public TrimmingStringReader(string text) {
            if(text == null) {
                throw new ArgumentNullException("text");
            }
            _text = text;

            // skip leading & trailing whitespace characters
            _position = 0;
            if((_text.Length > 0) && (_text[_position] == 0xFEFF)) {

                // skip BOM character
                ++_position;
            }
            for(; (_position < _text.Length) && char.IsWhiteSpace(_text[_position]); ++_position) { }
            for(_length = _text.Length; (_length > _position) && char.IsWhiteSpace(_text[_length - 1]); --_length) { }
        }

        //--- Methods ---

        /// <summary>
        /// Override of <see cref="TextReader.Dispose(bool)"/> that also clears out internal string storage.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing) {
            _text = null;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Reads the next character without changing the state of the reader or the character source.
        /// Returns the next available character without actually reading it from the input stream.
        /// </summary>
        /// <returns>
        /// An integer representing the next character to be read, or -1 if no more characters are available or the stream does not support seeking.
        /// </returns>
        public override int Peek() {
            if(_text == null) {
                throw new ObjectDisposedException("reader has already been closed or disposed");
            }
            return (_position < _text.Length) ? _text[_position] : -1;
        }

        /// <summary>
        /// Reads the next character from the input stream and advances the character position by one character.
        /// </summary>
        /// <returns>
        /// The next character from the input stream, or -1 if no more characters are available. The default implementation returns -1.
        /// </returns>
        public override int Read() {
            if(_text == null) {
                throw new ObjectDisposedException("reader has already been closed or disposed");
            }
            return (_position < _text.Length) ? _text[_position++] : -1;
        }

        /// <summary>
        /// Reads a maximum of count characters from the current stream and writes the data to buffer, beginning at index.
        /// </summary>
        /// <param name="buffer">
        /// When this method returns, contains the specified character array with the values between index and (index + count - 1)
        /// replaced by the characters read from the current source.
        /// </param>
        /// <param name="index">The place in buffer at which to begin writing.</param>
        /// <param name="count">
        /// The maximum number of characters to read. If the end of the stream is reached before count of characters is read into buffer,
        /// the current method returns.
        /// </param>
        /// <returns>
        /// The number of characters that have been read. The number will be less than or equal to count, depending on whether the data is 
        /// available within the stream. This method returns zero if called when no more characters are left to read.
        /// </returns>
        public override int Read(char[] buffer, int index, int count) {
            if(buffer == null) {
                throw new ArgumentNullException("buffer");
            }
            if(index < 0) {
                throw new ArgumentOutOfRangeException("index");
            }
            if(count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }
            if(index > (buffer.Length - count)) {
                throw new ArgumentOutOfRangeException("count");
            }
            if(_text == null) {
                throw new ObjectDisposedException("reader has already been closed or disposed");
            }
            count = Math.Min(count, _length - _position);
            if(count > 0) {
                _text.CopyTo(_position, buffer, index, count);
                _position += count;
            }
            return count;
        }

        /// <summary>
        /// Reads a line of characters from the current stream and returns the data as a string.
        /// </summary>
        /// <returns>The next line from the input stream, or null if all characters have been read.</returns>
        public override string ReadLine() {
            if(_text == null) {
                throw new ObjectDisposedException("reader has already been closed or disposed");
            }

            // TODO (steveb): by .Net spec, this should also detect Environment.NewLine sequence

            // loop over string until we find a newline character
            for(int current = _position; current < _length; ++current) {
                char ch = _text[current];

                // check if we found a newline character
                if((ch == '\r') || (ch == '\n')) {
                    string result = _text.Substring(_position, current - _position);
                    _position = current + 1;

                    // check if we need to skip an additional character (i.e. \r\n)
                    if((ch == '\r') && (_position < _length) && (_text[_position] == '\n')) {
                        _position++;
                    }
                    return result;
                }
            }
            int count = _length - _position;
            if(count > 0) {
                string result = _text.Substring(_position, count);
                _position = _length;
                return result;
            }
            return null;
        }

        /// <summary>
        /// Reads all characters from the current position to the end of the TextReader and returns them as one string.
        /// </summary>
        /// <returns>A string containing all characters from the current position to the end of the TextReader.</returns>
        public override string ReadToEnd() {
            if(_text == null) {
                throw new ObjectDisposedException("reader has already been closed or disposed");
            }
            string result = ((_position == 0) && (_length == _text.Length)) ? _text : _text.Substring(_position, _length - _position);
            _position = _length;
            return result;
        }
    }
}