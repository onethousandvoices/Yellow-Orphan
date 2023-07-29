using NUnit.Framework;
using Assert = ModestTree.Assert;

namespace Zenject.Tests.Bindings
{
    [TestFixture]
    public class TestFromIFactory2 : ZenjectUnitTestFixture
    {
        [Test]
        public void Test1()
        {
            Container.BindFactory<int, Foo, Foo.Factory>().WithId("foo1")
                .FromIFactory(x => x.To<FooFactory>().AsCached().WithArguments("asdf"));

            Container.BindFactory<int, Foo, Foo.Factory>().WithId("foo2")
                .FromIFactory(x => x.To<FooFactory>().AsCached().WithArguments("zxcv"));

            Foo.Factory factory1 = Container.ResolveId<Foo.Factory>("foo1");
            Foo.Factory factory2 = Container.ResolveId<Foo.Factory>("foo2");

            Foo foo1 = factory1.Create(5);
            Foo foo2 = factory2.Create(2);

            Assert.IsEqual(foo1.Value, "asdf");
            Assert.IsEqual(foo1.Value2, 5);

            Assert.IsEqual(foo2.Value, "zxcv");
            Assert.IsEqual(foo2.Value2, 2);
        }

        public class Foo
        {
            public Foo(string value, int value2)
            {
                Value = value;
                Value2 = value2;
            }

            public int Value2
            {
                get; private set;
            }

            public string Value
            {
                get; private set;
            }

            public class Factory : PlaceholderFactory<int, Foo>
            {
            }
        }

        public class FooFactory : IFactory<int, Foo>
        {
            readonly string _value;
            readonly DiContainer _container;

            public FooFactory(
                DiContainer container,
                string value)
            {
                _value = value;
                _container = container;
            }

            public Foo Create(int value)
            {
                return _container.Instantiate<Foo>(new object [] { value, _value });
            }
        }
    }
}

