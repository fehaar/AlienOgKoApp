using Newtonsoft.Json;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Gosuman.EntitySystem.Serialization
{
    public partial class EntityConverter : JsonConverter<Entity>
    {
        public SerializerTypeContext context { get; private set; }

        private class DummyEntity : Entity
        {
        }

        public EntityConverter(SerializerTypeContext context)
        {
            this.context = context;
        }

        internal EntityConverter(EntityConverter entityConverter)
        {
            context = entityConverter.context;
        }

        public override Entity? ReadJson(JsonReader reader, Type objectType, Entity? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                // We are reading in a full entity
                // Read start object
                reader.Read();
                // Read property name Id
                reader.Read();
                var id = reader.Value?.ToString();
                // Read Id value
                reader.Read();
                var secret = false;
                if (reader.TokenType == JsonToken.PropertyName && reader.Value?.ToString() == "secret")
                {
                    // Read secret property if it is a ClientEntity
                    reader.Read();
                    // Read secret 
                    secret = Convert.ToBoolean(reader.Value);
                    reader.Read();
                }
                // Read property name to get the entity type
                var typeName = reader.Value?.ToString() ?? string.Empty;
                if (!context.HasType(typeName))
                {
                    throw new Exception($"Converter does not know about a type called {typeName}");
                }
                var type = context.GetType(typeName);
                reader.Read();
                // Read the start of the entity object
                reader.Read();
                // Read entity values
                var entity = (Entity?)Activator.CreateInstance(type);
                if (entity == null)
                {
                    // We can't create the entity. Should we fail or just skip it?
                    throw new NullReferenceException($"Could not create an instance of {type.Name}");
                }
                while (reader.TokenType != JsonToken.EndObject)
                {
                    // This is a member of the entity type
                    var propName = reader.Value?.ToString() ?? string.Empty;
                    var memberProperty = type.GetProperty(propName);
                    reader.Read();
                    if (memberProperty == null)
                    {
                        var memberField = type.GetField(propName);
                        if (memberField != null)
                        { 
                            memberField.SetValue(entity, serializer.Deserialize(reader, memberField.FieldType));
                        }
                        else
                        {
#if NETCOREAPP
                            System.Diagnostics.Debug.WriteLine($"{type.Name} does not have a property called {propName} that has a value in JSON");
#endif
                        }
                    }
                    else
                    {
                        if (memberProperty.CanWrite)
                        {
                            memberProperty.SetValue(entity, serializer.Deserialize(reader, memberProperty.PropertyType));
                        }
                    }
                    reader.Read();
                }
                // Read the end of the entity object
                reader.Read();
                // Read components if there are any
                while (reader.TokenType == JsonToken.PropertyName)
                {
                    var componentTypeName = context.DesanitizeComponentName(reader.Value?.ToString() ?? string.Empty);
                    var propertyType = context.GetType(componentTypeName);
                    reader.Read();
                    var component = (IComponent?)serializer.Deserialize(reader, propertyType);
                    if (component != null)
                    {
                        entity.AddComponent(component);
                    }
                    reader.Read();
                }
                if (id != null)
                {
                    entity.Id = id;
                    if (entity is ClientEntity ce)
                    {
                        ce.Secret = secret;
                    }
                }
                return entity;
            }
            else if (reader.TokenType == JsonToken.String)
            {
                var val = reader.Value?.ToString();
                if (objectType == typeof(Entity))
                {
                    return new DummyEntity { Id = val! };
                }
                var dummy = Activator.CreateInstance(objectType) as Entity;
                dummy!.Id = val!;
                return dummy;
            }
            return null;
        }


#if NETCOREAPP
        private Regex[] fullSerializePaths = new Regex[] {
            Path1(),
            Path2(),
            Path3(),
            Path4(),
        };

        [GeneratedRegex("^(result\\.|arguments\\[\\d+\\]\\.)?(entities)?\\[\\d+\\]$")]
        private static partial Regex Path1();

        [GeneratedRegex("^(result\\.|arguments\\[\\d+\\]\\.)?entities$")]
        private static partial Regex Path2();

        [GeneratedRegex("^result$")]
        private static partial Regex Path3();

        [GeneratedRegex("^arguments$")]
        private static partial Regex Path4();
#else
        private Regex[] fullSerializePaths = new Regex[] {
            new Regex(@"^(result\\.|arguments\\[\\d+\\]\\.)?(entities)?\\[\\d+\\]$"),
            new Regex(@"^(result\\.|arguments\\[\\d+\\]\\.)?entities$"),
            new Regex(@"^arguments$"),
            new Regex(@"^result$")
        };
#endif

        public override void WriteJson(JsonWriter writer, Entity? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            if (!string.IsNullOrEmpty(writer.Path) && !fullSerializePaths.Any(r => r.IsMatch(writer.Path)))
            {
                // We are serializing an entity in an enumerable
                writer.WriteValue(value.Id);
                return;
            }
            writer.WriteStartObject();
            writer.WritePropertyName("id");
            writer.WriteValue(value.Id);
            if (value is ClientEntity ce && ce.Secret)
            {
                writer.WritePropertyName("secret");
                serializer.Serialize(writer, ce.Secret);
            }
            writer.WritePropertyName(value.GetType().Name.ToLowerInvariant());
            writer.WriteStartObject();
            // We will write our own handling here because we want the object to be serialized in a specific way
            foreach (var prop in value.GetType().GetProperties())
            {
                if (!prop.CanWrite)
                {
                    continue;
                }
                if (prop.DeclaringType == typeof(Entity))
                {
                    continue;
                }
                if (prop.GetCustomAttributes<JsonIgnoreAttribute>()?.Any() ?? false)
                {
                    continue;
                }
                writer.WritePropertyName(prop.Name);
                if (typeof(Entity).IsAssignableFrom(prop.PropertyType))
                {
                    var v = prop.GetValue(value) as Entity;
                    if (v != null)
                    {
                        serializer.Serialize(writer, v.Id);
                    }
                    else
                    {
                        writer.WriteNull();
                    }
                }
                else
                {
                    serializer.Serialize(writer, prop.GetValue(value));
                }
            }
            foreach (var field in value.GetType().GetFields())
            {
                if (field.IsInitOnly)
                {
                    continue;
                }
                if (field.GetCustomAttributes<JsonIgnoreAttribute>()?.Any() ?? false)
                {
                    continue;
                }
                writer.WritePropertyName(field.Name);
                if (typeof(Entity).IsAssignableFrom(field.FieldType))
                {
                    var v = field.GetValue(value) as Entity;
                    if (v != null)
                    {
                        serializer.Serialize(writer, v.Id);
                    }
                    else
                    {
                        writer.WriteNull();
                    }
                }
                else
                {
                    serializer.Serialize(writer, field.GetValue(value));
                }
            }
            writer.WriteEndObject();
            // Serialize components
            foreach (var item in value.Components)
            {
                writer.WritePropertyName(context.SanitizeComponentName(item.GetType()));
                serializer.Serialize(writer, item);
            }
            writer.WriteEndObject();
        }
    }
}
