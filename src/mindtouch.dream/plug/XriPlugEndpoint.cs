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

using System.Collections.Generic;

using MindTouch.Tasking;

namespace MindTouch.Dream {
    using Yield = IEnumerator<IYield>;

    internal class XriPlugEndpoint : IPlugEndpoint {

        //--- Methods ---
        public int GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            normalized = uri;
            switch(uri.Scheme) {
            case "xri":
                return Plug.BASE_ENDPOINT_SCORE;
            default:
                return 0;
            }
        }

        public Yield Invoke(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {

            // NOTE (steveb): we convert 'xri://@name/path?params' into 'http://xri.net/@name/path?params'

            // prepend segments with authority
            List<string> segments = new List<string>();
            segments.Add(uri.Authority);
            if(uri.Segments != null) {
                segments.AddRange(uri.Segments);
            }

            // build new plug
            List<PlugHandler> preHandlers = (plug.PreHandlers != null) ? new List<PlugHandler>(plug.PreHandlers) : null;
            List<PlugHandler> postHandlers = (plug.PostHandlers != null) ? new List<PlugHandler>(plug.PostHandlers) : null;
            Plug xri = new Plug(new XUri("http", null, null, "xri.net", 80, segments.ToArray(), uri.TrailingSlash, uri.Params, uri.Fragment), plug.Timeout, request.Headers, preHandlers, postHandlers, plug.Credentials, plug.CookieJar, plug.MaxAutoRedirects);

            // add 'Accept' header for 'application/xrds+xml' mime-type
            if((xri.Headers == null) || (xri.Headers.Accept == null)) {
                xri = xri.WithHeader(DreamHeaders.ACCEPT, MimeType.RenderAcceptHeader(MimeType.XRDS));
            }

            // BUGBUGBUG (steveb): this will probably fail in some cases since we may exit this coroutine before the call has completed!

            xri.InvokeEx(verb, request, response);
            yield break;
        }
    }
}
