using Xunit;

namespace Gosuman.EntitySystem.Test.Serialization
{
    public class FixingEntityReferences : xSpec
    {
        [Fact]
        public void FixSimpleReferences()
        {
            var db = TestHelper.DummyDatabase;
            var entity1 = default(TestEntity);
            var entity2 = default(TestEntityWithReference);
            Given("an entity database with two entites where one has a dummy reference", () =>
            {
                db = new EntityDatabase();
                entity1 = new TestEntity();
                entity2 = new TestEntityWithReference();
                entity2.Field = new TestEntity { Id = entity1.Id };
                entity2.Property = new TestEntity { Id = entity1.Id };
                db.AddEntity(entity1);
                db.AddEntity(entity2);
            });
            Where("we fix the references", () =>
            {
                EntityReferenceFixer.Fix(db);
            });
            Expect("that we now have the correct reference", () =>
            {
                entity2!.Field.Should().Be(entity1);
                entity2!.Property.Should().Be(entity1);
            });
        }

        [Fact]
        public void FixArrayReferences()
        {
            var db = TestHelper.DummyDatabase;
            var entity1 = default(TestEntityWithEnumerableReference);
            Given("an entity database with an entity that has dummy array references", () =>
            {
                entity1 = new TestEntityWithEnumerableReference();
                var entity2 = new TestEntity(); 
                var entity3 = new TestEntity(); 
                var entity4 = new TestEntity();
                entity1.Entities = new TestEntity[] { 
                    new TestEntity { Id = entity2.Id }, 
                    new TestEntity { Id = entity3.Id }, 
                    new TestEntity { Id = entity4.Id }, 
                };
                entity1.EntitiesField = new List<TestEntity> {
                    new TestEntity { Id = entity2.Id },
                    new TestEntity { Id = entity3.Id },
                    new TestEntity { Id = entity4.Id },
                };
                db.AddEntities(new Entity[] { entity1, entity2, entity3, entity4 });
            });
            Where("we fix the references", () =>
            {
                EntityReferenceFixer.Fix(db);
            });
            Expect("that the references are now the ones from the database", () =>
            {
                entity1!.Entities.Should().HaveCount(3);
                foreach (var entity in entity1.Entities)
                {
                    db.GetById(entity.Id).Should().Be(entity);
                }
                foreach (var entity in entity1.EntitiesField)
                {
                    db.GetById(entity.Id).Should().Be(entity);
                }
            });
        }

        [Fact]
        public void FixComponentReferences()
        {
            var db = TestHelper.DummyDatabase;
            var entity1 = default(TestEntity);
            var entity2 = default(TestEntity);
            Given("an entity database with two entites where one has a component with a reference", () =>
            {
                db = new EntityDatabase();
                entity1 = new TestEntity();
                entity2 = new TestEntity();
                entity2.AddComponent(new TestComponentWithReference { Entity = new TestEntity { Id = entity1.Id } });
                db.AddEntity(entity1);
                db.AddEntity(entity2);
            });
            Where("we fix the references", () =>
            {
                EntityReferenceFixer.Fix(db);
            });
            Expect("that we now have the correct reference", () =>
            {
                entity2!.GetComponent<TestComponentWithReference>().Entity.Should().Be(entity1);
            });
        }

        [Fact]
        public void WeCanFixOtherReferencesUsingADatabase()
        {
            var db = TestHelper.DummyDatabase;
            var entity1 = default(TestEntityWithReference);
            var entity2 = default(TestEntityWithEnumerableReference);
            Given("a database with an entity and some entities with dummy references to that entity", () =>
            {
                db = new EntityDatabase();
                db.AddEntities(new Entity[] { new TestEntity { Id = "Test1" }, new TestEntity { Id = "Test2" } });
                entity1 = new TestEntityWithReference { Property = new TestEntity { Id = "Test1" } };
                entity2 = new TestEntityWithEnumerableReference { Entities = new TestEntity[] { new TestEntity { Id = "Test1" }, new TestEntity { Id = "Test2" } } };
            });
            Where("we fix up the references for the entities", () =>
            {
                EntityReferenceFixer.Fix(new Entity[] { entity1!, entity2! }, db);
            });
            Expect("that their references have been fixed", () =>
            {
                entity1!.Property.Should().Be(db.GetById("Test1"));
                entity2!.Entities.Should().HaveCount(2);
                entity2.Entities.Should().Contain(db.GetById<TestEntity>("Test1"));
                entity2.Entities.Should().Contain(db.GetById<TestEntity>("Test2"));
            });
        }

