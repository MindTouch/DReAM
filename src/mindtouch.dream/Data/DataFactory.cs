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
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace MindTouch.Data {

    /// <summary>
    /// Provides a factory of ADO.NET objects for use by <see cref="DataCatalog"/>.
    /// </summary>
    public class DataFactory {

        //--- Fields ---
        private readonly string _parameterChar;
        private ConstructorInfo _connectionConstructor;
        private ConstructorInfo _adapterConstructor;
        private ConstructorInfo _parameterConstructor;
        private ConstructorInfo _commandConstructor;
        private readonly DbProviderFactory _factory;

        //--- Constructor ---

        /// <summary>
        /// This constructor is obsolete. Use a constructor using or creating a <see cref="DbProviderFactory"/> instead (including constructor that takes assemblyName and parameterChar).
        /// </summary>
        [Obsolete("Please use DbProviderFactory based constructor (including constructor that takes assemblyName and parameterChar)")]
        public DataFactory(string assemblyName, string connectionTypeName, string adapterTypeName, string parameterTypeName, string commandTypeName, string parameterChar) {
            if(assemblyName == null) {
                throw new ArgumentNullException("assemblyName");
            }
            if(connectionTypeName == null) {
                throw new ArgumentNullException("connectionTypeName");
            }
            if(adapterTypeName == null) {
                throw new ArgumentNullException("adapterTypeName");
            }
            if(parameterTypeName == null) {
                throw new ArgumentNullException("parameterTypeName");
            }
            if(commandTypeName == null) {
                throw new ArgumentNullException("commandTypeName");
            }
            _parameterChar = parameterChar ?? string.Empty;
            Assembly assembly = Assembly.Load(assemblyName);
            Initialize(assembly.GetType(connectionTypeName), assembly.GetType(parameterTypeName), assembly.GetType(commandTypeName), assembly.GetType(adapterTypeName));
        }

        /// <summary>
        /// This constructor is obsolete. Use a constructor using or creating a <see cref="DbProviderFactory"/> instead (including constructor that takes assemblyName and parameterChar).
        /// </summary>
        [Obsolete("Please use DbProviderFactory based constructor (including constructor that takes assemblyName and parameterChar)")]
        public DataFactory(string assemblyName, string typePrefix, string parameterChar) {
            if(assemblyName == null) {
                throw new ArgumentNullException("assemblyName");
            }
            if(typePrefix == null) {
                throw new ArgumentNullException("typePrefix");
            }
            _parameterChar = parameterChar ?? string.Empty;
            Assembly assembly = Assembly.Load(assemblyName);
            Initialize(assembly.GetType(typePrefix + "Connection"), assembly.GetType(typePrefix + "Parameter"), assembly.GetType(typePrefix + "Command"), assembly.GetType(typePrefix + "DataAdapter"));
        }

        /// <summary>
        /// This constructor is obsolete. Use a constructor using or creating a <see cref="DbProviderFactory"/> instead (including constructor that takes assemblyName and parameterChar).
        /// </summary>
        [Obsolete("Please use DbProviderFactory based constructor (including constructor that takes assemblyName and parameterChar)")]
        public DataFactory(Type connectionType, Type adapterType, Type parameterType, Type commandType, string parameterChar) {
            _parameterChar = parameterChar ?? string.Empty;
            Initialize(connectionType, parameterType, commandType, adapterType);
        }

        /// <summary>
        /// Create a new instance by dynamically loading a <see cref="DbProviderFactory"/>.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly containing a <see cref="DbProviderFactory"/>.</param>
        /// <param name="parameterChar">Query parameter prefix character.</param>
        public DataFactory(string assemblyName, string parameterChar) {
            if(assemblyName == null) {
                throw new ArgumentNullException("assemblyName");
            }
            _parameterChar = parameterChar ?? string.Empty;
            Assembly assembly = Assembly.Load(assemblyName);
            Type factoryType = typeof(DbProviderFactory);
            foreach(Type t in assembly.GetTypes()) {
                if(t.IsAbstract || !factoryType.IsAssignableFrom(t)) {
                    continue;
                }
                FieldInfo info = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                _factory = (DbProviderFactory)info.GetValue(null);
                break;
            }
            if(_factory == null) {
                throw new ArgumentException("Unable to find an implementation of DbProviderFactory in assembly", "assemblyName");
            }
        }

        /// <summary>
        /// Create a new instance based on an existing <see cref="DbProviderFactory"/>.
        /// </summary>
        /// <param name="factory">Provider factory instance.</param>
        /// <param name="parameterChar">Query parameter prefix character.</param>
        public DataFactory(DbProviderFactory factory, string parameterChar) {
            if(factory == null) {
                throw new ArgumentNullException("factory");
            }
            _factory = factory;
            _parameterChar = parameterChar ?? string.Empty;
        }

        //--- Properties ---
        internal String ParameterChar { get { return _parameterChar; } }

        //--- Methods ---
        internal IDbConnection OpenConnection(string connectionString) {
            IDbConnection result = null;
            try {
                if(_factory != null) {
                    result = _factory.CreateConnection();
                    result.ConnectionString = connectionString;
                } else {
                    result = (IDbConnection)_connectionConstructor.Invoke(new object[] { connectionString });
                }
                if(result.State == ConnectionState.Closed) {
                    try {
                        result.Open();
                    } catch(EntryPointNotFoundException e) {
                        throw new DataException("Unable to create a connection, likely due to shared library incompatibility or missing library path to appropriate library", e);
                    }
                }
            } catch {
                if(result != null) {

                    // have to dispose connection if an initialization failure occured
                    result.Dispose();
                }
				throw;
            }
            return result;
        }

        internal IDbCommand CreateQuery(string query) {
            IDbCommand result = null;
            try {
                if (_factory != null) {
                    result = _factory.CreateCommand();
                    result.CommandText = query;
                } else {
                    result = (IDbCommand) _commandConstructor.Invoke(new object[] {query});
                }
            }catch {
                if(result != null) {

                    // must dispose of command in case of failure
                    result.Dispose();
                }
				throw;
            }
            return result;
        }

        internal IDbCommand CreateProcedure(string name) {
            IDbCommand result;
            if(_factory != null) {
                result = _factory.CreateCommand();
                result.CommandText = name;
            } else {
                result = (IDbCommand)_commandConstructor.Invoke(new object[] { name });
            }
            result.CommandType = CommandType.StoredProcedure;
            return result;
        }

        internal IDbDataAdapter CreateAdapter(IDbCommand cmd) {
            IDbDataAdapter result;
            if(_factory != null) {
                result = _factory.CreateDataAdapter();
                result.SelectCommand = cmd;
            } else {
                result = (IDbDataAdapter)_adapterConstructor.Invoke(new object[] { cmd });
            }
            return result;
        }

        internal IDataParameter CreateParameter(object name, object value) {
            IDataParameter result;
            if(_factory != null) {
                result = _factory.CreateParameter();
                result.ParameterName = _parameterChar + name.ToString();
                result.Value = value;
            } else {
                result = (IDataParameter)_parameterConstructor.Invoke(new object[] { _parameterChar + name.ToString(), value });
            }
            return result;
        }

        internal IDataParameter CreateParameter(object name, object value, ParameterDirection dir) {
            IDataParameter result;
            if(_factory != null) {
                result = _factory.CreateParameter();
                result.ParameterName = _parameterChar + name.ToString();
                result.Value = value;
            } else {
                result = (IDataParameter)_parameterConstructor.Invoke(new object[] { _parameterChar + name.ToString(), value });
            }
            result.Direction = dir;
            return result;
        }

        private void Initialize(Type connectionType, Type parameterType, Type commandType, Type adapterType) {

            // connection type
            if(connectionType == null) {
                throw new ArgumentNullException("connectionType");
            }
            if(!typeof(IDbConnection).IsAssignableFrom(connectionType)) {
                throw new ArgumentException("connectionType has bad type");
            }
            _connectionConstructor = connectionType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(String) }, null);
            if(_connectionConstructor == null) {
                throw new ArgumentException("connectionType missing standard constructor");
            }

            // parameter type
            if(parameterType == null) {
                throw new ArgumentNullException("parameterType");
            }
            if(!typeof(IDbDataParameter).IsAssignableFrom(parameterType)) {
                throw new ArgumentException("parameterType has bad type");
            }
            _parameterConstructor = parameterType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(String), typeof(object) }, null);
            if(_parameterConstructor == null) {
                throw new ArgumentException("parameterType missing standard constructor");
            }

            // command type
            if(commandType == null) {
                throw new ArgumentNullException("commandType");
            }
            if(!typeof(IDbCommand).IsAssignableFrom(commandType)) {
                throw new ArgumentException("commandType has bad type");
            }
            _commandConstructor = commandType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(String) }, null);
            if(_commandConstructor == null) {
                throw new ArgumentException("commandType missing standard constructor");
            }

            // adapter type
            if(adapterType == null) {
                throw new ArgumentNullException("adapterType");
            }
            if(!typeof(IDbDataAdapter).IsAssignableFrom(adapterType)) {
                throw new ArgumentException("adapterType has bad type");
            }
            _adapterConstructor = adapterType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { commandType }, null);
            if(_adapterConstructor == null) {
                throw new ArgumentException("adapterType missing standard constructor");
            }
        }
    }
}
