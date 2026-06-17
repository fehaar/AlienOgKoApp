using Gosuman.EntitySystem;
using Gosuman.EntitySystem.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gosuman.TBF.Entities
{
    public abstract class WeightedDeck<T, T1> : ServerEntity, IReferenceFixable where T : Blueprint<T1> where T1 : Entity
    {
        public WeightedDeck()
        {
        }

        private readonly List<WeightedEntry<T>> entries = new List<WeightedEntry<T>>();
        public IEnumerable<WeightedEntry<T>> Entries
        {
            get
            {
                return entries;
            }
            set
            {
                entries.Clear();
                entries.AddRange(value);
            }
        }

        public Range CardsToPull { get; set; }

        public IEnumerable<T1> PullCards(Random rnd, bool secret = true)
        {
            var totalProbability = Entries.Sum(e => e.Weight);
            var cardCount = CardsToPull.RandomInRange(rnd);
            while (cardCount-- > 0)
            {
                var random = rnd.NextDouble() * totalProbability;
                var entity = entries.First(e => (random -= e.Weight) <= 0).Entity!.Create(rnd);
                if (entity != null)
                {
                    yield return entity;
                }
                else
                {
                    throw new Exception("There is something wrong with this function");
                }
            }
        }

        public void FixReferences(Func<string, Entity?> getEntityById)
        {
            foreach (var entry in Entries)
            {
                if (entry.Entity != null)
                {
                    var newEntity = getEntityById(entry.Entity.Id);
                    if (newEntity != null)
                    {
                        entry.Entity = newEntity as T;
                    }
                }
            }
        }
    }
}
