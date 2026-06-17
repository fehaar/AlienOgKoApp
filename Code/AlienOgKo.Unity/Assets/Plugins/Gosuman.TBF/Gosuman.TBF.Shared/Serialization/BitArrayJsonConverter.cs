using Newtonsoft.Json;
using System;
using System.Collections;
using System.Linq;

namespace Gosuman.TBF.Logic.Serialization;
public class BitArrayJsonConverter : JsonConverter<BitArray>
{
    public override BitArray? ReadJson(JsonReader reader, Type objectType, BitArray? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var data = serializer.Deserialize<byte[]>(reader);
        if (data == null || data.Length == 0)
        {
            return null;
        }
        if (!hasExistingValue)
        {
            var array = new BitArray(data[0]);
            var tempArray = new BitArray(data.Skip(1).ToArray());
            for (int i = 0; i < array.Count; i++)
            {
                array[i] = tempArray[i];
            }
            return array;
        }
        return existingValue;
    }

    public override void WriteJson(JsonWriter writer, BitArray? value, JsonSerializer serializer)
    {
        if (value != null)
        {
            var values = new byte[(int)Math.Ceiling(value.Count / 8f) + 1];
            values[0] = (byte)value.Count;
            value.CopyTo(values, 1);
            serializer.Serialize(writer, values);
        }
        else
        {
            writer.WriteNull();
        }
    }
}
