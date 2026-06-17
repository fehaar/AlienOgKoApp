using Newtonsoft.Json;
using System.Text;

namespace Gosuman.EntitySystem.Test.Serialization;

public class SerializingEntities : xSpec
{
    [Fact]
    public void SerializeAPlainEntiy()
    {
        var entity = default(TestEntity);
        var serializer = new JsonSerializer();
        Given("an entity and a serializer",
            () =>
            {
                entity = new TestEntity() {
                    Id = "Tester",
                    Field = "Value",
                    Property = "Property"
                };
                serializer = new JsonSerializer();
                serializer.Converters.Add(new EntityConverter(new SerializerTypeContext()));
            });
        var sb = new StringBuilder();
        Where("we serialize and deserialize the entity",
            () =>
            {
                using (var tw = new StringWriter(sb))
                {
                    serializer.Serialize(tw, entity);
                }
            });
        Expect("all properties to have been returned and the new entity to be of the same type",
            () =>
            {
                var serializedEntity = default(Entity);
                using (var tr = new JsonTextReader(new StringReader(sb.ToString())))
                {
                    serializedEntity = serializer.Deserialize<Entity>(tr);
                }
                serializedEntity.Should().NotBeNull();
                serializedEntity.Should().BeOfType<TestEntity>();
                serializedEntity!.Id.Should().Be(entity?.Id);
                (serializedEntity as TestEntity)?.Field.Should().Be(entity?.Field);
                (serializedEntity as TestEntity)?.Property.Should().Be(entity?.Property);
            });
    }

    [Fact]
    public void SerializeASecretEntiy()
    {
        var entity = default(TestClientEntity);
        var serializer = default(JsonSerializer);
        Given("an entity and a serializer",
            () =>
            {
                entity = new TestClientEntity()
                {
                    Id = "Tester",
                    Secret = true,
                    Field = "Value",
                    Property = "Property"
                };
                serializer = new JsonSerializer();
                serializer.Converters.Add(new EntityConverter(new SerializerTypeContext()));
            });
        var sb = new StringBuilder();
        Where("we serialize and deserialize the entity",
            () =>
            {
                using (var tw = new StringWriter(sb))
                {
                    serializer?.Serialize(tw, entity);
                }
            });
        Expect("all properties to have been returned and the new entity to be of the same type",
            () =>
            {
                var serializedEntity = default(ClientEntity);
                using (var tr = new JsonTextReader(new StringReader(sb.ToString())))
                {
                    serializedEntity = serializer?.Deserialize<ClientEntity>(tr);
                }
                serializedEntity.Should().NotBeNull();
                serializedEntity.Should().BeOfType<TestClientEntity>();
                serializedEntity!.Id.Should().Be(entity?.Id);
                serializedEntity.Secret.Should().BeTrue();
                (serializedEntity as TestClientEntity)?.Field.Should().Be(entity?.Field);
                (serializedEntity as TestClientEntity)?.Property.Should().Be(entity?.Property);
            });
    }

    [Fact]
    public void SerializeAnEntityWithAnEntityReference()
    {
        var entity = default(TestEntityWithReference);
        var serializer = default(JsonSerializer);
        Given("an entity and a serializer",
            () =>
            {
                entity = new TestEntityWithReference()
                {
                    Id = "Tester",
                    Field = new TestEntity() { Id = "Ref", Field = "Value", Property = "Prop" },
                    Property = new TestEntity() { Id = "Ref1", Field = "Value", Property = "Prop" }
                };
                serializer = new JsonSerializer();
                serializer.Converters.Add(new EntityConverter(new SerializerTypeContext()));
            });
        var sb = new StringBuilder();
        Where("we serialize and deserialize the entity",
            () =>
            {
                using (var tw = new StringWriter(sb))
                {
                    serializer?.Serialize(tw, entity);
                }
            });
        Expect("the deserialized entity contains a dummy entity with the same id as the original",
            () =>
            {
                var serializedEntity = default(Entity);
                using (var tr = new JsonTextReader(new StringReader(sb.ToString())))
                {
                    serializedEntity = serializer?.Deserialize<Entity>(tr);
                }
                serializedEntity.Should().NotBeNull();
                serializedEntity.Should().BeOfType<TestEntityWithReference>();
                serializedEntity!.Id.Should().Be(entity!.Id);
                var testEntity = (serializedEntity as TestEntityWithReference);
                testEntity!.Field.Should().NotBeNull();
                testEntity.Field.Should().NotBe(entity.Field);
                testEntity.Field!.Id.Should().Be(entity.Field!.Id);
                testEntity!.Property.Should().NotBeNull();
                testEntity.Property.Should().NotBe(entity.Property);
                testEntity.Property!.Id.Should().Be(entity.Property!.Id);
            });
    }


    [Fact]
    public void SerializeAnEntityWithANullReference()
    {
        var entity = default(TestEntityWithReference);
        var serializer = default(JsonSerializer);
        Given("an entity and a serializer",
            () =>
            {
                entity = new TestEntityWithReference()
                {
                    Id = "Tester",
                    Field = null,
                    Property = null
                };
                serializer = new JsonSerializer();
                serializer.Converters.Add(new EntityConverter(new SerializerTypeContext()));
            });
        var sb = new StringBuilder();
        Where("we serialize and deserialize the entity",
            () =>
            {
                using (var tw = new StringWriter(sb))
                {
                    serializer?.Serialize(tw, entity);
                }
            });
        Expect("the deserialized entity does not contain the reference",
            () =>
            {
                var serializedEntity = default(Entity);
                using (var tr = new JsonTextReader(new StringReader(sb.ToString())))
                {
                    serializedEntity = serializer?.Deserialize<Entity>(tr);
                }
                serializedEntity.Should().NotBeNull();
                serializedEntity.Should().BeOfType<TestEntityWithReference>();
                serializedEntity!.Id.Should().Be(entity?.Id);
                (serializedEntity as TestEntityWithReference)?.Field.Should().BeNull();
                (serializedEntity as TestEntityWithReference)?.Property.Should().BeNull();
            });
    }

