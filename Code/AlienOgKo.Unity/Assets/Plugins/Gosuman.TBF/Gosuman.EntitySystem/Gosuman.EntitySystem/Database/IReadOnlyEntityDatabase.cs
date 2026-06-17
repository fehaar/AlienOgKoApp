using System.Diagnostics.CodeAnalysis;

namespace Gosuman.EntitySystem.Database
{
    public interface IReadOnlyEntityDatabase
    {
        IEnumerable<Entity> Entities { get; }
        T GetSingle<T>() where T : Entity;
        T GetSingle<T>(Predicate<T> filter) where T : Entity;
        bool TryGetSingle<T>([NotNullWhen(returnValue: true)] out T? entity) where T : Entity;
        bool TryGetSingle<T>(Predicate<T> filter, [NotNullWhen(returnValue: true)] out T? entity) where T : Entity;
        IEnumerable<T> GetAll<T>() where T : Entity;
        bool Has<T>() where T : Entity;
        bool Has<T>(Predicate<T> filter) where T : Entity;
        bool HasId(string id);
        Entity GetById(string id);
        T GetById<T>(string id) where T : Entity;
        bool TryGetById<T>(string id, [NotNullWhen(returnValue: true)] out T? entity) where T : Entity;
        bool IsEmpty();
    }
}
