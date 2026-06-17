using Gosuman.EntitySystem.Serialization;
using Gosuman.TBF.Shared.Serialization;
using System.Reflection;

namespace Gosuman.TBF.Test
{
    internal static class TestHelper
    {
        internal static EntityDatabase DummyDatabase = new EntityDatabase();
        internal static TestGame DummyGame = new TestGame();
        internal static T Dummy<T>() where T : Entity, new() => new T();

        public static IEnumerable<JsonConverter> GetJsonConverters(EntityDatabase database, params Assembly[] extraAssemblies)
        {
            var assemblies = new Assembly[] {
                Assembly.GetAssembly(typeof(TestGame))!,
                Assembly.GetAssembly(typeof(Player))!,
            }.Concat(extraAssemblies ?? Array.Empty<Assembly>());
            var context = new SerializerTypeContext(assemblies);
            yield return new EntityConverter(context);
            yield return new EntityDatabaseConverter();
            yield return new GameStateIncrementConverter();
            yield return new GameActionConverter<IClientGameAction>(assemblies.ToArray());
        }

        internal static JsonSerializer GetJsonSerializer(EntityDatabase db)
        {
            var serializer = new JsonSerializer();
            foreach (var converter in GetJsonConverters(db))
            {
                serializer.Converters.Add(converter);
            }
            return serializer;
        }
    }
}
