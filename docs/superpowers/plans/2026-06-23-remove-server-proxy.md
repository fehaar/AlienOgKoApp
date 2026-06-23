# Remove ServerProxy / Hub-Connect Step Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** AlienOgKo has no game server of its own (no SignalR hub to deploy/connect to), so stop the startup flow from registering `ServerProxy` or connecting to a hub — the app should only authenticate against the configured identity backend (self-hosted Nakama for now) and stop there.

**Architecture:** `Bootstrap.cs` currently wires `PlayerProxy.OnRegister` → `PlayerLoggedInCommand` → `AuthProxy.LogIn` → `NakamaAuthBackend.AuthenticateAsync` → `AuthProxy.Notifications.LoggedIn` → `LoggedInCommand` (sets `PlayerProxy.PlayerId` **and** calls `ServerProxy.ConnectToHub`). `LoggedInCommand`/`ServerProxy` are shared template code (`Gosuman.TBF.Commands`/`Gosuman.TBF.Proxies`, also used by other TBF-based projects that *do* have a real server) — we don't modify or delete them. Instead, AlienOgKo's `Bootstrap.cs` stops registering `ServerProxy` and swaps in a new, AlienOgKo-specific `PlayerAuthenticatedCommand` that does only the `PlayerId` assignment, dropping the hub-connect call.

**Tech Stack:** PureMVC (`dk.gosuman.puremvc`), Cysharp UniTask, the existing `Gosuman.TBF.Proxies.AuthProxy`/`PlayerProxy` and `Gosuman.TBF.Auth.IAuthBackend`/`NakamaAuthBackend` (all already in the codebase, untouched by this plan).

## Global Constraints

- Do not modify or delete `Code/AlienOgKo.Unity/Assets/Plugins/Gosuman.TBF/Gosuman.TBF.Client/Proxies/ServerProxy.cs` or `.../Commands/LoggedInCommand.cs` — both are shared template code other TBF-based projects (e.g. CanVerse) use with a real server. AlienOgKo simply stops opting into them from its own `Bootstrap.cs`.
- Confirmed by direct grep (no other call sites exist): `ServerProxy` is referenced only in `Bootstrap.cs` and `LoggedInCommand.cs`; `ServerSettings` only in `Bootstrap.cs` and its own definition. Removing the registration is safe — nothing else in AlienOgKo's code depends on either.
- `PlayerLoggedInCommand.cs` (shared) is unaffected and stays registered as-is — it only forwards to `AuthProxy.LogIn`, no server-specific logic.
- `ServerSettings` (`Code/AlienOgKo.Unity/Assets/Scripts/Settings/ServerSettings.cs`) is AlienOgKo's own project-local copy (lives under `Assets/Scripts/`, not `Assets/Plugins/`), not shared at runtime with other projects — safe to edit directly.
- Keep all of `ServerSettings`'s Nakama/PlayFab fields (`authBackend`, `nakamaScheme`, `nakamaHost`, `nakamaPort`, `nakamaServerKey`) exactly as-is — `Bootstrap.CreateAuthBackend` still needs them. Only `localServerUrl`/`remoteServerUrl` become dead fields once `ServerProxy` registration is removed.
- Local self-hosted Nakama defaults (`nakamaScheme: "http"`, `nakamaHost: "localhost"`, `nakamaPort: 7350`) stay as the default — cloud-hosted Nakama is explicitly a separate, later issue, out of scope here.
- New AlienOgKo-specific code goes in the `AlienOgKo` namespace, matching `MapDataProxy.cs`/`MapSettings.cs` precedent. No existing `Assets/Scripts/Commands/` folder exists yet — this plan creates it.
- No automated test is added for `PlayerAuthenticatedCommand` — matches the existing codebase convention where no PureMVC `Command`/`Proxy` class (`ServerProxy`, `PlayerProxy`, `AuthProxy`, `LoggedInCommand`, `PlayerLoggedInCommand`) has a unit test; only the pure-logic pieces (`Place`, `MapBounds`) are tested. Verification is: project compiles with 0 errors, and a manual Play-mode check confirms the `Debug.Log` from the new command fires with no errors.
- Unity Editor lock: at plan-writing time, the human owner has the Editor open on `Code/AlienOgKo.Unity` (PID held since this session began). If still the case when this plan executes, do not run Unity batchmode against that path directly — use the pre-warmed sandbox at `/tmp/alienogko-verify` instead (sync via `rsync -a --delete .../Code/AlienOgKo.Unity/Assets/ /tmp/alienogko-verify/Assets/`, then run batchmode with `-projectPath /tmp/alienogko-verify`). If the lock is gone, run directly against the real path instead.

---

### Task 1: Drop ServerProxy registration, authenticate via Nakama/PlayFab only

