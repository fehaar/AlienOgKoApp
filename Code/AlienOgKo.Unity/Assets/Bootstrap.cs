using Cysharp.Threading.Tasks;
using Gosuman.TBF.Commands;
using Gosuman.TBF.Logic;
using Gosuman.TBF.Proxies;
using Newtonsoft.Json;
using PureMVC.Interfaces;
using PureMVC.Patterns.Facade;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlienOgKo
{
    public static class Bootstrap
    {
        private static IFacade facade;
#if UNITY_EDITOR
        private static bool switchToMenu = false;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;

            facade = Facade.GetInstance("AlienOgKo", key => new Facade(key));

#if UNITY_EDITOR
            if (SceneManager.GetActiveScene().name == "MultiplayerGame" || SceneManager.GetActiveScene().name == "Grid")
            {
                switchToMenu = true;
            }
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoad()
        {
            InitializeProgram().Forget();
        }

        private static async UniTaskVoid InitializeProgram()
        {
#if UNITY_EDITOR
            if (switchToMenu)
            {
                await SceneManager.LoadSceneAsync("Menu");
            }
#endif

            Debug.Log("Initializing AlienOgKo");

            JsonConvert.DefaultSettings = () => JsonConverters.GetJsonSerializerSettings();

            facade.RegisterCommand(PlayerProxy.Notifications.PlayerLoggedIn, () => new PlayerLoggedInCommand());
            facade.RegisterCommand(ServerProxy.Notifications.LogInUser, () => new LoginWithCustomIdCommand());
            facade.RegisterCommand(ServerProxy.Notifications.LoginTicketReceived, () => new LoginTicketReceivedCommand());
            facade.RegisterCommand(PlayFabProxy.Notifications.LoggedInToPlayFab, () => new LoggedInToPlayFabCommand());

            var mapView = Object.FindAnyObjectOfType<MapView>();
            if (mapView != null)
                facade.RegisterMediator(new MapMediator(mapView));

            var serverSettings = Resources.Load<ServerSettings>(ServerSettings.ResourcePath);
            if (serverSettings == null)
            {
                Debug.LogError($"ServerSettings asset not found at Resources/{ServerSettings.ResourcePath}. Create one via Assets > Create > Gosuman > Server Settings and place it under Assets/Resources.");
                return;
            }
#if LOCAL_SERVER
            facade.RegisterProxy(new ServerProxy(serverSettings.LocalServerUrl));
#else
            facade.RegisterProxy(new ServerProxy(serverSettings.RemoteServerUrl));
#endif

            facade.RegisterProxy(new PlayerProxy());
        }
    }
}
