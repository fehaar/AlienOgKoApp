using Gosuman.EntitySystem.Database;

namespace Gosuman.TBF.Interfaces;

/// <summary>
/// This is an interface for an action trigger.
/// </summary>
public interface IActionTrigger
{
    IEnumerable<Type> TriggerOn { get; }
    IEnumerable<IServerGameAction> Trigger(IServerGameAction action, IReadOnlyEntityDatabase database);
}
