namespace Gosuman.TBF.Test;

public class TimedActionTests : xSpec
{
    [Fact]
    public void AddingATimedActionToTheLoop()
    {
        TestGame tbs = default!;
        TestLoopAction action1 = default!;
        Given("A tbs with an avaialable action that queues up a timed action", () =>
        {
            tbs = new TestGame();
            action1 = new TestLoopAction();

            tbs.customLoopActions = LoopActions();

            IEnumerable<IServerGameAction> LoopActions()
            {
                yield return action1;
                yield return new TestTimedAction();
            }
        });
        Where("We start the tbs", () =>
        {
            tbs.Initialize();
            tbs.Start();
        });
        Expect("that the timed action is in available actions", () =>
        {
            tbs.AvailableActions.Should().Contain(a => a is TestTimedAction);
        });
    }

    [Fact]
    public void QueingATimedAction()
    {
        TestGame tbs = default!;
        TestLoopAction action1 = default!;
        Given("A tbs with an avaialable action that queues up a timed action", () =>
        {
            tbs = new TestGame();
            action1 = new TestLoopAction();
            action1.customExecute += (_, _, queue) =>
            {
                queue.Add(new TestTimedAction());
            };

            tbs.customLoopActions = LoopActions();

            IEnumerable<IServerGameAction> LoopActions()
            {
                yield return action1;
            }
        });
        Where("We start the tbs and execute the action", () =>
        {
            tbs.Initialize();
            tbs.Start();
            tbs.Execute(action1);
        });
        Expect("that the timed action is in available actions", () =>
        {
            tbs.AvailableActions.Should().Contain(a => a is TestTimedAction);
        });
    }

    [Fact]
    public void QueuedTimedActionsWillPersist()
    {
        TestGame tbs = default!;
        TestLoopAction action1 = default!;
        Given("A tbs after an action that has queued up a timed action", () =>
        {
            tbs = new TestGame();
            action1 = new TestLoopAction();
            action1.customExecute += (_, _, queue) =>
            {
                queue.Add(new TestTimedAction());
            };

            bool firstRun = true;
            tbs.customLoopActions = LoopActions();
            tbs.Initialize();
            tbs.Start();
            tbs.Execute(action1);

            IEnumerable<IServerGameAction> LoopActions()
            {
                if (firstRun)
                {
                    firstRun = false;
                    yield return action1;
                }
                else
                {
                    yield return new TestLoopAction();
                }
            }
        });
        Where("We execute the other action", () =>
        {
            var action = tbs.AvailableActions.First();
            tbs.Execute(action);
        });
        Expect("that the timed action is still in available actions", () =>
        {
            tbs.AvailableActions.Should().Contain(a => a is TestTimedAction);
        });
    }

    [Fact]
    public void InvalidatingATimedAction()
    {
        TestGame tbs = default!;
        TestLoopAction action1 = default!;
        Given("A tbs after an action that has queued up a timed action", () =>
        {
            tbs = new TestGame();
            action1 = new TestLoopAction();
            action1.customExecute += (_, _, queue) =>
            {
                queue.Add(new TestTimedAction() { Cancel = () => true }); 
            };

            bool firstRun = true;
            tbs.customLoopActions = LoopActions();
            tbs.Initialize();
            tbs.Start();
            tbs.Execute(action1);

            IEnumerable<IServerGameAction> LoopActions()
            {
                if (firstRun)
                {
                    firstRun = false;
                    yield return action1;
                }
                else
                {
                    yield return new TestLoopAction();
                }
            }
        });
        Where("We execute the other action that cancels the timed action", () =>
        {
            var action = tbs.AvailableActions.First();
            tbs.Execute(action);
        });
        Expect("that the timed action is no longer in available actions", () =>
        {
            tbs.AvailableActions.Should().NotContain(a => a is TestTimedAction);
        });
    }

    [Fact]
    public void ExecutingATimedAction()
    {
        TestGame tbs = default!;
        TestLoopAction action1 = default!;
        Given("A tbs after an action that has queued up a timed action", () =>
        {
            tbs = new TestGame();
            action1 = new TestLoopAction();
            action1.customExecute += (_, _, queue) =>
            {
                queue.Add(new TestTimedAction() { Cancel = () => true });
            };

            bool firstRun = true;
            tbs.customLoopActions = LoopActions();
            tbs.Initialize();
            tbs.Start();
            tbs.Execute(action1);

            IEnumerable<IServerGameAction> LoopActions()
            {
                if (firstRun)
                {
                    firstRun = false;
                    yield return action1;
                }
                else
                {
                    yield return new TestLoopAction();
                }
            }
        });
        Where("We execute the timed action", () =>
        {
            var action = tbs.AvailableActions.First(a => a is ITimedAction);
            tbs.Execute(action);
        });
        Expect("that the timed action is no longer in available actions", () =>
        {
            tbs.AvailableActions.Should().NotContain(a => a is TestTimedAction);
        });
    }
}