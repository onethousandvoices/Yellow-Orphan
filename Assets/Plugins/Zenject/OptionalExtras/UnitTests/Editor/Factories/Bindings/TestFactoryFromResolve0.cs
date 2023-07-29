using NUnit.Framework;
using Assert = ModestTree.Assert;

namespace Zenject.Tests.Bindings
{
    [TestFixture]
    public class TestFactoryFromResolve0 : ZenjectUnitTestFixture
    {
        [Test]
        public void TestSelf()
        {
            Foo foo = new Foo();

            Container.BindInstance(foo).NonLazy();

            Container.BindFactory<Foo, Foo.Factory>().FromResolve().NonLazy();

            Assert.IsEqual(Container.Resolve<Foo.Factory>().Create(), foo);
        }

        [Test]
        public void TestConcrete()
        {
            Foo foo = new Foo();

            Container.BindInstance(foo).NonLazy();

            Container.BindFactory<IFoo, IFooFactory>().To<Foo>().FromResolve().NonLazy();

            Assert.IsEqual(Container.Resolve<IFooFactory>().Create(), foo);
        }

        [Test]
        public void TestSelfIdentifier()
        {
            Foo foo = new Foo();

            Container.BindInstance(foo).WithId("foo").NonLazy();

            Container.BindFactory<Foo, Foo.Factory>().FromResolve("foo").NonLazy();

            Assert.IsEqual(Container.Resolve<Foo.Factory>().Create(), foo);
        }

        [Test]
        public void TestConcreteIdentifier()
        {
            Foo foo = new Foo();

            Container.BindInstance(foo).WithId("foo").NonLazy();

            Container.BindFactory<IFoo, IFooFactory>().To<Foo>().FromResolve("foo").NonLazy();

            Assert.IsEqual(Container.Resolve<IFooFactory>().Create(), foo);
        }

        interface IFoo
        {
        }

        class IFooFactory : PlaceholderFactory<IFoo>
        {
        }

        class Foo : IFoo
        {
            public class Factory : PlaceholderFactory<Foo>
            {
            }
        }
    }
}
