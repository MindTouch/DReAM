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

namespace MindTouch.Xml {

    /// <summary>
    /// Simple soap envelope extension on top of XDoc for building Soap messages.
    /// </summary>
    public class XSoap : XDoc {

        //--- Constants ---
        /// <summary>
        /// Soap envelope namespace uri.
        /// </summary>
        public const string SOAP_NAMESPACE = "http://schemas.xmlsoap.org/soap/envelope/";

        /// <summary>
        /// Soap encoding namespace uri.
        /// </summary>
        public const string SOAP_ENCODING_NAMESPACE = "http://schemas.xmlsoap.org/soap/encoding/";

        /// <summary>
        /// Xml Schema instance namespace uri.
        /// </summary>
        public const string XSI_NAMESPACE = "http://www.w3.org/2001/XMLSchema-instance";

        /// <summary>
        /// Xml Schema namespace uri.
        /// </summary>
        public const string XSD_NAMESPACE = "http://www.w3.org/2001/XMLSchema";

        //--- Constructors ---

        /// <summary>
        /// Create a version of an Xml document with the soap namespaces and prefixes imported.
        /// </summary>
        /// <param name="doc">Existing document to wrap.</param>
        public XSoap(XDoc doc) : base(doc) {
            UsePrefix("soap", SOAP_NAMESPACE);
            UsePrefix("soapenc", SOAP_ENCODING_NAMESPACE);
            UsePrefix("xsi", XSI_NAMESPACE);
            UsePrefix("xsd", XSD_NAMESPACE);
        }

        /// <summary>
        /// Create a new empty soap envelope.
        /// </summary>
        /// <param name="tag">Root node name.</param>
        public XSoap(string tag) : base("soap", tag, SOAP_NAMESPACE) {
            UsePrefix("soap", SOAP_NAMESPACE);
            UsePrefix("soapenc", SOAP_ENCODING_NAMESPACE);
            UsePrefix("xsi", XSI_NAMESPACE);
            UsePrefix("xsd", XSD_NAMESPACE);
        }

        /// <summary>
        /// Create a new empty envelope.
        /// </summary>
        public XSoap() : this("Envelope") { }

        //--- Properties ---

        /// <summary>
        /// Accesss the envelope header.
        /// </summary>
        public XDoc Header {
            get {
                return this["soap:Header"];
            }
        }

        /// <summary>
        /// Access the envelope body.
        /// </summary>
        public XDoc Body {
            get {
                return this["soap:Body"];
            }
        }

        /// <summary>
        /// Access the body fault message (if one exists).
        /// </summary>
        public XDoc Fault {
            get {
                return this["soap:Body/soap:Fault"];
            }
        }
    }
}
