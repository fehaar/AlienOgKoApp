using Gosuman.EntitySystem;
using Gosuman.TBF.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gosuman.TBF.Shared
{
    public class PlayerGameStateIncrement: IGameStateIncrement
    {
        public PlayerGameStateIncrement(string playerId, IGameStateIncrement increment)
        {
            this.playerId = playerId ?? throw new ArgumentNullException(nameof(playerId));
            this.increment = increment ?? throw new ArgumentNullException(nameof(increment));
        }

        private string playerId;
        private IGameStateIncrement increment;

        public IEnumerable<Entity> Entities => increment.Entities.Where(e => e is not OwnedEntity owned || owned.PlayerId == playerId);
        public IEnumerable<IClientGameAction> AvailableActions => increment.AvailableActions.Where(a => a is not IPlayerAction playerAction || playerAction.Player?.Id == playerId) ;
    }
}