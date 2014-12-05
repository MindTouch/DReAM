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
using System.IO;
using System.Text;
using log4net;

using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Dream.Storage.Test {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class PrivateStorageTests {
        //--- Constants ---
        private const string TEST_PATH = "private-storage-proxy";

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private Plug testService;
        private DreamHostInfo _hostInfo;
        private string _folder;
        private string _storageFolder;

        [TestFixtureSetUp]
        public void Init() {
            _folder = Path.GetTempPath();
            Directory.CreateDirectory(_folder);
            _storageFolder = Path.Combine(Path.GetTempPath(), StringUtil.CreateAlphaNumericKey(6));
            Directory.CreateDirectory(_storageFolder);
            XDoc config = new XDoc("config").Elem("service-dir", _folder);
            _hostInfo = DreamTestHelper.CreateRandomPortHost(config);
            CreatePrivateStorageServiceProxy();
        }

        [TearDown]
        public void DeinitTest() {
            System.GC.Collect();
        }

        [TestFixtureTearDown]
        public void TearDown() {
            _hostInfo.Dispose();
            Directory.Delete(_storageFolder, true);
        }

        [Test]
        public void Can_init() {
        }

        [Test]
        public void Can_create_two_services_with_private_storage() {
            _log.Debug("start test");
            CreateSecondPrivateStorageServiceProxy();
        }

        [Test]
        public void Service_can_store_and_retrieve_file() {
            DreamMessage response = testService.AtPath("create-retrieve-delete").Post(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
        }

        [Test]
        public void Service_can_store_and_retrieve_head() {
            DreamMessage response = testService.AtPath("create-retrievehead-delete").Post(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
        }

        [Test]
        public void Service_storage_will_expire_file() {
            DreamMessage response = testService.AtPath("create-expire").Post(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
        }

        private void CreatePrivateStorageServiceProxy() {
            _hostInfo.Host.Self.At("load").With("name", "test.mindtouch.storage").Post(DreamMessage.Ok());
            _hostInfo.Host.Self.At("services").Post(
                new XDoc("config")
                    .Elem("class", typeof(TestServiceWithPrivateStorage).FullName)
                    .Elem("path", TEST_PATH));
            testService = Plug.New(_hostInfo.Host.LocalMachineUri).At(TEST_PATH);
        }

        private void CreateSecondPrivateStorageServiceProxy() {
            _hostInfo.Host.Self.At("services").Post(
                new XDoc("config")
                    .Elem("class", typeof(TestServiceWithPrivateStorage).FullName)
                    .Elem("path", TEST_PATH + "2"));
            _log.Debug("created second storage service");
            testService = Plug.New(_hostInfo.Host.LocalMachineUri).At(TEST_PATH + "2");
        }
    }

    [DreamService("TestServiceWithPrivateStorage", "Copyright (c) 2008 MindTouch, Inc.",
        Info = "",
        SID = new[] { "sid://mindtouch.com/TestServiceWithPrivateStorage" }
    )]
    [DreamServiceBlueprint("setup/private-storage")]
    public class TestServiceWithPrivateStorage : DreamService {

        //--- Constants ---
        private const string TEST_CONTENTS = "Sample content";
        private const string TEST_FILE_URI = "testfile";

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        [DreamFeature("POST:create-retrieve-delete", "Create and retrieve test")]
        public Yield TestCreateRetrieveDelete(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string filename = Path.GetTempFileName();
            using(Stream s = File.OpenWrite(filename)) {
                byte[] data = Encoding.UTF8.GetBytes(TEST_CONTENTS);
                s.Write(data, 0, data.Length);
            }
            _log.Debug("created file");

            // add a file
            Storage.AtPath(TEST_FILE_URI).Put(DreamMessage.FromFile(filename, false));
            File.Delete(filename);
            _log.Debug("put file");

            // get file and compare contents
            string contents = Storage.AtPath(TEST_FILE_URI).Get().ToText();
            Assert.AreEqual(TEST_CONTENTS, contents);
            _log.Debug("got file");

            // delete file
            Storage.AtPath(TEST_FILE_URI).Delete();
            _log.Debug("deleted file");
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("POST:create-retrievehead-delete", "Create and retrieve head test")]
        public Yield TestCreateRetrieveHeadDelete(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string filename = Path.GetTempFileName();
            using(Stream s = File.OpenWrite(filename)) {
                byte[] data = Encoding.UTF8.GetBytes(TEST_CONTENTS);
                s.Write(data, 0, data.Length);
            }
            _log.Debug("created file");

            // add a file
            Storage.AtPath(TEST_FILE_URI).Put(DreamMessage.FromFile(filename, false));
            File.Delete(filename);
            _log.Debug("put file");

            // get file and compare contents
            DreamMessage headResponse = Storage.AtPath(TEST_FILE_URI).Invoke(Verb.HEAD, DreamMessage.Ok());
            Assert.AreEqual(TEST_CONTENTS.Length, headResponse.ContentLength);
            _log.Debug("got content length");

            // delete file
            Storage.AtPath(TEST_FILE_URI).Delete();
            _log.Debug("deleted file");
            response.Return(DreamMessage.Ok());
            yield break;
        }
        [DreamFeature("POST:create-expire", "Create and expire test")]
        public Yield TestCreateTtlExpire(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            // add a file
            Storage.AtPath(TEST_FILE_URI).With("ttl", "2").Put(DreamMessage.Ok(MimeType.TEXT, TEST_CONTENTS));
            _log.DebugFormat("File stored at: {0}", DateTime.UtcNow);

            // get file and compare contents
            string contents = Storage.AtPath(TEST_FILE_URI).Get().ToText();
            Assert.AreEqual(TEST_CONTENTS, contents);
            _log.DebugFormat("check file at: {0}", DateTime.UtcNow);
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(4));

            // get file and compare contents
            _log.DebugFormat("Checking for expired file at: {0}", DateTime.UtcNow);
            DreamMessage getResponse = Storage.AtPath(TEST_FILE_URI).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.NotFound, getResponse.Status);

            response.Return(DreamMessage.Ok());
            yield break;
        }

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            result.Return();
        }
    }
}