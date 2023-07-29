using NUnit.Framework;
using Assert = ModestTree.Assert;

namespace Zenject.Tests.Bindings
{
    [TestFixture]
    public class TestFactoryFromMethod0 : ZenjectUnitTestFixture
    {
        [Test]
        public void TestSelf()
        {
            Foo foo = new Foo();

            Container.BindFactory<Foo, Foo.Factory>().FromMethod(c => foo).NonLazy();

            Assert.IsEqual(Container.Resolve<Foo.Factory>().Create(), foo);
        }

        [Test]
        public void TestConcrete()
        {
            Foo foo = new Foo();

            Container.BindFactory<IFoo, IFooFactory>().FromMethod(c => foo).NonLazy();

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

