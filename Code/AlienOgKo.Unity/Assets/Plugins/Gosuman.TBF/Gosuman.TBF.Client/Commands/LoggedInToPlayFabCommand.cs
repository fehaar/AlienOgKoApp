using Gosuman.TBF.Proxies;
using PureMVC.Interfaces;
using PureMVC.Patterns.Command;

namespace Gosuman.TBF.Commands
{
    public class LoggedInToPlayFabCommand : SimpleCommand
    {
        public override void Execute(INotification notification)
        {
            base.Execute(notification);
            UnityEngine.Debug.Log($"PlayFab logged in");
            // Try connecting to the game hub
            var data = (PlayFabProxy.LoggedInData)notification.Body;
            var playerProxy = (PlayerProxy)Facade.RetrieveProxy(PlayerProxy.NAME);
            playerProxy.PlayerId = data.PlayerId;
            var gameServer = (ServerProxy)Facade.RetrieveProxy(ServerProxy.NAME);
            gameServer.ConnectToHub(data.Ticket).Forget();
        }
    }
}
