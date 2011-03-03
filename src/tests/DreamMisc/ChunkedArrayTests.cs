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
using System.Linq;
using MindTouch.Collections;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class ChunkedArrayTests {

        [Test]
        public void ChunkedArray_creates_appropriate_number_of_chunks() {
            int size = 16 * 1024 * 4 + 100;
            int chunkCount = (int)Math.Ceiling((double)size * sizeof(int) / (16 * 1024));
            var array = new ChunkedArray<int>(size);
            Assert.AreEqual(chunkCount, array.ChunkCount);
        }

        [Test]
        public void ChunkedArray_reports_proper_length() {
            var length = 16 * 1024 * 4 + 100;
            var array = new ChunkedArray<int>(length);
            Assert.AreEqual(length, array.Length);
        }

        [Test]
        public void Can_get_expected_value_via_indexer() {
            var n = 16 * 1024 * 2 + 1800;
            var array = new ChunkedArray<int>(n);
            for(var i = 0; i < n; i++) {
                array[i] = 2 * i;
            }
            for(var i = 0; i < n; i++) {
                Assert.AreEqual(2 * i, array[i]);
            }
        }

        [Test]
        public void Can_enumerate_value_in_proper_order() {
            var n = 16 * 1024 * 2 + 1800;
            var array1 = new int[n];
            var array2 = new ChunkedArray<int>(n);
            for(var i = 0; i < n; i++) {
                array1[i] = i;
                array2[i] = i;
            }
            Assert.AreEqual(array1, array2.ToArray());
        }
    }
}
