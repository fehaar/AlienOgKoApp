namespace Gosuman.TBF.Test;

/// <summary>
/// Action triggers is a functionality where you can hook up a trigger 
/// that can add in extra actions to the action queue after a given action has been executed.
/// </summary>
public class ActionTriggers : xSpec
{
    private class TestTrigger : IActionTrigger
    {
        public TestTrigger(Type[] triggerActions)
        {
            TriggerOn = triggerActions;
        }

        public event Action<IServerGameAction, IReadOnlyEntityDatabase, IList<IServerGameAction>>? OnTrigger;

        public IEnumerable<Type> TriggerOn { get; private set; }

        public IEnumerable<IServerGameAction> Trigger(IServerGameAction action, IReadOnlyEntityDatabase database)
        {
            var actionList = new List<IServerGameAction>();
            OnTrigger?.Invoke(action, database, actionList);
            return actionList;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionTriggerRegistryAvailablitity(bool atrEnabled)
    {
        TestGame testGame = default!;
        Given("a newly created turn based system with action trigger registry enabled", () =>
        {
            testGame = new TestGame();
            testGame.Initialize(atrEnabled);
        });
        Expect("that we have an action trigger registry entity in the database", () =>
        {
            testGame.Database.Has<ActionTriggerRegistry>().Should().Be(atrEnabled);
        });
    }

    [Fact]
    public void ActivatingAnActionTriggerOnStart()
    {
        TestGame testGame = default!;
        IServerGameAction triggeringAction = default!;
        TestTrigger trigger = default!;
        ISet<Entity> changes = default!;
        
        Given("a turn based system", () =>
        {
            testGame = new TestGame();
            testGame.customStartActions = [ new TestStartingAction() ];
            testGame.Initialize(true);
            testGame.Start();
        });
        bool triggerCalled = false;
        IServerGameAction returnedAction = default!;
        Where("we register an action trigger", () =>
        {
            returnedAction = new TestStartingAction();
            triggeringAction = testGame.AvailableActions.First();
            trigger = new TestTrigger(new[] { triggeringAction.GetType() });
            trigger.OnTrigger += (action, db, actionList) => {
                triggerCalled = true;
                action.Should().Be(triggeringAction);
                actionList.Add(returnedAction);
            };
            var atr = testGame.Database.GetSingle<ActionTriggerRegistry>();
            atr.RegisterActionTrigger(trigger);
        });
        Then("executes the action that the trigger is registered for", () =>
        {
            changes = new HashSet<Entity>(testGame.Execute(triggeringAction));
        });
        Expect("that our trigger code will get called", () =>
        {
            triggerCalled.Should().BeTrue();
            testGame.QueuedActions.Should().Contain(returnedAction);
            
            TriggerActivated? triggerSignal = changes.OfType<TriggerActivated>().FirstOrDefault();
            triggerSignal.Should().NotBeNull();
            triggerSignal!.Trigger.Should().Be(trigger);
        });
    }

    [Fact]
    public void ActivatingAnActionTriggerOnLoop()
    {
        TestGame testGame = default!;
        IServerGameAction triggeringAction = default!;
        TestTrigger trigger = default!;
        ISet<Entity> changes = default!;
        
        Given("a turn based system", () =>
        {
            testGame = new TestGame();
            testGame.customLoopActions = [new TestLoopAction()];
            testGame.Initialize(true);
            testGame.Start();
        });
        bool triggerCalled = false;
        IServerGameAction returnedAction = default!;
        Where("we register an action trigger", () =>
        {
            returnedAction = new TestStartingAction();
            triggeringAction = testGame.AvailableActions.First();
            trigger = new TestTrigger(new[] { triggeringAction.GetType() });
            trigger.OnTrigger += (action, db, actionList) => {
                triggerCalled = true;
                action.Should().Be(triggeringAction);
                actionList.Add(returnedAction);
            };
            var atr = testGame.Database.GetSingle<ActionTriggerRegistry>();
            atr.RegisterActionTrigger(trigger);
        });
        Then("executes the action that the trigger is registered for", () =>
        {
            changes = new HashSet<Entity>(testGame.Execute(triggeringAction));
        });
        Expect("that our trigger code will get called", () =>
        {
            triggerCalled.Should().BeTrue();
            testGame.QueuedActions.Should().Contain(returnedAction);
            
            TriggerActivated? triggerSignal = changes.OfType<TriggerActivated>().FirstOrDefault();
            triggerSignal.Should().NotBeNull();
            triggerSignal!.Trigger.Should().Be(trigger);
        });
    }

    [Fact]
    public void ActivatingAnActionTriggerOnEnd()
    {
        TestGame testGame = default!;
        IServerGameAction triggeringAction = default!;
        TestTrigger trigger = default!;
        ISet<Entity> changes = default!;
        
        Given("a turn based system", () =>
        {
            testGame = new TestGame();
            testGame.customEndActions = [new TestEndingAction()];
            testGame.Initialize(true);
            testGame.Start();
        });
        bool triggerCalled = false;
        IServerGameAction returnedAction = default!;
        Where("we register an action trigger", () =>
        {
            returnedAction = new TestStartingAction();
            triggeringAction = testGame.AvailableActions.First();
            trigger = new TestTrigger(new[] { triggeringAction.GetType() });
            trigger.OnTrigger += (action, db, actionList) => {
                triggerCalled = true;
                action.Should().Be(triggeringAction);
                actionList.Add(returnedAction);
            };
            var atr = testGame.Database.GetSingle<ActionTriggerRegistry>();
            atr.RegisterActionTrigger(trigger);
        });
        Then("executes the action that the trigger is registered for", () =>
        {
            changes = new HashSet<Entity>(testGame.Execute(triggeringAction));
        });
        Expect("that our trigger code will get called", () =>
        {
            triggerCalled.Should().BeTrue();
            testGame.QueuedActions.Should().Contain(returnedAction);
            
            TriggerActivated? triggerSignal = changes.OfType<TriggerActivated>().FirstOrDefault();
            triggerSignal.Should().NotBeNull();
            triggerSignal!.Trigger.Should().Be(trigger);
        });
    }

    [Fact]
    public void RegisteringOnTheWrongType()
    {
        TestGame testGame = default!;
        IServerGameAction triggeringAction = default!;
        Given("a turn based system", () =>
        {
            testGame = new TestGame();
            testGame.customLoopActions = [new TestLoopAction()];
            testGame.Initialize(true);
            testGame.Start();
        });
        bool triggerCalled = false;
        Where("we register an action trigger", () =>
        {
            triggeringAction = testGame.AvailableActions.First();
            var trigger = new TestTrigger(new[] { typeof(TestStartingAction) });
            trigger.OnTrigger += (action, db, actionList) => {
                triggerCalled = true;
            };
            var atr = testGame.Database.GetSingle<ActionTriggerRegistry>();
            atr.RegisterActionTrigger(trigger);
        });
        Then("executes the action that the trigger is registered for", () =>
        {
            testGame.Execute(triggeringAction);
        });
        Expect("that our trigger code will get called", () =>
        {
            triggerCalled.Should().BeFalse();
            testGame.QueuedActions.Should().BeEmpty();
        });
    }

    [Fact]
    public void RegisteringForMultipleTypes()
    {
        TestGame testGame = default!;
        Given("a turn based system", () =>
        {
            testGame = new TestGame();
            testGame.customStartActions = [new TestStartingAction()];
            testGame.customLoopActions = [new TestLoopAction()];
            testGame.customEndActions = [new TestEndingAction()];
            testGame.Initialize(true);
            testGame.Start();
        });
        HashSet<Type> triggeredOn = new();
        Where("we register an action trigger", () =>
        {
            var trigger = new TestTrigger([typeof(TestStartingAction), typeof(TestEndingAction)]);
            trigger.OnTrigger += (action, db, actionList) => {
                triggeredOn.Add(action.GetType());
            };
            var atr = testGame.Database.GetSingle<ActionTriggerRegistry>();
            atr.RegisterActionTrigger(trigger);
        });
        Then("executes the action that the trigger is registered for", () =>
        {
            while (testGame.AvailableActions.Any())
            {
                var action = testGame.AvailableActions.First();
                if (action is TestLoopAction)
                {
                    testGame.customLoopActions = [];
                }
                testGame.Execute(action);
            }
        });
        Expect("that our trigger code will get called", () =>
        {
            triggeredOn.Should().Contain(typeof(TestStartingAction));
            triggeredOn.Should().Contain(typeof(TestEndingAction));
            triggeredOn.Should().NotContain(typeof(TestLoopAction));
        });
    }

    [Fact]
    public void UnregisteringATrigger()
    {
        TestGame testGame = default!;
        bool triggerCalled = false;
        TestTrigger trigger = default!;
        IServerGameAction triggeringAction = default!;
        Given("a turn based system with a registered action trigger", () =>
        {
            testGame = new TestGame();
            testGame.customLoopActions = [new TestLoopAction()];
            testGame.Initialize(true);
            testGame.Start();
            triggeringAction = testGame.AvailableActions.First();
            trigger = new TestTrigger(new[] { triggeringAction.GetType() });
            trigger.OnTrigger += (action, db, actionList) => {
                triggerCalled = true;
            };
            var atr = testGame.Database.GetSingle<ActionTriggerRegistry>();
            atr.RegisterActionTrigger(trigger);
        });
        Where("we unregister the trigger", () =>
        {
            var atr = testGame.Database.GetSingle<ActionTriggerRegistry>();
            atr.UnregisterActionTrigger(trigger);
        });
        Then("executes the action that the trigger is registered for", () =>
        {
            testGame.Execute(triggeringAction);
        });
        Expect("that our trigger code will not get called", () =>
        {
            triggerCalled.Should().BeFalse();
        });
    }

    [Fact]
    public void NullChecksOnRegisterCalls()
    {
        TestGame testGame = default!;
        Given("a turn based system", () =>
        {
            testGame = new TestGame();
            testGame.customLoopActions = [new TestLoopAction()];
            testGame.Initialize(true);
            testGame.Start();
        });
        Expect("that we will get exceptions when register/unregister argument is null", () =>
        {
            var atr = testGame.Database.GetSingle<ActionTriggerRegistry>();
            atr.Invoking(x => x.RegisterActionTrigger(null!))
                .Should().Throw<ArgumentNullException>();
            atr.Invoking(x => x.UnregisterActionTrigger(null!))
                .Should().Throw<ArgumentNullException>();
        });
    }

    [Fact]
    public void ExceptionPropagationInTheTrigger()
    {
        TestGame testGame = default!;
        IServerGameAction startAction = default!;
        IServerGameAction loopAction = default!;
        Given("a turn based system", () =>
        {
            startAction = new TestStartingAction();
            loopAction = new TestLoopAction();
            testGame = new TestGame();
            testGame.customStartActions = [startAction];
            testGame.customLoopActions = [ loopAction ];
            testGame.Initialize(true);
            testGame.Start();
        });
        Where("we register an action trigger that throws an exception", () =>
        {
            var trigger = new TestTrigger(new[] { typeof(TestLoopAction) });
            trigger.OnTrigger += (action, db, actionList) => {
                throw new Exception("Test exception");
            };
            var atr = testGame.Database.GetSingle<ActionTriggerRegistry>();
            atr.RegisterActionTrigger(trigger);
        });
        Then("executing an action that triggers will throw the exception", () =>
        {
            testGame.Invoking(g => g.Execute(startAction)).Should().NotThrow();
            testGame.Invoking(g => g.Execute(loopAction)).Should().Throw<Exception>()
                .WithMessage("Test exception");
        });
    }
}
