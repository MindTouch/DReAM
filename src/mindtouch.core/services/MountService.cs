/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
using System.Collections.Generic;
using System.IO;
using log4net;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Dream Mount Service", "Copyright (c) 2006-2009 MindTouch, Inc.", 
        Info = "http://developer.mindtouch.com/Dream/Reference/Services/Mount",
        SID = new string[] { 
            "sid://mindtouch.com/2006/11/dream/mount",
            "http://services.mindtouch.com/dream/draft/2006/11/mount" 
       }
    )]
    [DreamServiceConfig("mount/*", "any", "Configuration for mounted service.")]
    [DreamServiceConfig("mount/@to", "string", "Identifier for mounted service.")]
    internal class MountService : DreamService {

        // --- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        IList<Plug> plugs = new List<Plug>();

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            foreach(XDoc mount in config["mount[@to]"]) {

                // TODO (steveb): this is poorly designed

                string path = mount["@to"].AsText;
                yield return CreateService(path, "sid://mindtouch.com/2006/11/dream/mountfilesystem", new XDoc("config").Add(mount), new Result<Plug>()).Set(v => plugs.Add(v));
            }
            result.Return();
        }

        protected override Yield Stop(Result result) {
            foreach(Plug plug in plugs) {
                yield return plug.Delete(new Result<DreamMessage>(TimeSpan.MaxValue)).CatchAndLog(_log);
            }
            plugs.Clear();
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }
    }

    [DreamService("MindTouch Dream Mount File System Service", "Copyright (c) 2006-2009 MindTouch, Inc.",
        Info = "http://developer.mindtouch.com/Dream/Reference/Services/Mount",
        SID = new string[] { 
            "sid://mindtouch.com/2006/11/dream/mountfilesystem",
            "http://services.mindtouch.com/dream/draft/2006/11/mountfilesystem" 
        }
    )]
    [DreamServiceConfig("mount", "path", "Path to local filesystem to mount (may contain environment variables).")]
    internal class MountFileSystemService : DreamService {

        //--- Class Methods ---
        private static void AddDirectories(DirectoryInfo dir, string pattern, XDoc doc) {
            foreach(DirectoryInfo subDir in pattern != "" ? dir.GetDirectories(pattern) : dir.GetDirectories()) {
                AddDirectory(subDir, doc);
            }
        }

        private static void AddFiles(DirectoryInfo dir, string pattern, XDoc doc) {
            foreach(FileInfo file in pattern != "" ? dir.GetFiles(pattern) : dir.GetFiles()) {
                AddFile(file, doc);
            }
        }

        private static void AddDirectory(DirectoryInfo dir, XDoc doc) {
            doc.Start("directory")
                .Elem("name", dir.Name)
                .Elem("num-files", dir.GetFiles().Length)
                .Elem("num-directories", dir.GetDirectories().Length)
                .Elem("created", dir.CreationTimeUtc)
                .Elem("modified", dir.LastWriteTimeUtc)
                .Elem("accessed", dir.LastAccessTimeUtc)
            .End();
        }

        private static void AddFile(FileInfo file, XDoc doc) {
            doc.Start("file")
                .Elem("name", file.Name)
                .Elem("size", file.Length)
                .Elem("extension", file.Extension)
                .Elem("created", file.CreationTimeUtc)
                .Elem("modified", file.LastWriteTimeUtc)
                .Elem("accessed", file.LastAccessTimeUtc)
            .End();
        }

        //--- Fields ---
        private string _path;

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            _path = Environment.ExpandEnvironmentVariables(config["mount"].Contents);
            if(!Path.IsPathRooted(_path)) {
                throw new ArgumentException(string.Format("storage path is not absolute: {0}", _path));
            }
            if(!Directory.Exists(_path)) {
                throw new ArgumentException(string.Format("mount path does not exist: {0}", _path));
            }

            // make sure path ends with a '\' as it makes processing simpler later on
            if((_path.Length != 0) && ((_path[_path.Length - 1] != '/') || (_path[_path.Length - 1] != '\\'))) {
                _path += Path.DirectorySeparatorChar;
            }
            result.Return();
        }

        [DreamFeature("GET://*", "Retrieve a file from the mount folder")]
        [DreamFeature("HEAD://*", "Retrieve information about a file from the mount folder")]
        [DreamFeatureParam("pattern", "string", "pattern to filter file results by")]
        public Yield GetFileHandler(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string suffixPath = string.Join("" + Path.DirectorySeparatorChar, context.GetSuffixes(UriPathFormat.Decoded));
            string filename = Path.Combine(_path, suffixPath);
            if(Directory.Exists(filename)) {
                XDoc ret = new XDoc("files");
                string pattern = context.GetParam("pattern", "");
                AddDirectories(new DirectoryInfo(filename), pattern, ret);
                AddFiles(new DirectoryInfo(filename), pattern, ret);
                response.Return(DreamMessage.Ok(ret));
                yield break;
            }

            DreamMessage message;
            try {
                message = DreamMessage.FromFile(filename, StringUtil.EqualsInvariant(context.Verb, "HEAD"));
            } catch(FileNotFoundException) {
                message = DreamMessage.NotFound("file not found");
            } catch(Exception) {
                message = DreamMessage.BadRequest("invalid path");
            }

            // open file and stream it to the requester
            response.Return(message);
        }
    }
}
