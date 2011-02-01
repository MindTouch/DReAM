/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006, 2007 MindTouch, Inc.
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
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Sample.Services {
    [DreamService("Dream Tutorial Show Headers", "Copyright (c) 2006, 2007 MindTouch, Inc.",
        Info = "http://doc.opengarden.org/Dream_SDK/Tutorials/Show_Headers",
        SID = new[] { "http://services.mindtouch.com/dream/tutorial/2007/03/showheaders" }
    )]
    public class ShowHeadersService : DreamService {

        //--- Features ---
        [DreamFeature("*:headers", "Get message headers of request")]
        public IEnumerator<IYield> WildCardHeaders(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // convert dream message into an XML document
            XDoc xmessage = new XMessage(request);

            // select <headers> element
            XDoc headers = xmessage["headers"];

            // send it back
            response.Return(DreamMessage.Ok(headers));
            yield break;
        }

        [DreamFeature("*:body", "Get message body of request")]
        public IEnumerator<IYield> WildCardBody(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // convert dream message into an XML document
            XDoc xmessage = new XMessage(request);

            // select first child of <body> element
            XDoc body = xmessage["body"];

            // respond by sending only the body back
            response.Return(DreamMessage.Ok(body));
            yield break;
        }

        [DreamFeature("*:message", "Get entire request message")]
        public IEnumerator<IYield> WildCardMessage(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // convert dream message into an XML document
            XDoc xmessage = new XMessage(request);

            // respond by sending the message back
            response.Return(DreamMessage.Ok(xmessage));
            yield break;
        }
    }
}
