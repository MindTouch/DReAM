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

namespace MindTouch.Dream {

    /// <summary>
    /// Provides Dream Service meta-data for <see cref="IDreamService"/> implementations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class DreamServiceAttribute : Attribute {

        //--- Fields ---
        private string _name;
        private string _copyright;
        private string _info;
        private XUri[] _sid;

        //--- Constructors ---

        /// <summary>
        /// Create new attribute.
        /// </summary>
        /// <param name="name">Dream Service name.</param>
        /// <param name="copyright">Copyright notice.</param>
        public DreamServiceAttribute(string name, string copyright) {
            _name = name;
            _copyright = copyright;
        }

        /// <summary>
        /// Create new attribute.
        /// </summary>
        /// <param name="name">Dream Service name.</param>
        /// <param name="copyright">Copyright notice.</param>
        /// <param name="info">Service information Uri.</param>
        public DreamServiceAttribute(string name, string copyright, string info) {
            _name = name;
            _copyright = copyright;
            _info = info;
        }

        //--- Properties ---

        /// <summary>
        /// Dream Service name.
        /// </summary>
        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Copyright notice.
        /// </summary>
        public string Copyright {
            get { return _copyright; }
            set { _copyright = value; }
        }

        /// <summary>
        /// Service inforamtion Uri.
        /// </summary>
        public string Info {
            get { return _info; }
            set { _info = value; }
        }

        /// <summary>
        /// Service Identifier Uris.
        /// </summary>
        public string[] SID {
            get {
                return Array.ConvertAll<XUri, string>(_sid, delegate(XUri uri) { return uri.ToString(); });
            }
            set {
                _sid = Array.ConvertAll<string, XUri>(value, delegate(string uri) { return new XUri(uri); });
            }
        }

        //--- Methods ---

