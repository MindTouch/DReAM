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
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Registration;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    // ReSharper disable InconsistentNaming
    [TestFixture]
    public class XDocAutofacContainerConfiguratorTests {

        private ILifetimeScope _hostContainer;
        private ILifetimeScope _serviceContainer;
        private ILifetimeScope _requestContainer;

        [SetUp]
        public void Setup() {
            _hostContainer = new ContainerBuilder().Build(ContainerBuildOptions.Default).BeginLifetimeScope(DreamContainerScope.Host);
            _serviceContainer = _hostContainer.BeginLifetimeScope(DreamContainerScope.Service);
            _requestContainer = _serviceContainer.BeginLifetimeScope(DreamContainerScope.Request);
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
            IsRegisteredInScopeWithName(typeof(Foo), DreamContainerScope.Service, "fooz");
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
            IComponentRegistration registration;
            Assert.IsTrue(_serviceContainer.ComponentRegistry.TryGetRegistration(new TypedService(type), out registration),
                          string.Format("no registration found for type '{0}'", type));
            Assert.AreEqual(InstanceOwnership.OwnedByLifetimeScope, registration.Ownership);
            object instance;
            switch(scope) {
            case DreamContainerScope.Factory:
                Assert.AreEqual(InstanceSharing.None, registration.Sharing);
                break;
            case DreamContainerScope.Host:
                Assert.AreEqual(InstanceSharing.Shared, registration.Sharing);
                Assert.IsTrue(_hostContainer.TryResolveService(new TypedService(type), out instance), "unable to resolve in host");
                Assert.IsTrue(_serviceContainer.TryResolveService(new TypedService(type), out instance), "unable to resolve in service");
                Assert.IsTrue(_requestContainer.TryResolveService(new TypedService(type), out instance), "unable to resolve in request");
                break;
            case DreamContainerScope.Service:
                Assert.AreEqual(InstanceSharing.Shared, registration.Sharing);
                try {
                    Assert.IsFalse(_hostContainer.TryResolveService(new TypedService(type), out instance), "able to resolve in host");
                } catch(DependencyResolutionException) {}
                Assert.IsTrue(_serviceContainer.TryResolveService(new TypedService(type), out instance), "unable to resolve in service");
                Assert.IsTrue(_requestContainer.TryResolveService(new TypedService(type), out instance), "unable to resolve in request");
                break;
            case DreamContainerScope.Request:
                Assert.AreEqual(InstanceSharing.Shared, registration.Sharing);
                try {
                    Assert.IsFalse(_hostContainer.TryResolveService(new TypedService(type), out instance), "able to resolve in host");
                } catch(DependencyResolutionException) {}
                try {
                    Assert.IsFalse(_serviceContainer.TryResolveService(new TypedService(type), out instance), "able to resolve in service");
                } catch(DependencyResolutionException) {}
                Assert.IsTrue(_requestContainer.TryResolveService(new TypedService(type), out instance), "unable to resolve in request");
                break;
            }
        }


        private void IsRegisteredInScopeWithName(Type type, DreamContainerScope scope, string name) {
            IComponentRegistration registration;
            Assert.IsTrue(_serviceContainer.ComponentRegistry.TryGetRegistration(new KeyedService(name, type), out registration),
                          string.Format("no registration found for type '{0}' with name '{1}'", typeof(Foo), "fooz"));
            Assert.AreEqual(InstanceOwnership.OwnedByLifetimeScope, registration.Ownership);
            object instance;
            switch(scope) {
            case DreamContainerScope.Factory:
                Assert.AreEqual(InstanceSharing.None, registration.Sharing);
                break;
            case DreamContainerScope.Host:
                Assert.AreEqual(InstanceSharing.Shared, registration.Sharing);
                Assert.IsTrue(_hostContainer.TryResolveNamed(name, type, out instance), "unable to resolve in host");
                Assert.IsTrue(_serviceContainer.TryResolveNamed(name, type, out instance), "unable to resolve in service");
                Assert.IsTrue(_requestContainer.TryResolveNamed(name, type, out instance), "unable to resolve in request");
                break;
            case DreamContainerScope.Service:
                Assert.AreEqual(InstanceSharing.Shared, registration.Sharing);
                try {
                    Assert.IsFalse(_hostContainer.TryResolveNamed(name, type, out instance), "able to resolve in host");
                } catch(DependencyResolutionException) {}
                Assert.IsTrue(_serviceContainer.TryResolveNamed(name, type, out instance), "unable to resolve in service");
                Assert.IsTrue(_requestContainer.TryResolveNamed(name, type, out instance), "unable to resolve in request");
                break;
            case DreamContainerScope.Request:
                Assert.AreEqual(InstanceSharing.Shared, registration.Sharing);
                try {
                    Assert.IsFalse(_hostContainer.TryResolveNamed(name, type, out instance), "able to resolve in host");
                } catch(DependencyResolutionException) {}
                try {
                    Assert.IsFalse(_serviceContainer.TryResolveNamed(name, type, out instance), "able to resolve in service");
                } catch(DependencyResolutionException) {}
                Assert.IsTrue(_requestContainer.TryResolveNamed(name, type, out instance), "unable to resolve in request");
                break;
            }
        }

        private void Configure(XDoc doc) {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new XDocAutofacContainerConfigurator(doc, DreamContainerScope.Service));
            builder.Update(_hostContainer.ComponentRegistry);
        }

        public class Foo : IFoo, IBaz {
            public Foo() { }
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
