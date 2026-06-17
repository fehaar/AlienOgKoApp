using Xunit;
namespace Gosuman.EntitySystem.Test.Serialization
{
    public class TemporaryEntities : xSpec
    {
        [Fact]
        public void ATemporaryEntityWillNotGetAddedToTheDatabase()
        {
            var database = TestHelper.DummyDatabase;
            Given("an entity database",
                () =>
                {
                    database = new EntityDatabase();
                });
            Where("we add some temporary entities",
                () =>
                {
                    database.AddEntity(new TemporaryEntity());
                    database.AddEntities(new Entity[] {
                        new TestEntity(),
                        new TemporaryEntity(),
                        new TestEntity(),
                        new TemporaryEntity(),
                    });
                });
            Expect("that we will only have the regular entities in the database",
                () =>
                {
                    database.Entities.Should().NotBeNullOrEmpty();
                    database.Has<TemporaryEntity>().Should().BeFalse();
                    database.Entities.Should().HaveCount(2);
                });
        }
    }
}
