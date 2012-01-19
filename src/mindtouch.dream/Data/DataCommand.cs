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
using System.Diagnostics;
using System.Reflection;

using MindTouch.Xml;

namespace MindTouch.Data {

    /// <summary>
    /// Provides a a database query/stored procedure command builder.
    /// </summary>
    public class DataCommand : IDataCommand {

        //--- Types ---
        internal class DataColumnField {

            //--- Fields ---
            private readonly PropertyInfo _property;
            private readonly FieldInfo _field;
            private readonly Type _type;

            //--- Constructors ---
            internal DataColumnField(PropertyInfo info) {
                if(info == null) {
                    throw new ArgumentNullException("info");
                }
                _property = info;
                _type = info.PropertyType;
            }

            internal DataColumnField(FieldInfo info) {
                if(info == null) {
                    throw new ArgumentNullException("info");
                }
                _field = info;
                _type = info.FieldType;
            }

            //--- Methods ---
            internal void SetValue(object instance, object value) {
                if((value == null) || (value is DBNull)) {
                    if(!_type.IsValueType || (_type.IsGenericType && (_type.GetGenericTypeDefinition() == typeof(Nullable<>)))) {
                        if(_property != null) {
                            _property.SetValue(instance, null, null);
                        } else {
                            _field.SetValue(instance, null);
                        }
                    }
                } else if(_type.IsEnum) {
                    switch(Convert.GetTypeCode(value)) {
                    case TypeCode.String:
                        if(_property != null) {
                            _property.SetValue(instance, Enum.Parse(_type, (string)value, true), null);
                        } else {
                            _field.SetValue(instance, Enum.Parse(_type, (string)value, true));
                        }
                        break;
                    case TypeCode.Byte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                        string text = Enum.GetName(_type, value);
                        if(text != null) {
                            if(_property != null) {
                                _property.SetValue(instance, text, null);
                            } else {
                                _field.SetValue(instance, text);
                            }
                        }
                        break;
                    }
                } else if(value.GetType() != _type) {
                    if(_property != null) {
                        _property.SetValue(instance, SysUtil.ChangeType(value, _type), null);
                    } else {
                        _field.SetValue(instance, SysUtil.ChangeType(value, _type));
                    }
                } else {
                    if(_property != null) {
                        _property.SetValue(instance, value, null);
                    } else {
                        _field.SetValue(instance, value);
                    }
                }
            }
        }

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static readonly Dictionary<Type, Dictionary<string, DataColumnField>> _typeCache = new Dictionary<Type, Dictionary<string, DataColumnField>>();
        private static readonly TimeSpan SLOW_SQL = TimeSpan.FromSeconds(5);

        //--- Class Methods ---

        /// <summary>
        /// Ensure that string is safe for use in SQL statements.
        /// </summary>
        /// <param name="text">String to escape</param>
        /// <returns>Escaped string</returns>
        public static string MakeSqlSafe(string text) {
            if(string.IsNullOrEmpty(text)) {
                return text;
            }
            text = text.ReplaceAll(
                "\\", "\\\\", 
                "\0", "\\0",
                "\n", "\\n",
                "\r", "\\r",
                "'", "\\'",
                "\"", "\\\"",
                "\x1a", "\\x1a"
            );
            return text;
        }

        private static Dictionary<string, DataColumnField> GetDataFields(Type type) {
            Dictionary<string, DataColumnField> result;
            if(!_typeCache.TryGetValue(type, out result)) {
                lock(_typeCache) {
                    result = new Dictionary<string, DataColumnField>(StringComparer.OrdinalIgnoreCase);

                    // enumerate all properties of this type
                    foreach(PropertyInfo property in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) {
                        foreach(DataColumnAttribute attribute in property.GetCustomAttributes(typeof(DataColumnAttribute), true)) {
                            result.Add(attribute.Name ?? property.Name, new DataColumnField(property));
                        }
                    }

                    // enumerate all fields of this type
                    foreach(FieldInfo field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) {
                        foreach(DataColumnAttribute attribute in field.GetCustomAttributes(typeof(DataColumnAttribute), true)) {
                            result.Add(attribute.Name ?? field.Name, new DataColumnField(field));
                        }
                    }
                    if(result.Count == 0) {
                        throw new MissingFieldException("Type does not have any properties decorated with DataColumn attribute");
                    }
                    _typeCache[type] = result;
                }
            }
            return result;
        }

