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
using MindTouch.Text.CharDet;

namespace MindTouch.Text {

    /// <summary>
    /// Text encoding detector using stream sampling to detect character encoding.
    /// </summary>
    public class CharacterEncodingDetector : IEncodingDetector {

        //--- Constants ---
        private const int DEFAULT_MAX_SAMPLE_SIZE = 1024;
        private const int BUFFER_SIZE = 1024;

        //--- Fields ---
        private readonly int _maxSampleSize;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance.
        /// </summary>
        public CharacterEncodingDetector() : this(DEFAULT_MAX_SAMPLE_SIZE) { }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="maxSampleSize">Maximum number of bytes to examine.</param>
        public CharacterEncodingDetector(int maxSampleSize) {
            _maxSampleSize = maxSampleSize;
        }

        //--- Methods ---

        /// <summary>
        /// Detect the encoding for a given stream.
        /// </summary>
        /// <param name="stream">Stream to examine</param>
        /// <returns>Detected encoding or null.</returns>
        public Encoding Detect(Stream stream) {
            var buffer = new byte[BUFFER_SIZE];
            var detector = new Detector(Language.ALL);
            long position = stream.Position;

            // process stream until we have a result or have exhausted the sample size
            var remainingSampleSize = _maxSampleSize;
            while((remainingSampleSize > 0) && (detector.Result == null)) {
                int read = stream.Read(buffer, 0, Math.Min(remainingSampleSize, buffer.Length));

                // check if we reached the end of the stream
                if(read == 0) {
                    break;
                }
                remainingSampleSize -= read;

                // feed sample to character encoding detector
                detector.HandleData(buffer, read);
            }
            detector.DataEnd();

            // restore stream position and return the detector's result
            stream.Position = position;
            return detector.Result;
        }
    }
}
