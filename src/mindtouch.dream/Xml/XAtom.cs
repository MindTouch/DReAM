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

using MindTouch.Dream;

namespace MindTouch.Xml {

    /// <summary>
    /// Provides an a base Xml document abstraction based on <see cref="XDoc"/> with additional methods to ease creating Atom documents.
    /// </summary>
    public abstract class XAtomBase : XDoc {

        //--- Constants ---

        /// <summary>
        /// Atom xml namespace
        /// </summary>
        public const string ATOM_NAMESPACE = "http://www.w3.org/2005/Atom";

        //--- Types ---

        /// <summary>
        /// Atom link rel attribute enumeration.
        /// </summary>
        public enum LinkRelation {

            /// <summary>
            /// Signifies a link that points to an alternate version of the current resource.
            /// </summary>
            Alternate,

            /// <summary>
            /// Signifies a link that points to a resource that is related to the current content.
            /// </summary>
            Related,

            /// <summary>
            /// Signifies a link to a resource that is the equivalent of the current content.
            /// </summary>
            Self,

            /// <summary>
            /// Signifies a link to a related resource that may be large in size or requires special handling.
            /// </summary>
            Enclosure,

            /// <summary>
            /// Signifies a link to a resource that is the source of the materail of the current content.
            /// </summary>
            Via,

            /// <summary>
            /// Signifies a link to the resource that allows editing of the current content.
            /// </summary>
            Edit,

            /// <summary>
            /// Signifies a link to furthest preceeding resource in a series of resources.
            /// </summary>
            First,
            
            /// <summary>
            /// Signifies a link to furthest following resource in a series of resources.
            /// </summary>
            Last,

            /// <summary>
            /// Signifies a link to immediately following resource in a series of resources.
            /// </summary>
            Next,

            /// <summary>
            /// Signifies a link to immediately preceeding resource in a series of resources.
            /// </summary>
            Previous
        }

        //--- Constructors ---

        /// <summary>
        /// Create a new Atom document from an existing document.
        /// </summary>
        /// <param name="doc">The source document.</param>
        public XAtomBase(XDoc doc) : base(doc) { }

        /// <summary>
        /// Creates an Atom document with a given root tag and updated date.
        /// </summary>
        /// <param name="tag">Root tag.</param>
        /// <param name="updated">Date the document was last updated.</param>
        public XAtomBase(string tag, DateTime updated) : base(tag, ATOM_NAMESPACE) {
            Start("generator").Attr("version", DreamUtil.DreamVersion).Value("MindTouch Dream XAtom").End();
            Start("updated").Value(updated).End();
        }

        //--- Properties ---

        /// <summary>
        /// Atom Id uri.
        /// </summary>
        public XUri Id {
            get {
                return this["id"].AsUri;
            }
            set {
                if(value == null) {
                    this["id"].Remove();
                } else if(this["id"].IsEmpty) {
                    Elem("id", value);
                } else {
                    this["id"].ReplaceValue(value);
                }
            }
        }

        //--- Methods ---

        /// <summary>
        /// Add an author to the document.
        /// </summary>
        /// <param name="name">Author name.</param>
        /// <param name="uri">Uri to Author resource.</param>
        /// <param name="email">Author email.</param>
        /// <returns>Returns the current document instance.</returns>
        public XAtomBase AddAuthor(string name, XUri uri, string email) {
            return AddPerson("author", name, uri, email);
        }

        /// <summary>
        /// Add a contributor to the document.
        /// </summary>
        /// <param name="name">Contributor name.</param>
        /// <param name="uri">Uri to Contributor resource.</param>
        /// <param name="email">Contributor email.</param>
        /// <returns>Returns the current document instance.</returns>
        public XAtomBase AddContributor(string name, XUri uri, string email) {
            return AddPerson("contributor", name, uri, email);
        }

        /// <summary>
        /// Add a category to the document.
        /// </summary>
        /// <param name="term">Category term.</param>
        /// <param name="scheme">Category scheme.</param>
        /// <param name="label">Category label.</param>
        /// <returns>Returns the current document instance.</returns>
        public XAtomBase AddCategory(string term, XUri scheme, string label) {
            if(string.IsNullOrEmpty(term)) {
                throw new ArgumentNullException("term");
            }
            Start("category");
            Attr("term", term);
            if(scheme != null) {
                Attr("scheme", scheme);
            }
            if(!string.IsNullOrEmpty(label)) {
                Attr("label", label);
            }
            End();
            return this;
        }

