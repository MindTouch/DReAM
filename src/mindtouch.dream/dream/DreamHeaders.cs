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
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;
using MindTouch.Web;

namespace MindTouch.Dream {

    // TODO (steveb:
    // * add 'CacheControl' type
    // * add 'Authentication' type (used by 'Authorization' and 'Proxy-Authorization')

    /// <summary>
    /// Provides a http header collection for <see cref="DreamMessage"/>.
    /// </summary>
    public class DreamHeaders : IEnumerable<KeyValuePair<string, string>> {

        //--- Constants ---

        //___ Generic Headers ___

        /// <summary>
        /// 'Accept' Header.
        /// </summary>
        public const string ACCEPT = "Accept";

        /// <summary>
        /// 'Accept-Charset' Header.
        /// </summary>
        public const string ACCEPT_CHARSET = "Accept-Charset";

        /// <summary>
        /// 'Accept-Encoding' Header.
        /// </summary>
        public const string ACCEPT_ENCODING = "Accept-Encoding";

        /// <summary>
        /// 'Accept-Language' Header.
        /// </summary>
        public const string ACCEPT_LANGUAGE = "Accept-Language";

        /// <summary>
        /// 'Accept-Ranges' Header.
        /// </summary>
        public const string ACCEPT_RANGES = "Accept-Ranges";

        /// <summary>
        /// 'Allow' Header.
        /// </summary>
        public const string ALLOW = "Allow";

        /// <summary>
        /// 'WWW-Authenticate' Header.
        /// </summary>
        public const string AUTHENTICATE = "WWW-Authenticate";

        /// <summary>
        /// 'Authorization' Header.
        /// </summary>
        public const string AUTHORIZATION = "Authorization";

        /// <summary>
        /// 'Cache-Control' Header.
        /// </summary>
        public const string CACHE_CONTROL = "Cache-Control";

        /// <summary>
        /// 'Connection' Header.
        /// </summary>
        public const string CONNECTION = "Connection";

        /// <summary>
        /// '"Content-Disposition' Header.
        /// </summary>
        public const string CONTENT_DISPOSITION = "Content-Disposition";

        /// <summary>
        /// 'Content-Encoding' Header.
        /// </summary>
        public const string CONTENT_ENCODING = "Content-Encoding";

        /// <summary>
        /// 'Content-Language' Header.
        /// </summary>
        public const string CONTENT_LANGUAGE = "Content-Language";

        /// <summary>
        /// 'Content-Length' Header.
        /// </summary>
        public const string CONTENT_LENGTH = "Content-Length";

        /// <summary>
        /// 'Content-Location' Header.
        /// </summary>
        public const string CONTENT_LOCATION = "Content-Location";

        /// <summary>
        /// 'Content-MD5' Header.
        /// </summary>
        public const string CONTENT_MD5 = "Content-MD5";

        /// <summary>
        /// 'Content-Range' Header.
        /// </summary>
        public const string CONTENT_RANGE = "Content-Range";

        /// <summary>
        /// 'Content-Type' Header.
        /// </summary>
        public const string CONTENT_TYPE = "Content-Type";

        /// <summary>
        /// 'Cookie' Header.
        /// </summary>
        public const string COOKIE = "Cookie";

        /// <summary>
        /// 'Date' Header.
        /// </summary>
        public const string DATE = "Date";

        /// <summary>
        /// 'ETag' Header.
        /// </summary>
        public const string ETAG = "ETag";

        /// <summary>
        /// 'Expect' Header.
        /// </summary>
        public const string EXPECT = "Expect";

        /// <summary>
        /// 'From' Header.
        /// </summary>
        public const string FROM = "From";

        /// <summary>
        /// 'Host' Header.
        /// </summary>
        public const string HOST = "Host";

        /// <summary>
        /// 'If-Match' Header.
        /// </summary>
        public const string IF_MATCH = "If-Match";

        /// <summary>
        /// 'If-Modified-Since' Header.
        /// </summary>
        public const string IF_MODIFIED_SINCE = "If-Modified-Since";

        /// <summary>
        /// 'If-None-Match' Header.
        /// </summary>
        public const string IF_NONE_MATCH = "If-None-Match";

        /// <summary>
        /// 'If-Range' Header.
        /// </summary>
        public const string IF_RANGE = "If-Range";

