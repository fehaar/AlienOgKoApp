using Xunit;
namespace Gosuman.TBF.Test.Entities
{
    public class UsingTheDeckCreator : xSpec
    {
        [Fact]
        public void TheDeckWillHoldTheCorrectCards()
        {
            var creator = TestHelper.Dummy<TestDeckCreator>();
            Given("a card creator with different cards", () =>
            {
                creator = new TestDeckCreator()
                {
                    Blueprints = new[]
                    {
                        new CardCount<TestBlueprint, TestEntity> { Entity = new TestBlueprint() { Name = "First" }, Count = 1 },
                        new CardCount<TestBlueprint, TestEntity> { Entity = new TestBlueprint() { Name = "Second" }, Count = 2 },
                        new CardCount<TestBlueprint, TestEntity> { Entity = new TestBlueprint() { Name = "Third" }, Count = 3 },
                    },
                    OwnerId = "Test"
                };
            });
            var deck = TestHelper.Dummy<TestDeck>();
            Where("we create a deck from the creator", () =>
            {
                deck = creator.CreateDeck(new Random());
            });
            Expect("the deck to have the correct cards", () =>
            {
                deck.DrawPile.Should().HaveCount(6);
                deck.DrawPile.Where(c => c.Name == "First").Should().HaveCount(1);
                deck.DrawPile.Where(c => c.Name == "Second").Should().HaveCount(2);
                deck.DrawPile.Where(c => c.Name == "Third").Should().HaveCount(3);
            });
            Expect("the deck to have the same owner ad the creator", () =>
            {
                deck.OwnerId.Should().Be(creator.OwnerId);
            });
        }

        [Fact]
        public void FixingReferencesInTheDeckCreator()
        {
            var creator = TestHelper.Dummy<TestDeckCreator>();
            var realBlueprint1 = TestHelper.Dummy<TestBlueprint>();
            var realBlueprint2 = TestHelper.Dummy<TestBlueprint>();
            Given("a deck creator where the references are dummies", () =>
            {
                realBlueprint1 = new TestBlueprint { Id = "Blueprint1" };
                realBlueprint2 = new TestBlueprint { Id = "Blueprint2" };
                creator = new TestDeckCreator()
                {
                    Blueprints = new[]
                    {
                        new CardCount<TestBlueprint, TestEntity> { Entity = new TestBlueprint() { Id = "Blueprint1" }, Count = 1 },
                        new CardCount<TestBlueprint, TestEntity> { Entity = new TestBlueprint() { Id = "Blueprint2" }, Count = 2 },
                    },
                    OwnerId = "Test"
                };
            });
            Where("we fix references in the creator", () =>
            {
                creator.FixReferences((id) => (id == "Blueprint1") ? realBlueprint1 : realBlueprint2);
            });
            Expect("that the references have been fixed", () =>
            {
                creator.Blueprints.Should().Contain(e => e.Entity == realBlueprint1);
                creator.Blueprints.Should().Contain(e => e.Entity == realBlueprint2);
            });
        }
    }
}
