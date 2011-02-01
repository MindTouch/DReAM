/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2010 MindTouch, Inc.
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
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MvcAtomFeed.Services {

    [DreamService("Dream Example Feed Service", "Copyright (c) 2010 MindTouch, Inc.",
       Info = "http://developer.mindtouch.com/Dream/Samples/Feed",
       SID = new[] { "side://services.mindtouch.com/dream/samples/2010/12/feed" }
    )]
    public class FeedService : DreamService {

        [DreamFeature("GET:", "Get feed")]
        public XDoc GetFeed() {
            return LoadFeed();
        }

        [DreamFeature("GET:{date}/{title}", "Get feed entry")]
        public DreamMessage GetFeedEntry(string title, string date) {
            var entryDoc = (from entry in LoadFeed()["entry"]
                            let pathInfo = EntryHelper.GetPathInfo(entry)
                            where pathInfo.Title == title && pathInfo.Date == date
                            select entry).FirstOrDefault();
            return entryDoc == null ? DreamMessage.NotFound("no such entry") : DreamMessage.Ok(entryDoc);
        }

        [DreamFeature("POST:", "Add feed entry")]
        public DreamMessage PostFeedEntry(XDoc body) {
            var published = body["published"].AsDate ?? DateTime.UtcNow;
            var title = body["title"].AsText;
            var summary = body["summary"].AsText;
            var content = body["content"].AsText;
            var feed = LoadFeed();
            var feedUri = feed["_:link[@rel='self']/@href"].AsUri;
            var link = feedUri.At(published.ToString("yyyy-MM-dd")).At(GetUriTitle(title));
            var entry = new XAtomEntry(title, published, published);
            entry.AddLink(link, XAtomBase.LinkRelation.Self, null, null, title);
            entry.AddSummary(summary);
            entry.AddContent(content);
            feed.Add(entry);
            SaveFeed(feed);
            return DreamMessage.Ok(entry);
        }

        private string GetUriTitle(string title) {
            return XUri.EncodeSegment(title.ReplaceAll("-", "_", " ", "_"));
        }

        private XAtomFeed LoadFeed() {
            var storageResponse = Storage.At("feed.xml").Get(new Result<DreamMessage>()).Wait();
            if(storageResponse.IsSuccessful) {
                return new XAtomFeed(storageResponse.ToDocument());
            }
            var feed = new XAtomFeed("some feed", Self.Uri.AsPublicUri(), DateTime.UtcNow);
            SaveFeed(feed);
            return feed;
        }

        private void SaveFeed(XAtomFeed feed) {
            Storage.At("feed.xml").Put(feed);
        }
    }
}