        /// <summary>
        /// 'If-Unmodified-Since' Header.
        /// </summary>
        public const string IF_UNMODIFIED_SINCE = "If-Unmodified-Since";

        /// <summary>
        /// 'Last-Modified' Header.
        /// </summary>
        public const string LAST_MODIFIED = "Last-Modified";

        /// <summary>
        /// 'Location' Header.
        /// </summary>
        public const string LOCATION = "Location";

        /// <summary>
        /// 'Max-Forwards' Header.
        /// </summary>
        public const string MAX_FORWARDS = "Max-Forwards";

        /// <summary>
        /// 'Pragma' Header.
        /// </summary>
        public const string PRAGMA = "Pragma";

        /// <summary>
        /// 'Range' Header.
        /// </summary>
        public const string RANGE = "Range";

        /// <summary>
        /// 'Referer' Header.
        /// </summary>
        public const string REFERER = "Referer";

        /// <summary>
        /// 'Response-Uri' Header.
        /// </summary>
        public const string RESPONSE_URI = "Response-Uri";

        /// <summary>
        /// 'Retry-After' Header.
        /// </summary>
        public const string RETRY_AFTER = "Retry-After";

        /// <summary>
        /// 'Server' Header.
        /// </summary>
        public const string SERVER = "Server";

        /// <summary>
        /// 'Set-Cookie' Header.
        /// </summary>
        public const string SET_COOKIE = "Set-Cookie";

        /// <summary>
        /// 'Transfer-Encoding' Header.
        /// </summary>
        public const string TRANSFER_ENCODING = "Transfer-Encoding";

        /// <summary>
        /// 'User-Agent' Header.
        /// </summary>
        public const string USER_AGENT = "User-Agent";

        /// <summary>
        /// 'Vary' Header.
        /// </summary>
        public const string VARY = "Vary";

        /// <summary>
        /// 'Proxy-Connection' Header.
        /// </summary>
        public const string PROXY_CONNECTION = "Proxy-Connection";

        //___ Dream-specific Headers ___

        /// <summary>
        /// 'X-Dream-Transport' Dream Header.
        /// </summary>
        public const string DREAM_TRANSPORT = "X-Dream-Transport";

        /// <summary>
        /// 'X-Dream-Service' Dream Header.
        /// </summary>
        public const string DREAM_SERVICE = "X-Dream-Service";

        /// <summary>
        /// 'X-Dream-Public-Uri' Dream Header.
        /// </summary>
        public const string DREAM_PUBLIC_URI = "X-Dream-Public-Uri";

        /// <summary>
        /// 'X-Dream-User-Host' Dream Header.
        /// </summary>
        public const string DREAM_USER_HOST = "X-Dream-User-Host";

        /// <summary>
        /// 'X-Dream-Origin' Dream Header.
        /// </summary>
        public const string DREAM_ORIGIN = "X-Dream-Origin";

        /// <summary>
        /// 'X-Dream-ClientIP' Dream Header.
        /// </summary>
        public const string DREAM_CLIENTIP = "X-Dream-ClientIP";

        /// <summary>
        /// 'X-Dream-Event-Id' Dream Header.
        /// </summary>
        public const string DREAM_EVENT_ID = "X-Dream-Event-Id";

        /// <summary>
        /// 'X-Dream-Event-Channel' Dream Header.
        /// </summary>
        public const string DREAM_EVENT_CHANNEL = "X-Dream-Event-Channel";

        /// <summary>
        /// 'X-Dream-Event-Resource-Uri' Dream Header.
        /// </summary>
        public const string DREAM_EVENT_RESOURCE = "X-Dream-Event-Resource-Uri";

        /// <summary>
        /// 'X-Dream-Event-Origin-Uri' Dream Header.
        /// </summary>
        public const string DREAM_EVENT_ORIGIN = "X-Dream-Event-Origin-Uri";

        /// <summary>
        /// 'X-Dream-Event-Recipient-Uri' Dream Header.
        /// </summary>
        public const string DREAM_EVENT_RECIPIENT = "X-Dream-Event-Recipient-Uri";

        /// <summary>
        /// 'X-Dream-Event-Via-Uri' Dream Header.
        /// </summary>
        public const string DREAM_EVENT_VIA = "X-Dream-Event-Via-Uri";

        /// <summary>
        /// 'X-Dream-Request-Id' Dream Header.
        /// </summary>
        public const string DREAM_REQUEST_ID = "X-Dream-Request-Id";

