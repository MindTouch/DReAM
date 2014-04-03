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

using MindTouch.IO;
using Moq;
using NUnit.Framework;

namespace MindTouch.Dream.Test.TransactionalQueue {
    
    [TestFixture]
    public class QueueStreamValueTests {

        [Test]
        public void Can_use_equality_operator_to_compare_Empties() {
            var a = QueueStreamRecord.Empty;
            Assert.IsTrue(QueueStreamRecord.Empty == a);
        }

        [Test]
        public void Can_use_equality_operator_to_make_sure_value_is_not_Empty() {
            var a = new QueueStreamRecord(null,new Mock<IQueueStreamHandle>().Object);
            Assert.IsFalse(QueueStreamRecord.Empty == a);
        }

        [Test]
        public void Creating_instance_from_by_assigning_from_another_retains_equality() {
            var a = new QueueStreamRecord(null, new Mock<IQueueStreamHandle>().Object);
            var b = a;
            Assert.IsTrue(a.Equals(b));

        }

        [Test]
        public void Hashcodes_of_different_instances_with_same_content_match() {
            var handle = new Mock<IQueueStreamHandle>().Object;
            var a = new QueueStreamRecord(null, handle);
            var b = new QueueStreamRecord(null, handle);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void Different_instances_with_same_content_are_equal() {
            var handle = new Mock<IQueueStreamHandle>().Object;
            var a = new QueueStreamRecord(null, handle);
            var b = new QueueStreamRecord(null, handle);
            Assert.AreEqual(a,b);
        }
    }
}
