/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2013 MindTouch, Inc.
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
using System.IO;
using System.Threading;

using MindTouch.IO;
using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class DreamMiscTest {

        [Test]
        public void ExclusiveFileAccess() {

            // create the test file
            using(StreamWriter writer = new StreamWriter(File.OpenWrite("test.test"))) {
                writer.Write("Hello World!");
                writer.Close();
            }

            // kick off the thread to do the second open
            ManualResetEvent first = new ManualResetEvent(false);
            ManualResetEvent second = new ManualResetEvent(false);
            ManualResetEvent third = new ManualResetEvent(false);
            ManualResetEvent fourth = new ManualResetEvent(false);
            AsyncUtil.Fork(delegate() {
                first.WaitOne();
                try {
                    using(Stream f = StreamUtil.FileOpenExclusive("test.test")) {
                        Assert.IsNull(f, "file open succeeded when it was expected to fail");
                    }
                } catch {
                    third.Set();
                    fourth.Set();
                    throw;
                }
                second.WaitOne();
                third.Set();
                try {
                    using(Stream f = StreamUtil.FileOpenExclusive("test.test")) {
                        Assert.IsNotNull(f, "file open failed when it was expected to succeed");
                    }
                } catch {
                    fourth.Set();
                    throw;
                }
                fourth.Set();
            }, null);

            // open the file first
            using(Stream g = StreamUtil.FileOpenExclusive("test.test")) {
                first.Set();
                Thread.Sleep(2000);
                second.Set();
                Thread.Sleep(500);
                third.WaitOne();
                g.Close();
            }
            fourth.WaitOne();
            File.Delete("test.test");
        }

        [Test]
        public void ResourceUri() {
            Plug p = Plug.New("resource://test.mindtouch.dream/MindTouch.Dream.Test.Resources.resource-test.txt");
            string text = p.Get().ToText();
            Assert.AreNotEqual(string.Empty, text, "resource was empty");
        }

        [Test]
        public void PipeTest1() {
            Stream writer;
            Stream reader;
            StreamUtil.CreatePipe(out writer, out reader);
            byte[] write = new byte[] { 1, 2, 3, 4 };
            writer.Write(write, 0, write.Length);
            writer.Close();
            byte[] read = new byte[write.Length];
            int count = reader.Read(read, 0, read.Length);
            reader.Close();
            Assert.AreEqual(write.Length, count);
            Assert.AreEqual(write, read);
        }

        [Test]
        public void PipeTest2() {
            Stream writer;
            Stream reader;
            StreamUtil.CreatePipe(1, out writer, out reader);
            byte[] write = new byte[] { 1, 2, 3, 4 };
            AsyncUtil.Fork(delegate() {
                writer.Write(write, 0, write.Length);
                writer.Close();
            }, null);
            byte[] read = new byte[write.Length];
            int count = reader.Read(read, 0, read.Length);
            reader.Close();
            Assert.AreEqual(write.Length, count);
            Assert.AreEqual(write, read);
        }

        [Test]
        public void PipeTest3() {
            Stream writer;
            Stream reader;
            StreamUtil.CreatePipe(1, out writer, out reader);
            byte[] write = new byte[] { 1, 2, 3, 4 };
            AsyncUtil.Fork(delegate() {
                writer.Write(write, 0, write.Length);
                writer.Close();
            }, null);
            byte[] read = new byte[10];
            int count = reader.Read(read, 0, read.Length);
            reader.Close();
            Assert.AreEqual(write.Length, count);
            Assert.AreEqual(write, ArrayUtil.SubArray(read, 0, count));
        }

        [Ignore("xri stuff not working right")]
        [Test]
        public void XriTest() {
            DreamMessage result = Plug.New("xri://=roy").GetAsync().Wait();
            Assert.AreEqual("XRDS", result.ToDocument().Name);
        }
    }
}