/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
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

namespace MindTouch.Dream.AmazonS3 {

    /// <summary>
    /// File Handle container.
    /// </summary>
    /// <remarks>This class is deprecated and has been replaced with MindTouch.Aws.AwsS3FileHandle. It will be removed in a future version.</remarks>
    [Obsolete("This class has been replaced with MindTouch.Aws.AwsS3FileHandle and will be removed in a future version")]
    public class AmazonS3FileHandle {

        /// <summary>
        /// File modification, if the file was retrieved from S3.
        /// </summary>
        public DateTime Modified { get; set; }

        /// <summary>
        /// Size of the file.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// File data stream (null, if this handle refers to a HEAD request.
        /// </summary>
        public Stream Stream { get; set; }

        /// <summary>
        /// File mime type.
        /// </summary>
        public MimeType MimeType { get; set; }

        /// <summary>
        /// File expiration date.
        /// </summary>
        public DateTime? Expiration { get; set; }

        /// <summary>
        /// File time-to-live.
        /// </summary>
        public TimeSpan? TimeToLive { get; set; }
    }
}