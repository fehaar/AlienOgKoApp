using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Gosuman.TBF.Shared.Serialization
{
    public class GameActionConverter<T> : JsonConverter<T> where T : notnull
    {
        private Dictionary<string, Type> typeDictionary = new Dictionary<string, Type>();

        public GameActionConverter(params Assembly[] assemblies)
        {
            var actionAss = Assembly.GetAssembly(typeof(T));
            if (actionAss != null)
            {
                LoadTypesFromAssembly(actionAss);
                if (Assembly.GetCallingAssembly() != actionAss)
                {
                    LoadTypesFromAssembly(Assembly.GetCallingAssembly());
                }
            }
            if (assemblies != null)
            {
                foreach (var assembly in assemblies)
                {
                    LoadTypesFromAssembly(assembly);
                }
            }
        }

        private void LoadTypesFromAssembly(Assembly a)
        {
            /// Get all available component types for parsing
            foreach (var tp in a.GetTypes())
            {
                if (typeof(T).IsAssignableFrom(tp))
                {
                    typeDictionary[tp.Name.ToLowerInvariant()] = tp;
                }
            }
        }

        public override T? ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                reader.Read();
            }
            var typeName = reader.Value?.ToString();
            if (typeName == null)
            {
                throw new ArgumentNullException(typeName);
            }
            if (!typeDictionary.ContainsKey(typeName))
            {
                throw new InvalidDataException($"Converter does not know about a type called {typeName}");
            }
            var type = typeDictionary[typeName];
            reader.Read();
            reader.Read();
            var action = (T?)Activator.CreateInstance(type);
            while (reader.TokenType != JsonToken.EndObject)
            {
                if (reader.Value != null)
                {
                    var member = type.GetProperty(reader.Value.ToString() ?? "");
                    reader.Read();
                    if (member != null)
                    {
                        member.SetValue(action, serializer.Deserialize(reader, member.PropertyType));
                    }
                    reader.Read();
                }
            }
            reader.Read();
            return action;
        }

        public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
        {
            var wroteObjectStart = false;
            if (writer.WriteState != WriteState.Object)
            {
                wroteObjectStart = true;
                writer.WriteStartObject();
            }
            if (value != null)
            {
                writer.WritePropertyName(value.GetType().Name.ToLowerInvariant());
                writer.WriteStartObject();
                var isPlayerAction = value.GetType().GetInterface("IPlayerAction") != null;
                foreach (var prop in value.GetType().GetProperties())
                {
                    if (isPlayerAction && prop.Name == "Player")
                    {
                        continue;
                    }
                    if (typeof(Delegate).IsAssignableFrom(prop.PropertyType))
                    {
                        continue;
                    }
                    if (prop.CanWrite)
                    {
                        writer.WritePropertyName(prop.Name);
                        serializer.Serialize(writer, prop.GetValue(value));
                    }
                }
                writer.WriteEndObject();
            }
            if (wroteObjectStart)
            {
                writer.WriteEndObject();
            }
        }
    }
}