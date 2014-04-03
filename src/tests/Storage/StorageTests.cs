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
using log4net.Config;

using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Dream.Storage.Test {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class StorageTest {

        //--- Constants ---
        public const string TEST_CONTENTS = "Sample content";
        public const string TEST_FILE_URI = "testfile";
        public const string TEST_SHARED_PATH = "shared";
        private const string TEST_PATH = "public-storage-proxy";
        private const string CROSS_TEST_PATH = "public-storage-crossover";

        //--- Class Fields ---
        private static readonly string[] _fileUri = new string[] { "testfolder", "testfile" };

        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private Plug _storage;
        private DreamHostInfo _hostInfo;
        private string _folder;
        private string _storageFolder;
        private Plug _testService;
        public static XUri _crossServiceUri;
        private Plug _testCrossService;

        [TestFixtureSetUp]
        public void Init() {
            BasicConfigurator.Configure();
            _folder = Path.GetTempPath();
            Directory.CreateDirectory(_folder);
            _storageFolder = Path.Combine(Path.GetTempPath(), StringUtil.CreateAlphaNumericKey(6));
            Directory.CreateDirectory(_storageFolder);

            XDoc config = new XDoc("config").Elem("service-dir", _folder);
            _hostInfo = DreamTestHelper.CreateRandomPortHost(config);
            CreateStorageService();
            CreateStorageServiceProxies();
        }

        [TestFixtureTearDown]
        public void GlobalCleanup() {
            LogManager.Shutdown();
            _hostInfo.Dispose();
        }

        [Test]
        public void Can_init() {
        }

        [Test]
        public void TestSendFile() {
            string filename = Path.GetTempFileName();
            using(Stream s = File.OpenWrite(filename)) {
                byte[] data = Encoding.UTF8.GetBytes(TEST_CONTENTS);
                s.Write(data, 0, data.Length);
            }

            // add a file
            _storage.At(_fileUri).Put(DreamMessage.FromFile(filename, false));
            File.Delete(filename);

            // get file and compare contents
            string contents = _storage.At(_fileUri).Get().ToText();
            Assert.AreEqual(TEST_CONTENTS, contents);

            // delete file
            _storage.At(_fileUri).Delete();
        }

        [Test]
        public void Can_store_and_retrieve_xml()
        {
            string file = "foo.xml";
            // add a file to it
            _storage.At(file).Put(new XDoc("foo").Elem("bar","baz"));

            // get file and compare contents
            XDoc doc = _storage.At(file).Get().ToDocument();
            Assert.AreEqual("baz", doc["bar"].AsText);

            // delete file
            _storage.At(file).Delete();
           
        }

        [Test]
        public void Can_store_files_at_service_root_level() {
            string file = "foo.txt";
            // add a file to it
            _storage.At(file).Put(DreamMessage.Ok(MimeType.TEXT, TEST_CONTENTS));

            // get file and compare contents
            string contents = _storage.At(file).Get().ToText();
            Assert.AreEqual(TEST_CONTENTS, contents);

            // delete file
            _storage.At(file).Delete();

        }

        [Test]
        public void Can_store_files_at_any_depth() {
            string file = "foo.txt";
            // add a file to it
            _storage.At("foo", file).Put(DreamMessage.Ok(MimeType.TEXT, TEST_CONTENTS));

            // get file and compare contents
            string contents = _storage.At("foo", file).Get().ToText();
            Assert.AreEqual(TEST_CONTENTS, contents);

            // delete file
            _storage.At("foo", file).Delete();

            // add a file to it
            _storage.At("foo", "bar", file).Put(DreamMessage.Ok(MimeType.TEXT, TEST_CONTENTS));

            // get file and compare contents
            contents = _storage.At("foo", "bar", file).Get().ToText();
            Assert.AreEqual(TEST_CONTENTS, contents);

            // delete file
            _storage.At("foo", "bar", file).Delete();

            // add a file to it
            _storage.At("foo", "bar", "baz", file).Put(DreamMessage.Ok(MimeType.TEXT, TEST_CONTENTS));

            // get file and compare contents
            contents = _storage.At("foo", "bar", "baz", file).Get().ToText();
            Assert.AreEqual(TEST_CONTENTS, contents);

            // delete file
            _storage.At("foo", "bar", "baz", file).Delete();
        }

        [Test]
        public void Delete_of_subdir_wipes_all_children() {
            string file = "foo.txt";
            // create some files
            _storage.At("foo", file).Put(DreamMessage.Ok(MimeType.TEXT, TEST_CONTENTS));
            _storage.At("foo", "bar", file).Put(DreamMessage.Ok(MimeType.TEXT, TEST_CONTENTS));
            _storage.At("foo", "bar", "baz", file).Put(DreamMessage.Ok(MimeType.TEXT, TEST_CONTENTS));

            XDoc files = _storage.At("foo", "bar").Get().ToDocument();
            Assert.AreEqual("baz", files["folder/name"].Contents);
            Assert.AreEqual("foo.txt", files["file/name"].Contents);

            files = _storage.At("foo").Get().ToDocument();
            Assert.AreEqual("bar", files["folder/name"].Contents);
            Assert.AreEqual("foo.txt", files["file/name"].Contents);

            _storage.At("foo", "bar").Delete();

            DreamMessage response = _storage.At("foo", "bar").GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.NotFound, response.Status);

            files = _storage.At("foo").Get().ToDocument();
            Assert.IsTrue(files["folder"].IsEmpty);
            Assert.AreEqual("foo.txt", files["file/name"].Contents);

            _storage.At("foo").Delete();
        }

        [Test]
        public void Delete_of_random_path_is_ok() {
            DreamMessage response = _storage.At("foo", "bar", "baz").DeleteAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Ok, response.Status);
        }

        [Test]
        public void Head_on_folder_is_ok() {
            DreamMessage response = _storage.HeadAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Ok, response.Status);
        }

        [Ignore("this needs to be revised")]
        [Test]
        [ExpectedException(typeof(DreamResponseException))]
        public void TestSendFolder_Fail() {
            string foldername = Path.GetTempPath();

            DreamMessage folderMsg = DreamMessage.FromFile(foldername, false);
            // add a file
            _storage.At(_fileUri).Put(folderMsg);

            // delete file (should never happen)
            _storage.At(_fileUri).Delete();
        }

        [Test]
        public void TestPutGetDelete() {

            // add a file to it
            _storage.At(_fileUri).Put(DreamMessage.Ok(MimeType.TEXT, TEST_CONTENTS));

            // get file and compare contents
            string contents = _storage.At(_fileUri).Get().ToText();
            Assert.AreEqual(TEST_CONTENTS, contents);

            // delete file
            _storage.At(_fileUri).Delete();
        }

        [Test]
        public void TestPutHeadDelete() {

            // add a file to it
            _storage.At(_fileUri).Put(DreamMessage.Ok(MimeType.TEXT, TEST_CONTENTS));

            // get file and compare contents
            DreamMessage response = _storage.At(_fileUri).Invoke(Verb.HEAD, DreamMessage.Ok());
            Assert.AreEqual(TEST_CONTENTS.Length, response.ContentLength);

            // delete file
            _storage.At(_fileUri).Delete();
        }

        [Test]
        public void TestPutGetTTL() {

            // add a file
            _storage.At(_fileUri).With("ttl", "2").Put(DreamMessage.Ok(MimeType.TEXT, TEST_CONTENTS));
            _log.DebugFormat("File stored at: {0}", DateTime.UtcNow);

            // get file and compare contents
            string contents = _storage.At(_fileUri).Get().ToText();
            Assert.AreEqual(TEST_CONTENTS, contents);
            _log.DebugFormat("check file at: {0}", DateTime.UtcNow);
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(4));

            // get file and compare contents
            _log.DebugFormat("Checking for expired file at: {0}", DateTime.UtcNow);
            DreamMessage response = _storage.At(_fileUri).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.NotFound, response.Status);
        }

        [Ignore("issues with service deletion")]
        [Test]
        public void TestPutRestartGetTTL() {

            // add a file to it
            _storage.At(_fileUri).With("ttl", "5").Put(DreamMessage.Ok(MimeType.TEXT, TEST_CONTENTS));

            // get file and compare contents
            string contents = _storage.At(_fileUri).Get().ToText();
            Assert.AreEqual(TEST_CONTENTS, contents);

            // destroy storage service
            DestroyStorageService();
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(6));

            // re-create storage service
            CreateStorageService();

            // get file and compare contents
            contents = _storage.At(_fileUri).Get().ToText();
            Assert.AreEqual(TEST_CONTENTS, contents);

            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));

            // get file and compare contents
            DreamMessage response = _storage.At(_fileUri).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.NotFound, response.Status);
        }

        [Test]
        public void Access_to_host_shared_private_service_should_be_forbidden() {
            DreamMessage response = _hostInfo.LocalHost.At("host", "$store").GetAsync().Wait();
            Assert.IsFalse(response.IsSuccessful, response.ToText());
        }

        [Test]
        public void Service_can_store_and_retrieve_file() {
            DreamMessage response = _testService.AtPath("create-retrieve-delete").PostAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
        }

        [Test]
        public void Service_can_store_and_retrieve_head() {
            DreamMessage response = _testService.AtPath("create-retrievehead-delete").PostAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
        }

        [Test]
        public void Service_storage_will_expire_file() {
            DreamMessage response = _testService.AtPath("create-expire").PostAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
        }

        [Test]
        public void Service_can_store_and_retrieve_file_from_another_services_shared_private_storage() {
            DreamMessage response = _testCrossService.AtPath("create-retrieve-delete").PostAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
        }

        [Test]
        public void Services_can_manipulate_shared_private_storage() {
            DreamMessage response = _testService.AtPath("shared-create").PostAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            response = _testCrossService.AtPath("shared-retrieve-delete").PostAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());
        }

        [TearDown]
        public void DeinitTest() {
            System.GC.Collect();
        }

        private void CreateStorageService() {

            // create storage service
            XDoc config = new XDoc("config");
            config.Elem("path", "storage");
            config.Elem("sid", "sid://mindtouch.com/2007/03/dream/storage");
            config.Elem("folder", _storageFolder);
            //DreamMessage result = _host.Self.At("services").PostAsync(config).Wait();          
            DreamMessage result = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).PostAsync(config).Wait();
            Assert.IsTrue(result.IsSuccessful, result.ToText());

            // initialize storage plug
            _storage = _hostInfo.LocalHost.At("storage");
        }

        private void DestroyStorageService() {
            DreamMessage response = _storage.DeleteAsync().Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToText());

        }

        private void CreateStorageServiceProxies() {
            _hostInfo.Host.Self.At("load").With("name", "test.mindtouch.storage").Post(DreamMessage.Ok());
            _hostInfo.Host.Self.At("services").Post(
                new XDoc("config")
                    .Elem("class", typeof(TestServiceWithPublicStorage).FullName)
                    .Elem("path", TEST_PATH));
            _testService = Plug.New(_hostInfo.Host.LocalMachineUri).At(TEST_PATH);
            _hostInfo.Host.Self.At("services").Post(
                new XDoc("config")
                    .Elem("class", typeof(TestCrossServiceStorageAccessor).FullName)
                    .Elem("path", CROSS_TEST_PATH));
            _testCrossService = Plug.New(_hostInfo.Host.LocalMachineUri).At(CROSS_TEST_PATH);
        }

        [DreamService("TestServiceWithPublicStorage", "Copyright (c) 2008 MindTouch, Inc.",
            Info = "",
            SID = new string[] { "sid://mindtouch.com/TestServiceWithPublicStorage" }
            )]
        public class TestServiceWithPublicStorage : DreamService {

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
                _log.DebugFormat("storage path: {0}", Storage.Uri);
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
                DreamMessage getResponse = Storage.AtPath(TEST_FILE_URI).GetAsync().Wait();
                Assert.AreEqual(DreamStatus.NotFound, getResponse.Status);

                response.Return(DreamMessage.Ok());
                yield break;
            }

            [DreamFeature("POST:shared-create", "Create and retrieve test")]
            public Yield TestCreateForCrossService(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                string filename = Path.GetTempFileName();
                using(Stream s = File.OpenWrite(filename)) {
                    byte[] data = Encoding.UTF8.GetBytes(TEST_CONTENTS);
                    s.Write(data, 0, data.Length);
                }
                _log.Debug("created file");

                // derive shared storage path
                Plug sharedStorage = Plug.New(Storage.Uri.WithoutLastSegment().At(TEST_SHARED_PATH));
                _log.DebugFormat("shared storage: {0}", sharedStorage.Uri);

                // add a file
                sharedStorage.AtPath(TEST_FILE_URI).Put(DreamMessage.FromFile(filename, false));
                File.Delete(filename);
                _log.Debug("put file");

                // get file and compare contents
                string contents = sharedStorage.AtPath(TEST_FILE_URI).Get().ToText();
                Assert.AreEqual(TEST_CONTENTS, contents);
                _log.Debug("got file");
                response.Return(DreamMessage.Ok());
                yield break;
            }

            protected override Yield Start(XDoc config, Result result) {
                yield return Coroutine.Invoke(base.Start, config, new Result());
                _crossServiceUri = Storage.Uri;
                result.Return();
            }
        }

        [DreamService("TestCrossServiceStorageAccessor", "Copyright (c) 2008 MindTouch, Inc.",
            Info = "",
            SID = new string[] { 
                                   "sid://mindtouch.com/TestCrossServiceStorageAccessor"
                               }
            )]
        public class TestCrossServiceStorageAccessor : DreamService {

            //--- Class Fields ---
            private static readonly ILog _log = LogUtils.CreateLog();

            [DreamFeature("POST:shared-retrieve-delete", "Create and retrieve test")]
            public Yield TestSharedRetrieveDelete(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                string filename = Path.GetTempFileName();
                using(Stream s = File.OpenWrite(filename)) {
                    byte[] data = Encoding.UTF8.GetBytes(TEST_CONTENTS);
                    s.Write(data, 0, data.Length);
                }
                _log.Debug("created file");

                // derive shared storage path
                Plug sharedStorage = Plug.New(Storage.Uri.WithoutLastSegment().At(TEST_SHARED_PATH));
                _log.DebugFormat("shared storage: {0}", sharedStorage.Uri);

                // get file and compare contents
                string contents = sharedStorage.AtPath(TEST_FILE_URI).Get().ToText();
                Assert.AreEqual(TEST_CONTENTS, contents);
                _log.Debug("got file");

                // delete file
                sharedStorage.AtPath(TEST_FILE_URI).Delete();
                _log.Debug("deleted file");
                response.Return(DreamMessage.Ok());
                yield break;
            }

            [DreamFeature("POST:create-retrieve-delete", "Create and retrieve test")]
            public Yield TestCreateRetrieveDelete(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                string filename = Path.GetTempFileName();
                using(Stream s = File.OpenWrite(filename)) {
                    byte[] data = Encoding.UTF8.GetBytes(TEST_CONTENTS);
                    s.Write(data, 0, data.Length);
                }
                _log.Debug("created file");

                // add a file
                Plug cross = Plug.New(_crossServiceUri);
                _log.DebugFormat("cross service path storage path: {0}", cross.Uri);
                cross.AtPath(TEST_FILE_URI).Put(DreamMessage.FromFile(filename, false));
                File.Delete(filename);
                _log.Debug("put file");

                // get file and compare contents
                string contents = cross.AtPath(TEST_FILE_URI).Get().ToText();
                Assert.AreEqual(TEST_CONTENTS, contents);
                _log.Debug("got file");

                // delete file
                cross.AtPath(TEST_FILE_URI).Delete();
                _log.Debug("deleted file");
                response.Return(DreamMessage.Ok());
                yield break;
            }

        }
    }
}