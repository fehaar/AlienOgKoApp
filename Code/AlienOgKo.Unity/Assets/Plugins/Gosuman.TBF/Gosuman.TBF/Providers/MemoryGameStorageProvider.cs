using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gosuman.TBF.Providers
{
    public class MemoryGameStorageProvider : IGameStorageProvider
    {
        private readonly IDictionary<string, TurnBasedSystem> games = new Dictionary<string, TurnBasedSystem>();

        public Task DeleteGame(string gameId)
        {
            games.Remove(gameId);
            return Task.CompletedTask;
        }

        public Task<TurnBasedSystem?> LoadGame(string gameId)
        {
            games.TryGetValue(gameId, out TurnBasedSystem? game);
            return Task.FromResult<TurnBasedSystem?>(game);
        }

        public Task SaveGame(string gameId, TurnBasedSystem game)
        {
            games[gameId] = game;
            return Task.CompletedTask;
        }
    }
}