        private static T FillObject<T>(T item, Dictionary<string, DataColumnField> fields, IDataReader reader) {
            for(int i = 0; i < reader.FieldCount; i++) {
                DataColumnField field;
                if(fields.TryGetValue(reader.GetName(i), out field)) {
                    field.SetValue(item, reader.GetValue(i));
                }
            }
            return item;
        }

        //--- Fields ---
        private readonly DataFactory _factory;
        private readonly IDbCommand _command;
        private readonly string _connection;
        private readonly Stopwatch _stopWatch = new Stopwatch();
        private readonly DataCatalog _catalog;

        //--- Constructors ---
        internal DataCommand(DataFactory factory, DataCatalog catalog, string connection, IDbCommand command) {
            if(factory == null) {
                throw new ArgumentNullException("factory");
            }
            if(catalog ==  null) {
                throw new ArgumentNullException("catalog");
            }
            if(connection == null) {
                throw new ArgumentNullException("connection");
            }
            if(command == null) {
                throw new ArgumentNullException("command");
            }
            _factory = factory;
            _connection = connection;
            _command = command;
            _catalog = catalog;
        }

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if this command is a stored procedure.
        /// </summary>
        public bool IsStoredProcedure { get { return _command.CommandType == CommandType.StoredProcedure; } }

        /// <summary>
        /// Execution time of the last query 
        /// </summary>
        public TimeSpan ExecutionTime { get { return _stopWatch.Elapsed; } }

        //--- Methods ---

        /// <summary>
        /// Adds an input parameter to the command.
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <param name="value">Value of the parameter</param>
        /// <returns>Returns this command</returns>
        public DataCommand With(string key, object value) {
            _command.Parameters.Add(_factory.CreateParameter(key, value, ParameterDirection.Input));
            return this;
        }

        IDataCommand IDataCommand.With(string key, object value) {
            return With(key, value);
        }

        /// <summary>
        /// Adds an input-output parameter to the command.
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <param name="value">Value of the parameter</param>
        /// <returns>Returns this command</returns>
        public DataCommand WithInOut(string key, object value) {
            _command.Parameters.Add(_factory.CreateParameter(key, value, ParameterDirection.InputOutput));
            return this;
        }

        IDataCommand IDataCommand.WithInOut(string key, object value) {
            return WithInOut(key, value);
        }

        /// <summary>
        /// Adds an output parameter to the command.
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <returns>Returns this command</returns>
        public DataCommand WithOutput(string key) {
            _command.Parameters.Add(_factory.CreateParameter(key, null, ParameterDirection.Output));
            return this;
        }

        IDataCommand IDataCommand.WithOutput(string key) {
            return WithOutput(key);
        }

        /// <summary>
        /// Adds an return parameter to the command.
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <returns>Returns this command</returns>
        public DataCommand WithReturn(string key) {
            _command.Parameters.Add(_factory.CreateParameter(key, null, ParameterDirection.ReturnValue));
            return this;
        }

        IDataCommand IDataCommand.WithReturn(string key) {
            return WithReturn(key);
        }

        /// <summary>
        /// Retrieve an output/return value from the finished command.
        /// </summary>
        /// <typeparam name="T">Returned value type</typeparam>
        /// <param name="key">Name of returned parameter (provided previously using 'WithOutput()' or 'WithInOut()' or 'WithReturn()'</param>
        /// <returns>Converted value</returns>
        public T At<T>(string key) {
            return At(key, default(T));
        }

