using PureMVC.Patterns.Proxy;
using System;
using System.IO;

using UnityEngine;

namespace Gosuman.TBF.Proxies
{
    /// <summary>
    /// This proxy handles player identity and login state.
    /// </summary>
    public class PlayerProxy : Proxy
    {
        public static new string NAME = "PlayerProxy";
        public static class Notifications
        {
            public const string PlayerLoggedIn = "PlayerProxy.PlayerLoggedIn";
        }
        public PlayerProxy() : base(NAME)
        {
        }

        public string UserID { get; private set; }
        public string PlayerId { get; set; }

        public override void OnRegister()
        {
            base.OnRegister();
            // No saved ticket, generate or load custom id
            var idPath = Path.Combine(Application.persistentDataPath, "id");
#if UNITY_EDITOR
            //if (!Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor)
            //{
            //    idPath = Path.Combine(Application.persistentDataPath, "id-editor1");
            //}
            //else
            //{
                idPath = Path.Combine(Application.persistentDataPath, "id-editor");
            //}
#endif
            if (File.Exists(idPath))
            {
                UserID = File.ReadAllText(idPath);
                SendNotification(Notifications.PlayerLoggedIn, UserID);
            }
            else
            {
                UserID = Ulid.NewUlid().ToString();
                File.WriteAllText(idPath, UserID);
                SendNotification(ServerProxy.Notifications.LogInUser, UserID);
            }
        }
    }
}