    [Fact]
    public void SerializeEntititiesThatReferences()
    {
        var entity = TestHelper.Dummy<TestEntityWithReference>();
        var entity1 = TestHelper.Dummy<TestEntity>();
        var serializer = default(JsonSerializer);
        Given("an entity and a serializer",
            () =>
            {
                entity1 = new TestEntity() { Id = "Ref", Field = "Value", Property = "Prop" };
                entity = new TestEntityWithReference()
                {
                    Id = "Tester",
                    Field = entity1,
                    Property = entity1
                };
                serializer = new JsonSerializer();
                serializer.Converters.Add(new EntityConverter(new SerializerTypeContext()));
            });
        var sb = new StringBuilder();
        Where("we serialize and deserialize the entities",
            () =>
            {
                using (var tw = new StringWriter(sb))
                {
                    serializer?.Serialize(tw, new Entity[] { entity, entity1 });
                }
            });
        Expect("the deserialized entity does not contain the reference as it has not been serialized seperately",
            () =>
            {
                var serializedEntities = default(Entity[]);
                using (var tr = new JsonTextReader(new StringReader(sb.ToString())))
                {
                    serializedEntities = serializer?.Deserialize<Entity[]>(tr);
                }
                serializedEntities.Should().NotBeNull();
                serializedEntities!.Length.Should().Be(2);
                serializedEntities.Count(e => e is TestEntity).Should().Be(1);
                serializedEntities.Count(e => e is TestEntityWithReference).Should().Be(1);
                var testEntity = serializedEntities.First(e => e is TestEntity);
                var testEntityWithRef = serializedEntities.First(e => e is TestEntityWithReference);
                (testEntityWithRef as TestEntityWithReference)?.Field!.Id.Should().Be(testEntity.Id);
                (testEntityWithRef as TestEntityWithReference)?.Property!.Id.Should().Be(testEntity.Id);
            });
    }

    [Fact]
    public void SerializeAnEntityWithAnEntityList()
    {
        var entity = TestHelper.Dummy<TestEntityWithEnumerableReference>();
        var entities = Array.Empty<TestEntity>();
        var serializer = default(JsonSerializer);
        Given("an entity and a serializer",
            () =>
            {
                entities = new[]
                {
                    new TestEntity() { Id = "One", Field = "Value", Property = "Prop" },
                    new TestEntity() { Id = "Two", Field = "Value", Property = "Prop" },
                    new TestEntity() { Id = "Three", Field = "Value", Property = "Prop" }
                };
                entity = new TestEntityWithEnumerableReference()
                {
                    Id = "Tester",
                    Entities = entities
                };
                serializer = new JsonSerializer();
                var context = new SerializerTypeContext();
                serializer.Converters.Add(new EntityConverter(context));
            });
        var sb = new StringBuilder();
        Where("we serialize and deserialize the entities",
            () =>
            {
                using (var tw = new StringWriter(sb))
                {
                    serializer?.Serialize(tw, new Entity[] { entity }.Concat(entities));
                }
            });
        Expect("the deserialized entity has a list of dummy entities",
            () =>
            {
                var serializedEntities = default(Entity[]);
                using (var tr = new JsonTextReader(new StringReader(sb.ToString())))
                {
                    serializedEntities = serializer?.Deserialize<Entity[]>(tr);
                }
                serializedEntities.Should().NotBeNull();
                serializedEntities!.Length.Should().Be(4);
                serializedEntities.Count(e => e is TestEntity).Should().Be(3);
                serializedEntities.Count(e => e is TestEntityWithEnumerableReference).Should().Be(1);
                var testEntityWithRef = (TestEntityWithEnumerableReference)serializedEntities.First(e => e is TestEntityWithEnumerableReference);
                testEntityWithRef.Entities.Should().NotBeNull();
                testEntityWithRef.Entities.Count().Should().Be(3);
                testEntityWithRef.Entities.All(e => !serializedEntities.Contains(e)).Should().BeTrue();
            });
    }

    [Fact]
    public void IgnoringFieldsAndProperties()
    {
        var entity = default(TestEntityWithIgnoredData);
        var serializer = new JsonSerializer();
        Given("an entity and a serializer",
            () =>
            {
                entity = new TestEntityWithIgnoredData
                {
                    Id = "Tester",
                    Field = "Value",
                    Property = "Property"
                };
                serializer = new JsonSerializer();
                serializer.Converters.Add(new EntityConverter(new SerializerTypeContext()));
            });
        var sb = new StringBuilder();
        Where("we serialize and deserialize the entity",
            () =>
            {
                using (var tw = new StringWriter(sb))
                {
                    serializer.Serialize(tw, entity);
                }
            });
        Expect("all properties to have been returned and the new entity to be of the same type",
            () =>
            {
                var serializedEntity = default(Entity);
                using (var tr = new JsonTextReader(new StringReader(sb.ToString())))
                {
                    serializedEntity = serializer.Deserialize<Entity>(tr);
                }
                serializedEntity.Should().NotBeNull();
                serializedEntity.Should().BeOfType<TestEntityWithIgnoredData>();
                serializedEntity!.Id.Should().Be(entity?.Id);
                (serializedEntity as TestEntityWithIgnoredData)!.Field.Should().BeNull();
                (serializedEntity as TestEntityWithIgnoredData)!.Property.Should().Be(string.Empty);
            });
    }

}
