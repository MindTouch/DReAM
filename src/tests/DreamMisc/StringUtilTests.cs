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
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class StringUtilTests {

        [Test]
        public void IfNullOrEmpty_null() {
            string before = null;
            string after = before.IfNullOrEmpty("bye");
            Assert.AreEqual("bye", after);
        }

        [Test]
        public void IfNullOrEmpty_empty() {
            string before = string.Empty;
            string after = before.IfNullOrEmpty("bye");
            Assert.AreEqual("bye", after);
        }

        [Test]
        public void IfNullOrEmpty_value() {
            string before = "hi";
            string after = before.IfNullOrEmpty("bye");
            Assert.AreEqual("hi", after);
        }

        [Test]
        public void ReplaceAll_with_no_substitions() {
            string before = "This is a string to replace stuff in.";
            string after = before.ReplaceAll();
            Assert.IsTrue(ReferenceEquals(before, after), "returned string reference different from source string reference");
        }

        [Test]
        public void ReplaceAll_with_no_substitions_ignore_case() {
            string before = "This is a string to replace stuff in.";
            string after = before.ReplaceAll(StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(ReferenceEquals(before, after), "returned string reference different from source string reference");
        }

        [Test]
        public void ReplaceAll_with_two_substitutions() {
            string before = "This is a string to replace stuff in.";
            string after = before.ReplaceAll(
                "a string", "text",
                "replace", "add"
            );
            Assert.AreEqual("This is text to add stuff in.", after);
        }

        [Test]
        public void ReplaceAll_with_two_substitutions_that_should_be_non_transitive() {
            string before = "This is a string to replace text in.";
            string after = before.ReplaceAll(
                "a string", "text",
                "text", "stuff"
            );
            Assert.AreEqual("This is text to replace stuff in.", after);
        }

        [Test]
        public void ReplaceAll_with_two_substitutions_ignore_case() {
            string before = "This is a string to replace stuff in.";
            string after = before.ReplaceAll(
                StringComparison.OrdinalIgnoreCase,
                "A STRING", "text",
                "REPLACE", "add"
            );
            Assert.AreEqual("This is text to add stuff in.", after);
        }

        [Test]
        public void ReplaceAll_with_two_substitutions_that_should_be_non_transitive_ignore_case() {
            string before = "This is a string to replace text in.";
            string after = before.ReplaceAll(
                StringComparison.OrdinalIgnoreCase,
                "A STRING", "text",
                "TEXT", "stuff"
            );
            Assert.AreEqual("This is text to replace stuff in.", after);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ReplaceAll_with_uneven_substitutions() {
            string before = "This is a string to replace stuff in.";
            string after = before.ReplaceAll(
                "a string", "text",
                "replace"
            );
        }
    }
}
