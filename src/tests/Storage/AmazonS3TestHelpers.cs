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
using log4net;
using MindTouch.Dream.AmazonS3;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.Storage.Test {
    public static class AmazonS3TestHelpers {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();
        public static readonly XUri AWS = new XUri("http://s3.amazonaws.com");

        //--- Class Methods ---
        public static DreamMessage CallStorage(this MockServiceInfo mock, Func<Plug, Result<DreamMessage>> mockCall) {
            mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                var storage = (context.Service as MockService).Storage;
                response2.Return(mockCall(storage).Wait());
            };
            return mock.AtLocalMachine.Get(new Result<DreamMessage>()).Wait();
        }

        public static bool ValidateFileHandle(AmazonS3FileHandle handle, string data, TimeSpan? ttl) {
            using(var reader = new StreamReader(handle.Stream)) {
                var read = reader.ReadToEnd();
                if(data != read) {
                    _log.DebugFormat("'{0}' != '{1}'", data, read);
                    return false;
                }
            }
            if(ttl != handle.TimeToLive) {
                _log.DebugFormat("'{0}' != '{1}'", ttl, handle.TimeToLive);
                return false;
            }
            return true;
        }

        public static AmazonS3DataInfo CreateFileInfo(string data) {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(data);
            writer.Flush();
            stream.Position = 0;
            return new AmazonS3DataInfo(new AmazonS3FileHandle() {
                Expiration = null,
                TimeToLive = null,
                MimeType = MimeType.TEXT,
                Modified = DateTime.UtcNow,
                Size = stream.Length,
                Stream = stream
            });
        }

        public static XDoc CreateRandomDocument() {
            return new XDoc("doc").Elem("x", StringUtil.CreateAlphaNumericKey(10));
        }

        public static string[] RootedPath(this AmazonS3ClientConfig config, params string[] path) {
            return ArrayUtil.Concat(new[] { config.Bucket }, config.RootPath.Split('/'), path);
        }

        public static AmazonS3FileHandle CreateFileHandle(XDoc data, TimeSpan? ttl) {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            data.WriteTo(stream);
            stream.Position = 0;
            return new AmazonS3FileHandle {
                Expiration = null,
                TimeToLive = ttl,
                MimeType = MimeType.TEXT_XML,
                Modified = DateTime.UtcNow,
                Size = stream.Length,
                Stream = stream
            };
        }

    }
}