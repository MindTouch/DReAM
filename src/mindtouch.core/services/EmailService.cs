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

using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Autofac;
using log4net;

using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Email Sender", "Copyright (c) 2006-2011 MindTouch, Inc.",
        Info = "http://developer.mindtouch.com/Dream/Services/EmailService",
        SID = new[] { "sid://mindtouch.com/2009/01/dream/email" }
    )]
    [DreamServiceConfig("smtp-host", "hostname", "")]
    [DreamServiceConfig("smtp-port", "port", "")]
    [DreamServiceConfig("smtp-auth-user", "username", "")]
    [DreamServiceConfig("smtp-auth-password", "username", "")]
    [DreamServiceConfig("use-ssl", "bool", "")]
    internal class EmailService : DreamService {

        //--- Types ---

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private string _emailApikey;
        private readonly Dictionary<string, SmtpSettings> _smtpSettings = new Dictionary<string, SmtpSettings>();
        private SmtpSettings _defaultSettings;
        private ISmtpClientFactory _clientFactory;

        //--- Features ---
        [DreamFeature("POST:message", "Send an email")]
        internal Yield SendEmail(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var mailDoc = request.ToDocument();
            var mailMsg = new MailMessage();
            foreach(XDoc to in mailDoc["to"]) {
                var email = to.AsText;
                if(string.IsNullOrEmpty(email)) {
                    continue;
                }
                _log.DebugFormat("Adding TO address '{0}'", email);
                mailMsg.To.Add(GetEmailAddress(email));
            }
            if(mailMsg.To.Count == 0) {
                throw new DreamBadRequestException("message does not contains any TO email addresses");
            }
            var from = mailDoc["from"].AsText;
            _log.DebugFormat("from address: {0}", from);
            mailMsg.From = GetEmailAddress(from);
            mailMsg.Subject = mailDoc["subject"].AsText;
            string plaintextBody = null;
            foreach(var body in mailDoc["body"]) {
                AlternateView view;
                if(body["@html"].AsBool ?? false) {
                    _log.Debug("adding html body");
                    view = AlternateView.CreateAlternateViewFromString(body.Contents, Encoding.UTF8, "text/html");
                    view.TransferEncoding = TransferEncoding.Base64;
                    mailMsg.AlternateViews.Add(view);
                } else {
                    plaintextBody = body.Contents;
                }
            }
            if(!string.IsNullOrEmpty(plaintextBody)) {
                _log.Debug("adding plain text body");
                mailMsg.Body = plaintextBody;
            }
            foreach(var header in mailDoc["headers/header"]) {
                var name = header["name"].AsText;
                var value = header["value"].AsText;
                _log.DebugFormat("adding header '{0}': {1}", name, value);
                mailMsg.Headers.Add(name, value);
            }
            GetClient(mailDoc["@configuration"].AsText).Send(mailMsg);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        private MailAddress GetEmailAddress(string addressString) {
            var address = new MailAddress(addressString);
            if(string.IsNullOrEmpty(address.DisplayName)) {
                address = new MailAddress(addressString, addressString);
            }
            return address;
        }

        [DreamFeature("PUT:configuration/{configuration}", "Set smtp settings for a named configuration")]
        internal Yield ConfigureSmtp(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var configuration = context.GetParam("configuration");
            var settingsDoc = request.ToDocument();
            _log.DebugFormat("configuring settings for configuration '{0}'", configuration);
            var host = settingsDoc["smtp-host"].AsText;
            var apikey = settingsDoc["apikey"].AsText;
            if(string.IsNullOrEmpty(host) && string.IsNullOrEmpty(apikey)) {
                response.Return(DreamMessage.BadRequest("must specify either new smtp config with a host or specify an apikey"));
                yield break;
            }
            SmtpSettings settings;
            if(string.IsNullOrEmpty(host)) {
                settings = new SmtpSettings() {
                    Host = _defaultSettings.Host,
                    Apikey = apikey,
                    AuthPassword = _defaultSettings.AuthPassword,
                    AuthUser = _defaultSettings.AuthUser,
                    EnableSsl = _defaultSettings.EnableSsl,
                    Port = _defaultSettings.Port,
                };
            } else {
                _log.DebugFormat("Smtp Host: {0}", host);
                settings = new SmtpSettings {
                    Host = host,
                    AuthUser = settingsDoc["smtp-auth-user"].AsText,
                    AuthPassword = settingsDoc["smtp-auth-password"].AsText,
                    Apikey = apikey,

                    // Note (arnec): ssl requires mono 2.0 and likely root certificate import via 'mozroots --import --ask-remove --machine'
                    EnableSsl = settingsDoc["use-ssl"].AsBool ?? false
                };
                if(settingsDoc["smtp-port"].AsInt.HasValue) {
                    settings.Port = settingsDoc["smtp-port"].AsInt.Value;
                }
            }
                lock(_smtpSettings) {
                    _smtpSettings[configuration] = settings;
                }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:configuration/{configuration}", "Get smtp settings for a named configuration (minus password)")]
        internal Yield InspectSmtp(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string configuration = context.GetParam("configuration");
            SmtpSettings settings;
            DreamMessage msg;
            lock(_smtpSettings) {
                if(_smtpSettings.TryGetValue(configuration, out settings)) {
                    msg = DreamMessage.Ok(new XDoc("smtp")
                        .Elem("smtp-host", settings.Host)
                        .Elem("smtp-port", settings.Port)
                        .Elem("use-ssl", settings.EnableSsl)
                        .Elem("smtp-auth-user", settings.AuthUser));
                } else {
                    msg = DreamMessage.NotFound("No such configuration");
                }
            }
            response.Return(msg);
            yield break;
        }

        [DreamFeature("DELETE:configuration/{configuration}", "Set smtp settings for a specific wiki")]
        internal Yield DeleteSmtpSettings(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string configuration = context.GetParam("configuration");
            _log.DebugFormat("removing settings for configuration '{0}'", configuration);
            lock(_smtpSettings) {
                _smtpSettings.Remove(configuration);
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, ILifetimeScope container, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            _defaultSettings = new SmtpSettings { Host = config["smtp-host"].AsText };
            if(string.IsNullOrEmpty(_defaultSettings.Host)) {
                _defaultSettings.Host = "localhost";
            }
            _log.DebugFormat("Smtp Host: {0}", _defaultSettings.Host);

            // Note (arnec): ssl requires mono 2.0 and likely root certificate import via 'mozroots --import --ask-remove --machine'
            _defaultSettings.EnableSsl = config["use-ssl"].AsBool ?? false;
            if(config["smtp-port"].AsInt.HasValue) {
                _defaultSettings.Port = config["smtp-port"].AsInt.Value;
            }
            _defaultSettings.AuthUser = config["smtp-auth-user"].AsText;
            _defaultSettings.AuthPassword = config["smtp-auth-password"].AsText;
            _clientFactory = container.IsRegistered<ISmtpClientFactory>()
                ? container.Resolve<ISmtpClientFactory>()
                : new SmtpClientFactory();

            // get an apikey for accessing the services without it's private/internal keys
            _emailApikey = config["apikey"].AsText;
            result.Return();
        }

        protected override Yield Stop(Result result) {
            _smtpSettings.Clear();
            _defaultSettings = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        private ISmtpClient GetClient(string configuration) {
            _log.DebugFormat("Getting smtp settings for configuration '{0}'", configuration);
            SmtpSettings settings;
            lock(_smtpSettings) {
                if(!_smtpSettings.TryGetValue(configuration, out settings)) {
                    _log.DebugFormat("Using default settings");
                    settings = _defaultSettings;
                }
            }
            return _clientFactory.CreateClient(settings);
        }

        protected override DreamAccess DetermineAccess(DreamContext context, string key) {
            if(!string.IsNullOrEmpty(key)) {

                // Grant internal access for proper apikey
                if(!string.IsNullOrEmpty(_emailApikey) && _emailApikey == key) {
                    return DreamAccess.Internal;
                }

                // Check whether we can test an apikey from the targeted configuration
                var configuration = context.GetParam("configuration", null);
                if(string.IsNullOrEmpty(configuration) && context.Request.HasDocument) {
                    configuration = context.Request.ToDocument()["@configuration"].AsText;
                }
                if(!string.IsNullOrEmpty(configuration)) {
                    SmtpSettings settings;
                    lock(_smtpSettings) {
                        _smtpSettings.TryGetValue(configuration, out settings);
                    }
                    if(settings != null && !string.IsNullOrEmpty(settings.Apikey) && settings.Apikey == key) {
                        return DreamAccess.Internal;
                    }
                }
            }
            return base.DetermineAccess(context, key);
        }
    }

    /// <summary>
    /// Settings container for clients created with <see cref="ISmtpClientFactory"/>.
    /// </summary>
    public class SmtpSettings {

        //--- Fields ---

        /// <summary>
        /// Smtp Host
        /// </summary>
        public string Host;

        /// <summary>
        /// Optional Smtp Port;
        /// </summary>
        public int? Port;

        /// <summary>
        /// Optional Authentication User
        /// </summary>
        public string AuthUser;

        /// <summary>
        /// Optional Authentication Password
        /// </summary>
        public string AuthPassword;

        /// <summary>
        /// Try to use secure connection?
        /// </summary>
        public bool EnableSsl;

        /// <summary>
        /// Apikey that will be provided to authorize the use of these settings.
        /// </summary>
        public string Apikey;
    }

    /// <summary>
    /// Implementation of <see cref="ISmtpClientFactory"/>
    /// </summary>
    public class SmtpClientFactory : ISmtpClientFactory {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Methods ---

        /// <summary>
        /// Create a new <see cref="ISmtpClient"/>.
        /// </summary>
        /// <param name="settings">Client settings.</param>
        /// <returns>New <see cref="ISmtpClient"/> instance</returns>
        public ISmtpClient CreateClient(SmtpSettings settings) {
            var client = new SmtpClient {
                Host = settings.Host,
                EnableSsl = settings.EnableSsl
            };
            _log.DebugFormat("SSL enabled: {0}", client.EnableSsl);
            if(settings.Port.HasValue) {
                client.Port = settings.Port.Value;
                _log.DebugFormat("using custom port: {0}", client.Port);
            }
            if(!string.IsNullOrEmpty(settings.AuthUser)) {
                _log.DebugFormat("using authentication user: {0}", settings.AuthUser);
                var credentials = new NetworkCredential(settings.AuthUser, settings.AuthPassword);
                client.Credentials = credentials;
            }
            return new SmtpClientWrapper(client);
        }
    }

    /// <summary>
    /// Factory for creating <see cref="ISmtpClient"/> instances.
    /// </summary>
    public interface ISmtpClientFactory {

        //--- Methods ---

        /// <summary>
        /// Create a new <see cref="ISmtpClient"/>.
        /// </summary>
        /// <param name="settings">Client settings.</param>
        /// <returns>New <see cref="ISmtpClient"/> instance</returns>
        ISmtpClient CreateClient(SmtpSettings settings);
    }

    /// <summary>
    /// Implemenation of <see cref="ISmtpClient"/> wrapping the standard <see cref="SmtpClient"/>.
    /// </summary>
    public class SmtpClientWrapper : ISmtpClient {

        //--- Fields ---
        private readonly SmtpClient _client;

        //--- Constructors ---
        
        /// <summary>
        /// Create a new <see cref="SmtpClient"/> wrapper.
        /// </summary>
        /// <param name="client"></param>
        public SmtpClientWrapper(SmtpClient client) {
            _client = client;
        }

        //--- ISmtpClient Members ---
        void ISmtpClient.Send(MailMessage message) {
            _client.Send(message);
        }
    }

    /// <summary>
    /// Simple Smtp client interface
    /// </summary>
    public interface ISmtpClient {

        //--- Methods ---

        /// <summary>
        /// Send a mail message.
        /// </summary>
        /// <param name="message">Message to send.</param>
        void Send(MailMessage message);
    }



}
