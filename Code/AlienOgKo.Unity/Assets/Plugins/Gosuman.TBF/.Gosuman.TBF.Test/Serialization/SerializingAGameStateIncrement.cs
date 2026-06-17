using Gosuman.TBF.Shared;
using System.Text;

namespace Gosuman.TBF.Test.Serialization
{
    public class SerializingAGameStateIncrement : xSpec
    {
        [Fact]
        public void ABasicIncrement()
        {
            var game = TestHelper.DummyGame;
            var player = TestHelper.Dummy<Player>();
            Given("a game where we have an action and an entity", () =>
            {
                game = new TestGame();
                player = new Player();
                game.Initialize(false, player);
                game.Start();
            });
            var sb = new StringBuilder();
            Where("we create a game state increment and serialize it", () =>
            {
                var db = new EntityDatabase();
                var serializer = TestHelper.GetJsonSerializer(db);
                using (var tr = new StringWriter(sb))
                {
                    var increment = game.ToIncrement();
                    serializer.Serialize(tr, increment);
                }
            });
            Expect("that we have the entity and action when we deserialize the increment", () =>
            {
                var db = new EntityDatabase();
                var serializer = TestHelper.GetJsonSerializer(db);
                var gsi = default(GameStateIncrement);
                using (var tr = new JsonTextReader(new StringReader(sb.ToString())))
                {
                    gsi = serializer.Deserialize<GameStateIncrement>(tr)!;
                }
                gsi.Should().NotBeNull();
                gsi.AvailableActions.Should().Contain(a => a is TestLoopAction);
                gsi.Entities.Should().Contain(e => e is Player && e.Id == player.Id);
            });
        }

        [Fact]
        public void AnIncrementWithReferences()
        {
            var game = TestHelper.DummyGame;
            var player = TestHelper.Dummy<Player>();
            Given("a game where we have an action and an entity with references", () =>
            {
                game = new TestGame();
                var player = new Player();
                var entity = new TestEntityWithReference();
                entity.Ref = new TestEntity() { Id = "Test", Name = "Foo" };
                game.Initialize(false, player, entity, entity.Ref);
            });
            var sb = new StringBuilder();
            Where("we create a game state increment and serialize it", () =>
            {
                var db = new EntityDatabase();
                var serializer = TestHelper.GetJsonSerializer(db);
                using (var tr = new StringWriter(sb))
                {
                    var increment = game.ToIncrement();
                    serializer.Serialize(tr, increment);
                }
            });
            Expect("that the reference is a dummy when we serialize it back", () =>
            {
                var db = new EntityDatabase();
                var serializer = TestHelper.GetJsonSerializer(db);
                var gsi = default(GameStateIncrement);
                using (var tr = new JsonTextReader(new StringReader(sb.ToString())))
                {
                    gsi = serializer.Deserialize<GameStateIncrement>(tr)!;
                }
                gsi.Should().NotBeNull();
                var entity = gsi.Entities.OfType<TestEntityWithReference>().FirstOrDefault();
                entity.Should().NotBeNull();
                entity!.Ref.Should().NotBeNull();
                entity!.Ref!.Id.Should().Be("Test");
                entity!.Ref!.Name.Should().NotBe("Foo");
            });
        }

        [Fact]
        public void AnIncrementWithDuplicateActions()
        {
            var game = TestHelper.DummyGame;
            var player = TestHelper.Dummy<Player>();
            Given("a game where we have two similar actions and an entity", () =>
            {
                game = new TestGame();
                game.customLoopActions = LoopingActions();
                
                IEnumerable<IServerGameAction> LoopingActions()
                {
                    yield return new TestLoopAction();
                    yield return new TestLoopAction();
                }
                
                player = new Player();
                game.Initialize(false, player);
                game.Start();
            });
            var sb = new StringBuilder();
            Where("we create a game state increment and serialize it", () =>
            {
                var db = new EntityDatabase();
                var serializer = TestHelper.GetJsonSerializer(db);
                using (var tr = new StringWriter(sb))
                {
                    var increment = game.ToIncrement();
                    serializer.Serialize(tr, increment);
                }
            });
            Expect("that we have the entity and action when we deserialize the increment", () =>
            {
                var db = new EntityDatabase();
                var serializer = TestHelper.GetJsonSerializer(db);
                var gsi = default(GameStateIncrement);
                using (var tr = new JsonTextReader(new StringReader(sb.ToString())))
                {
                    gsi = serializer.Deserialize<GameStateIncrement>(tr)!;
                }
                gsi.Should().NotBeNull();
                gsi.AvailableActions.Count(a => a is TestLoopAction).Should().Be(2);
                gsi.Entities.Should().Contain(e => e is Player && e.Id == player.Id);
            });
        }
    }
}