        /// <summary>
        /// Add a link to the document.
        /// </summary>
        /// <param name="href">Uri to the linked resource.</param>
        /// <param name="relation">Relationship of the linked resource to the current document.</param>
        /// <param name="type">Type of resource being linked.</param>
        /// <param name="length">Size in bytes.</param>
        /// <param name="title">Title of the linked resource.</param>
        /// <returns>Returns the current document instance.</returns>
        public XAtomBase AddLink(XUri href, LinkRelation relation, MimeType type, long? length, string title) {
            Start("link");
            Attr("href", href);
            Attr("rel", relation.ToString().ToLowerInvariant());
            if(type != null) {
                Attr("type", type.FullType);
            }
            if(length != null) {
                Attr("length", length ?? 0);
            }
            if(!string.IsNullOrEmpty(title)) {
                Attr("title", title);
            }
            End();
            return this;
        }

        /// <summary>
        /// Add text to the document.
        /// </summary>
        /// <param name="tag">Enclosing tag for the text.</param>
        /// <param name="type">Type attribute for the enclosed.</param>
        /// <param name="text">Text content to add.</param>
        /// <returns>Returns the current document instance.</returns>
        public XAtomBase AddText(string tag, string type, string text) {
            Start(tag).Attr("type", type).Value(text).End();
            return this;
        }


        /// <summary>
        /// Add text to the document.
        /// </summary>
        /// <param name="tag">Enclosing tag for the text.</param>
        /// <param name="mime">Mime type of the enclosed text.</param>
        /// <param name="data">The text body as a byte array.</param>
        /// <returns>Returns the current document instance.</returns>
        public XAtomBase AddText(string tag, MimeType mime, byte[] data) {
            Start(tag).Attr("type", mime.FullType).Value(data).End();
            return this;
        }

        /// <summary>
        /// Add text to the document.
        /// </summary>
        /// <param name="tag">Enclosing tag for the text.</param>
        /// <param name="mime">Mime type of the enclosed text.</param>
        /// <param name="xml">The body document to add.</param>
        /// <returns>Returns the current document instance.</returns>
        public XAtomBase AddText(string tag, MimeType mime, XDoc xml) {
            if(mime.Match(MimeType.XHTML)) {
                Start(tag).Attr("type", "xhtml");

                // add content and normalize the root node
                XDoc added = xml.Clone().Rename("div");
                if(added["@xmlns"].IsEmpty) {
                    added.Attr("xmlns", "http://www.w3.org/1999/xhtml");
                }
                Add(added);
            } else if(mime.Match(MimeType.HTML)) {
                Start(tag).Attr("type", "html");

                // embed HTML as text
                Value(xml.ToInnerXHtml());
            } else {
                Start(tag).Attr("type", mime.FullType);
                Add(xml);
            }

            // close element
            End();
            return this;
        }

        /// <summary>
        /// Add a person reference to the document.
        /// </summary>
        /// <param name="tag">Enclosing tag for the person entry.</param>
        /// <param name="name">Person name.</param>
        /// <param name="uri">Uri to Person resource.</param>
        /// <param name="email">Person email.</param>
        /// <returns>Returns the current document instance.</returns>
        public XAtomBase AddPerson(string tag, string name, XUri uri, string email) {
            Start(tag);
            Elem("name", name ?? string.Empty);
            if(uri != null) {
                Elem("uri", uri);
            }
            if(!string.IsNullOrEmpty(email)) {
                Elem("email", email);
            }
            End();
            return this;
        }
    }


    /// <summary>
    /// Provides a Atom feed document abstraction based on <see cref="XDoc"/>.
    /// </summary>
    public class XAtomFeed : XAtomBase {

        //--- Constructors ---

        /// <summary>
        /// Parse an existing Atom feed into the <see cref="XAtomFeed"/> representation.
        /// </summary>
        /// <param name="doc">The document to parse as an atom feed.</param>
        public XAtomFeed(XDoc doc) : base(doc) {
            if(doc.Name != "feed") {
                throw new ArgumentException("doc");
            }
            UsePrefix("georss", "http://www.georss.org/georss");
            UsePrefix("gml", "http://www.opengis.net/gml");
        }

        /// <summary>
        /// Create a new Atom feed.
        /// </summary>
        /// <param name="title">Title of the feed.</param>
        /// <param name="link">Canonical uri to the feed resource.</param>
        /// <param name="updated">Last time the feed was updated.</param>
        public XAtomFeed(string title, XUri link, DateTime updated) : base("feed", updated) {
            if(string.IsNullOrEmpty(title)) {
                throw new ArgumentNullException("title");
            }
            if(link == null) {
                throw new ArgumentNullException("link");
            }

            // feed elements
            Start("title").Attr("type", "text").Value(title).End();
            Start("link").Attr("rel", "self").Attr("href", link).End();
        }

        //--- Properties ---

        /// <summary>
        /// Get an array of all entries in the feed.
        /// </summary>
        public XAtomEntry[] Entries {
            get {
                return Array.ConvertAll<XDoc, XAtomEntry>(Root["_:entry"].ToList().ToArray(), delegate(XDoc entry) { return new XAtomEntry(entry);});
            }
        }