        //___ Application-specific Headers ___

        /// <summary>
        /// 'X-XRDS-Location' Application-specific Header.
        /// </summary>
        public const string XRDS_LOCATION = "X-XRDS-Location";

        /// <summary>
        /// 'X-HTTP-Method-Override' Application-specific Header.
        /// </summary>
        public const string METHOD_OVERRIDE = "X-HTTP-Method-Override";

        /// <summary>
        /// 'Front-End-Https' Application-specific Header.
        /// </summary>
        public const string FRONT_END_HTTPS = "Front-End-Https";

        //___ Apache mod_proxy headers ___

        /// <summary>
        /// 'X-Forwarded-Host' Apache mod_proxy Header (original Host header).
        /// </summary>
        public const string FORWARDED_HOST = "X-Forwarded-Host";

        /// <summary>
        /// 'X-Forwarded-For' Apache mod_proxy Header (original client IP).
        /// </summary>
        public const string FORWARDED_FOR = "X-Forwarded-For";

        /// <summary>
        /// 'X-Forwarded-Server' Apache mod_proxy Header (server which did the forwarding).
        /// </summary>
        public const string FORWARDED_SERVER = "X-Forwarded-Server";

        //--- Types ---
        internal class Entry {

            //--- Fields ---
            internal Entry Next;
            internal string Value;

            //--- Constructors ---
            internal Entry(string value) {
                this.Value = value;
            }

            internal Entry(string value, Entry next) {
                this.Value = value;
                this.Next = next;
            }

            //--- Properties ---
            internal Entry Last {
                get {
                    Entry result = this;
                    while(result.Next != null) {
                        result = result.Next;
                    }
                    return result;
                }
            }
        }

        //--- Fields ---
        private readonly Dictionary<string, Entry> _headers = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        private List<DreamCookie> _cookies;

        //--- Constructors ---

        /// <summary>
        /// Create new instance.
        /// </summary>
        public DreamHeaders() { }

        /// <summary>
        /// Create new instance.
        /// </summary>
        /// <param name="headers">Headers to copy into new instance.</param>
        public DreamHeaders(DreamHeaders headers) {
            AddRange(headers);
        }

        /// <summary>
        /// Create new instance.
        /// </summary>
        /// <param name="headers">Header name/value pairs to copy into new instance.</param>
        public DreamHeaders(NameValueCollection headers) {
            AddRange(headers);
        }

        /// <summary>
        /// Create new instance.
        /// </summary>
        /// <param name="headers">Header name/value pairs to copy into new instance.</param>
        public DreamHeaders(IEnumerable<KeyValuePair<string, string>> headers) {
            AddRange(headers);
        }

        //--- Properties ---

        /// <summary>
        /// Total number of headers.
        /// </summary>
        public int Count { get { return _headers.Count; } }

        /// <summary>
        /// <see langword="True"/> if the collection has a cookie header that is not empty.
        /// </summary>
        public bool HasCookies { get { return (_cookies != null) && (_cookies.Count > 0); } }

        /// <summary>
        /// 'Accept' Header.
        /// </summary>
        public string Accept {
            get { return this[ACCEPT]; }
            set { this[ACCEPT] = value; }
        }

        /// <summary>
        /// 'Accept-Charset' Header.
        /// </summary>
        public string AcceptCharset {
            get { return this[ACCEPT_CHARSET]; }
            set { this[ACCEPT_CHARSET] = value; }
        }

        /// <summary>
        /// 'Accept-Encoding' Header.
        /// </summary>
        public string AcceptEncoding {
            get { return this[ACCEPT_ENCODING]; }
            set { this[ACCEPT_ENCODING] = value; }
        }

        /// <summary>
        /// 'Accept-Language' Header.
        /// </summary>
        public string AcceptLanguage {
            get { return this[ACCEPT_LANGUAGE]; }
            set { this[ACCEPT_LANGUAGE] = value; }
        }

        /// <summary>
        /// 'Accept-Ranges' Header.
        /// </summary>
        public string AcceptRanges {
            get { return this[ACCEPT_RANGES]; }
            set { this[ACCEPT_RANGES] = value; }
        }

        /// <summary>
        /// 'Allow' Header.
        /// </summary>
        public string Allow {
            get { return this[ALLOW]; }
            set { this[ALLOW] = value; }
        }

