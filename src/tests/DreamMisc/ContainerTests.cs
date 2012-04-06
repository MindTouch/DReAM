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
using Autofac;
using Autofac.Builder;
using MindTouch.Dream.Test.ContainerTestClasses;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class ContainerTests {

        [Test]
        public void Can_set_request_scope_registration_on_provided_container() {
            var builder = new ContainerBuilder();
            builder.RegisterType<Foo>().As<IFoo>().InScope(DreamContainerScope.Request);
            var hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config"), builder.Build(ContainerBuildOptions.Default));
            var service = hostInfo.CreateService(typeof(ContainerTestService), "test");
            CheckResponse(service.AtLocalHost.At("scope").Get(new Result<DreamMessage>()).Wait());
            var requestScope1 = ContainerTestService.Scoped;
            Assert.IsNotNull(requestScope1);
            CheckResponse(service.AtLocalHost.At("scope").Get(new Result<DreamMessage>()).Wait());
            var requestScope2 = ContainerTestService.Scoped;
            Assert.IsNotNull(requestScope2);
            Assert.AreNotSame(requestScope1, requestScope2);
        }

        [Test]
        public void Can_set_service_scope_registration_on_provided_container() {
            var builder = new ContainerBuilder();
            builder.RegisterType<Foo>().As<IFoo>().InScope(DreamContainerScope.Service);
            var hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config"), builder.Build(ContainerBuildOptions.Default));
            var service = hostInfo.CreateService(typeof(ContainerTestService), "test");
            CheckResponse(service.AtLocalHost.At("scope").Get(new Result<DreamMessage>()).Wait());
            var serviceScope1 = ContainerTestService.Scoped;
            Assert.IsNotNull(serviceScope1);
            CheckResponse(service.AtLocalHost.At("scope").Get(new Result<DreamMessage>()).Wait());
            var serviceScope2 = ContainerTestService.Scoped;
            Assert.IsNotNull(serviceScope2);
            Assert.AreSame(serviceScope1, serviceScope2);
        }

        [Test]
        public void Can_set_service_scope_registration_on_provided_container2() {
            var builder = new ContainerBuilder();
            builder.RegisterType<Foo>().As<IFoo>().InScope(DreamContainerScope.Service);
            var hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config"), builder.Build(ContainerBuildOptions.Default));
            var service1 = hostInfo.CreateService(typeof(ContainerTestService), "test");
            var service2 = hostInfo.CreateService(typeof(ContainerTestService), "test");
            CheckResponse(service1.AtLocalHost.At("scope").Get(new Result<DreamMessage>()).Wait());
            var serviceScope1 = ContainerTestService.Scoped;
            Assert.IsNotNull(serviceScope1);
            CheckResponse(service2.AtLocalHost.At("scope").Get(new Result<DreamMessage>()).Wait());
            var serviceScope2 = ContainerTestService.Scoped;
            Assert.IsNotNull(serviceScope2);
            Assert.AreNotSame(serviceScope1, serviceScope2);
        }

        [Test]
        public void Can_set_host_scope_registration_on_provided_container() {
            var builder = new ContainerBuilder();
            builder.RegisterType<Foo>().As<IFoo>().InScope(DreamContainerScope.Host);
            var hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config"), builder.Build(ContainerBuildOptions.Default));
            var service1 = hostInfo.CreateService(typeof(ContainerTestService), "test");
            var service2 = hostInfo.CreateService(typeof(ContainerTestService), "test");
            CheckResponse(service1.AtLocalHost.At("scope").Get(new Result<DreamMessage>()).Wait());
            var hostScope1 = ContainerTestService.Scoped;
            Assert.IsNotNull(hostScope1);
            CheckResponse(service2.AtLocalHost.At("scope").Get(new Result<DreamMessage>()).Wait());
            var hostScope2 = ContainerTestService.Scoped;
            Assert.IsNotNull(hostScope2);
            Assert.AreSame(hostScope1, hostScope2);
        }

        [Test]
        public void Can_register_at_host_level() {
            var hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config")
                .Start("components")
                    .Start("component")
                        .Attr("implementation", typeof(Foo).AssemblyQualifiedName)
                        .Attr("type", typeof(IFoo).AssemblyQualifiedName)
                    .End()
                .End());
            var service = DreamTestHelper.CreateService(hostInfo, typeof(ContainerTestService), "test");
            CheckResponse(service.AtLocalHost.At("registerfoo").Get(new Result<DreamMessage>()).Wait());
        }

        [Test]
        public void Can_register_at_service_level() {
            var hostInfo = DreamTestHelper.CreateRandomPortHost();
            var service = DreamTestHelper.CreateService(hostInfo, typeof(ContainerTestService), "test", new XDoc("config")
                .Start("components")
                    .Start("component")
                        .Attr("implementation", typeof(Foo).AssemblyQualifiedName)
                        .Attr("type", typeof(IFoo).AssemblyQualifiedName)
                    .End()
                .End());
            CheckResponse(service.AtLocalHost.At("registerfoo").Get(new Result<DreamMessage>()).Wait());
        }


        [Test]
        public void Can_register_at_request_level() {
            var hostInfo = DreamTestHelper.CreateRandomPortHost();
            var service = DreamTestHelper.CreateService(hostInfo, typeof(ContainerTestService), "test", new XDoc("config")
                .Start("components")
                    .Start("component")
                        .Attr("scope", "request")
                        .Attr("implementation", typeof(Foo).AssemblyQualifiedName)
                        .Attr("type", typeof(IFoo).AssemblyQualifiedName)
                    .End()
                .End());
            CheckResponse(service.AtLocalHost.At("registerfoo").Get(new Result<DreamMessage>()).Wait());
        }

        [Test]
        public void Request_level_container_scoped_instances_are_disposed_at_end_of_request() {
            var hostInfo = DreamTestHelper.CreateRandomPortHost();
            var service = DreamTestHelper.CreateService(hostInfo, typeof(ContainerTestService), "test", new XDoc("config")
                .Start("components")
                    .Start("component")
                        .Attr("scope", "request")
                        .Attr("implementation", typeof(LifetimeTest).AssemblyQualifiedName)
                        .Attr("type", typeof(ILifetimeTest).AssemblyQualifiedName)
                    .End()
                .End());
            CheckResponse(service.AtLocalHost.At("lifetime").Get(new Result<DreamMessage>()).Wait());
            Assert.IsNotNull(ContainerTestService.LifetimeTest);
            Assert.IsTrue(ContainerTestService.LifetimeTest.IsDisposed);
        }

        [Test]
        public void Service_level_container_scoped_instances_are_not_disposed_at_end_of_request() {
            var hostInfo = DreamTestHelper.CreateRandomPortHost();
            var service = DreamTestHelper.CreateService(hostInfo, typeof(ContainerTestService), "test", new XDoc("config")
                .Start("components")
                    .Start("component")
                        .Attr("scope", "service")
                        .Attr("implementation", typeof(LifetimeTest).AssemblyQualifiedName)
                        .Attr("type", typeof(ILifetimeTest).AssemblyQualifiedName)
                    .End()
                .End());
            CheckResponse(service.AtLocalHost.At("lifetime").Get(new Result<DreamMessage>()).Wait());
            Assert.IsNotNull(ContainerTestService.LifetimeTest);
            Assert.IsFalse(ContainerTestService.LifetimeTest.IsDisposed);
        }

        [Test]
        public void Service_level_container_scoped_instances_are_disposed_at_end_of_service_life() {
            var hostInfo = DreamTestHelper.CreateRandomPortHost();
            var service = DreamTestHelper.CreateService(hostInfo, typeof(ContainerTestService), "test", new XDoc("config")
                .Start("components")
                    .Start("component")
                        .Attr("scope", "service")
                        .Attr("implementation", typeof(LifetimeTest).AssemblyQualifiedName)
                        .Attr("type", typeof(ILifetimeTest).AssemblyQualifiedName)
                    .End()
                .End());
            CheckResponse(service.AtLocalHost.At("lifetime").Get(new Result<DreamMessage>()).Wait());
            CheckResponse(service.WithPrivateKey().AtLocalHost.Delete(new Result<DreamMessage>()).Wait());
            Assert.IsNotNull(ContainerTestService.LifetimeTest);
            Assert.IsTrue(ContainerTestService.LifetimeTest.IsDisposed);
        }

        [Test]
        public void Can_shadow_host_registration() {
            var hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config")
                .Start("components")
                    .Start("component")
                        .Attr("implementation", typeof(Foo).AssemblyQualifiedName)
                        .Attr("type", typeof(IFoo).AssemblyQualifiedName)
                    .End()
                .End());
            var service = DreamTestHelper.CreateService(hostInfo, typeof(ContainerTestService), "test", new XDoc("config")
                .Start("components")
                    .Start("component")
                        .Attr("scope", "service")
                        .Attr("implementation", typeof(Fu).AssemblyQualifiedName)
                        .Attr("type", typeof(IFoo).AssemblyQualifiedName)
                    .End()
                .End());
            CheckResponse(service.AtLocalHost.At("shadow").Get(new Result<DreamMessage>()).Wait());
            Assert.IsNotNull(ContainerTestService.Shadowed);
            Assert.AreEqual(typeof(Fu), ContainerTestService.Shadowed.GetType());
        }

        [Test]
        public void Can_shadow_service_registration() {
            var hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config")
                .Start("components")
                    .Start("component")
                        .Attr("implementation", typeof(Foo).AssemblyQualifiedName)
                        .Attr("type", typeof(IFoo).AssemblyQualifiedName)
                    .End()
                .End());
            var service = DreamTestHelper.CreateService(hostInfo, typeof(ContainerTestService), "test", new XDoc("config")
                .Start("components")
                    .Start("component")
                        .Attr("scope", "service")
                        .Attr("implementation", typeof(Fu).AssemblyQualifiedName)
                        .Attr("type", typeof(IFoo).AssemblyQualifiedName)
                    .End()
                    .Start("component")
                        .Attr("scope", "request")
                        .Attr("implementation", typeof(Faux).AssemblyQualifiedName)
                        .Attr("type", typeof(IFoo).AssemblyQualifiedName)
                    .End()
                .End());
            CheckResponse(service.AtLocalHost.At("shadow").Get(new Result<DreamMessage>()).Wait());
            Assert.IsNotNull(ContainerTestService.Shadowed);
            Assert.AreEqual(typeof(Faux), ContainerTestService.Shadowed.GetType());
        }

        [Test]
        public void ServiceScoped_registrations_create_different_instances_for_different_service_instances() {
            var hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config")
                .Start("components")
                    .Start("component")
                        .Attr("scope", "service")
                        .Attr("implementation", typeof(LifetimeTest).AssemblyQualifiedName)
                        .Attr("type", typeof(ILifetimeTest).AssemblyQualifiedName)
                    .End()
                .End());
            var service1 = DreamTestHelper.CreateService(
                hostInfo,
                typeof(ContainerTestService),
                "test1",
                new XDoc("config").Elem("servicescopetest", true));
            CheckResponse(service1.AtLocalHost.At("servicescope").Get(new Result<DreamMessage>()).Wait());
            var service2 = DreamTestHelper.CreateService(
                hostInfo,
                typeof(ContainerTestService),
                "test2",
                new XDoc("config").Elem("servicescopetest", true));
            CheckResponse(service2.AtLocalHost.At("servicescope").Get(new Result<DreamMessage>()).Wait());
            var serviceScoped1 = ContainerTestService.ServiceScope[service1.AtLocalHost.Uri.Path];
            var serviceScoped2 = ContainerTestService.ServiceScope[service2.AtLocalHost.Uri.Path];
            Assert.IsNotNull(serviceScoped1);
            Assert.IsNotNull(serviceScoped2);
            Assert.AreNotSame(serviceScoped1,serviceScoped2);
            Assert.IsFalse(serviceScoped1.IsDisposed);
            Assert.IsFalse(serviceScoped2.IsDisposed);
        }

        [Test]
        public void RequestContainer_can_inject_current_dream_context_into_instances() {
            var hostInfo = DreamTestHelper.CreateRandomPortHost();
            var service = DreamTestHelper.CreateService(hostInfo, typeof(ContainerTestService), "test", new XDoc("config")
                .Start("components")
                    .Start("component")
                        .Attr("scope", "request")
                        .Attr("implementation", typeof(CanIHazDreamContextPleeze).AssemblyQualifiedName)
                        .Attr("type", typeof(ICanHazDreamContext).AssemblyQualifiedName)
                    .End()
                .End());
            CheckResponse(service.AtLocalHost.At("contextinjection").Get(new Result<DreamMessage>()).Wait());
        }

        private void CheckResponse(DreamMessage message) {
            if(message.IsSuccessful) {
                return;
            }
            Assert.Fail(message.ToDocument()["message"].AsText);
        }
    }

    [DreamService("ContainerTestService", "Copyright (c) 2010 MindTouch, Inc.", SID = new string[] { "sid://mindtouch.com/ContainerTestService" })]
    public class ContainerTestService : DreamService {

        public static LifetimeTest LifetimeTest;
        public static Dictionary<string, ILifetimeTest> ServiceScope = new Dictionary<string, ILifetimeTest>();
        public static IFoo Shadowed;
        public static IFoo Scoped;

        [DreamFeature("GET:registerfoo", "test")]
        public Yield RegisterFoo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            IFoo foo;
            if(!context.Container.TryResolve(out foo)) {
                throw new DreamInternalErrorException("foo didn't resolve");
            }
            if(typeof(Foo) != foo.GetType()) {
                throw new DreamInternalErrorException("foo wasn't proper class");
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:lifetime", "test")]
        public Yield Lifetime(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            LifetimeTest = context.Container.Resolve<ILifetimeTest>() as LifetimeTest;
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:shadow", "test")]
        public Yield Shadow(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            Shadowed = context.Container.Resolve<IFoo>();
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:scope", "test")]
        public Yield Scope(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            Scoped = context.Container.Resolve<IFoo>();
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:contextinjection", "test")]
        public Yield ContextInjection(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var icanhazdreamcontext = context.Container.Resolve<ICanHazDreamContext>();
            Assert.AreSame(context, icanhazdreamcontext.Context);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:servicescope", "test")]
        public Yield ContainerScope(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            ServiceScope[Self.Uri.Path] = context.Container.Resolve<ILifetimeTest>();
            response.Return(DreamMessage.Ok());
            yield break;
        }

        protected override Yield Start(XDoc config, ILifetimeScope container, Result result) {
            yield return Coroutine.Invoke(base.Start, config, container, new Result());
            LifetimeTest = null;
            Shadowed = null;
            result.Return();
        }
    }
}

namespace MindTouch.Dream.Test.ContainerTestClasses {
    public interface IFoo { }
    public class Foo : IFoo { }
    public class Fu : IFoo { }
    public class Faux : IFoo { }
    public interface IBar { }
    public class Bar : IBar { }
    public interface IBaz { }
    public class Baz : IBaz { }

    public interface ILifetimeTest : IDisposable {
        bool IsDisposed { get; }
    }

    public class LifetimeTest : ILifetimeTest {
        public bool IsDisposed { get; set; }
        public void Dispose() {
            IsDisposed = true;
        }
    }

    public interface ICanHazDreamContext {
        DreamContext Context { get; }
    }

    public class CanIHazDreamContextPleeze : ICanHazDreamContext {
        public CanIHazDreamContextPleeze(DreamContext context) {
            Context = context;
        }
        public DreamContext Context { get; private set; }
    }
}