**Files:**
- Create: `Code/AlienOgKo.Unity/Assets/Scripts/Commands/PlayerAuthenticatedCommand.cs`
- Modify: `Code/AlienOgKo.Unity/Assets/Bootstrap.cs`
- Modify: `Code/AlienOgKo.Unity/Assets/Scripts/Settings/ServerSettings.cs`

**Interfaces:**
- Consumes: `Gosuman.TBF.Proxies.AuthProxy.LoggedInData` (existing struct: `string PlayerId`, `string Token`), `Gosuman.TBF.Proxies.PlayerProxy.PlayerId` (existing settable property), `Gosuman.TBF.Proxies.AuthProxy.Notifications.LoggedIn` (existing notification name).
- Produces: `AlienOgKo.PlayerAuthenticatedCommand` — a `PureMVC.Patterns.Command.SimpleCommand` registered against `AuthProxy.Notifications.LoggedIn` in place of the shared `Gosuman.TBF.Commands.LoggedInCommand`.

- [ ] **Step 1: Create the AlienOgKo-specific post-login command**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Commands/PlayerAuthenticatedCommand.cs`:

```csharp
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
```

- [ ] **Step 2: Rewire Bootstrap.cs to use it and drop ServerProxy**

In `Code/AlienOgKo.Unity/Assets/Bootstrap.cs`, change this line:

```csharp
            facade.RegisterCommand(AuthProxy.Notifications.LoggedIn, () => new LoggedInCommand());
```

to:

```csharp
            facade.RegisterCommand(AuthProxy.Notifications.LoggedIn, () => new PlayerAuthenticatedCommand());
```

Then delete this whole block (it currently sits between the `serverSettings == null` null-check and the `AuthProxy` registration):

```csharp
#if LOCAL_SERVER
            facade.RegisterProxy(new ServerProxy(serverSettings.LocalServerUrl));
#else
            facade.RegisterProxy(new ServerProxy(serverSettings.RemoteServerUrl));
#endif

```

Finally, update the comment directly above `facade.RegisterProxy(new PlayerProxy());` from:

```csharp
            // Registered last: PlayerProxy.OnRegister kicks off login, which needs AuthProxy + ServerProxy ready.
```

to:

```csharp
            // Registered last: PlayerProxy.OnRegister kicks off login, which needs AuthProxy ready.
```

The full `InitializeProgram` method should now read:

```csharp
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
            facade.RegisterCommand(AuthProxy.Notifications.LoggedIn, () => new PlayerAuthenticatedCommand());

            var mapView = Object.FindAnyObjectByType<MapView>();
            if (mapView != null)
                facade.RegisterMediator(new MapMediator(mapView));

            var topBarView = Object.FindAnyObjectByType<TopBarView>();
            if (topBarView != null)
                facade.RegisterMediator(new TopBarMediator(topBarView));

            var bottomBarView = Object.FindAnyObjectByType<BottomBarView>();
            if (bottomBarView != null)
                facade.RegisterMediator(new BottomBarMediator(bottomBarView));

            var serverSettings = Resources.Load<ServerSettings>(ServerSettings.ResourcePath);
            if (serverSettings == null)
            {
                Debug.LogError($"ServerSettings asset not found at Resources/{ServerSettings.ResourcePath}. Create one via Assets > Create > Gosuman > Server Settings and place it under Assets/Resources.");
                return;
            }

            facade.RegisterProxy(new AuthProxy(CreateAuthBackend(serverSettings)));

            // Registered last: PlayerProxy.OnRegister kicks off login, which needs AuthProxy ready.
            facade.RegisterProxy(new PlayerProxy());

            var mapSettings = Resources.Load<MapSettings>(MapSettings.ResourcePath);
            if (mapSettings == null)
            {
                Debug.LogError($"MapSettings asset not found at Resources/{MapSettings.ResourcePath}. Create one via Assets > Create > AlienOgKo > Map Settings, or run Gosuman > Create Map Settings Asset, and place it under Assets/Resources.");
            }
            else
            {
                facade.RegisterProxy(new MapDataProxy(mapSettings));
            }
        }
```

(`CreateAuthBackend` below it, and everything above `InitializeProgram`, are unchanged.)

- [ ] **Step 3: Remove the now-dead server URL fields from ServerSettings**

In `Code/AlienOgKo.Unity/Assets/Scripts/Settings/ServerSettings.cs`, remove these two lines:

```csharp
        [SerializeField] private string localServerUrl = "https://localhost:32769";
        [SerializeField] private string remoteServerUrl = "";

```

and these two lines:

```csharp
        public string LocalServerUrl => localServerUrl;
        public string RemoteServerUrl => remoteServerUrl;

```

The full file should now read:

```csharp
using UnityEngine;

namespace Gosuman.TBF
{
    /// <summary>The identity backend a build authenticates against. A build ships with exactly one.</summary>
    public enum AuthBackendType
    {
        PlayFab,
        Nakama
    }

