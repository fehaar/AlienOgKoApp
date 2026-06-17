using System;

namespace Gosuman.TBF.Test;

public class ActionQueueTests : xSpec
{
	[Fact]
	public void AddingAnActionToTheActionQueue_ShouldPresentOnlyThatActionAsTheNextAction()
	{
		TestGame tbs = default!;
		TestLoopAction action1 = default!;
		TestLoopAction queuedAction = default!;
		
		Given("A tbs with 1 avaialable action.", () =>
		{
			tbs = new TestGame();
			action1 = new TestLoopAction();

			tbs.customLoopActions = LoopActions();
			IEnumerable<IServerGameAction> LoopActions()
			{
				yield return action1;
			}
		});
		And("that action queues up a new action", () =>
		{
			queuedAction = new TestLoopAction();
			
			action1.customExecute += (_, _, queue) =>
			{
				queue.Add(queuedAction);
			};
		});
		Where("we start the tbs and execute action 1", () =>
		{
			tbs.Initialize();
			tbs.Start();
			tbs.Execute(action1);
		});
		Expect("the available action to just be the queued action", () =>
		{
			tbs.AvailableActions.Count().Should().Be(1);
			tbs.AvailableActions.First().Should().Be(queuedAction);
		});
	}


	[Fact]
	public void ExecutingAQueuedAction_ShouldPresentTheDefaultActions()
	{
		TestGame tbs = default!;
		TestLoopAction action1 = default!;
		TestLoopAction queuedAction = default!;
		
		Given("A tbs with 1 avaialable action.", () =>
		{
			tbs = new TestGame();
			action1 = new TestLoopAction();

			tbs.customLoopActions = LoopActions();
			IEnumerable<IServerGameAction> LoopActions()
			{
				yield return action1;
			}
		});
		And("that action queues up a new action", () =>
		{
			queuedAction = new TestLoopAction();
			
			action1.customExecute += (_, _, queue) =>
			{
				queue.Add(queuedAction);
			};
		});
		Where("we start the tbs and execute action 1", () =>
		{
			tbs.Initialize();
			tbs.Start();
			tbs.Execute(action1);
		});
		And("We execute the queued action", () =>
		{
			tbs.Execute(tbs.AvailableActions.First());
		});
		Expect("the available action to once more be the default action", () =>
		{
			tbs.AvailableActions.Count().Should().Be(1);
			tbs.AvailableActions.First().Should().Be(action1);
		});
	}


	[Fact]
	public void AddingTwoQueuedActions_ShouldPresentOneAfterTheOther()
	{
		TestGame tbs = default!;
		TestLoopAction action1 = default!;
		TestLoopAction queuedAction1 = default!;
		TestLoopAction queuedAction2 = default!;
		
		
		Given("A tbs with 1 avaialable action.", () =>
		{
			tbs = new TestGame();
			action1 = new TestLoopAction();
			
			tbs.customLoopActions = LoopActions();
			IEnumerable<IServerGameAction> LoopActions()
			{
				yield return action1;
			}
		});
		And("that action queues up two new action", () =>
		{
			queuedAction1 = new TestLoopAction();
			queuedAction2 = new TestLoopAction();
			
			action1.customExecute += (_, _, queue) =>
			{
				queue.Add(queuedAction1);
				queue.Add(queuedAction2);
			};
		});
		Where("we start the tbs and execute action 1", () =>
		{
			tbs.Initialize();
			tbs.Start();
			tbs.Execute(action1);
		});
		Expect("the only available action to the the first queued action", () =>
		{
			tbs.AvailableActions.Count().Should().Be(1);
			tbs.AvailableActions.First().Should().Be(queuedAction1);
		});
		Then("we execute the first queued action", () =>
		{
			tbs.Execute(tbs.AvailableActions.First());
		});
		Expect("the only available action to be the second queued action", () =>
		{
			tbs.AvailableActions.Count().Should().Be(1);
			tbs.AvailableActions.First().Should().Be(queuedAction2);
		});
	}


	[Fact]
	public void NewlyQueuedActions_ShouldBePresentedBeforePreviouslyQueuedActions()
	{
		TestGame tbs = default!;
		TestLoopAction action1 = default!;
		TestLoopAction queuedAction1 = default!;
		TestLoopAction queuedAction2 = default!;
		TestLoopAction queuedAction3 = default!;


		Given("A tbs with 1 avaialable action.", () =>
		{
			tbs = new TestGame();
			action1 = new TestLoopAction();

			tbs.customLoopActions = LoopActions();

			IEnumerable<IServerGameAction> LoopActions()
			{
				yield return action1;
			}
		});
		And("that action queues up two new action", () =>
		{
			queuedAction1 = new TestLoopAction();
			queuedAction2 = new TestLoopAction();

			action1.customExecute += (_, _, queue) =>
			{
				queue.Add(queuedAction1);
				queue.Add(queuedAction2);
			};
		});
		And("that the first queued action queues up a third action", () =>
		{
			queuedAction3 = new TestLoopAction();

			queuedAction1.customExecute += (_, _, queue) => { queue.Add(queuedAction3); };
		});
		Where("we start the tbs and execute action 1", () =>
		{
			tbs.Initialize();
			tbs.Start();
			tbs.Execute(action1);
		});
		Expect("the only available action to the the first queued action", () =>
		{
			tbs.AvailableActions.Count().Should().Be(1);
			tbs.AvailableActions.First().Should().Be(queuedAction1);
		});
		Then("we execute the first queued action", () => { tbs.Execute(tbs.AvailableActions.First()); });
		Expect("the only available action to be the third queued action", () =>
		{
			tbs.AvailableActions.Count().Should().Be(1);
			tbs.AvailableActions.First().Should().Be(queuedAction3);
		});
		Then("we execute the third queued action", () => { tbs.Execute(tbs.AvailableActions.First()); });
		Expect("the only available action to be the second queued action", () =>
		{
			tbs.AvailableActions.Count().Should().Be(1);
			tbs.AvailableActions.First().Should().Be(queuedAction2);
		});
	}
}