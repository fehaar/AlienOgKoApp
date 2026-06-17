namespace Gosuman.TBF.Test
{

    public class SubmittingAndResolvingPlayerActions : xSpec
    {
        [Fact]
        public void WhenThereIsOnePlayerPlayerActionsWillGetResolvedImmediately()
        {
            var game = default(TurnBasedSystem);
            var resolveHasBeenCalled = false;
            Given("a game in the looping phase",
                () =>
                {
                    game = new TestGame() { OnResolveActions = (a) => resolveHasBeenCalled = true };
                    game.Initialize(false, new Player());
                    game.Start();
                });
            var hasExecuted = false;
            Where("we execute a player action",
                () =>
                {
                    var action = game?.GetAction<TestLoopAction>();
                    if (action != null)
                    {
                        action.customExecute += (_, _, _) =>
                        {
                            hasExecuted = true;
                        };
                        game?.Execute(action);
                    }
                });
            Expect("that it will get executed immediately and the resolve player actions method will get called",
                () =>
                {
                    hasExecuted.Should().BeTrue();
                    resolveHasBeenCalled.Should().BeTrue();
                });
        }
    }
}