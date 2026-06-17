using Gosuman.TBF.Shared.Interfaces;
using System.Threading.Tasks;

namespace Gosuman.TBF.Interfaces
{
    public interface IGameServerClient
    {
        Task GameStateUpdated(IGameStateIncrement increment);
    }
}
