using Gosuman.EntitySystem.Database;
using System.Collections;

namespace Gosuman.EntitySystem.Serialization
{
    public interface IReferenceFixable
    {
        void FixReferences(Func<string, Entity?> getEntityById);
    }

    public static class EntityReferenceFixer
    {
        public static void Fix(EntityDatabase database)
        {
            foreach (var entity in database.Entities)
            {
                FixObject(entity, database, (id) => null);
                (entity as IReferenceFixable)?.FixReferences((id) => (database.HasId(id)) ? database.GetById(id) : null);
                foreach (var component in entity.Components)
                {
                    FixObject(component, database, (id) => null);
                    (component as IReferenceFixable)?.FixReferences((id) => (database.HasId(id)) ? database.GetById(id) : null);
                }
            }
        }

        public static void Fix(IEnumerable<Entity> entities, EntityDatabase database)
        {
            foreach (var entity in entities)
            {
                FixObject(entity, database, (id) => entities.FirstOrDefault(e => e.Id == id));
                (entity as IReferenceFixable)?.FixReferences((id) => entities.FirstOrDefault(e => e.Id == id));
                foreach (var component in entity.Components)
                {
                    FixObject(component, database, (id) => entities.FirstOrDefault(e => e.Id == id));
                    (component as IReferenceFixable)?.FixReferences((id) => entities.FirstOrDefault(e => e.Id == id));
                }
            }
        }

        public static void FixObject(Object obj, EntityDatabase database, Func<string, Entity?> fallback)
        {
            foreach (var member in obj.GetType().GetProperties())
            {
                if (!member.CanWrite) continue;

                if (typeof(Entity).IsAssignableFrom(member.PropertyType))
                {
                    var value = (Entity?)member.GetValue(obj);
                    if (value != null)
                    {
                        var found = (database.HasId(value.Id)) ? database.GetById(value.Id) : fallback(value.Id);
                        if (found != null)
                        {
                            member.SetValue(obj, found);
                        }
                    }
                }
                else if (typeof(IEnumerable<Entity>).IsAssignableFrom(member.PropertyType))
                {
                    var oldEnum = member.GetValue(obj) as IEnumerable<Entity>;
                    if (oldEnum != null)
                    {
                        var list = (IList?)Activator.CreateInstance(typeof(List<>).MakeGenericType(member.PropertyType.GenericTypeArguments));
                        foreach (var value in oldEnum)
                        {
                            var found = (database.HasId(value.Id)) ? database.GetById(value.Id) : fallback(value.Id);
                            if (found != null)
                            {
                                list!.Add(found);
                            }
                        }
                        member.SetValue(obj, list);
                    }
                }
            }
            foreach (var member in obj.GetType().GetFields())
            {
                if (typeof(Entity).IsAssignableFrom(member.FieldType))
                {
                    var value = (Entity?)member.GetValue(obj);
                    if (value != null)
                    {
                        var found = (database.HasId(value.Id)) ? database.GetById(value.Id) : fallback(value.Id);
                        if (found != null)
                        {
                            member.SetValue(obj, found);
                        }
                    }
                }
                else if (typeof(IEnumerable<Entity>).IsAssignableFrom(member.FieldType))
                {
                    var oldEnum = member.GetValue(obj) as IEnumerable<Entity>;
                    if (oldEnum != null)
                    {
                        var list = (IList?)Activator.CreateInstance(typeof(List<>).MakeGenericType(member.FieldType.GenericTypeArguments));
                        foreach (var value in oldEnum)
                        {
                            var found = (database.HasId(value.Id)) ? database.GetById(value.Id) : fallback(value.Id);
                            if (found != null)
                            {
                                list!.Add(found);
                            }
                        }
                        member.SetValue(obj, list);
                    }
                }
            }
        }
    }
}
