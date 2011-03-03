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
using System.Data;

using MindTouch.Xml;

namespace MindTouch.Data {

    /// <summary>
    /// Provides a a database query/stored procedure command builder.
    /// </summary>
    public interface IDataCommand {

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if this command is a stored procedure.
        /// </summary>
        bool IsStoredProcedure { get; }

        /// <summary>
        /// Execution time of the last query 
        /// </summary>
        TimeSpan ExecutionTime { get; }

        //--- Methods ---

        /// <summary>
        /// Adds an input parameter to the command.
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <param name="value">Value of the parameter</param>
        /// <returns>Returns this command</returns>
        IDataCommand With(string key, object value);

        /// <summary>
        /// Adds an input-output parameter to the command.
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <param name="value">Value of the parameter</param>
        /// <returns>Returns this command</returns>
        IDataCommand WithInOut(string key, object value);

        /// <summary>
        /// Adds an output parameter to the command.
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <returns>Returns this command</returns>
        IDataCommand WithOutput(string key);

        /// <summary>
        /// Adds an return parameter to the command.
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <returns>Returns this command</returns>
        IDataCommand WithReturn(string key);

        /// <summary>
        /// Retrieve an output/return value from the finished command.
        /// </summary>
        /// <typeparam name="T">Returned value type</typeparam>
        /// <param name="key">Name of returned parameter (provided previously using 'WithOutput()' or 'WithInOut()' or 'WithReturn()'</param>
        /// <returns>Converted value</returns>
        T At<T>(string key);

        /// <summary>
        /// Retrieve an output/return value from the finished command.
        /// </summary>
        /// <typeparam name="T">Returned value type</typeparam>
        /// <param name="key">Name of returned parameter (provided previously using 'WithOutput()' or 'WithInOut()' or 'WithReturn()'</param>
        /// <param name="def">Value to return if returned value is null or DbNull</param>
        /// <returns>Converted value</returns>
        T At<T>(string key, T def);

        /// <summary>
        /// Execute command.
        /// </summary>
        void Execute();

        /// <summary>
        /// Execute command and call handler with an open IDataReader on the result set.  
        /// IDataReader and connection will be automatically closed upon completion of the handler.
        /// </summary>
        /// <param name="handler">Handler to invoke</param>
        void Execute(Action<IDataReader> handler);

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Read value</returns>
        string Read();

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        bool? ReadAsBool();

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        byte? ReadAsByte();

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        short? ReadAsShort();

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        ushort? ReadAsUShort();

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        int? ReadAsInt();

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        long? ReadAsLong();

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        uint? ReadAsUInt();

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        ulong? ReadAsULong();

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        DateTime? ReadAsDateTime();

        /// <summary>
        /// Execute command and read result into an XDoc.
        /// </summary>
        /// <param name="table">Name of the root element</param>
        /// <param name="row">Name of the element created for each row</param>
        /// <returns>Read DataSet object</returns>
        XDoc ReadAsXDoc(string table, string row);

        /// <summary>
        /// Execute command and convert the first row into an object.
        /// </summary>
        /// <typeparam name="T">Object type to create</typeparam>
        /// <returns>Created object</returns>
        T ReadAsObject<T>() where T : new();

        /// <summary>
        /// Execute command and convert all rows into a list of objects.
        /// </summary>
        /// <typeparam name="T">Object type to create</typeparam>
        /// <returns>List of created objects</returns>
        List<T> ReadAsObjects<T>() where T : new();
    }
}