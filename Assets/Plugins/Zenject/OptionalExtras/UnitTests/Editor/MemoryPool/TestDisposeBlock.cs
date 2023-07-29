using System;
using NUnit.Framework;
using Assert = ModestTree.Assert;

namespace Zenject.Tests
{
    [TestFixture]
    public class TestDisposeBlock : ZenjectUnitTestFixture
    {
        class Foo : IDisposable
        {
            public static readonly StaticMemoryPool<string, Foo> Pool =
                new StaticMemoryPool<string, Foo>(OnSpawned, OnDespawned);

            public void Dispose()
            {
                Pool.Despawn(this);
            }

            static void OnDespawned(Foo that)
            {
                that.Value = null;
            }

            static void OnSpawned(string value, Foo that)
            {
                that.Value = value;
            }

            public string Value
            {
                get; private set;
            }
        }

        public class Bar : IDisposable
        {
            readonly Pool _pool;

            public Bar(Pool pool)
            {
                _pool = pool;
            }

            public void Dispose()
            {
                _pool.Despawn(this);
            }

            public class Pool : MemoryPool<Bar>
            {
            }
        }

        public class Qux : IDisposable
        {
            public bool WasDisposed
            {
                get; private set;
            }

            public void Dispose()
            {
                WasDisposed = true;
            }
        }

        [Test]
        public void TestExceptions()
        {
            Qux qux1 = new Qux();
            Qux qux2 = new Qux();

            try
            {
                using (DisposeBlock block = DisposeBlock.Spawn())
                {
                    block.Add(qux1);
                    block.Add(qux2);
                    throw new Exception();
                }
            }
            catch
            {
            }

            Assert.That(qux1.WasDisposed);
            Assert.That(qux2.WasDisposed);
        }

        [Test]
        public void TestWithStaticMemoryPool()
        {
            StaticMemoryPool<string, Foo> pool = Foo.Pool;

            pool.Clear();

            Assert.IsEqual(pool.NumTotal, 0);
            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumInactive, 0);

            using (DisposeBlock block = DisposeBlock.Spawn())
            {
                block.Spawn(pool, "asdf");

                Assert.IsEqual(pool.NumTotal, 1);
                Assert.IsEqual(pool.NumActive, 1);
                Assert.IsEqual(pool.NumInactive, 0);
            }

            Assert.IsEqual(pool.NumTotal, 1);
            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumInactive, 1);
        }

        [Test]
        public void TestWithNormalMemoryPool()
        {
            Container.BindMemoryPool<Bar, Bar.Pool>();

            Bar.Pool pool = Container.Resolve<Bar.Pool>();

            Assert.IsEqual(pool.NumTotal, 0);
            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumInactive, 0);

            using (DisposeBlock block = DisposeBlock.Spawn())
            {
                block.Spawn(pool);

                Assert.IsEqual(pool.NumTotal, 1);
                Assert.IsEqual(pool.NumActive, 1);
                Assert.IsEqual(pool.NumInactive, 0);
            }

            Assert.IsEqual(pool.NumTotal, 1);
            Assert.IsEqual(pool.NumActive, 0);
            Assert.IsEqual(pool.NumInactive, 1);
        }
    }
}
