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
using NUnit.Framework;

namespace MindTouch.Dream.PriorityQueue.Test {
    [TestFixture]
    public class PriorityQueueTests {

        [Test]
        public void Create() {
            PriorityQueue<int> p = new PriorityQueue<int>(delegate(int i, int j) { return i - j; });
            Random r = new Random();
            for(int i = 0; i < 10000; ++i) {
                p.Enqueue(r.Next(1000));
            }
        }

        [Test]
        public void Dequeue() {
            PriorityQueue<int> p = new PriorityQueue<int>(delegate(int i, int j) { return i - j; });
            Random r = new Random();
            for(int i = 0; i < 10000; ++i) {
                p.Enqueue(r.Next(1000));
            }

            int item = p.Dequeue();
            while(p.Count > 0) {
                int next = p.Dequeue();
                if(next < item) {
                    Assert.Fail("bad order detected");
                }
            }
        }

        [Test]
        public void Remove() {
            PriorityQueue<int> p = new PriorityQueue<int>(delegate(int i, int j) { return i - j; });
            Random r = new Random();
            for(int i = 0; i < 10000; ++i) {
                p.Enqueue(r.Next(1000));
            }

            int removed = 0;
            while(removed < 100) {
                int count = p.Count;
                p.Remove(r.Next(1000));
                if(count > p.Count) {
                    ++removed;
                }
            }

            int item = p.Dequeue();
            while(p.Count > 0) {
                int next = p.Dequeue();
                if(next < item) {
                    Assert.Fail("bad order detected");
                }
            }
        }
    }
}