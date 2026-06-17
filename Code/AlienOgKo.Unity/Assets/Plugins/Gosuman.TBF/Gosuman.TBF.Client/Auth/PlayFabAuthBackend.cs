using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using System;

namespace Gosuman.TBF.Auth
{
    /// <summary>
    /// PlayFab identity backend. Authenticates the device directly via LoginWithCustomID
    /// and presents the resulting session ticket to the game server.
    /// </summary>
    public class PlayFabAuthBackend : IAuthBackend
    {
        private readonly PlayFabClientInstanceAPI client = new PlayFabClientInstanceAPI();
        private string playerId;
        private string ticket;

        public string AuthorizationHeader => "PlayFab " + ticket;
        public string AccessTokenQueryParam => "playFab";
        public string Token => ticket;

        public UniTask<AuthResult> AuthenticateAsync(string customId)
        {
            var tcs = new UniTaskCompletionSource<AuthResult>();
            client.LoginWithCustomID(
                new LoginWithCustomIDRequest { CustomId = customId, CreateAccount = true },
                result =>
                {
                    playerId = result.PlayFabId;
                    ticket = result.SessionTicket;
                    tcs.TrySetResult(new AuthResult { PlayerId = playerId, Token = ticket });
                },
                error => tcs.TrySetException(new Exception($"PlayFab login failed: {error.Error} {error.GenerateErrorReport()}")));
            return tcs.Task;
        }

        public UniTask<AuthResult> RefreshAsync()
        {
            // PlayFab session tickets are not refreshed client-side here; return the current values.
            return UniTask.FromResult(new AuthResult { PlayerId = playerId, Token = ticket });
        }
    }
}
