/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
using System.Web;
using log4net;
using MindTouch.Dream;

namespace MindTouch.Sample {
    public class MvcApplication : HttpApplication {

        //--- Class Fields ---
        protected static readonly ILog _log = LogUtils.CreateLog();
        
        //--- Methods ---
        protected void Application_Start() {
            log4net.Config.XmlConfigurator.Configure();
            _log.Debug("application start");
            DreamApplication.CreateInHttpApplication(this);
        }
    }
}