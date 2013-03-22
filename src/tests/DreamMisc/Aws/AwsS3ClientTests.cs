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
using MindTouch.Aws;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;
using MindTouch.Extensions.Time;

namespace MindTouch.Dream.Test.Aws {
    [TestFixture]
    public class AwsS3ClientTests {
        private AwsS3ClientConfig _config;
        private AwsS3Client _client;

        [SetUp]
        public void Setup() {
            _config = new AwsS3ClientConfig {
                Endpoint = AwsTestHelpers.AWS,
                Bucket = "bucket",
                Delimiter = "/",
                RootPath = "root/path",
                PrivateKey = "private",
                PublicKey = "public",
                Timeout = TimeSpan.FromSeconds(30)
            };
            _client = new AwsS3Client(_config, TaskTimerFactory.Current);
            MockPlug.DeregisterAll();
        }

        [Test]
        public void Can_put_file() {
            var data = AwsTestHelpers.CreateRandomDocument();
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("PUT")
                .At(_config.RootedPath("foo", "bar"))
                .WithBody(data)
                .Returns(DreamMessage.Ok())
                .ExpectAtLeastOneCall();
            _client.PutFile("foo/bar", AwsTestHelpers.CreateFileHandle(data, null));
            MockPlug.VerifyAll();
        }

        [Test]
        public void Can_put_file_with_expiration() {
            var data = AwsTestHelpers.CreateRandomDocument();
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("PUT")
                .At(_config.RootedPath("foo", "bar"))
                .WithHeader("x-amz-meta-ttl", 1.ToString())
                .WithBody(data)
                .Returns(DreamMessage.Ok())
                .ExpectAtLeastOneCall();
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("DELETE")
                .At(_config.RootedPath("foo", "bar"))
                .Returns(new DreamMessage(DreamStatus.NoContent, null))
                .ExpectAtLeastOneCall();
            _client.PutFile("foo/bar", AwsTestHelpers.CreateFileHandle(data, 1.Seconds()));
            MockPlug.VerifyAll(10.Seconds());
        }

        [Test]
        public void Put_at_directory_path_throws() {
            var data = AwsTestHelpers.CreateRandomDocument();
            try {
                _client.PutFile("foo/bar/", AwsTestHelpers.CreateFileHandle(data, null));
                Assert.Fail("didn't throw");
            } catch(InvalidOperationException) { }
        }

        [Test]
        public void Can_delete_file() {
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("DELETE")
                .At(_config.RootedPath("foo", "bar"))
                .Returns(new DreamMessage(DreamStatus.NoContent, null))
                .ExpectAtLeastOneCall();
            _client.Delete("foo/bar");
        }

        [Test]
        public void Can_delete_directory() {
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("GET")
                .At(_config.Bucket)
                .With("prefix", "root/path/foo/bar/")
                .Returns(DreamMessage.Ok(new XDoc("ListBucketResult", "http://s3.amazonaws.com/doc/2006-03-01/")
                                             .Elem("NextMarker", "root/path/x")
                                             .Start("Contents")
                                             .Elem("Key", "root/path/a/b")
                                             .End()))
                .ExpectAtLeastOneCall();
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("GET")
                .At(_config.Bucket)
                .With("prefix", "root/path/foo/bar/")
                .With("marker", "root/path/x")
                .Returns(DreamMessage.Ok(new XDoc("ListBucketResult", "http://s3.amazonaws.com/doc/2006-03-01/")
                                             .Start("Contents")
                                             .Elem("Key", "root/path/x")
                                             .End()))
                .ExpectAtLeastOneCall();
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("DELETE")
                .At(_config.RootedPath("a", "b"))
                .Returns(new DreamMessage(DreamStatus.NoContent, null))
                .ExpectAtLeastOneCall();
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("DELETE")
                .At(_config.RootedPath("x"))
                .Returns(new DreamMessage(DreamStatus.NoContent, null))
                .ExpectAtLeastOneCall();
            _client.Delete("foo/bar/");
            MockPlug.VerifyAll();
        }

