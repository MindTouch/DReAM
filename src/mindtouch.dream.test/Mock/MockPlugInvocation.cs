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
namespace MindTouch.Dream.Test.Mock {

#pragma warning disable 1574
    /// <summary>
    /// Provides access to <see cref="MockPlug"/> invocation values, used in defining the <see cref="MockPlug.Returns(System.Func{MindTouch.Dream.Test.Mock.MockPlugInvocation,MindTouch.Dream.DreamMessage})"/> callback.
    /// </summary>
#pragma warning restore 1574
    public class MockPlugInvocation {

        //--- Fields ---

        /// <summary>
        /// Mock invocation verb.
        /// </summary>
        public readonly string Verb;

        /// <summary>
        /// Mock invocation  uri.
        /// </summary>
        public readonly XUri Uri;

        /// <summary>
        /// Mock invocation request message.
        /// </summary>
        public readonly DreamMessage Request;

        /// <summary>
        /// Headers the mock will attach to the returned message.
        /// </summary>
        public readonly DreamHeaders ResponseHeaders;

        //--- Constructors ---
        internal MockPlugInvocation(string verb, XUri uri, DreamMessage request, DreamHeaders responseHeaders) {
            Verb = verb;
            Uri = uri;
            Request = request;
            ResponseHeaders = responseHeaders;
        }
    }
}