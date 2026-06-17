using Gosuman.EntitySystem.Serialization;
using Gosuman.TBF.Logic.Serialization;
using Gosuman.TBF.Shared.Entities;
using Gosuman.TBF.Shared.Serialization;
using Newtonsoft.Json;
using System.Reflection;

namespace Gosuman.TBF.Logic;
public static class JsonConverters
{
    public static IEnumerable<JsonConverter> GetJsonConverters(params Assembly[] extraAssemblies)
    {
        var assemblies = new Assembly[] {
            Assembly.GetExecutingAssembly(),
            typeof(GameState).Assembly
        }.Concat(extraAssemblies ?? Array.Empty<Assembly>());
        var context = new SerializerTypeContext(assemblies);
        yield return new EntityConverter(context);
        yield return new EntityDatabaseConverter();
        yield return new GameStateIncrementConverter();
        yield return new BitArrayJsonConverter();
        yield return new ShortArrayConverter();
#if NETCOREAPP
        yield return new GameActionConverter<Gosuman.TBF.Interfaces.IServerGameAction>();
#else
        yield return new GameActionConverter<Gosuman.TBF.Shared.Interfaces.IClientGameAction>();
#endif
    }

    public static JsonSerializerSettings GetJsonSerializerSettings(params Assembly[] extraAssemblies)
    {
        var settings = new JsonSerializerSettings();
        foreach (var conv in GetJsonConverters(extraAssemblies))
        {
            settings.Converters.Add(conv);
        }
        return settings;
    }
}
