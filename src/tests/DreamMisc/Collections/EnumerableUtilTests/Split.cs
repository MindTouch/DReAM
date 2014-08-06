/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
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
using System.Linq;
using NUnit.Framework;

namespace MindTouch.Dream.Test.Collections.EnumerableUtilTests {
    
    [TestFixture]
    public class Split {

        [Test]
        public void Split_generates_chunks_of_specified_length() {
            
            // Arrange
            var array = Enumerable.Range(0, 1000);

            // Act
            var chunks = array.Split(100);

            // Assert
            foreach(var c in chunks) {
                Assert.AreEqual(100, c.Count(), "Chunk size is incorrect");
            }
        }

        [Test]
        public void Split_can_handle_one_element() {

            // Arrange
            var array = new[] { 1 };

            // Act
            var chunks = array.Split(100);

            // Assert
            Assert.AreEqual(1, chunks.Count(), "There should only be one chunk");
            Assert.AreEqual(1, chunks.First().Count(), "Chunk should only contain one element");
        }
    }
}
