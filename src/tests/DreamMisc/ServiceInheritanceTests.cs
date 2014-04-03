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

using System.Collections.Generic;

using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Dream.Test {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class ServiceInheritanceTests {

        private DreamHostInfo _hostInfo;
        private DreamServiceInfo _parent;
        private DreamServiceInfo _child;

        [TestFixtureSetUp]
        public void Init() {
            _hostInfo = DreamTestHelper.CreateRandomPortHost();
            _parent = DreamTestHelper.CreateService(_hostInfo, typeof(InheritanceParent), "parent");
            _child = DreamTestHelper.CreateService(_hostInfo, typeof(InheritanceChild), "child");
        }

        [Test]
        public void Parent_blueprint_has_all_members() {
            DreamMessage response = _parent.AtLocalHost.At("@blueprint").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            XDoc blueprint = response.ToDocument();
            Assert.AreEqual("public", blueprint["features/feature[pattern='GET:public']/access"].AsText);
            Assert.AreEqual("internal", blueprint["features/feature[pattern='GET:internal']/access"].AsText);
            Assert.AreEqual("private", blueprint["features/feature[pattern='GET:protected']/access"].AsText);
            Assert.AreEqual("private", blueprint["features/feature[pattern='GET:private']/access"].AsText);
        }

        [Test]
        public void Child_blueprint_has_all_members_except_private() {
            DreamMessage response = _child.AtLocalHost.At("@blueprint").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            XDoc blueprint = response.ToDocument();
            Assert.AreEqual("public", blueprint["features/feature[pattern='GET:public']/access"].AsText);
            Assert.AreEqual("internal", blueprint["features/feature[pattern='GET:internal']/access"].AsText);
            Assert.AreEqual("private", blueprint["features/feature[pattern='GET:protected']/access"].AsText);
            Assert.IsTrue(blueprint["features/feature[pattern='GET:private']"].IsEmpty);
        }

    }

    [DreamService("MindTouch Test Parent ", "Copyright (c) 2006-2014 MindTouch, Inc.",
        SID = new string[] { "http://services.mindtouch.com/dream/test/2008/11/inheritance/parent" }
    )]
    public class InheritanceParent : DreamService {

        //--- Features ---
        [DreamFeature("GET:public", "public feature")]
        public Yield Public(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:internal", "public feature")]
        internal Yield Internal(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:protected", "public feature")]
        protected Yield Protected(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:private", "public feature")]
        private Yield Private(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            response.Return(DreamMessage.Ok());
            yield break;
        }
    }

    [DreamService("MindTouch Test Parent ", "Copyright (c) 2006-2014 MindTouch, Inc.",
       SID = new string[] { "http://services.mindtouch.com/dream/test/2008/11/inheritance/parent" }
    )]
    public class InheritanceChild : InheritanceParent { }


}
