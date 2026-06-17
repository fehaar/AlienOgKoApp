using PlayFab;
using PureMVC.Patterns.Proxy;
using UnityEngine;

namespace Gosuman.TBF.Proxies
{
    /// <summary>
    /// This proxy handles communication with PlayFab.
    /// </summary>
    public class PlayFabProxy : Proxy
    {
        public static new string NAME = "PlayFabProxy";

        public static class Notifications
        {
            public const string LoggedInToPlayFab = "PlayFabProxy.LoggedInToPlayFab";
            public const string LogInWithCustomId = "PlayFabProxy.LogInWithCustomId";
        }

        public struct LoggedInData
        {
            public string PlayerId;
            public string Ticket;
        }

        public PlayFabProxy(string ticketOrUser) : base(NAME)
        {
            playFabClient = new PlayFabClientInstanceAPI();
            SetTicketOrUser(ticketOrUser);
        }

        public void SetTicketOrUser(string ticketOrUser)
        {
            if (ticketOrUser.IndexOf('-') > -1)
            {
                // This is a ticket, so we have logged in via the server
                playFabClient.authenticationContext.ClientSessionTicket = ticketOrUser;
            }
            else
            {
                // This is a user ID, log in locally to get a ticket
                playFabClient.authenticationContext.PlayFabId = ticketOrUser;
            }
        }

        public override void OnRegister()
        {
            base.OnRegister();
            var playerProxy = Facade.RetrieveProxy(PlayerProxy.NAME) as PlayerProxy;
            if (!string.IsNullOrEmpty(playFabClient.authenticationContext.ClientSessionTicket))
            {
                // We have a ticket from the server so we do not need to log in to the client
                SendNotification(Notifications.LoggedInToPlayFab, new LoggedInData
                {
                    PlayerId = playFabClient.authenticationContext.ClientSessionTicket.Substring(0, playFabClient.authenticationContext.ClientSessionTicket.IndexOf('-')),
                    Ticket = playFabClient.authenticationContext.ClientSessionTicket
                });
            }
            else
            {
                // We have a ticket from the server
                playFabClient.LoginWithCustomID(new PlayFab.ClientModels.LoginWithCustomIDRequest { CustomId = playerProxy.UserID },
                    (result) =>
                    {
                        SendNotification(Notifications.LoggedInToPlayFab, new LoggedInData
                        {
                            PlayerId = result.PlayFabId,
                            Ticket = result.SessionTicket
                        });
                    },
                    (error) =>
                    {
                        PlayFabError(error);
                        SendNotification(ServerProxy.Notifications.LogInUser, playerProxy.UserID);
                    }
                );
            }
        }

        private PlayFabClientInstanceAPI playFabClient;

        private void PlayFabError(PlayFabError error)
        {
            Debug.Log($"PlayFab error {error.Error} {error.GenerateErrorReport()}");
        }
    }
}
