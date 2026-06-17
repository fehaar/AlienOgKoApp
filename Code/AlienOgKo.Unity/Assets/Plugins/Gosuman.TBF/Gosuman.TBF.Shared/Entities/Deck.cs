using Gosuman.EntitySystem;
using System.Collections.Generic;
using System.Linq;

namespace Gosuman.TBF.Shared.Entities
{
    /// <summary>
    /// This is a deck of cards where the cards are either in the draw pile, in hand or in the discard pile.
    /// The deck is never shuffled so when the draw pile is empty and you need to refresh it, the discard pile is turned and now becomes the draw pile.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Deck<T> : Entity where T : Entity
    {
        public Deck()
        {
        }

        public Deck(IEnumerable<T> list)
        {
            DrawPile = list;
        }

        public string OwnerId { get; set; } = string.Empty;

        protected readonly List<T> drawPile = new List<T>();
        public IEnumerable<T> DrawPile
        {
            get
            {
                return drawPile;
            }
            set
            {
                drawPile.Clear();
                drawPile.AddRange(value);
            }
        }
        public int DrawPileCount => drawPile.Count;

        protected readonly List<T> hand = new List<T>();
        public IEnumerable<T> Hand
        {
            get
            {
                return hand;
            }
            set
            {
                hand.Clear();
                hand.AddRange(value);
            }
        }
        public int HandCount => hand.Count;

        protected readonly List<T> discardPile = new List<T>();
        public IEnumerable<T> DiscardPile
        {
            get
            {
                return discardPile;
            }
            set
            {
                discardPile.Clear();
                discardPile.AddRange(value);
            }
        }
        public int DiscardPileCount => discardPile.Count;

        /// <summary>
        /// How many cards can you hold in your hand
        /// </summary>
        public int HandSize { get; set; }

        /// <summary>
        /// Draw cards from the draw pile to your hand until your hand is full.
        /// Will follow normal rules for drawing cards.
        /// </summary>
        /// <param name="random">If set, the discard pile will be shuffled before added to the draw pile.</param>
        public void FillHand(System.Random? random = null)
        {
            var missingCards = HandSize - HandCount;
            for (int i = 0; i < missingCards; i++)
            {
                DrawCard(random);
            }
        }

        /// <summary>
        /// Draw a specific number of cards from the draw pile to your hand.
        /// Will follow normal rules for drawing cards.
        /// </summary>
        /// <param name="random">If set, the discard pile will be shuffled before added to the draw pile.</param>
        /// <param name="count">The number of cards to draw</param>
        public void DrawCards(int count, System.Random? random = null)
        {
            for (int i = 0; i < count; i++)
            {
                DrawCard(random);
            }
        }

        /// <summary>
        /// Draw a card from the draw pile to your hand.
        /// If the draw pile is empty it will be refilled from the discard pile.
        /// If the optional random parameter is set, the discard pile will get shuffled when refilled.
        /// </summary>
        public void DrawCard(System.Random? random = null)
        {
            if (HandCount == HandSize)
            {
                return;
            }
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                {
                    // We do not have any cards in the discard pile either, so just don't draw a card.
                    return;
                }
                drawPile.AddRange(discardPile);
                discardPile.Clear();
            }
            hand.Add(drawPile.First());
            drawPile.RemoveAt(0);
        }

        /// <summary>
        /// Discard a card from your hand to the discard pile
        /// </summary>
        /// <param name="card"></param>
        public void Discard(T card)
        {
            if (hand.Remove(card) || drawPile.Remove(card))
            {
                discardPile.Add(card);
            }
        }

        /// <summary>
        /// Add a card to the bottom of the draw pile.
        /// </summary>
        /// <param name="card">The card to add</param>
        protected void AddCard(T card)
        {
            drawPile.Add(card);
        }
    }
}