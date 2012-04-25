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
using System.IO;
using System.Linq;
using log4net;
using MindTouch.Collections;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Dream Storage", "Copyright (c) 2006-2011 MindTouch, Inc.",
        Info = "http://developer.mindtouch.com/Dream/Reference/Services/Storage",
        SID = new string[] { 
            "sid://mindtouch.com/2007/03/dream/storage",
            "sid://mindtouch.com/2007/07/dream/storage.private",
            "http://services.mindtouch.com/dream/stable/2007/03/storage",
            "http://services.mindtouch.com/dream/draft/2007/07/storage.private" 
        }
    )]
    [DreamServiceConfig("folder", "path", "Rooted path to the folder managed by the storeage service.")]
    internal class StorageService : DreamService {

        //--- Constants ---
        private const string STATE_FILENAME = "storage.state.xml";
        private const string META = ".meta";

        // --- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private string _path;
        private bool _private;
        private bool _privateRoot;
        private ExpiringHashSet<string> _expirationEntries;

        //--- Features ---
        [DreamFeature("GET://*", "Retrieve a file or a list of all files and folders at the specified path")]
        [DreamFeature("HEAD://*", "Retrieve information about a file or folder from the storage folder")]
        public Yield GetFileOrFolderListing(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            bool head = StringUtil.EqualsInvariant(context.Verb, "HEAD");
            string path = GetPath(context);

            DreamMessage result;
            if(File.Exists(path)) {

                // dealing with a file request 
                TouchMeta(path);

                // check if request contains a 'if-modified-since' header
                var lastmodified = File.GetLastWriteTime(path);
                if(request.CheckCacheRevalidation(lastmodified) && (lastmodified.Year >= 1900)) {
                    response.Return(DreamMessage.NotModified());
                    yield break;
                }

                // retrieve file
                try {
                    result = DreamMessage.FromFile(path, head);
                } catch(FileNotFoundException) {
                    result = DreamMessage.NotFound("file not found");
                } catch(Exception) {
                    result = DreamMessage.BadRequest("invalid path");
                }

                // add caching headers if file was found
                if(!head && result.IsSuccessful) {

                    // add caching information; this will avoid unnecessary data transfers by user-agents with caches
                    result.SetCacheMustRevalidate(lastmodified);
                }
            } else if(Directory.Exists(path)) {

                // dealing with a directory request
                if(head) {

                    // HEAD for a directory doesn't really mean anything, so we just return ok, to indicate that it exists
                    result = DreamMessage.Ok();
                } else {
                    var doc = new XDoc("files");

                    // list directory contents
                    var directories = Directory.GetDirectories(path);
                    foreach(var dir in directories) {
                        if(dir.EndsWithInvariantIgnoreCase(META)) {
                            continue;
                        }
                        doc.Start("folder")
                            .Elem("name", Path.GetFileName(dir))
                            .End();
                    }
                    foreach(var filepath in Directory.GetFiles(path)) {
                        var file = new FileInfo(filepath);
                        doc.Start("file")
                            .Elem("name", file.Name)
                            .Elem("size", file.Length)
                            .Elem("date.created", file.CreationTimeUtc)
                            .Elem("date.modified", file.LastWriteTimeUtc);
                        var entry = SyncMeta(filepath);
                        if(entry != null) {
                            doc.Elem("date.expire", entry.When);
                            doc.Elem("date.ttl", entry.TTL);
                        }
                        doc.End();
                    }
                    result = DreamMessage.Ok(doc);
                }
            } else {

                // nothin here
                result = DreamMessage.NotFound("no such file or folder");
            }

            response.Return(result);
            yield break;

        }

        [DreamFeature("PUT://*", "Add a file at a specified path")]
        [DreamFeatureParam("ttl", "int", "time-to-live in seconds for the posted event")]
        public Yield PutFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string filepath = GetPath(context);
            string folderpath = Path.GetDirectoryName(filepath);
            double ttl = context.GetParam("ttl", 0.0);
            TimeSpan? timeToLive = null;
            if(ttl > 0.0) {
                timeToLive = TimeSpan.FromSeconds(ttl);
            }
            if(Directory.Exists(filepath)) {

                // filepath is actually an existing directory
                response.Return(DreamMessage.Conflict("there exists a directory at the specified file path"));
                yield break;
            }

            // create folder if need be
            if(!Directory.Exists(folderpath)) {
                Directory.CreateDirectory(folderpath);
            }

            // save request stream in target file
            DreamMessage result;
            try {
                request.ToStream().CopyToFile(filepath, request.ContentLength);
                WriteMeta(filepath, timeToLive, null);
                result = DreamMessage.Ok();
            } catch(DirectoryNotFoundException) {
                result = DreamMessage.NotFound("directory not found");
            } catch(PathTooLongException) {
                result = DreamMessage.BadRequest("path too long");
            } catch(NotSupportedException) {
                result = DreamMessage.BadRequest("not supported");
            }
            response.Return(result);
            yield break;
        }

        [DreamFeature("DELETE://*", "Delete file from the storage folder")]
        public Yield DeleteFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var path = GetPath(context);
            var result = DreamMessage.Ok();
            if(Directory.Exists(path)) {

                // folder delete
                try {
                    Directory.Delete(path, true);
                } catch { }
            } else if(File.Exists(path)) {

                // delete target file
                try {
                    _expirationEntries.Delete(path);
                    try {
                        File.Delete(path);
                    } catch { }
                    WriteMeta(path, null, null);
                } catch(FileNotFoundException) {
                } catch(DirectoryNotFoundException) {
                } catch(PathTooLongException) {
                    result = DreamMessage.BadRequest("path too long");
                } catch(NotSupportedException) {
                    result = DreamMessage.BadRequest("not supported");
                }

                // try to clean up empty directory
                string folderpath = Path.GetDirectoryName(path);
                if(Directory.Exists(folderpath) && (Directory.GetFileSystemEntries(folderpath).Length == 0)) {
                    try {
                        Directory.Delete(folderpath);
                    } catch { }
                }
            }
            response.Return(result);
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // are we a private storage service?
            _private = config["sid"].Contents == "sid://mindtouch.com/2007/07/dream/storage.private";
            _log.DebugFormat("storage is {0}", _private ? "private" : "public");

            // is the root blocked from access?
            _privateRoot = config["private-root"].AsBool.GetValueOrDefault();
            _log.DebugFormat("storage root is {0}accessible", _privateRoot ? "not " : "");
            _expirationEntries = new ExpiringHashSet<string>(TimerFactory);
            _expirationEntries.EntryExpired += OnDelete;

            // check if folder exists
            _path = Environment.ExpandEnvironmentVariables(config["folder"].Contents);
            _log.DebugFormat("storage path: {0}", _path);
            if(!Path.IsPathRooted(_path)) {
                throw new ArgumentException(string.Format("storage path must be absolute: {0}", _path));
            }

            // make sure path ends with a '\' as it makes processing simpler later on
            if((_path.Length != 0) && ((_path[_path.Length - 1] != '/') || (_path[_path.Length - 1] != '\\'))) {
                _path += Path.DirectorySeparatorChar;
            }

            if(!_private && !Directory.Exists(_path)) {
                throw new ArgumentException(string.Format("storage path does not exist: {0}", _path));
            }

            // Fire off meta data scanning
            AsyncUtil.Fork(ScanMetaData);
            result.Return();
        }

        protected override Yield Stop(Result result) {
            _expirationEntries.Dispose();
            _expirationEntries.EntryExpired -= OnDelete;
            _expirationEntries = null;
            _path = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        public override DreamFeatureStage[] Prologues {
            get {
                return new[] { 
                    new DreamFeatureStage("check-private-storage-access", this.ProloguePrivateStorage, DreamAccess.Public), 
                };
            }
        }

        private Yield ProloguePrivateStorage(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            //check if this services is private
            if(_private) {
                DreamCookie cookie = DreamCookie.GetCookie(request.Cookies, "service-key");
                if(cookie == null || cookie.Value != PrivateAccessKey) {
                    throw new DreamForbiddenException("insufficient access privileges");
                }
            }
            response.Return(request);
            yield break;
        }

        private string GetPath(DreamContext context) {
            string[] parts = context.GetSuffixes(UriPathFormat.Decoded);
            string path = _path;
            foreach(string part in parts) {
                if(part.EqualsInvariant("..")) {
                    throw new DreamBadRequestException("paths cannot contain '..'");
                }
                path = Path.Combine(path, part);
            }
            if(_privateRoot && (parts.Length == 0 || (parts.Length == 1 && !Directory.Exists(path)))) {
                throw new DreamForbiddenException("Root level access is forbidden for this storage service");
            }
            return path;
        }

        private void TouchMeta(string path) {
            var meta = SyncMeta(path);
            if(meta != null) {
                WriteMeta(path, meta.TTL, null);
            }
        }

        private void ScanMetaData() {
            ScanMetaData(_path);
            MigrateLegacyStateFile();
        }

        private void MigrateLegacyStateFile() {

            // check if state file exists, and convert state to meta files
            var statefile = Path.Combine(_path, STATE_FILENAME);
            if(!File.Exists(statefile)) {
                return;
            }
            var state = XDocFactory.LoadFrom(statefile, MimeType.XML);

            // restore file expiration list
            foreach(var entry in state["file"]) {
                var filepath = Path.Combine(_path, entry["path"].Contents);
                var when = entry["date.expire"].AsDate;
                var ttl = TimeSpan.FromSeconds(entry["date.ttl"].AsDouble ?? 0);
                if(!File.Exists(filepath) || !when.HasValue) {
                    continue;
                }
                _log.DebugFormat("migrating file meta data. Scheduled for deletion at {0}", when);
                WriteMeta(filepath, ttl, when);
            }
            File.Delete(statefile);
        }

        private void ScanMetaData(string path) {
            foreach(var file in Directory.GetFiles(path)) {
                SyncMeta(file);
            }
            foreach(var directory in Directory.GetDirectories(path)) {
                if(directory.EqualsInvariantIgnoreCase(META)) {
                    continue;
                }
                ScanMetaData(directory);
            }
        }

        private ExpiringHashSet<string>.Entry SyncMeta(string filePath) {
            lock(_expirationEntries) {
                DateTime? expire = null;
                var ttl = TimeSpan.Zero;
                var metaPath = GetMetaPath(filePath);
                if(File.Exists(metaPath)) {
                    var meta = XDocFactory.LoadFrom(metaPath, MimeType.XML);
                    expire = meta["expire.date"].AsDate;
                    ttl = TimeSpan.FromSeconds(meta["expire.ttl"].AsDouble ?? 0);
                }
                if(expire.HasValue) {

                    // set up expiration
                    _expirationEntries.SetOrUpdate(filePath, expire.Value, ttl);
                } else {

                    // no expiration anymore, so expiration needs to be removed
                    _expirationEntries.Delete(filePath);
                }
                return _expirationEntries[filePath];
            }
        }

        private void WriteMeta(string filePath, TimeSpan? ttl, DateTime? when) {
            lock(_expirationEntries) {
                var metaPath = GetMetaPath(filePath);
                if(ttl.HasValue) {

                    // set up expiration and write to meta file
                    if(when.HasValue) {
                        _expirationEntries.SetOrUpdate(filePath, when.Value, ttl.Value);
                    } else {
                        _expirationEntries.SetOrUpdate(filePath, ttl.Value);
                        when = _expirationEntries[filePath].When;
                    }
                    var meta = new XDoc("meta")
                        .Elem("expire.ttl", ttl.Value.TotalSeconds)
                        .Elem("expire.date", when.Value);
                    Directory.CreateDirectory(Path.GetDirectoryName(metaPath));
                    meta.Save(metaPath);
                } else {

                    // remove expiration and remove it from meta file
                    _expirationEntries.Delete(filePath);
                    if(File.Exists(metaPath)) {
                        try {
                            File.Delete(metaPath);
                        } catch {
                            // ignore file deletion exception

                            // BUG #806: we should try again in a few seconds; however, we need to be smart about it and count how often
                            //           we tried, otherwise we run the risk of bogging down the system b/c we're attempting to delete undeletable files.
                        }
                    }
                }
            }
        }

        private string GetMetaPath(string path) {
            var dir = Path.GetDirectoryName(path);
            var file = Path.GetFileName(path) + ".xml";
            return Path.Combine(dir, Path.Combine(META, file));
        }

        private void OnDelete(object sender, ExpirationEventArgs<string> e) {
            var filepathEntry = e.Entry;
            if(filepathEntry.When > DateTime.UtcNow) {
                _log.DebugFormat("Ignoring premature expiration event for '{0}' scheduled for '{1}'", filepathEntry.Value, filepathEntry.When);
                return;
            }
            var metaPath = GetMetaPath(filepathEntry.Value);
            if(File.Exists(filepathEntry.Value)) {
                try {
                    File.Delete(filepathEntry.Value);
                } catch {
                    // ignore file deletion exception

                    // BUG #806: we should try again in a few seconds; however, we need to be smart about it and count how often
                    //           we tried, otherwise we run the risk of bogging down the system b/c we're attempting to delete undeletable files.
                }
            }
            if(File.Exists(metaPath)) {
                try {
                    File.Delete(metaPath);
                } catch {
                    // ignore file deletion exception

                    // BUG #806: we should try again in a few seconds; however, we need to be smart about it and count how often
                    //           we tried, otherwise we run the risk of bogging down the system b/c we're attempting to delete undeletable files.
                }
            }
        }
    }
}
