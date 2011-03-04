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

using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream {

    /// <summary>
    /// Provides extension methods for <see cref="Plug"/>.
    /// </summary>
    public static class PlugEx {

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.GET"/> verb and no message body and return as a document result.
        /// </summary>
        /// <remarks>
        /// Since this method goes straight from a <see cref="DreamMessage"/> to a document, this method will set a
        /// <see cref="DreamResponseException"/> on the result, if <see cref="DreamMessage.IsSuccessful"/> is <see langword="False"/>.
        /// </remarks>
        /// <param name="plug">Plug instance to invoke.</param>
        /// <param name="result">The <see cref="Result{XDoc}"/>instance to be returned by this method.</param>
        /// <returns>Synchronization handle.</returns>
        public static Result<XDoc> Get(this Plug plug, Result<XDoc> result) {
            plug.Invoke(Verb.GET, DreamMessage.Ok(), new Result<DreamMessage>()).WhenDone(r => {
                if(r.HasException) {
                    result.Throw(r.Exception);
                } else if(!r.Value.IsSuccessful) {
                    result.Throw(new DreamResponseException(r.Value));
                } else {
                    result.Return(r.Value.ToDocument());
                }
            });
            return result;
        }
    }
}
