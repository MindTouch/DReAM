/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
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
        private static readonly MockPlug.IMockInvokee DefaultInvokee = new MockPlug.MockInvokee(null, (p, v, u, r, response) => response.Return(DreamMessage.Ok(new XDoc("empty"))), int.MaxValue);

        //--- Class Constructors ---
        static MockEndpoint() {
            Plug.AddEndpoint(Instance);
        }

        //--- Fields ---
        private readonly Dictionary<XUri, MockPlug.IMockInvokee> _registry = new Dictionary<XUri, MockPlug.IMockInvokee>();
        private readonly XUriMap<MockPlug.IMockInvokee> _map = new XUriMap<MockPlug.IMockInvokee>();

        //--- Events ---
        public event EventHandler AllDeregistered;

        //--- Constructors ---
        private MockEndpoint() { }

        //--- Methods ---
        public int GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            var match = GetBestMatch(uri);
            normalized = uri;
            return match == null ? 0 : match.EndPointScore;
        }

        private MockPlug.IMockInvokee GetBestMatch(XUri uri) {
            MockPlug.IMockInvokee invokee;

            // using _registry as our guard for both _map and _registry, since they are always modified in sync
            lock(_registry) {
                int result;
                _map.TryGetValue(uri, out invokee, out result);
            }
            if(invokee != null) {
                return invokee;
            }
            return uri.SchemeHostPort.EqualsInvariant(DEFAULT) ? DefaultInvokee : null;
        }

        public IEnumerator<IYield> Invoke(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
            var match = GetBestMatch(uri);
            yield return AsyncUtil.Fork(() => match.Invoke(plug, verb, uri,  MemorizeAndClone(request), response), new Result(TimeSpan.MaxValue));
        }

        public void Register(MockPlug.IMockInvokee invokee) {
            lock(_registry) {
                if(_registry.ContainsKey(invokee.Uri)) {
                    throw new ArgumentException("the uri already has a mock registered");
                }
                _registry.Add(invokee.Uri, invokee);
                _map.Add(invokee.Uri, invokee);
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

        private DreamMessage MemorizeAndClone(DreamMessage request) {
            return request.IsCloneable ? request.Clone() : new DreamMessage(request.Status,request.Headers,request.ContentType,request.ToBytes());
        }
    }
}