        [Test]
        public void Can_read_file() {
            var data = AwsTestHelpers.CreateRandomDocument();
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("GET")
                .At(_config.RootedPath("foo", "bar"))
                .Returns(DreamMessage.Ok(data))
                .ExpectAtLeastOneCall();
            var response = _client.GetDataInfo("foo/bar", false);
            MockPlug.VerifyAll();
            Assert.IsFalse(response.IsDirectory);
            var fileinfo = response.AsFileHandle();
            Assert.AreEqual(MimeType.XML.ToString(), fileinfo.MimeType.ToString());
            var data2 = XDocFactory.From(fileinfo.Stream, fileinfo.MimeType);
            Assert.AreEqual(data, data2);
        }

        [Test]
        public void Read_file_with_lazy_expiration_returns_null() {
            var data = AwsTestHelpers.CreateRandomDocument();
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("GET")
                .At(_config.RootedPath("foo", "bar"))
                .Returns(invocation => {
                    var msg = DreamMessage.Ok(data);
                    msg.Headers["x-amz-meta-expire"] = DateTime.UtcNow.Subtract(10.Minutes()).ToEpoch().ToString();
                    msg.Headers["x-amz-meta-ttl"] = "10";
                    return msg;
                })
                .ExpectAtLeastOneCall();
            Assert.IsNull(_client.GetDataInfo("foo/bar", false));
            MockPlug.VerifyAll();
        }

        [Test]
        public void Read_nonexistent_file_returns_null() {
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("GET")
                .At(_config.RootedPath("foo", "bar"))
                .Returns(DreamMessage.NotFound("nada"))
                .ExpectAtLeastOneCall();
            Assert.IsNull(_client.GetDataInfo("foo/bar", false));
            MockPlug.VerifyAll();
        }

        [Test]
        public void Can_read_directory() {
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("GET")
                .At(_config.Bucket)
                .With("prefix", "root/path/foo/bar/")
                .Returns(DreamMessage.Ok(new XDoc("ListBucketResult", "http://s3.amazonaws.com/doc/2006-03-01/")
                                             .Elem("NextMarker", "root/path/foo/bar/b")
                                             .Start("Contents")
                                             .Elem("Key", "root/path/foo/bar/a")
                                             .End()))
                .ExpectAtLeastOneCall();
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("GET")
                .At(_config.Bucket)
                .With("prefix", "root/path/foo/bar/")
                .With("marker", "root/path/foo/bar/b")
                .Returns(DreamMessage.Ok(new XDoc("ListBucketResult", "http://s3.amazonaws.com/doc/2006-03-01/")
                                             .Start("CommonPrefixes")
                                             .Elem("Prefix", "root/path/foo/bar/b")
                                             .End()))
                .ExpectAtLeastOneCall();
            var response = _client.GetDataInfo("foo/bar/", false);
            MockPlug.VerifyAll();
            Assert.IsTrue(response.IsDirectory);
            var dir = response.AsDirectoryDocument();
            var fileInfo = dir["file"];
            Assert.AreEqual(1, fileInfo.ListLength);
            Assert.AreEqual("a", fileInfo["name"].AsText);
            var dirInfo = dir["folder"];
            Assert.AreEqual(1, dirInfo.ListLength);
            Assert.AreEqual("b", dirInfo["name"].AsText);
        }

        [Test]
        public void Read_nonexistent_directory_returns_null() {
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("GET")
                .At(_config.Bucket)
                .With("delimiter", "/")
                .With("prefix", "root/path/foo/bar/")
                .Returns(DreamMessage.Ok(new XDoc("ListBucketResult", "http://s3.amazonaws.com/doc/2006-03-01/")))
                .ExpectAtLeastOneCall();
            Assert.IsNull(_client.GetDataInfo("foo/bar/", false));
            MockPlug.VerifyAll();
        }
    }
}