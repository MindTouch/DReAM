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
                IEnumerable<AwsSqsMessage> messages = null;
                while(!_isDisposed) {
                    yield return _client.ReceiveMax(_queuename, new Result<IEnumerable<AwsSqsMessage>>()).Set(v => messages = v);
                    if(!messages.Any()) {
                        result.Return();
                        yield break;
                    }
                    foreach(var msg in messages) {
                        if(_cache.SetOrUpdate(msg.MessageId, _cacheTimer)) {

                            // we've recently seen this message, so ignore it
                            continue;
                        }
                        try {
                            _callback(msg);
                        } catch {
                            _log.Warn("");
                            continue;
                        }
                        yield return _client.Delete(msg, new Result<AwsSqsResponse>());
                    }
                }
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