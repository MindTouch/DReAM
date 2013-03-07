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
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test {
    
    [TestFixture]
    public class AutoMockPlugTests {
        
        [TearDown]
        public void PerTestCleanup() {
            MockPlug.DeregisterAll();
        }

        [Test]
        public void Waits_until_expectations_are_met_after_each_reset() {
            AutoMockPlug autoPlug = MockPlug.Register(new XUri("http://auto/plug"));
            autoPlug.Expect("POST", new XUri("http://auto/plug/a"));
            autoPlug.Expect("PUT", new XUri("http://auto/plug/b"), new XDoc("foo"));
            AsyncUtil.Fork(() => {
                Plug.New("http://auto/plug/a").Post();
                Plug.New("http://auto/plug/b").Put(new XDoc("foo"));
            }, new Result());
            Assert.IsTrue(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(2)), autoPlug.VerificationFailure);
            autoPlug.Reset();
            autoPlug.Expect("GET", new XUri("http://auto/plug/c"));
            autoPlug.Expect("GET", new XUri("http://auto/plug/d"));
            AsyncUtil.Fork(() => {
                Plug.New("http://auto/plug/c").Get();
                Plug.New("http://auto/plug/d").Get();
            }, new Result());
            Assert.IsTrue(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(2)), autoPlug.VerificationFailure);
        }

        [Test]
        public void Collects_excess_expectations() {
            AutoMockPlug autoPlug = MockPlug.Register(new XUri("http://auto/plug"));
            autoPlug.Expect("POST");
            AsyncUtil.Fork(() => {
                Plug.New("http://auto/plug/a").PostAsync().Block();
                Plug.New("http://auto/plug/b").PostAsync().Block();
                Plug.New("http://auto/plug/c").PostAsync().Block();
                Plug.New("http://auto/plug/d").PostAsync().Block();
            }, new Result());
            Assert.IsFalse(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(15)), autoPlug.VerificationFailure);
            Assert.IsTrue(autoPlug.HasInterceptsInExcessOfExpectations);
            Assert.AreEqual(3, autoPlug.ExcessInterceptions.Length);
            Assert.AreEqual("http://auto/plug/b", autoPlug.ExcessInterceptions[0].Uri.ToString());
            Assert.AreEqual("http://auto/plug/c", autoPlug.ExcessInterceptions[1].Uri.ToString());
            Assert.AreEqual("http://auto/plug/d", autoPlug.ExcessInterceptions[2].Uri.ToString());
        }

        [Test]
        public void Complains_about_unmet_expectations_after_timeout() {
            AutoMockPlug autoPlug = MockPlug.Register(new XUri("http://auto/plug"));
            autoPlug.Expect("POST", new XUri("http://auto/plug/a"));
            autoPlug.Expect("PUT", new XUri("http://auto/plug/b"), new XDoc("foo"));
            Assert.IsFalse(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(1)), autoPlug.VerificationFailure);

            autoPlug.Reset();
            autoPlug.Expect("POST", new XUri("http://auto/plug/a"));
            autoPlug.Expect("PUT", new XUri("http://auto/plug/b"), new XDoc("foo"));
            AsyncUtil.Fork(() => Plug.New("http://auto/plug/a").Post(), new Result());
            Assert.IsFalse(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(1)), autoPlug.VerificationFailure);
            Assert.AreEqual(1, autoPlug.MetExpectationCount);
        }

        [Test]
        public void Considers_missordered_expectations_as_unmet() {
            AutoMockPlug autoPlug = MockPlug.Register(new XUri("http://auto/plugx"));
            autoPlug.Expect("POST", new XUri("http://auto/plugx/a"));
            autoPlug.Expect("PUT", new XUri("http://auto/plugx/b"), new XDoc("foo"));
            AsyncUtil.Fork(() => {
                Plug.New("http://auto/plugx/b").Put(new XDoc("foo"));
                Plug.New("http://auto/plugx/a").Post();
            }, new Result());
            Assert.IsFalse(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(1)), autoPlug.VerificationFailure);
            Assert.AreEqual(0, autoPlug.MetExpectationCount);
        }

        [Test]
        public void Autoplug_without_expectations_wait_for_timeout() {
            AutoMockPlug autoPlug = MockPlug.Register(new XUri("http://auto/plug"));
            Assert.IsTrue(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(1)), autoPlug.VerificationFailure);
        }

        [Test]
        public void Autoplug_without_expectations_should_still_fail_on_excess() {
            AutoMockPlug autoPlug = MockPlug.Register(new XUri("http://auto/plug"));
            AsyncUtil.Fork(() => {
                Plug.New("http://auto/plug/b").PutAsync(new XDoc("foo"));
                Plug.New("http://auto/plug/a").PostAsync();
            }, new Result());
            Assert.IsFalse(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(1)), autoPlug.VerificationFailure);
            Assert.AreEqual(2, autoPlug.ExcessInterceptions.Length);
        }

        [Test]
        public void Excess_expectations_are_BadRequest() {
            AutoMockPlug autoPlug = MockPlug.Register(new XUri("http://auto/plug"));
            DreamMessage message = Plug.New("http://auto/plug/a").GetAsync().Wait();
            Assert.IsFalse(message.IsSuccessful);
            Assert.AreEqual(DreamStatus.BadRequest, message.Status);
            Assert.IsFalse(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(1)), autoPlug.VerificationFailure);
            Assert.AreEqual(1, autoPlug.ExcessInterceptions.Length);
        }

        [Test]
        public void Can_match_request_DreamMessages_for_expectation() {
            AutoMockPlug autoPlug = MockPlug.Register(new XUri("http://auto/plug"));
            autoPlug.Expect().Verb("POST").Uri(new XUri("http://auto/plug/a")).Request(DreamMessage.Ok(MimeType.TEXT_UTF8, "blah"));
            AsyncUtil.Fork(() => Plug.New("http://auto/plug/a").PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, "blah")), new Result());
            Assert.IsTrue(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(1)), autoPlug.VerificationFailure);
        }

        [Test]
        public void Catches_mismatched_request_headers() {
            AutoMockPlug autoPlug = MockPlug.Register(new XUri("http://auto/plug"));
            autoPlug.Expect().Verb("GET").Uri(new XUri("http://auto/plug/a"))
                .RequestHeader("Foo", "123")
                .RequestHeader("Bar", "right");
            AsyncUtil.Fork(() => Plug.New("http://auto/plug/a").WithHeader("Foo", "123").WithHeader("Bar", "wrong").GetAsync(), new Result());
            Assert.IsFalse(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(1)), autoPlug.VerificationFailure);
            Assert.AreEqual("Expectations were unmet:\r\nExpectation #1: Expected header 'Bar:\r\nExpected: right\r\nGot:      wrong\r\n", autoPlug.VerificationFailure);
        }

        [Test]
        public void Catches_missing_headers() {
            AutoMockPlug autoPlug = MockPlug.Register(new XUri("http://auto/plug"));
            autoPlug.Expect().Verb("GET").Uri(new XUri("http://auto/plug/a"))
                .RequestHeader("Foo", "123")
                .RequestHeader("Bar", "required");
            AsyncUtil.Fork(() => Plug.New("http://auto/plug/a").WithHeader("Foo", "123").GetAsync(), new Result());
            Assert.IsFalse(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(1)), autoPlug.VerificationFailure);
            Assert.AreEqual("Expectations were unmet:\r\nExpectation #1: Expected header 'Bar', got none\r\n", autoPlug.VerificationFailure);
        }

        [Test]
        public void Ignores_excess_headers() {
            AutoMockPlug autoPlug = MockPlug.Register(new XUri("http://auto/plug"));
            autoPlug.Expect().Verb("POST").Uri(new XUri("http://auto/plug/a"));
            AsyncUtil.Fork(() => Plug.New("http://auto/plug/a").WithHeader("Foo", "123").PostAsync(), new Result());
            Assert.IsTrue(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(1)), autoPlug.VerificationFailure);
        }

        [Test]
        public void Matches_request_doc_to_expectations() {
            AutoMockPlug autoPlug = MockPlug.Register(new XUri("http://auto/plug"));
            autoPlug.Expect().Verb("POST").Uri(new XUri("http://auto/plug/a")).RequestDocument(new XDoc("foo"));
            AsyncUtil.Fork(() => Plug.New("http://auto/plug/a").PostAsync(new XDoc("foo")), new Result());
            Assert.IsTrue(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(1)), autoPlug.VerificationFailure);
        }

        [Test]
        public void Should_be_able_to_call_same_url_with_different_headers() {
            AutoMockPlug autoPlug = MockPlug.Register(new XUri("http://auto/plug"));
            autoPlug.Expect().Verb("GET").Uri(new XUri("http://auto/plug/a")).RequestHeader("Foo", "");
            autoPlug.Expect().Verb("GET").Uri(new XUri("http://auto/plug/a")).RequestHeader("Foo", "baz");
            Plug p = Plug.New("http://auto/plug/a");
            AsyncUtil.Fork(() => {
                p.WithHeader("Foo", "").GetAsync().Block();
                p.WithHeader("Foo", "baz").GetAsync().Block();
            }, new Result());
            Assert.IsTrue(autoPlug.WaitAndVerify(TimeSpan.FromSeconds(1)), autoPlug.VerificationFailure);
        }
    }
}