        [Fact]
        public void WeCanFixOtherReferencesInCompoenentsUsingADatabase()
        {
            var db = TestHelper.DummyDatabase;
            var entity1 = default(TestEntity);
            var entity2 = default(TestEntity);
            Given("a database with an entity and some entities with dummy references to that entity", () =>
            {
                db = new EntityDatabase();
                db.AddEntities(new Entity[] { new TestEntity { Id = "Test1" }, new TestEntity { Id = "Test2" } });
                entity1 = new TestEntity();
                entity1.AddComponent(new TestComponentWithReference { Entity = new TestEntity { Id = "Test1" } });
                entity2 = new TestEntity();
                entity2.AddComponent(new TestComponentWithEnumerableReferences { Entities = new TestEntity[] { new TestEntity { Id = "Test1" }, new TestEntity { Id = "Test2" } } });
            });
            Where("we fix up the references for the entities", () =>
            {
                EntityReferenceFixer.Fix(new Entity[] { entity1!, entity2! }, db);
            });
            Expect("that their references have been fixed", () =>
            {
                var c1 = entity1!.GetComponent<TestComponentWithReference>();
                c1.Entity.Should().Be(db.GetById("Test1"));
                var c2 = entity2!.GetComponent<TestComponentWithEnumerableReferences>();
                c2.Entities.Should().HaveCount(2);
                c2.Entities.Should().Contain(db.GetById<TestEntity>("Test1"));
                c2.Entities.Should().Contain(db.GetById<TestEntity>("Test2"));
            });
        }

        [Fact]
        public void OtherReferencesWithBrokenInterdependencies()
        {
            var entityList = default(Entity[]);
            var testEntity1 = default(TestEntity);
            var testEntity2 = default(TestEntity);
            var entity1 = default(TestEntityWithReference);
            var entity2 = default(TestEntityWithEnumerableReference);
            Given("a list of entities with broken interdependencies", () =>
            {
                testEntity1 = new TestEntity { Id = "Test1" };
                testEntity2 = new TestEntity { Id = "Test2" };
                entity1 = new TestEntityWithReference { Property = new TestEntity { Id = "Test1" } };
                entity2 = new TestEntityWithEnumerableReference { Entities = new TestEntity[] { new TestEntity { Id = "Test1" }, new TestEntity { Id = "Test2" } } };
                entityList = new Entity[] { testEntity1, testEntity2, entity1, entity2 };
            });
            Where("we fix up the references for the entities", () =>
            {
                EntityReferenceFixer.Fix(entityList!, new EntityDatabase());
            });
            Expect("that their references have been fixed", () =>
            {
                entity1!.Property.Should().Be(testEntity1);
                entity2!.Entities.Should().HaveCount(2);
                entity2.Entities.Should().Contain(testEntity1!);
                entity2.Entities.Should().Contain(testEntity2!);
            });
        }

        [Fact]
        public void HiddenReferencesInAnEntityList()
        {
            var entityList = default(Entity[]);
            var testEntity1 = default(TestEntity);
            var entity1 = default(TestEntityWithHiddenReferenceAndNoInterface);
            Given("a list of entities where one implements the helper interface", () =>
            {
                testEntity1 = new TestEntity { Id = "Test1" };
                entity1 = new TestEntityWithHiddenReferenceAndNoInterface { Reference = new HiddenReference { Entity = new TestEntity { Id = "Test1" } } };
                entityList = new Entity[] { testEntity1, entity1 };
            });
            Where("we fix up the references for the entities", () =>
            {
                EntityReferenceFixer.Fix(entityList!, new EntityDatabase());
            });
            Expect("", () =>
            {
                entity1!.Reference.Entity.Should().NotBe(testEntity1);
            });
        }

        [Fact]
        public void UsingTheIEntityReferenceFixableInterface()
        {
            var entityList = default(Entity[]);
            var testEntity1 = default(TestEntity);
            var entity1 = default(TestEntityWithHiddenReference);
            Given("a list of entities where one implements the helper interface", () =>
            {
                testEntity1 = new TestEntity { Id = "Test1" };
                entity1 = new TestEntityWithHiddenReference { Reference = new HiddenReference { Entity = new TestEntity { Id = "Test1" } } };
                entityList = new Entity[] { testEntity1, entity1 };
            });
            Where("we fix up the references for the entities", () =>
            {
                EntityReferenceFixer.Fix(entityList!, new EntityDatabase());
            });
            Expect("", () =>
            {
                entity1!.InterfaceCalled.Should().BeTrue();
                entity1.Reference.Entity.Should().Be(testEntity1);
            });
        }

        [Fact]
        public void HiddenComponentReferenceInAList()
        {
            var entityList = default(Entity[]);
            var testEntity1 = default(TestEntity);
            var entity1 = default(TestEntity);
            Given("a database with entities where one has a component with a hidden reference", () =>
            {
                testEntity1 = new TestEntity { Id = "Test1" };
                entity1 = new TestEntity();
                entity1.AddComponent(new TestComponentWithHiddenReferenceAndNoInterface { Reference = new HiddenReference { Entity = new TestEntity { Id = "Test1" } } });
                entityList = new Entity[] { testEntity1, entity1 };
            });
            Where("we fix up the references for the entities", () =>
            {
                EntityReferenceFixer.Fix(entityList!, new EntityDatabase());
            });
            Expect("", () =>
            {
                var c = entity1!.GetComponent<TestComponentWithHiddenReferenceAndNoInterface>();
                c.Reference.Entity.Should().NotBe(testEntity1);
            });
        }

