using Gosuman.EntitySystem;
using Gosuman.EntitySystem.Serialization;
using Gosuman.TBF.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gosuman.TBF.Entities
{

    /// <summary>
    /// This is a deck of cards that can be pulled from where every entry has a given number of cards so we can't pull more of the same card than the amount.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T1"></typeparam>
    public abstract class DeckCreator<T, T1, T2> : ServerEntity, IReferenceFixable
        where T : Blueprint<T1>
        where T1 : Entity
        where T2 : Deck<T1>, new()
    {
        public DeckCreator()
        {
        }

        public string OwnerId { get; set; } = string.Empty;

        private readonly List<CardCount<T, T1>> blueprints = new List<CardCount<T, T1>>();
        public IEnumerable<CardCount<T, T1>> Blueprints
        {
            get
            {
                return blueprints;
            }
            set
            {
                blueprints.Clear();
                blueprints.AddRange(value);
            }
        }

        /// <summary>
        /// We will pull all the cards from the deck randomly.
        /// </summary>
        /// <param name="rnd">The random distribution used</param>
        /// <param name="secret">Are the resulting cards marked secret</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public T2 CreateDeck(Random rnd, bool secret = true)
        {
            var list = new List<T1>();
            foreach (var bp in blueprints)
            {
                for (int i = 0; i < bp.Count; i++)
                {
                    var card = bp.Entity!.Create(rnd);
                    list.Add(card);
                }
            }
            var deck = new T2() { OwnerId = OwnerId };
            deck.DrawPile = list.OrderBy(_ => rnd.NextDouble());
            return deck;
        }

        public void FixReferences(Func<string, Entity?> getEntityById)
        {
            foreach (var bp in blueprints)
            {
                if (bp.Entity != null)
                {
                    var newEntity = getEntityById(bp.Entity.Id);
                    if (newEntity != null)
                    {
                        bp.Entity = newEntity as T;
                    }
                }
            }
        }
    }
}