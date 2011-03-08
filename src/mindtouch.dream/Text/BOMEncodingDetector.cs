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

using System.IO;
using System.Text;

namespace MindTouch.Text {

    /// <summary>
    /// Text encoding detector implementation checking for a stream byte-order marker (BOM).
    /// </summary>
    public class BOMEncodingDetector : IEncodingDetector {

        //--- Methods ---

        /// <summary>
        /// Detect the encoding for a given stream.
        /// </summary>
        /// <param name="stream">Stream to examine</param>
        /// <returns>Detected encoding or null.</returns>
        public Encoding Detect(Stream stream) {
            long position = stream.Position;
            int bytecode = stream.ReadByte();
            if(bytecode == 0xEF) {
                
                // possibly UTF-8
                bytecode = stream.ReadByte();
                if(bytecode == 0xBB) {
                    bytecode = stream.ReadByte();
                    if(bytecode == 0xBF) {
                        return Encoding.UTF8;
                    }
                    goto failed;
                }
                goto failed;
            }
            if(bytecode == 0xFF) {
                
                // possibly little-endian UTF-16
                bytecode = stream.ReadByte();
                if(bytecode == 0xFE) {
                    return Encoding.Unicode;
                }
                goto failed;
            }
            if(bytecode == 0xFE) {
                
                // possibly big-endian UTF-16
                bytecode = stream.ReadByte();
                if(bytecode == 0xFF) {
                    return Encoding.BigEndianUnicode;
                }
                goto failed;
            }
        failed:

            // restore the stream position where we started
            stream.Position = position;
            return null;
        }
    }
}
