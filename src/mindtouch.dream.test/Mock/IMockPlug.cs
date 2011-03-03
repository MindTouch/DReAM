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
using MindTouch.Xml;

namespace MindTouch.Dream.Test.Mock {

    /// <summary>
    /// Interface describing a <see cref="MockPlug"/> definition.
    /// </summary>
    public interface IMockPlug {

        /// <summary>
        /// Except a given verb.
        /// </summary>
        /// <param name="verb">Verb to expect.</param>
        /// <returns>Same instance of <see cref="IMockPlug"/>.</returns>
        IMockPlug Verb(string verb);

        /// <summary>
        /// Modify the expected uri, to expec the call at the given relative path.
        /// </summary>
        /// <param name="path">Relative path to add to existing or base uri expectation.</param>
        /// <returns>Same instance of <see cref="IMockPlug"/>.</returns>
        IMockPlug At(params string[] path);

        /// <summary>
        /// Expect query key/value pair.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>Same instance of <see cref="IMockPlug"/>.</returns>
        IMockPlug With(string key, string value);

        /// <summary>
        /// Expect query key with a value checked by a callback.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="valueCallback">Callback to evaluate query value.</param>
        /// <returns>Same instance of <see cref="IMockPlug"/>.</returns>
        IMockPlug With(string key, Predicate<string> valueCallback);

        /// <summary>
        /// Expect the query uri to have a trailing slash
        /// </summary>
        /// <remarks>By default presence or lack of presence of trailing slashes is not considered when matching, but this call makes it significant</remarks>
        /// <returns>Same instance of <see cref="IMockPlug"/>.</returns>
        IMockPlug WithTrailingSlash();

        /// <summary>
        /// Expect the query uri to not have a trailing slash
        /// </summary>
        /// <remarks>By default presence or lack of presence of trailing slashes is not considered when matching, but this call makes it significant</remarks>
        /// <returns>Same instance of <see cref="IMockPlug"/>.</returns>
        IMockPlug WithoutTrailingSlash();

        /// <summary>
        /// Expect a given request document.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Same instance of <see cref="IMockInvokeExpectationParameter"/>.</returns>
        IMockPlug WithBody(XDoc request);

        /// <summary>
        /// Register a callback function to perform custom expectation matching.
        /// </summary>
        /// <param name="requestCallback">Callback to determine whether the document matches expecations.</param>
        /// <returns>Same instance of <see cref="IMockPlug"/>.</returns>
        IMockPlug WithMessage(Func<DreamMessage, bool> requestCallback);

        /// <summary>
        /// Expect the presence of a given header.
        /// </summary>
        /// <param name="key">Header key.</param>
        /// <param name="value">Header value.</param>
        /// <returns>Same instance of <see cref="IMockPlug"/>.</returns>
        IMockPlug WithHeader(string key, string value);

        /// <summary>
        /// Expect the presence of a given head with a value checked by a callback.
        /// </summary>
        /// <param name="key">Header key.</param>
        /// <param name="valueCallback">Callback to evaluate header value.</param>
        /// <returns>Same instance of <see cref="IMockPlug"/>.</returns>
        IMockPlug WithHeader(string key, Predicate<string> valueCallback);

        /// <summary>
        /// Provide a response message on expectation match.
        /// </summary>
        /// <param name="response">Response message.</param>
        /// <returns>Same instance of <see cref="IMockPlug"/>.</returns>
        IMockPlug Returns(DreamMessage response);

        /// <summary>
        /// Provide a response message on expectation match.
        /// </summary>
        /// <param name="response">Response message.</param>
        /// <returns>Same instance of <see cref="IMockPlug"/>.</returns>
        IMockPlug Returns(XDoc response);

        /// <summary>
        /// Provide a response message on expectation match.
        /// </summary>
        /// <param name="response">Response message.</param>
        /// <returns>Same instance of <see cref="IMockPlug"/>.</returns>
        IMockPlug Returns(Func<MockPlugInvocation, DreamMessage> response);

        /// <summary>
        /// Provide a response header on expectation match.
        /// </summary>
        /// <param name="key">Header key.</param>
        /// <param name="value">Header value.</param>
        /// <returns>Same instance of <see cref="IMockPlug"/>.</returns>
        IMockPlug WithResponseHeader(string key, string value);

        /// <summary>
        /// Verify that the <see cref="MockPlug"/> was called as expected.
        /// </summary>
        /// <remarks>
        /// Uses a 5 second timeout and will return immediately if <see cref="ExpectAtLeastOneCall"/> 
        /// or <see cref="ExpectCalls"/> was called previously.
        /// </remarks>
        void Verify();

        /// <summary>
        /// Verify that the <see cref="MockPlug"/> was called the expected number of <see cref="Times"/>
        /// </summary>
        /// <remarks>Uses a 5 second timeout.</remarks>
        /// <param name="times">Times instance to use for expectations.</param>
        void Verify(Times times);

        /// <summary>
        /// Verify that the <see cref="MockPlug"/> was called as expected.
        /// </summary>
        /// <remarks>
        /// Will return immediately if <see cref="ExpectAtLeastOneCall"/> or <see cref="ExpectCalls"/> was called previously.
        /// </remarks>
        /// <param name="timeout">The time to wait for expectations to be met.</param>
        void Verify(TimeSpan timeout);

        /// <summary>
        /// Verify that the <see cref="MockPlug"/> was called the expected number of <see cref="Times"/>
        /// </summary>
        /// <param name="timeout">The time to wait for expectations to be met.</param>
        /// <param name="times">Times instance to use for expectations.</param>
        void Verify(TimeSpan timeout, Times times);

        /// <summary>
        /// Set expectations to be at least one call.
        /// </summary>
        /// <returns></returns>
        IMockPlug ExpectAtLeastOneCall();

        /// <summary>
        /// Set expectations to be the specified <see cref="Times"/>.
        /// </summary>
        /// <param name="called"></param>
        /// <returns></returns>
        IMockPlug ExpectCalls(Times called);
    }
}