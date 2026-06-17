using Newtonsoft.Json;
using System.Text;

namespace Gosuman.EntitySystem.Test.Serialization
{
    public class UsingTheEntityDatabase : xSpec
    {
        [Fact]
        public void WeCanAddEntitiesToTheDatabase()
        {
            var database = TestHelper.DummyDatabase;
            Given("an entity database",
                () =>
                {
                    database = new EntityDatabase();
                });
            Where("we add some entities",
                () =>
                {
                    database.AddEntities(new[] { 
                        new TestEntity(),
                        new TestEntity(),
                        new TestEntity(),
                        new TestEntity(),
                    });
                });
            Expect("that the entites are there",
                () =>
                {
                    database.Entities.Should().NotBeNullOrEmpty();
                    database.Entities.Should().HaveCount(4);
                });
        }

        [Fact]
        public void WeCanGetEntitiesEasilyByType()
        {
            var database = TestHelper.DummyDatabase;
            Given("an entity database",
                () =>
                {
                    database = new EntityDatabase();
                });
            var entity = default(TestEntity);
            Where("we add an entity of a specific type",
                () =>
                {
                    entity = new TestEntity();
                    database.AddEntity(entity);
                });
            Expect("to be able to get the entity back by type",
                () =>
                {
                    database.GetSingle<TestEntity>().Should().Be(entity);
                });
            Expect("that a non existing type will fail",
                () =>
                {
                    database.Invoking(x => x.GetSingle<TestEntityWithReference>()).Should().Throw<Exception>();
                });
        }

        [Fact]
        public void WeCanGetEntitiesByPredicate()
        {
            var database = TestHelper.DummyDatabase;
            var entity = TestHelper.Dummy<TestEntity>();
            Given("a database with a named entity", () =>
            {
                database = new EntityDatabase();
                entity = new TestEntity() { Field = "Test" };
                database.AddEntity(entity);
            });
            Expect("that we can get the entity by predicate", () =>
            {
                database.GetSingle<TestEntity>(x => x.Field == "Test").Should().Be(entity);
            });
        }

