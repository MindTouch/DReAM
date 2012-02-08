/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2012 MindTouch, Inc.
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
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Reflection;
using log4net;

using MindTouch.Dream;
using MindTouch.Data;

namespace MindTouch.Dream.Test {

    internal class TestDataUpdater : ADataUpdater {

        //--- Constructors ---
        public TestDataUpdater(string version) {
            if(string.IsNullOrEmpty(version)) {
                _targetVersion = null;
            } else {
                _targetVersion = new VersionInfo(version);
                if(!_targetVersion.IsValid) {
                    throw new VersionInfoException(_targetVersion);
                }
            }
        }

        //--- Methods ---
        public override void TestConnection() { }
    }

    [DataUpgrade]
    internal class DummyUpgradeClass {

        //--- Types ---
        public struct Method : IComparable<Method> {
            public string _methodName;
            public VersionInfo _version;
            public string[] _args;

            public Method(string name, VersionInfo version) {
                _methodName = name;
                _version = version;
                _args = null;
            }

            public Method(string name, VersionInfo version, string[] args) {
                _methodName = name;
                _version = version;
                _args = args;
            }

            public int CompareTo(Method other) {
                var otherVersion = other._version;
                var change = _version.CompareTo(otherVersion).Change;
                switch(change) {
                case VersionChange.None:
                    return _methodName.CompareTo(other._methodName);
                case VersionChange.Upgrade:
                    return 1;
                default:
                    return -1;
                }
            }
        }

        public static List<Method> ExecutedMethods = new List<Method>();

        public void CustomMethod1() {
            ExecutedMethods.Add(new Method("CustomMethod1", null));
        }

        public void CustomMethod2(params string[] args) {
            ExecutedMethods.Add(new Method("CustomMethod2", null, args));
        }

        [DataIntegrityCheck("11.0.0")]
        public void DataIntegrityMethod1() {
            ExecutedMethods.Add(new Method("DataIntegrityMethod1", new VersionInfo("11.0.0")));
        }

        [EffectiveVersion("10.0.0")]
        public void UpgradeMethod1() {
            ExecutedMethods.Add(new Method("UpgradeMethod1", new VersionInfo("10.0.0")));
        }

        [EffectiveVersion("10.0.1")]
        public void UpgradeMethod2() {
            ExecutedMethods.Add(new Method("UpgradeMethod2", new VersionInfo("10.0.1")));
        }

        [EffectiveVersion("10.0.0")]
        public void UpgradeMethod3() {
            ExecutedMethods.Add(new Method("UpgradeMethod3", new VersionInfo("10.0.0")));
        }

        [EffectiveVersion("9.0.0")]
        public void UpgradeMethod4() {
            ExecutedMethods.Add(new Method("UpgradeMethod4", new VersionInfo("9.0.0")));
        }

        [EffectiveVersion("11.0.0")]
        public void UpgradeMethod5() {
            ExecutedMethods.Add(new Method("UpgradeMethod5", new VersionInfo("11.0.0")));
        }

        [EffectiveVersion("11.3.0")]
        public void UpgradeMethod6() {
            ExecutedMethods.Add(new Method("UpgradeMethod6", new VersionInfo("11.3.0")));
        }

        [EffectiveVersion("9.8.0")]
        public void UpgradeMethod7() {
            ExecutedMethods.Add(new Method("UpgradeMethod7", new VersionInfo("9.8.0")));
        }

        [EffectiveVersion("11.0.3")]
        public void UpgradeMethod8() {
            ExecutedMethods.Add(new Method("UpgradeMethod8", new VersionInfo("11.0.3")));
        }
    }

    [TestFixture]
    public class DataUpdaterTests {

        //--- Fields ---

        private Assembly _testAssembly;
        private TestDataUpdater _dataUpdater;

        //--- Methods ---

        [TestFixtureSetUp]
        public void Init() {
            _testAssembly = Assembly.GetExecutingAssembly();
            _dataUpdater = new TestDataUpdater("100.0.0");
            _dataUpdater.LoadMethods(_testAssembly);
        }

        [SetUp]
        public void Setup() {
            DummyUpgradeClass.ExecutedMethods.Clear();
        }

        [Test]
        public void load_methods_and_execute_using_reflection() {
            var methods = _dataUpdater.GetMethods();
            Assert.IsTrue(methods.Count > 0, "No methods were loaded");
            foreach(var method in methods) {
                _dataUpdater.ExecuteMethod(method);
            }
            Assert.AreEqual(methods.Count, DummyUpgradeClass.ExecutedMethods.Count, "The number of methods to be executed does not match.");
            for(int i = 0; i < methods.Count; i++) {
                Assert.AreEqual(methods[i], DummyUpgradeClass.ExecutedMethods[i]._methodName, "Method name that was executed does not match");
            }
        }

