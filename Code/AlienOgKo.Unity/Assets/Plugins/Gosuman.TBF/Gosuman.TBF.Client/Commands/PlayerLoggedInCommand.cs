using Cysharp.Threading.Tasks;
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

            var customId = (string)notification.Body;
            // Authenticate against the configured identity backend (registered at startup).
            var authProxy = (AuthProxy)Facade.RetrieveProxy(AuthProxy.NAME);
            authProxy.LogIn(customId).Forget();
        }
    }
}
