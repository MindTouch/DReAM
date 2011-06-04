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
using MindTouch.Dream.Services.PubSub;
using MindTouch.Xml;

namespace MindTouch.Dream.Test.PubSub {
    public class MockDispatcher : IPubSubDispatcher {

        public static int Instantiations;

        public MockDispatcher() {
            Instantiations++;
        }

        public PubSubSubscriptionSet CombinedSet {
            get { throw new NotImplementedException(); }
        }

        public PubSubSubscriptionSet this[string location] {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<PubSubSubscriptionSet> GetAllSubscriptionSets() {
            throw new NotImplementedException();
        }

        public Tuplet<PubSubSubscriptionSet, bool> RegisterSet(string location, XDoc setDoc, string accessKey) {
            throw new NotImplementedException();
        }

        public void Dispatch(DispatcherEvent ev) {
            throw new NotImplementedException();
        }

        public PubSubSubscriptionSet ReplaceSet(string location, XDoc setDoc, string accessKey) {
            throw new NotImplementedException();
        }

        public bool RemoveSet(string location) {
            throw new NotImplementedException();
        }

        public event EventHandler<EventArgs> CombinedSetUpdated;
    }
}