        [Test]
        public void load_and_execute() {
            _dataUpdater.LoadMethodsAndExecute(_testAssembly);
            Assert.IsTrue(DummyUpgradeClass.ExecutedMethods.Count > 0, "No Methods were executed");
        }

        [Test]
        public void loaded_methods_proper_order() {
            
            // Execute all methods
            var methods = _dataUpdater.GetMethods();
            Assert.IsTrue(methods.Count > 0, "There were no methods found");
            foreach(var method in methods) {
                _dataUpdater.ExecuteMethod(method);
            }

            // Check that methods were executed in proper order
            Assert.IsTrue(DummyUpgradeClass.ExecutedMethods.Count > 0, "No Methods were executed");
            for(int i = 1; i < DummyUpgradeClass.ExecutedMethods.Count; i++) {
                Assert.IsTrue(DummyUpgradeClass.ExecutedMethods[i].CompareTo(DummyUpgradeClass.ExecutedMethods[i - 1]) == 1, "Ordering of methods is wrong.");
            }
        }

        [Test]
        public void invoke_custom_method() {
            _dataUpdater.ExecuteCustomMethod("CustomMethod1", _testAssembly);
            Assert.AreEqual("CustomMethod1", DummyUpgradeClass.ExecutedMethods.First()._methodName, "The wrong method was executed");
        }

        [Test]
        public void invoke_custom_method_with_parameters() {
            var parameters = new string[4] {"--param1", "value1", "--param2", "value2"};
            _dataUpdater.ExecuteCustomMethod("CustomMethod2", _testAssembly, parameters);
            Assert.AreEqual("CustomMethod2", DummyUpgradeClass.ExecutedMethods.First()._methodName, "The wrong method was executed");
            Assert.AreEqual(parameters, DummyUpgradeClass.ExecutedMethods.First()._args, "The proper arguments were not passed in to the custom method");
        }

        [Test]
        public void run_methods_up_to_certain_version() {
            string version = "10.0.1";
            var maxVersion = new VersionInfo(version);
            var dataUpdater = new TestDataUpdater(version);
            dataUpdater.LoadMethodsAndExecute(_testAssembly);
            Assert.IsTrue(DummyUpgradeClass.ExecutedMethods.Count > 0, "No Methods were executed");
            foreach(var method in DummyUpgradeClass.ExecutedMethods) { 
                var maxCompare = method._version.CompareTo(maxVersion).Change;
                Assert.IsTrue(maxCompare == VersionChange.Downgrade || maxCompare == VersionChange.None, string.Format("Version is larger than {0}", version));
            }
        }

        [Test]
        public void run_methods_with_source_and_target_version() {
            string targetVersion = "11.0.3";
            string sourceVersion = "10.0.0";
            var maxVersion = new VersionInfo(targetVersion);
            var minVersion = new VersionInfo(sourceVersion);
            var dataUpdater = new TestDataUpdater(targetVersion);
            dataUpdater.SourceVersion = sourceVersion;
            dataUpdater.LoadMethodsAndExecute(_testAssembly);
            Assert.IsTrue(DummyUpgradeClass.ExecutedMethods.Count > 0, "No Methods were executed");
            foreach(var method in DummyUpgradeClass.ExecutedMethods) {
                var maxCompare = method._version.CompareTo(maxVersion).Change;
                var minCompare = method._version.CompareTo(minVersion).Change;
                Assert.IsTrue(maxCompare == VersionChange.Downgrade || maxCompare == VersionChange.None, string.Format("Version is larger than {0}", targetVersion));
                Assert.IsTrue(minCompare == VersionChange.Upgrade || minCompare == VersionChange.None, string.Format("Version is not larger than source version {0}", sourceVersion));
            }
        }

        [Test]
        public void get_and_execute_data_integrity_methods() {
            var methods = (from method in _dataUpdater.GetDataIntegrityMethods()
                           select _dataUpdater.GetMethodInfo(method));
            Assert.IsTrue(methods.Count() > 0, "There were no data integrity methods found");
            foreach(var method in methods) {
                var attributes = method.GetMethodInfo.GetCustomAttributes(false);
                Assert.IsTrue(attributes.Count() > 0, "method does not have any attributes");
                Assert.IsTrue(attributes.First() is DataIntegrityCheck, string.Format("Method {0} does not have proper data integrity attribute", method.GetMethodInfo.Name));
                _dataUpdater.ExecuteMethod(method.GetMethodInfo.Name);
            }

        }
    }
}
