using Gosuman.TBF.Proxies;
using PureMVC.Interfaces;
using PureMVC.Patterns.Command;
using UnityEngine;

namespace AlienOgKo
{
    // AlienOgKo has no game server of its own — unlike Gosuman.TBF.Commands.LoggedInCommand,
    // this records the authenticated player id only; it never connects to a hub.
    public class PlayerAuthenticatedCommand : SimpleCommand
    {
        public override void Execute(INotification notification)
        {
            base.Execute(notification);
            Debug.Log("Authenticated with identity backend");
            var data = (AuthProxy.LoggedInData)notification.Body;
            var playerProxy = (PlayerProxy)Facade.RetrieveProxy(PlayerProxy.NAME);
            playerProxy.PlayerId = data.PlayerId;
        }
    }
}
