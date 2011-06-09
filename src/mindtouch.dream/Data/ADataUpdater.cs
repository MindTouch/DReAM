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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MindTouch.Data {

    /// <summary>
    /// Provides an interface for updating Data in Dream Applications
    /// </summary>
    public abstract class ADataUpdater : IDataUpdater {

        //--- Types ---
        protected class UpdateMethod : IComparable<UpdateMethod> {

            //--- Fields ---
            private readonly MethodInfo _methodInfo;
            private readonly VersionInfo _effectiveVersion;

            //--- Constructors ---
            public UpdateMethod(MethodInfo methodInfo, VersionInfo effectiveVersion) {
                _methodInfo = methodInfo;
                _effectiveVersion = effectiveVersion;
            }

            //--- Methods ---
            public MethodInfo GetMethodInfo {
                get { return _methodInfo; }
            }

            public VersionInfo GetVersionInfo {
                get { return _effectiveVersion; }
            }

            // Compares by version then by name
            public int CompareTo(UpdateMethod other) {
                var otherVersion = other.GetVersionInfo;
                var change = _effectiveVersion.CompareTo(otherVersion).Change;
                switch(change) {
                case VersionChange.None:
                    return _methodInfo.Name.CompareTo(other._methodInfo.Name);
                case VersionChange.Upgrade:
                    return 1;
                default:
                    return -1;
                }
            }
        }

        //--- Fields ---
        protected VersionInfo _effectiveVersion = null;
        protected List<UpdateMethod> _methodList = null;
        protected Type _dataUpgradeClass = null;
        protected object _dataUpgradeClassInstance = null;

        //--- Methods ---

        /// <summary>
        ///  Get or set the effective version
        /// </summary>
        /// <returns> The string representation of the effective version</returns>
        public string EffectiveVersion {
            get {
                if(_effectiveVersion == null) {
                    return "";
                }
                return _effectiveVersion.ToString(); 
            }
            set { 
                _effectiveVersion = new VersionInfo(value);
                if(!_effectiveVersion.IsValid) {
                    throw new VersionInfoException(_effectiveVersion);
                }
            }
        }

        /// <summary>
        ///  Get a list of methods that will be run
        /// </summary>
        /// <returns>
        ///  List of method names
        /// </returns>
        public List<string> GetMethods() {
            if(_methodList == null) {
                return null;
            }
            var list = (from method in _methodList select method.GetMethodInfo.Name).ToList();
            return list;
        }

        /// <summary>
        /// Tests the connection with the Data Store 
        /// that is to be updated. Throws an Exception 
        /// if an error occurs.
        /// </summary>
        /// <returns></returns>
        public abstract void TestConnection();

        /// <summary>
        /// Loads the Methods to run into a list 
        /// </summary>
        /// <param name="updateAssembly">Assembly object to perform reflection on</param>
        /// <returns></returns>
        public virtual void LoadMethods(Assembly updateAssembly) { 

            // Make sure we have a defined version
            if(_effectiveVersion == null) {
                throw new VersionInfoException(_effectiveVersion);
            }
            
            // get all the members of the Assembly
            var types = updateAssembly.GetTypes();

            // Find the class with attribute "DataUpgrade"
            var classTypes = from type in types where type.IsClass select type;
            foreach(var type in from type in classTypes from attribute in (from a in System.Attribute.GetCustomAttributes(type) where a is DataUpgradeAttribute select a) select type) {
                _dataUpgradeClass = type;
            }

            // if no class was found exit 
            if(_dataUpgradeClass == null) {
                throw new NoUpgradeAttributesFound();
            }

            // search the class for methods labeled with Attribute "EffectiveVersion("version")"
            var methods = _dataUpgradeClass.GetMethods();
            _methodList = new List<UpdateMethod>();
            foreach(var methodInfo in methods) {
                foreach(var attr in (from m in methodInfo.GetCustomAttributes(false) where m is EffectiveVersionAttribute select m)) {
                    var version = new VersionInfo(((EffectiveVersionAttribute)attr).VersionString);
                    if(version.CompareTo(_effectiveVersion).Change != VersionChange.Upgrade) {
                        _methodList.Add(new UpdateMethod(methodInfo, version));
                    }
                }
            }

            // Sort Methods by version then by name
            _methodList.Sort();
        }


        /// <summary>
        /// Loads the Methods to run into a list amd executes them
        /// </summary>
        /// <param name="updateAssembly">Assembly object to perform reflection on</param>
        /// <returns></returns>
        public virtual void LoadMethodsAndExecute(Assembly updateAssembly) {
            LoadMethods(updateAssembly);
            Execute();
        }

        /// <summary>
        /// Execute the methods in the assembly
        /// </summary>
        public virtual void Execute() {
            if(_methodList == null) {
                throw new NoMethodsLoaded();
            }
            foreach(var method in _methodList) {
                ExecuteMethod(method.GetMethodInfo.Name);
            }
        }

        /// <summary>
        /// Execute the method with the given name
        /// <param name="name">Name of the method to execute. Case Sensitive.</param>
        /// </summary>
        public virtual void ExecuteMethod(string name) {
            if(_dataUpgradeClassInstance == null) {
                _dataUpgradeClassInstance = CreateActivatorInstance(_dataUpgradeClass); 
            }
            
            // Check that method is in the methodlist
            var nameList = (from method in _methodList select method.GetMethodInfo.Name).ToList();
            if(!nameList.Contains(name)) {
                throw new MethodMissingAttribute();
            }
            _dataUpgradeClass.InvokeMember(name, BindingFlags.Default | BindingFlags.InvokeMethod, null, _dataUpgradeClassInstance, null);
        }


        /// <summary>
        /// Create instance of class defined by the provided Type
        /// <param name="dataUpgradeType">"The Type instance to activate"</param>
        /// </summary>
        protected virtual object CreateActivatorInstance(Type dataUpgradeType) {
            return Activator.CreateInstance(dataUpgradeType);
        }
    }

    public class VersionInfoException : Exception {
        public VersionInfoException(VersionInfo _versionInfo)
            : base(String.Format("Version string is invalid : {0}", _versionInfo.ToString())) {}
    }

    public class NoUpgradeAttributesFound : Exception {
        public NoUpgradeAttributesFound()
            : base ("Did not find any class with DataUpgrade attribute") {}
    }

    public class NoMethodsLoaded : Exception {
        public NoMethodsLoaded()
            : base("No Methods were loaded. Run LoadMethods() first.") { }
    }

    public class MethodMissingAttribute : Exception {
        public MethodMissingAttribute()
            : base("The Method you are trying to Execute does not have the proper attribute: [EffectiveVersion]") { }
    }
}
