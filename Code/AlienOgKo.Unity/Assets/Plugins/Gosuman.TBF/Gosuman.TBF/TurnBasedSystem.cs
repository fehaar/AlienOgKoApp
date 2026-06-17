using Gosuman.EntitySystem.Database;
using Gosuman.TBF.Entities;
using Gosuman.TBF.Interfaces;
using Gosuman.TBF.Shared;
using Gosuman.TBF.Shared.Entities;
using Gosuman.TBF.Shared.Interfaces;

namespace Gosuman.TBF
{
    public abstract class TurnBasedSystem
    {
        public IEnumerable<IServerGameAction> AvailableActions => availableActions;
        public IEnumerable<IServerGameAction> QueuedActions => availableActionStack;
        public bool HasAction<T>() where T : IServerGameAction => availableActions.Any(a => a is T);
        public T GetAction<T>() where T : IServerGameAction => (T)availableActions.First(a => a is T);

        public IReadOnlyEntityDatabase Database => database;

        private readonly List<ITimedAction> QueuedTimedActions = new();

        public GameState GameState => Database.GetSingle<GameState>();
        public GamePhases Phase => GameState?.Phase ?? GamePhases.NotStarted;

        private readonly EntityDatabase database;
        private readonly HashSet<Entity> changedEntities = new();
        private readonly List<IServerGameAction> actionQueue = new();
        private readonly Random random = new();

        private readonly List<IServerGameAction> availableActions = new();
        private readonly Stack<IServerGameAction> availableActionStack = new();
        private IEnumerator<IServerGameAction>? queuedActionsEnumerator;

        /// <summary>
        /// Make sure that the action exists in available actions before executing it.
        /// If not set, we will only validate that an action of the same type exists.
        /// For the server this should be false as we have serialized actions.
        /// </summary>
        protected bool StrictActionExistsValidation = true;

        public TurnBasedSystem()
        {
            this.database = new();
        }

        public TurnBasedSystem(EntityDatabase db)
        {
            this.database = db;
            UpdateAvailableActions();
        }

        public void Initialize(bool addActionTriggerRegistry = false, params Entity[] entities)
        {
            Initialize(entities, addActionTriggerRegistry);
        }

        public void Initialize(IEnumerable<Entity>? entities, bool addActionTriggerRegistry = false)
        {
            if (!ValidateStartEntities(entities))
            {
                throw new InvalidOperationException($"The provided start entities are not valid for this game.");
            }
            if (entities != null)
            {
                database.AddEntities(entities);
            }
            if (!database.Has<GameState>())
            {
                // This is a new game, create a game state
                database.AddEntity(new GameState());
            }
            if (addActionTriggerRegistry)
            {
                database.AddEntity(new ActionTriggerRegistry());
            }
        }

        protected virtual bool ValidateStartEntities(IEnumerable<Entity>? entities)
        {
            return true;
        }

        public void Start()
        {
            SetGamePhase(GamePhases.Starting);

            while (Phase != GamePhases.Ended && !UpdateAvailableActions())
            {
                // If we have no available actions, increment the phase.
                SetGamePhase(Phase + 1);
                changedEntities.Add(GameState);
            }
        }


        protected void SetGamePhase(GamePhases newPhase)
        {
            switch (newPhase)
            {
                case GamePhases.Starting:
                    {
                        queuedActionsEnumerator?.Dispose();
                        queuedActionsEnumerator = GetStartActions().GetEnumerator();
                    }
                    break;


                case GamePhases.Ending:
                    {
                        queuedActionsEnumerator?.Dispose();
                        queuedActionsEnumerator = GetEndActions().GetEnumerator();
                    }
                    break;


                case GamePhases.NotStarted:
                case GamePhases.Looping:
                case GamePhases.Ended:
                    {
                        queuedActionsEnumerator?.Dispose();
                        queuedActionsEnumerator = null;
                    }
                    break;


                default:
                    {
                        throw new NotImplementedException();
                    }
            }

            GameState.Phase = newPhase;
        }


        protected bool UpdateAvailableActions()
        {
            availableActions.Clear();

            if (availableActionStack.Count > 0)
            {
                var topAction = availableActionStack.Peek();
                while (topAction is ITimedAction timedAction && timedAction.Timeout >= TimeSpan.Zero)
                {
                    // If the top action is a timed action that has timed out, we remove it from the stack and do not add it to the available actions.
                    QueuedTimedActions.Add(timedAction);
                    availableActionStack.Pop();
                    if (availableActionStack.Count == 0)
                    {
                        topAction = null;
                        break;
                    }
                    topAction = availableActionStack.Peek();
                }
                if (topAction != null)
                {
                    availableActions.Add(topAction);
                }
            }

            if (!availableActions.Any())
            {
                switch (Phase)
                {
                    case GamePhases.Starting:
                    case GamePhases.Ending:
                        if (queuedActionsEnumerator!.MoveNext())
                        {
                            availableActions.Add(queuedActionsEnumerator.Current);
                        }
                        break;


                    case GamePhases.Looping:
                        availableActions.AddRange(GetLoopActions());
                        availableActions.AddRange(QueuedTimedActions);
                        break;


                    default:
                    case GamePhases.NotStarted:
                    case GamePhases.Ended:
                        throw new InvalidOperationException($"Phase {Phase} is not a valid action phase");
                }
            }

            return availableActions.Count > 0;
        }


