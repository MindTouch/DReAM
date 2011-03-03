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
using System.Xml;

namespace MindTouch.Xml {
    internal class XDocRecursiveEnumerable : IEnumerable<XDoc> {

        //--- Fields ---
        private readonly XmlNode _start;
        private readonly XmlNamespaceManager _nsManager;
        private readonly Predicate<XDoc> _enumerateChildren;
        private readonly Action<XDoc> _nodeExitCallback;

        //--- Constructors ---
        public XDocRecursiveEnumerable(XmlNode start, XmlNamespaceManager nsManager, Predicate<XDoc> enumerateChildren, Action<XDoc> nodeExitCallback) {
            _start = start;
            _nsManager = nsManager;
            _enumerateChildren = enumerateChildren;
            _nodeExitCallback = nodeExitCallback;
        }

        //-- Methods ---
        public IEnumerator<XDoc> GetEnumerator() {
            return new XDocRecursiveEnumerator(_start, _nsManager, _enumerateChildren, _nodeExitCallback);
        }

        //--- IEnumerable Members ---
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}