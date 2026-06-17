using Gosuman.TBF.Proxies;
using PureMVC.Interfaces;
using PureMVC.Patterns.Command;

namespace Gosuman.TBF.Commands
{
    public class LoginTicketReceivedCommand : SimpleCommand
    {
        public override void Execute(INotification notification)
        {
            base.Execute(notification);

            // We will just translate this directly to a log in
            var ticket = (string)notification.Body;
            SendNotification(PlayerProxy.Notifications.PlayerLoggedIn, ticket);
        }
    }
}
