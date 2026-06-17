using Gosuman.EntitySystem;
using Gosuman.TBF.Shared.Entities;
using Gosuman.TBF.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gosuman.TBF.Shared
{
    public class GameStateIncrement : IGameStateIncrement
    {
        public IEnumerable<Entity> Entities { get; set; } = Array.Empty<Entity>();
        public IEnumerable<IClientGameAction> AvailableActions { get; set; } = Array.Empty<IClientGameAction>();

        public GameStateIncrement()
        {
        }

        public string GameId => GetSingle<GameState>()?.Id ?? string.Empty;

        public static GameStateIncrement StartingState()
        {
            return new GameStateIncrement()
            {
                Entities = Array.Empty<Entity>()
            };
        }

        public bool TryGet<T>(out T? entity) where T : Entity
        {
            entity = GetSingle<T>();
            return entity != null;
        }
        public T? GetSingle<T>() where T : Entity
        {
            return Entities.SingleOrDefault(e => e is T) as T;
        }

        public bool Has<T>() where T : Entity
        {
            return Entities.Any(e => e is T);
        }

        public bool HasAction<T>() where T : IClientGameAction
        {
            if (AvailableActions != null)
            {
                return AvailableActions.Any(a => a is T);
            }
            return false;
        }

        public T? GetAction<T>() where T : IClientGameAction
            => (T?)AvailableActions.FirstOrDefault(a => a is T);

        public bool TryGetAction<T>(out T? gameAction) where T : IClientGameAction
        {
            gameAction = GetAction<T>();
            return gameAction != null;
        }
    }
}