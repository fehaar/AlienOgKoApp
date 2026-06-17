using Gosuman.EntitySystem;
using Gosuman.TBF.Interfaces;

namespace Gosuman.TBF.Entities
{
    /// <summary>
    /// This is a registry for action triggers that lives in the database as a singleton and is used only internally to
    /// keept track on action triggers and make them available to actions that never touch the TurnBasedSystem.
    /// 
    /// TODO: #86c3a3115: We need some special considerations for how to handle serialization/deserialization of this class.
    /// </summary>
    public class ActionTriggerRegistry : Entity
    {
        internal ActionTriggerRegistry()
        {
        }

        private readonly Dictionary<Type, ISet<IActionTrigger>> actionTriggers = new();

        public void RegisterActionTrigger(IActionTrigger trigger)
        {
            if (trigger == null)
            {
                throw new ArgumentNullException(nameof(trigger));
            }
            foreach (var actionType in trigger.TriggerOn)
            {
                if (!actionTriggers.ContainsKey(actionType))
                {
                    actionTriggers[actionType] = new HashSet<IActionTrigger>();
                }
                actionTriggers[actionType].Add(trigger);
            }
        }

        public void UnregisterActionTrigger(IActionTrigger trigger)
        {
            if (trigger == null)
            {
                throw new ArgumentNullException(nameof(trigger));
            }
            foreach (var actionType in trigger.TriggerOn)
            {
                if (actionTriggers.ContainsKey(actionType))
                {
                    actionTriggers[actionType].Remove(trigger);
                    if (actionTriggers[actionType].Count == 0)
                    {
                        actionTriggers.Remove(actionType);
                    }
                }
            }
        }

        internal IEnumerable<IActionTrigger> GetTriggers(Type type)
        {
            for (Type? t = type; t != null; t = t.BaseType)
            {
                if (actionTriggers.TryGetValue(t, out ISet<IActionTrigger>? triggers))
                {
                    return triggers;
                }
            }
            return Enumerable.Empty<IActionTrigger>();
        }
    }
}
