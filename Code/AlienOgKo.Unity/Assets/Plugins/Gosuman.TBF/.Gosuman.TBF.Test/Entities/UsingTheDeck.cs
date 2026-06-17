namespace Gosuman.TBF.Test.Entities
{
    public class UsingTheDeck : xSpec
    {
        [Fact]
        public void DrawingCardsToTheHand()
        {
            var deck = TestHelper.Dummy<TestDeck>();
            Given("a deck of cards", () =>
            {
                deck = new TestDeck()
                {
                    DrawPile = new[]
                    {
                        new TestEntity() { Name = "Third" },
                        new TestEntity() { Name = "First" },
                        new TestEntity() { Name = "Second" },
                        new TestEntity() { Name = "Third" },
                        new TestEntity() { Name = "Second" },
                        new TestEntity() { Name = "Third" },
                    }
                };
            });
            var firstCards = deck.DrawPile.Take(3).ToArray();
            Where("we draw up to our hand size", () =>
            {
                deck.HandSize = 3;
                deck.FillHand();

            });
            Expect("the right cards to be in hand", () =>
            {
                deck.Hand.Should().HaveCount(3);
                deck.Hand.Should().Contain(firstCards);
            });
            Expect("the draw pile to not have the cards anymore", () =>
            {
                deck.DrawPile.Should().NotContain(firstCards);
                deck.DrawPile.Should().HaveCount(3);
            });
        }

        [Fact]
        public void FillTheHand()
        {
            var deck = TestHelper.Dummy<TestDeck>();
            Given("a deck with cards drawn but fewer than the hand size", () =>
            {
                deck = new TestDeck()
                {
                    HandSize = 3,
                    DrawPile = new[]
                    {
                        new TestEntity() { Name = "Second" },
                        new TestEntity() { Name = "Third" },
                        new TestEntity() { Name = "Second" },
                        new TestEntity() { Name = "Third" },
                    },
                    Hand = new[]
                    {
                        new TestEntity() { Name = "Third" },
                        new TestEntity() { Name = "First" },
                    }
                };
            });
            var card = deck.DrawPile.First();
            Where("we fill the hand", () =>
            {
                deck.FillHand();
            });
            Expect("that the hand now has full size from the draw pile", () =>
            {
                deck.Hand.Should().HaveCount(3);
                deck.Hand.Should().Contain(card);
            });
        }

        [Fact]
        public void DiscardingACardFromHand()
        {
            var deck = TestHelper.Dummy<TestDeck>();
            Given("a deck with cards drawn", () =>
            {
                deck = new TestDeck()
                {
                    HandSize = 3,
                    DrawPile = new[]
                    {
                        new TestEntity() { Name = "Third" },
                        new TestEntity() { Name = "Second" },
                        new TestEntity() { Name = "Third" },
                    },
                    Hand = new[]
                    {
                        new TestEntity() { Name = "Third" },
                        new TestEntity() { Name = "First" },
                        new TestEntity() { Name = "Second" },
                    }
                };
                deck.FillHand();
            });
            var card = TestHelper.Dummy<TestEntity>();
            Where("we discard a card from the draw pile", () =>
            {
                card = deck.Hand.First();
                deck.Discard(card);
            });
            Expect("that the card is in the discard pile now", () =>
            {
                deck.DiscardPile.Should().HaveCount(1);
                deck.DiscardPile.Should().Contain(card);
                deck.Hand.Should().HaveCount(2);
                deck.Hand.Should().NotContain(card);
            });
        }

        [Fact]
        public void DiscardingACardFromTheDrawPile()
        {
            var deck = TestHelper.Dummy<TestDeck>();
            Given("a deck with cards drawn", () =>
            {
                deck = new TestDeck()
                {
                    HandSize = 3,
                    DrawPile = new[]
                    {
                        new TestEntity() { Name = "Third" },
                        new TestEntity() { Name = "Second" },
                        new TestEntity() { Name = "Third" },
                    },
                    Hand = new[]
                    {
                        new TestEntity() { Name = "Third" },
                        new TestEntity() { Name = "First" },
                        new TestEntity() { Name = "Second" },
                    }
                };
                deck.FillHand();
            });
            var card = TestHelper.Dummy<TestEntity>();
            Where("we discard a card from the draw pile", () =>
            {
                card = deck.DrawPile.First();
                deck.Discard(card);
            });
            Expect("that the card is in the discard pile now", () =>
            {
                deck.DiscardPile.Should().HaveCount(1);
                deck.DiscardPile.Should().Contain(card);
                deck.DrawPile.Should().HaveCount(2);
                deck.DrawPile.Should().NotContain(card);
            });
        }

        [Fact]
        public void DrawingWhenTheDrawPileIsEmpty()
        {
            var deck = TestHelper.Dummy<TestDeck>();
            Given("a deck with an empty draw pile", () =>
            {
                deck = new TestDeck()
                {
                    HandSize = 3,
                    DiscardPile = new[]
                    {
                        new TestEntity() { Name = "Third" },
                        new TestEntity() { Name = "Second" },
                        new TestEntity() { Name = "Third" },
                        new TestEntity() { Name = "Third" },
                        new TestEntity() { Name = "First" },
                        new TestEntity() { Name = "Second" },
                    },
                };
            });
            var cards = deck.DiscardPile.Take(3).ToArray();
            Where("we fill the hand", () =>
            {
                deck.FillHand();
            });
            Expect("that the discard pile is now empty and the draw pile contains the right cards", () =>
            {
                deck.DiscardPile.Should().HaveCount(0);
                deck.Hand.Should().HaveCount(3);
                deck.Hand.Should().Contain(cards);
                deck.DrawPile.Should().HaveCount(3);
                deck.DrawPile.Should().NotContain(cards);
            });
        }

        [Fact]
        public void DrawingWhenThePileBecomesEmpty()
        {
            var deck = TestHelper.Dummy<TestDeck>();
            Given("a deck with an almost empty draw pile", () =>
            {
                deck = new TestDeck()
                {
                    HandSize = 3,
                    DrawPile = new[]
                    {
                        new TestEntity() { Name = "Third" },
                    },
                    DiscardPile = new[]
                    {
                        new TestEntity() { Name = "Second" },
                        new TestEntity() { Name = "Third" },
                        new TestEntity() { Name = "Third" },
                        new TestEntity() { Name = "First" },
                        new TestEntity() { Name = "Second" },
                    },
                };
            });
            var cardInDrawPile = deck.DrawPile.First();
            var cards = deck.DiscardPile.Take(2).ToArray();
            Where("we fill the hand", () =>
            {
                deck.FillHand();
            });
            Expect("that the discard pile is now empty and the draw pile contains the right cards", () =>
            {
                deck.DiscardPile.Should().HaveCount(0);
                deck.Hand.Should().HaveCount(3);
                deck.Hand.Should().Contain(cards);
                deck.Hand.Should().Contain(cardInDrawPile);
                deck.DrawPile.Should().HaveCount(3);
                deck.DrawPile.Should().NotContain(cards);
                deck.DrawPile.Should().NotContain(cardInDrawPile);
            });
        }
    }
}
