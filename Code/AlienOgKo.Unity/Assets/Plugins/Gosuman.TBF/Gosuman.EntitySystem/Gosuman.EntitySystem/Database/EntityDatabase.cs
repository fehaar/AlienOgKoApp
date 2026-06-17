using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Gosuman.EntitySystem.Database
{
    [Serializable]
    public class EntityDatabase : IReadOnlyEntityDatabase
    {
        private readonly Dictionary<string, Entity> database = new Dictionary<string, Entity>();
        private readonly Dictionary<Type, List<Entity>> typeIndex = new Dictionary<Type, List<Entity>>();
        private readonly HashSet<string> deletionTracker = new HashSet<string>();

        private static readonly List<Entity> EmptyList = new List<Entity>();

        public IEnumerable<Entity> Entities
        {
            get
            {
                return database.Values;
            }
        }

        /// <summary>
        /// Resets the database so it can be reused
        /// </summary>
        public void Clear()
        {
            database.Clear();
            typeIndex.Clear();
        }

        public void AddEntities(IEnumerable<Entity> entities)
        {
            if (entities == null)
            {
                return;
            }
            deletionTracker.Clear();
            foreach (var entity in entities)
            {
                if (!deletionTracker.Contains(entity.Id))
                {
                    AddEntity(entity);
                }
                if (entity is RemoveEntity)
                {
                    deletionTracker.Add(entity.Id);
                }
            }
        }

        public void AddEntity(Entity entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                entity.Id = Ulid.NewUlid().ToString();
            }
            if (entity is RemoveEntity)
            {
                RemoveById(entity.Id);
            }
            if (entity is TemporaryEntity)
            {
                return;
            }
            if (database.TryGetValue(entity.Id, out var existing))
            {
                RemoveFromTypeIndex(existing);
            }
            database[entity.Id] = entity;
            AddToTypeIndex(entity);
        }

        private void RemoveById(string id)
        {
            if (database.TryGetValue(id, out var existing))
            {
                database.Remove(id);
                RemoveFromTypeIndex(existing);
            }
        }

        private void AddToTypeIndex(Entity entity)
        {
            for (var t = entity.GetType(); t != null && typeof(Entity).IsAssignableFrom(t); t = t.BaseType)
            {
                if (!typeIndex.TryGetValue(t, out var list))
                {
                    list = new List<Entity>();
                    typeIndex[t] = list;
                }
                list.Add(entity);
            }
        }

        private void RemoveFromTypeIndex(Entity entity)
        {
            for (var t = entity.GetType(); t != null && typeof(Entity).IsAssignableFrom(t); t = t.BaseType)
            {
                if (typeIndex.TryGetValue(t, out var list))
                {
                    list.Remove(entity);
                }
            }
        }

        private List<Entity> GetTypeList(Type t)
        {
            return typeIndex.TryGetValue(t, out var list) ? list : EmptyList;
        }

        public bool IsEmpty() => database.Count == 0;

        public T GetSingle<T>() where T : Entity
        {
            return (T)GetTypeList(typeof(T)).Single();
        }

        public T GetSingle<T>(Predicate<T> filter) where T : Entity
        {
            return (T)GetTypeList(typeof(T)).Single(e => filter((T)e));
        }

        public bool TryGetSingle<T>([NotNullWhen(returnValue: true)] out T? entity) where T : Entity
        {
            var list = GetTypeList(typeof(T));
            entity = list.Count > 0 ? (T)list[0] : null;
            return entity != null;
        }

        public bool TryGetSingle<T>(Predicate<T> filter, [NotNullWhen(returnValue: true)] out T? entity) where T : Entity
        {
            foreach (var e in GetTypeList(typeof(T)))
            {
                var typed = (T)e;
                if (filter(typed))
                {
                    entity = typed;
                    return true;
                }
            }
            entity = null;
            return false;
        }

        public bool Has<T>() where T : Entity
        {
            return GetTypeList(typeof(T)).Count > 0;
        }

        public bool Has<T>(Predicate<T> filter) where T : Entity
        {
            foreach (var e in GetTypeList(typeof(T)))
            {
                if (filter((T)e))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasId(string id)
        {
            return database.ContainsKey(id);
        }

        public Entity GetById(string id)
        {
            if (database.TryGetValue(id, out var entity))
            {
                return entity;
            }
            throw new KeyNotFoundException($"Entity with id {id} not found");
        }

        public T GetById<T>(string id) where T : Entity
        {
            return (T)GetById(id);
        }

        public bool TryGetById<T>(string id, [NotNullWhen(returnValue: true)] out T? entity) where T : Entity
        {
            if (database.TryGetValue(id, out var found))
            {
                entity = (T)found;
                return true;
            }
            entity = default;
            return false;
        }

        public IEnumerable<T> GetAll<T>() where T : Entity
        {
            return GetTypeList(typeof(T)).Cast<T>();
        }
    }
}
