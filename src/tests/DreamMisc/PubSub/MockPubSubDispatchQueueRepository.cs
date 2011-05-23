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
using MindTouch.Dream.Services.PubSub;
using MindTouch.Tasking;

namespace MindTouch.Dream.Test.PubSub {
    public class MockPubSubDispatchQueueRepository : IPubSubDispatchQueueRepository {
        public static int Instantiations;
        public static int InitCalled;
        public static int GetUninitializedSetsCalled;

        public MockPubSubDispatchQueueRepository() {
            Instantiations++;
        }

        #region Implementation of IPubSubDispatchQueueRepository
        public IEnumerable<PubSubSubscriptionSet> GetUninitializedSets() {
            GetUninitializedSetsCalled++;
            return new PubSubSubscriptionSet[0];
        }

        public void InitializeRepository(Func<DispatchItem, Result<bool>> dequeueHandler) {
            InitCalled++;
        }

        public void RegisterOrUpdate(PubSubSubscriptionSet set) {
            throw new NotImplementedException();
        }

        public void Delete(PubSubSubscriptionSet set) {
            throw new NotImplementedException();
        }

        public IPubSubDispatchQueue this[PubSubSubscriptionSet set] {
            get { throw new NotImplementedException(); }
        }
        #endregion

        #region Implementation of IDisposable
        public void Dispose() { }
        #endregion
    }
}
