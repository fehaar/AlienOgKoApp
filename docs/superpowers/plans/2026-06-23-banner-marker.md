# Banner Marker on Map Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show the live position of the Alien & Ko banner on `map-venue.png`, sourced from `GET /trails/latest`, and show a "taking a break" message in the bottom bar instead when the banner is south of the configured map area (the camping zone, kept private).

**Architecture:** A new `BannerProxy` polls `/trails/latest` every ~45s and fires one of two PureMVC notifications depending on whether the reported latitude is inside or south of `MapSettings`'s existing NE/SW box. `MapMediator` reacts by showing/hiding a new `BannerMarkerView` (a child of the existing map image, inheriting its pan/zoom for free). `BottomBarMediator` reacts by fading a message `Text` in/out via PrimeTween (bumped from 1.4.0 to 1.4.8 as part of this work, since this is its first real usage). The underlying lat/lon→screen-position math is a small, pure, unit-tested utility (`MapProjection`), reusable by issue #3 later.

**Tech Stack:** PureMVC (existing), Best.HTTP + UniTask (existing, same pattern as `MapDataProxy`), PrimeTween 1.4.8 (existing dependency, first real usage), Newtonsoft.Json (existing).

## Global Constraints

- Full design background and rationale: `docs/superpowers/specs/2026-06-23-banner-marker-design.md`. Read it for the "why" behind decisions below; this plan only restates the binding "what."
- **Do not add new fields to `MapSettings`.** Reuse its existing `NeLatitude`/`NeLongitude`/`SwLatitude`/`SwLongitude` exactly as-is for both the existing places-filtering (unchanged) and this feature's projection/asleep-threshold math. Confirmed: keeping these values unchanged preserves `MapDataProxy`'s current 7-place filtered list (no regression).
- **"Asleep" semantics:** if a fetched `Latitude < MapSettings.SwLatitude`, the banner is south of the configured area (the camping zone) → fire `BannerProxy.Notifications.BannerAsleep` and show the bottom-bar message; do not attempt to compute or show a map position in this case.
- **Endpoint:** `GET {MapSettings.BaseUrl}/trails/latest`, `Authorization` header = `MapSettings.ApiKey` (same auth pattern as `MapDataProxy`). Poll every 45 seconds.
- **Normalized position convention:** `MapProjection.LatLonToNormalized` returns a `Vector2` clamped to `[0,1]` on both axes. X = longitude (west→east). Y = latitude (south→north: `0` = south/SW edge, `1` = north/NE edge) — matches Unity UI's bottom-left anchor origin.
- New pure-logic code (`MapProjection`, `BannerLocation`) goes in the existing tested assembly `AlienOgKo.Map.Core` (`Assets/Scripts/Map/Core/`), alongside `Place`/`MapBounds`. New untested integration/view code (`BannerProxy`, `BannerMarkerView`) goes in `Assets/Scripts/Map/` (default assembly), matching `MapDataProxy`/`MapView`'s existing placement — **no automated test for these**, consistent with the established convention that no PureMVC `Proxy`/view class has a unit test in this codebase.
- **`MapSceneSetup.cs` has its own active development happening outside this plan** (it already independently gained `map-venue.png`, dynamic aspect ratio, and permanent `TopBar`/`BottomBar` bars since this plan was drafted) — **re-read the live file's current content before editing it in Task 4**; do not assume the snippets below are still character-for-character accurate, only structurally accurate (what GameObjects/components exist, in what hierarchy).
- `BottomBar` is a **permanent, always-visible** bar (not hidden/off-screen by default) — the asleep message is a `Text` child inside it that **fades alpha in/out** (not a positional slide), starting at alpha `0`.
- PrimeTween bump: replace `Code/AlienOgKo.Unity/Assets/Plugins/PrimeTween/internal/com.kyrylokuzyk.primetween.tgz` (currently 1.4.0) with 1.4.8, downloaded from `https://registry.npmjs.org/com.kyrylokuzyk.primetween/-/com.kyrylokuzyk.primetween-1.4.8.tgz` (sha1 `d86bb6572edd55c78342155a510f8d8c429f81ab` — verify before replacing). Same bundling convention as the existing file (a local `.tgz`, not a registry reference in `manifest.json`).
- Unity Editor lock: check `Code/AlienOgKo.Unity/Temp/UnityLockfile` before any batchmode command. If locked, use the pre-warmed sandbox at `/tmp/alienogko-verify` (`rsync -a --delete .../Code/AlienOgKo.Unity/Assets/ /tmp/alienogko-verify/Assets/`, then `-projectPath /tmp/alienogko-verify`); never pass `-quit` together with `-runTests` on the sandbox (it races). If free, run directly against the real path (`-quit` is fine there).