        protected abstract IEnumerable<IServerGameAction> GetStartActions();
        protected abstract IEnumerable<IServerGameAction> GetLoopActions();
        protected abstract IEnumerable<IServerGameAction> GetEndActions();


        public IEnumerable<Entity> Execute(IServerGameAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException($"Cannot execute a null action.");
            }
            if (Phase is GamePhases.NotStarted or GamePhases.Ended)
            {
                throw new InvalidOperationException($"{nameof(TurnBasedSystem)} cannot execute an action while it is in phase {Phase}.");
            }
            if (action is ITimedAction timedAction && QueuedTimedActions.Contains(timedAction))
            {
                QueuedTimedActions.Remove(timedAction);
            }
            for (int i = QueuedTimedActions.Count() - 1; i >= 0; i--)
            {
                if (QueuedTimedActions[i].CancelOnAction(action))
                {
                    QueuedTimedActions.RemoveAt(i);
                }
            }
            if (StrictActionExistsValidation)
            {
                if (!AvailableActions.Contains(action))
                {
                    throw new InvalidOperationException($"Cannot execute action {action.GetType().Name} when it is not available.");
                }
            }
            else
            {
                var actionType = action.GetType();
                if (!AvailableActions.Any(a => a.GetType() == actionType))
                {
                    // Instead of throwing an exception here we will just return no changes
                    return Array.Empty<Entity>();
                }
            }
            if (action is IValidatedGameAction validatedGameAction && !validatedGameAction.IsValid(database))
            {
                throw new InvalidOperationException($"Cannot execute the invalid action {action.GetType().Name}.");
            }

            // We've accepted the action, so clear our list of available actions to prevent a double execution.
            availableActions.Clear();
            changedEntities.Clear();
            actionQueue.Clear();

            // If we're executing the top action on the stack, pop it from the stack.
            if (availableActionStack.Count > 0 && availableActionStack.Peek() == action)
            {
                availableActionStack.Pop();
            }


            if (action is IRandomGameAction)
            {
                ((IRandomGameAction)action).Random = random;
            }

            action.Execute(Database, changedEntities, actionQueue);

            if (action is IPlayerAction)
            {
                var state = database.GetSingle<GameState>();
                if (state != null)
                {
                    PlayerActionsResolved(state, changedEntities);
                }
            }


            // We add the changed entities to the database before updating available actions or running action triggers,
            // as the new actions may depend on the changes.
            database.AddEntities(changedEntities);


            // We check if the action queued up any new actions and add them to the available action stack in reverse order,
            // so that the first action is at the top of the stack.
            if (actionQueue.Count > 0)
            {
                for (int i = actionQueue.Count - 1; i >= 0; i--)
                {
                    availableActionStack.Push(actionQueue[i]);
                }
            }


            // Check if we have any action triggers for the action type
            if (database.TryGetSingle(out ActionTriggerRegistry? atr))
            {
                actionQueue.Clear();
                foreach (var trigger in atr.GetTriggers(action.GetType()))
                {
                    int currentActions = actionQueue.Count;

                    actionQueue.AddRange(trigger.Trigger(action, database));

                    // If any new actions were queued up, we signal that a trigger was activated.
                    if (actionQueue.Count > currentActions)
                    {
                        TriggerActivated triggerSignal = new TriggerActivated(trigger);
                        changedEntities.Add(triggerSignal);
                    }
                }

                // We check if any trigger queued up any new actions and add them to the available action stack in reverse order,
                // so that the first action is at the top of the stack.
                if (actionQueue.Count > 0)
                {
                    for (int i = actionQueue.Count - 1; i >= 0; i--)
                    {
                        availableActionStack.Push(actionQueue[i]);
                    }
                }
            }


            while (Phase != GamePhases.Ended && !UpdateAvailableActions())
            {
                // If we have no available actions, increment the phase.
                SetGamePhase(Phase + 1);

                // Add the changed gamestate to both the database and the list of changed entities.
                changedEntities.Add(GameState);
                database.AddEntity(GameState);
            }

            return changedEntities;
        }


        protected virtual void PlayerActionsResolved(GameState state, ISet<Entity> changedEntities)
        {
        }

        public GameStateIncrement ToIncrement()
        {
            return new GameStateIncrement()
            {
                AvailableActions = AvailableActions.OfType<IClientGameAction>().ToArray(),
                Entities = database.Entities.Where(e => e is not ServerEntity).ToArray()
            };
        }

        public ServerGameStateIncrement ToServerIncrement(Player[] players)
        {
            return new ServerGameStateIncrement()
            {
                Players = players,
                AvailableActions = AvailableActions.OfType<IClientGameAction>().ToArray(),
                TimedActions = AvailableActions.OfType<ITimedAction>().ToArray(),
                Entities = database.Entities.Where(e => e is not ServerEntity).ToArray()
            };
        }

        public ServerGameStateIncrement ToServerIncrement(Player[] players, IEnumerable<Entity> entities)
        {
            return new ServerGameStateIncrement()
            {
                Players = players,
                AvailableActions = AvailableActions.Where(a => a is IClientGameAction).Cast<IClientGameAction>().ToArray(),
                TimedActions = AvailableActions.OfType<ITimedAction>().ToArray(),
                Entities = entities.Where(e => e is not ServerEntity).ToArray()
            };
        }
    }
}