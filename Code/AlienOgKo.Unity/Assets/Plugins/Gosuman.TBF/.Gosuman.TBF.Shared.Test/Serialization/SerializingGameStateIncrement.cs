using Gosuman.EntitySystem.Serialization;
using Gosuman.TBF.Shared.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Gosuman.TBF.Shared.Test.Serialization;
public class SerializingGameStateIncrement : xSpec
{
    private IEnumerable<Entity> TestEntities()
    {
        var entity = new TestEntity { Field = "MyField", Property = "MyProperty" };
        yield return entity;
        yield return new TestEntityWithReference { Entity = entity };
    }

    [Fact]
    public void SerializeToJson()
    {
        var increment = default(GameStateIncrement);
        var serializer = default(JsonSerializer);
        Given("a game state increment with entities in it and a primed serializer", () => {
            increment = new GameStateIncrement()
            {
                AvailableActions = new IClientGameAction[] {
                        new TestAction() { Value = 2 },
                        new TestAction() { Value = 3 }
                    },
                Entities = TestEntities().ToArray(),
            };
            serializer = new JsonSerializer();
            serializer.Converters.Add(new EntityConverter(new SerializerTypeContext()));
            serializer.Converters.Add(new GameStateIncrementConverter());
            serializer.Converters.Add(new GameActionConverter<IClientGameAction>());
        });
        var increment1 = default(GameStateIncrement);
        That("we serialize to JSON and back", () => {
            var sb = new StringBuilder();
            using (var tw = new StringWriter(sb))
            {
                serializer?.Serialize(tw, increment);
            }
            var t = sb.ToString();
            using (var tr = new JsonTextReader(new StringReader(t)))
            {
                increment1 = serializer?.Deserialize<GameStateIncrement>(tr);
            }
        });
        Expect("the increments to be the same", () => {
            if (increment != null && increment1 != null)
            {
                increment1.AvailableActions.Should().HaveCount(2);
                var action = increment1.AvailableActions.First() as TestAction;
                action.Should().NotBeNull();
                action?.Value.Should().Be(2);

                increment1.Entities.Should().NotBeNullOrEmpty();
                increment1.Entities.Should().HaveCount(increment?.Entities.Count() ?? -1);
                var missingEntities = increment?.Entities.Except(increment1.Entities, new EntityEqualityComparer()).ToArray();
                missingEntities.Should().BeEmpty();
            }
        });
    }

    [Fact]
    public void SerializingToStream()
    {
        var increment = default(GameStateIncrement);
        var serializer = default(JsonSerializer);
        Given("a game state increment with entities in it and a primed serializer", () => {
            increment = new GameStateIncrement()
            {
                AvailableActions = new IClientGameAction[] {
                        new TestAction() { Value = 2 },
                        new TestAction() { Value = 3 }
                    },
                Entities = TestEntities().ToArray(),
            };
            serializer = new JsonSerializer();
            serializer.Converters.Add(new EntityConverter(new SerializerTypeContext()));
            serializer.Converters.Add(new GameStateIncrementConverter());
            serializer.Converters.Add(new GameActionConverter<IClientGameAction>());
        });

        That("we serialize to a memory stream", () => {
            using var ms = new MemoryStream();
            using var tw = new StreamWriter(ms);
            serializer?.Serialize(tw, increment);
            tw.Flush();
            ms.Position.Should().BeGreaterThan(0);
        });
    }
}
