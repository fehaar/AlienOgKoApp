using Gosuman.EntitySystem.Database;
using Newtonsoft.Json;

namespace Gosuman.EntitySystem.Serialization
{
    public class EntityDatabaseConverter : JsonConverter<IReadOnlyEntityDatabase>
    {
        public override IReadOnlyEntityDatabase ReadJson(JsonReader reader, Type objectType, IReadOnlyEntityDatabase? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            reader.Read();
            var db = new EntityDatabase();
            while (reader.TokenType == JsonToken.StartObject)
            {
                var entity = serializer.Deserialize<Entity>(reader);
                if (entity != null)
                {
                    db.AddEntity(entity);
                }
                reader.Read();
            }
            return db;
        }

        public override void WriteJson(JsonWriter writer, IReadOnlyEntityDatabase? value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            if (value != null)
            {
                foreach (var entity in value.Entities)
                {
                    serializer.Serialize(writer, entity);
                }
            }
            writer.WriteEndArray();
        }
    }
}
