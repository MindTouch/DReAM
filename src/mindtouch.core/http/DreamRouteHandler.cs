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
using System.Web;
using System.Web.Routing;

namespace MindTouch.Dream.Http {

    internal class DreamRouteHandler : IRouteHandler {

        //--- Fields ---
        private readonly DreamApplication _application;
        private readonly IDreamEnvironment _env;

        //--- Constructors ---
        public DreamRouteHandler(DreamApplication application, IDreamEnvironment env) {
            _application = application;
            _env = env;
        }

        //--- Interface Methods ---
        IHttpHandler IRouteHandler.GetHttpHandler(RequestContext requestContext) {
            return new RoutedHttpHandler(_application, _env);
        }
    }
}