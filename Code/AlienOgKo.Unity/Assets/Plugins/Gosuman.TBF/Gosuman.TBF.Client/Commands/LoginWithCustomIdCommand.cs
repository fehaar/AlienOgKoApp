using Gosuman.TBF.Proxies;
using PureMVC.Interfaces;
using PureMVC.Patterns.Command;

namespace Gosuman.TBF.Commands
{
    public class LoginWithCustomIdCommand : SimpleCommand
    {
        public override void Execute(INotification notification)
        {
            base.Execute(notification);
            var customId = (string)notification.Body;
            var proxy = (ServerProxy)Facade.RetrieveProxy(ServerProxy.NAME);
            proxy.Login(customId).Forget();
        }
    }
}
