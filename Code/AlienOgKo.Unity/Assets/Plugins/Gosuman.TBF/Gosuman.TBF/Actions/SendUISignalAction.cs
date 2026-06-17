using Gosuman.EntitySystem.Database;
using Gosuman.TBF.Interfaces;


namespace Gosuman.TBF.Actions
{
	public class SendUISignalAction : IServerGameAction
	{
		public readonly TemporaryEntity Entity;
	
	
		public SendUISignalAction(TemporaryEntity entity)
		{
			Entity = entity;
		}
	
	
		public void Execute(IReadOnlyEntityDatabase database, ISet<Entity> changes, List<IServerGameAction> actionQueue)
		{
			changes.Add(Entity);
		}
	}	
}