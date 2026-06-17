using Gosuman.EntitySystem;
using Gosuman.EntitySystem.Database;
using Gosuman.TBF.Logic.Entities;
using Gosuman.TBF.Interfaces;
using Gosuman.TBF.Shared.Components;
using Gosuman.TBF.Shared.Entities;

namespace Gosuman.TBF.Logic.Server.Actions;

public class SwitchTurnAction : IServerGameAction
{
    public void Execute(IReadOnlyEntityDatabase database, ISet<Entity> changes, List<IServerGameAction> actionQueue)
    {
        // Remove the active turn to signal that the turn has ended. The StartTurnAction will then create a new active turn for the next player.
        var activeTurn = database.GetSingle<ActiveTurn>();        
        changes.Add(new RemoveEntity(activeTurn));

        foreach (var player in database.GetAll<Player>())
        {
            // We switch the active player by toggling the ActivePlayerComponent on the players. It only works for two players.
            // TODO: Make a more robust system for switching active players that works for more than two players.
            if (player.HasComponent<ActivePlayerComponent>())
            {
                player.RemoveComponent<ActivePlayerComponent>();
            }
            else
            {
                player.AddComponent(new ActivePlayerComponent());
            }
            changes.Add(player);
        }
    }
}