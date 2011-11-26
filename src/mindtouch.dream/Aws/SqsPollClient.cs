/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */
using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using MindTouch.Collections;
using MindTouch.Dream;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;

namespace MindTouch.Aws {
    public class SqsPollClient : ISqsPollClient {

        //--- Types ---
        private class Listener : IDisposable {

            //--- Class Fields ---
            private static readonly ILog _log = LogUtils.CreateLog();

            //--- Fields ---
            private readonly string _queuename;
            private readonly Action<AwsSqsMessage> _callback;
            private readonly IAwsSqsClient _client;
            private readonly TaskTimer _pollTimer;
            private readonly ExpiringHashSet<string> _cache;
            private bool _isDisposed;
            private readonly TimeSpan _cacheTimer;

            //--- Constructors ---
            public Listener(string queuename, Action<AwsSqsMessage> callback, IAwsSqsClient client, TaskTimerFactory timerFactory, TimeSpan interval) {
                _queuename = queuename;
                _callback = callback;
                _client = client;
                _cache = new ExpiringHashSet<string>(timerFactory);
                _cacheTimer = ((interval.TotalSeconds * 2 < 60) ? 60 : interval.TotalSeconds * 2 + 1).Seconds();
                _pollTimer = timerFactory.New(tt => Coroutine.Invoke(PollSqs, new Result()).WhenDone(r => _pollTimer.Change(interval, TaskEnv.None)), null);
                _pollTimer.Change(0.Seconds(), TaskEnv.None);
            }

            //--- Methods ---
            private IEnumerator<IYield> PollSqs(Result result) {
                _log.DebugFormat("polling SQS queue '{0}'", _queuename);
                while(!_isDisposed) {
                    Result<IEnumerable<AwsSqsMessage>> messageResult;
                    yield return messageResult = _client.ReceiveMax(_queuename, new Result<IEnumerable<AwsSqsMessage>>()).Catch();
                    if(messageResult.HasException) {
                        LogError(messageResult.Exception, "fetching messages");
                        result.Return();
                        yield break;
                    }
                    var messages = messageResult.Value;
                    if(!messages.Any()) {
                        result.Return();
                        yield break;
                    }
                    foreach(var msg in messages.Where(msg => !_cache.SetOrUpdate(msg.MessageId, _cacheTimer))) {
                        try {
                            _log.DebugFormat("dispatching message '{0}' from queue '{1}'", msg.MessageId, _queuename);
                            _callback(msg);
                        } catch(Exception e) {
                            _log.Warn(
                                string.Format("dispatching message '{0}' from queue '{1}' threw '{2}': {3}",
                                    msg.MessageId,
                                    _queuename,
                                    e,
                                    e.Message
                                ),
                                e
                            );
                            continue;
                        }
                        Result<AwsSqsResponse> deleteResult;
                        yield return deleteResult = _client.Delete(msg, new Result<AwsSqsResponse>()).Catch();
                        if(deleteResult.HasException) {
                            LogError(deleteResult.Exception, string.Format("deleting message '{0}'", msg.MessageId));
                        } else {
                            _cache.SetOrUpdate(msg.MessageId, _cacheTimer);
                        }
                    }
                }
            }

            private void LogError(Exception e, string prefix) {
                if(e.GetType().IsA<AwsSqsRequestException>()) {
                    var awsException = e as AwsSqsRequestException;
                    if(awsException.IsSqsError) {
                        _log.WarnFormat("{0} resulted in AWS error {1}/{2}: {3}",
                            prefix,
                            awsException.Error.Code,
                            awsException.Error.Type,
                            awsException.Error.Message
                        );
                        return;
                    }
                }
                _log.Warn(string.Format("{0} resulted in non-AWS exception: {1}", prefix, e.Message), e);
            }

            public void Dispose() {
                _isDisposed = true;
                _pollTimer.Cancel();
            }
        }

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly IAwsSqsClient _client;
        private readonly TaskTimerFactory _timerFactory;
        private readonly List<Listener> _listeners = new List<Listener>();

        //--- Constructors ---
        public SqsPollClient(IAwsSqsClient client, TaskTimerFactory timerFactory) {
            _client = client;
            _timerFactory = timerFactory;
        }

        //--- Methods ---
        public void Listen(string queuename, TimeSpan pollInterval, Action<AwsSqsMessage> callback) {
            _listeners.Add(new Listener(queuename, callback, _client, _timerFactory, pollInterval));
        }

        public void Dispose() {
            foreach(var listener in _listeners) {
                listener.Dispose();
            }
            _listeners.Clear();
        }
    }
}