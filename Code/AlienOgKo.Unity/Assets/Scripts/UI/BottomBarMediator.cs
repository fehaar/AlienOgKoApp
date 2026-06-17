using PureMVC.Interfaces;
using PureMVC.Patterns.Mediator;

namespace AlienOgKo
{
    public class BottomBarMediator : Mediator
    {
        public new const string NAME = nameof(BottomBarMediator);

        BottomBarView BottomBarView => (BottomBarView)ViewComponent;

        public BottomBarMediator(BottomBarView view) : base(NAME, view) { }

        public override string[] ListNotificationInterests() => System.Array.Empty<string>();

        public override void HandleNotification(INotification notification) { }
    }
}
