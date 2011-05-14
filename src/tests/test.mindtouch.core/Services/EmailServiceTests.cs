/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
using System.Net.Mail;
using Autofac.Builder;
using log4net;
using MindTouch.Dream;
using MindTouch.Dream.Services;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Core.Test.Services {
    
    [TestFixture]
    public class EmailServiceTests {

        //--- Types ---
        public class SmtpClientFactoryMock : ISmtpClientFactory {
            public SmtplClientMock Client = new SmtplClientMock();
            public SmtpSettings Settings;

            public ISmtpClient CreateClient(SmtpSettings settings) {
                Settings = settings;
                return Client;
            }
        }

        public class SmtplClientMock : ISmtpClient {
            public MailMessage Message;
            public void Send(MailMessage message) {
                Message = message;
            }
        }

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private DreamHostInfo _hostInfo;
        private DreamServiceInfo _emailService;
        private Plug _plug;
        private SmtpClientFactoryMock _smtpClientFactory;

        //--- Methods ---

        [TestFixtureSetUp]
        public void GlobalSetup() {
            var config = new XDoc("config");
            var builder = new ContainerBuilder();
            _smtpClientFactory = new SmtpClientFactoryMock();
            builder.Register(c => _smtpClientFactory).As<ISmtpClientFactory>().ServiceScoped();
            _hostInfo = DreamTestHelper.CreateRandomPortHost(config, builder.Build());
            _emailService = DreamTestHelper.CreateService(
                _hostInfo, 
                "sid://mindtouch.com/2009/01/dream/email", 
                "email", 
                new XDoc("config").Elem("apikey", "servicekey"));
            _plug = _emailService.WithInternalKey().AtLocalHost;
        }

        [TestFixtureTearDown]
        public void GlobalTeardown() {
            _hostInfo.Dispose();
        }

        [SetUp]
        public void Setup() {
            _smtpClientFactory.Client = new SmtplClientMock();
            _smtpClientFactory.Settings = null;
        }

        [Test]
        public void Can_send_email_with_default_settings() {
            var email = new XDoc("email")
                .Attr("configuration", "default")
                .Elem("to", "to@bar.com")
                .Elem("from", "from@bar.com")
                .Elem("subject", "subject")
                .Elem("body", "body");
            _log.Debug("sending message");
            var response = _plug.At("message").Post(email, new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DEFAULT_HOST, _smtpClientFactory.Settings.Host);
            Assert.AreEqual("\"from@bar.com\" <from@bar.com>", _smtpClientFactory.Client.Message.From.ToString());
            Assert.AreEqual("\"to@bar.com\" <to@bar.com>", _smtpClientFactory.Client.Message.To.First().ToString());
            Assert.AreEqual("subject", _smtpClientFactory.Client.Message.Subject);
            Assert.AreEqual("body", _smtpClientFactory.Client.Message.Body);
        }

        [Test]
        public void Can_set_custom_settings() {
            var settings = new XDoc("config")
                .Elem("smtp-host", "customhost")
                .Elem("smtp-port", 42);
            var response = _plug.At("configuration", "custom").Put(settings);
            Assert.IsTrue(response.IsSuccessful);
            var email = new XDoc("email")
                .Attr("configuration", "custom")
                .Elem("to", "to@bar.com")
                .Elem("from", "from@bar.com")
                .Elem("subject", "subject")
                .Elem("body", "body");
            _log.Debug("sending message");
            response = _plug.At("message").Post(email, new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual("customhost", _smtpClientFactory.Settings.Host);
            Assert.AreEqual(42, _smtpClientFactory.Settings.Port);
            Assert.IsNotNull(_smtpClientFactory.Client.Message);
        }

        [Test]
        public void Omitting_host_in_custom_settings_but_providing_apikey_inherits_default_settings() {
            var apikey = StringUtil.CreateAlphaNumericKey(6);
            var settings = new XDoc("config")
                .Elem("apikey", apikey);
            var response = _plug.At("configuration", "custom").Put(settings);
            Assert.IsTrue(response.IsSuccessful);
            var email = new XDoc("email")
                .Attr("configuration", "custom")
                .Elem("to", "to@bar.com")
                .Elem("from", "from@bar.com")
                .Elem("subject", "subject")
                .Elem("body", "body");
            _log.Debug("sending message");
            var settingsPlug = Plug.New(_plug.Uri).With("apikey", apikey);
            response = settingsPlug.At("message").Post(email, new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(DEFAULT_HOST, _smtpClientFactory.Settings.Host);
            Assert.IsNotNull(_smtpClientFactory.Client.Message);
            Assert.AreEqual("\"from@bar.com\" <from@bar.com>", _smtpClientFactory.Client.Message.From.ToString());
        }

        [Test]
        public void Can_send_using_settings_apikey() {
            var apikey = StringUtil.CreateAlphaNumericKey(6);
            var settings = new XDoc("config")
                .Elem("smtp-host", "customhost")
                .Elem("apikey", apikey);
            var response = _plug.At("configuration", "custom").Put(settings);
            Assert.IsTrue(response.IsSuccessful);
            var email = new XDoc("email")
                .Attr("configuration", "custom")
                .Elem("to", "to@bar.com")
                .Elem("from", "from@bar.com")
                .Elem("subject", "subject")
                .Elem("body", "body");
            _log.Debug("sending message");
            var settingsPlug = Plug.New(_plug.Uri).With("apikey",apikey);
            response = settingsPlug.At("message").Post(email, new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual("customhost", _smtpClientFactory.Settings.Host);
            Assert.IsNotNull(_smtpClientFactory.Client.Message);
            Assert.AreEqual("\"from@bar.com\" <from@bar.com>", _smtpClientFactory.Client.Message.From.ToString());
        }
    }
}
