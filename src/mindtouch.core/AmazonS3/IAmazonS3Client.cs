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
using MindTouch.Dream.Services;

namespace MindTouch.Dream.AmazonS3 {

    /// <summary>
    /// Amazon S3 Client abstraction for use by <see cref="S3StorageService"/>
    /// </summary>
    /// <remarks>This class is deprecated and has been replaced with MindTouch.Aws.IAwsS3Client. It will be removed in a future version.</remarks>
    [Obsolete("This class has been replaced with MindTouch.Aws.IAwsS3Client and will be removed in a future version")]
    public interface IAmazonS3Client : IDisposable {
        
        //--- Methods ---

        /// <summary>
        /// Retrieve file or directory information at given path.
        /// </summary>
        /// <param name="path">Path to retrieve.</param>
        /// <param name="head">Perform a HEAD request only.</param>
        /// <returns></returns>
        AmazonS3DataInfo GetDataInfo(string path, bool head);

        /// <summary>
        /// Store a file at a path.
        /// </summary>
        /// <param name="path">Storage path.</param>
        /// <param name="fileInfo">File to store.</param>
        void PutFile(string path, AmazonS3FileHandle fileInfo);

        /// <summary>
        /// Delete a file or directory.
        /// </summary>
        /// <param name="path">Path to delete.</param>
        void Delete(string path);
    }
}