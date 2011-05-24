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
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Principal;

using Autofac;

using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream {
    using Yield = IEnumerator<IYield>;

    /// <summary>
    /// Dream Feature Access level.
    /// </summary>
    public enum DreamAccess {

        /// <summary>
        /// Feature can be called by anyone
        /// </summary>
        Public,

        /// <summary>
        /// Feature access requries the internal or private service key.
        /// </summary>
        Internal,

        /// <summary>
        /// Feature access requires the private service key.
        /// </summary>
        Private
    }

    /// <summary>
    /// Provides the interface for the Dream host environment.
    /// </summary>
    public interface IDreamEnvironment {

        //--- Properties ---

        /// <summary>
        /// Host Globally Unique Identifier.
        /// </summary>
        Guid GlobalId { get; }

        /// <summary>
        /// <see langword="True"/> if the host is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// <see langword="True"/> if the current environment is in debug mode
        /// </summary>
        bool IsDebugEnv { get; }

        /// <summary>
        /// The host's local uri.
        /// </summary>
        XUri LocalMachineUri { get; }

        /// <summary>
        /// <see cref="Plug"/> for host.
        /// </summary>
        Plug Self { get; }

        /// <summary>
        /// Current Activity messages.
        /// </summary>
        Tuplet<DateTime, string>[] ActivityMessages { get; }

        //--- Methods ---

        /// <summary>
        /// Initialize the host.
        /// </summary>
        /// <param name="config">Configuration document.</param>
        void Initialize(XDoc config);

        /// <summary>
        /// Shut down the host.
        /// </summary>
        void Deinitialize();

        /// <summary>
        /// Asynchronously submit a request to the host.
        /// </summary>
        /// <param name="verb">Request Http verb.</param>
        /// <param name="uri">Request Uri.</param>
        /// <param name="user">Request user, if applicable.</param>
        /// <param name="request">Request message.</param>
        /// <param name="response">The response message synchronization instance to be returned by this method.</param>
        /// <returns>Synchronization handle for request.</returns>
        Result<DreamMessage> SubmitRequestAsync(string verb, XUri uri, IPrincipal user, DreamMessage request, Result<DreamMessage> response);

        /// <summary>
        /// Block execution until host has shut down.
        /// </summary>
        void WaitUntilShutdown();

        /// <summary>
        /// Add an activity.
        /// </summary>
        /// <param name="key">Activity key.</param>
        /// <param name="description">Activity description.</param>
        void AddActivityDescription(object key, string description);

        /// <summary>
        /// Remove an activity.
        /// </summary>
        /// <param name="key">Activity key.</param>
        void RemoveActivityDescription(object key);

        /// <summary>
        /// Update the information message for a source.
        /// </summary>
        /// <param name="source">Source to update.</param>
        /// <param name="message">Info message.</param>
        void UpdateInfoMessage(string source, string message);

        /// <summary>
        /// Check response cache for a service.
        /// </summary>
        /// <param name="service">Service whose cache to check.</param>
        /// <param name="key">Cache key.</param>
        void CheckResponseCache(IDreamService service, object key);

        /// <summary>
        /// Remove an item from a service's cache.
        /// </summary>
        /// <param name="service">Service whose cache to check.</param>
        /// <param name="key">Cache key.</param>
        void RemoveResponseCache(IDreamService service, object key);

        /// <summary>
        /// Empty entire cache for a service.
        /// </summary>
        /// <param name="service">Service to clear the cache for.</param>
        void EmptyResponseCache(IDreamService service);

        /// <summary>
        /// Called by <see cref="IDreamService"/> on startup to have the environment create and initialize a service level container.
        /// </summary>
        /// <remarks>
        /// Returned instance should only be used to configure the container. For any type resolution, <see cref="DreamContext.Container"/> should be used
        /// instead.
        /// </remarks>
        /// <param name="service"></param>
        /// <returns></returns>
        IContainer CreateServiceContainer(IDreamService service);

        /// <summary>
        /// Must be called at <see cref="IDreamService"/> shutdown to dispose of the service level container.
        /// </summary>
        /// <param name="service"></param>
        void DisposeServiceContainer(IDreamService service);
    }

    /// <summary>
    /// Provides interface that all services hosted in Dream must implement.
    /// </summary>
    public interface IDreamService {

        //--- Properties ---

        /// <summary>
        /// <see cref="Plug"/> for Service's Host environment.
        /// </summary>
        Plug Env { get; }

        /// <summary>
        /// Service <see cref="Plug"/>
        /// </summary>
        Plug Self { get; }

        /// <summary>
        /// Service cookie jar.
        /// </summary>
        DreamCookieJar Cookies { get; }

        /// <summary>
        /// Prologue request stages to be executed before a Feature is executed.
        /// </summary>
        DreamFeatureStage[] Prologues { get; }

        /// <summary>
        /// Epilogue request stages to be executed after a Feature has completed.
        /// </summary>
        DreamFeatureStage[] Epilogues { get; }

        /// <summary>
        /// Exception translators given an opportunity to rewrite an exception before it is returned to the initiator of a request.
        /// </summary>
        ExceptionTranslator[] ExceptionTranslators { get; }
        
        //--- Methods ---

        /// <summary>
        /// Initialize a service instance.
        /// </summary>
        /// <param name="environment">Host environment.</param>
        /// <param name="blueprint">Service blueprint.</param>
        void Initialize(IDreamEnvironment environment, XDoc blueprint);

        /// <summary>
        /// Determine the access appropriate for an incoming request.
        /// </summary>
        /// <param name="context">Request context.</param>
        /// <param name="request">Request message.</param>
        /// <returns>Access level for request.</returns>
        DreamAccess DetermineAccess(DreamContext context, DreamMessage request);
    }

    /// <summary>
    /// Interface for <see cref="IDreamService"/> implementations that require a service license.
    /// </summary>
    public interface IDreamServiceLicense {

        //--- Properties ---

        /// <summary>
        /// License for service.
        /// </summary>
        string ServiceLicense { get; }
    }

    /// <summary>
    /// Common Http verbs.
    /// </summary>
    public static class Verb {

        //--- Constants ---

        /// <summary>
        /// POST verb.
        /// </summary>
        public const string POST = "POST";

        /// <summary>
        /// PUT verb.
        /// </summary>
        public const string PUT = "PUT";

        /// <summary>
        /// GET verb.
        /// </summary>
        public const string GET = "GET";

        /// <summary>
        /// DELETE verb.
        /// </summary>
        public const string DELETE = "DELETE";

        /// <summary>
        /// HEAD verb.
        /// </summary>
        public const string HEAD = "HEAD";

        /// <summary>
        /// OPTIONS verb.
        /// </summary>
        public const string OPTIONS = "OPTIONS";
    }

    /// <summary>
    /// Common URI schemes
    /// </summary>
    public static class Scheme {

        //--- Constants ---

        /// <summary>
        /// Hyper-text transport protocol.
        /// </summary>
        public const string HTTP = "http";

        /// <summary>
        /// Secure Hyper-text transport protocol.
        /// </summary>
        public const string HTTPS = "https";

        /// <summary>
        /// eXtensible Resource Identifier.
        /// </summary>
        public const string XRI = "xri";

        /// <summary>
        /// Dream Host internal transport.
        /// </summary>
        public const string LOCAL = "local";
    }

    /// <summary>
    /// Dream specific input query parameters.
    /// </summary>
    public static class DreamInParam {

        //--- Constants ---

        /// <summary>
        /// Request message format
        /// </summary>
        public const string FORMAT = "dream.in.format";

        /// <summary>
        /// Request originating host.
        /// </summary>
        public const string HOST = "dream.in.host";

        /// <summary>
        /// Request originating path root.
        /// </summary>
        public const string ROOT = "dream.in.root";

        /// <summary>
        /// Request originating verb.
        /// </summary>
        public const string VERB = "dream.in.verb";

        /// <summary>
        /// Request origin.
        /// </summary>
        public const string ORIGIN = "dream.in.origin";

        /// <summary>
        /// Request originating scheme.
        /// </summary>
        public const string SCHEME = "dream.in.scheme";

        /// <summary>
        /// Original request uri.
        /// </summary>
        public const string URI = "dream.in.uri";
    }

    /// <summary>
    /// Dream specific output query parameters.
    /// </summary>
    public static class DreamOutParam {

        //--- Constants ---

        /// <summary>
        /// Expected response message format.
        /// </summary>
        public const string FORMAT = "dream.out.format";

        /// <summary>
        /// Response prefix.
        /// </summary>
        public const string PREFIX = "dream.out.pre";

        /// <summary>
        /// Response postix.
        /// </summary>
        public const string POSTFIX = "dream.out.post";

        /// <summary>
        /// Response callback.
        /// </summary>
        public const string CALLBACK = "dream.out.callback";

        /// <summary>
        /// Response selector.
        /// </summary>
        public const string SELECT = "dream.out.select";

        /// <summary>
        /// Response chunk.
        /// </summary>
        public const string CHUNK = "dream.out.chunk";

        /// <summary>
        /// Response type.
        /// </summary>
        public const string TYPE = "dream.out.type";

        /// <summary>
        /// Response 'Save As' directive.
        /// </summary>
        public const string SAVEAS = "dream.out.saveas";
    }

    /// <summary>
    /// Delegate type for implemting an exception translator.
    /// </summary>
    /// <param name="context">Request context.</param>
    /// <param name="e">Original exception.</param>
    /// <returns>DreamMessage formatted exception, or null, if the translator declines to handle the exception.</returns>
    public delegate DreamMessage ExceptionTranslator(DreamContext context, Exception e);

    /// <summary>
    /// Encapsulation of processing chain for a feature in an <see cref="IDreamService"/>
    /// </summary>
    public class DreamFeature {

        //--- Class Methods ---
        private static void ParseFeatureSignature(XUri baseUri, string signature, out string[] pathSegments, out KeyValuePair<int, string>[] paramNames, out int optional) {
            List<string> segments = new List<string>(baseUri.GetSegments(UriPathFormat.Normalized));
            List<KeyValuePair<int, string>> names = new List<KeyValuePair<int, string>>();
            optional = 0;

            // normalize and remove any leading and trailing '/'
            signature = signature.ToLowerInvariant().Trim();
            if((signature.Length > 0) && (signature[0] == '/')) {
                signature = signature.Substring(1);
            }
            if((signature.Length > 0) && (signature[signature.Length - 1] == '/')) {
                signature = signature.Substring(0, signature.Length - 1);
            }
            if(signature.Length > 0) {
                string[] parts = signature.Split('/');

                // loop over all parts
                for(int i = 0; i < parts.Length; ++i) {

                    // check if part is empty (this only happens for "//")
                    if(parts[i].Length == 0) {

                        // we found two slashes in a row; the next token MUST be the final token
                        if((i != (parts.Length - 2)) || (parts[i + 1] != "*")) {
                            throw new ArgumentException("invalid feature signature", signature);
                        }
                        optional = int.MaxValue;
                        break;
                    } else {
                        string part = parts[i].Trim();
                        if((part.Length >= 2) && (part[0] == '{') && (part[part.Length - 1] == '}')) {

                            // we have a path variable (e.g. /{foo}/)
                            if(optional != 0) {
                                throw new ArgumentException("invalid feature signature", signature);
                            }
                            segments.Add(SysUtil.NameTable.Add("*"));
                            names.Add(new KeyValuePair<int, string>(baseUri.Segments.Length + i, SysUtil.NameTable.Add(part.Substring(1, part.Length - 2))));
                        } else if(part == "*") {

                            // we have a path wildcard (e.g. /*/)
                            if(optional != 0) {
                                throw new ArgumentException("invalid feature signature", signature);
                            }
                            segments.Add(SysUtil.NameTable.Add(part));
                            names.Add(new KeyValuePair<int, string>(baseUri.Segments.Length + i, SysUtil.NameTable.Add(i.ToString())));
                        } else if(part == "?") {

                            // we have an optional path (e.g. /?/)
                            ++optional;
                            segments.Add(SysUtil.NameTable.Add(part));
                        } else {

                            // we have a path constant (e.g. /foo/)
                            if(optional != 0) {
                                throw new ArgumentException("invalid feature signature", signature);
                            }
                            segments.Add(SysUtil.NameTable.Add(part));
                        }
                    }
                }
            }
            pathSegments = segments.ToArray();
            paramNames = names.ToArray();
        }

        //--- Fields ---

        /// <summary>
        /// Owning Service.
        /// </summary>
        public readonly IDreamService Service;

        /// <summary>
        /// Uri for Service.
        /// </summary>
        public readonly XUri ServiceUri;

        /// <summary>
        /// Request Verb.
        /// </summary>
        public readonly string Verb;

        /// <summary>
        /// Request stages.
        /// </summary>
        public readonly DreamFeatureStage[] Stages;

        /// <summary>
        /// Request path segments.
        /// </summary>
        public readonly string[] PathSegments;

        /// <summary>
        /// Number of optional segments
        /// </summary>
        public readonly int OptionalSegments;

        /// <summary>
        /// Index into <see cref="Stages"/> for the <see cref="DreamFeatureAttribute"/> marked stage for this request.
        /// </summary>
        public readonly int MainStageIndex;

        /// <summary>
        /// Exception translators for this request.
        /// </summary>
        public readonly ExceptionTranslator[] ExceptionTranslators;

        private KeyValuePair<int, string>[] _paramNames;
        private int _counter;

        //--- Constructors ---

        /// <summary>
        /// Create a new feature instance.
        /// </summary>
        /// <param name="service">Owning Service.</param>
        /// <param name="serviceUri">Service Uri.</param>
        /// <param name="mainStageIndex">Main stage index.</param>
        /// <param name="stages">Feature stages.</param>
        /// <param name="verb">Request verb.</param>
        /// <param name="signature">Feature signature.</param>
        public DreamFeature(IDreamService service, XUri serviceUri, int mainStageIndex, DreamFeatureStage[] stages, string verb, string signature) {
            this.Service = service;
            this.ServiceUri = serviceUri;
            this.Stages = stages;
            this.MainStageIndex = mainStageIndex;
            this.Verb = verb;
            this.ExceptionTranslators = service.ExceptionTranslators;
            ParseFeatureSignature(serviceUri, signature, out this.PathSegments, out _paramNames, out this.OptionalSegments);
        }

        //--- Properties ---

        /// <summary>
        /// Feature signature.
        /// </summary>
        public string Signature { get { return string.Join("/", PathSegments, ServiceUri.Segments.Length, PathSegments.Length - ServiceUri.Segments.Length); } }

        /// <summary>
        /// Feature path.
        /// </summary>
        public string Path { get { return string.Join("/", PathSegments); } }

        /// <summary>
        /// <see cref="Verb"/> + ":" + <see cref="Signature"/>.
        /// </summary>
        public string VerbSignature { get { return Verb + ":" + Signature; } }

        /// <summary>
        /// <see cref="Verb"/> + ":" + <see cref="Path"/>. 
        /// </summary>
        public string VerbPath { get { return Verb + ":" + Path; } }

        /// <summary>
        /// Number of times this Feature has been called in current instance.
        /// </summary>
        public int HitCounter { get { return _counter; } }

        /// <summary>
        /// Main feature Stage.
        /// </summary>
        public DreamFeatureStage MainStage { get { return Stages[MainStageIndex]; } }

        //--- Methods ---

        /// <summary>
        /// Extract a list of suffixes and a dictionary of arguments from the request.
        /// </summary>
        /// <param name="uri">Request Uri.</param>
        /// <param name="suffixes">Extracted suffixes.</param>
        /// <param name="pathParams">Extracted path parameters.</param>
        public void ExtractArguments(XUri uri, out string[] suffixes, out Dictionary<string, string[]> pathParams) {
            Dictionary<string, List<string>> tmpPathParams = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            List<string> suffixesList = new List<string>(_paramNames.Length + (uri.Segments.Length - PathSegments.Length));
            for(int i = 0; i < _paramNames.Length; ++i) {
                string value = uri.Segments[_paramNames[i].Key];
                suffixesList.Add(value);
                List<string> values;
                if(!tmpPathParams.TryGetValue(_paramNames[i].Value, out values)) {
                    values = new List<string>(1);
                    tmpPathParams.Add(_paramNames[i].Value, values);
                }
                values.Add(XUri.Decode(value));
            }
            pathParams = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach(KeyValuePair<string, List<string>> tmpPathParam in tmpPathParams) {
                pathParams.Add(tmpPathParam.Key, tmpPathParam.Value.ToArray());
            }
            for(int i = PathSegments.Length; i < uri.Segments.Length; ++i) {
                suffixesList.Add(uri.Segments[i]);
            }
            suffixes = suffixesList.ToArray();
        }

        /// <summary>
        /// Increment the feature hit counter.
        /// </summary>
        public void IncreaseHitCounter() {
            System.Threading.Interlocked.Increment(ref _counter);
        }
    }

    /// <summary>
    /// Encapsulation for Http Content-Disposition header.
    /// </summary>
    public class ContentDisposition {

        //--- Class Fields
        private static readonly Regex MIME_ENCODE_REGEX = new Regex("(Firefox|Chrome)");
        private static readonly Regex URL_ENCODE_REGEX = new Regex("(MSIE)");

        //--- Fields ---

        /// <summary>
        /// Inline content.
        /// </summary>
        public bool Inline;

        /// <summary>
        /// Content creation date.
        /// </summary>
        public DateTime? CreationDate;

        /// <summary>
        /// Content modification date.
        /// </summary>
        public DateTime? ModificationDate;

        /// <summary>
        /// Date content was read.
        /// </summary>
        public DateTime? ReadDate;

        /// <summary>
        /// Local filename for content.
        /// </summary>
        public string FileName;

        /// <summary>
        /// Content file size (if available).
        /// </summary>
        public long? Size;

        /// <summary>
        /// Content target user agent.
        /// </summary>
        public string UserAgent;

        //--- Construtors ---

        /// <summary>
        /// Create a new content disposition.
        /// </summary>
        public ContentDisposition() { }

        /// <summary>
        /// Create a new content dispisition from a Content-Disposition header string.
        /// </summary>
        /// <param name="value"></param>
        public ContentDisposition(string value) {
            Dictionary<string, string> values = HttpUtil.ParseNameValuePairs(value);
            if(values.ContainsKey("#1")) {
                string type = values["#1"];
                this.Inline = StringUtil.EqualsInvariant(type, "inline");
            }
            if(values.ContainsKey("creation-date")) {
                DateTime date;
                if(DateTimeUtil.TryParseInvariant(values["creation-date"], out date)) {
                    this.CreationDate = date.ToUniversalTime();
                }
            }
            if(values.ContainsKey("modification-date")) {
                DateTime date;
                if(DateTimeUtil.TryParseInvariant(values["modification-date"], out date)) {
                    this.ModificationDate = date.ToUniversalTime();
                }
            }
            if(values.ContainsKey("read-date")) {
                DateTime date;
                if(DateTimeUtil.TryParseInvariant(values["read-date"], out date)) {
                    this.ReadDate = date.ToUniversalTime();
                }
            }
            if(values.ContainsKey("filename")) {
                this.FileName = values["filename"];
            }
            if(values.ContainsKey("size")) {
                long size;
                if(long.TryParse(values["size"], out size)) {
                    this.Size = size;
                }
            }
        }

        /// <summary>
        /// Create a new content disposition.
        /// </summary>
        /// <param name="inline">Inline the content.</param>
        /// <param name="created">Creation date.</param>
        /// <param name="modified">Modification date.</param>
        /// <param name="read">Read date.</param>
        /// <param name="filename">Content filename.</param>
        /// <param name="size">Content size.</param>
        public ContentDisposition(bool inline, DateTime? created, DateTime? modified, DateTime? read, string filename, long? size) {
            this.Inline = inline;
            this.CreationDate = created;
            this.ModificationDate = modified;
            this.ReadDate = read;
            this.FileName = filename;
            this.Size = size;
        }

        /// <summary>
        /// Create a new content disposition.
        /// </summary>
        /// <param name="inline">Inline the content.</param>
        /// <param name="created">Creation date.</param>
        /// <param name="modified">Modification date.</param>
        /// <param name="read">Read date.</param>
        /// <param name="filename">Content filename.</param>
        /// <param name="size">File size.</param>
        /// <param name="userAgent">Target user agent.</param>
        public ContentDisposition(bool inline, DateTime? created, DateTime? modified, DateTime? read, string filename, long? size, string userAgent) {
            this.Inline = inline;
            this.CreationDate = created;
            this.ModificationDate = modified;
            this.ReadDate = read;
            this.FileName = filename;
            this.Size = size;
            this.UserAgent = userAgent;
        }

        //--- Methods ---

        /// <summary>
        /// Convert to Content-Disposition header string.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            StringBuilder result = new StringBuilder();
            if(Inline) {
                result.Append("inline");
            } else {
                result.Append("attachment");
            }
            if(CreationDate != null) {
                result.Append("; creation-date=\"").Append(CreationDate.Value.ToUniversalTime().ToString("r")).Append("\"");
            }
            if(ModificationDate != null) {
                result.Append("; modification-date=\"").Append(ModificationDate.Value.ToUniversalTime().ToString("r")).Append("\"");
            }
            if(!string.IsNullOrEmpty(FileName)) {
                bool gotFilename = false;
                if(!string.IsNullOrEmpty(UserAgent)) {
                    if(URL_ENCODE_REGEX.IsMatch(UserAgent)) {
                        
                        // Filename is uri encoded to support non ascii characters.
                        // + is replaced with %20 for IE otherwise it saves names containing spaces with plusses.
                        result.Append("; filename=\"").Append(XUri.Encode(FileName).Replace("+", "%20")).Append("\"");
                        gotFilename = true;
                    } else if(MIME_ENCODE_REGEX.IsMatch(UserAgent)) {
                        result.Append("; filename=\"=?UTF-8?B?").Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(FileName))).Append("?=\"");
                        gotFilename = true;
                    }
                }
                if(!gotFilename) {
                    result.Append("; filename=\"").Append(Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(FileName))).Append("\"");
                }
            }
            if(Size != null) {
                result.Append("; size=").Append(Size.Value);
            }
            return result.ToString();
        }
    }
}
