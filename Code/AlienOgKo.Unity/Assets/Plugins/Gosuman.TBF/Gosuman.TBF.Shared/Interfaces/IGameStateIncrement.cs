using Gosuman.EntitySystem;
using System.Collections.Generic;

namespace Gosuman.TBF.Shared.Interfaces
{
    public interface IGameStateIncrement
    {
        IEnumerable<IClientGameAction> AvailableActions { get; }
        IEnumerable<Entity> Entities { get; }
    }
}