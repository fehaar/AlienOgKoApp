using Cysharp.Threading.Tasks;

namespace Gosuman.TBF.Auth
{
    /// <summary>
    /// Result of authenticating a player against an identity backend.
    /// </summary>
    public struct AuthResult
    {
        /// <summary>Backend-specific player/user id (PlayFab id, Nakama user id, ...).</summary>
        public string PlayerId;

        /// <summary>The token presented to our own game server when connecting to the hub.</summary>
        public string Token;
    }

    /// <summary>
    /// Abstraction over the identity provider (PlayFab, Nakama, ...). A game ships configured
    /// for exactly one backend; the implementation chosen at startup is the only one used.
    /// The client never reads game data from the backend - it only authenticates and hands the
    /// resulting token to our own game server.
    /// </summary>
    public interface IAuthBackend
    {
        /// <summary>Authenticate (creating an account if needed) using the device/custom id.</summary>
        UniTask<AuthResult> AuthenticateAsync(string customId);

        /// <summary>Refresh the session token if the backend supports it; otherwise returns the current values.</summary>
        UniTask<AuthResult> RefreshAsync();

        /// <summary>Full HTTP Authorization header value, e.g. "PlayFab &lt;ticket&gt;" or "Bearer &lt;jwt&gt;".</summary>
        string AuthorizationHeader { get; }

        /// <summary>
        /// Query-string parameter name used to carry the token on websocket upgrades
        /// (where an Authorization header can't be set). Must match what the game server expects.
        /// </summary>
        string AccessTokenQueryParam { get; }

        /// <summary>The raw token value (without scheme prefix).</summary>
        string Token { get; }
    }
}
