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
using System.Net;
using System.Web;
using System.Web.Mvc;
using log4net;
using MindTouch;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;
using MvcAtomFeed.Models;

namespace MvcAtomFeed.Controllers {
    public class HomeController : Controller {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Properties ---
        public Plug FeedService { get { return DreamApplication.Current.Self.At("Feed"); } }

        //--- Methods ---
        public ActionResult Index() {
            _log.Debug("Getting feed index");
            var feed = FeedService.Get().ToDocument();
            var posts = from entry in feed["entry"]
                        let pathInfo = EntryHelper.GetPathInfo(entry)
                        select new PostModel() {
                            Title = entry["title"].AsText,
                            Summary = entry["summary"].AsText,
                            PathDate = pathInfo.Date,
                            PathTitle = pathInfo.Title
                        };
            return View(posts.ToArray());
        }

        public ActionResult Entry(string date, string title) {
            _log.DebugFormat("Getting feed entry {0}/{1}", date, title);
            var entryResponse = FeedService.At(date, title).Get(new Result<DreamMessage>()).Wait();
            if(!entryResponse.IsSuccessful) {
                throw new HttpException((int)HttpStatusCode.NotFound, "no such entry");
            }
            var entry = entryResponse.ToDocument();
            return View(new PostModel {
                Title = entry["title"].AsText,
                Content = entry["content"].AsText
            });
        }

        public ActionResult Add() {
            return View(new PostModel());
        }

        [HttpPost]
        public ActionResult Add(PostModel model) {
            var entry = new XDoc("entry")
                .Elem("title", model.Title)
                .Elem("summary", model.Content.Substring(0, Math.Min(80, model.Content.Length)))
                .Elem("content", model.Content);
            FeedService.Post(entry);
            return RedirectToAction("Index");
        }
    }
}