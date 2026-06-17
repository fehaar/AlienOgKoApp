using Cysharp.Threading.Tasks;
using Nakama;

namespace Gosuman.TBF.Auth
{
    /// <summary>
    /// Nakama identity backend. Authenticates the device against Nakama and presents the
    /// resulting JWT session token to the game server.
    /// </summary>
    public class NakamaAuthBackend : IAuthBackend
    {
        private readonly IClient client;
        private ISession session;

        public NakamaAuthBackend(string scheme, string host, int port, string serverKey)
        {
            // UnityWebRequestAdapter keeps the client working across all platforms, including WebGL.
            client = new Client(scheme, host, port, serverKey, UnityWebRequestAdapter.Instance);
        }

        // The game server validates the Nakama JWT; the query param name must match what it expects.
        public string AuthorizationHeader => "Bearer " + session?.AuthToken;
        public string AccessTokenQueryParam => "token";
        public string Token => session?.AuthToken;

        public async UniTask<AuthResult> AuthenticateAsync(string customId)
        {
            session = await client.AuthenticateDeviceAsync(customId);
            return new AuthResult { PlayerId = session.UserId, Token = session.AuthToken };
        }

        public async UniTask<AuthResult> RefreshAsync()
        {
            if (session != null && !session.IsRefreshExpired)
            {
                session = await client.SessionRefreshAsync(session);
            }
            return new AuthResult { PlayerId = session?.UserId, Token = session?.AuthToken };
        }
    }
}
