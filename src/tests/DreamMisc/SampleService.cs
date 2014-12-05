/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
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
using System.Collections.Generic;
using log4net;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.Test {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Test Service", "Copyright (c) 2006-2014 MindTouch, Inc.", 
        Info = "http://www.mindtouch.com",
        SID = new string[] { "http://services.mindtouch.com/dream/test/2007/03/sample" }
    )]
    public class SampleService : DreamService {

        // --- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //-- Fields ---
        private Plug _inner;

        //--- Features ---
        [DreamFeature("GET:foo", "foo feature")]
        public Yield GetFoo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            response.Return(DreamMessage.NotImplemented(""));
            yield break;
        }

        [DreamFeature("GET:bar", "foo feature")]
        public Yield GetBar(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            response.Return(DreamMessage.NotImplemented(""));
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            yield return CreateService("inner", "http://services.mindtouch.com/dream/test/2007/03/sample-inner", new XDoc("config").Start("prologue").Attr("name", "dummy").Value("p3").End().Start("epilogue").Attr("name", "dummy").Value("e3").End(), new Result<Plug>()).Set(v => _inner = v);
            result.Return();
        }

        protected override Yield Stop(Result result) {
            if(_inner != null) {
                yield return _inner.Delete(new Result<DreamMessage>()).CatchAndLog(_log);
                _inner = null;
            }
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }
    }

    [DreamService("MindTouch Inner Test Service", "Copyright (c) 2006-2014 MindTouch, Inc.", 
        Info = "http://www.mindtouch.com",
        SID = new string[] { "http://services.mindtouch.com/dream/test/2007/03/sample-inner" }
    )]
    public class SampleInnerService : DreamService {

        //--- Features ---
        [DreamFeature("GET:foo", "foo feature")]
        public Yield GetFoo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            response.Return(DreamMessage.NotImplemented(""));
            yield break;
        }

        [DreamFeature("GET:bar", "foo feature")]
        public Yield GetBar(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            response.Return(DreamMessage.NotImplemented(""));
            yield break;
        }
    }

    [DreamService("MindTouch Bad Test Service", "Copyright (c) 2006-2014 MindTouch, Inc.",
        Info = "http://www.mindtouch.com",
        SID = new string[] { "http://services.mindtouch.com/dream/test/2007/03/bad" }
    )]
    public class SampleBadService : DreamService {

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            throw new Exception("this will never work!");
        }
    }
}
