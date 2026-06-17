using Gosuman.TBF.Shared.Entities;
using Gosuman.TBF.Shared.Interfaces;

namespace Gosuman.TBF.Logic.Actions;

public class ClientGameAction : IClientGameAction, IPlayerAction
{
    public Player? Player { get; set; }

    public Action<IClientGameAction>? ClientExecute { get; set; }
    public void ExecuteOnClient()
    {
        ClientExecute?.Invoke(this);
    }
}