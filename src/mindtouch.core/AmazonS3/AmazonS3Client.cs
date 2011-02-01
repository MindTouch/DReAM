/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2010 MindTouch, Inc.
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
using System.Linq;
using log4net;
using MindTouch.Collections;
using MindTouch.Dream.Services;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.AmazonS3 {

    /// <summary>
    /// Amazon S3 Client abstraction for use by <see cref="S3StorageService"/>
    /// </summary>
    public class AmazonS3Client : IAmazonS3Client {

        //--- Constants ---
        private const string EXPIRE = "X-Amz-Meta-Expire";
        private const string TTL = "X-Amz-Meta-TTL";

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly AmazonS3ClientConfig _config;
        private readonly Plug _bucketPlug;
        private readonly Plug _rootPlug;
        private readonly ExpiringHashSet<string> _expirationEntries;
        private readonly string[] _keyRootParts;

        //--- Constructors ---

        /// <summary>
        /// Create new client instance 
        /// </summary>
        /// <param name="config">Client configuration.</param>
        /// <param name="timerFactory">Timer factory.</param>
        public AmazonS3Client(AmazonS3ClientConfig config, TaskTimerFactory timerFactory) {
            _config = config;
            _bucketPlug = Plug.New(_config.S3BaseUri)
                .WithS3Authentication(_config.PrivateKey, _config.PublicKey)
                .WithTimeout(_config.Timeout)
                .At(_config.Bucket);
            _rootPlug = _bucketPlug;
            if(!string.IsNullOrEmpty(_config.RootPath)) {
                _keyRootParts = _config.RootPath.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                if(_keyRootParts != null && _keyRootParts.Any()) {
                    _rootPlug = _rootPlug.At(_keyRootParts);
                }
            }
            _expirationEntries = new ExpiringHashSet<string>(timerFactory);
            _expirationEntries.EntryExpired += OnDelete;
        }

        //--- Methods ---
        /// <summary>
        /// Retrieve file or directory information at given path.
        /// </summary>
        /// <param name="path">Path to retrieve.</param>
        /// <param name="head">Perform a HEAD request only.</param>
        /// <returns></returns>
        public AmazonS3DataInfo GetDataInfo(string path, bool head) {
            return IsDirectoryPath(path)
                ? GetDirectory(path, head)
                : GetFile(path, head);
        }

        /// <summary>
        /// Store a file at a path.
        /// </summary>
        /// <param name="path">Storage path.</param>
        /// <param name="fileHandle">File to store.</param>
        public void PutFile(string path, AmazonS3FileHandle fileHandle) {
            if(IsDirectoryPath(path)) {
                throw new InvalidOperationException(string.Format("cannot put a file at a path with a trailing {0}", _config.Delimiter));
            }
            var p = _rootPlug.AtPath(path);
            if(fileHandle.TimeToLive.HasValue) {
                var expiration = fileHandle.Expiration ?? DateTime.UtcNow.Add(fileHandle.TimeToLive.Value);
                p = p.WithHeader(EXPIRE, expiration.ToEpoch().ToString())
                    .WithHeader(TTL, fileHandle.TimeToLive.Value.TotalSeconds.ToString());
                _expirationEntries.SetExpiration(path, expiration, fileHandle.TimeToLive.Value);
            }
            var request = DreamMessage.Ok(fileHandle.MimeType, fileHandle.Size, fileHandle.Stream);
            var response = p.Put(request, new Result<DreamMessage>()).Wait();
            if(response.IsSuccessful) {
                return;
            }
            throw new DreamResponseException(response);
        }

        /// <summary>
        /// Delete a file or directory.
        /// </summary>
        /// <param name="path">Path to delete.</param>
        public void Delete(string path) {
            if(IsDirectoryPath(path)) {
                DeleteDirectory(path);
            } else {
                DeleteFile(path);
            }
        }

        /// <summary>
        /// See <see cref="IDisposable.Dispose"/>.
        /// </summary>
        public void Dispose() {
            _expirationEntries.EntryExpired -= OnDelete;
            _expirationEntries.Dispose();
        }

        private AmazonS3DataInfo GetFile(string path, bool head) {
            var response = _rootPlug.AtPath(path).InvokeEx(head ? "HEAD" : "GET", DreamMessage.Ok(), new Result<DreamMessage>()).Wait();
            if(response.Status == DreamStatus.NotFound) {
                response.Close();
                return null;
            }
            if(!response.IsSuccessful) {
                response.Memorize(new Result()).Wait();
                throw new DreamResponseException(response);
            }

            // got a file
            var expireEpoch = response.Headers[EXPIRE];
            var expiration = string.IsNullOrEmpty(expireEpoch) ? (DateTime?)null : DateTimeUtil.FromEpoch(SysUtil.ChangeType<uint>(expireEpoch));
            var ttlString = response.Headers[TTL];
            var ttl = string.IsNullOrEmpty(ttlString) ? (TimeSpan?)null : TimeSpan.FromSeconds(SysUtil.ChangeType<double>(ttlString));
            if(expiration.HasValue && ttl.HasValue) {
                if(DateTime.UtcNow > expiration) {

                    // lazy expiration
                    _log.DebugFormat("lazy expiration of {0}", path);
                    _expirationEntries.Delete(path);
                    response.Close();
                    return null;
                }
                _expirationEntries.SetExpiration(path, expiration.Value, ttl.Value);
            }
            var filehandle = new AmazonS3FileHandle {
                Expiration = expiration,
                TimeToLive = ttl,
                Size = response.ContentLength,
                MimeType = response.ContentType,
                Modified = response.Headers.LastModified ?? DateTime.UtcNow,
                Stream = head ? null : response.ToStream(),
            };
            if(head) {
                response.Close();
            }
            return new AmazonS3DataInfo(filehandle);
        }

        private AmazonS3DataInfo GetDirectory(string path, bool head) {
            string marker = null;
            var hasItems = false;
            var doc = new XDoc("files");
            while(true) {
                var p = _bucketPlug.With("delimiter", _config.Delimiter).With("prefix", GetRootedPath(path));
                if(!string.IsNullOrEmpty(marker)) {
                    p = p.With("marker", marker);
                }
                var dirResponse = p.Get(new Result<DreamMessage>()).Wait();
                var dirDoc = dirResponse.ToDocument().UsePrefix("aws", "http://s3.amazonaws.com/doc/2006-03-01/");
                if(!dirResponse.IsSuccessful) {
                    throw new Exception(string.Format("{0}: {1}", dirDoc["Code"].AsText, dirDoc["Message"].AsText));
                }

                // list directories
                foreach(var dir in dirDoc["aws:CommonPrefixes/aws:Prefix"]) {
                    string last = GetName(dir.AsText);
                    if(string.IsNullOrEmpty(last)) {
                        continue;
                    }
                    doc.Start("folder")
                        .Elem("name", last)
                        .End();
                    hasItems = true;
                }

                // list files
                foreach(var file in dirDoc["aws:Contents"]) {
                    var filename = GetName(file["aws:Key"].AsText);
                    var size = file["aws:Size"].AsText;
                    var modified = file["aws:LastModified"].AsDate ?? DateTime.MaxValue;

                    // Note (arnec): The S3 version of the Storage Service does not track creation time and never reports on expirations
                    // in listings
                    doc.Start("file")
                        .Elem("name", filename)
                        .Elem("size", size)
                        .Elem("date.created", modified)
                        .Elem("date.modified", modified)
                        .End();
                    hasItems = true;
                }
                marker = dirDoc["aws:NextMarker"].AsText;
                if(!string.IsNullOrEmpty(marker) && !head) {

                    // there are more items in this directory
                    continue;
                }
                if(!hasItems) {

                    // no items, i.e. no file or directory at path
                    return null;
                }
                return new AmazonS3DataInfo(head ? new XDoc("files") : doc);
            }
        }

        private void OnDelete(object sender, ExpirationEventArgs<string> e) {
            var filepathEntry = e.Entry;
            if(filepathEntry.When > DateTime.UtcNow) {
                _log.DebugFormat("Ignoring premature expiration event for '{0}' scheduled for '{1}'", filepathEntry.Value, filepathEntry.When);
                return;
            }
            DeleteFile(filepathEntry.Value);
        }

        private void DeleteFile(string path) {
            var response = _rootPlug.AtPath(path).Delete(new Result<DreamMessage>()).Wait();
            if(response.Status != DreamStatus.NoContent) {

                // request failed, bail
                throw new DreamResponseException(response);
            }
            _expirationEntries.Delete(path);
        }

        private void DeleteDirectory(string path) {
            string marker = null;
            while(true) {
                var p = _bucketPlug.With("prefix", GetRootedPath(path));
                if(!string.IsNullOrEmpty(marker)) {
                    p = p.With("marker", marker);
                }
                var response = p.Get(new Result<DreamMessage>()).Wait();
                var dirDoc = response.ToDocument().UsePrefix("aws", "http://s3.amazonaws.com/doc/2006-03-01/");
                if(!response.IsSuccessful) {
                    throw new Exception(string.Format("{0}: {1}", dirDoc["Code"].AsText, dirDoc["Message"].AsText));
                }
                foreach(var keyDoc in dirDoc["aws:Contents/aws:Key"]) {
                    var key = keyDoc.AsText;
                    response = _bucketPlug.AtPath(key).Delete(new Result<DreamMessage>()).Wait();
                    if(!response.IsSuccessful && response.Status != DreamStatus.NoContent) {
                        throw new DreamResponseException(response);
                    }
                    _expirationEntries.Delete(key);
                }
                marker = dirDoc["aws:NextMarker"].AsText;
                if(string.IsNullOrEmpty(marker)) {

                    // there are no more items
                    return;
                }
            }
        }

        private string GetName(string path) {
            return path.Split(new[] { _config.Delimiter }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        }

        private string GetRootedPath(string path) {
            if(_keyRootParts == null || !_keyRootParts.Any()) {
                return path;
            }
            return string.Join(_config.Delimiter, ArrayUtil.Concat(_keyRootParts, new[] { path }));
        }

        private bool IsDirectoryPath(string path) {
            return path.EndsWith(_config.Delimiter);
        }
    }
}