        /// <summary>
        /// 'WWW-Authenticate' Header.
        /// </summary>
        public string Authenticate {
            get { return this[AUTHENTICATE]; }
            set { this[AUTHENTICATE] = value; }
        }

        /// <summary>
        /// 'Authorization' Header.
        /// </summary>
        public string Authorization {
            get { return this[AUTHORIZATION]; }
            set { this[AUTHORIZATION] = value; }
        }

        /// <summary>
        /// 'Cache-Control' Header.
        /// </summary>
        public string CacheControl {
            get { return this[CACHE_CONTROL]; }
            set { this[CACHE_CONTROL] = value; }
        }

        /// <summary>
        /// 'Connection' Header.
        /// </summary>
        public string Connection {
            get { return this[CONNECTION]; }
            set { this[CONNECTION] = value; }
        }

        /// <summary>
        /// 'Content-Disposition' Header.
        /// </summary>
        public ContentDisposition ContentDisposition {
            get {
                string value = this[CONTENT_DISPOSITION];
                if(value != null) {
                    return new ContentDisposition(value);
                }
                return null;
            }
            set {
                this[CONTENT_DISPOSITION] = (value != null) ? value.ToString() : null;
            }
        }

        /// <summary>
        /// 'Content-Encoding' Header.
        /// </summary>
        public string ContentEncoding {
            get { return this[CONTENT_ENCODING]; }
            set { this[CONTENT_ENCODING] = value; }
        }

        /// <summary>
        /// 'Content-Language' Header.
        /// </summary>
        public string ContentLanguage {
            get { return this[CONTENT_LANGUAGE]; }
            set { this[CONTENT_LANGUAGE] = value; }
        }

        /// <summary>
        /// 'Content-Length' Header.
        /// </summary>
        public long? ContentLength {
            get {
                string value = this[CONTENT_LENGTH];
                return (value != null) ? (long?)long.Parse(value) : null;
            }
            set {
                this[CONTENT_LENGTH] = (value != null) ? value.ToString() : null;
            }
        }

        /// <summary>
        /// 'Content-Location' Header.
        /// </summary>
        public XUri ContentLocation {
            get {
                string value = this[CONTENT_LOCATION];
                return (value != null) ? new XUri(value) : null;
            }
            set {
                this[CONTENT_LOCATION] = (value != null) ? value.ToString() : null;
            }
        }

        /// <summary>
        /// 'Content-MD5' Header.
        /// </summary>
        public Guid? ContentMd5 {
            get {
                string value = this[CONTENT_MD5];
                if(value != null) {
                    return new Guid(Convert.FromBase64String(value));
                }
                return null;
            }
            set {
                this[CONTENT_MD5] = (value != null) ? Convert.ToBase64String(value.Value.ToByteArray()) : null;
            }
        }

        /// <summary>
        /// 'Content-Range' Header.
        /// </summary>
        public string ContentRange {
            get { return this[CONTENT_RANGE]; }
            set { this[CONTENT_RANGE] = value; }
        }

        /// <summary>
        /// 'Content-Type' Header.
        /// </summary>
        public MimeType ContentType {
            get {
                string value = this[CONTENT_TYPE];
                if(value != null) {
                    return new MimeType(value);
                }
                return null;
            }
            set {
                this[CONTENT_TYPE] = (value != null) ? value.ToString() : null;
            }
        }

        /// <summary>
        /// 'Cookie' Header.
        /// </summary>
        public List<DreamCookie> Cookies {
            get {
                if(_cookies == null) {
                    _cookies = new List<DreamCookie>();
                }
                return _cookies;
            }
            set {
                _cookies = value;
            }
        }

        /// <summary>
        /// 'Date' Header.
        /// </summary>
        public DateTime? Date {
            get {
                string value = this[DATE];
                if(value != null) {
                    DateTime result;
                    if(DateTimeUtil.TryParseInvariant(value, out result)) {
                        return result.ToUniversalTime();
                    }
                }
                return null;
            }
            set {
                this[DATE] = (value != null) ? value.Value.ToString("r") : null;
            }
        }

        /// <summary>
        /// 'ETag' Header.
        /// </summary>
        public string ETag {
            get { return this[ETAG]; }
            set { this[ETAG] = value; }
        }

        /// <summary>
        /// 'Expect' Header.
        /// </summary>
        public string Expect {
            get { return this[EXPECT]; }
            set { this[EXPECT] = value; }
        }

