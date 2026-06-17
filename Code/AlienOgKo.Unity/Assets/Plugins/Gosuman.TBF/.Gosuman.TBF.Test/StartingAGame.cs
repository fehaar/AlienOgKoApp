namespace Gosuman.TBF.Test
{
    public class StartingAGame : xSpec
    {
        [Fact]
        public void AGameWithoutActionsWillNotExplodeOnStart()
        {
            var game = default(TestGame);
            Given("an initialized test game", () =>
            {
                game = new TestGame();
                game.Initialize(false, new TestEntity(), new Player());
                game.customStartActions = new List<IServerGameAction>();
                game.customLoopActions = new List<IServerGameAction>();
                game.customEndActions = new List<IServerGameAction>();
            });
            Where("we start the game", () =>
            {
            });
            Expect("that we can start it without an exception", () =>
            {
                game!.Invoking(g => g.Start()).Should().NotThrow();
            });
        }
    }
}
