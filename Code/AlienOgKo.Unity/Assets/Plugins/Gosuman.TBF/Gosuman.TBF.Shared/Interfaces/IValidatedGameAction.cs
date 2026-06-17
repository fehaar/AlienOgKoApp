using Gosuman.EntitySystem.Database;

namespace Gosuman.TBF.Shared.Interfaces
{
    public interface IValidatedGameAction : IClientGameAction
    {
        bool IsValid(IReadOnlyEntityDatabase database);
    }
}