---

### Task 1: MapProjection + BannerLocation (pure data layer)

**Files:**
- Create: `Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/MapProjection.cs`
- Create: `Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/BannerLocation.cs`
- Test: `Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/MapProjectionTests.cs`
- Test: `Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/BannerLocationTests.cs`

**Interfaces:**
- Produces: `AlienOgKo.MapProjection.LatLonToNormalized(double lat, double lon, double neLatitude, double neLongitude, double swLatitude, double swLongitude) : UnityEngine.Vector2` (clamped `[0,1]` both axes).
- Produces: `AlienOgKo.BannerLocation` — public properties `double Latitude`, `double Longitude`. Deserializable via `Newtonsoft.Json.JsonConvert.DeserializeObject<BannerLocation>(...)`.

- [ ] **Step 1: Write the failing tests for MapProjection**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/MapProjectionTests.cs`. All four cases use the project's real configured box (`Docs/mapdata.txt`: NE `55.62350, 12.07042` / SW `55.61706, 12.08611`) — note NE's longitude is numerically *less* than SW's, the same inconsistent-labeling quirk `MapBounds` already defends against:

```csharp
using NUnit.Framework;
using UnityEngine;

namespace AlienOgKo.Tests
{
    public class MapProjectionTests
    {
        const double NeLat = 55.62350, NeLon = 12.07042, SwLat = 55.61706, SwLon = 12.08611;

        [Test]
        public void LatLonToNormalized_NeCorner_MapsToWestNorth()
        {
            var result = MapProjection.LatLonToNormalized(NeLat, NeLon, NeLat, NeLon, SwLat, SwLon);

            Assert.AreEqual(0f, result.x, 1e-5f);
            Assert.AreEqual(1f, result.y, 1e-5f);
        }

        [Test]
        public void LatLonToNormalized_SwCorner_MapsToEastSouth()
        {
            var result = MapProjection.LatLonToNormalized(SwLat, SwLon, NeLat, NeLon, SwLat, SwLon);

            Assert.AreEqual(1f, result.x, 1e-5f);
            Assert.AreEqual(0f, result.y, 1e-5f);
        }

        [Test]
        public void LatLonToNormalized_Midpoint_MapsToCenter()
        {
            double midLat = (NeLat + SwLat) / 2;
            double midLon = (NeLon + SwLon) / 2;

            var result = MapProjection.LatLonToNormalized(midLat, midLon, NeLat, NeLon, SwLat, SwLon);

            Assert.AreEqual(0.5f, result.x, 1e-5f);
            Assert.AreEqual(0.5f, result.y, 1e-5f);
        }

