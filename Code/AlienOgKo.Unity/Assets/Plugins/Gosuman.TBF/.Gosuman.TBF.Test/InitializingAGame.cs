namespace Gosuman.TBF.Test
{
    public class InitializingAGame : xSpec
    {
        [Fact]
        public void WhenAGameIsInitialized()
        {
            var game = TestGame.DummyGame;
            Given("a game",
                () =>
                {
                    game = new TestGame();
                });
            var entity = default(Entity);
            That("we initialize with an entity",
                () =>
                {
                    entity = new TestEntity();
                    game.Initialize(false, entity, new Player());
                    game.Start();
                });
            Expect("that the game will now have that entity",
                () =>
                {
                    game.Database.GetSingle<TestEntity>().Should().Be(entity);
                });
            And("a game state",
                () =>
                {
                    game.GameState.Should().NotBeNull();
                });
            And("an available action", () =>
                {
                    game.AvailableActions.Any().Should().BeTrue();
                });
        }


        [Fact]
        public void WhenInitializingARunningGame()
        {
            var entities = default(IEnumerable<Entity>);
            Given("the entities from a running game",
                () =>
                {
                    var game = new TestGame();
                    game.Initialize(false, new TestEntity(), new Player());
                    entities = game.Database.Entities;
                });
            var game = default(TurnBasedSystem);
            That("we use to initialize a new game as we would when deserializing",
                () =>
                {
                    game = new TestGame();
                    if (entities != null)
                    {
                        game?.Initialize(entities);
                    }
                });
            Expect("that the game will now have that entity",
                () =>
                {
                    game?.Database.GetSingle<TestEntity>().Should().NotBe(null);
                });
            And("a game state", () =>
                {
                    game?.GameState.Should().NotBeNull();
                });
        }

    }
}
