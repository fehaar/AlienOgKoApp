using System.Text;
using Newtonsoft.Json;

namespace Gosuman.EntitySystem.Test.Serialization
{
    public class SerializingComponents : xSpec
    {
        [Fact]
        public void SerializingAComponent()
        {
            var entity = default(TestEntity);
            var serializer = new JsonSerializer();
            Given("an entity with a component and a serializar with the right converters", () =>
            {
                entity = new TestEntity()
                {
                    Id = "Tester",
                    Field = "Value",
                    Property = "Property"
                };
                entity.AddComponent(new TestComponent() { Field = "Data" });
                var context = new SerializerTypeContext();
                serializer.Converters.Add(new EntityConverter(context));
            });
            var serializedEntity = TestHelper.Dummy<TestEntity>();
            Where("we serialize and deserialize the entity", () =>
            {
                var sb = new StringBuilder();
                using (var tw = new StringWriter(sb))
                {
                    serializer?.Serialize(tw, entity);
                }
                using (var tr = new StringReader(sb.ToString()))
                {
                    serializedEntity = serializer?.Deserialize<TestEntity>(new JsonTextReader(tr));
                }
            });
            Expect("that the entity will have the component", () =>
            {
                serializedEntity.Should().NotBeNull();
                serializedEntity.HasComponent<TestComponent>().Should().BeTrue();
                serializedEntity.GetComponent<TestComponent>()!.Field.Should().Be("Data");
            });
        }

        [Fact]
        public void ComponentNamesWhenSerializing()
        {
            var entity = default(TestEntity);
            var serializer = new JsonSerializer();
            Given("an entity with a component and a serializar with the right converters", () =>
            {
                entity = new TestEntity()
                {
                    Id = "Tester",
                    Field = "Value",
                    Property = "Property"
                };
                entity.AddComponent(new TestComponent());
                var context = new SerializerTypeContext();
                serializer.Converters.Add(new EntityConverter(context));
            });
            var sb = new StringBuilder();
            Where("we serialize the entity", () =>
            {
                using (var tw = new StringWriter(sb))
                {
                    serializer?.Serialize(tw, entity);
                }
            });
            Expect("that the resulting text will contain the name of the component without the word Component", () =>
            {
                sb.ToString().Should().NotContain("testcomponent");
            });
        }

        [Fact]
        public void ComponentsWithEntityReferences()
        {
            var entity = default(TestEntity);
            var serializer = new JsonSerializer();
            var database = TestHelper.DummyDatabase;
            Given("an entity with a component that has a reference to another component", () =>
            {
                database = new EntityDatabase();
                entity = new TestEntity()
                {
                    Id = "Tester",
                    Field = "Value",
                    Property = "Property"
                };
                database.AddEntity(entity);
                var otherEntity = new TestEntity()
                {
                    Id = "Referenced",
                    Field = "Value",
                    Property = "Property"
                };
                database.AddEntity(otherEntity);
                entity.AddComponent(new TestComponentWithReference() { Entity = otherEntity });
                var context = new SerializerTypeContext();
                serializer.Converters.Add(new EntityDatabaseConverter());
                serializer.Converters.Add(new EntityConverter(context));
            });
            var serializedDb = TestHelper.DummyDatabase;
            Where("we serialize and deserialize the entity", () =>
            {
                var sb = new StringBuilder();
                using (var tw = new StringWriter(sb))
                {
                    serializer?.Serialize(tw, database);
                }
                using (var tr = new StringReader(sb.ToString()))
                {
                    serializedDb = serializer?.Deserialize<EntityDatabase>(new JsonTextReader(tr));
                }
            });
            Expect("that the entity will have the component and that it will have a dummy reference", () =>
            {
                serializedDb.Should().NotBeNull();
                var entity = serializedDb.GetById<TestEntity>("Tester");  
                entity.Should().NotBeNull();
                entity!.HasComponent<TestComponentWithReference>().Should().BeTrue();
                var component = entity!.GetComponent<TestComponentWithReference>()!;
                component.Entity.Should().NotBeNull();
                serializedDb.GetById<TestEntity>(component.Entity!.Id).Should().NotBeNull();
                component.Entity.Should().NotBe(serializedDb.GetById<TestEntity>("Referenced"));
            });
        }

        [Fact]
        public void ComponentsWithEntityLists()
        {
            var entity = default(TestEntity);
            var serializer = new JsonSerializer();
            var database = TestHelper.DummyDatabase;
            Given("an entity with a component that has a list of component references", () =>
            {
                database = new EntityDatabase();
                entity = new TestEntity()
                {
                    Id = "Tester",
                    Field = "Value",
                    Property = "Property"
                };
                database.AddEntity(entity);
                var otherEntity1 = new TestEntity()
                {
                    Id = "Referenced1",
                    Field = "Value",
                    Property = "Property"
                };
                var otherEntity2 = new TestEntity()
                {
                    Id = "Referenced2",
                    Field = "Value",
                    Property = "Property"
                };
                database.AddEntity(otherEntity1);
                database.AddEntity(otherEntity2);
                entity.AddComponent(new TestComponentWithEnumerableReferences() { Entities = new[] { otherEntity1, otherEntity2 } });
                var context = new SerializerTypeContext();
                serializer.Converters.Add(new EntityDatabaseConverter());
                serializer.Converters.Add(new EntityConverter(context));
            });
            var serializedDb = TestHelper.DummyDatabase;
            Where("we serialize and deserialize the entity", () =>
            {
                var sb = new StringBuilder();
                using (var tw = new StringWriter(sb))
                {
                    serializer?.Serialize(tw, database);
                }
                using (var tr = new StringReader(sb.ToString()))
                {
                    serializedDb = serializer?.Deserialize<EntityDatabase>(new JsonTextReader(tr));
                }
            });
            Expect("that the entity will have the component and that it will have the list of references", () =>
            {
                serializedDb.Should().NotBeNull();
                var entity = serializedDb.GetById<TestEntity>("Tester");
                entity.Should().NotBeNull();
                entity!.HasComponent<TestComponentWithEnumerableReferences>().Should().BeTrue();
                var component = entity!.GetComponent<TestComponentWithEnumerableReferences>()!;
                component.Entities.Should().NotBeNullOrEmpty();
                foreach (var otherEntity in component.Entities)
                {
                    otherEntity.Should().NotBeNull();
                    var e = serializedDb.GetById<TestEntity>(otherEntity.Id);
                    e.Should().NotBeNull();
                    e.Should().NotBe(otherEntity);
                }   
            });
        }
    }
}