        [Fact]
        public void NotFindingTheAntityWillThrow()
        {
            var database = TestHelper.DummyDatabase;
            Given("a database with a named entity", () =>
            {
                database = new EntityDatabase();
            });
            Expect("that looking for an unknown entity will fail", () =>
            {
                database.Invoking(x => x.GetSingle<TestEntity>(x => x.Field == "Test")).Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public void WeCanTryGettingASingleEntity()
        {
            var database = TestHelper.DummyDatabase;
            var entity = TestHelper.Dummy<TestEntity>();
            Given("a database with a named entity", () =>
            {
                database = new EntityDatabase();
                entity = new TestEntity() { Field = "Test" };
                database.AddEntity(entity);
            });
            Expect("that we can try to get a single entity", () =>
            {
                database.TryGetSingle<TestEntity>(x => x.Field == "Test", out var result).Should().BeTrue();
                result.Should().Be(entity);
                database.TryGetSingle<TestEntity>(x => x.Field == "Test1", out var result1).Should().BeFalse();
                result1.Should().BeNull();
            });
        }

        [Fact]
        public void WeCanCheckIfASpecificEntityIsThere()
        {
            var database = TestHelper.DummyDatabase;
            Given("a database with a named entity", () =>
            {
                database = new EntityDatabase();
                var entity = new TestEntity() { Field = "Test" };
                database.AddEntity(entity);
            });
            Expect("that we will know if an entity is there", () =>
            {
                database.Has<TestEntity>(x => x.Field == "Test").Should().BeTrue();
                database.Has<TestEntity>(x => x.Field == "Test1").Should().BeFalse();
            });
        }

        [Fact]
        public void WeCanGetAllEntitiesOfAGivenType()
        {
            var database = TestHelper.DummyDatabase;
            Given("an entity database",
                () =>
                {
                    database = new EntityDatabase();
                });
            var entity = TestHelper.Dummy<TestEntity>();
            var entity1 = TestHelper.Dummy<TestEntity>();
            Where("we add two entities of a specific type and one that is not of that type",
                () =>
                {
                    entity = new TestEntity();
                    entity1 = new TestEntity();
                    database.AddEntity(entity);
                    database.AddEntity(entity1);
                });
            Expect("to be able to get all the entities back by type",
                () =>
                {
                    var entities = database.GetAll<TestEntity>().ToArray();
                    entities.Should().Contain(entity);
                    entities.Should().Contain(entity1);
                });
            Expect("that a non existing type will not fail",
                () =>
                {
                    database.GetAll<TestEntityWithReference>().Should().BeEmpty();
                });
        }

        [Fact]
        public void WeCanGetCheckIfAnEntityIsThere()
        {
            var database = TestHelper.DummyDatabase;
            Given("an entity database",
                () =>
                {
                    database = new EntityDatabase();
                });
            var entity = default(TestEntity);
            Where("we add an entity of a specific type",
                () =>
                {
                    entity = new TestEntity();
                    database.AddEntity(entity);
                });
            Expect("that the database has an entity of that type, but not another",
                () =>
                {
                    database.Has<TestEntity>().Should().BeTrue();
                    database.Has<TestEntityWithReference>().Should().BeFalse();
                });
        }

        [Fact]
        public void GettiingANonExistantIdWillThrow()
        {
            var database = TestHelper.DummyDatabase;
            Given("an entity database", () => {
                database = new EntityDatabase();
            });
            Expect("that if we get a non exitant entity by Id it will throw", () => {
                database.Invoking(x => x.GetById<TestEntity>("Test")).Should().Throw<KeyNotFoundException>();
            });
        }

        [Fact]
        public void AddingAnEntityWithoutId()
        {
            var database = TestHelper.DummyDatabase;
            Given("an entity database",
                () =>
                {
                    database = new EntityDatabase();
                });
            var entity = TestHelper.Dummy<TestEntity>();
            Where("we add an entity without an Id",
                () =>
                {
                    entity = new TestEntity() { Id = String.Empty };
                    database.AddEntity(entity);
                });
            Expect("the entity to have gotten an new Id",
                () =>
                {
                    entity.Id.Should().NotBeNullOrEmpty();
                });
        }

        [Fact]
        public void WeCanDeleteAnEntity()
        {
            var database = TestHelper.DummyDatabase;
            var entity = TestHelper.Dummy<TestEntity>();
            Given("an entity database with an entity",
                () =>
                {
                    database = new EntityDatabase();
                    entity = new TestEntity() { Id = "Entity" };
                    database.AddEntity(entity);
                });
            Where("we will delete the entity by adding a remove entity",
                () =>
                {
                    database.AddEntity(new RemoveEntity() { Id = entity.Id });
                });
            Expect("that the database no longer has the entity and the remove entity is not there either",
                () =>
                {
                    database.Has<TestEntity>().Should().BeFalse();
                    database.Has<RemoveEntity>().Should().BeFalse();
                });
        }

        [Fact]
        public void WeCanDeleteMultipleEntities()
        {
            var database = TestHelper.DummyDatabase;
            var entity = TestHelper.Dummy<TestEntity>();
            var entity1 = TestHelper.Dummy<TestEntity>();
            Given("an entity database with an entity",
                () =>
                {
                    database = new EntityDatabase();
                    entity = new TestEntity() { Id = "Entity" };
                    entity1 = new TestEntity() { Id = "Entity1" };
                    database.AddEntity(entity);
                    database.AddEntity(entity1);
                });
            Where("we will delete the entity by adding a remove entity",
                () =>
                {
                    database.AddEntities(new[] { new RemoveEntity() { Id = entity.Id }, new RemoveEntity() { Id = entity1.Id } });
                });
            Expect("that the database no longer has the entities and the remove entity is not there either",
                () =>
                {
                    database.Has<TestEntity>().Should().BeFalse();
                    database.Has<RemoveEntity>().Should().BeFalse();
                });
        }

        [Fact]
        public void DeletingAnEntityThatIsNotThereWillNotFail()
        {
            var database = TestHelper.DummyDatabase;
            var entity = TestHelper.Dummy<TestEntity>();
            Given("an entity database with an entity",
                () =>
                {
                    database = new EntityDatabase();
                    entity = new TestEntity() { Id = "Entity" };
                    database.AddEntity(entity);
                });
            Where("we will delete the entity by adding a remove entity",
                () =>
                {
                    database.AddEntity(new RemoveEntity() { Id = Guid.NewGuid().ToString() });
                });
            Expect("that the database still has the entity and the remove entity is not there either",
                () =>
                {
                    database.Has<TestEntity>().Should().BeTrue();
                    database.Has<RemoveEntity>().Should().BeFalse();
                });
        }

        [Fact]
        public void DeletingAnAndAddingTheSameEntity()
        {
            var database = TestHelper.DummyDatabase;
            var entity = TestHelper.Dummy<TestEntity>();
            Given("an entity database with an entity",
                () =>
                {
                    database = new EntityDatabase();
                    entity = new TestEntity() { Id = "Entity" };
                    database.AddEntity(entity);
                });
            Where("we remove and add the same entity at the same time",
                () =>
                {
                    database.AddEntities(new Entity[] {
                        new RemoveEntity() { Id = entity.Id },
                        entity
                    } );
                });
            Expect("that the database no longer has the entity and the remove entity is not there either",
                () =>
                {
                    database.Has<TestEntity>().Should().BeFalse();
                    database.Has<RemoveEntity>().Should().BeFalse();
                });
        }

        [Fact]
        public void SerializeToJson()
        {
            var database = TestHelper.DummyDatabase;
            var serializer = new JsonSerializer();
            Given("an entity database with entities in it and a primed serializer", () => {
                database = new EntityDatabase();
                var te = new TestEntity();
                var ter = new TestEntityWithReference { Property = te };
                database.AddEntities(new Entity[] { te, ter });
                var context = new SerializerTypeContext();
                serializer.Converters.Add(new EntityConverter(context));
                serializer.Converters.Add(new EntityDatabaseConverter());
            });
            var database1 = TestHelper.DummyDatabase;
            That("we serialize to JSON and back", () => {
                var sb = new StringBuilder();
                using (var tw = new StringWriter(sb))
                {
                    serializer.Serialize(tw, database);
                }
                var t = sb.ToString();
                using (var tr = new JsonTextReader(new StringReader(t)))
                {
                    database1 = serializer.Deserialize<EntityDatabase>(tr);
                }
            });
            Expect("the databases to be the same", () => {
                var missing = database.Entities.Except(database1.Entities, new EntityEqualityComparer()).ToArray();
                missing.Should().BeEmpty();
                database1.Entities.Should().HaveCount(database.Entities.Count());                
            });
        }
    }
}
