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
using log4net;
using MindTouch.Tasking;

namespace MindTouch.Sqs {

    /// <summary>
    /// SQS polling client for ISqsClient.
    /// </summary>
    public class SqsPollingClient : ISqsPollingClient, IDisposable {

        //--- Types ---
        private sealed class Listener : IDisposable {

            //--- Class Fields ---
            private static readonly ILog _log = LogUtils.CreateLog();

            //--- Fields ---
            private readonly SqsPollingClient _container;
            private volatile bool _isDisposed;

            //--- Constructors ---
            public Listener(SqsPollingClient container) {
                this._container = container;
            }

            //--- Methods ---
            public void Dispose() {
                _isDisposed = true;
                _container.Remove(this);
            }

            internal void LongPollSqs(SqsPollingClientSettings settings) {
                var failCounter = 0;
                while(true) {
                    IEnumerable<SqsMessage> messages;
                    try {
                        messages = _container._client.ReceiveMessages(settings.QueueName, settings.LongPollInterval, settings.MaxNumberOfMessages);
                        failCounter = 0;
                    } catch(Exception e) {
                        LogError(e, string.Format("ReceiveMessages (fail count: {0:#,##0})", ++failCounter));
                        AsyncUtil.Sleep(settings.WaitTimeOnError);
                        continue;
                    }
                    if(_isDisposed) {
                        break;
                    }
                    if(messages.None()) {
                        continue;
                    }
                    try {
                        settings.Callback(messages);
                    } catch(Exception e) {
                        LogError(e, "callback failed");
                    }
                }
            }

            private void LogError(Exception e, string prefix) {
                var awsException = e as SqsException;
                if(awsException != null) {
                    _log.WarnExceptionFormat(awsException, "{0} resulted in AWS error", prefix);
                    return;
                }
                _log.Warn(prefix, e);
            }
        }

        //--- Fields ---
        private readonly ISqsClient _client;
        private readonly List<Listener> _listeners = new List<Listener>();

        //--- Constructors ---

        /// <summary>
        /// Constructor for creating an instance.
        /// </summary>
        /// <param name="client">ISqsClient to use.</param>
        public SqsPollingClient(ISqsClient client) {
            _client = client;
        }

        //--- Methods ---

        /// <summary>
        /// Start listening for SQS messages with the provided settings.
        /// </summary>
        /// <param name="settings">Polling settings.</param>
        /// <returns>Object to dispose listener when no longer needed.</returns>
        public IDisposable Listen(SqsPollingClientSettings settings) {
            if(settings == null) {
                throw new ArgumentNullException("settings");
            }
            var listener = new Listener(this);
            lock(_listeners) {
                _listeners.Add(listener);
            }
            AsyncUtil.Fork(() => listener.LongPollSqs(settings));
            return listener;
        }

        /// <summary>
        /// Dispose of all listeners.
        /// </summary>
        public void Dispose() {
            var listeners = _listeners.ToArray();
            foreach(var listener in listeners) {
                listener.Dispose();
            }
        }

        private void Remove(Listener listener) {
            lock(_listeners) {
                _listeners.Remove(listener);
            }
        }
    }
}