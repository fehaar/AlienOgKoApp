# MapDataProxy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `MapDataProxy`, registered at app startup, that loads the map's bounding box and API key from a local config asset, fetches places from the backend, filters them to the configured box, and holds them in memory.

**Architecture:** `MapDataProxy` is a PureMVC `Proxy` registered in `Bootstrap.cs` at app start. It reads NE/SW bounds and the API key from a `MapSettings` `ScriptableObject` (`Resources/MapSettings.asset`), fetches `GET /places/all` from the backend via Best.HTTP (same pattern as the existing `ServerProxy`), deserializes the response into `Place` objects via Newtonsoft.Json, filters them with the pure `MapBounds.FilterContained` function to only places fully inside the configured box, and exposes the result in memory as `Places` for later consumption by `MapMediator`/`MapView`.

**Tech Stack:** Best.HTTP 3.0.17 (embedded package at `Packages/com.tivadar.best.http`), Cysharp UniTask, Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`), PureMVC (`dk.gosuman.puremvc`), Unity Test Framework 1.7.0 (NUnit, EditMode tests).

## Global Constraints

- Project: `Code/AlienOgKo.Unity` (Unity 6000.5.0f1). Editor binary for batchmode commands: `/home/fehaar/Unity/Hub/Editor/6000.5.0f1/Editor/Unity`. Verified the project currently compiles with 0 errors before this plan's changes.
- Endpoint: `GET https://hvor-fanden-er-fanen.osc-fr1.scalingo.io/places/all`. Auth via `Authorization` header carrying the raw API key (no `Bearer` prefix) — confirmed against the live API (returns 401 without the header, 200 with it).
- The backend's JSON casing is irregular: `ID` and `TS` are upper-case, every other field (`fixed`, `tag`, `upperLeftX1`, `upperLeftY1`, `lowerRightX2`, `lowerRightY2`) is camelCase. Confirmed against a real captured response (`Docs/places.json`).
- "Within the map zone" means a place's full bounding box (`[upperLeftX1,upperLeftY1]` to `[lowerRightX2,lowerRightY2]`) is entirely inside the configured NE/SW box — overlap is not enough. `Docs/mapdata.txt`'s NE/SW corners must be normalized with min/max before comparison: its NE longitude (12.07042) is numerically *less* than its SW longitude (12.08611), i.e. the corners are not consistently labeled, so bounds must never be compared without normalizing.
- Never commit a real API key. `MapSettings.asset` ships with an empty `apiKey` field, exactly like the existing `ServerSettings.asset` ships with an empty `remoteServerUrl`.
- Follow existing conventions exactly: PureMVC `Proxy` subclass + `facade.RegisterProxy(...)` in `Bootstrap.cs` (see `ServerProxy`/`PlayerProxy`), `ScriptableObject` settings loaded via `Resources.Load<T>(T.ResourcePath)` (see `ServerSettings.cs`), Best.HTTP + UniTask for the network call (see `ServerProxy.Login`), Newtonsoft `JsonConvert` for (de)serialization.
- New game-specific code lives under `Assets/Scripts/Map/` and `Assets/Scripts/Settings/` in the `AlienOgKo` namespace (matching `MapView.cs`/`MapMediator.cs`), not under the shared `Assets/Plugins/Gosuman.TBF/...` template folder (that folder is for cross-project template code, e.g. `ServerProxy`/`PlayerProxy`).
- **Asmdef layout (verified by direct compile, do not deviate):** a custom `.asmdef`'s `references` array cannot target the special default `Assembly-CSharp` assembly — Unity silently drops that entry rather than erroring on the asmdef itself, which then surfaces later as a `CS0246` in whatever consumes the type. Confirmed by inspecting the actual `csc` `.rsp` file Unity generated: `Assembly-CSharp.dll` was absent from the reference list despite being listed in the asmdef. Referencing one custom/package asmdef from another by its plain `name` string (e.g. `"UnityEngine.TestRunner"`, `"AlienOgKo.Map.Core"`) does work — confirmed in the same compile run. Therefore: `Place.cs` and `MapBounds.cs` (the two pieces that need EditMode tests) live in their own production assembly `Assets/Scripts/Map/Core/AlienOgKo.Map.Core.asmdef`, and `AlienOgKo.Map.Tests.asmdef` references that assembly by name — not `Assembly-CSharp`. `MapView.cs`/`MapMediator.cs` (untouched by this plan) and the untested `MapDataProxy.cs`/`MapSettings.cs` stay in the default assembly as before; default-assembly code can reference a custom asmdef (the supported direction), so `Bootstrap.cs`/`MapDataProxy.cs` need no special handling to use `Place`/`MapBounds`.
- Newtonsoft.Json is auto-referenced by default for a custom asmdef with `overrideReferences: false` (confirmed: `Gosuman.TBF.Logic.asmdef` uses `Newtonsoft.Json` with no explicit reference and `overrideReferences: false`). `overrideReferences: true` (needed on the Tests asmdef for `nunit.framework.dll`) disables *all* implicit auto-references, so that asmdef must also explicitly list `Newtonsoft.Json.dll` in `precompiledReferences`.

