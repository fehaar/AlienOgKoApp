namespace Gosuman.TBF.Test
{
    public class PhasesTests : xSpec
    {
        [Fact]
        public void PhaseProgressesTo_Starting()
        {
            var game = default(TestGame);
            Given("a test game with a start action", () =>
            {
                game = new TestGame();
                game.customStartActions = StartActions();
                IEnumerable<IServerGameAction> StartActions()
                {
                    yield return new TestStartingAction();
                }
                
                game.Initialize(false, new Player());
                game.Start();
            });
            Expect("the phase progresses to the starting phase", () =>
            {
                game!.Phase.Should().Be(GamePhases.Starting);
            });
            And("the TestStartingAction is available", () =>
            {
                game?.AvailableActions.First().Should().BeOfType<TestStartingAction>();
            });
        }

        [Fact]
        public void StartingExecutesActions_Linearly()
        {
            var game = default(TestGame);
            TestStartingAction action1 = new TestStartingAction();
            TestStartingAction action2 = new TestStartingAction();
            
            Given("a test game with 2 start actions", () =>
            {
                game = new TestGame();
                game.customStartActions = StartActions();
                IEnumerable<IServerGameAction> StartActions()
                {
                    yield return action1;
                    yield return action2;
                }
                
                game.Initialize(false, new Player());
                game.Start();
            });
            Expect("the phase progresses to the starting phase", () =>
            {
                game!.Phase.Should().Be(GamePhases.Starting);
            });
            And("only the first starting action to be available", () =>
            {
                game!.AvailableActions.Count().Should().Be(1);
                game!.AvailableActions.First().Should().Be(action1);
            });
            
            
            Then("execute the action", () =>
            {
                game!.Execute(game.AvailableActions.First());
            });
            Expect("only the second action to be available", () =>
            {
                game!.AvailableActions.Count().Should().Be(1);
                game!.AvailableActions.First().Should().Be(action2);
            });
            
            
            Then("execute the action", () =>
            {
                game!.Execute(game.AvailableActions.First());
            });
            Expect("the phase progresses to the looping phase", () =>
            {
                game!.Phase.Should().Be(GamePhases.Looping);
            });
        }

        
        [Fact]
        public void PhaseProgressesTo_Looping()
        {
            var game = default(TestGame);
            Given("a test game with a start action", () =>
            {
                game = new TestGame();
                game.customStartActions = StartActions();
                IEnumerable<IServerGameAction> StartActions()
                {
                    yield return new TestEndingAction();
                }
                
                game.Initialize(false, new Player());
                game.Start();
            });
            Where("we execute the start action", () =>
            {
                game!.Execute(game.AvailableActions.First());
            });
            Expect("the phase progresses to the loop phase", () =>
            {
                game!.Phase.Should().Be(GamePhases.Looping);
            });
            And("the TestLoopAction is available", () =>
            {
                game!.AvailableActions.First().Should().BeOfType<TestLoopAction>();
            });
        }

        [Fact]
        public void LoopingExecutesActions_Continuously()
        {
            var game = default(TestGame);
            TestLoopAction action1 = new TestLoopAction();
            TestLoopAction action2 = new TestLoopAction();
            
            Given("a test game with 2 loop actions", () =>
            {
                game = new TestGame();
                game.customLoopActions = LoopActions();
                IEnumerable<IServerGameAction> LoopActions()
                {
                    yield return action1;
                    yield return action2;
                }
                
                game.Initialize(false, new Player());
                game.Start();
            });
            Expect("the phase progresses to the looping phase", () =>
            {
                game!.Phase.Should().Be(GamePhases.Looping);
            });
            And("both actions should be available", () =>
            {
                game!.AvailableActions.Count().Should().Be(2);
            });
            
            
            Then("execute an action", () =>
            {
                game!.Execute(game.AvailableActions.First());
            });
            Expect("both actions should still be available", () =>
            {
                game!.AvailableActions.Count().Should().Be(2);
            });
            
            
            Then("execute an action that empties the loop actions", () =>
            {
                game!.customLoopActions = NoLoopActions();
                game!.Execute(game.AvailableActions.First());

                IEnumerable<IServerGameAction> NoLoopActions()
                {
                    yield break;
                }
            });
            Expect("the phase progresses to the ended phase", () =>
            {
                game!.Phase.Should().Be(GamePhases.Ended);
            });
        }

        
        [Fact]
        public void PhaseProgressesTo_Ending()
        {
            var game = default(TestGame);
            Given("a test game with an end action", () =>
            {
                game = new TestGame();
                game.customEndActions = EndActions();
                IEnumerable<IServerGameAction> EndActions()
                {
                    yield return new TestEndingAction();
                }
                
                game.Initialize(false, new Player());
                game.Start();
            });
            Where("we run out of loop actions", () =>
            {
                IEnumerable<IServerGameAction> NoActions()
                {
                    yield break;
                }
                game!.customLoopActions = NoActions();
                game!.Execute(game.AvailableActions.First());
            });
            Expect("the phase progresses to the ending phase", () =>
            {
                game!.Phase.Should().Be(GamePhases.Ending);
            });
            And("the TestEndingAction is available", () =>
            {
                game!.AvailableActions.First().Should().BeOfType<TestEndingAction>();
            });
        }
        
        [Fact]
        public void EndingExecutesActions_Linearly()
        {
            var game = default(TestGame);
            TestEndingAction action1 = new TestEndingAction();
            TestEndingAction action2 = new TestEndingAction();
            
            Given("a test game with 2 start actions", () =>
            {
                game = new TestGame();
                game.customLoopActions = NoLoopActions();
                game.customEndActions = EndActions();

                IEnumerable<IServerGameAction> NoLoopActions()
                {
                    yield break;
                }
                
                IEnumerable<IServerGameAction> EndActions()
                {
                    yield return action1;
                    yield return action2;
                }
                
                game.Initialize(false, new Player());
                game.Start();
            });
            Expect("the phase progresses to the ending phase", () =>
            {
                game!.Phase.Should().Be(GamePhases.Ending);
            });
            And("only the first starting action to be available", () =>
            {
                game!.AvailableActions.Count().Should().Be(1);
                game!.AvailableActions.First().Should().Be(action1);
            });
            
            
            Then("execute the action", () =>
            {
                game!.Execute(game.AvailableActions.First());
            });
            Expect("only the second action to be available", () =>
            {
                game!.AvailableActions.Count().Should().Be(1);
                game!.AvailableActions.First().Should().Be(action2);
            });
            
            
            Then("execute the action", () =>
            {
                game!.Execute(game.AvailableActions.First());
            });
            Expect("the phase progresses to the ended phase", () =>
            {
                game!.Phase.Should().Be(GamePhases.Ended);
            });
        }
    }
}