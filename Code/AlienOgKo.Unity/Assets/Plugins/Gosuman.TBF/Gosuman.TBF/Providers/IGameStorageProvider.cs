using System.Threading.Tasks;

namespace Gosuman.TBF.Providers
{
    public interface IGameStorageProvider
    {
        Task<TurnBasedSystem?> LoadGame(string gameId);
        Task SaveGame(string gameId, TurnBasedSystem game);
        Task DeleteGame(string gameId);
    }
}
