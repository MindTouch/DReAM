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
using System.IO;
using System.Linq;
using System.Text;
using MindTouch.Dream.Services.PubSub;
using MindTouch.Tasking;
using NUnit.Framework;
using MindTouch.Extensions.Time;

namespace MindTouch.Dream.Test.PubSub {
    
    [TestFixture]
    public class PersistentPubSubDispatchQueueRespositoryTests {
        private string _queuePath;
        private PersistentPubSubDispatchQueueRepository _repository;

        [SetUp]
        public void Setup() {
            _queuePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _repository = new PersistentPubSubDispatchQueueRepository(_queuePath, TaskTimerFactory.Current, 10.Seconds());
        }

        [Test]
        public void Repository_creates_queue_path() {
            Assert.IsTrue(Directory.Exists(_queuePath),"queue path did not exist");
        }

        [Test]
        public void Respository_pulls_existing_subscriptions_from_disk() {
            Assert.Fail();
        }

        [Test]
        public void Sets_returned_by_initialize_use_provided_dequeue_handler() {
            Assert.Fail();
        }

        [Test]
        public void Unknown_set_returns_null_as_its_dispatch_queue() {
            Assert.Fail();
        }

        [Test]
        public void Can_register_a_new_set_and_retrieve_it() {
            Assert.Fail();
        }

        [Test]
        public void Registered_set_uses_dequeue_handler_from_init() {
            Assert.Fail();
        }

        [Test]
        public void Registering_a_set_persists_it_to_disk() {
            Assert.Fail();
        }

        [Test]
        public void Can_updated_set() {
            Assert.Fail();
        }

        [Test]
        public void Deleting_set_removes_it_from_repository() {
            Assert.Fail();
        }

        [Test]
        public void Deleting_set_removes_it_from_disk() {
            Assert.Fail();
        }
    }
}