        /// <summary>
        /// Retrieve an output/return value from the finished command.
        /// </summary>
        /// <typeparam name="T">Returned value type</typeparam>
        /// <param name="key">Name of returned parameter (provided previously using 'WithOutput()' or 'WithInOut()' or 'WithReturn()'</param>
        /// <param name="def">Value to return if returned value is null or DbNull</param>
        /// <returns>Converted value</returns>
        public T At<T>(string key, T def) {
            object value = ((IDataParameter)_command.Parameters[_factory.ParameterChar + key]).Value;
            if(value == null) {
                return def;
            }
            if(value is DBNull) {
                return def;
            }
            if(value is T) {
                return (T)value;
            }
            return (T)SysUtil.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Execute command.
        /// </summary>
        public void Execute() {
            _log.TraceMethodCall("Execute()", _command.CommandText);
            QueryStart();
            using(IDbConnection connection = _factory.OpenConnection(_connection)) {
                using(IDbCommand command = CreateExecutableCommand(connection)) {
                    try {
                        command.ExecuteNonQuery();
                    } catch(Exception e) {
                        _log.DebugFormat(e,"Execute(): Text: '{0}', Type: {1}", _command.CommandText, _command.CommandType);
                        throw;
                    } finally {
                        QueryFinished(command);
                    }
                }
            }
        }

        /// <summary>
        /// Execute command and call handler with an open IDataReader on the result set.  
        /// IDataReader and connection will be automatically closed upon completion of the handler.
        /// </summary>
        /// <param name="handler">Handler to invoke</param>
        public void Execute(Action<IDataReader> handler) {
            _log.TraceMethodCall("Execute(Action<IDataReader>)", _command.CommandText);
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            QueryStart();
            using(IDbConnection connection = _factory.OpenConnection(_connection)) {
                using(IDbCommand command = CreateExecutableCommand(connection)) {
                    try {
                        using(IDataReader reader = command.ExecuteReader()) {
                            handler(reader);
                        }
                    } catch(Exception e) {
                        _log.DebugFormat(e, "Execute(handler): Text: '{0}', Type: {1}", _command.CommandText, _command.CommandType);
                        throw;
                    } finally {
                        QueryFinished(command);
                    }
                }
            }            
        }

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Read value</returns>
        public string Read() {
            _log.TraceMethodCall("Read()", _command.CommandText);
            QueryStart();
            using(IDbConnection connection = _factory.OpenConnection(_connection)) {
                using(IDbCommand command = CreateExecutableCommand(connection)) {
                    try {
                        object value = command.ExecuteScalar();
                        if(value == null) {
                            return null;
                        }
                        if(value is DBNull) {
                            return null;
                        }
                        return (string) SysUtil.ChangeType(value, typeof(string));
                    } catch(Exception e) {
                        _log.DebugFormat(e, "Read(): Text: '{0}', Type: {1}", _command.CommandText, _command.CommandType);
                        throw;
                    } finally {
                        QueryFinished(command);
                    }
                }
            }
        }

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        public bool? ReadAsBool() {
            return ReadAs<bool>();
        }

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        public byte? ReadAsByte() {
            return ReadAs<byte>();
        }

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        public short? ReadAsShort() {
            return ReadAs<short>();
        }

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        public ushort? ReadAsUShort() {
            return ReadAs<ushort>();
        }

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        public int? ReadAsInt() {
            return ReadAs<int>();
        }

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        public long? ReadAsLong() {
            return ReadAs<long>();
        }

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        public uint? ReadAsUInt() {
            return ReadAs<uint>();
        }

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        public ulong? ReadAsULong() {
            return ReadAs<ulong>();
        }

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <returns>Converted value</returns>
        public DateTime? ReadAsDateTime() {
            return ReadAs<DateTime>();
        }

        /// <summary>
        /// Execute command and return value from the first column in the first row.
        /// </summary>
        /// <typeparam name="T">Returned value type</typeparam>
        /// <returns>Converted value</returns>
        private T? ReadAs<T>() where T : struct {
            _log.TraceMethodCall("ReadAs<T>()", typeof(T).FullName, _command.CommandText);
            QueryStart();
            using(IDbConnection connection = _factory.OpenConnection(_connection)) {
                using(IDbCommand command = CreateExecutableCommand(connection)) {
                    try {
                        object value = command.ExecuteScalar();
                        if(value == null) {
                            return null;
                        }
                        if(value is DBNull) {
                            return null;
                        }
                        return (T) SysUtil.ChangeType(value, typeof(T));
                    } catch(Exception e) {
                        _log.DebugFormat(e, "ReadAs(): Text: '{0}', Type: {1}", _command.CommandText, _command.CommandType);
                        throw;
                    } finally {
                        QueryFinished(command);
                    }
                }
            }
        }

        private IDbCommand CreateExecutableCommand(IDbConnection connection) {
            IDbCommand command = _factory.CreateQuery(_command.CommandText);
            try {
                command.CommandType = _command.CommandType;
                command.Connection = connection;
                foreach(IDataParameter parameter in _command.Parameters) {
                    IDataParameter parameterCopy = command.CreateParameter();
                    parameterCopy.ParameterName = parameter.ParameterName;
                    parameterCopy.Value = parameter.Value;
                    parameterCopy.Direction = parameter.Direction;
                    command.Parameters.Add(parameterCopy);
                }
            } catch {
                if(command != null) {

                    // must dispose of command in case of failure
                    command.Dispose();
                }
                throw;
            }
            return command;
        }

        /// <summary>
        /// Execute command and read result into a DataSet.
        /// </summary>
        /// <returns>Read DataSet object</returns>
        public DataSet ReadAsDataSet() {
            _log.TraceMethodCall("ReadAsDataSet()", _command.CommandText);
            DataSet result = new DataSet();
            using(IDbConnection connection = _factory.OpenConnection(_connection)) {
                using(IDbCommand command = CreateExecutableCommand(connection)) {
                    try {
                        _factory.CreateAdapter(command).Fill(result);
                    } catch(Exception e) {
                        _log.DebugFormat(e, "ReadAsDataSet(): Text: '{0}', Type: {1}", _command.CommandText, _command.CommandType);
                        throw;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Execute command and read result into an XDoc.
        /// </summary>
        /// <param name="table">Name of the root element</param>
        /// <param name="row">Name of the element created for each row</param>
        /// <returns>Read DataSet object</returns>
        public XDoc ReadAsXDoc(string table, string row) {
            _log.TraceMethodCall("ReadAsXDoc()", _command.CommandText);
            XDoc result = new XDoc(table);
            Execute(delegate(IDataReader reader) {

                // capture row columns
                int count = reader.FieldCount;
                string[] columns = new string[count];
                bool[] attr = new bool[count];
                for(int i = 0; i < count; ++i) {
                    columns[i] = reader.GetName(i);
                    if(columns[i].StartsWith("@")) {
                        attr[i] = true;
                        columns[i] = columns[i].Substring(1);
                    }
                }

                // read records
                while(reader.Read()) {
                    result.Start(row);
                    for(int i = 0; i < count; ++i) {
                        if(!reader.IsDBNull(i)) {
                            string column = columns[i];
                            if(attr[0]) {
                                result.Attr(column, reader.GetValue(i).ToString());
                            } else {
                                result.Elem(column, reader.GetValue(i).ToString());
                            }
                        }
                    }
                    result.End();
                }
            });
            return result;
        }

        /// <summary>
        /// Execute command and convert the first row into an object.
        /// </summary>
        /// <typeparam name="T">Object type to create</typeparam>
        /// <returns>Created object</returns>
        public T ReadAsObject<T>() where T : new() {
            _log.TraceMethodCall("ReadAsObject()", _command.CommandText);
            Dictionary<string, DataColumnField> fields = GetDataFields(typeof(T));

            // read item from database
            T result = default(T);
            Execute(delegate(IDataReader reader) {
                if(reader.Read()) {
                    result = FillObject(new T(), fields, reader);
                }
            });
            return result;
        }

        /// <summary>
        /// Execute command and convert all rows into a list of objects.
        /// </summary>
        /// <typeparam name="T">Object type to create</typeparam>
        /// <returns>List of created objects</returns>
        public List<T> ReadAsObjects<T>() where T : new() {
        	if(_log.IsTraceEnabled()) {
                _log.TraceMethodCall("ReadAsObject<T>()", typeof(T).FullName, _command.CommandText);
            }
            Dictionary<string, DataColumnField> fields = GetDataFields(typeof(T));

            // read item from database
            List<T> result = new List<T>();
            Execute(delegate(IDataReader reader) {
                while(reader.Read()) {
                    result.Add(FillObject(new T(), fields, reader));
                }
            });
            return result;
        }

        private void QueryStart() {
            _stopWatch.Reset();
            _stopWatch.Start();

        }

        private void QueryFinished(IDbCommand command) {
            _stopWatch.Stop();
            if(_stopWatch.Elapsed > SLOW_SQL) {
                _log.WarnFormat("SLOW SQL ({0:0.000}s, database: {2}): {1}", _stopWatch.Elapsed.TotalSeconds, command.CommandText, command.Connection.Database);
            }
            _catalog.FireQueryFinished(this);
        }
    }
}