        /// <summary>
        /// 'From' Header.
        /// </summary>
        public string From {
            get { return this[FROM]; }
            set { this[FROM] = value; }
        }

        /// <summary>
        /// 'Host' Header.
        /// </summary>
        public string Host {
            get { return this[HOST]; }
            set { this[HOST] = value; }
        }

        /// <summary>
        /// 'If-Match' Header.
        /// </summary>
        public string IfMatch {
            get { return this[IF_MATCH]; }
            set { this[IF_MATCH] = value; }
        }

        /// <summary>
        /// 'If-Modified-Since' Header.
        /// </summary>
        public DateTime? IfModifiedSince {
            get {
                string value = this[IF_MODIFIED_SINCE];
                if(value != null) {

                    // NOTE: certain version of IE appends a "; size=x" to this header
                    value = value.Split(new char[] { ';' })[0];

                    DateTime result;
                    if(DateTimeUtil.TryParseInvariant(value, out result)) {
                        return result.ToUniversalTime();
                    }
                }
                return null;
            }
            set {
                this[IF_MODIFIED_SINCE] = (value != null) ? value.Value.ToString("r") : null;
            }
        }

        /// <summary>
        /// 'If-None-Match' Header.
        /// </summary>
        public string IfNoneMatch {
            get { return this[IF_NONE_MATCH]; }
            set { this[IF_NONE_MATCH] = value; }
        }

        /// <summary>
        /// 'If-Range' Header.
        /// </summary>
        public string IfRange {
            get { return this[IF_RANGE]; }
            set { this[IF_RANGE] = value; }
        }

        /// <summary>
        /// 'If-Unmodified-Since' Header.
        /// </summary>
        public DateTime? IfUnmodifiedSince {
            get {
                string value = this[IF_UNMODIFIED_SINCE];
                if(value != null) {
                    DateTime result;
                    if(DateTimeUtil.TryParseInvariant(value, out result)) {
                        return result.ToUniversalTime();
                    }
                }
                return null;
            }
            set {
                this[IF_UNMODIFIED_SINCE] = (value != null) ? value.Value.ToString("r") : null;
            }
        }

        /// <summary>
        /// 'Last-Modified' Header.
        /// </summary>
        public DateTime? LastModified {
            get {
                string value = this[LAST_MODIFIED];
                if(value != null) {
                    DateTime result;
                    if(DateTimeUtil.TryParseInvariant(value, out result)) {
                        return result.ToUniversalTime();
                    }
                }
                return null;
            }
            set {
                this[LAST_MODIFIED] = (value != null) ? value.Value.ToUniversalTime().ToString("r") : null;
            }
        }

        /// <summary>
        /// 'Location' Header.
        /// </summary>
        public XUri Location {
            get {
                string value = this[LOCATION];
                return (value != null) ? new XUri(value) : null;
            }
            set {
                this[LOCATION] = (value != null) ? value.ToString() : null;
            }
        }

        /// <summary>
        /// 'Max-Forwards' Header.
        /// </summary>
        public int? MaxForwards {
            get {
                string value = this[MAX_FORWARDS];
                return (value != null) ? (int?)int.Parse(value) : null;
            }
            set {
                this[MAX_FORWARDS] = (value != null) ? value.ToString() : null;
            }
        }

        /// <summary>
        /// 'Pragma' Header.
        /// </summary>
        public string Pragma {
            get { return this[PRAGMA]; }
            set { this[PRAGMA] = value; }
        }

        /// <summary>
        /// 'Range' Header.
        /// </summary>
        public string Range {
            get { return this[RANGE]; }
            set { this[RANGE] = value; }
        }

        /// <summary>
        /// 'Referer' Header.
        /// </summary>
        public string Referer {
            get { return this[REFERER]; }
            set { this[REFERER] = value; }
        }

        /// <summary>
        /// 'Response-Uri' Header.
        /// </summary>
        public XUri ResponseUri {
            get {
                string value = this[RESPONSE_URI];
                return (value != null) ? new XUri(value) : null;
            }
            set {
                this[RESPONSE_URI] = (value != null) ? value.ToString() : null;
            }
        }

        /// <summary>
        /// 'Retry-After' Header.
        /// </summary>
        public string RetryAfter {
            get { return this[RETRY_AFTER]; }
            set { this[RETRY_AFTER] = value; }
        }

