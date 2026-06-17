using Newtonsoft.Json;

namespace Gosuman.EntitySystem.Test
{
    public static class TestHelper
    {
        public static EntityDatabase DummyDatabase = new EntityDatabase();
        internal static T Dummy<T>() where T : Entity, new()
        {
            return new T();
        }
    }

    public class TestEntity : Entity
    {
        public string Field = string.Empty;
        public string Property { get; set; } = string.Empty;
    }

    public class TestClientEntity : ClientEntity
    {
        public string Field = string.Empty;
        public string Property { get; set; } = string.Empty;
    }

    public class TestEntityWithReference : Entity
    {
        public TestEntity? Field;
        public TestEntity? Property { get; set; }
    }

    public class TestEntityWithEnumerableReference : Entity
    {
        private readonly List<TestEntity> _entities = new List<TestEntity>();
        public IEnumerable<TestEntity> Entities
        {
            get
            {
                return _entities;
            }
            set
            {
                _entities.Clear();
                _entities.AddRange(value);
            }
        }

        public List<TestEntity> EntitiesField = new List<TestEntity>();
    }

    public class TestEntityWithIgnoredData : Entity
    {
        [JsonIgnore]
        public string? Field;
        [JsonIgnore]
        public string Property { get; set; } = string.Empty;
    }

    public struct HiddenReference
    {
        public TestEntity? Entity { get; set; }
    }

    public class TestEntityWithHiddenReferenceAndNoInterface : Entity
    {
        public HiddenReference Reference { get; set; }
    }

    public class TestEntityWithHiddenReference : Entity, IReferenceFixable
    {
        public HiddenReference Reference { get; set; }

        public bool InterfaceCalled { get; private set; }

        public void FixReferences(Func<string, Entity?> getEntityById)
        {
            InterfaceCalled = true;
            if (Reference.Entity != null)
            {
                Reference = new HiddenReference() { Entity = getEntityById(Reference.Entity?.Id ?? string.Empty) as TestEntity };
            }
        }
    }

    public class TestComponent : IComponent
    {
        public string Field { get; set; } = string.Empty;
    }

    public class TestComponentWithReference : IComponent
    {   
        public TestEntity? Entity { get; set; }
    }

    public class TestComponentWithEnumerableReferences : IComponent
    {
        private readonly List<TestEntity> _entities = new List<TestEntity>();
        public IEnumerable<TestEntity> Entities
        {
            get
            {
                return _entities;
            }
            set
            {
                _entities.Clear();
                _entities.AddRange(value);
            }
        }
    }

    public class TestComponentWithHiddenReferenceAndNoInterface : IComponent
    {
        public HiddenReference Reference { get; set; }
    }

    public class TestComponentWithHiddenReference : IComponent, IReferenceFixable
    {
        public HiddenReference Reference { get; set; }
        public bool InterfaceCalled { get; private set; }
        public void FixReferences(Func<string, Entity?> getEntityById)
        {
            InterfaceCalled = true;
            if (Reference.Entity != null)
            {
                Reference = new HiddenReference() { Entity = getEntityById(Reference.Entity?.Id ?? string.Empty) as TestEntity };
            }
        }
    }
}