---

### Task 1: Test assembly + `Place` model

**Files:**
- Create: `Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/AlienOgKo.Map.Core.asmdef`
- Create: `Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/Place.cs`
- Create: `Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/AlienOgKo.Map.Tests.asmdef`
- Test: `Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/PlaceTests.cs`

**Interfaces:**
- Produces: `AlienOgKo.Place` (in the `AlienOgKo.Map.Core` assembly) — public properties `int Id`, `DateTime Ts`, `bool Fixed`, `string Tag`, `double UpperLeftX1`, `double UpperLeftY1`, `double LowerRightX2`, `double LowerRightY2`. Deserializable from the backend's `/places/all` JSON shape via `Newtonsoft.Json.JsonConvert.DeserializeObject<Place>(...)` / `DeserializeObject<List<Place>>(...)`.

- [ ] **Step 1: Create the production assembly for testable map logic**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/AlienOgKo.Map.Core.asmdef`. This holds the pieces that need EditMode tests (`Place`, and `MapBounds` in Task 2), separate from `MapView.cs`/`MapMediator.cs` which stay in the default assembly untouched. A custom asmdef cannot reference the special default `Assembly-CSharp` assembly (Unity silently drops that reference — see Global Constraints), so giving these two files their own assembly is what makes them testable at all:

```json
{
    "name": "AlienOgKo.Map.Core",
    "rootNamespace": "AlienOgKo",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Create the EditMode test assembly definition**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/AlienOgKo.Map.Tests.asmdef`. It references `AlienOgKo.Map.Core` by name (custom-to-custom asmdef references by name work — only referencing `Assembly-CSharp` this way fails). `overrideReferences: true` (required for `nunit.framework.dll`) disables implicit auto-references project-wide for this assembly, so `Newtonsoft.Json.dll` must also be listed explicitly here even though `AlienOgKo.Map.Core` gets it automatically:

```json
{
    "name": "AlienOgKo.Map.Tests",
    "rootNamespace": "AlienOgKo",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "AlienOgKo.Map.Core"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll",
        "Newtonsoft.Json.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Write the failing test**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/PlaceTests.cs`:

```csharp
using Newtonsoft.Json;
using NUnit.Framework;

namespace AlienOgKo.Tests
{
    public class PlaceTests
    {
        [Test]
        public void Deserialize_ParsesRealApiShape()
        {
            const string json = @"{
                ""ID"": 15,
                ""TS"": ""2026-02-14T21:39:43.000Z"",
                ""fixed"": true,
                ""lowerRightX2"": 55.620282,
                ""lowerRightY2"": 12.073561,
                ""tag"": ""Apollo"",
                ""upperLeftX1"": 55.621252,
                ""upperLeftY1"": 12.071597
            }";

            var place = JsonConvert.DeserializeObject<Place>(json);

            Assert.AreEqual(15, place.Id);
            Assert.AreEqual("Apollo", place.Tag);
            Assert.IsTrue(place.Fixed);
            Assert.AreEqual(55.621252, place.UpperLeftX1, 1e-6);
            Assert.AreEqual(12.071597, place.UpperLeftY1, 1e-6);
            Assert.AreEqual(55.620282, place.LowerRightX2, 1e-6);
            Assert.AreEqual(12.073561, place.LowerRightY2, 1e-6);
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it fails**

Run:
```bash
/home/fehaar/Unity/Hub/Editor/6000.5.0f1/Editor/Unity -batchmode -nographics \
  -projectPath /home/fehaar/devdrive/AlienOgKoApp/Code/AlienOgKo.Unity \
  -runTests -testPlatform EditMode \
  -testResults /tmp/alienogko-task1-red.xml \
  -logFile /tmp/alienogko-task1-red.log -quit
grep -c "error CS" /tmp/alienogko-task1-red.log
```
Expected: at least one `error CS0246: The type or namespace name 'Place' could not be found` in the log (compile fails because `Place` doesn't exist yet), and `/tmp/alienogko-task1-red.xml` is absent or shows zero tests executed.

- [ ] **Step 5: Write the minimal implementation**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/Place.cs`:

```csharp
using Newtonsoft.Json;
using System;

namespace AlienOgKo
{
    public class Place
    {
        [JsonProperty("ID")]
        public int Id { get; set; }

        [JsonProperty("TS")]
        public DateTime Ts { get; set; }

        [JsonProperty("fixed")]
        public bool Fixed { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("upperLeftX1")]
        public double UpperLeftX1 { get; set; }

        [JsonProperty("upperLeftY1")]
        public double UpperLeftY1 { get; set; }

        [JsonProperty("lowerRightX2")]
        public double LowerRightX2 { get; set; }

        [JsonProperty("lowerRightY2")]
        public double LowerRightY2 { get; set; }
    }
}
```

- [ ] **Step 6: Run the test to verify it passes**

Run:
```bash
/home/fehaar/Unity/Hub/Editor/6000.5.0f1/Editor/Unity -batchmode -nographics \
  -projectPath /home/fehaar/devdrive/AlienOgKoApp/Code/AlienOgKo.Unity \
  -runTests -testPlatform EditMode \
  -testResults /tmp/alienogko-task1-green.xml \
  -logFile /tmp/alienogko-task1-green.log -quit
grep -o 'result="[A-Za-z]*"' /tmp/alienogko-task1-green.xml | head -1
grep "PlaceTests" /tmp/alienogko-task1-green.xml
```
Expected: `result="Passed"` and a `<test-case ... name="Deserialize_ParsesRealApiShape" ... result="Passed" ...>` entry. This run also generates `.meta` files for all new files under `Assets/Scripts/Map/Core/` and `Assets/Scripts/Map/Tests/` — check `git status` afterward and include them in the commit.

- [ ] **Step 7: Commit**

```bash
git add Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/AlienOgKo.Map.Core.asmdef* \
        Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/Place.cs* \
        Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/AlienOgKo.Map.Tests.asmdef* \
        Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/PlaceTests.cs*
git commit -m "Add Place model with EditMode test assembly"
```

---

### Task 2: `MapBounds` containment filter

**Files:**
- Create: `Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/MapBounds.cs`
- Test: `Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/MapBoundsTests.cs`

**Interfaces:**
- Consumes: `AlienOgKo.Place` (Task 1).
- Produces: `AlienOgKo.MapBounds.FilterContained(IEnumerable<Place> places, double neLatitude, double neLongitude, double swLatitude, double swLongitude) : List<Place>` — returns only the places whose full bounding box is contained within the (min/max-normalized) NE/SW box.

- [ ] **Step 1: Write the failing tests**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/MapBoundsTests.cs`. Both fixtures are real places verified by hand against the live API earlier (Apollo is one of the 7 places fully inside `Docs/mapdata.txt`'s box; Roskilde Festival overlaps the box but is bigger than it, so must be excluded):

```csharp
using NUnit.Framework;
using System.Collections.Generic;

namespace AlienOgKo.Tests
{
    public class MapBoundsTests
    {
        // Docs/mapdata.txt:
        // Kort NE 55.62350, 12.07042
        // Kort SW 55.61706, 12.08611
        const double NeLat = 55.62350, NeLon = 12.07042, SwLat = 55.61706, SwLon = 12.08611;

        [Test]
        public void FilterContained_KeepsPlaceFullyInsideBox()
        {
            var apollo = new Place
            {
                Tag = "Apollo",
                UpperLeftX1 = 55.621252,
                UpperLeftY1 = 12.071597,
                LowerRightX2 = 55.620282,
                LowerRightY2 = 12.073561
            };

            var result = MapBounds.FilterContained(new List<Place> { apollo }, NeLat, NeLon, SwLat, SwLon);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Apollo", result[0].Tag);
        }

        [Test]
        public void FilterContained_DropsPlaceThatOnlyOverlaps()
        {
            var roskildeFestival = new Place
            {
                Tag = "Roskilde Festival",
                UpperLeftX1 = 55.62291,
                UpperLeftY1 = 12.064422,
                LowerRightX2 = 55.60926,
                LowerRightY2 = 12.098569
            };

            var result = MapBounds.FilterContained(new List<Place> { roskildeFestival }, NeLat, NeLon, SwLat, SwLon);

            Assert.AreEqual(0, result.Count);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:
```bash
/home/fehaar/Unity/Hub/Editor/6000.5.0f1/Editor/Unity -batchmode -nographics \
  -projectPath /home/fehaar/devdrive/AlienOgKoApp/Code/AlienOgKo.Unity \
  -runTests -testPlatform EditMode \
  -testResults /tmp/alienogko-task2-red.xml \
  -logFile /tmp/alienogko-task2-red.log -quit
grep -c "error CS" /tmp/alienogko-task2-red.log
```
Expected: `error CS0246: The type or namespace name 'MapBounds' could not be found` in the log.

- [ ] **Step 3: Write the minimal implementation**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/MapBounds.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienOgKo
{
    public static class MapBounds
    {
        public static List<Place> FilterContained(
            IEnumerable<Place> places, double neLatitude, double neLongitude, double swLatitude, double swLongitude)
        {
            double latMin = Math.Min(neLatitude, swLatitude);
            double latMax = Math.Max(neLatitude, swLatitude);
            double lonMin = Math.Min(neLongitude, swLongitude);
            double lonMax = Math.Max(neLongitude, swLongitude);

            return places.Where(p => IsFullyContained(p, latMin, latMax, lonMin, lonMax)).ToList();
        }

        private static bool IsFullyContained(Place place, double latMin, double latMax, double lonMin, double lonMax)
        {
            double placeLatMin = Math.Min(place.UpperLeftX1, place.LowerRightX2);
            double placeLatMax = Math.Max(place.UpperLeftX1, place.LowerRightX2);
            double placeLonMin = Math.Min(place.UpperLeftY1, place.LowerRightY2);
            double placeLonMax = Math.Max(place.UpperLeftY1, place.LowerRightY2);

            return placeLatMin >= latMin && placeLatMax <= latMax
                && placeLonMin >= lonMin && placeLonMax <= lonMax;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run:
```bash
/home/fehaar/Unity/Hub/Editor/6000.5.0f1/Editor/Unity -batchmode -nographics \
  -projectPath /home/fehaar/devdrive/AlienOgKoApp/Code/AlienOgKo.Unity \
  -runTests -testPlatform EditMode \
  -testResults /tmp/alienogko-task2-green.xml \
  -logFile /tmp/alienogko-task2-green.log -quit
grep -o 'result="[A-Za-z]*"' /tmp/alienogko-task2-green.xml | head -1
grep "MapBoundsTests" /tmp/alienogko-task2-green.xml
```
Expected: `result="Passed"`, with both `FilterContained_KeepsPlaceFullyInsideBox` and `FilterContained_DropsPlaceThatOnlyOverlaps` showing `result="Passed"`.

- [ ] **Step 5: Commit**

```bash
git add Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/MapBounds.cs* \
        Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/MapBoundsTests.cs*
git commit -m "Add MapBounds containment filter"
```

---

### Task 3: `MapSettings` config asset

**Files:**
- Create: `Code/AlienOgKo.Unity/Assets/Scripts/Settings/MapSettings.cs`
- Create: `Code/AlienOgKo.Unity/Assets/Editor/MapSettingsAssetCreator.cs`

**Interfaces:**
- Produces: `AlienOgKo.MapSettings : ScriptableObject` — `public const string ResourcePath = "MapSettings"`; read-only properties `string BaseUrl`, `string ApiKey`, `double NeLatitude`, `double NeLongitude`, `double SwLatitude`, `double SwLongitude`. Loadable via `Resources.Load<MapSettings>(MapSettings.ResourcePath)`.

- [ ] **Step 1: Create the settings ScriptableObject**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Settings/MapSettings.cs`:

```csharp
using UnityEngine;

namespace AlienOgKo
{
    [CreateAssetMenu(fileName = "MapSettings", menuName = "AlienOgKo/Map Settings")]
    public class MapSettings : ScriptableObject
    {
        public const string ResourcePath = "MapSettings";

        [SerializeField] private string baseUrl = "https://hvor-fanden-er-fanen.osc-fr1.scalingo.io";
        [SerializeField] private string apiKey = "";
        [SerializeField] private double neLatitude = 55.62350;
        [SerializeField] private double neLongitude = 12.07042;
        [SerializeField] private double swLatitude = 55.61706;
        [SerializeField] private double swLongitude = 12.08611;

        public string BaseUrl => baseUrl;
        public string ApiKey => apiKey;
        public double NeLatitude => neLatitude;
        public double NeLongitude => neLongitude;
        public double SwLatitude => swLatitude;
        public double SwLongitude => swLongitude;
    }
}
```

- [ ] **Step 2: Add an Editor tool to create the asset without hand-writing YAML/GUIDs**

Create `Code/AlienOgKo.Unity/Assets/Editor/MapSettingsAssetCreator.cs`:

```csharp
using UnityEditor;
using UnityEngine;

namespace AlienOgKo.EditorTools
{
    public static class MapSettingsAssetCreator
    {
        private const string AssetPath = "Assets/Resources/MapSettings.asset";

        [MenuItem("Gosuman/Create Map Settings Asset")]
        public static void CreateAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<MapSettings>(AssetPath) != null)
            {
                Debug.Log($"{AssetPath} already exists, skipping.");
                return;
            }

            var settings = ScriptableObject.CreateInstance<MapSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {AssetPath}");
        }
    }
}
```

- [ ] **Step 3: Run the creator and verify the asset exists**

Run:
```bash
/home/fehaar/Unity/Hub/Editor/6000.5.0f1/Editor/Unity -batchmode -nographics \
  -projectPath /home/fehaar/devdrive/AlienOgKoApp/Code/AlienOgKo.Unity \
  -executeMethod AlienOgKo.EditorTools.MapSettingsAssetCreator.CreateAsset \
  -logFile /tmp/alienogko-task3-create.log -quit
grep "Created Assets/Resources/MapSettings.asset" /tmp/alienogko-task3-create.log
test -f /home/fehaar/devdrive/AlienOgKoApp/Code/AlienOgKo.Unity/Assets/Resources/MapSettings.asset && echo ASSET_FOUND
```
Expected: log contains `Created Assets/Resources/MapSettings.asset` and the script prints `ASSET_FOUND`. Open the generated `.asset` file and confirm `apiKey:` is blank (empty string) — it must never carry a real key in git.

- [ ] **Step 4: Commit**

```bash
git add Code/AlienOgKo.Unity/Assets/Scripts/Settings/MapSettings.cs* \
        Code/AlienOgKo.Unity/Assets/Editor/MapSettingsAssetCreator.cs* \
        Code/AlienOgKo.Unity/Assets/Resources/MapSettings.asset*
git commit -m "Add MapSettings config asset and creation tool"
```

---

### Task 4: `MapDataProxy` + Bootstrap registration

**Files:**
- Create: `Code/AlienOgKo.Unity/Assets/Scripts/Map/MapDataProxy.cs`
- Modify: `Code/AlienOgKo.Unity/Assets/Bootstrap.cs`

**Interfaces:**
- Consumes: `AlienOgKo.Place` (Task 1), `AlienOgKo.MapBounds.FilterContained(...)` (Task 2), `AlienOgKo.MapSettings` (Task 3, with `BaseUrl`, `ApiKey`, `NeLatitude`, `NeLongitude`, `SwLatitude`, `SwLongitude`).
- Produces: `AlienOgKo.MapDataProxy : PureMVC.Patterns.Proxy.Proxy` — `public static new string NAME = "MapDataProxy"`; `public static class Notifications { public const string PlacesLoaded = "MapDataProxy.PlacesLoaded"; public const string LoadPlacesFailed = "MapDataProxy.LoadPlacesFailed"; }`; constructor `MapDataProxy(MapSettings settings)`; `public IReadOnlyList<Place> Places { get; }`; `public async UniTaskVoid LoadPlaces()`.

- [ ] **Step 1: Implement MapDataProxy**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Map/MapDataProxy.cs`:

```csharp
using Best.HTTP;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using PureMVC.Patterns.Proxy;
using System.Collections.Generic;
using UnityEngine;

namespace AlienOgKo
{
    public class MapDataProxy : Proxy
    {
        public static new string NAME = "MapDataProxy";

        public static class Notifications
        {
            public const string PlacesLoaded = "MapDataProxy.PlacesLoaded";
            public const string LoadPlacesFailed = "MapDataProxy.LoadPlacesFailed";
        }

        private readonly MapSettings settings;

        public IReadOnlyList<Place> Places { get; private set; } = new List<Place>();

        public MapDataProxy(MapSettings settings) : base(NAME)
        {
            this.settings = settings;
        }

        public override void OnRegister()
        {
            base.OnRegister();
            LoadPlaces().Forget();
        }

        public async UniTaskVoid LoadPlaces()
        {
            var request = HTTPRequest.CreateGet($"{settings.BaseUrl}/places/all");
            request.SetHeader("Authorization", settings.ApiKey);
            await request.Send().WithCancellation(Application.exitCancellationToken);

            if (request.State != HTTPRequestStates.Finished || !request.Response.IsSuccess)
            {
                Debug.LogError($"Could not load places. {request.State} {request.Response?.StatusCode}");
                SendNotification(Notifications.LoadPlacesFailed);
                return;
            }

            var allPlaces = JsonConvert.DeserializeObject<List<Place>>(request.Response.DataAsText);
            Places = MapBounds.FilterContained(allPlaces, settings.NeLatitude, settings.NeLongitude, settings.SwLatitude, settings.SwLongitude);
            Debug.Log($"MapDataProxy loaded {Places.Count} places within map bounds");
            SendNotification(Notifications.PlacesLoaded, Places);
        }
    }
}
```

- [ ] **Step 2: Register MapDataProxy in Bootstrap**

In `Code/AlienOgKo.Unity/Assets/Bootstrap.cs`, after the existing `facade.RegisterProxy(new PlayerProxy());` line (line 77), add:

```csharp
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
```

- [ ] **Step 3: Run the EditMode tests to verify the whole project still compiles and all tests pass**

Run:
```bash
/home/fehaar/Unity/Hub/Editor/6000.5.0f1/Editor/Unity -batchmode -nographics \
  -projectPath /home/fehaar/devdrive/AlienOgKoApp/Code/AlienOgKo.Unity \
  -runTests -testPlatform EditMode \
  -testResults /tmp/alienogko-task4-green.xml \
  -logFile /tmp/alienogko-task4-green.log -quit
grep -c "error CS" /tmp/alienogko-task4-green.log
grep -o 'result="[A-Za-z]*"' /tmp/alienogko-task4-green.xml | head -1
```
Expected: `0` compile errors, `result="Passed"` (both `PlaceTests` and `MapBoundsTests` from Tasks 1-2 still pass).

- [ ] **Step 4: Manual smoke test (network call can't be unit-tested without committing a real key)**

In the Unity Editor (not headless): open `Assets/Resources/MapSettings.asset` in the Inspector, paste your dev API key into the `Api Key` field locally (do not stage/commit this change), open any scene that loads at startup, press Play, and confirm the Console shows `MapDataProxy loaded N places within map bounds` (N > 0) with no errors. Afterward, revert the local edit to `MapSettings.asset` (`git checkout -- Code/AlienOgKo.Unity/Assets/Resources/MapSettings.asset`) so the blank `apiKey` stays committed.

- [ ] **Step 5: Commit**

```bash
git add Code/AlienOgKo.Unity/Assets/Scripts/Map/MapDataProxy.cs* \
        Code/AlienOgKo.Unity/Assets/Bootstrap.cs
git commit -m "Add MapDataProxy and register it at startup"
```

---

## Self-Review Notes

- **Spec coverage:** config file → `MapSettings` (Task 3); fetch places from endpoint → `MapDataProxy.LoadPlaces` (Task 4); filter to map zone → `MapBounds.FilterContained` (Task 2), wired into `LoadPlaces`; keep in memory → `Places` property (Task 4); registered at app start → `Bootstrap.cs` (Task 4, Step 2). All four checklist items from issue #14 are covered.
- **Out of scope (left for issue #14's noted follow-up):** wiring `MapMediator`/`MapView` to actually render the places — issue #14 explicitly calls this a separate follow-up.
- **Type consistency checked:** `MapBounds.FilterContained` signature (Task 2) matches exactly how `MapDataProxy.LoadPlaces` calls it (Task 4): `(IEnumerable<Place>, double neLat, double neLon, double swLat, double swLon)`. `MapSettings` property names (`NeLatitude`, `NeLongitude`, `SwLatitude`, `SwLongitude`, `BaseUrl`, `ApiKey`) match their usage in `MapDataProxy` exactly.
