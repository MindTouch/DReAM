/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.Test.Mock {
    internal class MockEndpoint : IPlugEndpoint {

        //--- Class Fields ---
        public static readonly MockEndpoint Instance = new MockEndpoint();

        // Note (arnec): This is a field, not constant so that access triggers the static constructor
        public readonly static string DEFAULT = "mock://mock";

        //--- Class Constructors ---
        static MockEndpoint() {
            Plug.AddEndpoint(Instance);
        }

        //--- Fields ---
        private readonly Dictionary<XUri, MockPlug.MockInvokeDelegate> _registry = new Dictionary<XUri, MockPlug.MockInvokeDelegate>();
        private readonly XUriMap<XUri> _map = new XUriMap<XUri>();

        //--- Events ---
        public event EventHandler AllDeregistered;

        //--- Constructors ---
        private MockEndpoint() { }
        //--- Methods ---
        public int GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            Tuplet<XUri, int> match = GetBestMatch(uri);
            normalized = uri;
            return match.Item2;
        }

        private Tuplet<XUri, int> GetBestMatch(XUri uri) {
            int result;
            XUri prefix;

            // using _registry as our guard for both _map and _registry, since they are always modified in sync
            lock(_registry) {
                _map.TryGetValue(uri, out prefix, out result);
            }
            if(prefix != null) {

                // always plus the score on a match, so that the mock gets first preference
                result = int.MaxValue;
            } else if(uri.SchemeHostPort.EqualsInvariant(DEFAULT)) {
                result = int.MaxValue;
            }
            return new Tuplet<XUri, int>(prefix, result);
        }

        public IEnumerator<IYield> Invoke(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
            MockPlug.MockInvokeDelegate callback;
            var match = GetBestMatch(uri);
            if(match.Item1 == null) {
                response.Return(DreamMessage.Ok(new XDoc("empty")));
                yield break;
            }
            lock(_registry) {
                callback = _registry[match.Item1];
            }
            yield return Async.Fork(() => callback(plug, verb, uri, request.Clone(), response), new Result(TimeSpan.MaxValue));
        }

        public void Register(XUri uri, MockPlug.MockInvokeDelegate invokeDelegate) {
            lock(_registry) {
                if(_registry.ContainsKey(uri)) {
                    throw new ArgumentException("the uri already has a mock registered");
                }
                _registry.Add(uri, invokeDelegate);
                _map.Add(uri, uri);
            }
        }

        public void Deregister(XUri uri) {
            lock(_registry) {
                if(!_registry.ContainsKey(uri)) {
                    return;
                }
                _registry.Remove(uri);
                _map.Remove(uri);
            }
        }

        public void DeregisterAll() {
            lock(_registry) {
                _registry.Clear();
                _map.Clear();
                if(AllDeregistered != null) {
                    AllDeregistered(this, EventArgs.Empty);
                }
                AllDeregistered = null;
            }
        }
    }
}