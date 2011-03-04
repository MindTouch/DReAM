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

using System;
using System.Collections.Generic;

using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Dream Atom", "Copyright (c) 2006-2011 MindTouch, Inc.",
        Info = "http://developer.mindtouch.com/Dream/Reference/Services/Atom",
        SID = new string[] { 
            "sid://mindtouch.com/2007/06/dream/atom",
            "http://services.mindtouch.com/dream/stable/2007/06/atom" 
        }
    )]
    [DreamServiceConfig("default-ttl", "double?", "Default time in seconds for events to live (default: 3600 seconds).")]
    [DreamServiceConfig("feed-title", "string?", "Feed title (default: \"Atom Feed\")")]
    internal class AtomService : DreamService {

        //--- Fields ---
        private XAtomFeed _feed;
        private int _counter;
        private double _defaultTTL;

        //--- Features ---
        [DreamFeature("GET:", "Retrieve feed")]
        public Yield GetEntries(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XAtomFeed feed = _feed;
            if(feed != null) {
                response.Return(DreamMessage.Ok(MimeType.ATOM, _feed));
            } else {
                throw new DreamBadRequestException("not initialized");
            }
            yield break;
        }

        [DreamFeature("POST:", "Add entry")]
        [DreamFeatureParam("ttl", "int?", "time-to-live in seconds for the posted event")]
        internal Yield PostEntries(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(request.ToDocument().Name != "entry") {
                throw new DreamBadRequestException("invalid format");
            }

            // prepare entry
            XAtomEntry entry = new XAtomEntry(request.ToDocument());
            int number = System.Threading.Interlocked.Increment(ref _counter);
            XUri link = Self.At(number.ToString());
            entry.Id = link;
            entry.AddLink(link, XAtomBase.LinkRelation.Edit, null, 0, null);

            // update feed
            XAtomFeed feed = _feed;
            if(feed != null) {
                lock(feed) {
                    feed.Add(entry);
                }
            } else {
                throw new DreamBadRequestException("not initialized");
            }

            // schedule entry deletion
            double seconds = context.GetParam<double>("ttl", _defaultTTL);
            if(seconds > 0) {
                TimerFactory.New(TimeSpan.FromSeconds(seconds), AutoDeletePost, number, TaskEnv.Clone());
            }

            // return updated entry
            response.Return(DreamMessage.Created(link, entry));
            yield break;
        }

        [DreamFeature("GET:{id}", "Retrieve entry")]
        public Yield GetEntry(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string id = context.GetParam("id");
            XAtomEntry entry = null;

            // get feed
            XAtomFeed feed = _feed;
            if(feed != null) {
                lock(feed) {
                    entry = new XAtomEntry(feed[string.Format("entry[id='{0}']", Self.At(id).Uri)]);
                }
            } else {
                throw new DreamBadRequestException("not initialized");
            }
            if(entry.IsEmpty) {
                response.Return(DreamMessage.NotFound("entry not found"));
            } else {
                response.Return(DreamMessage.Ok(MimeType.ATOM, entry));
            }
            yield break;
        }

        [DreamFeature("PUT:{id}", "Update entry")]
        internal Yield PutEntry(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(request.ToDocument().Name != "entry") {
                throw new DreamBadRequestException("invalid format");
            }
            string id = context.GetParam("id");

            // prepare entry
            XAtomEntry entry = new XAtomEntry(request.ToDocument());
            XUri link = Self.At(id);
            entry.Id = link;
            entry.AddLink(link, XAtomBase.LinkRelation.Edit, null, 0, null);

            // update feed
            XAtomFeed feed = _feed;
            XAtomEntry oldEntry;
            if(feed != null) {
                lock(feed) {
                    oldEntry = new XAtomEntry(feed[string.Format("entry[id='{0}']", link)]);
                    if(!oldEntry.IsEmpty) {
                        oldEntry.Replace(entry);
                    }
                }
            } else {
                throw new DreamBadRequestException("not initialized");
            }
            if(oldEntry.IsEmpty) {
                response.Return(DreamMessage.NotFound("entry not found"));
            } else {
                response.Return(DreamMessage.Ok(MimeType.ATOM, entry));
            }
            yield break;
        }

        [DreamFeature("DELETE:{id}", "Delete entry")]
        internal Yield DeleteEntry(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string id = context.GetParam("id");
            XAtomFeed feed = _feed;
            if(feed != null) {
                lock(feed) {
                    feed[string.Format("entry[id='{0}']", id)].Remove();
                }
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            if(_feed == null) {
                _feed = new XAtomFeed(config["feed-title"].AsText ?? "Atom Feed", Self, DateTime.UtcNow);
                _defaultTTL = config["default-ttl"].AsDouble ?? 3600.0;
                _counter = 0;
            }
            result.Return();
        }

        protected override Yield Stop(Result result) {
            _feed = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        private void AutoDeletePost(TaskTimer timer) {
            XAtomFeed feed = _feed;
            if(feed != null) {
                lock(feed) {
                    feed[string.Format("_:entry[_:id='{0}']", Self.At(((int)timer.State).ToString()).Uri.AsPublicUri())].Remove();
                }
            }
        }
    }
}
