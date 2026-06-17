using Gosuman.EntitySystem;
using Gosuman.TBF.Entities;

namespace Gosuman.TBF.Test
{    
    internal class TestEntity : Entity { 
        public string Name { get; set; } = string.Empty;
    }

    internal class TestEntityWithReference : Entity
    {
        public TestEntity? Ref { get; set; }
    }

    internal class TestBlueprint : Blueprint<TestEntity>
    {
        public override TestEntity Create(Random rnd)
        {
            return new TestEntity() { Name = Name };
        }
    }

    internal class TestWeightedDeck : WeightedDeck<TestBlueprint, TestEntity>
    {        
    }

    internal class TestDeck : Deck<TestEntity>
    {
    }

    internal class  TestDeckCreator : DeckCreator<TestBlueprint, TestEntity, TestDeck>
    {        
    }
}
