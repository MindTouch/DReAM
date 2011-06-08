using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MindTouch.Data {
    interface IDataUpdater {

        /// <summary>
        ///  Get or set the effective version
        /// </summary>
        /// <returns> The string representation of the effective version</returns>
        string EffectiveVersion { get; set; }

        /// <summary>
        ///  Get a list of methods that will be run
        /// </summary>
        /// <returns>
        ///  List of method names
        /// </returns>
        List<string> GetMethods();

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
    }
}
