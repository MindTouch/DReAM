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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MindTouch.Data;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Dream.Test.Data {

    internal interface IMockDataCommandOwner {
        void RegisterExpectation(MockDataCatalog.MockDataCommand cmd, int expectedCalls);
        MockDataCatalog.MockDataCommand GetExpectation(MockDataCatalog.MockDataCommand cmd);
    }

    /// <summary>
    /// Provides a mocking framework for <see cref="IDataCatalog"/>.
    /// </summary>
    /// <remarks>
    /// This framework is not complete and represents a work in progress.
    /// </remarks>
    public class MockDataCatalog : IDataCatalog, IMockDataCommandOwner {

        //--- Types ---

        /// <summary>
        /// Provides an <see cref="IDataCommand"/> mock that matches actual calls based on its command signature and is configured via a fluent
        /// api mirroring the <see cref="IDataCommand"/> fluent api. Configuration of calls are order sensitive.
        /// </summary>
        public class MockDataCommand : IDataCommand {

            //--- Class Methods ---
            private static string Clean(string query) {
                return _EXCESS_WHITESPACE.Replace(_LEADING_TRAILING_WHITESPACE.Replace(query, ""), m => {
                    if(m.Groups[2].Success) {
                        return m.Groups[2].Value + " " + m.Groups[3].Value;
                    }
                    return " ";
                });
            }

            //--- Fields ---
            private static readonly Regex _LEADING_TRAILING_WHITESPACE = new Regex(@"(^\s+|\s+$)", RegexOptions.Compiled | RegexOptions.Multiline);
            private static readonly Regex _EXCESS_WHITESPACE = new Regex(@"((\S)\n+(\S)|\s\s+)", RegexOptions.Compiled | RegexOptions.Multiline);
            private readonly IMockDataCommandOwner _owner;
            private readonly int _expectedCalls;
            private readonly StringBuilder _signature = new StringBuilder();
            private readonly bool _isExpectation;
            private IDataReader _dataReader;
            private object _dataSlot;

            //--- Constructors ---
            internal MockDataCommand(IMockDataCommandOwner owner, int expectedCalls, string query) {
                _owner = owner;
                _expectedCalls = expectedCalls;
                _isExpectation = _expectedCalls > 0;
                _signature.Append(Clean(query));
            }

            //--- Properties ---
            internal string Signature { get { return _signature.ToString(); } }

            //--- Methods ---

            /// <summary>
            /// Define a parameter to expect.
            /// </summary>
            /// <param name="key">Expected key.</param>
            /// <param name="value">Expected value.</param>
            /// <returns>Same instance.</returns>
            public MockDataCommand With(string key, object value) {
                _signature.AppendFormat("-With({0},{1})", key, value);
                return this;
            }

            /// <summary>
            /// Define a parameter to expect.
            /// </summary>
            /// <param name="key">Expected key.</param>
            /// <param name="value">Expected value.</param>
            /// <returns>Same instance.</returns>
            public MockDataCommand WithInOut(string key, object value) {
                _signature.AppendFormat("-WithInOut({0},{1})", key, value);
                return this;
            }

            /// <summary>
            /// Define an output parameter to expect.
            /// </summary>
            /// <param name="key">Expected key.</param>
            /// <returns>Same instance.</returns>
            public MockDataCommand WithOutput(string key) {
                _signature.AppendFormat("-WithOutput({0})", key);
                return this;
            }

            /// <summary>
            /// Define an return parameter to expect.
            /// </summary>
            /// <param name="key">Expected key.</param>
            /// <returns>Same instance.</returns>
            public MockDataCommand WithReturn(string key) {
                _signature.AppendFormat("-WithReturn({0})", key);
                return this;
            }

            /// <summary>
            /// Defines the execution command to expect.
            /// </summary>
            public void Execute() {
                _signature.AppendFormat("-Execute()");
                if(_isExpectation) {
                    _owner.RegisterExpectation(this, _expectedCalls);
                    return;
                }
                _owner.GetExpectation(this);
            }

            /// <summary>
            /// Defines the execution command to expect.
            /// </summary>
            /// <param name="dataReader">The data reader to return to the caller.</param>
            public void Execute(IDataReader dataReader) {
                _signature.AppendFormat("-Execute(<handler>)");
                _dataReader = dataReader;
                _owner.RegisterExpectation(this, _expectedCalls);
                return;
            }

            /// <summary>
            /// Defines the execution command to expect.
            /// </summary>
            public void Read() {
                ReadInternal();
            }

            /// <summary>
            /// Defines the execution command to expect.
            /// </summary>
            public void ReadAsBool() {
                ReadAs<bool>();
            }

            /// <summary>
            /// Defines the execution command to expect.
            /// </summary>
            public void ReadAsByte() {
                ReadAs<byte>();
            }

            /// <summary>
            /// Defines the execution command to expect.
            /// </summary>
            public void ReadAsUShort() {
                ReadAs<ushort>();
            }

            /// <summary>
            /// Defines the execution command to expect.
            /// </summary>
            public void ReadAsShort() {
                ReadAs<short>();
            }

            /// <summary>
            /// Defines the execution command to expect.
            /// </summary>
            public void ReadAsUInt() {
                ReadAs<uint>();
            }

            /// <summary>
            /// Defines the execution command to expect.
            /// </summary>
            public void ReadAsInt() {
                ReadAs<int>();
            }

            /// <summary>
            /// Defines the execution command to expect.
            /// </summary>
            public void ReadAsULong() {
                ReadAs<ulong>();
            }

            /// <summary>
            /// Defines the execution command to expect.
            /// </summary>
            public void ReadAsLong() {
                ReadAs<long>();
            }

            /// <summary>
            /// Defines the execution command to expect.
            /// </summary>
            public void ReadAsDateTime() {
                ReadAs<DateTime>();
            }

            /// <summary>
            /// Define data object to return on read operation.
            /// </summary>
            /// <param name="data">Data object.</param>
            /// <returns>Same instance.</returns>
            public MockDataCommand WithExpectedReturnValue(object data) {
                _dataSlot = data;
                return this;
            }

            private T? ReadAs<T>() where T : struct {
                _signature.AppendFormat("-Read[{0}]()", typeof(T));
                if(_isExpectation) {
                    _owner.RegisterExpectation(this, _expectedCalls);
                    return null;
                }
                var expectation = _owner.GetExpectation(this);
                return (T?)expectation._dataSlot;
            }

            private string ReadInternal() {
                _signature.AppendFormat("-Read()");
                if(_isExpectation) {
                    _owner.RegisterExpectation(this, _expectedCalls);
                    return null;
                }
                var expectation = _owner.GetExpectation(this);
                return (string)expectation._dataSlot;
            }

            #region --- IDataCommand Members ---
            bool IDataCommand.IsStoredProcedure { get { throw new NotImplementedException(); } }
            TimeSpan IDataCommand.ExecutionTime { get { throw new NotImplementedException(); } }

            IDataCommand IDataCommand.With(string key, object value) {
                return With(key, value);
            }

            IDataCommand IDataCommand.WithInOut(string key, object value) {
                return WithInOut(key, value);
            }

            IDataCommand IDataCommand.WithOutput(string key) {
                return WithOutput(key);
            }

            IDataCommand IDataCommand.WithReturn(string key) {
                return WithReturn(key);
            }

            T IDataCommand.At<T>(string key) {
                throw new NotImplementedException();
            }

            T IDataCommand.At<T>(string key, T def) {
                throw new NotImplementedException();
            }

            void IDataCommand.Execute() {
                Execute();
            }

            void IDataCommand.Execute(Action<IDataReader> handler) {
                _signature.AppendFormat("-Execute(<handler>)");
                var expectation = _owner.GetExpectation(this);
                handler(expectation._dataReader);
            }

            string IDataCommand.Read() {
                return ReadInternal();
            }

            bool? IDataCommand.ReadAsBool() {
                return ReadAs<bool>();
            }

            byte? IDataCommand.ReadAsByte() {
                return ReadAs<byte>();
            }

            ushort? IDataCommand.ReadAsUShort() {
                return ReadAs<ushort>();
            }

            short? IDataCommand.ReadAsShort() {
                return ReadAs<short>();
            }

            uint? IDataCommand.ReadAsUInt() {
                return ReadAs<uint>();
            }

            int? IDataCommand.ReadAsInt() {
                return ReadAs<int>();
            }

            ulong? IDataCommand.ReadAsULong() {
                return ReadAs<ulong>();
            }

            long? IDataCommand.ReadAsLong() {
                return ReadAs<long>();
            }

            DateTime? IDataCommand.ReadAsDateTime() {
                return ReadAs<DateTime>();
            }

            XDoc IDataCommand.ReadAsXDoc(string table, string row) {
                throw new NotImplementedException();
            }

            T IDataCommand.ReadAsObject<T>() {
                throw new NotImplementedException();
            }

            List<T> IDataCommand.ReadAsObjects<T>() {
                throw new NotImplementedException();
            }
            #endregion
        }

        /// <summary>
        /// Provides a <see cref="IDataReader"/> mock backed by a two-dimensional object array.
        /// </summary>
        public class MockDataReader : IDataReader {
            private readonly Dictionary<string, int> _keyLookup;
            private readonly string[] _keys;
            private readonly object[][] _table;
            private int _row = -1;

            /// <summary>
            /// Create a new mock instance.
            /// </summary>
            /// <param name="keys">Array of field names.</param>
            /// <param name="table">Array of data rows, each containing an array of values to match the field names.</param>
            public MockDataReader(string[] keys, object[][] table) {
                _keys = keys;
                _table = table;
                int i = 0;
                _keyLookup = _keys.ToDictionary(k => k, v => i++);
            }

            void IDisposable.Dispose() {
            }

            string IDataRecord.GetName(int i) {
                return _keys[i];
            }

            string IDataRecord.GetDataTypeName(int i) {
                throw new NotImplementedException();
            }

            Type IDataRecord.GetFieldType(int i) {
                throw new NotImplementedException();
            }

            object IDataRecord.GetValue(int i) {
                return _table[_row][i];
            }

            int IDataRecord.GetValues(object[] values) {
                throw new NotImplementedException();
            }

            int IDataRecord.GetOrdinal(string name) {
                throw new NotImplementedException();
            }

            bool IDataRecord.GetBoolean(int i) {
                throw new NotImplementedException();
            }

            byte IDataRecord.GetByte(int i) {
                throw new NotImplementedException();
            }

            long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) {
                throw new NotImplementedException();
            }

            char IDataRecord.GetChar(int i) {
                throw new NotImplementedException();
            }

            long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) {
                throw new NotImplementedException();
            }

            Guid IDataRecord.GetGuid(int i) {
                throw new NotImplementedException();
            }

            short IDataRecord.GetInt16(int i) {
                throw new NotImplementedException();
            }

            int IDataRecord.GetInt32(int i) {
                throw new NotImplementedException();
            }

            long IDataRecord.GetInt64(int i) {
                throw new NotImplementedException();
            }

            float IDataRecord.GetFloat(int i) {
                throw new NotImplementedException();
            }

            double IDataRecord.GetDouble(int i) {
                throw new NotImplementedException();
            }

            string IDataRecord.GetString(int i) {
                throw new NotImplementedException();
            }

            decimal IDataRecord.GetDecimal(int i) {
                throw new NotImplementedException();
            }

            DateTime IDataRecord.GetDateTime(int i) {
                throw new NotImplementedException();
            }

            IDataReader IDataRecord.GetData(int i) {
                throw new NotImplementedException();
            }

            bool IDataRecord.IsDBNull(int i) {
                throw new NotImplementedException();
            }

            int IDataRecord.FieldCount {
                get { return _keys.Length; }
            }

            object IDataRecord.this[int i] {
                get { return _table[_row][i]; }
            }

            object IDataRecord.this[string name] {
                get { return _table[_row][_keyLookup[name]]; }
            }

            void IDataReader.Close() {
                throw new NotImplementedException();
            }

            DataTable IDataReader.GetSchemaTable() {
                throw new NotImplementedException();
            }

            bool IDataReader.NextResult() {
                throw new NotImplementedException();
            }

            bool IDataReader.Read() {
                _row++;
                return _row < _table.Length;
            }

            int IDataReader.Depth {
                get { throw new NotImplementedException(); }
            }

            bool IDataReader.IsClosed {
                get { throw new NotImplementedException(); }
            }

            int IDataReader.RecordsAffected {
                get { throw new NotImplementedException(); }
            }
        }

        private readonly Dictionary<string, List<MockDataCommand>> _expectations = new Dictionary<string, List<MockDataCommand>>();

        IDataCommand IDataCatalog.NewQuery(string query) {
            return ((IDataCatalog)this).NewQuery(query, false);
        }

        IDataCommand IDataCatalog.NewReadOnlyQuery(string query) {
            return ((IDataCatalog)this).NewQuery(query, true);
        }

        IDataCommand IDataCatalog.NewQuery(string query, bool @readonly) {
            return new MockDataCommand(this, 0, @readonly ? "READONLY-" : "" + query);
        }

        void IDataCatalog.TestConnection() {
            throw new NotImplementedException();
        }

        void IDataCatalog.TestConnection(bool @readonly) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a new mock command to intercept a specific <see cref="IDataCatalog.NewQuery(string)"/> call.
        /// </summary>
        /// <param name="query">Sql query string.</param>
        /// <param name="expectedCalls">Number of times this call is expected to occur.</param>
        /// <returns>New mock command.</returns>
        public MockDataCommand ExpectNewQuery(string query, int expectedCalls) {
            return ExpectNewQuery(query, false, expectedCalls);
        }

        /// <summary>
        /// Create a new mock command to intercept a specific <see cref="IDataCatalog.NewReadOnlyQuery"/> call.
        /// </summary>
        /// <param name="query">Sql query string.</param>
        /// <param name="expectedCalls">Number of times this call is expected to occur.</param>
        /// <returns>New mock command.</returns>
        public MockDataCommand ExpectNewReadOnlyQuery(string query, int expectedCalls) {
            return ExpectNewQuery(query, true, expectedCalls);
        }

        /// <summary>
        /// Create a new mock command to intercept a specific <see cref="IDataCatalog.NewQuery(string,bool)"/> call.
        /// </summary>
        /// <param name="query">Sql query string.</param>
        /// <param name="readonly"><see langword="True"/> if the query is readonly.</param>
        /// <param name="expectedCalls">Number of times this call is expected to occur.</param>
        /// <returns>New mock command.</returns>
        public MockDataCommand ExpectNewQuery(string query, bool @readonly, int expectedCalls) {
            var cmd = new MockDataCommand(this, expectedCalls, @readonly ? "READONLY-" : "" + query);
            return cmd;
        }

        /// <summary>
        /// Verify that the setup expectations occured.
        /// </summary>
        /// <returns></returns>
        public bool Verify() {
            VerificationFailure = null;
            if(_expectations.Count == 0) {
                return true;
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Leftover expectations:");
            foreach(var leftover in _expectations) {
                sb.AppendFormat("{0} times: {1}\r\n", leftover.Value.Count, leftover.Key);
            }
            VerificationFailure = sb.ToString();
            return false;
        }


        /// <summary>
        /// A text explanation of why <see cref="Verify"/> failed.
        /// </summary>
        public string VerificationFailure { get; private set; }

        void IMockDataCommandOwner.RegisterExpectation(MockDataCommand cmd, int expectedCalls) {
            List<MockDataCommand> cmds;
            if(!_expectations.TryGetValue(cmd.Signature, out cmds)) {
                cmds = new List<MockDataCommand>();
                _expectations.Add(cmd.Signature, cmds);
            }
            for(int i = 0; i < expectedCalls; i++) {
                cmds.Add(cmd);
            }
        }

        MockDataCommand IMockDataCommandOwner.GetExpectation(MockDataCommand cmd) {
            List<MockDataCommand> cmds;
            if(!_expectations.TryGetValue(cmd.Signature, out cmds)) {
                throw new AssertionException(string.Format("cannot find an expectation for query with signature: {0}", cmd.Signature));
            }
            var expectation = cmds[0];
            cmds.RemoveAt(0);
            if(cmds.Count == 0) {
                _expectations.Remove(cmd.Signature);
            }
            return expectation;
        }
    }
}