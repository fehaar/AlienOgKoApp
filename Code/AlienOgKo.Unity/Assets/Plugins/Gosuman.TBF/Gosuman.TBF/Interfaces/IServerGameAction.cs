using Gosuman.EntitySystem;
using Gosuman.EntitySystem.Database;
using System.Collections.Generic;

namespace Gosuman.TBF.Interfaces
{
    public interface IServerGameAction
    {
        /// <summary>
        /// Execute the action, returning all the entites that changed from the action.
        /// </summary>
        /// <param name="database">The current database so we can get relevant data</param>
        /// <param name="changes">The entites that has changed.</param>
        /// <param name= "actionQueue">The queue of actions that should be executed after this action.</param>
        void Execute(IReadOnlyEntityDatabase database, ISet<Entity> changes, List<IServerGameAction> actionQueue);
    }
}