        [Test]
        public void LatLonToNormalized_PointSouthOfBox_ClampsToZero()
        {
            // Grøn's southern edge (55.618803) — confirmed earlier to sit south of this box.
            var result = MapProjection.LatLonToNormalized(55.618803, 12.0842885, NeLat, NeLon, SwLat, SwLon);

            Assert.AreEqual(0f, result.y, 1e-5f);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run (substitute the real/sandbox project path and `-quit` handling per Global Constraints):
```bash
/home/fehaar/Unity/Hub/Editor/6000.5.0f1/Editor/Unity -batchmode -nographics \
  -projectPath /tmp/alienogko-verify \
  -runTests -testPlatform EditMode \
  -testResults /tmp/alienogko-banner-task1-red.xml \
  -logFile /tmp/alienogko-banner-task1-red.log
grep -c "error CS" /tmp/alienogko-banner-task1-red.log
```
Expected: at least one `error CS0246: The type or namespace name 'MapProjection' could not be found`.

- [ ] **Step 3: Implement MapProjection**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/MapProjection.cs`:

```csharp
using System;
using UnityEngine;

namespace AlienOgKo
{
    public static class MapProjection
    {
        public static Vector2 LatLonToNormalized(
            double lat, double lon, double neLatitude, double neLongitude, double swLatitude, double swLongitude)
        {
            double latMin = Math.Min(neLatitude, swLatitude);
            double latMax = Math.Max(neLatitude, swLatitude);
            double lonMin = Math.Min(neLongitude, swLongitude);
            double lonMax = Math.Max(neLongitude, swLongitude);

            double x = (lon - lonMin) / (lonMax - lonMin);
            double y = (lat - latMin) / (latMax - latMin);

            return new Vector2(Mathf.Clamp01((float)x), Mathf.Clamp01((float)y));
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run the same command as Step 2 (fresh `-testResults`/`-logFile` paths, e.g. `...task1-green...`).
Expected: `0` compile errors, all 4 `MapProjectionTests` show `result="Passed"`.

- [ ] **Step 5: Write the failing test for BannerLocation**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/BannerLocationTests.cs`. The JSON is the real `/trails/latest` response shape captured during this project's development:

```csharp
using Newtonsoft.Json;
using NUnit.Framework;

namespace AlienOgKo.Tests
{
    public class BannerLocationTests
    {
        [Test]
        public void Deserialize_ParsesRealApiShape()
        {
            const string json = @"{
                ""ID"": 42218,
                ""Latitude"": 55.67375946,
                ""Longitude"": 12.48732662,
                ""TS"": ""2026-06-23T11:59:39.000Z"",
                ""place"": {
                    ""ID"": 9,
                    ""TS"": ""2026-02-23T11:04:15.000Z"",
                    ""fixed"": true,
                    ""lowerRightX2"": 55.630924,
                    ""lowerRightY2"": 12.538649,
                    ""tag"": ""Valby"",
                    ""upperLeftX1"": 55.67723,
                    ""upperLeftY1"": 12.481308
                }
            }";

            var location = JsonConvert.DeserializeObject<BannerLocation>(json);

            Assert.AreEqual(55.67375946, location.Latitude, 1e-6);
            Assert.AreEqual(12.48732662, location.Longitude, 1e-6);
        }
    }
}
```

- [ ] **Step 6: Run the test to verify it fails**

Same command shape as Step 2. Expected: `error CS0246: The type or namespace name 'BannerLocation' could not be found`.

- [ ] **Step 7: Implement BannerLocation**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/BannerLocation.cs`:

```csharp
using Newtonsoft.Json;

namespace AlienOgKo
{
    public class BannerLocation
    {
        [JsonProperty("Latitude")]
        public double Latitude { get; set; }

        [JsonProperty("Longitude")]
        public double Longitude { get; set; }
    }
}
```

- [ ] **Step 8: Run the tests to verify they pass**

Same command shape as Step 2 (fresh result paths). Expected: `0` compile errors, `result="Passed"` for both `MapProjectionTests` and `BannerLocationTests` (6 tests total, plus the existing `PlaceTests`/`MapBoundsTests` from prior work still passing).

- [ ] **Step 9: Commit**

```bash
git add Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/MapProjection.cs* \
        Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/BannerLocation.cs* \
        Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/MapProjectionTests.cs* \
        Code/AlienOgKo.Unity/Assets/Scripts/Map/Tests/BannerLocationTests.cs*
git commit -m "Add MapProjection and BannerLocation for banner-marker feature"
```

(Check `git status --porcelain Code/AlienOgKo.Unity/Assets/Scripts/Map/` first and include any auto-generated `.meta` files in the `git add`.)

---

### Task 2: BannerProxy + Bootstrap registration

**Files:**
- Create: `Code/AlienOgKo.Unity/Assets/Scripts/Map/BannerProxy.cs`
- Modify: `Code/AlienOgKo.Unity/Assets/Bootstrap.cs`

**Interfaces:**
- Consumes: `AlienOgKo.MapProjection.LatLonToNormalized(...)` (Task 1), `AlienOgKo.BannerLocation` (Task 1), `AlienOgKo.MapSettings` (existing: `BaseUrl`, `ApiKey`, `NeLatitude`, `NeLongitude`, `SwLatitude`, `SwLongitude`).
- Produces: `AlienOgKo.BannerProxy : PureMVC.Patterns.Proxy.Proxy` — `public static new string NAME = "BannerProxy"`; `public static class Notifications { public const string BannerLocationUpdated = "BannerProxy.BannerLocationUpdated"; public const string BannerAsleep = "BannerProxy.BannerAsleep"; }`; constructor `BannerProxy(MapSettings settings)`; `public bool IsAsleep { get; }`; `public Vector2? NormalizedPosition { get; }`.

- [ ] **Step 1: Implement BannerProxy**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Map/BannerProxy.cs`:

```csharp
using Best.HTTP;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using PureMVC.Patterns.Proxy;
using UnityEngine;

namespace AlienOgKo
{
    public class BannerProxy : Proxy
    {
        public static new string NAME = "BannerProxy";

        public static class Notifications
        {
            public const string BannerLocationUpdated = "BannerProxy.BannerLocationUpdated";
            public const string BannerAsleep = "BannerProxy.BannerAsleep";
        }

        private const int PollIntervalMs = 45_000;

        private readonly MapSettings settings;

        public bool IsAsleep { get; private set; }
        public Vector2? NormalizedPosition { get; private set; }

        public BannerProxy(MapSettings settings) : base(NAME)
        {
            this.settings = settings;
        }

        public override void OnRegister()
        {
            base.OnRegister();
            PollLoop().Forget();
        }

        private async UniTaskVoid PollLoop()
        {
            while (true)
            {
                await FetchOnce();
                await UniTask.Delay(PollIntervalMs, cancellationToken: Application.exitCancellationToken);
            }
        }

        private async UniTask FetchOnce()
        {
            var request = HTTPRequest.CreateGet($"{settings.BaseUrl}/trails/latest");
            request.SetHeader("Authorization", settings.ApiKey);
            await request.Send().WithCancellation(Application.exitCancellationToken);

            if (request.State != HTTPRequestStates.Finished || !request.Response.IsSuccess)
            {
                Debug.LogError($"Could not load banner location. {request.State} {request.Response?.StatusCode} {request.Exception}");
                return;
            }

            var location = JsonConvert.DeserializeObject<BannerLocation>(request.Response.DataAsText);
            if (location == null)
            {
                Debug.LogError("Banner location response could not be parsed.");
                return;
            }

            if (location.Latitude < settings.SwLatitude)
            {
                IsAsleep = true;
                NormalizedPosition = null;
                SendNotification(Notifications.BannerAsleep);
            }
            else
            {
                IsAsleep = false;
                NormalizedPosition = MapProjection.LatLonToNormalized(
                    location.Latitude, location.Longitude,
                    settings.NeLatitude, settings.NeLongitude, settings.SwLatitude, settings.SwLongitude);
                SendNotification(Notifications.BannerLocationUpdated, NormalizedPosition.Value);
            }
        }
    }
}
```

- [ ] **Step 2: Register BannerProxy in Bootstrap**

In `Code/AlienOgKo.Unity/Assets/Bootstrap.cs`, change:

```csharp
            else
            {
                facade.RegisterProxy(new MapDataProxy(mapSettings));
            }
```

to:

```csharp
            else
            {
                facade.RegisterProxy(new MapDataProxy(mapSettings));
                facade.RegisterProxy(new BannerProxy(mapSettings));
            }
```

- [ ] **Step 3: Verify the project compiles with 0 errors**

Check the Editor lock state, then run the EditMode suite (same command shape as Task 1's steps, against whichever path the lock state dictates). Expected: `0` compile errors, all 6 tests from Task 1 (plus prior `PlaceTests`/`MapBoundsTests`) still `result="Passed"` — this task adds no new automated test (per Global Constraints, `BannerProxy` is untested integration glue like `MapDataProxy`).

- [ ] **Step 4: Commit**

```bash
git add Code/AlienOgKo.Unity/Assets/Scripts/Map/BannerProxy.cs* \
        Code/AlienOgKo.Unity/Assets/Bootstrap.cs
git commit -m "Add BannerProxy and register it at startup"
```

---

### Task 3: View components — BannerMarkerView, BottomBarView, and the PrimeTween bump

**Files:**
- Create: `Code/AlienOgKo.Unity/Assets/Scripts/Map/BannerMarkerView.cs`
- Modify: `Code/AlienOgKo.Unity/Assets/Scripts/UI/BottomBarView.cs`
- Replace: `Code/AlienOgKo.Unity/Assets/Plugins/PrimeTween/internal/com.kyrylokuzyk.primetween.tgz`

**Interfaces:**
- Produces: `AlienOgKo.BannerMarkerView : MonoBehaviour` — `public void ShowAt(Vector2 normalized)`, `public void Hide()`. Expects to be placed as a child of a `RectTransform` whose `rect.size` defines the area to position within (e.g. the map image), with its own anchors/pivot at `(0.5, 0.5)`.
- Modifies: `AlienOgKo.BottomBarView` — adds `public void ShowMessage(string text)`, `public void HideMessage()`.

- [ ] **Step 1: Bump PrimeTween from 1.4.0 to 1.4.8**

Download and verify the official package tarball before replacing the bundled file:

```bash
curl -sS -o /tmp/primetween-1.4.8.tgz "https://registry.npmjs.org/com.kyrylokuzyk.primetween/-/com.kyrylokuzyk.primetween-1.4.8.tgz"
sha1sum /tmp/primetween-1.4.8.tgz
```
Expected sha1: `d86bb6572edd55c78342155a510f8d8c429f81ab`. If it doesn't match, stop and report BLOCKED rather than proceeding with an unverified file.

```bash
cp /tmp/primetween-1.4.8.tgz /home/fehaar/devdrive/AlienOgKoApp/Code/AlienOgKo.Unity/Assets/Plugins/PrimeTween/internal/com.kyrylokuzyk.primetween.tgz
```

This is a drop-in replacement — both versions package as a `package/` root tarball, and `Packages/manifest.json` already references this file by path (`file:../Assets/Plugins/PrimeTween/internal/com.kyrylokuzyk.primetween.tgz`), not by version, so no manifest change is needed.

- [ ] **Step 2: Implement BannerMarkerView**

Create `Code/AlienOgKo.Unity/Assets/Scripts/Map/BannerMarkerView.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace AlienOgKo
{
    [RequireComponent(typeof(RectTransform))]
    public class BannerMarkerView : MonoBehaviour
    {
        [SerializeField] private Image icon;

        private RectTransform rt;
        private RectTransform parentRt;

        void Awake()
        {
            rt = GetComponent<RectTransform>();
            parentRt = (RectTransform)rt.parent;
            if (icon == null)
                icon = GetComponent<Image>();
        }

        public void ShowAt(Vector2 normalized)
        {
            Vector2 size = parentRt.rect.size;
            rt.anchoredPosition = new Vector2(
                (normalized.x - 0.5f) * size.x,
                (normalized.y - 0.5f) * size.y);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
```

- [ ] **Step 3: Add fade in/out to BottomBarView**

Read the current content of `Code/AlienOgKo.Unity/Assets/Scripts/UI/BottomBarView.cs` first (it may have changed since this plan was written — re-check before editing). As of this plan, it is:

```csharp
using UnityEngine;

namespace AlienOgKo
{
    public class BottomBarView : MonoBehaviour
    {
    }
}
```

Replace it with:

```csharp
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace AlienOgKo
{
    public class BottomBarView : MonoBehaviour
    {
        [SerializeField] private Text messageText;

        private const float FadeDuration = 0.3f;

        void Awake()
        {
            if (messageText == null)
                messageText = GetComponentInChildren<Text>();
        }

        public void ShowMessage(string text)
        {
            messageText.text = text;
            Tween.Alpha(messageText, 1f, FadeDuration);
        }

        public void HideMessage()
        {
            Tween.Alpha(messageText, 0f, FadeDuration);
        }
    }
}
```

If the live file already has other content beyond the empty stub shown above (per Global Constraints, scene-related files have been under active parallel development), preserve whatever else is there and add only the fields/methods shown — report DONE_WITH_CONCERNS rather than silently dropping unrelated content if the merge isn't obvious.

- [ ] **Step 4: Verify the project compiles with 0 errors**

Check the Editor lock state, then run the EditMode suite. Expected: `0` compile errors (this confirms the PrimeTween 1.4.8 API — `Tween.Alpha(Graphic, float, float)` — still resolves; it was verified against the 1.4.8 source directly while writing this plan, but the compile check is the authoritative confirmation), all prior tests still passing. No new automated test in this task (`BannerMarkerView`/`BottomBarView` are untested view components, consistent with `MapView`).

- [ ] **Step 5: Commit**

```bash
git add Code/AlienOgKo.Unity/Assets/Scripts/Map/BannerMarkerView.cs* \
        Code/AlienOgKo.Unity/Assets/Scripts/UI/BottomBarView.cs \
        Code/AlienOgKo.Unity/Assets/Plugins/PrimeTween/internal/com.kyrylokuzyk.primetween.tgz
git commit -m "Add BannerMarkerView, fade-based BottomBarView messages, bump PrimeTween to 1.4.8"
```

---

### Task 4: Scene wiring + Mediator notification wiring

**Files:**
- Modify: `Code/AlienOgKo.Unity/Assets/Editor/MapSceneSetup.cs`
- Modify: `Code/AlienOgKo.Unity/Assets/Scripts/Map/MapMediator.cs`
- Modify: `Code/AlienOgKo.Unity/Assets/Scripts/UI/BottomBarMediator.cs`
- Modify: `Code/AlienOgKo.Unity/Assets/Bootstrap.cs`

**Interfaces:**
- Consumes: `BannerProxy.Notifications.*` (Task 2), `BannerMarkerView.ShowAt/Hide` (Task 3), `BottomBarView.ShowMessage/HideMessage` (Task 3).

- [ ] **Step 1: Re-read MapSceneSetup.cs's current content**

This file has had active, independent changes during this plan's drafting. Read it fresh now (`Code/AlienOgKo.Unity/Assets/Editor/MapSceneSetup.cs`) before editing — do not assume the structure described below is still exact, only that it's the latest known shape: a `MapContainer` holding `MapImage` (with `MapView`), plus permanent `TopBar` and `BottomBar` bars (each with a background `Image` and their respective view component already attached).

- [ ] **Step 2: Add the BannerMarker child under MapImage**

Inside `Setup()`, immediately after the line that adds `MapView` to `imageGO` (`imageGO.AddComponent<MapView>();`), add:

```csharp
        // Banner marker — child of MapImage so it inherits pan/zoom
        var markerGO = new GameObject("BannerMarker");
        markerGO.transform.SetParent(imageGO.transform, false);
        var markerImage = markerGO.AddComponent<Image>();
        markerImage.color = Color.red;
        var markerRt = markerGO.GetComponent<RectTransform>();
        markerRt.anchorMin = new Vector2(0.5f, 0.5f);
        markerRt.anchorMax = new Vector2(0.5f, 0.5f);
        markerRt.pivot = new Vector2(0.5f, 0.5f);
        markerRt.sizeDelta = new Vector2(40f, 40f);
        markerGO.AddComponent<BannerMarkerView>();
        markerGO.SetActive(false);
```

(`imageGO` here refers to whatever local variable name the live file currently uses for the map image GameObject — confirm the exact name when you re-read the file in Step 1.)

- [ ] **Step 3: Add the MessageText child under BottomBar**

Immediately after the line that adds `BottomBarView` to the bottom bar GameObject (e.g. `bottomBarGO.AddComponent<BottomBarView>();`), add:

```csharp
        var messageGO = new GameObject("MessageText");
        messageGO.transform.SetParent(bottomBarGO.transform, false);
        var messageText = messageGO.AddComponent<Text>();
        messageText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = new Color(1f, 1f, 1f, 0f);
        var messageRt = messageGO.GetComponent<RectTransform>();
        messageRt.anchorMin = Vector2.zero;
        messageRt.anchorMax = Vector2.one;
        messageRt.sizeDelta = Vector2.zero;
```

(Again, substitute the live file's actual bottom-bar GameObject variable name.) The starting alpha of `0` means the message is invisible until `BottomBarView.ShowMessage` fades it in.

- [ ] **Step 4: Wire MapMediator to the new notifications**

Replace `Code/AlienOgKo.Unity/Assets/Scripts/Map/MapMediator.cs`'s content with:

```csharp
using PureMVC.Interfaces;
using PureMVC.Patterns.Mediator;
using UnityEngine;

namespace AlienOgKo
{
    public class MapMediator : Mediator
    {
        public new const string NAME = nameof(MapMediator);

        private readonly BannerMarkerView bannerMarker;

        MapView MapView => (MapView)ViewComponent;

        public MapMediator(MapView view, BannerMarkerView bannerMarker) : base(NAME, view)
        {
            this.bannerMarker = bannerMarker;
        }

        public override string[] ListNotificationInterests() => new[]
        {
            BannerProxy.Notifications.BannerLocationUpdated,
            BannerProxy.Notifications.BannerAsleep
        };

        public override void HandleNotification(INotification notification)
        {
            switch (notification.Name)
            {
                case BannerProxy.Notifications.BannerLocationUpdated:
                    bannerMarker.ShowAt((Vector2)notification.Body);
                    break;
                case BannerProxy.Notifications.BannerAsleep:
                    bannerMarker.Hide();
                    break;
            }
        }
    }
}
```

- [ ] **Step 5: Wire BottomBarMediator to the new notifications**

Replace `Code/AlienOgKo.Unity/Assets/Scripts/UI/BottomBarMediator.cs`'s content with:

```csharp
using PureMVC.Interfaces;
using PureMVC.Patterns.Mediator;

namespace AlienOgKo
{
    public class BottomBarMediator : Mediator
    {
        public new const string NAME = nameof(BottomBarMediator);

        BottomBarView BottomBarView => (BottomBarView)ViewComponent;

        public BottomBarMediator(BottomBarView view) : base(NAME, view) { }

        public override string[] ListNotificationInterests() => new[]
        {
            BannerProxy.Notifications.BannerAsleep,
            BannerProxy.Notifications.BannerLocationUpdated
        };

        public override void HandleNotification(INotification notification)
        {
            switch (notification.Name)
            {
                case BannerProxy.Notifications.BannerAsleep:
                    BottomBarView.ShowMessage("Alien er gået på pause 😴");
                    break;
                case BannerProxy.Notifications.BannerLocationUpdated:
                    BottomBarView.HideMessage();
                    break;
            }
        }
    }
}
```

- [ ] **Step 6: Update Bootstrap's MapMediator construction**

In `Code/AlienOgKo.Unity/Assets/Bootstrap.cs`, change:

```csharp
            var mapView = Object.FindAnyObjectByType<MapView>();
            if (mapView != null)
                facade.RegisterMediator(new MapMediator(mapView));
```

to:

```csharp
            var mapView = Object.FindAnyObjectByType<MapView>();
            if (mapView != null)
            {
                var bannerMarker = Object.FindAnyObjectByType<BannerMarkerView>(FindObjectsInactive.Include);
                facade.RegisterMediator(new MapMediator(mapView, bannerMarker));
            }
```

`FindObjectsInactive.Include` is required here because `BannerMarker` starts inactive (`markerGO.SetActive(false)` in `MapSceneSetup.cs`) — without it, `FindAnyObjectByType` would return `null` and `MapMediator` would NRE the first time a notification arrives.

The existing `BottomBarMediator` registration block (lines querying `Object.FindAnyObjectByType<BottomBarView>()`) needs no change — it already exists and will start actually firing now that `BottomBarView` is genuinely present in the scene.

- [ ] **Step 7: Run the EditMode scene-setup tool and verify**

Check the Editor lock state (per Global Constraints).

If **free** (you can run batchmode directly against the real project):
```bash
/home/fehaar/Unity/Hub/Editor/6000.5.0f1/Editor/Unity -batchmode -nographics \
  -projectPath /home/fehaar/devdrive/AlienOgKoApp/Code/AlienOgKo.Unity \
  -executeMethod MapSceneSetup.Setup \
  -logFile /tmp/alienogko-banner-scenesetup.log -quit
grep -c "error CS" /tmp/alienogko-banner-scenesetup.log
grep "AlienOgKo: Map scene setup complete" /tmp/alienogko-banner-scenesetup.log
```
Expected: `0` compile errors, the completion log line present.

Then run the EditMode test suite the same way as prior tasks. Expected: `0` compile errors, all tests still passing.

If **locked**, run both the scene-setup `-executeMethod` call and the EditMode suite against the sandbox (`/tmp/alienogko-verify`) instead, syncing first as in prior tasks. Note: `-executeMethod` is not a test run, so `-quit` is safe to combine with it even on the sandbox (the `-quit`-races-`-runTests` issue is specific to `-runTests`).

- [ ] **Step 8: Manual follow-up (deferred to a human, or via the unity-mcp bridge now available in this project)**

With the local Nakama-adjacent backend reachable and a real `/trails/latest` value available, open/reload the scene in Play mode and confirm: if the live banner position is north of `MapSettings.SwLatitude`, `BannerMarker` appears at a plausible position on the map and no error appears in the Console; if south of it, `BannerMarker` is hidden and the bottom bar's message fades in. This can be checked live via the `mcp__unity-mcp__Unity_ReadConsole`/`Unity_ManageEditor` tools against the running Editor instead of requiring a human to press Play manually, if the Editor is open and on this branch.

- [ ] **Step 9: Commit**

```bash
git add Code/AlienOgKo.Unity/Assets/Editor/MapSceneSetup.cs \
        Code/AlienOgKo.Unity/Assets/Scripts/Map/MapMediator.cs \
        Code/AlienOgKo.Unity/Assets/Scripts/UI/BottomBarMediator.cs \
        Code/AlienOgKo.Unity/Assets/Bootstrap.cs
git commit -m "Wire BannerMarkerView and BottomBar message into the scene and mediators"
```

---

## Self-Review Notes

- **Spec coverage:** all design-doc sections have a task — data layer (Task 1), fetching/asleep-detection (Task 2), view components + PrimeTween bump (Task 3), scene construction + mediator wiring (Task 4). Out-of-scope items from the spec (tap tooltip, real marker art, on-the-ground recalibration, issue #3) are correctly not tasked here.
- **Type consistency checked:** `MapProjection.LatLonToNormalized`'s signature (Task 1) matches its call site in `BannerProxy.FetchOnce` (Task 2) argument-for-argument. `BannerProxy.Notifications.*` names match exactly between `BannerProxy` (Task 2) and both `MapMediator`/`BottomBarMediator` (Task 4). `BannerMarkerView.ShowAt/Hide` and `BottomBarView.ShowMessage/HideMessage` (Task 3) match their call sites in Task 4 exactly.
- **Known volatility flagged explicitly:** `MapSceneSetup.cs` and `BottomBarView.cs` have had independent, parallel changes during this plan's drafting (confirmed by re-reading them live twice and finding different content each time). Task 3/4 both instruct re-reading the live file before editing rather than trusting the snippets blindly — this is a deliberate, repeated instruction, not an oversight.
