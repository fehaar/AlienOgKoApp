using Xunit;
namespace Gosuman.TBF.Test.Entities
{
    public class UsingTheWeightedDeck : xSpec
    {
        [Fact]
        public void CardsPickedWillHaveAnotherIdThanTheOriginalCardAsEachWillBeUnique()
        {
            var deck = TestHelper.Dummy<TestWeightedDeck>();
            Given("a loot deck with two options", () =>
            {
                deck = new TestWeightedDeck()
                {
                    CardsToPull = 3..3,
                    Entries = new[] {
                       new WeightedEntry<TestBlueprint> { Entity = new TestBlueprint(), Weight = 1 },
                       new WeightedEntry<TestBlueprint> { Entity = new TestBlueprint(), Weight = 1 },
                       new WeightedEntry<TestBlueprint> { Entity = new TestBlueprint(), Weight = 1 },
                    }
                };
            });
            var cards = Array.Empty<TestEntity>();
            Where("we pull cards", () =>
            {
                cards = deck.PullCards(new Random()).ToArray();
            });
            Expect("that all cards picked will have unique Ids that are not in the deck", () =>
            {
                cards.Should().OnlyHaveUniqueItems();
                cards.Should().NotContain(c => deck.Entries.Any(e => e.Entity!.Id == c.Id));
            });
        }

        [Fact]
        public void FixingReferencesInTheWeightedDeck()
        {
            var deck = TestHelper.Dummy<TestWeightedDeck>();
            var realBlueprint1 = TestHelper.Dummy<TestBlueprint>();
            var realBlueprint2 = TestHelper.Dummy<TestBlueprint>();
            Given("a weigthed deck where entity references are dummies", () =>
            {
                realBlueprint1 = new TestBlueprint { Id = "Blueprint1" };
                realBlueprint2 = new TestBlueprint { Id = "Blueprint2" };
                deck = new TestWeightedDeck()
                {
                    CardsToPull = 3..3,
                    Entries = new[] {
                       new WeightedEntry<TestBlueprint> { Entity = new TestBlueprint { Id = "Blueprint1" }, Weight = 1 },
                       new WeightedEntry<TestBlueprint> { Entity = new TestBlueprint { Id = "Blueprint2" }, Weight = 1 },
                    }
                };
            });
            Where("we fix references in the deck", () =>
            {
                deck.FixReferences((id) => (id == "Blueprint1") ? realBlueprint1 : realBlueprint2);
            });
            Expect("that thre references have been fixed", () =>
            {
                deck.Entries.Should().Contain(e => e.Entity == realBlueprint1);
                deck.Entries.Should().Contain(e => e.Entity == realBlueprint2);
            });
        }
    }
}
