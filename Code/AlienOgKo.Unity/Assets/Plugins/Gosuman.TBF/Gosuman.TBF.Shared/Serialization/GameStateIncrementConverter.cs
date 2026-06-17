using Gosuman.EntitySystem;
using Gosuman.TBF.Shared.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Gosuman.TBF.Shared.Serialization
{
    public class GameStateIncrementConverter : JsonConverter<IGameStateIncrement>
    {
        private readonly List<IClientGameAction> actions = new List<IClientGameAction>();

        public override IGameStateIncrement ReadJson(JsonReader reader, Type objectType, [AllowNull] IGameStateIncrement existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var gsi = new GameStateIncrement();
            reader.Read(); // Start object
            reader.Read();
            reader.Read();
            actions.Clear();
            while (reader.TokenType == JsonToken.StartObject)
            {
                var action = serializer.Deserialize<IClientGameAction>(reader);
                if (action != null)
                {
                    actions.Add(action);
                }
                reader.Read();
            }
            gsi.AvailableActions = actions.ToArray();
            reader.Read();
            reader.Read();
            reader.Read(); // StartArray
            var entities = new List<Entity>();
            while (reader.TokenType == JsonToken.StartObject)
            {
                var entity = serializer.Deserialize<Entity>(reader);
                if (entity != null)
                {
                    entities.Add(entity);
                }
                reader.Read();
            }
            gsi.Entities = entities;
            reader.Read(); // End array
            return gsi;
        }

        public override void WriteJson(JsonWriter writer, IGameStateIncrement? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteStartObject();
            writer.WritePropertyName("actions");
            writer.WriteStartArray();
            foreach (var action in value.AvailableActions)
            {
                serializer.Serialize(writer, action);
            }
            writer.WriteEndArray();
            writer.WritePropertyName("entities");
            writer.WriteStartArray();
            foreach (var entity in value.Entities)
            {
                serializer.Serialize(writer, entity);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}