        /// <summary>
        /// 'Server' Header.
        /// </summary>
        public string Server {
            get { return this[SERVER]; }
            set { this[SERVER] = value; }
        }

        /// <summary>
        /// 'Transfer-Encoding' Header.
        /// </summary>
        public string TransferEncoding {
            get { return this[TRANSFER_ENCODING]; }
            set { this[TRANSFER_ENCODING] = value; }
        }

        /// <summary>
        /// 'User-Agent' Header.
        /// </summary>
        public string UserAgent {
            get { return this[USER_AGENT]; }
            set { this[USER_AGENT] = value; }
        }

        /// <summary>
        /// 'Vary' Header.
        /// </summary>
        public string Vary {
            get { return this[VARY]; }
            set { this[VARY] = value; }
        }

        /// <summary>
        /// 'Proxy-Connection' Header.
        /// </summary>
        public string ProxyConnection {
            get { return this[PROXY_CONNECTION]; }
            set { this[PROXY_CONNECTION] = value; }
        }

        /// <summary>
        /// 'X-Dream-Transport' Header.
        /// </summary>
        public string DreamTransport {
            get { return this[DREAM_TRANSPORT]; }
            set { this[DREAM_TRANSPORT] = value; }
        }

        /// <summary>
        /// 'X-Dream-Service' Header.
        /// </summary>
        public string DreamService {
            get { return this[DREAM_SERVICE]; }
            set { this[DREAM_SERVICE] = value; }
        }

        /// <summary>
        /// 'X-Dream-Public-Uri' Header.
        /// </summary>
        public string DreamPublicUri {
            get { return this[DREAM_PUBLIC_URI]; }
            set { this[DREAM_PUBLIC_URI] = value; }
        }

        /// <summary>
        /// 'X-Dream-User-Host' Header.
        /// </summary>
        public string DreamUserHost {
            get { return this[DREAM_USER_HOST]; }
            set { this[DREAM_USER_HOST] = value; }
        }

        /// <summary>
        /// 'X-Dream-Origin' Header.
        /// </summary>
        public string DreamOrigin {
            get { return this[DREAM_ORIGIN]; }
            set { this[DREAM_ORIGIN] = value; }
        }

        /// <summary>
        /// 'X-Dream-ClientIP' Header.
        /// </summary>
        public string[] DreamClientIP {
            get { return this.GetValues(DREAM_CLIENTIP); }
            set {
                Remove(DREAM_CLIENTIP);
                foreach(string v in value) {
                    Add(DREAM_CLIENTIP, v);
                }
            }
        }

        /// <summary>
        /// 'X-Dream-Event-Id' Header.
        /// </summary>
        public string DreamEventId {
            get { return this[DREAM_EVENT_ID]; }
            set { this[DREAM_EVENT_ID] = value; }
        }

        /// <summary>
        /// 'X-Dream-Event-Channel' Header.
        /// </summary>
        public string DreamEventChannel {
            get { return this[DREAM_EVENT_CHANNEL]; }
            set { this[DREAM_EVENT_CHANNEL] = value; }
        }

        /// <summary>
        /// 'X-Dream-Event-Resource-Uri' Header.
        /// </summary>
        public string DreamEventResource {
            get { return this[DREAM_EVENT_RESOURCE]; }
            set { this[DREAM_EVENT_RESOURCE] = value; }
        }

        /// <summary>
        /// 'X-Dream-Event-Origin-Uri' Header.
        /// </summary>
        public string[] DreamEventOrigin {
            get { return this.GetValues(DREAM_EVENT_ORIGIN); }
            set {
                Remove(DREAM_EVENT_ORIGIN);
                foreach(string v in value) {
                    Add(DREAM_EVENT_ORIGIN, v);
                }
            }
        }

        /// <summary>
        /// 'X-Dream-Recipient-Uri' Header.
        /// </summary>
        public string[] DreamEventRecipients {
            get { return this.GetValues(DREAM_EVENT_RECIPIENT); }
            set {
                Remove(DREAM_EVENT_RECIPIENT);
                foreach(string v in value) {
                    Add(DREAM_EVENT_RECIPIENT, v);
                }
            }
        }

