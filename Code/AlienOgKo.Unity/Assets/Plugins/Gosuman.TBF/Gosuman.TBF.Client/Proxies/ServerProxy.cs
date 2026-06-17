using Best.HTTP;
using Best.SignalR;
using Best.SignalR.Encoders;
using Cysharp.Threading.Tasks;
using Gosuman.TBF.Commands;
using Gosuman.TBF.Logic.Entities;
using Gosuman.TBF.Providers;
using Gosuman.TBF.Shared;
using Gosuman.TBF.Shared.Interfaces;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Gosuman.TBF.Proxies
{
    /// <summary>
    /// This proxy handles communication with the game server.
    /// </summary>
    public class ServerProxy : PureMVC.Patterns.Proxy.Proxy
    {
        public static new string NAME = "ServerProxy";
        private string baseUri;
        private HubConnection gameHub;
        private string gameId;
        private string ticket;

        public ServerProxy(string baseUri) : base(NAME)
        {
            this.baseUri = baseUri;
            Debug.Log($"Use server URL: {baseUri}");
        }

        public static class Notifications
        {
            public const string LogInUser = "ServerProxy.LogInUser";
            public const string LoginTicketReceived = "ServerProxy.LoginTicketReceived";
            public const string ConnectedToHub = "ServerProxy.ConnectedToHub";
            public const string CreateGame = "ServerProxy.CreateGame";
            public const string GameCreated = "ServerProxy.GameCreated";
            public const string JoinGame = "ServerProxy.JoinGame";
            public const string JoinGameFailed = "ServerProxy.JoinGameFailed";
            public const string GetActiveGames = "ServerProxy.GetActiveGames";
            public const string ShowActiveGames = "ServerProxy.ShowActiveGames";
            public const string GameDataReceived = "ServerProxy.GameDataReceived";
        }

        public async UniTaskVoid Login(string customId)
        {
            var request = HTTPRequest.CreatePost($"{baseUri}/identity");
            request.SetHeader("Content-Type", "application/json");
            request.UploadSettings.UploadStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(customId)));
            await request.Send().WithCancellation(Application.exitCancellationToken);
            if (request.State != HTTPRequestStates.Finished)
            {
                // Something went wrong. Should we look at retrying?
                Debug.LogError($"Could not connect to server. {request.State} {request.Exception}");
                return;
            }

            if (request.Response.IsSuccess)
            {
                var ticket = Encoding.UTF8.GetString(request.Response.Data);
                SendNotification(Notifications.LoginTicketReceived, ticket);
            }
            else
            {
                Debug.LogError($"Error logging in: {request.Response.StatusCode} {request.Response.DataAsText}");
            }
        }

        public async UniTaskVoid ConnectToHub(string authenticationTicket)
        {
            var playFabProxy = Facade.RetrieveProxy(PlayFabProxy.NAME) as PlayFabProxy;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                gameHub = new HubConnection(new Uri($"{baseUri}/webGameHub"), new JsonProtocol(new JsonDotNetEncoder(JsonConvert.DefaultSettings())));
                ticket = authenticationTicket;
            }
            else
            {
                gameHub = new HubConnection(new Uri($"{baseUri}/gameHub"), new JsonProtocol(new JsonDotNetEncoder(JsonConvert.DefaultSettings())));
                gameHub.AuthenticationProvider = new HubAuthenticationProvider(authenticationTicket);
            }
            gameHub.OnConnected += GameHub_OnConnected;
            gameHub.OnClosed += GameHub_OnClosed;
            gameHub.OnError += GameHub_OnError;
            gameHub.On<GameStateIncrement>("GameStateUpdated", OnGameStateUpdated);
            Debug.Log($"Connecting to hub with ticket {authenticationTicket}");
            await gameHub.ConnectAsync();
            Debug.Log("Connecting done... " + gameHub.State);
            SendNotification(Notifications.ConnectedToHub);
            Application.quitting += OnApplicationQuitting;
            return;
        }

        private void OnApplicationQuitting()
        {
            if (gameHub != null)
            {
                gameHub.CloseAsync().Forget();
            }
        }

        private void OnGameStateUpdated(GameStateIncrement increment)
        {
            SendNotification(Notifications.GameDataReceived, increment);
        }

        private void GameHub_OnError(HubConnection arg1, string arg2)
        {
            Debug.LogError($"Hub error: {arg2}");
            gameHub = null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void GameHub_OnClosed(HubConnection obj)
        {
            Debug.Log($"Hub closed");
            gameHub = null;
        }

        private void GameHub_OnConnected(HubConnection obj)
        {
            Debug.Log($"Hub connected");
        }

        public async UniTaskVoid CreateGame(GameSettings gameSettings)
        {
            if (gameHub != null)
            {
                var gameId = await gameHub.InvokeAsync<string>("CreateGame", gameSettings);
                if (!string.IsNullOrEmpty(gameId))
                {
                    this.gameId = gameId;
                    // TODO: Reenable GameDataReceived command when we reenable creating games.
                    //Facade.RegisterCommand(Notifications.GameDataReceived, () => new GameDataReceivedCommand());
                    SendNotification(Notifications.GameCreated, gameId);
                }
                else
                {
                    Debug.LogError("Returned GameId was null");
                }
            }
            else
            {
                Debug.LogError("Not connected to hub");
            }
        }

        public async UniTaskVoid JoinGame(string gameId)
        {
            if (gameHub != null)
            {
                bool joined = false;
                // TODO: Reenable GameDataReceived command when we reenable creating games.
                //Facade.RegisterCommand(Notifications.GameDataReceived, () => new GameDataReceivedCommand());
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    joined = await gameHub.InvokeAsync<bool>("JoinGame", ticket, gameId);
                }
                else
                {
                    joined = await gameHub.InvokeAsync<bool>("JoinGame", gameId);
                }
                if (joined)
                {
                    this.gameId = gameId;
                    Debug.Log($"Joined game {gameId}");
                }
                else
                {
                    this.gameId = string.Empty;
                    Facade.RemoveCommand(Notifications.GameDataReceived);
                    SendNotification(Notifications.JoinGameFailed, gameId);
                }
            }
            else
            {
                Debug.LogError("Not connected to hub");
            }
        }

        public async UniTask<string[]> GetActiveGames()
        {
            if (gameHub != null)
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    var games = await gameHub.InvokeAsync<string[]>("ActiveGames", ticket);
                    return games;
                }
                else
                {
                    var games = await gameHub.InvokeAsync<string[]>("ActiveGames");
                    return games;
                }
            }
            else
            {
                Debug.LogError("Not connected to hub");
                return Array.Empty<string>();
            }
        }

        public async UniTaskVoid ExecuteAction(IClientGameAction action)
        {
            if (gameHub != null)
            {
                await gameHub.SendAsync("ExecuteAction", gameId, action);
            }
            else
            {
                Debug.LogError("Not connected to hub when trying to execute action");
            }
        }
    }
}
