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
using System.Collections;
using System.Collections.Generic;

namespace MindTouch.Collections {

    /// <summary>
    /// Create an array with chunked internal storage to avoid large object allocations.
    /// </summary>
    /// <typeparam name="T">Type of data to store in array.</typeparam>
    public class ChunkedArray<T> : IEnumerable<T> {

        //--- Constants ---
        private const int MAX_CHUNK_LENGTH = 16 * 1024;

        //--- Fields ---
        private readonly T[][] _chunks;
        private readonly int _length;
        private readonly int _chunkSize;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="length">Length of the array.</param>
        public ChunkedArray(int length) {
            var t = typeof(T);
            var itemSize = 4;
            if(t == typeof(DateTime)) {

                // Note (arnec): DateTime is a value type but cannot be used with sizeof or Marshal.SizeOf and appears to have a size 8 bytes in mono
                itemSize = 8;
            } else if(t.IsValueType) {
                try {
                    itemSize = System.Runtime.InteropServices.Marshal.SizeOf(t);
                } catch { }
            }
            _chunkSize = MAX_CHUNK_LENGTH / itemSize;
            _length = length;
            var fullchunks = length / _chunkSize;
            var remainder = length % _chunkSize;
            var chunkCount = fullchunks + (remainder == 0 ? 0 : 1);
            _chunks = new T[chunkCount][];
            for(var i = 0; i < fullchunks; i++) {
                _chunks[i] = new T[_chunkSize];
            }
            if(remainder > 0) {
                _chunks[fullchunks] = new T[remainder];
            }
        }

        //--- Properties ---

        /// <summary>
        /// Length of the array.
        /// </summary>
        public int Length { get { return _length; } }

        /// <summary>
        /// Number of chunks used by the array.
        /// </summary>
        public int ChunkCount { get { return _chunks.Length; } }

        /// <summary>
        /// Accessor for item in array.
        /// </summary>
        /// <param name="index">0 based index into array.</param>
        /// <returns>Value of item at index.</returns>
        public T this[int index] {
            get {
                var chunk = index / _chunkSize;
                var idx = index % _chunkSize;
                return _chunks[chunk][idx];
            }
            set {
                var chunk = index / _chunkSize;
                var idx = index % _chunkSize;
                _chunks[chunk][idx] = value;
            }
        }

        //--- IEnumerable<T> Members ---
        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            for(var i = 0; i < _chunks.Length; i++) {
                for(var j = 0; j < _chunks[i].Length; j++) {
                    yield return _chunks[i][j];
                }
            }
        }

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}