        /// <summary>
        /// 'X-Dream-Event-Via' Header.
        /// </summary>
        public string[] DreamEventVia {
            get { return this.GetValues(DREAM_EVENT_VIA); }
            set {
                Remove(DREAM_EVENT_VIA);
                foreach(string v in value) {
                    Add(DREAM_EVENT_VIA, v);
                }
            }
        }

        /// <summary>
        /// 'X-Dream-Request-Id' Header.
        /// </summary>
        public string DreamRequestId {
            get { return this[DREAM_REQUEST_ID]; }
            set { this[DREAM_REQUEST_ID] = value; }
        }

        /// <summary>
        /// 'X-XRDS-Location' Header.
        /// </summary>
        public XUri XrdsLocation {
            get {
                string value = this[XRDS_LOCATION];
                return (value != null) ? new XUri(value) : null;
            }
            set {
                this[XRDS_LOCATION] = (value != null) ? value.ToString() : null;
            }
        }

        /// <summary>
        /// 'X-HTTP-Method-Override' Header.
        /// </summary>
        public string MethodOverride {
            get { return this[METHOD_OVERRIDE]; }
            set { this[METHOD_OVERRIDE] = value; }
        }

        /// <summary>
        /// 'Front-End-Https' Header.
        /// </summary>
        public string FrontEndHttps {
            get { return this[FRONT_END_HTTPS]; }
            set { this[FRONT_END_HTTPS] = value; }
        }

        /// <summary>
        /// 'X-Forwarded-Host' Header.
        /// </summary>
        public string ForwardedHost {
            get { return this[FORWARDED_HOST]; }
            set { this[FORWARDED_HOST] = value; }
        }

        /// <summary>
        /// 'X-Forwarded-For' Header.
        /// </summary>
        public string[] ForwardedFor {
            get { return GetValues(FORWARDED_FOR); }
            set {
                Remove(FORWARDED_FOR);
                foreach(string v in value) {
                    Add(FORWARDED_FOR, v);
                }
            }
        }

        /// <summary>
        /// 'X-Forwarded-Server' Header.
        /// </summary>
        public string ForwardedServer {
            get { return this[FORWARDED_SERVER]; }
            set { this[FORWARDED_SERVER] = value; }
        }

        /// <summary>
        /// Accessor to any header by name.
        /// </summary>
        public string this[string name] {
            get {
                Entry entry;
                if(_headers.TryGetValue(name, out entry)) {
                    return entry.Value;
                }
                return null;
            }
            set {
                if(value == null) {
                    _headers.Remove(name);
                } else {
                    _headers[name] = new Entry(value);
                }
            }
        }

        //--- Methods ---

        /// <summary>
        /// Clear all headers.
        /// </summary>
        public void Clear() {
            _cookies = null;
            _headers.Clear();
        }

        /// <summary>
        /// Add a header.
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value.</param>
        /// <returns>Current instance.</returns>
        public DreamHeaders Add(string name, string value) {
            if(name == COOKIE) {
                Cookies.AddRange(DreamCookie.ParseCookieHeader(value));
            } else if(name == SET_COOKIE) {
                Cookies.AddRange(DreamCookie.ParseSetCookieHeader(value));
            } else {
                Entry entry;
                if(_headers.TryGetValue(name, out entry)) {
                    entry.Last.Next = new Entry(value);
                } else {

                    // create a new entry
                    _headers[name] = new Entry(value);
                }
            }
            return this;
        }

