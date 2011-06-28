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
using Autofac;
using log4net;
using MindTouch.Aws;
using MindTouch.Collections;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.Services.PubSub {
    using Yield = IEnumerator<IYield>;

    public class SqsPubSubPollster : ISqsPubSubPollster {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly string _queue;
        private readonly TimeSpan _pollInterval;
        private readonly IAwsSqsClient _client;
        private readonly TimeSpan _cacheTtl;
        private ExpiringHashSet<string> _cache;
        private Plug _plug;
        private TaskTimer _poll;

        //--- Constructors ---
        public SqsPubSubPollster(XDoc config, ILifetimeScope container) {
            _queue = config["queue"].AsText;
            _pollInterval = (config["poll-interval"].AsDouble ?? 60).Seconds();
            _cacheTtl = (config["cache-ttl"].AsDouble ?? 60).Seconds();
            _client = container.Resolve<IAwsSqsClient>(TypedParameter.From(new AwsSqsClientConfig(config["sqs-client"])));
        }

        //--- Methods ---
        public void RegisterEndPoint(Plug plug, TaskTimerFactory taskTimerFactory) {
            if(_poll != null) {
                throw new InvalidOperationException("cannot start a pollster more than once");
            }
            _cache = new ExpiringHashSet<string>(taskTimerFactory);
            _plug = plug;
            _poll = taskTimerFactory.New(tt => Coroutine.Invoke(PollSqs, new Result()).WhenDone(r => _poll.Change(_pollInterval, TaskEnv.None)), null);
            _log.DebugFormat("registered endpoint '{0}' as destination for queue '{1}'", plug, _queue);
            _poll.Change(0.Seconds(), TaskEnv.None);
        }

        private Yield PollSqs(Result result) {
            _log.DebugFormat("polling SQS for queu '{0}'", _queue);
            IEnumerable<AwsSqsMessage> messages = null;
            while(true) {
                yield return _client.ReceiveMax(_queue, new Result<IEnumerable<AwsSqsMessage>>()).Set(v => messages = v);

                if(!messages.Any()) {
                    result.Return();
                    yield break;
                }
                foreach(var msg in messages) {
                    if(_cache.SetOrUpdate(msg.Id, _cacheTtl)) {
                        continue;
                    }
                    DreamMessage response = null;
                    yield return _plug.Post(msg.BodyToDocument(), new Result<DreamMessage>()).Set(v => response = v);
                    if(response.IsSuccessful) {
                        yield return _client.Delete(msg, new Result<AwsSqsResponse>());
                    }
                }
            }
        }

        public void Dispose() {
            _poll.Cancel();
        }
    }
}