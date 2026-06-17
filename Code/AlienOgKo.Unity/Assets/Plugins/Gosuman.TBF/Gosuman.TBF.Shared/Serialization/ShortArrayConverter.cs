using Newtonsoft.Json;
using System;

namespace Gosuman.TBF.Logic.Serialization;

public class ShortArrayConverter : JsonConverter<short[]>
{
    public override short[]? ReadJson(JsonReader reader, Type objectType, short[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var data = serializer.Deserialize<byte[]>(reader);
        if (data == null || data.Length == 0 || data.Length % 2 != 0)
        {
            return null;
        }
        if (!hasExistingValue)
        {
            var array = new short[data.Length / 2];
            Buffer.BlockCopy(data, 0, array, 0, data.Length);
            return array;
        }
        return existingValue;
    }
    public override void WriteJson(JsonWriter writer, short[]? value, JsonSerializer serializer)
    {
        if (value != null)
        {
            var values = new byte[value.Length * 2];
            Buffer.BlockCopy(value, 0, values, 0, values.Length);
            serializer.Serialize(writer, values);
        }
        else
        {
            writer.WriteNull();
        }
    }
}
