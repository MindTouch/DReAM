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
using MindTouch.Xml;

namespace MindTouch.Aws {

    /// <summary>
    /// Container for data returned from <see cref="IAwsS3Client.GetDataInfo"/>.
    /// </summary>
    public class AwsS3DataInfo {

        //--- Fields ---
        private readonly AwsS3FileHandle _fileHandle;
        private readonly XDoc _directoryDocument;

        //--- Constructors ---

        /// <summary>
        /// Create a directory data info instance.
        /// </summary>
        /// <param name="directoryDocument">Directory document.</param>
        public AwsS3DataInfo(XDoc directoryDocument) {
            _directoryDocument = directoryDocument;
        }

        /// <summary>
        /// Create a file data info instance.
        /// </summary>
        /// <param name="fileHandle">File info instance.</param>
        public AwsS3DataInfo(AwsS3FileHandle fileHandle) {
            _fileHandle = fileHandle;
        }

        //--- Methods ---

        /// <summary>
        /// True if the data info refers to directory
        /// </summary>
        public bool IsDirectory { get { return _directoryDocument != null; } }

        /// <summary>
        /// Return the underlying directory document.
        /// </summary>
        /// <returns>Directory document.</returns>
        public XDoc AsDirectoryDocument() {
            return _directoryDocument;
        }

        /// <summary>
        /// Return the underlying file handle.
        /// </summary>
        /// <returns>File handle instance.</returns>
        public AwsS3FileHandle AsFileHandle() {
            return _fileHandle;
        }
    }
}