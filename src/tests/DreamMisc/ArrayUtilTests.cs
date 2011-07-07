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
using System.Diagnostics;
using System.Linq;
using log4net;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class ArrayUtilTests {

        //--- Fields ---
        private ILog _log = LogUtils.CreateLog();

        //--- Methods ---

        [Test]
        public void ToDictionary_with_overwriteDuplicate_set_to_false_does_not_throw_on_duplicates() {
            var list = new[] { 1, 2, 3, 4, 2 };
            var dictionary = list.ToDictionary(x => x, true);
            Assert.AreEqual(4, dictionary.Count);
        }

        [Test]
        public void ToDictionary_with_overwriteDuplicate_set_to_true_does_throw_on_duplicates() {
            var list = new[] { 1, 2, 3, 4, 2 };
            try {
                var dictionary = list.ToDictionary(x => x, false);
                Assert.Fail("didn't throw");
            } catch(ArgumentException) {
                return;
            }
        }

        [Test]
        public void Intersection_should_return_common_items() {
            int[] left = new int[] { 1, 2, 3 };
            int[] right = new int[] { 2, 5, 3 };
            int[] intersection = ArrayUtil.Intersect(left, right);
            Assert.AreEqual(2, intersection.Length);
            Assert.Contains(2, intersection);
            Assert.Contains(3, intersection);
        }

        [Test]
        public void Intersection_of_different_arrays_returns_empty_set() {
            int[] left = new int[] { 1, 2, 3 };
            int[] right = new int[] { 4, 5, 6 };
            int[] intersection = ArrayUtil.Intersect(left, right);
            Assert.AreEqual(0, intersection.Length);
        }

        [Test]
        public void Intersection_with_left_or_right_null_returns_empty_set() {
            int[] left = new int[] { 1, 2, 3 };
            int[] intersection = ArrayUtil.Intersect(left, null);
            Assert.AreEqual(0, intersection.Length);

        }

        [Test]
        public void Intersection_of_complex_types_based_on_comparison_delegate() {
            ComplexType[] left = new ComplexType[] {
                new ComplexType(1),
                new ComplexType(2),
                new ComplexType(3) 
            };
            ComplexType[] right = new ComplexType[] {
                new ComplexType(4),
                new ComplexType(5),
                new ComplexType(1) 
            };
            ComplexType[] intersection = ArrayUtil.Intersect(left, right, delegate(ComplexType a, ComplexType b) {
                return a.Value.CompareTo(b.Value);
            });
            Assert.AreEqual(1, intersection.Length);
            Assert.AreEqual(1, intersection[0].Value);
        }

        [Test]
        public void Intersection_of_complex_types_comparing_instances_results_in_empty_set() {
            ComplexType[] left = new ComplexType[] {
                new ComplexType(1),
                new ComplexType(2),
                new ComplexType(3) 
            };
            ComplexType[] right = new ComplexType[] {
                new ComplexType(4),
                new ComplexType(5),
                new ComplexType(1) 
            };
            ComplexType[] intersection = ArrayUtil.Intersect(left, right, delegate(ComplexType a, ComplexType b) {
                return ReferenceEquals(a, b) ? 0 : 1;
            });
            Assert.AreEqual(0, intersection.Length);
        }

        [Test]
        public void Union_should_return_items_from_both_collections() {
            int[] left = new int[] { 1, 2, 3 };
            int[] right = new int[] { 4, 5, 6 };
            int[] union = ArrayUtil.Union(left, right);
            Assert.AreEqual(6, union.Length);
            Assert.AreEqual(union, new int[] { 1, 2, 3, 4, 5, 6 });
        }

        [Test]
        public void Union_should_remove_duplicates_from_either_collection() {
            int[] left = new int[] { 1, 2, 1 };
            int[] right = new int[] { 2, 3, 3 };
            int[] union = ArrayUtil.Union(left, right);
            Assert.AreEqual(3, union.Length);
            Assert.AreEqual(union, new int[] { 1, 2, 3 });
        }

        [Test]
        public void Union_with_left_or_right_null_returns_the_non_null_set() {
            int[] nonNull = new int[] { 1, 2, 3 };
            int[] union = ArrayUtil.Union(nonNull, null);
            Assert.AreEqual(3, union.Length);
            Assert.AreEqual(nonNull, union);
            union = ArrayUtil.Union(null, nonNull);
            Assert.AreEqual(3, union.Length);
            Assert.AreEqual(nonNull, union);
        }

        [Test]
        public void Union_with_both_null_returns_empty_set() {
            int[] union = ArrayUtil.Union((int[])null, null);
            Assert.AreEqual(0, union.Length);
        }

        [Test]
        public void Union_of_complex_types_based_on_comparison_delegate() {
            ComplexType[] left = new ComplexType[] {
                new ComplexType(1),
                new ComplexType(2),
                new ComplexType(3) 
            };
            ComplexType[] right = new ComplexType[] {
                new ComplexType(4),
                new ComplexType(5),
                new ComplexType(1) 
            };
            ComplexType[] union = ArrayUtil.Union(left, right, delegate(ComplexType a, ComplexType b) {
                return a.Value.CompareTo(b.Value);
            });
            Assert.AreEqual(5, union.Length);
            Assert.AreEqual(1, union[0].Value);
        }

        [Test]
        public void Union_of_complex_types_comparing_instances_results_in_all_values() {
            ComplexType[] left = new ComplexType[] {
                new ComplexType(1),
                new ComplexType(2),
                new ComplexType(3) 
            };
            ComplexType[] right = new ComplexType[] {
                new ComplexType(4),
                new ComplexType(5),
                new ComplexType(1) 
            };
            ComplexType[] union = ArrayUtil.Union(left, right, delegate(ComplexType a, ComplexType b) {
                return ReferenceEquals(a, b) ? 0 : 1;
            });
            Assert.AreEqual(6, union.Length);
        }

        private class ComplexType {
            public readonly int Value;
            public ComplexType(int value) {
                Value = value;
            }
        }

        [Test]
        public void Can_diff_two_arrays() {
            var a = new[] { "a", "b", "c", "d" };
            var b = new[] { "a", "x", "b", "d" };
            var diff = ArrayUtil.Diff(a, b, 5, (x, y) => x.Equals(y));
            var manualDiff = new[] {
                new Tuplet<ArrayDiffKind, string>(ArrayDiffKind.Same, "a"),
                new Tuplet<ArrayDiffKind, string>(ArrayDiffKind.Added, "x"),
                new Tuplet<ArrayDiffKind, string>(ArrayDiffKind.Same, "b"),
                new Tuplet<ArrayDiffKind, string>(ArrayDiffKind.Removed, "c"),
                new Tuplet<ArrayDiffKind, string>(ArrayDiffKind.Same, "d"),
            };
            Assert.AreEqual(manualDiff.Length, diff.Length);
            for(var i = 0; i < diff.Length; i++) {
                Assert.AreEqual(manualDiff[i].Item1, diff[i].Item1);
                Assert.AreEqual(manualDiff[i].Item2, diff[i].Item2);
            }
        }

        [Test, Ignore]
        public void Diff_performance_test_25000_delta() {
            var before = "the quick brown fox jumped over the lazy dog".Split(' ');
            var after = Enumerable.Range(0, 25000).Select(x => StringUtil.CreateAlphaNumericKey(4)).ToArray();
            var sw = Stopwatch.StartNew();
            ArrayUtil.Diff(before, after, int.MaxValue, null);
            sw.Stop();
            _log.DebugFormat("Time: {0:#,##0.00}s", sw.Elapsed.TotalSeconds);
        }
    }
}
