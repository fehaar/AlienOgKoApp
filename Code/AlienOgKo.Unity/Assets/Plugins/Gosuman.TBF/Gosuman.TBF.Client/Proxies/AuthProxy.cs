using Cysharp.Threading.Tasks;
using Gosuman.TBF.Auth;
using PureMVC.Patterns.Proxy;

namespace Gosuman.TBF.Proxies
{
    /// <summary>
    /// Handles player authentication via the configured identity backend (PlayFab or Nakama).
    /// Backend-neutral: the rest of the system only sees <see cref="Notifications.LoggedIn"/>.
    /// </summary>
    public class AuthProxy : Proxy
    {
        public static new string NAME = "AuthProxy";

        public static class Notifications
        {
            public const string LoggedIn = "AuthProxy.LoggedIn";
        }

        public struct LoggedInData
        {
            public string PlayerId;
            public string Token;
        }

        private readonly IAuthBackend backend;

        public AuthProxy(IAuthBackend backend) : base(NAME)
        {
            this.backend = backend;
        }

        public IAuthBackend Backend => backend;

        public async UniTaskVoid LogIn(string customId)
        {
            var result = await backend.AuthenticateAsync(customId);
            SendNotification(Notifications.LoggedIn, new LoggedInData
            {
                PlayerId = result.PlayerId,
                Token = result.Token
            });
        }
    }
}
