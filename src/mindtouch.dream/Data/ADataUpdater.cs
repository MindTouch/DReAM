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

    public enum MethodType { Update, DataIntegrity };

    public class DbMethod : IComparable<DbMethod> {

        //--- Fields ---
        private readonly MethodInfo _methodInfo;
        private readonly VersionInfo _targetVersion;
        private readonly MethodType _methodType;

        //--- Constructors ---
        public DbMethod(MethodInfo methodInfo, VersionInfo targetVersion) {
            _methodInfo = methodInfo;
            _targetVersion = targetVersion;
            _methodType = MethodType.Update;
        }

        public DbMethod(MethodInfo methodInfo, VersionInfo targetVersion, MethodType methodType) {
            _methodInfo = methodInfo;
            _targetVersion = targetVersion;
            _methodType = methodType;
        }

        //--- Methods ---
        public MethodType GetMethodType {
            get { return _methodType; }
        }

        public MethodInfo GetMethodInfo {
            get { return _methodInfo; }
        }

        public VersionInfo GetTargetVersion {
            get { return _targetVersion; }
        }

        // Compares by version then by name
        public int CompareTo(DbMethod other) {
            var otherVersion = other.GetTargetVersion;
            var change = _targetVersion.CompareTo(otherVersion).Change;
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

    /// <summary>
    /// Provides an interface for updating Data in Dream Applications
    /// </summary>
    public abstract class ADataUpdater : IDataUpdater {
       
        //--- Fields ---
        protected VersionInfo _targetVersion = null;
        protected VersionInfo _sourceVersion = null;
        protected List<DbMethod> _methodList = null;
        protected Type _dataUpgradeClass = null;
        protected object _dataUpgradeClassInstance = null;

        //--- Methods ---

        /// <summary>
        ///  Get or set the target version
        /// </summary>
        /// <returns> The string representation of the target version</returns>
        public string TargetVersion {
            get {
                if(_targetVersion == null) {
                    return "";
                }
                return _targetVersion.ToString(); 
            }
            set { 
                _targetVersion = new VersionInfo(value);
                if(!_targetVersion.IsValid) {
                    throw new VersionInfoException(_targetVersion);
                }
            }
        }

        /// <summary>
        ///  Get or set the source version
        /// </summary>
        /// <returns> The string representation of the source version</returns>
        public string SourceVersion {
            get {
                if(_sourceVersion == null) {
                    return "";
                }
                return _sourceVersion.ToString();
            }
            set {
                _sourceVersion = new VersionInfo(value);
                if(!_sourceVersion.IsValid) {
                    throw new VersionInfoException(_sourceVersion);
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
            var list = (from method in _methodList where method.GetMethodType == MethodType.Update select method.GetMethodInfo.Name).ToList();
            return list;
        }

        /// <summary>
        /// Get a list of methods that only check data integrity
        /// </summary>
        /// <returns>
        ///  List of method names
        /// </returns>
        public List<string> GetDataIntegrityMethods() {
            if(_methodList == null) {
                return null;
            }
            var list = (from method in _methodList where method.GetMethodType == MethodType.DataIntegrity select method.GetMethodInfo.Name).ToList();
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
            if(_targetVersion == null) {
                throw new VersionInfoException(_targetVersion);
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

            // search the class for methods labeled with Attribute "EffectiveVersion("version")" and "CheckDataIntegrity("version")"
            var methods = _dataUpgradeClass.GetMethods();
            _methodList = new List<DbMethod>();
            foreach(var methodInfo in methods) {
                foreach(var attr in (from m in methodInfo.GetCustomAttributes(false) select m)) {
                    VersionInfo version;
                    var type = MethodType.Update;
                    if(attr.IsA<EffectiveVersionAttribute>()) {
                        version = new VersionInfo(((EffectiveVersionAttribute)attr).VersionString);
                    } else if(attr.IsA<DataIntegrityCheck>()) {
                        version = new VersionInfo(((DataIntegrityCheck)attr).VersionString);
                        type = MethodType.DataIntegrity;
                    } else {
                        continue;
                    }
                    if(version.CompareTo(_targetVersion).Change != VersionChange.Upgrade &&
                        (_sourceVersion == null || version.CompareTo(_sourceVersion).Change != VersionChange.Downgrade )) {
                        _methodList.Add(new DbMethod(methodInfo, version, type));
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
        /// Execute the method with the exact name, this method
        /// does not need to be tagged with the appropriate attribute
        /// </summary>
        /// <param name="name">Exact name of the method to be executed</param>
        /// <param name="updateAssembly">Assembly object to perform reflection on</param>
        /// <param name="param">Parameter array to pass to custom method</param>
        public void ExecuteCustomMethod(string name, Assembly updateAssembly, params object[] param) {
            if(_dataUpgradeClass == null) {
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
            }
            if(_dataUpgradeClassInstance == null) {
                _dataUpgradeClassInstance = CreateActivatorInstance(_dataUpgradeClass);
            }

            // Attempt to execute method
            _dataUpgradeClass.InvokeMember(name, BindingFlags.Default | BindingFlags.InvokeMethod, null, _dataUpgradeClassInstance, param);
        }

        /// <summary>
        /// Create instance of class defined by the provided Type
        /// <param name="dataUpgradeType">"The Type instance to activate"</param>
        /// </summary>
        protected virtual object CreateActivatorInstance(Type dataUpgradeType) {
            return Activator.CreateInstance(dataUpgradeType);
        }

        /// <summary>
        /// Get the Method details of a method
        /// </summary>
        /// <param name="name">Name of the Method</param>
        /// <returns>Object of type DbMethod</returns>
        public virtual DbMethod GetMethodInfo(string name) {
            if(_methodList.Count == 0) {
                throw new NoMethodsLoaded();
            }
            var methods = (from method in _methodList where method.GetMethodInfo.Name.EqualsInvariant(name) select method);
            return methods.First();
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
