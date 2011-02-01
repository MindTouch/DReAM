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
using MindTouch.Dream;
using MindTouch.Xml;

namespace MvcAtomFeed {
    public static class EntryHelper {
        public static EntryPathInfo GetPathInfo(XDoc entry) {
            var link = entry["_:link[@rel='self']/@href"].AsUri;
            if(link == null) {
                return null;
            }
            var segments = link.GetSegments(UriPathFormat.Decoded);
            return new EntryPathInfo(segments[segments.Length - 2], segments[segments.Length - 1]);
        }
    }
}