        //--- Methods ---

        /// <summary>
        /// Start a new entry block.
        /// </summary>
        /// <remarks>Behaves like the normal <see cref="XDoc.Start(string)"/> and sets the cursor to the new entry until a matching
        /// <see cref="XDoc.End()"/> is encountered.</remarks>
        /// <param name="title">Entry title.</param>
        /// <param name="published">Entry publication date.</param>
        /// <param name="updated">Last time the entry was updated.</param>
        /// <returns>A new <see cref="XAtomEntry"/> as a node in the current document.</returns>
        public XAtomEntry StartEntry(string title, DateTime published, DateTime updated) {
            Start("entry");
            AddText("title", "text", title);
            Elem("published", published);
            Elem("updated", updated);
            return new XAtomEntry(this);
        }
    }

    /// <summary>
    /// Provides a Atom feed entry document abstraction based on <see cref="XDoc"/>.
    /// </summary>
    public class XAtomEntry : XAtomBase {

        //--- Constructors ---

        /// <summary>
        /// Parse an existing Atom feed entry into the <see cref="XAtomEntry"/> representation.
        /// </summary>
        /// <param name="doc">Document in the Atom feed entry format.</param>
        public XAtomEntry(XDoc doc) : base(doc) {
            if(doc.Name != "entry") {
                throw new ArgumentException("doc");
            }
            UsePrefix("georss", "http://www.georss.org/georss");
            UsePrefix("gml", "http://www.opengis.net/gml");
        }

        /// <summary>
        /// Create a new Atom feed entry document.
        /// </summary>
        /// <param name="title">Entry title.</param>
        /// <param name="published">Entry publication date.</param>
        /// <param name="updated">Last time the entry was updated.</param>
        public XAtomEntry(string title, DateTime published, DateTime updated) : base("entry", updated) {
            Attr("xmlns:georss", "http://www.georss.org/georss");
            UsePrefix("georss", "http://www.georss.org/georss");
            Attr("xmlns:gml", "http://www.opengis.net/gml");
            UsePrefix("gml", "http://www.opengis.net/gml");
            AddText("title", "text", title);
            Start("published").Value(published).End();
        }

        //--- Properties ---

        /// <summary>
        /// Entry Geo Rss Tag.
        /// </summary>
        public Tuplet<double, double> Where {
            get {
                XDoc where = Root["georss:where/gml:Point/gml:pos"];
                if(where.IsEmpty) {
                    return null;
                }
                string[] coords = where.Contents.Split(new char[] { ' ', ',' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if(coords.Length != 2) {
                    return null;
                }
                double x;
                double y;
                if(!double.TryParse(coords[0], out x) || !double.TryParse(coords[1], out y)) {
                    return null;
                }
                return new Tuplet<double, double>(x, y);
            }
            set {
                this["georss:where"].Remove();
                if(value != null) {
                    Root.Start("georss:where").Start("gml:Point").Elem("gml:pos", string.Format("{0} {1}", value.Item1, value.Item2)).End().End();
                }
            }
        }

        //--- Methods ---
        
        /// <summary>
        /// Add content to the entry.
        /// </summary>
        /// <param name="text">Text body to add.</param>
        /// <returns>Returns the current document instance.</returns>
        public XAtomEntry AddContent(string text) {
            AddText("content", "text", text);
            return this;
        }

        /// <summary>
        /// Add a subdocument as content to the entry.
        /// </summary>
        /// <param name="mime">Mime type of the document to be added.</param>
        /// <param name="xml">Document to add.</param>
        /// <returns>Returns the current document instance.</returns>
        public XAtomEntry AddContent(MimeType mime, XDoc xml) {
            AddText("content", mime, xml);
            return this;
        }

        /// <summary>
        /// Add content to the entry.
        /// </summary>
        /// <param name="mime">Mime type of the content to be added.</param>
        /// <param name="data">Content as byte array.</param>
        /// <returns>Returns the current document instance.</returns>
        public XAtomEntry AddContent(MimeType mime, byte[] data) {
            AddText("content", mime, data);
            return this;
        }

        /// <summary>
        /// Add an entry summary.
        /// </summary>
        /// <param name="text">Summary text.</param>
        /// <returns>Returns the current document instance.</returns>
        public XAtomEntry AddSummary(string text) {
            AddText("summary", "text", text);
            return this;
        }

        /// <summary>
        /// Add an entry summary document.
        /// </summary>
        /// <param name="mime">Mime type of the summary document to be added.</param>
        /// <param name="xml">Summary document.</param>
        /// <returns>Returns the current document instance.</returns>
        public XAtomEntry AddSummary(MimeType mime, XDoc xml) {
            AddText("summary", mime, xml);
            return this;
        }
    }
}
