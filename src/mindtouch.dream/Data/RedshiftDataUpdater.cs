/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2016 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
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
using System.Data.Common;

namespace MindTouch.Data {
    public class RedshiftDataUpdater : ADataUpdater {

        //--- Class Methods ---
        private static string BuildConnectionString(string server, int port, string dbname, string dbuser, string dbpassword, uint timeout) {
            var optionString = "SSL=true;Sslmode=Require;Timeout=30;";
            if(timeout != uint.MaxValue) {
                optionString += string.Format("CommandTimeout={0};", timeout);
            }
            return string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};{5}", server, port, dbname, dbuser, dbpassword, optionString);
        }
        
        //--- Fields ---
        private readonly DataCatalog _dataCatalog;
        private readonly string _dbName;
        
        //--- Constructors ---
        public RedshiftDataUpdater(string server, int port, string dbname, string dbuser, string dbpassword, string version, uint timeout) {
            if(string.IsNullOrEmpty(version)) {
                _targetVersion = null;
            } else {
                _targetVersion = new VersionInfo(version);
                if(!_targetVersion.IsValid) {
                    throw new VersionInfoException(_targetVersion);
                }
            }
            
            // initialize the data catalog
            var dataFactory = new DataFactory(DbProviderFactories.GetFactory("Npgsql"), "?");
            _dbName = dbname;
            var connectionString = BuildConnectionString(server, port, dbname, dbuser, dbpassword, timeout);
            _dataCatalog = new DataCatalog(dataFactory, connectionString);
            _dataCatalog.TestConnection();
        }

        //--- Methods---
        public override void TestConnection() {
            _dataCatalog.TestConnection();
        }

        protected override object CreateActivatorInstance(Type dataUpgradeType) {
            return Activator.CreateInstance(dataUpgradeType, _dataCatalog, _dbName);
        }
    }
}
