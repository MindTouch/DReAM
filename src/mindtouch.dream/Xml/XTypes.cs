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

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Web;

namespace MindTouch.Xml {

    /// <summary>
    /// Provides a way to create an Xml serialization of an exception.
    /// </summary>
    public class XException : XDoc {

        //--- Types ---
        private class Ex : Exception {

            //--- Fields ---
            public readonly string RealType;
            private string _stacktrace;

            //--- Constructors ---
            internal Ex(string realtype, string message, string stacktrace, Exception inner)
                : base(message, inner) {
                this.RealType = realtype;
                _stacktrace = stacktrace;
            }

            //--- Properties ---
            public override string StackTrace {
                get {
                    return _stacktrace;
                }
            }

            public override string Message {
                get {
                    return "(Exception: " + RealType + ") " + base.Message;
                }
            }
        }

        //--- Class Methods ---

        /// <summary>
        /// Deserialize an exception from an Xml document represenation.
        /// </summary>
        /// <param name="exception">Exception Xml document.</param>
        /// <returns>Deserialized exception.</returns>
        public static Exception MakeException(XDoc exception) {
            return MakeIntenalException(exception);
        }

        private static Ex MakeIntenalException(XDoc exception) {
            if((exception == null) || exception.IsEmpty) {
                throw new ArgumentNullException("exception");
            }
            if(!exception.Name.EqualsInvariant("exception")) {
                throw new ArgumentException("argument must have <exception> as root element", "exception");
            }

            // check for presence of an inner exception
            Ex innerException = null;
            XDoc innerExceptionDoc = exception["exception"];
            if(!innerExceptionDoc.IsEmpty) {
                innerException = MakeIntenalException(innerExceptionDoc);
            }

            // rebuild stack trace
            StringBuilder stacktrace = new StringBuilder();
            List<XDoc> frames = exception["stacktrace/frame"].ToList();
            for(int i = 0; i < frames.Count; ++i) {
                XDoc frame = frames[i];
                string method = frame["@method"].AsText;
                string file = frame["@file"].AsText;
                string line = frame["@line"].AsText;
                string message = frame.AsText;
                if(method != null) {
                    stacktrace.Append("   at ");
                    stacktrace.Append(method);
                }
                if(file != null) {
                    stacktrace.Append(" in ");
                    stacktrace.Append(file);
                }
                if(line != null) {
                    stacktrace.Append(":line ");
                    stacktrace.Append(line);
                }
                if(message != null) {
                    stacktrace.Append(message);
                }
                if(i != (frames.Count - 1)) {
                    stacktrace.AppendLine();
                }
            }

            // build exception
            Ex result = new Ex(exception["type"].AsText ?? typeof(Ex).FullName, exception["message"].AsText, stacktrace.ToString(), innerException);
            result.Source = exception["source"].AsText;
            result.HelpLink = exception["helplink"].AsText;
            return result;
        }

        /// <summary>
        /// Add a stack trace to an Xml serialized exception.
        /// </summary>
        /// <param name="exception">Xml serialized exception.</param>
        /// <param name="stacktrace">Stack trace.</param>
        public static void AddStackTrace(XDoc exception, string stacktrace) {
            foreach(string trace in stacktrace.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)) {
                string line = null;
                string method = null;
                string path = null;
                string message = trace.Trim();
                message = (message.StartsWithInvariant("at ") ? message.Substring(3) : message);

                // extract line information
                int colonIndex = message.LastIndexOf(':');
                if(colonIndex > 0) {
                    line = message.Substring(colonIndex);
                    line = (line.StartsWithInvariant(":line ") ? line.Substring(6) : line);
                    message = message.Substring(0, colonIndex);
                }

                // extract method & path information
                int inIndex = message.IndexOf(" in ");
                if(inIndex > 0) {
                    method = message.Substring(0, inIndex);
                    path = message.Substring(inIndex + 4);
                    message = null;
                }

                // add stack information
                exception.Start("frame")
                    .Attr("method", method)
                    .Attr("file", path)
                    .Attr("line", line)
                    .Value(message)
                .End();
            }
        }

        //--- Construcotrs ---

        /// <summary>
        /// Serialize an exception into xml.
        /// </summary>
        /// <param name="e">The exception to serialize.</param>
        public XException(Exception e) : base("exception") {
            Elem("type", e.GetType().FullName);
            Elem("message", e.Message);
            Elem("source", e.Source);
            Elem("helplink", e.HelpLink);

            // append stack trace
            string stacktrace = e.StackTrace;
            if(stacktrace != null) {
                Start("stacktrace");
                AddStackTrace(this, stacktrace);
                End();
            }

            // check if inner exception is present
            if(e.InnerException != null) {
                Add(new XException(e.InnerException));
            }

            // check if a response is present
            if(e is DreamResponseException) {
                Add(new XMessage(((DreamResponseException)e).Response));
            }

            // check if exception contains coroutine information
            var coroutine = e.GetCoroutineInfo();
            if(coroutine != null) {
                Start("coroutine");
                foreach(var frame in coroutine.GetStackTrace()) {

                    // add stack information
                    Start("frame")
                        .Attr("method", frame.FullName)
                    .End();
                }
                End();
            }
        }
    }

    /// <summary>
    /// Provides a mechanism for serializing a <see cref="DreamMessage"/> as an Xml document.
    /// </summary>
    public class XMessage : XDoc {

        //--- Constructors ---

        /// <summary>
        /// Serialize message into Xml.
        /// </summary>
        /// <param name="message">Message to serialize.</param>
        public XMessage(DreamMessage message)
            : base("message") {
            Elem("status", (int)message.Status);
            Start("headers");
            foreach(KeyValuePair<string, string> pair in message.Headers) {
                Elem(pair.Key, pair.Value ?? string.Empty);
            }
            if(message.HasCookies) {
                foreach(DreamCookie cookie in message.Cookies) {

                    // NOTE (steveb): we rely on the 'Version' being set to differentiate between 'Cookie' and a 'Set-Cookie' headers

                    if(cookie.Version != 0) {
                        Add(cookie.AsSetCookieDocument);
                    } else {
                        Add(cookie.AsCookieDocument);
                    }
                }
            }
            End();
            Start("body");
            if(message.HasDocument) {
                Attr("format", "xml").Add(message.ToDocument());
            } else {
                byte[] bytes = message.ToBytes();
                string body = null;
                string attr = "none";

                // TODO (steveb): we need to use a content-type matching algorithm and match against all known text types (text/*, application/json, etc.)
                if(bytes.Length > 0) {
                    if(message.ContentType.Match(MimeType.ANY_TEXT)) {
                        body = message.ContentType.CharSet.GetString(bytes).EncodeHtmlEntities(Encoding.UTF8, false);
                        attr = "text";
                    } else {
                        body = Convert.ToBase64String(bytes);
                        attr = "base64";
                    }
                }
                Attr("format", attr).Value(body);
            }
            End();
        }
    }
}
