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
using System.Collections.Generic;
using System.IO;
using log4net;
using MindTouch.Collections;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Queue Service", "Copyright (c) 2006-2009 MindTouch, Inc.",
        Info = "http://developer.mindtouch.com/Dream/Services/QueueService",
        SID = new string[] { "sid://mindtouch.com/2009/12/dream/queue" }
    )]
    [DreamServiceConfig("folder", "path", "Rooted path to the folder managed by the queue service.")]
    [DreamServiceConfig("expire", "int?", "Default item expiration time in seconds (default 30).")]
    internal class QueueService : DreamService {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly Dictionary<string, ITransactionalQueue<XDoc>> _queues = new Dictionary<string, ITransactionalQueue<XDoc>>();
        private string _rootPath;
        private TimeSpan _defaultExpire;

        //--- Features ---
        [DreamFeature("POST:queue/{queuename}", "Put a new item in the named queue")]
        [DreamFeatureParam("queuename", "string", "Name of the queue")]
        [DreamFeatureStatus(DreamStatus.Ok, "The item was successfully enqueued")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Access to feature was denied")]
        internal Yield PostQueueItem(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var name = context.GetParam("queuename");
            var queue = GetQueue(name);
            var item = request.ToDocument();
            queue.Enqueue(item);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:queue/{queuename}", "Get the next item from the named queue")]
        [DreamFeatureParam("queuename", "string", "Name of the queue")]
        [DreamFeatureParam("expire", "int", "Time in milliseconds after which an undelete item is returned to the queue")]
        [DreamFeatureStatus(DreamStatus.Ok, "The next item in the queue was successfully returned")]
        [DreamFeatureStatus(DreamStatus.NoContent, "The queue is empty")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Access to feature was denied")]
        internal Yield GetNextQueueItem(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var name = context.GetParam("queuename");
            var queue = GetQueue(name);
            var expireSeconds = context.GetParam<int>("expire", 0);
            var expire = expireSeconds == 0 ? _defaultExpire : TimeSpan.FromSeconds(expireSeconds);
            var item = queue.Dequeue(expire);
            if(item == null) {
                response.Return(new DreamMessage(DreamStatus.NoContent, null));
                yield break;
            }
            var msg = DreamMessage.Ok(item.Value);

            msg.Headers.Location = context.Uri.At(item.Id.ToString()).AsPublicUri();
            response.Return(msg);
            yield break;
        }

        [DreamFeature("GET:queue/{queuename}/size", "Get the queue size")]
        [DreamFeatureParam("queuename", "string", "Name of the queue")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Access to feature was denied")]
        internal Yield GetQueueSize(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var name = context.GetParam("queuename");
            var queue = GetQueue(name);
            response.Return(DreamMessage.Ok(new XDoc("queue").Elem("name", name).Elem("size", queue.Count)));
            yield break;
        }

        [DreamFeature("DELETE:queue/{queuename}", "Delete a queue and all its items")]
        [DreamFeatureParam("queuename", "string", "Name of the queue")]
        [DreamFeatureStatus(DreamStatus.Ok, "The queues was wiped")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Access to feature was denied")]
        internal Yield DeleteQueue(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var name = context.GetParam("queuename");
            lock(_queues) {
                ITransactionalQueue<XDoc> queue;
                if(_queues.TryGetValue(name, out queue)) {
                    queue.Dispose();
                    Directory.Delete(Path.Combine(_rootPath, name),true);
                    _queues.Remove(name);
                }
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("DELETE:queue/{queuename}/{id}", "Delete a previously retrieved item")]
        [DreamFeatureParam("queuename", "string", "Name of the queue")]
        [DreamFeatureParam("id", "int", "Id of item")]
        [DreamFeatureParam("release", "bool", "Release the item back into the queue (default: false")]
        [DreamFeatureStatus(DreamStatus.Ok, "The item was deleted")]
        [DreamFeatureStatus(DreamStatus.Gone, "The item was no longer available for delete")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Access to feature was denied")]
        internal Yield DeleteQueueItem(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var name = context.GetParam("queuename");
            var id = context.GetParam("id", 0);
            var release = context.GetParam("release", false);
            var queue = GetQueue(name);
            if(release) {
                queue.RollbackDequeue(id);
                response.Return(DreamMessage.Ok());
            } else if(queue.CommitDequeue(id)) {
                response.Return(DreamMessage.Ok());
            } else {
                response.Return(new DreamMessage(DreamStatus.Gone, null));
            }
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            _rootPath = config["folder"].AsText;
            var expireSeconds = config["expire"].AsDouble ?? 30;
            _defaultExpire = TimeSpan.FromSeconds(expireSeconds);
            result.Return();
        }

        protected override Yield Stop(Result result) {
            lock(_queues) {
                foreach(var queue in _queues.Values) {
                    queue.Dispose();
                }
                _queues.Clear();
            }
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        private ITransactionalQueue<XDoc> GetQueue(string name) {
            ITransactionalQueue<XDoc> queue;
            lock(_queues) {
                if(!_queues.TryGetValue(name, out queue)) {
                    queue = new TransactionalQueue<XDoc>(new MultiFileQueueStream(Path.Combine(_rootPath, name)), new XDocQueueItemSerializer());
                    _queues.Add(name, queue);
                }
            }
            return queue;
        }
    }
}
