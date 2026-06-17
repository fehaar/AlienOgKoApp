using Gosuman.TBF.Proxies;
using PureMVC.Interfaces;
using PureMVC.Patterns.Command;

namespace Gosuman.TBF.Commands
{
    public class PlayerLoggedInCommand : SimpleCommand
    {
        public override void Execute(INotification notification)
        {
            base.Execute(notification);

            var ticketOrUser = (string)notification.Body;
            // Add the PlayFabProxy with the ticket so we can communicate with PlayFab
            Facade.RegisterProxy(new PlayFabProxy(ticketOrUser));
        }
    }
}
