using Cysharp.Threading.Tasks;
using Gosuman.TBF.Proxies;
using PureMVC.Interfaces;
using PureMVC.Patterns.Command;

namespace Gosuman.TBF.Commands
{
    public class LoggedInCommand : SimpleCommand
    {
        public override void Execute(INotification notification)
        {
            base.Execute(notification);
            UnityEngine.Debug.Log("Authenticated");
            var data = (AuthProxy.LoggedInData)notification.Body;
            var playerProxy = (PlayerProxy)Facade.RetrieveProxy(PlayerProxy.NAME);
            playerProxy.PlayerId = data.PlayerId;
            var gameServer = (ServerProxy)Facade.RetrieveProxy(ServerProxy.NAME);
            gameServer.ConnectToHub(data.Token).Forget();
        }
    }
}
