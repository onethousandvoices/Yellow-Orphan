using System;
using System.Collections.Generic;
using NUnit.Framework;
using Assert = ModestTree.Assert;

namespace Zenject.Tests
{
    [TestFixture]
    public class TestStaticMemoryPool : ZenjectUnitTestFixture
    {
        [SetUp]
        public void CommonInstall()
        {
            Container.Inject(this);
        }

        [Test]
        public void RunTest()
        {
            StaticMemoryPool<string, Foo> pool = Foo.Pool;

            pool.Clear();
            pool.ClearActiveCount();

            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumInactive, 0);
            Assert.IsEqual(pool.NumTotal, 0);

            Foo foo = pool.Spawn("asdf");

            Assert.IsEqual(pool.NumActive, 1);
            Assert.IsEqual(pool.NumInactive, 0);
            Assert.IsEqual(pool.NumTotal, 1);

            Assert.IsEqual(foo.Value, "asdf");
            pool.Despawn(foo);
            Assert.IsNull(foo.Value);

            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumInactive, 1);
            Assert.IsEqual(pool.NumTotal, 1);

            Foo foo2 = pool.Spawn("zxcv");
            Assert.That(ReferenceEquals(foo, foo2));
            Assert.IsEqual(foo2.Value, "zxcv");

            Assert.IsEqual(pool.NumActive, 1);
            Assert.IsEqual(pool.NumInactive, 0);
            Assert.IsEqual(pool.NumTotal, 1);

            Foo foo3 = pool.Spawn("bar");
            Assert.That(!ReferenceEquals(foo2, foo3));

            Assert.IsEqual(pool.NumActive, 2);
            Assert.IsEqual(pool.NumInactive, 0);
            Assert.IsEqual(pool.NumTotal, 2);

            pool.Despawn(foo3);
            pool.Despawn(foo2);

            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumInactive, 2);
            Assert.IsEqual(pool.NumTotal, 2);

            Assert.Throws(() => pool.Despawn(foo3));
        }

        [Test]
        public void TestListPool()
        {
            ListPool<string> pool = ListPool<string>.Instance;

            pool.Clear();
            pool.ClearActiveCount();

            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumInactive, 0);
            Assert.IsEqual(pool.NumTotal, 0);

            List<string> list = pool.Spawn();

            list.Add("asdf");
            list.Add("zbx");

            Assert.IsEqual(pool.NumActive, 1);
            Assert.IsEqual(pool.NumInactive, 0);
            Assert.IsEqual(pool.NumTotal, 1);

            pool.Despawn(list);

            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumInactive, 1);
            Assert.IsEqual(pool.NumTotal, 1);

            List<string> list2 = pool.Spawn();

            Assert.IsEqual(list2.Count, 0);
            Assert.IsEqual(list2, list);

            Assert.IsEqual(pool.NumActive, 1);
            Assert.IsEqual(pool.NumInactive, 0);
            Assert.IsEqual(pool.NumTotal, 1);

            List<string> list3 = pool.Spawn();

            Assert.IsNotEqual(list2, list3);

            Assert.IsEqual(pool.NumActive, 2);
            Assert.IsEqual(pool.NumInactive, 0);
            Assert.IsEqual(pool.NumTotal, 2);

            pool.Despawn(list3);
            pool.Despawn(list2);

            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumInactive, 2);
            Assert.IsEqual(pool.NumTotal, 2);

            Assert.Throws(() => pool.Despawn(list3));
        }

        [Test]
        public void TestPoolWrapper()
        {
            StaticMemoryPool<string, Foo> pool = Foo.Pool;

            pool.Clear();
            pool.ClearActiveCount();

            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumInactive, 0);
            Assert.IsEqual(pool.NumTotal, 0);

            using (DisposeBlock block = DisposeBlock.Spawn())
            {
                block.Spawn(pool, "asdf");

                Assert.IsEqual(pool.NumActive, 1);
                Assert.IsEqual(pool.NumInactive, 0);
                Assert.IsEqual(pool.NumTotal, 1);
            }

            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumInactive, 1);
            Assert.IsEqual(pool.NumTotal, 1);
        }

        [Test]
        public void TestResize()
        {
            StaticMemoryPool<Bar> pool = Bar.Pool;

            pool.Clear();
            pool.ClearActiveCount();

            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumTotal, 0);
            Assert.IsEqual(pool.NumInactive, 0);

            pool.Resize(2);

            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumTotal, 2);
            Assert.IsEqual(pool.NumInactive, 2);

            List<Bar> bars = new List<Bar>();

            bars.Add(pool.Spawn());
            bars.Add(pool.Spawn());
            bars.Add(pool.Spawn());
            bars.Add(pool.Spawn());
            bars.Add(pool.Spawn());

            Assert.IsEqual(pool.NumActive, 5);
            Assert.IsEqual(pool.NumTotal, 5);
            Assert.IsEqual(pool.NumInactive, 0);

            pool.Despawn(bars[0]);
            pool.Despawn(bars[1]);
            pool.Despawn(bars[2]);

            Assert.IsEqual(pool.NumActive, 2);
            Assert.IsEqual(pool.NumTotal, 5);
            Assert.IsEqual(pool.NumInactive, 3);

            pool.ShrinkBy(1);

            Assert.IsEqual(pool.NumActive, 2);
            Assert.IsEqual(pool.NumTotal, 4);
            Assert.IsEqual(pool.NumInactive, 2);

            pool.ExpandBy(1);

            Assert.IsEqual(pool.NumActive, 2);
            Assert.IsEqual(pool.NumTotal, 5);
            Assert.IsEqual(pool.NumInactive, 3);

            pool.Resize(1);

            Assert.IsEqual(pool.NumActive, 2);
            Assert.IsEqual(pool.NumTotal, 3);
            Assert.IsEqual(pool.NumInactive, 1);

            pool.Clear();

            Assert.IsEqual(pool.NumActive, 2);
            Assert.IsEqual(pool.NumTotal, 2);
            Assert.IsEqual(pool.NumInactive, 0);

            Assert.Throws(() => pool.Resize(-1));
            Assert.Throws(() => pool.ShrinkBy(1));
        }

        public class Bar
        {
            public static readonly StaticMemoryPool<Bar> Pool =
                new StaticMemoryPool<Bar>(OnSpawned, OnDespawned);

            static void OnSpawned(Bar that)
            {
            }

            static void OnDespawned(Bar that)
            {
            }
        }

        public class Foo : IDisposable
        {
            public static readonly StaticMemoryPool<string, Foo> Pool =
                new StaticMemoryPool<string, Foo>(OnSpawned, OnDespawned);

            public string Value
            {
                get; private set;
            }

            public void Dispose()
            {
                Pool.Despawn(this);
            }

            static void OnSpawned(string value, Foo that)
            {
                that.Value = value;
            }

            static void OnDespawned(Foo that)
            {
                that.Value = null;
            }
        }
    }
}
