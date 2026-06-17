
namespace Gosuman.TBF.Test
{
    internal class TestActionBase : IServerGameAction
    {
        public void Execute(IReadOnlyEntityDatabase database, ISet<Entity> changes, List<IServerGameAction> actionQueue) { }
    }
    internal class TestStartingAction : TestActionBase { }
    internal class TestEndingAction : TestActionBase { }
    
    internal class TestLoopAction : IPlayerAction, IServerGameAction, IClientGameAction
    {
        public Player Player { get; set; } = Player.DummyPlayer;

        public Action<IReadOnlyEntityDatabase, ISet<Entity>, List<IServerGameAction>>? customExecute { get; set; }
        
        public void Execute(IReadOnlyEntityDatabase database, ISet<Entity> changes, List<IServerGameAction> actionQueue)
        {
            customExecute?.Invoke(database, changes, actionQueue);
        }
    }

    internal class TestTimedAction : ITimedAction
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        public Func<bool> Cancel = () => false;

        public bool CancelOnAction(IServerGameAction action)
        {
            return Cancel();
        }

        public void Execute(IReadOnlyEntityDatabase database, ISet<Entity> changes, List<IServerGameAction> actionQueue)
        {
            // No-op for testing
        }
    }

    internal class TestGame : TurnBasedSystem
    {
        public static TestGame DummyGame = new TestGame();


        public IEnumerable<IServerGameAction>? customStartActions = null;
        public IEnumerable<IServerGameAction>? customLoopActions = null;
        public IEnumerable<IServerGameAction>? customEndActions = null;
        public Action<GameState>? OnResolveActions;


        protected override IEnumerable<IServerGameAction> GetStartActions() => customStartActions ?? DefaultStartActions();
        protected override IEnumerable<IServerGameAction> GetLoopActions() => customLoopActions ?? DefaultLoopActions();
        protected override IEnumerable<IServerGameAction> GetEndActions() => customEndActions ?? DefaultEndActions();


        private IEnumerable<IServerGameAction> DefaultStartActions()
        {
            yield break;
        }
        
        private IEnumerable<IServerGameAction> DefaultLoopActions()
        {
            yield return new TestLoopAction();
        }

        private IEnumerable<IServerGameAction> DefaultEndActions()
        {
            yield break;
        }
        

        protected override void PlayerActionsResolved(GameState state, ISet<Entity> changedEntities)
        {
            base.PlayerActionsResolved(state, changedEntities);
            OnResolveActions?.Invoke(state);
        }

        internal void ForcePhase(GamePhases phase)
        {
            SetGamePhase(phase);
        }
    }
}
