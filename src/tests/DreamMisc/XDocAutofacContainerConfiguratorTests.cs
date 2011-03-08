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
using System.Linq;
using Autofac;
using Autofac.Builder;
using Autofac.Component;
using Autofac.Component.Scope;
using Autofac.Component.Tagged;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    // ReSharper disable InconsistentNaming
    [TestFixture]
    public class XDocAutofacContainerConfiguratorTests {

        private IContainer _hostContainer;
        private IContainer _serviceContainer;
        private IContainer _requestContainer;

        [SetUp]
        public void Setup() {
            _hostContainer = new ContainerBuilder().Build();
            _hostContainer.TagWith(DreamContainerScope.Host);
            _serviceContainer = _hostContainer.CreateInnerContainer();
            _serviceContainer.TagWith(DreamContainerScope.Service);
            _requestContainer = _serviceContainer.CreateInnerContainer();
            _requestContainer.TagWith(DreamContainerScope.Request);
        }

        [Test]
        public void Component_without_scope_gets_default_scope() {
            Configure(new XDoc("components")
                .Start("component")
                    .Attr("type", typeof(Foo).AssemblyQualifiedName)
                .End());
            IsRegisteredInScope(typeof(Foo), DreamContainerScope.Service);
        }

        [Test]
        public void Can_register_host_scoped_component() {
            Configure(new XDoc("components")
                .Start("component")
                    .Attr("scope", "host")
                    .Attr("type", typeof(Foo).AssemblyQualifiedName)
                .End());
            IsRegisteredInScope(typeof(Foo), DreamContainerScope.Host);
        }

        [Test]
        public void Can_register_service_scoped_component() {
            Configure(new XDoc("components")
                .Start("component")
                    .Attr("scope", "service")
                    .Attr("type", typeof(Foo).AssemblyQualifiedName)
                .End());
            IsRegisteredInScope(typeof(Foo), DreamContainerScope.Service);
        }

        [Test]
        public void Can_register_request_scoped_component() {
            Configure(new XDoc("components")
                .Start("component")
                    .Attr("scope", "request")
                    .Attr("type", typeof(Foo).AssemblyQualifiedName)
                .End());
            IsRegisteredInScope(typeof(Foo), DreamContainerScope.Request);
        }

        [Test]
        public void Can_register_factory_scoped_component() {
            Configure(new XDoc("components")
                .Start("component")
                    .Attr("scope", "factory")
                    .Attr("type", typeof(Foo).AssemblyQualifiedName)
                .End());
            IsRegisteredInScope(typeof(Foo), DreamContainerScope.Factory);
        }

        [Test]
        public void Can_register_named_component() {
            Configure(new XDoc("components")
                .Start("component")
                    .Attr("name", "fooz")
                    .Attr("type", typeof(Foo).AssemblyQualifiedName)
                .End());
            Assert.IsTrue(_hostContainer.IsRegistered("fooz"));
            //var registration = _hostContainer.ComponentRegistrations.Where(x => x.Descriptor.BestKnownImplementationType == typeof(Foo)).First();
        }

        [Test]
        public void Can_register_class_as_service() {
            Configure(new XDoc("components")
                .Start("component")
                    .Attr("implementation", typeof(Foo).AssemblyQualifiedName)
                    .Attr("type", typeof(IFoo).AssemblyQualifiedName)
                .End());
            var foo = _requestContainer.Resolve<IFoo>();
            Assert.AreEqual(typeof(Foo), foo.GetType());
        }

        [Test]
        public void Can_specify_parameters() {
            Configure(new XDoc("components")
                .Start("component")
                    .Attr("type", typeof(Foo).AssemblyQualifiedName)
                    .Start("parameters")
                        .Start("parameter").Attr("name", "y").Attr("value", 17).End()
                        .Start("parameter").Attr("name", "x").Attr("value", 42).End()
                    .End()
                .End());
            var foo = _requestContainer.Resolve<Foo>();
            Assert.AreEqual(42, foo.X);
            Assert.AreEqual(17, foo.Y);
        }

        private void IsRegisteredInScope(Type type, DreamContainerScope scope) {
            IComponentRegistration registration = GetRegistration(type);
            if(scope == DreamContainerScope.Factory) {
                var componentRegistration = registration as Registration;
                Assert.IsNotNull(componentRegistration);
                Assert.AreEqual(typeof(FactoryScope), componentRegistration.Scope.GetType());
            } else {
                var taggedRegistration = registration as TaggedRegistration<DreamContainerScope>;
                Assert.IsNotNull(taggedRegistration);
                Assert.AreEqual(scope, taggedRegistration.Tag);
            }
        }

        private IComponentRegistration GetRegistration(Type type) {
            IComponentRegistration registration;
            Assert.IsTrue(_serviceContainer.TryGetDefaultRegistrationFor(new TypedService(type), out registration),
                          string.Format("no registration found for type '{0}'", type));
            return registration;
        }

        private void Configure(XDoc doc) {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new XDocAutofacContainerConfigurator(doc, DreamContainerScope.Service));
            builder.Build(_hostContainer);
        }

        public class Foo : IFoo, IBaz {
            public Foo() {}
            public Foo(int x, int y) {
                X = x;
                Y = y;
            }
            public int X { get; private set; }
            public int Y { get; private set; }
        }
        public interface IFoo { }
        public interface IBaz { }
    }
    // ReSharper restore InconsistentNaming
}
