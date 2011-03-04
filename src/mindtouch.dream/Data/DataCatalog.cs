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

using MindTouch.Xml;

namespace MindTouch.Data {

    /// <summary>
    /// Provides a database catalog abstraction.
    /// </summary>
    public interface IDataCatalog {

        /// <summary>
        /// Create a new query command.
        /// </summary>
        /// <param name="query">SQL query string.</param>
        /// <returns>Query instance.</returns>
        IDataCommand NewQuery(string query);

        /// <summary>
        /// Create new a read-only query command.
        /// </summary>
        /// <param name="query">SQL query string.</param>
        /// <returns>Query instance.</returns>
        IDataCommand NewReadOnlyQuery(string query);

        /// <summary>
        /// Create a new query command.
        /// </summary>
        /// <param name="query">SQL query string.</param>
        /// <param name="readonly"><see langword="True"/> if the query is read-only.</param>
        /// <returns>New Query instance.</returns>
        IDataCommand NewQuery(string query, bool @readonly);
    }

    /// <summary>
    /// Provides a database catalog abstraction.
    /// </summary>
    public class DataCatalog : IDataCatalog {

        //--- Fields ---
        private readonly DataFactory _factory;
        private readonly string _connection;
        private readonly string _readonlyconnection;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="factory">Factory to use for command construction and query execution.</param>
        /// <param name="connectionString">Database connection string.</param>
        public DataCatalog(DataFactory factory, string connectionString) {
            if(factory == null) {
                throw new ArgumentNullException("factory");
            }
            if(connectionString == null) {
                throw new ArgumentNullException("connectionString");
            }
            this._factory = factory;
            _connection = connectionString;
        }

        /// <summary>
        /// This constructor is deprecated, please use use the constructor requiring a connectionString instead
        /// </summary>
        [Obsolete("This constructor is deprecated, please use use the constructor requiring a connectionString instead")]
        public DataCatalog(DataFactory factory, XDoc config) {
            if(factory == null) {
                throw new ArgumentNullException("factory");
            }
            if(config == null) {
                throw new ArgumentNullException("config");
            }
            _factory = factory;

            // compose connection string from config document
            string server = config["db-server"].AsText ?? "localhost";
            int port = config["db-port"].AsInt ?? 3306;
            string catalog = config["db-catalog"].AsText;
            string user = config["db-user"].AsText;
            string password = config["db-password"].AsText ?? string.Empty;
            string options = config["db-options"].AsText;
            if(string.IsNullOrEmpty(catalog)) {
                throw new ArgumentNullException("config/catalog");
            }
            if(string.IsNullOrEmpty(user)) {
                throw new ArgumentNullException("config/user");
            }
            _connection = string.Format("Server={0};Port={1};Database={2};Uid={3};Pwd={4};{5}", server, port, catalog, user, password, options);

            // compose read-only connection string
            string readonly_server = config["db-readonly-server"].AsText ?? server;
            int readonly_port = config["db-readonly-port"].AsInt ?? port;
            string readonly_catalog = config["db-readonly-catalog"].AsText ?? catalog;
            string readonly_user = config["db-readonly-user"].AsText ?? user;
            string readonly_password = config["db-readonly-password"].AsText ?? password;
            string readonly_options = config["db-readonly-options"].AsText ?? options;
            _readonlyconnection = string.Format("Server={0};Port={1};Database={2};Uid={3};Pwd={4};{5}", readonly_server, readonly_port, readonly_catalog, readonly_user, readonly_password, readonly_options);
        }

        //--- Properties ---
        
        /// <summary>
        /// This property bypasses the safety measures provided by MindTouch.Data objects.  Please avoid using it if possible.
        /// </summary>
        [Obsolete("This property bypasses the safety measures provided by MindTouch.Data objects.  Please avoid using it if possible.")]
        public string ConnectionString { get { return _connection; } }

        //--- Events ---

        /// <summary>
        /// Notification of execution completion of an <see cref="DataCommand"/> created by this instance.
        /// </summary>
        public event Action<IDataCommand> OnQueryFinished;

        //--- Methods ---       

        /// <summary>
        /// Create a new query command.
        /// </summary>
        /// <param name="query">SQL query string.</param>
        /// <returns>Query instance.</returns>
        public DataCommand NewQuery(string query) {
            return NewQuery(query, false);
        }

        IDataCommand IDataCatalog.NewQuery(string query) {
            return NewQuery(query, false);
        }

        /// <summary>
        /// Create new a read-only query command.
        /// </summary>
        /// <param name="query">SQL query string.</param>
        /// <returns>Query instance.</returns>
        public DataCommand NewReadOnlyQuery(string query) {
            return NewQuery(query, true);
        }

        IDataCommand IDataCatalog.NewReadOnlyQuery(string query) {
            return NewReadOnlyQuery(query);
        }

        /// <summary>
        /// Create a new query command.
        /// </summary>
        /// <param name="query">SQL query string.</param>
        /// <param name="readonly"><see langword="True"/> if the query is read-only.</param>
        /// <returns>New Query instance.</returns>
        public DataCommand NewQuery(string query, bool @readonly) {
            return new DataCommand(_factory, this, @readonly ? _readonlyconnection : _connection, _factory.CreateQuery(query));
        }

        IDataCommand IDataCatalog.NewQuery(string query, bool @readonly) {
            return NewQuery(query, @readonly);
        }

        /// <summary>
        /// Create a new stored procedure command.
        /// </summary>
        /// <param name="name">Name of the stored procedure.</param>
        /// <returns>Stored procedure command.</returns>
        public DataCommand NewProcedure(string name) {
            return NewProcedure(name, false);
        }

        /// <summary>
        /// Create a new read-only stored procedure command.
        /// </summary>
        /// <param name="name">Name of the stored procedure.</param>
        /// <returns>Stored procedure command.</returns>
        public DataCommand NewReadOnlyProcedure(string name) {
            return NewProcedure(name, true);
        }

        /// <summary>
        /// Create a new stored procedure command.
        /// </summary>
        /// <param name="name">Name of the stored procedure.</param>
        /// <param name="readonly"><see langword="True"/> if the query is read-only.</param>
        /// <returns>Stored procedure command.</returns>
        public DataCommand NewProcedure(string name, bool @readonly) {
            return new DataCommand(_factory, this, @readonly ? _readonlyconnection : _connection, _factory.CreateProcedure(name));
        }

        /// <summary>
        /// Test the database connection.
        /// </summary>
        public void TestConnection() {
            TestConnection(false);
        }

        /// <summary>
        /// Test the read-only database connection.
        /// </summary>
        /// <param name="readonly"></param>
        public void TestConnection(bool @readonly) {
            using(IDbConnection testConnection = _factory.OpenConnection(@readonly ? _readonlyconnection : _connection)) {
                testConnection.Close();
            }
        }

        internal void FireQueryFinished(IDataCommand cmd) {
            if(OnQueryFinished != null) {
                OnQueryFinished(cmd);
            }
        }
    }
}
