using Gosuman.TBF.Interfaces;
using Gosuman.TBF.Shared;
using Gosuman.TBF.Shared.Entities;
using System;

namespace Gosuman.TBF
{
    public class ServerGameStateIncrement : GameStateIncrement
    {
        public Player[] Players { get; set; } = Array.Empty<Player>();
        public ITimedAction[] TimedActions = Array.Empty<ITimedAction>();
    }
}