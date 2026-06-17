using Gosuman.TBF.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gosuman.TBF.Shared
{
    public class TestEntity : Entity
    {
        public TestEntity()
        {
            Id = Ulid.NewUlid().ToString();
        }

        public string Field = String.Empty;
        public string Property { get; set; } = String.Empty;
    }

    public class TestEntity1 : Entity
    {
        public TestEntity1()
        {
            Id = Ulid.NewUlid().ToString();
        }

        public string Field = String.Empty;
        public string Property { get; set; } = String.Empty;
    }

    public class TestEntityWithReference : Entity
    {
        public TestEntityWithReference()
        {
            Id = Ulid.NewUlid().ToString();
        }

        public TestEntity? Entity { get; set; }
    }

    public class TestEntityWithEnumerableReference : Entity
    {
        public TestEntityWithEnumerableReference()
        {
            Id = Ulid.NewUlid().ToString();
        }

        public IEnumerable<TestEntity> Entities { get; set; } = Array.Empty<TestEntity>();
    }

    internal class TestDeck : Deck<TestEntity>
    {
    }
}
