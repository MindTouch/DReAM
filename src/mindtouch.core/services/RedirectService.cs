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

    [DreamService("MindTouch Dream Proxy", "Copyright (c) 2006-2011 MindTouch, Inc.", 
        Info = "http://developer.mindtouch.com/Dream/Reference/Services/Proxy",
        SID = new string[] { 
            "sid://mindtouch.com/2007/03/dream/proxy",
            "http://services.mindtouch.com/dream/stable/2007/03/proxy" 
        }
    )]
    [DreamServiceConfig("proxy", "uri", "Uri to which to forward requests to.")]
    [DreamServiceConfig("timeout", "int", "Timeout in seconds when forwarding a request.")]
    internal class RedirectService : DreamService {

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private Plug _redirect;

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            _redirect = Plug.New(config["proxy"].AsUri ?? config["redirect"].AsUri, TimeSpan.FromSeconds(config["timeout"].AsInt ?? (int)Plug.DEFAULT_TIMEOUT.TotalSeconds));
            if(_redirect == null) {
                throw new ArgumentException("redirect URI missing or invalid");
            }
            result.Return();
        }

        [DreamFeature("*://*", "Redirect web-request")]
        public Yield GetHttp(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            yield return context.Relay(_redirect.At(context.GetSuffixes(UriPathFormat.Original)), request, response);
        }
    }
}
