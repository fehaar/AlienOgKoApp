using PureMVC.Interfaces;
using PureMVC.Patterns.Mediator;

namespace AlienOgKo
{
    public class TopBarMediator : Mediator
    {
        public new const string NAME = nameof(TopBarMediator);

        TopBarView TopBarView => (TopBarView)ViewComponent;

        public TopBarMediator(TopBarView view) : base(NAME, view) { }

        public override string[] ListNotificationInterests() => System.Array.Empty<string>();

        public override void HandleNotification(INotification notification) { }
    }
}
