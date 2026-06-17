using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Gosuman.EntitySystem.Test.Serialization
{
    public class SerializingAnEntityDatabase : xSpec
    {
        [Fact]
        public void WeCanSerializeEntitiesWithoutReferences()
        {
            var db = TestHelper.DummyDatabase;
            Given("an entity database with basic entities", () =>
            {
                db = new EntityDatabase();
                db.AddEntity(new TestEntity());
                db.AddEntity(new TestEntity());
                db.AddEntity(new TestEntity());
            });
            var deserializedDb = TestHelper.DummyDatabase;
            That("we serialize out and back", () =>
            {
                var serializer = new JsonSerializer();
                var context = new SerializerTypeContext();
                serializer.Converters.Add(new EntityConverter(context));
                serializer.Converters.Add(new EntityDatabaseConverter());
                var sb = new StringBuilder();
                using (var tw = new StringWriter(sb))
                {
                    serializer.Serialize(tw, db);
                }
                using (var tr = new JsonTextReader(new StringReader(sb.ToString())))
                {
                    deserializedDb = serializer.Deserialize<EntityDatabase>(tr);
                }
            });
            Expect("that we will have all the entities deserialized", () =>
            {
                deserializedDb.Entities.Count().Should().Be(3);
            });
        }

        [Fact]
        public void WeCanSerializeEntitiesWithReferences()
        {
            var db = TestHelper.DummyDatabase;
            Given("an entity database with basic entities", () =>
            {
                db = new EntityDatabase();
                var entity1 = new TestEntity();
                var entity2 = new TestEntity();
                db.AddEntity(entity1);
                db.AddEntity(entity2);
                db.AddEntity(new TestEntityWithReference { Field = entity1, Property = entity1 });
                db.AddEntity(new TestEntityWithReference { Field = entity2, Property = entity2 });
            });
            var deserializedDb = TestHelper.DummyDatabase;
            That("we serialize out and back", () =>
            {
                var serializer = new JsonSerializer();
                var context = new SerializerTypeContext();
                serializer.Converters.Add(new EntityConverter(context));
                serializer.Converters.Add(new EntityDatabaseConverter());
                var sb = new StringBuilder();
                using (var tw = new StringWriter(sb))
                {
                    serializer.Serialize(tw, db);
                }
                using (var tr = new JsonTextReader(new StringReader(sb.ToString())))
                {
                    deserializedDb = serializer.Deserialize<EntityDatabase>(tr);
                }
            });
            Expect("that we will have all the entities deserialized", () =>
            {
                deserializedDb.Entities.Count().Should().Be(4);
                foreach (var entity in deserializedDb.GetAll<TestEntityWithReference>())
                {
                    entity.Property.Should().NotBeNull();
                    // We will have references that are dummies and needs to be fixed up
                    deserializedDb.Entities.Contains(entity.Field).Should().BeFalse();
                    deserializedDb.Entities.Contains(entity.Property).Should().BeFalse();
                }
            });
        }

        [Fact]
        public void WeCanSerializeEntitiesWithEnumerableReferences()
        {
            var db = TestHelper.DummyDatabase;
            Given("an entity database with basic entities", () =>
            {
                db = new EntityDatabase();
                var entity1 = new TestEntity();
                var entity2 = new TestEntity();
                var entity3 = new TestEntity();
                db.AddEntity(entity1);
                db.AddEntity(entity2);
                db.AddEntity(entity3);
                db.AddEntity(new TestEntityWithEnumerableReference { 
                    Entities = [entity1, entity2, entity3],
                    EntitiesField = [entity1, entity2, entity3]
                });
            });
            var deserializedDb = TestHelper.DummyDatabase;
            That("we serialize out and back", () =>
            {
                var serializer = new JsonSerializer();
                var context = new SerializerTypeContext();
                serializer.Converters.Add(new EntityConverter(context));
                serializer.Converters.Add(new EntityDatabaseConverter());
                var sb = new StringBuilder();
                using (var tw = new StringWriter(sb))
                {
                    serializer.Serialize(tw, db);
                }
                using (var tr = new JsonTextReader(new StringReader(sb.ToString())))
                {
                    deserializedDb = serializer.Deserialize<EntityDatabase>(tr);
                }
            });
            Expect("that we will have all the entities deserialized", () =>
            {
                deserializedDb.Entities.Count().Should().Be(4);
                foreach (var entity in deserializedDb.GetAll<TestEntityWithEnumerableReference>())
                {
                    entity.Entities.Should().NotBeNullOrEmpty();
                    foreach (var e in entity.Entities)
                    {
                        var fromDb = deserializedDb.GetById(e.Id);
                        fromDb.Should().NotBeNull();
                        fromDb.Should().NotBe(e);
                    }
                    foreach (var e in entity.EntitiesField)
                    {
                        var fromDb = deserializedDb.GetById(e.Id);
                        fromDb.Should().NotBeNull();
                        fromDb.Should().NotBe(e);
                    }
                }
            });
        }
    }
}