        /// <summary>
        /// Add a range of headers.
        /// </summary>
        /// <param name="headers">Header collection</param>
        /// <returns>Current instance.</returns>
        public DreamHeaders AddRange(DreamHeaders headers) {
            if(headers != null) {

                // add entries
                foreach(KeyValuePair<string, Entry> header in headers._headers) {
                    Entry existing;

                    // find existing entry
                    if(_headers.TryGetValue(header.Key, out existing)) {
                        existing = existing.Last;
                    }

                    // add new entries
                    for(Entry other = header.Value; other != null; other = other.Next) {
                        if(existing == null) {
                            existing = new Entry(other.Value);
                            _headers[header.Key] = existing;
                        } else {
                            existing.Next = new Entry(other.Value);
                            existing = existing.Next;
                        }
                    }
                }

                // add cookies
                if(headers.HasCookies) {
                    foreach(DreamCookie cookie in headers.Cookies) {
                        Cookies.Add(cookie);
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Add a range of headers.
        /// </summary>
        /// <param name="headers">Header collection.</param>
        /// <returns>Current instance.</returns>
        public DreamHeaders AddRange(NameValueCollection headers) {
            if(headers != null) {
                foreach(string key in headers.Keys) {
                    if(key.EqualsInvariant(COOKIE)) {
                        var value = headers[key];
                        if(!string.IsNullOrEmpty(value)) {
                            Cookies.AddRange(DreamCookie.ParseCookieHeader(value));
                        }
                    } else if(key.EqualsInvariant(SET_COOKIE)) {
                        var value = headers[key];
                        if(!string.IsNullOrEmpty(value)) {
                            Cookies.AddRange(DreamCookie.ParseSetCookieHeader(value));
                        }
                    } else {
                        string[] values = headers.GetValues(key);
                        if(!ArrayUtil.IsNullOrEmpty(values)) {
                            if(key.EqualsInvariant(FORWARDED_HOST) || key.EqualsInvariant(FORWARDED_FOR)) {

                                // NOTE (steveb): 'X-Forwarded-Host', 'X-Forwarded-For' may contain one or more entries, but NameValueCollection doesn't seem to care :(

                                // initialize 'last' to be the last entry added (if any) for the current header key
                                Entry last;
                                _headers.TryGetValue(key, out last);
                                if(last != null) {
                                    last = last.Last;
                                }

                                // loop over all header values
                                for(int i = 0; i < values.Length; ++i) {
                                    foreach(string value in values[i].Split(' ')) {
                                        string host = value;
                                        if((host.Length > 0) && (host[host.Length - 1] == ',')) {
                                            host = host.Substring(0, host.Length - 1);
                                        }
                                        host = host.Trim();
                                        if(!string.IsNullOrEmpty(host)) {
                                            if(last != null) {
                                                last.Next = new Entry(host, null);
                                                last = last.Next;
                                            } else {
                                                _headers[key] = last = new Entry(host, null);
                                            }
                                        }
                                    }
                                }
                            } else {
                                Entry last = null;
                                for(int i = 0; i < values.Length; ++i) {
                                    if(last != null) {
                                        last.Next = new Entry(values[i], null);
                                        last = last.Next;
                                    } else {
                                        _headers[key] = last = new Entry(values[i], null);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Add a range of headers.
        /// </summary>
        /// <param name="headers">Key value pair collection</param>
        /// <returns>Current instance.</returns>
        public DreamHeaders AddRange(IEnumerable<KeyValuePair<string, string>> headers) {
            if(headers != null) {
                foreach(var header in headers) {
                    if(header.Key.EqualsInvariant(COOKIE)) {
                        if(!string.IsNullOrEmpty(header.Value)) {
                            Cookies.AddRange(DreamCookie.ParseCookieHeader(header.Value));
                        }
                    } else if(header.Key.EqualsInvariant(SET_COOKIE)) {
                        if(!string.IsNullOrEmpty(header.Value)) {
                            Cookies.AddRange(DreamCookie.ParseSetCookieHeader(header.Value));
                        }
                    } else {

                        // Note (arnec): assume that there are no multi-value values
                        Entry existing;

                        // find existing entry
                        if(_headers.TryGetValue(header.Key, out existing)) {
                            existing = existing.Last;
                        }

                        // add new entries
                        if(existing == null) {
                            existing = new Entry(header.Value);
                            _headers[header.Key] = existing;
                        } else {
                            existing.Next = new Entry(header.Value);
                        }
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Remove a header.
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <returns>Current instance.</returns>
        public DreamHeaders Remove(string name) {
            _headers.Remove(name);
            return this;
        }

        /// <summary>
        /// Get header values.
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <returns>Array of values.</returns>
        public string[] GetValues(string name) {
            List<string> result = new List<string>();
            Entry entry;
            for(_headers.TryGetValue(name, out entry); entry != null; entry = entry.Next) {
                result.Add(entry.Value);
            }
            return result.ToArray();
        }

        //--- IEnumerable<KeyValuePair<string, string>> Members ---
        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator() {
            foreach(KeyValuePair<string, Entry> header in _headers) {
                for(Entry entry = header.Value; entry != null; entry = entry.Next) {
                    yield return new KeyValuePair<string, string>(header.Key, entry.Value);
                }
            }
        }

        //--- IEnumerable Members ---
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return ((IEnumerable<KeyValuePair<string, string>>)this).GetEnumerator();
        }
    }
}
