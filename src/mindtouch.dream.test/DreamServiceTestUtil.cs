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

using NUnit.Framework;

namespace MindTouch.Dream.Test {

    /// <summary>
    /// Provides NUnit Assertion extension methods for <see cref="Plug"/> response <see cref="DreamMessage"/> instances.
    /// </summary>
    public static class DreamServiceTestUtil {

        /// <summary>
        /// Assert that the status of the message is equal to an expected value.
        /// </summary>
        /// <param name="response">Response message.</param>
        /// <param name="status">Status to assert.</param>
        public static void AssertStatus(this DreamMessage response, DreamStatus status) {
            AssertStatus(response, status, null);
        }

        /// <summary>
        /// Assert that the status of the message is equal to an expected value.
        /// </summary>
        /// <param name="response">Response message.</param>
        /// <param name="status">Status to assert.</param>
        /// <param name="failureMessage">Failure message.</param>
        public static void AssertStatus(this DreamMessage response, DreamStatus status, string failureMessage) {
            if(response.Status == status) {
                return;
            }
            if(!string.IsNullOrEmpty(failureMessage)) {
                failureMessage = failureMessage + "\r\n";
            }
            Assert.Fail(
                string.Format("{0}Request status was {1} instead of {2}:\r\n{3}",
                failureMessage,
                response.Status,
                status,
                (response.HasDocument
                ? string.Format("{0}: {1}", response.Status, response.ToDocument()["message"].AsText)
                : response.Status.ToString())));
        }

        /// <summary>
        /// Assert that the response indicates a successful request.
        /// </summary>
        /// <param name="response">Response message.</param>
        public static void AssertSuccess(this DreamMessage response) {
            AssertSuccess(response, null);
        }

        /// <summary>
        /// Assert that the response indicates a successful request.
        /// </summary>
        /// <param name="response">Response message.</param>
        /// <param name="failureMessage">Failure message.</param>
        public static void AssertSuccess(this DreamMessage response, string failureMessage) {
            if(response.IsSuccessful) {
                return;
            }
            if(!string.IsNullOrEmpty(failureMessage)) {
                failureMessage = failureMessage + "\r\n";
            }
            Assert.Fail(
                failureMessage +
                "Request failed:\r\n" +
                (response.HasDocument
                ? string.Format("{0}: {1}", response.Status, response.ToDocument()["message"].AsText)
                : response.Status.ToString()));
        }
    }
}
