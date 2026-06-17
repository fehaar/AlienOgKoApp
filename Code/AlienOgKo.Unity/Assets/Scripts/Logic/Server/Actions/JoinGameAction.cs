using Gosuman.EntitySystem;
using Gosuman.EntitySystem.Database;
using Gosuman.TBF.Interfaces;
using Gosuman.TBF.Shared.Components;
using Gosuman.TBF.Shared.Entities;
using Gosuman.TBF.Shared.Interfaces;

namespace Gosuman.TBF.Logic.Server.Actions;

public class JoinGameAction : IServerGameAction, IValidatedGameAction
{
    public string PlayerId { get; set; } = string.Empty;

    public void Execute(IReadOnlyEntityDatabase database, ISet<Entity> changes, List<IServerGameAction> actionQueue)
    {
        var startingPlayer = database.GetSingle<Player>();
        startingPlayer.AddComponent(new ActivePlayerComponent());
        changes.Add(startingPlayer);
        Player player = new Player { Id = PlayerId, Name = $"Player {PlayerId.Substring(0, 4)}" };
        changes.Add(player);
    }

    public bool IsValid(IReadOnlyEntityDatabase database)
    {
        return !string.IsNullOrWhiteSpace(PlayerId) && !database.HasId(PlayerId);
    }
}