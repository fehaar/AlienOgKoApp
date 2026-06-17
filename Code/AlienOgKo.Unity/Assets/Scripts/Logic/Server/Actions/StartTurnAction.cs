using Gosuman.EntitySystem;
using Gosuman.EntitySystem.Database;
using Gosuman.TBF.Logic.Entities;
using Gosuman.TBF.Interfaces;
using Gosuman.TBF.Shared.Components;
using Gosuman.TBF.Shared.Entities;

namespace Gosuman.TBF.Logic.Server.Actions;

public class StartTurnAction : Logic.Actions.StartTurnAction, IServerGameAction
{
    public void Execute(IReadOnlyEntityDatabase database, ISet<Entity> changes, List<IServerGameAction> actionQueue)
    {
        var activePlayer = database.GetSingle<Player>(p => p.HasComponent<ActivePlayerComponent>());
        changes.Add(new ActiveTurn { ActivePlayer = activePlayer });
        var gameSettings = database.GetSingle<GameSettings>();
    }
}