        /// <summary>
        /// Get all Service Identifiers as XUri array.
        /// </summary>
        /// <returns>Array of Service Identifiers.</returns>
        public XUri[] GetSIDAsUris() {
            return _sid ?? new XUri[0]; 
        }
    }

    /// <summary>
    /// Provides meta data about expected service configuration key.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class DreamServiceConfigAttribute : Attribute {

        //--- Fields ---
        private string _name;
        private string _valueType;
        private string _description;

        //--- Constructors ---

        /// <summary>
        /// Create new attribute.
        /// </summary>
        /// <param name="name">Configuration Key.</param>
        /// <param name="type">Configuration Data Type.</param>
        /// <param name="description">Description of configuration value.</param>
        public DreamServiceConfigAttribute(string name, string type, string description) {
            _name = name;
            _valueType = type;
            _description = description;
        }

        //--- Properties ---
        /// <summary>
        /// Configuration key.
        /// </summary>
        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Configuration Data Type.
        /// </summary>
        public string ValueType {
            get { return _valueType; }
            set { _valueType = value; }
        }

        /// <summary>
        /// Description of configuration value.
        /// </summary>
        public string Description {
            get { return _description; }
            set { _description = value; }
        }
    }

    /// <summary>
    /// Provides addtional <see cref="IDreamService"/> blueprint meta-data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class DreamServiceBlueprintAttribute : Attribute {

        //--- Fields ---

        /// <summary>
        /// Name of meta-data key.
        /// </summary>
        public string Name;

        /// <summary>
        /// Blueprint meta-data value.
        /// </summary>
        public string Value;

        //--- Constructors ---

        /// <summary>
        /// Create new attribute.
        /// </summary>
        /// <param name="name">Name of meta-data key.</param>
        /// <param name="value">Blueprint meta-data value.</param>
        public DreamServiceBlueprintAttribute(string name, string value) {
            if(string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException("name");
            }
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Create new attribute
        /// </summary>
        /// <param name="name">Name of meta-data key.</param>
        public DreamServiceBlueprintAttribute(string name) : this(name, string.Empty) { }
    }

    /// <summary>
    /// Marks a method as <see cref="DreamFeature"/> and provides feature meta-data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class DreamFeatureAttribute : Attribute {

        // TODO (steveb): enforce syntax for feature definitions

        // NOTE (steveb): grammar for definition string
        //      definition  ::= id  ":" path pattern
        //      path        ::= ( id // "/" )
        //      pattern     ::= ( "/" // ( "*" | name | id )? ( "/" ( name )? "?" )* ( "//" ( "*" | name ))? ( "/" )?
        //      name        ::= "{" id "}"
        //      id          ::= ( 'a'..'z' | 'A'..'Z' | '0'..'9' | '-' | '.' | '_' | '~' | '%' | '!' | '$' | '&' | '\'' | '(' | ')' | '*' | '+' | ',' | ';' | '=' | ':' | '@' )*

        //--- Fields ---
        private string _pattern;
        private string _description;
        private string _info;
        private string _version = "*";
        private string _obsolete = null;

        //--- Constructors ---

        /// <summary>
        /// Obsolete: This form of the attribute construtor is deprecated. Please DreamFeatureAttribute(string,string) instead.
        /// </summary>
        [Obsolete("This form of the attribute construtor is deprecated. Please DreamFeatureAttribute(string,string) instead.")]
        public DreamFeatureAttribute(string path, string signature, string verb, string description, string info) {

            // remove trailing slash
            if((path.Length > 0) && (path[path.Length - 1] == '/')) {
                path = path.Substring(0, path.Length - 1);
            }

            // add initial slash
            if((signature.Length == 0) || (signature[0] != '/')) {
                signature = "/" + signature;
            }

            // initialize fields
            _pattern = verb + ":" + path + signature;
            _description = description;
            _info = info;
        }

        /// <summary>
        /// Create new attribute.
        /// </summary>
        /// <param name="pattern">The Uri pattern that this feature responds to.</param>
        /// <param name="description">Description of the <see cref="DreamFeature"/>.</param>
        public DreamFeatureAttribute(string pattern, string description) {
            _pattern = pattern;
            _description = description;
        }

        //--- Properties ---	

        /// <summary>
        /// The Uri pattern that this feature responds to.
        /// </summary>
        public string Pattern {
            get { return _pattern; }
            set { _pattern = value; }
        }

        /// <summary>
        /// Description of the <see cref="DreamFeature"/>.
        /// </summary>
        public string Description {
            get { return _description; }
            set { _description = value; }
        }

        /// <summary>
        /// Information Uri for this <see cref="DreamFeature"/>.
        /// </summary>
        public string Info {
            get { return _info; }
            set { _info = value; }
        }

        /// <summary>
        /// <see cref="DreamFeature"/> Version.
        /// </summary>
        public string Version {
            get { return _version; }
            set { _version = value; }
        }

        /// <summary>
        /// True if this feature is obsolete.
        /// </summary>
        public string Obsolete {
            get { return _obsolete; }
            set { _obsolete = value; }
        }
    }

    /// <summary>
    /// Defines a <see cref="DreamFeature"/> input parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class DreamFeatureParamAttribute : Attribute {

        //--- Fields ---
        private string _name;
        private string _valueType;
        private string _description;

        //--- Constructors ---

        /// <summary>
        /// Create new attribute.
        /// </summary>
        /// <param name="name">Name of the parameter.</param>
        /// <param name="type">Parameter Data Type.</param>
        /// <param name="description">Description of the parameter.</param>
        public DreamFeatureParamAttribute(string name, string type, string description) {
            _name = name;
            _valueType = type;
            _description = description;
        }

        //--- Properties ---
        
        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Parameter Data Type.
        /// </summary>
        public string ValueType {
            get { return _valueType; }
            set { _valueType = value; }
        }

        /// <summary>
        /// Description of the parameter.
        /// </summary>
        public string Description {
            get { return _description; }
            set { _description = value; }
        }
    }

    /// <summary>
    /// Provides <see cref="DreamFeature"/> meta-data about possible <see cref="DreamStatus"/> responses from feature.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class DreamFeatureStatusAttribute : Attribute {

        //--- Fields ---
        private DreamStatus _status;
        private string _description;

        //--- Constructors ---

        /// <summary>
        /// Create new attribute.
        /// </summary>
        /// <param name="status">Status message for Feature.</param>
        /// <param name="description">Description of scenario under which Status is returned.</param>
        public DreamFeatureStatusAttribute(DreamStatus status, string description) {
            _status = status;
            _description = description;
        }

        //--- Properties ---

        /// <summary>
        /// Status message for Feature.
        /// </summary>
        public DreamStatus Status {
            get { return _status; }
            set { _status = value; }
        }

        /// <summary>
        /// Description of scenario under which Status is returned.
        /// </summary>
        public string Description {
            get { return _description; }
            set { _description = value; }
        }
    }

    /// <summary>
    /// Defines a <see cref="DreamFeature"/> input header parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class HeaderAttribute : Attribute {
        
        //--- Constructors ---

        /// <summary>
        /// Create new attribute.
        /// </summary>
        public HeaderAttribute() { }

        /// <summary>
        /// Create new attribute.
        /// </summary>
        public HeaderAttribute(string description) {
            this.Description = description;
        }

        //--- Properties ---

        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the parameter.
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Defines a <see cref="DreamFeature"/> input cookie parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class CookieAttribute : Attribute {

        //--- Constructors ---

        /// <summary>
        /// Create new attribute.
        /// </summary>
        public CookieAttribute() { }

        /// <summary>
        /// Create new attribute.
        /// </summary>
        /// <param name="description">Description of the parameter.</param>
        public CookieAttribute(string description) {
            this.Description = description;
        }

        //--- Properties ---

        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the parameter.
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Defines a <see cref="DreamFeature"/> input path parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PathAttribute : Attribute {

        //--- Constructors ---

        /// <summary>
        /// Create new attribute.
        /// </summary>
        public PathAttribute() { }

        /// <summary>
        /// Create new attribute.
        /// </summary>
        /// <param name="description">Description of the parameter.</param>
        public PathAttribute(string description) {
            this.Description = description;
        }

        //--- Properties ---

        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the parameter.
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Defines a <see cref="DreamFeature"/> input query parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class QueryAttribute : Attribute {

        //--- Constructors ---

        /// <summary>
        /// Create new attribute.
        /// </summary>
        public QueryAttribute() { }

        /// <summary>
        /// Create new attribute.
        /// </summary>
        /// <param name="description">Description of the parameter.</param>
        public QueryAttribute(string description) {
            this.Description = description;
        }

        //--- Properties ---

        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the parameter.
        /// </summary>
        public string Description { get; set; }
    }
}