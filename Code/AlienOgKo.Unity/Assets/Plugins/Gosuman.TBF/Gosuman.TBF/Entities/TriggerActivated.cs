using Gosuman.TBF.Interfaces;


namespace Gosuman.TBF.Entities;

public class TriggerActivated : TemporaryEntity
{
	public IActionTrigger Trigger { get; private set; }


	public TriggerActivated(IActionTrigger trigger)
	{
		Trigger = trigger;
	}
}