        [Fact]
        public void FixingReferencesOnAHiddenComponentReferenceInAList()
        {
            var entityList = default(Entity[]);
            var testEntity1 = default(TestEntity);
            var entity1 = default(TestEntity);
            Given("a database with entities where one has a component with a hidden reference", () =>
            {
                testEntity1 = new TestEntity { Id = "Test1" };
                entity1 = new TestEntity();
                entity1.AddComponent(new TestComponentWithHiddenReference { Reference = new HiddenReference { Entity = new TestEntity { Id = "Test1" } } });
                entityList = new Entity[] { testEntity1, entity1 };
            });
            Where("we fix up the references for the entities", () =>
            {
                EntityReferenceFixer.Fix(entityList!, new EntityDatabase());
            });
            Expect("", () =>
            {
                var c = entity1!.GetComponent<TestComponentWithHiddenReference>();
                c.InterfaceCalled.Should().BeTrue();
                c.Reference.Entity.Should().Be(testEntity1);
            });
        }

        [Fact]
        public void HiddenReferencesInADatabase()
        {
            var db = TestHelper.DummyDatabase;
            var testEntity1 = default(TestEntity);
            var entity1 = default(TestEntityWithHiddenReferenceAndNoInterface);
            Given("a list of entities where one implements the helper interface", () =>
            {
                testEntity1 = new TestEntity { Id = "Test1" };
                entity1 = new TestEntityWithHiddenReferenceAndNoInterface { Reference = new HiddenReference { Entity = new TestEntity { Id = "Test1" } } };
                db = new EntityDatabase();
                db.AddEntities(new Entity[] { testEntity1, entity1 });
            });
            Where("we fix up the references for the entities", () =>
            {
                EntityReferenceFixer.Fix(db);
            });
            Expect("", () =>
            {
                entity1!.Reference.Entity.Should().NotBe(testEntity1);
            });
        }

        [Fact]
        public void UsingTheIEntityReferenceFixableInterfaceInADatabase()
        {
            var db = TestHelper.DummyDatabase;
            var testEntity1 = default(TestEntity);
            var entity1 = default(TestEntityWithHiddenReference);
            Given("a database with entities where one implements the helper interface", () =>
            {
                testEntity1 = new TestEntity { Id = "Test1" };
                entity1 = new TestEntityWithHiddenReference { Reference = new HiddenReference { Entity = new TestEntity { Id = "Test1" } } };
                db = new EntityDatabase();
                db.AddEntities(new Entity[] { testEntity1, entity1 });
            });
            Where("we fix up the references for the entities", () =>
            {
                EntityReferenceFixer.Fix(db);
            });
            Expect("", () =>
            {
                entity1!.InterfaceCalled.Should().BeTrue();
                entity1.Reference.Entity.Should().Be(testEntity1);
            });
        }

        [Fact]
        public void HiddenComponentReferenceInADatabase()
        {
            var db = TestHelper.DummyDatabase;
            var testEntity1 = default(TestEntity);
            var entity1 = default(TestEntity);
            Given("a database with entities where one has a component with a hidden reference", () =>
            {
                testEntity1 = new TestEntity { Id = "Test1" };
                entity1 = new TestEntity();
                entity1.AddComponent(new TestComponentWithHiddenReferenceAndNoInterface { Reference = new HiddenReference { Entity = new TestEntity { Id = "Test1" } } });
                db = new EntityDatabase();
                db.AddEntities(new Entity[] { testEntity1, entity1 });
            });
            Where("we fix up the references for the entities", () =>
            {
                EntityReferenceFixer.Fix(db);
            });
            Expect("", () =>
            {
                var c = entity1!.GetComponent<TestComponentWithHiddenReferenceAndNoInterface>();
                c.Reference.Entity.Should().NotBe(testEntity1);
            });
        }

        [Fact]
        public void FixingReferencesOnAHiddenComponentReferenceInADatabase()
        {
            var db = TestHelper.DummyDatabase;
            var testEntity1 = default(TestEntity);
            var entity1 = default(TestEntity);
            Given("a database with entities where one has a component with a hidden reference", () =>
            {
                testEntity1 = new TestEntity { Id = "Test1" };
                entity1 = new TestEntity();
                entity1.AddComponent(new TestComponentWithHiddenReference { Reference = new HiddenReference { Entity = new TestEntity { Id = "Test1" } } });
                db = new EntityDatabase();
                db.AddEntities(new Entity[] { testEntity1, entity1 });
            });
            Where("we fix up the references for the entities", () =>
            {
                EntityReferenceFixer.Fix(db);
            });
            Expect("", () =>
            {
                var c = entity1!.GetComponent<TestComponentWithHiddenReference>();
                c.InterfaceCalled.Should().BeTrue();
                c.Reference.Entity.Should().Be(testEntity1);
            });
        }
    }
}
