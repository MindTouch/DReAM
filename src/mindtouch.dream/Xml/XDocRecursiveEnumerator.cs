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
    internal class XDocRecursiveEnumerator : IEnumerator<XDoc> {

        //--- Fields ---
        private XmlNode _start;
        private XmlNamespaceManager _nsManager;
        private Predicate<XDoc> _enumerateChildren;
        private Action<XDoc> _nodeExitCallback;
        private XDoc _current;
        private bool _enumerateCurrentChildren;
        private Stack<XmlNode> _stack = new Stack<XmlNode>();
        private bool _disposed;

        //--- Constructors ---
        public XDocRecursiveEnumerator(XmlNode start, XmlNamespaceManager nsManager, Predicate<XDoc> enumerateChildren, Action<XDoc> nodeExitCallback) {
            _start = start;
            _nsManager = nsManager;
            _enumerateChildren = enumerateChildren;
            _nodeExitCallback = nodeExitCallback;
        }

        //--- Properties ---
        public XDoc Current {
            get {
                if(_disposed) {
                    throw new ObjectDisposedException("enumerator has already been disposed");
                }
                return _current;
            }
        }

        //--- Methods ---
        public void Dispose() {
            if(!_disposed) {
                _disposed = true;
                _nsManager = null;
                _enumerateChildren = null;
                _nodeExitCallback = null;
                _start = null;
                _stack = null;
            }
        }

        public bool MoveNext() {
            if(_disposed) {
                throw new ObjectDisposedException("enumerator has already been disposed");
            }

            // check for empty document case
            if(_start == null) {
                return false;
            }

            // check if current is initialized; if not, initialize it to the start node
            if(_current == null) {
                SetCurrent(_start);
                return true;
            }

            // check if current has children; if so, recurse into them
            var node = _current.AsXmlNode;
            if(_enumerateCurrentChildren && _current.AsXmlNode.HasChildNodes) {
                _stack.Push(node);
                SetCurrent(node.ChildNodes[0]);
                return true;
            }

            // check if we can move to the next node
            while(true) {
                if(_nodeExitCallback != null) {
                    _nodeExitCallback(new XDoc(new[] { node }, 0, _start, _nsManager));
                }
                if(node == _start) {
                    break;
                }
                var nextNode = node.NextSibling;
                if(nextNode != null) {
                    SetCurrent(nextNode);
                    return true;
                }
                if(_stack.Count == 0) {
                    break;
                }
                node = _stack.Pop();
            }

            // no more nodes to visit
            _current = null;
            return false;
        }

        private void SetCurrent(XmlNode node) {
            _current = new XDoc(new[] { node }, 0, _start, _nsManager);
            _enumerateCurrentChildren = ((_enumerateChildren == null) || _enumerateChildren(_current));
        }

        public void Reset() {
            if(_disposed) {
                throw new ObjectDisposedException("enumerator has already been disposed");
            }
            _current = null;
            _stack.Clear();
        }

        //--- IEnumerator Members ---
        object IEnumerator.Current { get { return Current; } }
    }
}
