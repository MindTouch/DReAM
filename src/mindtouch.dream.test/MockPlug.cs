/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2013 MindTouch, Inc.
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using log4net;
using MindTouch.Dream.Test.Mock;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.Test {

    /// <summary>
    /// Provides a mocking framework for intercepting <see cref="Plug"/> calls.
    /// </summary>
    /// <remarks>
    /// Meant to be used to test services without having to set up dependent remote endpoints the service relies on for proper execution.
    /// <see cref="MockPlug"/> provides 3 different mechanisms for mocking an endpoint:
    /// <list type="bullet">
    /// <item>
    /// <see cref="MockPlug"/> endpoints, which can match requests based on content and supply a reply. These endpoints are order independent and
    /// can be set up to verifiable.
    /// </item>
    /// <item>
    /// <see cref="AutoMockPlug"/> endpoints, which are order dependent and provide an Arrange/Act/Assert workflow for validating calls.
    /// </item>
    /// <item>
    /// <see cref="MockInvokeDelegate"/> endpoints which redirect an intercepted Uri (and child paths) to a delegate to be handled as the
    /// desired by the delegate implementor.
    /// </item>
    /// </list>
    /// </remarks>
    public class MockPlug : IMockPlug {

        //--- Types ---
        internal interface IMockInvokee {

            //--- Methods ---
            void Invoke(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response);

            //--- Properties ---
            int EndPointScore { get; }
            XUri Uri { get; }
        }

        internal class MockInvokee : IMockInvokee {

            //--- Fields ---
            private readonly XUri _uri;
            private readonly MockInvokeDelegate _callback;
            private readonly int _endpointScore;

            //--- Constructors ---
            public MockInvokee(XUri uri, MockInvokeDelegate callback, int endpointScore) {
                _uri = uri;
                _callback = callback;
                _endpointScore = endpointScore;
            }

            //--- Properties ---
            public int EndPointScore { get { return _endpointScore; } }
            public XUri Uri { get { return _uri; } }

            //--- Methods ---
            public void Invoke(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
                _callback(plug, verb, uri, request, response);
            }
        }


        //--- Delegates ---

        /// <summary>
        /// Delegate for registering a callback on Uri/Child Uri interception via <see cref="MockPlug.Register(MindTouch.Dream.XUri,MindTouch.Dream.Test.MockPlug.MockInvokeDelegate)"/>.
        /// </summary>
        /// <param name="plug">Invoking plug instance.</param>
        /// <param name="verb">Request verb.</param>
        /// <param name="uri">Request uri.</param>
        /// <param name="request">Request message.</param>
        /// <param name="response">Synchronization handle for response message.</param>
        public delegate void MockInvokeDelegate(Dream.Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response);

        //--- Class Fields ---
        private static readonly Dictionary<string, List<MockPlug>> _mocks = new Dictionary<string, List<MockPlug>>();
        private static readonly ILog _log = LogUtils.CreateLog();
        private static int _setupcounter = 0;

        //--- Class Properties ---

        /// <summary>
        /// The default base Uri that will return a <see cref="DreamMessage.Ok(MindTouch.Xml.XDoc)"/> for any request. Should be used as no-op endpoint.
        /// </summary>
        public static readonly XUri DefaultUri = new XUri(MockEndpoint.DEFAULT);

        //--- Class Methods ---

        /// <summary>
        /// Register a callback to intercept any calls to a uri and its child paths.
        /// </summary>
        /// <param name="uri">Base Uri to intercept.</param>
        /// <param name="mock">Interception callback.</param>
        public static void Register(XUri uri, MockInvokeDelegate mock) {
            Register(uri, mock, int.MaxValue);
        }

        /// <summary>
        /// Register a callback to intercept any calls to a uri and its child paths.
        /// </summary>
        /// <param name="uri">Base Uri to intercept.</param>
        /// <param name="mock">Interception callback.</param>
        /// <param name="endpointScore">The score to return to <see cref="IPlugEndpoint.GetScoreWithNormalizedUri"/> for this uri.</param>
        public static void Register(XUri uri, MockInvokeDelegate mock, int endpointScore) {
            MockEndpoint.Instance.Register(new MockInvokee(uri, mock, endpointScore));
        }

        /// <summary>
        /// Create an <see cref="AutoMockPlug"/> instance to intercept calls to a uri and its child paths for for Arrange/Act/Assert style mocking.
        /// </summary>
        /// <param name="uri">Base Uri to intercept.</param>
        /// <returns>A new interceptor instance responsible for the uri.</returns>
        public static AutoMockPlug Register(XUri uri) {
            return new AutoMockPlug(uri);
        }

        /// <summary>
        /// Setup a new <see cref="MockPlug"/> interceptor candidate for a uri and its child paths.
        /// </summary>
        /// <remarks>
        /// This mechanism has not been completed and is only a WIP.
        /// Must further configure ordered <see cref="IMockInvokeExpectationParameter"/> parameters to make validation possible.
        /// </remarks>
        /// <param name="baseUri">Base Uri to intercept.</param>
        /// <returns>A new interceptor instance that may intercept the uri, depending on its additional matching parameters.</returns>
        public static IMockPlug Setup(string baseUri) {
            return Setup(new XUri(baseUri));
        }

        /// <summary>
        /// Setup a new <see cref="MockPlug"/> interceptor candidate for a uri and its child paths.
        /// </summary>
        /// <remarks>
        /// This mechanism has not been completed and is only a WIP.
        /// Must further configure ordered <see cref="IMockInvokeExpectationParameter"/> parameters to make validation possible.
        /// </remarks>
        /// <param name="baseUri">Base Uri to intercept.</param>
        /// <param name="name">Debug name for setup</param>
        /// <returns>A new interceptor instance that may intercept the uri, depending on its additional matching parameters.</returns>
        public static IMockPlug Setup(string baseUri, string name) {
            return Setup(new XUri(baseUri), name);
        }

        /// <summary>
        /// Setup a new <see cref="MockPlug"/> interceptor candidate for a uri and its child paths.
        /// </summary>
        /// <remarks>
        /// This mechanism has not been completed and is only a WIP.
        /// Must further configure ordered <see cref="IMockInvokeExpectationParameter"/> parameters to make validation possible.
        /// </remarks>
        /// <param name="baseUri">Base Uri to intercept.</param>
        /// <returns>A new interceptor instance that may intercept the uri, depending on its additional matching parameters.</returns>
        public static IMockPlug Setup(XUri baseUri) {
            _setupcounter++;
            return Setup(baseUri, "Setup#" + _setupcounter, int.MaxValue);
        }

        /// <summary>
        /// Setup a new <see cref="MockPlug"/> interceptor candidate for a uri and its child paths.
        /// </summary>
        /// <remarks>
        /// This mechanism has not been completed and is only a WIP.
        /// Must further configure ordered <see cref="IMockInvokeExpectationParameter"/> parameters to make validation possible.
        /// Note: endPointScore is only set on the first set for a specific baseUri. Subsequent values are ignored.
        /// </remarks>
        /// <param name="baseUri">Base Uri to intercept.</param>
        /// <param name="endPointScore">The score to return to <see cref="IPlugEndpoint.GetScoreWithNormalizedUri"/> for this uri.</param>
        /// <returns>A new interceptor instance that may intercept the uri, depending on its additional matching parameters.</returns>
        public static IMockPlug Setup(XUri baseUri, int endPointScore) {
            _setupcounter++;
            return Setup(baseUri, "Setup#" + _setupcounter, endPointScore);
        }

        /// <summary>
        /// Setup a new <see cref="MockPlug"/> interceptor candidate for a uri and its child paths.
        /// </summary>
        /// <remarks>
        /// This mechanism has not been completed and is only a WIP.
        /// Must further configure ordered <see cref="IMockInvokeExpectationParameter"/> parameters to make validation possible.
        /// </remarks>
        /// <param name="baseUri">Base Uri to intercept.</param>
        /// <param name="name">Debug name for setup</param>
        /// <returns>A new interceptor instance that may intercept the uri, depending on its additional matching parameters.</returns>
        public static IMockPlug Setup(XUri baseUri, string name) {
            return Setup(baseUri, name, int.MaxValue);
        }

        /// <summary>
        /// Setup a new <see cref="MockPlug"/> interceptor candidate for a uri and its child paths.
        /// </summary>
        /// <remarks>
        /// This mechanism has not been completed and is only a WIP.
        /// Must further configure ordered <see cref="IMockInvokeExpectationParameter"/> parameters to make validation possible.
        /// Note: endPointScore is only set on the first set for a specific baseUri. Subsequent values are ignored.
        /// </remarks>
        /// <param name="baseUri">Base Uri to intercept.</param>
        /// <param name="name">Debug name for setup</param>
        /// <param name="endPointScore">The score to return to <see cref="IPlugEndpoint.GetScoreWithNormalizedUri"/> for this uri.</param>
        /// <returns>A new interceptor instance that may intercept the uri, depending on its additional matching parameters.</returns>
        public static IMockPlug Setup(XUri baseUri, string name, int endPointScore) {
            List<MockPlug> mocks;
            var key = baseUri.SchemeHostPortPath;
            lock(_mocks) {
                if(!_mocks.TryGetValue(key, out mocks)) {
                    mocks = new List<MockPlug>();
                    MockInvokeDelegate callback = (plug, verb, uri, request, response) => {
                        _log.DebugFormat("checking setups for match on {0}:{1}", verb, uri);
                        MockPlug bestMatch = null;
                        var matchScore = 0;
                        foreach(var match in mocks) {
                            var score = match.GetMatchScore(verb, uri, request);
                            if(score > matchScore) {
                                bestMatch = match;
                                matchScore = score;
                            }
                        }
                        if(bestMatch == null) {
                            _log.Debug("no match");
                            response.Return(DreamMessage.Ok(new XDoc("empty")));
                        } else {
                            _log.DebugFormat("[{0}] matched", bestMatch.Name);
                            response.Return(bestMatch.Invoke(verb, uri, request));
                        }
                    };
                    MockEndpoint.Instance.Register(new MockInvokee(baseUri, callback, endPointScore));
                    MockEndpoint.Instance.AllDeregistered += Instance_AllDeregistered;
                    _mocks.Add(key, mocks);
                }
            }
            var mock = new MockPlug(baseUri, name);
            mocks.Add(mock);
            return mock;
        }

        static void Instance_AllDeregistered(object sender, EventArgs e) {
            lock(_mocks) {
                _mocks.Clear();
            }
        }

        /// <summary>
        /// Verify all <see cref="MockPlug"/> instances created with <see cref="Setup(MindTouch.Dream.XUri)"/> since the last <see cref="DeregisterAll"/> call.
        /// </summary>
        /// <remarks>
        /// Uses a 10 second timeout.
        /// </remarks>
        public static void VerifyAll() {
            VerifyAll(TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Verify all <see cref="MockPlug"/> instances created with <see cref="Setup(MindTouch.Dream.XUri)"/> since the last <see cref="DeregisterAll"/> call.
        /// </summary>
        /// <param name="timeout">Time to wait for all expectations to be met.</param>
        public static void VerifyAll(TimeSpan timeout) {
            var verifiable = (from mocks in _mocks.Values
                              from mock in mocks
                              where mock.IsVerifiable
                              select mock);
            foreach(var mock in verifiable) {
                var stopwatch = Stopwatch.StartNew();
                ((IMockPlug)mock).Verify(timeout);
                stopwatch.Stop();
                timeout = timeout.Subtract(stopwatch.Elapsed);
                if(timeout.TotalMilliseconds < 0) {
                    timeout = TimeSpan.Zero;
                }
            }
        }

        /// <summary>
        /// Deregister all interceptors for a specific base uri
        /// </summary>
        /// <remarks>
        /// This will not deregister an interceptor that was registered specifically for a uri that is a child path of the provided uri.
        /// </remarks>
        /// <param name="uri">Base Uri to intercept.</param>
        public static void Deregister(XUri uri) {
            MockEndpoint.Instance.Deregister(uri);
        }

        /// <summary>
        /// Deregister all interceptors.
        /// </summary>
        public static void DeregisterAll() {
            MockEndpoint.Instance.DeregisterAll();
            _setupcounter = 0;
        }

        //--- Fields ---

        /// <summary>
        /// Name for the Mock Plug for debug logging purposes.
        /// </summary>
        public readonly string Name;

        private readonly AutoResetEvent _called = new AutoResetEvent(false);
        private readonly List<Tuplet<string, Predicate<string>>> _queryMatchers = new List<Tuplet<string, Predicate<string>>>();
        private readonly List<Tuplet<string, Predicate<string>>> _headerMatchers = new List<Tuplet<string, Predicate<string>>>();
        private readonly DreamHeaders _headers = new DreamHeaders();
        private readonly DreamHeaders _responseHeaders = new DreamHeaders();
        private XUri _uri;
        private string _verb = "*";
        private XDoc _request;
        private Func<DreamMessage, bool> _requestCallback;
        private DreamMessage _response;
        private Func<MockPlugInvocation, DreamMessage> _responseCallback;
        private int _times;
        private Times _verifiable;
        private bool _matchTrailingSlashes;

        //--- Constructors ---
        private MockPlug(XUri uri, string name) {
            _uri = uri;
            Name = name;
        }

        //--- Properties ---
        /// <summary>
        /// Used by <see cref="VerifyAll()"/> to determine whether instance should be included in verification.
        /// </summary>
        public bool IsVerifiable { get { return _verifiable != null; } }

        //--- Methods ---
        private int GetMatchScore(string verb, XUri uri, DreamMessage request) {
            var score = 0;
            if(verb.EqualsInvariantIgnoreCase(_verb)) {
                score = 1;
            } else if(_verb != "*") {
                return 0;
            }
            var path = _matchTrailingSlashes ? _uri.Path : _uri.WithoutTrailingSlash().Path;
            var incomingPath = _matchTrailingSlashes ? uri.Path : uri.WithoutTrailingSlash().Path;
            if(!incomingPath.EqualsInvariantIgnoreCase(path)) {
                return 0;
            }
            score++;
            if(_uri.Params != null) {
                foreach(var param in _uri.Params) {
                    var v = uri.GetParam(param.Key);
                    if(v == null || !v.EndsWithInvariantIgnoreCase(param.Value)) {
                        return 0;
                    }
                    score++;
                }
            }
            foreach(var matcher in _queryMatchers) {
                var v = uri.GetParam(matcher.Item1);
                if(v == null || !matcher.Item2(v)) {
                    return 0;
                }
                score++;
            }
            foreach(var matcher in _headerMatchers) {
                var v = request.Headers[matcher.Item1];
                if(string.IsNullOrEmpty(v) || !matcher.Item2(v)) {
                    return 0;
                }
                score++;
            }
            foreach(var header in _headers) {
                var v = request.Headers[header.Key];
                if(string.IsNullOrEmpty(v) || !v.EqualsInvariant(header.Value)) {
                    return 0;
                }
                score++;
            }
            if(_requestCallback != null) {
                if(!_requestCallback(request)) {
                    return 0;
                }
            } else if(_request != null && (!request.HasDocument || _request != request.ToDocument())) {
                return 0;
            }
            score++;
            return score;
        }

        private DreamMessage Invoke(string verb, XUri uri, DreamMessage request) {
            _times++;
            if(_responseCallback != null) {
                var response = _responseCallback(new MockPlugInvocation(verb, uri, request, _responseHeaders));
                _response = response;
            }
            if(_response == null) {
                _response = DreamMessage.Ok(new XDoc("empty"));
            }
            _response.Headers.AddRange(_responseHeaders);
            _called.Set();
            _log.DebugFormat("invoked {0}:{1}", verb, uri);
            return _response;
        }

        #region Implementation of IMockPlug
        IMockPlug IMockPlug.Verb(string verb) {
            _verb = verb;
            return this;
        }

        IMockPlug IMockPlug.At(string[] path) {
            _uri = _uri.At(path);
            return this;
        }

        IMockPlug IMockPlug.With(string key, string value) {
            _uri = _uri.With(key, value);
            return this;
        }

        IMockPlug IMockPlug.With(string key, Predicate<string> valueCallback) {
            _queryMatchers.Add(new Tuplet<string, Predicate<string>>(key, valueCallback));
            return this;
        }

        IMockPlug IMockPlug.WithTrailingSlash() {
            _uri = _uri.WithTrailingSlash();
            _matchTrailingSlashes = true;
            return this;
        }

        IMockPlug IMockPlug.WithoutTrailingSlash() {
            _uri = _uri.WithoutTrailingSlash();
            _matchTrailingSlashes = true;
            return this;
        }

        IMockPlug IMockPlug.WithBody(XDoc request) {
            _request = request;
            _requestCallback = null;
            return this;
        }

        IMockPlug IMockPlug.WithMessage(Func<DreamMessage, bool> requestCallback) {
            _requestCallback = requestCallback;
            _request = null;
            return this;
        }

        IMockPlug IMockPlug.WithHeader(string key, string value) {
            _headers[key] = value;
            return this;
        }

        IMockPlug IMockPlug.WithHeader(string key, Predicate<string> valueCallback) {
            _headerMatchers.Add(new Tuplet<string, Predicate<string>>(key, valueCallback));
            return this;
        }

        IMockPlug IMockPlug.Returns(DreamMessage response) {
            _response = response;
            return this;
        }

        IMockPlug IMockPlug.Returns(Func<MockPlugInvocation, DreamMessage> response) {
            _responseCallback = response;
            return this;
        }

        IMockPlug IMockPlug.Returns(XDoc response) {
            var status = _response == null ? DreamStatus.Ok : _response.Status;
            var headers = _response == null ? null : _response.Headers;
            _response = new DreamMessage(status, headers, response);
            return this;
        }

        IMockPlug IMockPlug.WithResponseHeader(string key, string value) {
            _responseHeaders[key] = value;
            return this;
        }

        void IMockPlug.Verify() {
            if(_verifiable == null) {
                return;
            }
            ((IMockPlug)this).Verify(TimeSpan.FromSeconds(5), _verifiable);
        }

        void IMockPlug.Verify(Times times) {
            ((IMockPlug)this).Verify(TimeSpan.FromSeconds(5), times);
        }

        IMockPlug IMockPlug.ExpectAtLeastOneCall() {
            _verifiable = Times.AtLeastOnce();
            return this;
        }

        IMockPlug IMockPlug.ExpectCalls(Times called) {
            _verifiable = called;
            return this;
        }

        void IMockPlug.Verify(TimeSpan timeout) {
            if(_verifiable == null) {
                return;
            }
            ((IMockPlug)this).Verify(timeout, _verifiable);
        }

        void IMockPlug.Verify(TimeSpan timeout, Times times) {
            while(true) {
                var verified = times.Verify(_times, timeout);
                if(verified == Times.Result.Ok) {
                    _log.DebugFormat("satisfied {0}", _uri);
                    return;
                }
                if(verified == Times.Result.TooMany) {
                    break;
                }

                // check if we have any time left to wait
                if(timeout.TotalMilliseconds < 0) {
                    break;
                }
                _log.DebugFormat("waiting on {0}:{1} with {2:0.00}ms left in timeout", _verb, _uri, timeout.TotalMilliseconds);
                var stopwatch = Stopwatch.StartNew();
                if(!_called.WaitOne(timeout)) {
                    break;
                }
                timeout = timeout.Subtract(stopwatch.Elapsed);
            }
            throw new MockPlugException(string.Format("[{0}] {1}:{2} was called {3} times before timeout.", Name, _verb, _uri, _times));
        }
        #endregion
    }

    /// <summary>
    /// Provides and Arrange/Act/Assert mocking framework for intercepting and handling <see cref="Plug"/> invocations.
    /// </summary>
    public class AutoMockPlug : MockPlug.IMockInvokee, IDisposable {

        //--- Delegates ---

        /// <summary>
        /// Delegate for custom expectations.
        /// </summary>
        /// <param name="verb">Request verb.</param>
        /// <param name="uri">Request uri.</param>
        /// <param name="request">Request message.</param>
        /// <param name="response">Response message output.</param>
        /// <param name="failureReason">Output for failure message, should the call not meet expectations.</param>
        /// <returns><see langword="False"/> if the call did not meet expectations of the callback.</returns>
        public delegate bool MockAutoInvokeDelegate(string verb, XUri uri, DreamMessage request, out DreamMessage response, out string failureReason);

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly XUri _baseUri;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        private readonly List<AutoMockInvokeExpectation> _expectations = new List<AutoMockInvokeExpectation>();
        private readonly List<ExcessInterception> _excess = new List<ExcessInterception>();
        private bool _failed = false;
        private int _current = 0;
        private int _index = 0;
        private string _failure;

        //--- Constructors ---
        internal AutoMockPlug(XUri baseUri) {
            _baseUri = baseUri;
            MockEndpoint.Instance.Register(this);
        }

        //--- Properties ---

        /// <summary>
        /// Base uri this instance is registered for.
        /// </summary>
        public XUri BaseUri { get { return _baseUri; } }

        /// <summary>
        /// <see langword="True"/> if after the Act phase has excess requests beyond set up expectations
        /// </summary>
        public bool HasInterceptsInExcessOfExpectations { get { return _excess.Count > 0; } }

        /// <summary>
        /// Total number of expectations set up.
        /// </summary>
        public int TotalExpectationCount { get { return _expectations.Count; } }

        /// <summary>
        /// Number of expecattions met.
        /// </summary>
        public int MetExpectationCount { get { return _current; } }

        /// <summary>
        /// Contains a text message detailing why <see cref="WaitAndVerify"/> failed.
        /// </summary>
        public string VerificationFailure { get { return _failure; } }

        /// <summary>
        /// Array of excess interceptions caught.
        /// </summary>
        public ExcessInterception[] ExcessInterceptions {
            get { return _excess.ToArray(); }
        }

        //--- Methods ---

        /// <summary>
        /// Set up an expectation from a chain of parameters.
        /// </summary>
        /// <remarks>
        /// <see cref="IMockInvokeExpectationParameter"/> is meant to be used as a fluent interface to set up parameter qualifications for the expecation.
        /// </remarks>
        /// <returns>A new expectation configuration instance.</returns>
        public IMockInvokeExpectationParameter Expect() {
            var expectation = new AutoMockInvokeExpectation(_baseUri, _index++);
            _expectations.Add(expectation);
            return expectation;
        }

        /// <summary>
        /// Expect a call at the base uri with the given verb, ignoring all other parameters.
        /// </summary>
        /// <param name="verb">Http verb to expect.</param>
        public void Expect(string verb) {
            Expect().Verb(verb);
        }

        /// <summary>
        /// Expect a call on the given uri with the given verb, ignoring all other parameters.
        /// </summary>
        /// <param name="verb">Http verb to expect.</param>
        /// <param name="uri">Uri to expect (must be a child path of <see cref="BaseUri"/>).</param>
        public void Expect(string verb, XUri uri) {
            Expect().Verb(verb).Uri(uri);
        }

        /// <summary>
        /// Expect a call on the given uri with the given verb and document, ignoring all other parameters.
        /// </summary>
        /// <param name="verb">Http verb to expect.</param>
        /// <param name="uri">Uri to expect (must be a child path of <see cref="BaseUri"/>).</param>
        /// <param name="requestDoc">Expected request document.</param>
        public void Expect(string verb, XUri uri, XDoc requestDoc) {
            Expect().Verb(verb).Uri(uri).RequestDocument(requestDoc);
        }

        /// <summary>
        /// Expect a call on the given uri with the given verb and document.
        /// </summary>
        /// <param name="verb">Http verb to expect.</param>
        /// <param name="uri">Uri to expect (must be a child path of <see cref="BaseUri"/>).</param>
        /// <param name="requestDoc">Expected request document.</param>
        /// <param name="response">Response message to return.</param>
        public void Expect(string verb, XUri uri, XDoc requestDoc, DreamMessage response) {
            Expect().Verb(verb).Uri(uri).RequestDocument(requestDoc).Response(response);
        }

        /// <summary>
        /// Create an expecation delegated to a callback.
        /// </summary>
        /// <param name="autoInvokeDelegate">Callback.</param>
        public void Expect(MockAutoInvokeDelegate autoInvokeDelegate) {
            AutoMockInvokeExpectation expectation = new AutoMockInvokeExpectation(autoInvokeDelegate);
            _expectations.Add(expectation);
        }

        /// <summary>
        /// Clear all expectations and status.
        /// </summary>
        public void Reset() {
            lock(this) {
                _expectations.Clear();
                _excess.Clear();
                _resetEvent.Reset();
                _failed = false;
                _index = 0;
                _current = 0;
                _failure = null;
            }
        }

        /// <summary>
        /// Block for expectations to be met, a bad expectation to come in or the timeout to expire.
        /// </summary>
        /// <param name="timeout">Wait timeout.</param>
        /// <returns><see langword="True"/> if all expectations were met.</returns>
        public bool WaitAndVerify(TimeSpan timeout) {
            var stopwatch = Stopwatch.StartNew();
            if(_expectations.Count == 0) {
                Thread.Sleep((int)timeout.TotalMilliseconds - 200);
            } else {
                bool waitResult = _resetEvent.WaitOne(timeout, false);
                if(!waitResult) {
                    if(_current == 0) {
                        AddFailure("None of the {0} expectations were called", _index);
                    } else {
                        AddFailure("Only {0} out of {1} expectations were called", _current, _index);
                    }
                }
            }
            stopwatch.Stop();
            if(string.IsNullOrEmpty(_failure)) {

                // wait at least 1000 milliseconds, or half as long as things took to executed so far
                // this should prevent excess expectations on a slow run be missed
                Thread.Sleep(Math.Max(1000, (int)(stopwatch.ElapsedMilliseconds / 2)));
                if(HasInterceptsInExcessOfExpectations) {
                    AddFailure("Excess expectations found");
                    return false;
                }
            }
            if(!string.IsNullOrEmpty(_failure)) {
                _log.DebugFormat(VerificationFailure);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Deregister this instance from uri interception.
        /// </summary>
        public void Dispose() {
            MockEndpoint.Instance.Deregister(_baseUri);
        }

        private void AddFailure(string format, params object[] args) {
            if(_failure == null) {
                _failure = string.Format("Expectations were unmet:\r\n");
            }
            _failure += string.Format(format, args) + "\r\n";
        }

        //--- MockPlug.IMockInvokee members ---
        int MockPlug.IMockInvokee.EndPointScore { get { return int.MaxValue; } }
        XUri MockPlug.IMockInvokee.Uri { get { return _baseUri; } }

        void MockPlug.IMockInvokee.Invoke(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
            lock(this) {
                if(_failed) {
                    _log.DebugFormat("we've already failed, no point checking more expectations");
                    response.Return(DreamMessage.InternalError());
                    return;
                }
                _log.DebugFormat("{0}={1}", verb, uri);
                XDoc requestDoc = request.HasDocument ? request.ToDocument() : null;
                if(_expectations.Count == _current) {
                    _log.DebugFormat("excess");
                    ExcessInterception excess = new ExcessInterception();
                    _excess.Add(excess);
                    ;
                    response.Return(excess.Call(verb, uri, requestDoc));
                    return;
                }
                AutoMockInvokeExpectation expectation = _expectations[_current];
                expectation.Call(verb, uri, request);
                if(!expectation.Verify()) {
                    AddFailure(_expectations[_current].VerificationFailure);
                    _log.DebugFormat("got failure, setting reset event ({0})", _current);
                    _failed = true;
                    _resetEvent.Set();
                    response.Return(DreamMessage.BadRequest("expectation failure"));
                    return;
                }
                _current++;
                _log.DebugFormat("expected");
                if(_expectations.Count == _current) {
                    _log.DebugFormat("setting reset event");
                    _resetEvent.Set();
                }
                response.Return(expectation.GetResponse());
            }
        }

    }

    /// <summary>
    /// Provides information about an excess request that occured after expecations had already been met.
    /// </summary>
    public class ExcessInterception {
        private string _verb;
        private XUri _uri;
        private XDoc _request;

        /// <summary>
        /// Request verb.
        /// </summary>
        public string Verb { get { return _verb; } }

        /// <summary>
        /// Request Uri.
        /// </summary>
        public XUri Uri { get { return _uri; } }

        /// <summary>
        /// Request document.
        /// </summary>
        public XDoc Request { get { return _request; } }

        internal DreamMessage Call(string verb, XUri uri, XDoc request) {
            _verb = verb;
            _uri = uri;
            _request = request;
            return DreamMessage.BadRequest("unexpected call");
        }
    }


    /// <summary>
    /// Provides a fluent interface for defining parameters of an <see cref="AutoMockPlug"/> expecation.
    /// </summary>
    public interface IMockInvokeExpectationParameter {

        /// <summary>
        /// Except a given verb.
        /// </summary>
        /// <param name="verb">Verb to expect.</param>
        /// <returns>Same instance of <see cref="IMockInvokeExpectationParameter"/>.</returns>
        IMockInvokeExpectationParameter Verb(string verb);

        /// <summary>
        /// Expect a given uri (must be a child of <see cref="AutoMockPlug.BaseUri"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>Same instance of <see cref="IMockInvokeExpectationParameter"/>.</returns>
        IMockInvokeExpectationParameter Uri(XUri uri);

        /// <summary>
        /// Modify the expected uri, to expec the call at the given relative path.
        /// </summary>
        /// <param name="path">Relative path to add to existing or base uri expectation.</param>
        /// <returns>Same instance of <see cref="IMockInvokeExpectationParameter"/>.</returns>
        IMockInvokeExpectationParameter At(string[] path);

        /// <summary>
        /// Expect query key/value pair.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>Same instance of <see cref="IMockInvokeExpectationParameter"/>.</returns>
        IMockInvokeExpectationParameter With(string key, string value);

        /// <summary>
        /// Expect a given request message (mutually exclusive to <see cref="RequestDocument(MindTouch.Xml.XDoc)"/> and <see cref="RequestDocument(System.Func{MindTouch.Xml.XDoc,bool})"/>).
        /// </summary>
        /// <param name="request">Expected message.</param>
        /// <returns>Same instance of <see cref="IMockInvokeExpectationParameter"/>.</returns>
        IMockInvokeExpectationParameter Request(DreamMessage request);

        /// <summary>
        /// Expect a given request document (mutually exclusive to <see cref="Request"/> and <see cref="RequestDocument(System.Func{MindTouch.Xml.XDoc,bool})"/>).
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Same instance of <see cref="IMockInvokeExpectationParameter"/>.</returns>
        IMockInvokeExpectationParameter RequestDocument(XDoc request);

        /// <summary>
        /// Register a callback function to perform custom expectation matching.
        /// </summary>
        /// <param name="requestCallback">Callback to determine whether the document matches expecations.</param>
        /// <returns>Same instance of <see cref="IMockInvokeExpectationParameter"/>.</returns>
        IMockInvokeExpectationParameter RequestDocument(Func<XDoc, bool> requestCallback);

        /// <summary>
        /// Expect the presence of a given header.
        /// </summary>
        /// <param name="key">Header key.</param>
        /// <param name="value">Header value.</param>
        /// <returns>Same instance of <see cref="IMockInvokeExpectationParameter"/>.</returns>
        IMockInvokeExpectationParameter RequestHeader(string key, string value);

        /// <summary>
        /// Provide a response message on expectation match.
        /// </summary>
        /// <param name="response">Response message.</param>
        /// <returns>Same instance of <see cref="IMockInvokeExpectationParameter"/>.</returns>
        IMockInvokeExpectationParameter Response(DreamMessage response);

        /// <summary>
        /// Provide a response header on expectation match.
        /// </summary>
        /// <param name="key">Header key.</param>
        /// <param name="value">Header value.</param>
        /// <returns>Same instance of <see cref="IMockInvokeExpectationParameter"/>.</returns>
        IMockInvokeExpectationParameter ResponseHeader(string key, string value);
    }

    internal class AutoMockInvokeExpectation : IMockInvokeExpectationParameter {

        //--- Static Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly AutoMockPlug.MockAutoInvokeDelegate _autoInvokeDelegate;
        private string _expectedVerb;
        private XUri _expectedUri;
        private XDoc _expectedRequestDoc;
        private Dictionary<string, string> _expectedRequestHeaders = new Dictionary<string, string>();
        private readonly XUri _baseUri;
        private readonly int _index;
        private DreamMessage _response = DreamMessage.Ok();
        private bool _called;
        private string _failure;
        private Func<XDoc, bool> _expectedRequestDocCallback;
        private DreamMessage _expectedRequest;

        //--- Properties ---
        public string VerificationFailure { get { return _failure; } }

        //--- Constructors ---
        public AutoMockInvokeExpectation(XUri baseUri, int index) {
            _baseUri = baseUri;
            _index = index;
        }

        public AutoMockInvokeExpectation(AutoMockPlug.MockAutoInvokeDelegate autoInvokeDelegate) {
            _autoInvokeDelegate = autoInvokeDelegate;
        }
        //--- Methods ---
        public IMockInvokeExpectationParameter Verb(string verb) {
            _expectedVerb = verb;
            return this;
        }

        public IMockInvokeExpectationParameter Uri(XUri uri) {
            _expectedUri = uri;
            return this;
        }

        public IMockInvokeExpectationParameter At(string[] path) {
            if(_expectedUri == null) {
                _expectedUri = _baseUri;
            }
            _expectedUri = _expectedUri.At(path);
            return this;
        }

        public IMockInvokeExpectationParameter With(string key, string value) {
            if(_expectedUri == null) {
                _expectedUri = _baseUri;
            }
            _expectedUri = _expectedUri.With(key, value);
            return this;
        }

        public IMockInvokeExpectationParameter Request(DreamMessage request) {
            _expectedRequest = request;
            foreach(KeyValuePair<string, string> pair in request.Headers) {
                RequestHeader(pair.Key, pair.Value);
            }
            return this;
        }

        public IMockInvokeExpectationParameter RequestDocument(XDoc request) {
            _expectedRequestDoc = request;
            return this;
        }

        public IMockInvokeExpectationParameter RequestDocument(Func<XDoc, bool> requestCallback) {
            _expectedRequestDocCallback = requestCallback;
            return this;
        }

        public IMockInvokeExpectationParameter RequestHeader(string key, string value) {
            _expectedRequestHeaders[key] = value;
            return this;
        }

        public IMockInvokeExpectationParameter Response(DreamMessage response) {
            _response = response;
            return this;
        }

        public IMockInvokeExpectationParameter ResponseHeader(string key, string value) {
            _response.Headers[key] = value;
            return this;
        }

        public DreamMessage GetResponse() { return _response; }

        public void Call(string verb, XUri uri, DreamMessage request) {
            _called = true;
            if(_autoInvokeDelegate != null) {
                DreamMessage response;
                string failure;
                if(!_autoInvokeDelegate(verb, uri, request, out response, out failure)) {
                    AddFailure(failure);
                } else {
                    _response = response;
                }
            }
            if(_expectedVerb != null && _expectedVerb != verb) {
                AddFailure("Expected verb '{0}', got '{1}'", _expectedVerb, verb);
            }
            if(_expectedUri != null && _expectedUri != uri) {
                AddFailure("Uri:\r\nExpected: {0}\r\nGot:      {1}", _expectedUri, uri);
            }
            if(_expectedRequest != null) {
                if(request.Status != _expectedRequest.Status) {
                    AddFailure("Status:\r\nExpected: {0}\r\nGot:      {1}", _expectedRequest.Status, request.Status);
                } else if(!request.ContentType.Match(_expectedRequest.ContentType)) {
                    AddFailure("Content type:\r\nExpected: {0}\r\nGot:      {1}", _expectedRequest.ContentType, request.ContentType);
                } else if(!StringUtil.EqualsInvariant(request.ToText(), _expectedRequest.ToText())) {
                    AddFailure("Content:\r\nExpected: {0}\r\nGot:      {1}", _expectedRequest.ToText(), request.ToText());
                }
            } else if(_expectedRequestDocCallback == null) {
                if(!request.HasDocument && _expectedRequestDoc != null) {
                    AddFailure("Expected a document in request, got none");
                } else if(request.HasDocument && _expectedRequestDoc != null && _expectedRequestDoc != request.ToDocument()) {
                    AddFailure("Content:\r\nExpected: {0}\r\nGot:      {1}", _expectedRequestDoc.ToString(), request.ToText());
                }
            } else {
                if(!request.HasDocument) {
                    AddFailure("Expected a document in request, got none for callback");
                } else if(request.HasDocument && !_expectedRequestDocCallback(request.ToDocument())) {
                    AddFailure("Request document'{0}', failed callback check", request.ToDocument());
                }
            }
            if(_expectedRequestHeaders.Count > 0) {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                foreach(KeyValuePair<string, string> header in request.Headers) {
                    if(_expectedRequestHeaders.ContainsKey(header.Key) && _expectedRequestHeaders[header.Key] != header.Value) {
                        AddFailure("Expected header '{0}:\r\nExpected: {1}\r\nGot:      {2}", header.Key, _expectedRequestHeaders[header.Key], header.Value);
                    }
                    headers[header.Key] = header.Value;
                }
                foreach(KeyValuePair<string, string> header in _expectedRequestHeaders) {
                    if(!headers.ContainsKey(header.Key)) {
                        AddFailure("Expected header '{0}', got none", header.Key);
                    }
                }
            }
        }

        public bool Verify() {
            if(!_called) {
                AddFailure("never triggered");
            }
            return _failure == null;
        }

        private void AddFailure(string format, params object[] args) {
            if(_failure == null) {
                _failure = string.Format("Expectation #{0}: ", _index + 1);
            } else {
                _failure += "; ";
            }
            string failure = string.Format(format, args);
            _log.DebugFormat("Expectation failure: {0}", failure);
            _failure += failure;
        }
    }
}