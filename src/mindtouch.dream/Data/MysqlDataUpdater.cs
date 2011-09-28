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
using System.Text;

namespace MindTouch.Data {
    public class MysqlDataUpdater : ADataUpdater {

        //--- Fields ---
        private DataCatalog _dataCatalog;
        
        //--- Constructors ---
        public MysqlDataUpdater(string server, int port, string dbname, string dbuser, string dbpassword, string version) {
            if(string.IsNullOrEmpty(version)) {
                _targetVersion = null;
            } else {
                _targetVersion = new VersionInfo(version);
                if(!_targetVersion.IsValid) {
                    throw new VersionInfoException(_targetVersion);
                }
            }
            
            // initialize the data catalog
            var dataFactory = new DataFactory("MySql.Data", "?");
            var connectionString = BuildConnectionString(server, port, dbname, dbuser, dbpassword);
            _dataCatalog = new DataCatalog(dataFactory, connectionString);
            _dataCatalog.TestConnection();
        }

        //--- Methods---
        public override void TestConnection() {
            _dataCatalog.TestConnection();
        }

        public void ChangeDatabase(string server, int port, string dbname, string dbuser, string dbpassword) {
            var dataFactory = new DataFactory("Mysql.Data", "?");
            var connectionString = BuildConnectionString(server, port, dbname, dbuser, dbpassword);
            _dataCatalog = new DataCatalog(dataFactory, connectionString);
            _dataCatalog.TestConnection();
        }

        protected override object CreateActivatorInstance(Type dataUpgradeType) {
            return Activator.CreateInstance(dataUpgradeType, _dataCatalog);
        }

        private string BuildConnectionString(string server, int port, string dbname, string dbuser, string dbpassword) {
            StringBuilder connectionString = new StringBuilder();
            connectionString.AppendFormat("Server={0};", server);
            connectionString.AppendFormat("Port={0};", port);
            connectionString.AppendFormat("Database={0};", dbname);
            connectionString.AppendFormat("User Id={0};", dbuser);
            connectionString.AppendFormat("Password={0};", dbpassword);
            return connectionString.ToString();
        }
    }
}
