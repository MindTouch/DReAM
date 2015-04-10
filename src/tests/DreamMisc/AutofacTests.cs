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

using Autofac;
using Autofac.Builder;
using Autofac.Core;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class AutofacTests {

        [Test]
        public void Last_registration_wins() {
            var hostScope = new ContainerBuilder().Build(ContainerBuildOptions.None).BeginLifetimeScope(DreamContainerScope.Host);
            var serviceScope = hostScope.BeginLifetimeScope(DreamContainerScope.Service, b => {
                b.RegisterType<Foo>().As<IFoo>().ServiceScoped();
                b.RegisterType<Fu>().As<IFoo>().ServiceScoped();
            });
            var foo = serviceScope.Resolve<IFoo>();
            Assert.AreEqual(typeof(Fu), foo.GetType());
        }

        [Test]
        public void Last_registration_wins_with_module() {
            var hostScope = new ContainerBuilder().Build(ContainerBuildOptions.None).BeginLifetimeScope(DreamContainerScope.Host);
            var serviceScope = hostScope.BeginLifetimeScope(DreamContainerScope.Service, b => {
                b.RegisterModule(new FuModule());
                b.RegisterType<Foo>().As<IFoo>().ServiceScoped();
            });
            var foo = serviceScope.Resolve<IFoo>();
            Assert.AreEqual(typeof(Foo), foo.GetType());
        }

        [Test]
        public void Last_module_wins() {
            var hostScope = new ContainerBuilder().Build(ContainerBuildOptions.None).BeginLifetimeScope(DreamContainerScope.Host);
            var serviceScope = hostScope.BeginLifetimeScope(DreamContainerScope.Service, b => {
                b.RegisterModule(new FooModule());
                b.RegisterModule(new FuModule());
            });
            var foo = serviceScope.Resolve<IFoo>();
            Assert.AreEqual(typeof(Fu), foo.GetType());
        }

        [Test]
        public void Can_register_service_level_component_at_service_scope_creation_and_resolve_in_service_scope() {
            var hostScope = new ContainerBuilder().Build(ContainerBuildOptions.None).BeginLifetimeScope(DreamContainerScope.Host);
            var serviceScope = hostScope.BeginLifetimeScope(DreamContainerScope.Service, b => b.RegisterType<Foo>().As<IFoo>().ServiceScoped());
            var foo = serviceScope.Resolve<IFoo>();
            Assert.IsNotNull(foo);
        }

        [Test]
        public void Can_register_request_level_component_at_service_scope_creation_and_resolve_in_request_scope() {
            var hostScope = new ContainerBuilder().Build(ContainerBuildOptions.None).BeginLifetimeScope(DreamContainerScope.Host);
            var serviceScope = hostScope.BeginLifetimeScope(DreamContainerScope.Service, b => b.RegisterType<Foo>().As<IFoo>().RequestScoped());
            var requestScope = serviceScope.BeginLifetimeScope(DreamContainerScope.Request);
            var foo = requestScope.Resolve<IFoo>();
            Assert.IsNotNull(foo);
        }

        [Test]
        public void Cannot_resolve_RequestScoped_component_registered_at_service_scope_creation_in_service_scope() {
            var hostScope = new ContainerBuilder().Build(ContainerBuildOptions.None).BeginLifetimeScope(DreamContainerScope.Host);
            var serviceScope = hostScope.BeginLifetimeScope(DreamContainerScope.Service, b => b.RegisterType<Foo>().As<IFoo>().RequestScoped());
            try {
                var foo = serviceScope.Resolve<IFoo>();
            } catch(DependencyResolutionException) {
                return;
            }
            Assert.Fail("resolved component in wrong scope");
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

        public class Fu : IFoo { }
        public interface IFoo { }
        public interface IBaz { }

        public class FuModule : Module {
            protected override void Load(ContainerBuilder builder) {
                builder.RegisterType<Fu>().As<IFoo>().ServiceScoped();
            }
        }

        public class FooModule : Module {
            protected override void Load(ContainerBuilder builder) {
                builder.RegisterType<Foo>().As<IFoo>().ServiceScoped();
            }
        }
    }
}
