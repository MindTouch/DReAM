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
using System.Collections.Generic;
using System.Reflection;

namespace MindTouch.Data {
    interface IDataUpdater {

        /// <summary>
        ///  Get or set the target version
        /// </summary>
        /// <returns> The string representation of the target version</returns>
        string TargetVersion { get; set; }

        /// <summary>
        ///  Get or set the source version
        /// </summary>
        /// <returns> The string representation of the source version</returns>
        string SourceVersion { get; set; }

        /// <summary>
        ///  Get a list of methods that will be run
        /// </summary>
        /// <returns>
        ///  List of method names
        /// </returns>
        List<string> GetMethods();

        /// <summary>
        /// Get a list of methods that only check data integrity
        /// </summary>
        /// <returns></returns>
        List<string> GetDataIntegrityMethods(); 

        /// <summary>
        /// Tests the connection with the Data Store 
        /// that is to be updated. Throws an Exception 
        /// if an error occurs.
        /// </summary>
        /// <returns></returns>
        void TestConnection();

        /// <summary>
        /// Loads the Methods to run into a list 
        /// </summary>
        /// <param name="updateAssembly">Assembly object to perform reflection on</param>
        /// <returns></returns>
        void LoadMethods(Assembly updateAssembly);

        /// <summary>
        /// Loads the Methods to run into a list amd executes them
        /// </summary>
        /// <param name="updateAssembly">Assembly object to perform reflection on</param>
        /// <returns></returns>
        void LoadMethodsAndExecute(Assembly updateAssembly);

        /// <summary>
        /// Execute the methods in the assembly
        /// </summary>
        void Execute();

        /// <summary>
        /// Execute the method with the given name
        /// <param name="name">Name of the method to execute. Case Sensitive.</param>
        /// </summary>
        void ExecuteMethod(string name);

        /// <summary>
        /// Execute the method with the exact name, this method
        /// does not need to be tagged with the appropriate attribute
        /// </summary>
        /// <param name="name">Name of the method to execute</param>
        /// <param name="updateAssembly">Assembly object to perform reflection on</param>
        void ExecuteCustomMethod(string name, Assembly updateAssembly);
    }
}
