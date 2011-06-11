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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac;
using Autofac.Builder;
using log4net;
using MindTouch.Dream.AmazonS3;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Dream S3 Storage", "Copyright (c) 2010 MindTouch, Inc.",
        Info = "http://developer.mindtouch.com/Dream/Reference/Services/S3Storage",
        SID = new[] { 
            "sid://mindtouch.com/2010/10/dream/s3.storage",
            "sid://mindtouch.com/2010/10/dream/s3.storage.private",
        }
    )]
    [DreamServiceConfig("privatekey", "string", "Private S3 key.")]
    [DreamServiceConfig("publickey", "string", "Public S3 key.")]
    [DreamServiceConfig("bucket", "string", "Storage root bucket.")]
    [DreamServiceConfig("folder", "string?", "Path root inside bucket (default: null).")]
    [DreamServiceConfig("baseuri", "string?", "S3 uri (default: http://s3.amazonaws.com")]
    [DreamServiceConfig("timeout", "int?", "S3 timeout (default: 30 seconds")]
    internal class S3StorageService : DreamService {

        //--- Constants ---
        private const double DEFAULT_S3_TIMEOUT = 30;

        // --- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private bool _private;
        private bool _privateRoot;
        private IAmazonS3Client _s3Client;

        //--- Features ---
        [DreamFeature("GET://*", "Retrieve a file or a list of all files and folders at the specified path")]
        [DreamFeature("HEAD://*", "Retrieve information about a file or folder from the storage folder")]
        public Yield GetFileOrFolderListing(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var head = "HEAD".EqualsInvariant(context.Verb);
            var path = GetPath(context);
            var data = _s3Client.GetDataInfo(path, head);
            DreamMessage result;
            if(data == null) {
                response.Return(DreamMessage.NotFound("no such file or folder"));
                yield break;
            }
            if(data.IsDirectory) {

                // Note (arnec): HEAD for a directory doesn't really mean anything, so we just return ok, to indicate that it exists
                result = head ? DreamMessage.Ok() : DreamMessage.Ok(data.AsDirectoryDocument());
            } else {

                // dealing with a file request 
                var filehandle = data.AsFileHandle();

                // check if request contains a 'if-modified-since' header
                if(request.CheckCacheRevalidation(filehandle.Modified) && (filehandle.Modified.Year >= 1900)) {
                    response.Return(DreamMessage.NotModified());
                    yield break;
                }
                result = head
                             ? new DreamMessage(DreamStatus.Ok, null, filehandle.MimeType, filehandle.Size, Stream.Null)
                             : DreamMessage.Ok(filehandle.MimeType, filehandle.Size, filehandle.Stream);

                // add caching headers if file was found
                if(!head && result.IsSuccessful) {

                    // add caching information; this will avoid unnecessary data transfers by user-agents with caches
                    result.SetCacheMustRevalidate(filehandle.Modified);
                }
            }
            response.Return(result);
            yield break;

        }

        [DreamFeature("PUT://*", "Add a file at a specified path")]
        [DreamFeatureParam("ttl", "int", "time-to-live in seconds for the posted event")]
        public Yield PutFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var filepath = GetPath(context);
            var ttl = context.GetParam("ttl", 0.0);
            TimeSpan? timeToLive = null;
            if(ttl > 0.0) {
                timeToLive = TimeSpan.FromSeconds(ttl);
            }
            try {
                _s3Client.PutFile(filepath, new AmazonS3FileHandle {
                    Stream = request.ToStream(),
                    Size = request.ContentLength,
                    MimeType = request.ContentType,
                    TimeToLive = timeToLive
                });
                response.Return(DreamMessage.Ok());
            } catch(Exception e) {
                throw new DreamBadRequestException(e.Message);
            }
            yield break;
        }

        [DreamFeature("DELETE://*", "Delete file from the storage folder")]
        public Yield DeleteFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var path = GetPath(context);
            _s3Client.Delete(path);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, ILifetimeScope container, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // are we a private storage service?
            _private = config["sid"].Contents == "sid://mindtouch.com/2010/10/dream/s3.storage.private";
            _log.DebugFormat("storage is {0}", _private ? "private" : "public");

            // is the root blocked from access?
            _privateRoot = config["private-root"].AsBool.GetValueOrDefault();
            _log.DebugFormat("storage root is {0}accessible", _privateRoot ? "not " : "");

            // set up S3 client
            var s3Config = new AmazonS3ClientConfig() {
                S3BaseUri = new XUri(config["baseuri"].AsText.IfNullOrEmpty("http://s3.amazonaws.com")),
                Bucket = config["bucket"].AsText,
                Delimiter = "/",
                RootPath = config["folder"].AsText,
                PrivateKey = config["privatekey"].AsText,
                PublicKey = config["publickey"].AsText,
                Timeout = TimeSpan.FromSeconds(config["timeout"].AsDouble ?? DEFAULT_S3_TIMEOUT)
            };
            if(string.IsNullOrEmpty(s3Config.Bucket)) {
                throw new ArgumentException("missing configuration parameter 'bucket'");
            }
            if(string.IsNullOrEmpty(s3Config.PrivateKey)) {
                throw new ArgumentException("missing configuration parameter 'privatekey'");
            }
            if(string.IsNullOrEmpty(s3Config.PublicKey)) {
                throw new ArgumentException("missing configuration parameter 'publickey'");
            }
            _s3Client = container.Resolve<IAmazonS3Client>(TypedParameter.From(s3Config));
            result.Return();
        }

        protected override Yield Stop(Result result) {
            _s3Client.Dispose();
            _s3Client = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        protected override void InitializeLifetimeScope(IRegistrationInspector rootContainer, ContainerBuilder lifetimeScopeBuilder, XDoc config) {
            if(!rootContainer.IsRegistered<IAmazonS3Client>()) {

                // Note (arnec): registering the client in the container to hand over disposal control to the container
                lifetimeScopeBuilder.RegisterType<AmazonS3Client>().As<IAmazonS3Client>();
            }
        }

        public override DreamFeatureStage[] Prologues {
            get {
                return new[] { 
                    new DreamFeatureStage("check-private-storage-access", ProloguePrivateStorage, DreamAccess.Public), 
                };
            }
        }

        private Yield ProloguePrivateStorage(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            //check if this services is private
            if(_private) {
                var cookie = DreamCookie.GetCookie(request.Cookies, "service-key");
                if(cookie == null || cookie.Value != PrivateAccessKey) {
                    throw new DreamForbiddenException("insufficient access privileges");
                }
            }
            response.Return(request);
            yield break;
        }

        private string GetPath(DreamContext context) {
            var parts = context.GetSuffixes(UriPathFormat.Decoded);
            if(parts.Where(p => p.EqualsInvariant("..")).Any()) {
                throw new DreamBadRequestException("paths cannot contain '..'");
            }
            if(_privateRoot && (parts.Length == 0 || (parts.Length == 1 && !context.Uri.TrailingSlash))) {
                throw new DreamForbiddenException("Root level access is forbidden for this storage service");
            }
            return context.Uri.GetRelativePathTo(context.Service.Self.Uri).IfNullOrEmpty("/");
        }
    }
}