    [CreateAssetMenu(fileName = "ServerSettings", menuName = "Gosuman/Server Settings")]
    public class ServerSettings : ScriptableObject
    {
        public const string ResourcePath = "ServerSettings";

        [Header("Identity backend")]
        [SerializeField] private AuthBackendType authBackend = AuthBackendType.Nakama;

        [Header("Nakama (used when Auth Backend = Nakama)")]
        [SerializeField] private string nakamaScheme = "http";
        [SerializeField] private string nakamaHost = "localhost";
        [SerializeField] private int nakamaPort = 7350;
        [SerializeField] private string nakamaServerKey = "defaultkey";

        public AuthBackendType AuthBackend => authBackend;
        public string NakamaScheme => nakamaScheme;
        public string NakamaHost => nakamaHost;
        public int NakamaPort => nakamaPort;
        public string NakamaServerKey => nakamaServerKey;
    }
}
```

Note: the committed `ServerSettings.asset` will still carry orphaned `localServerUrl`/`remoteServerUrl` YAML lines until Unity next re-saves it — Unity silently ignores serialized fields that no longer exist on the class, so this is harmless and not worth a special cleanup step.

- [ ] **Step 4: Verify the project compiles with 0 errors**

Check whether the Editor lock is held (human owner may have the project open):

```bash
ls /home/fehaar/devdrive/AlienOgKoApp/Code/AlienOgKo.Unity/Temp/UnityLockfile 2>/dev/null && echo LOCKED || echo FREE
```

If `LOCKED`, sync and verify against the sandbox:

```bash
rsync -a --delete /home/fehaar/devdrive/AlienOgKoApp/Code/AlienOgKo.Unity/Assets/ /tmp/alienogko-verify/Assets/
/home/fehaar/Unity/Hub/Editor/6000.5.0f1/Editor/Unity -batchmode -nographics \
  -projectPath /tmp/alienogko-verify \
  -runTests -testPlatform EditMode \
  -testResults /tmp/alienogko-removeserver-results.xml \
  -logFile /tmp/alienogko-removeserver.log
```

(Do not pass `-quit` together with `-runTests` — that combination races on this sandbox and can make Unity exit before tests finish; `-runTests` quits on its own.)

If `FREE`, run the same command directly against `-projectPath /home/fehaar/devdrive/AlienOgKoApp/Code/AlienOgKo.Unity` instead, and in that case `-quit` is fine to add since there's no sandbox-cold-import race.

Either way, check:

```bash
grep -c "error CS" /tmp/alienogko-removeserver.log
grep -o 'result="[A-Za-z]*"' /tmp/alienogko-removeserver-results.xml | head -1
```

Expected: `0` compile errors, `result="Passed"` (the existing `PlaceTests`/`MapBoundsTests` should still be the only tests and still pass — this task adds no new automated test, per Global Constraints).

- [ ] **Step 5: Commit**

```bash
git add Code/AlienOgKo.Unity/Assets/Scripts/Commands/PlayerAuthenticatedCommand.cs* \
        Code/AlienOgKo.Unity/Assets/Bootstrap.cs \
        Code/AlienOgKo.Unity/Assets/Scripts/Settings/ServerSettings.cs
git commit -m "Drop ServerProxy/hub-connect step; authenticate via Nakama/PlayFab only"
```

(If `Assets/Scripts/Commands/` and its `.meta` were auto-generated by the live Editor's background import — check `git status --porcelain Code/AlienOgKo.Unity/Assets/Scripts/Commands/` — include those `.meta` files in the `git add` too.)

---

## Self-Review Notes

- **Spec coverage:** "yank logging in to the server out of the flow, so that all we do is to auth with Nakama" → `ServerProxy` registration removed (Step 2), `PlayerAuthenticatedCommand` replaces `LoggedInCommand` and drops the `ConnectToHub` call (Step 1), confirmed `PlayerLoggedInCommand`/`AuthProxy`/Nakama auth path is otherwise untouched and still runs. Dead `ServerSettings` fields cleaned up (Step 3) since they have no remaining consumer.
- **Out of scope, confirmed explicitly by the user:** cloud-hosted Nakama deployment (separate future issue); telemetry-to-Nakama implementation (discussed but not requested yet); issue #11 (Nakama SDK config/verification) — this plan only removes the server step, it doesn't touch Nakama SDK setup itself.
- **Type consistency:** `PlayerAuthenticatedCommand` reads `AuthProxy.LoggedInData` and writes `PlayerProxy.PlayerId` — both pre-existing types/members, unchanged from how `LoggedInCommand` already uses them, so no signature drift risk.
- **Not deleting shared template files** (`ServerProxy.cs`, `LoggedInCommand.cs`) was a deliberate call, not an oversight — confirmed via grep that no other AlienOgKo code references them, so leaving them in place costs nothing and preserves them for other TBF-based projects.
