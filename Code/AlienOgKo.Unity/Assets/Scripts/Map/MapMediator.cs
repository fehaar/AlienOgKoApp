using PureMVC.Interfaces;
using PureMVC.Patterns.Mediator;

namespace AlienOgKo
{
    public class MapMediator : Mediator
    {
        public new const string NAME = nameof(MapMediator);

        MapView MapView => (MapView)ViewComponent;

        public MapMediator(MapView view) : base(NAME, view) { }

        public override string[] ListNotificationInterests() => System.Array.Empty<string>();

        public override void HandleNotification(INotification notification) { }
    }
}
