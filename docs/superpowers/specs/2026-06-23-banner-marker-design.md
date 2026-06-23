# Banner Marker on Map — Design

Implements GitHub issue #4 ("Show Alien & Ko banner location on the map").

## Summary

Show the live position of the Alien & Ko banner on `map-venue.png`, sourced from the existing `Hvor Fanden Er Fanen` backend's `GET /trails/latest` endpoint (the API's entire purpose is tracking this banner's reported sightings — it is not new data we need to invent). When the banner's position falls outside the map's configured area (specifically, south of it — into the camping area), hide the marker and show a "taking a break" message instead, so the camp's location stays private ("incognito").

## Background

- `MapDataProxy`/`MapSettings` (shipped in #14) already load a `places` list filtered to a configured NE/SW box (`Docs/mapdata.txt`: NE `55.62350, 12.07042` / SW `55.61706, 12.08611`) and already hold the API base URL + key.
- This same NE/SW box is reused as-is for this feature, unchanged. It was initially suspected to need recalibrating for `map-venue.png` specifically (a investigated, confirmed-by-image-inspection top-half crop of the larger `map.png`), but on reflection the existing box is believed to already align reasonably well with `map-venue.png`; precise on-the-ground calibration is deferred to when the team is physically at the festival. Using the existing, unchanged box keeps `MapDataProxy`'s current 7-place filtered list intact (no regression).
- `MapView`/`MapMediator` (shipped in #14) handle pan/zoom of the map image; `MapMediator` is currently an empty stub.
- `BottomBarView`/`BottomBarMediator` exist as empty stubs and are **not yet placed in the `map.unity` scene** — `Bootstrap.cs` only registers `BottomBarMediator` if `Object.FindAnyObjectByType<BottomBarView>()` finds an instance, which it currently doesn't.
- `MapSceneSetup.cs` (`Assets/Editor/MapSceneSetup.cs`) is an existing Editor tool (`AlienOgKo/Setup Map Scene` menu item) that programmatically builds the map Canvas/Image hierarchy — used here as the established pattern for scene construction rather than hand-editing `.unity` YAML.
- PrimeTween (`com.kyrylokuzyk.primetween`) is an existing project dependency, not yet used anywhere in the codebase — this feature is its first real usage.
- TextMeshPro is not installed in this project (only legacy `com.unity.ugui`) — UI text uses `UnityEngine.UI.Text`.

## Data flow

```
BannerProxy (polls every ~45s)
  → GET {MapSettings.BaseUrl}/trails/latest  (Authorization: MapSettings.ApiKey)
  → deserialize into BannerLocation { Latitude, Longitude }
  → if Latitude < MapSettings.SwLatitude:
        IsAsleep = true  →  SendNotification(BannerProxy.Notifications.BannerAsleep)
    else:
        NormalizedPosition = MapProjection.LatLonToNormalized(Latitude, Longitude, MapSettings.Ne*, MapSettings.Sw*)
        SendNotification(BannerProxy.Notifications.BannerLocationUpdated, NormalizedPosition)

MapMediator (listens for both notifications)
  → BannerLocationUpdated: BannerMarkerView.ShowAt(normalized)
  → BannerAsleep:          BannerMarkerView.Hide()

BottomBarMediator (listens for both notifications)
  → BannerAsleep:          BottomBarView.ShowMessage("Alien er gået på pause 😴")
  → BannerLocationUpdated: BottomBarView.HideMessage()
```

## Components

### `MapProjection` (new)
- File: `Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/MapProjection.cs`
- Assembly: `AlienOgKo.Map.Core` (the existing tested assembly, alongside `Place`/`MapBounds`) — pure, deterministic, unit-tested.
- `public static Vector2 LatLonToNormalized(double lat, double lon, double neLat, double neLon, double swLat, double swLon)`:
  - Normalizes NE/SW via min/max first (mirrors `MapBounds`'s defensive handling of inconsistently-labeled corners — confirmed earlier that this same box has NE longitude < SW longitude).
  - Returns a `Vector2` with both components clamped to `[0, 1]`. X maps longitude (west→east), Y maps latitude (south→north, i.e. `0` = south edge, `1` = north edge — matching Unity UI's bottom-left-origin anchoring convention).
  - Reusable as-is by issue #3 (user location dot) later — same math, different caller.

### `BannerLocation` (new)
- File: `Code/AlienOgKo.Unity/Assets/Scripts/Map/Core/BannerLocation.cs`
- Assembly: `AlienOgKo.Map.Core`.
- POCO deserializing `/trails/latest`'s response: `[JsonProperty("Latitude")] public double Latitude`, `[JsonProperty("Longitude")] public double Longitude`. The response's nested `place` object and `ID`/`TS` fields are not modeled — Newtonsoft ignores unmapped JSON fields by default.

### `BannerProxy` (new)
- File: `Code/AlienOgKo.Unity/Assets/Scripts/Map/BannerProxy.cs`
- Default assembly (like `MapDataProxy`) — not unit tested, matching the established convention that network-integration proxies aren't unit tested in this codebase.
- PureMVC `Proxy`, constructed with `MapSettings`. `OnRegister()` starts an infinite poll loop (`UniTaskVoid`, `Application.exitCancellationToken`-scoped, ~45s delay between iterations) using the same `Best.HTTP` + `Authorization` header pattern as `MapDataProxy`/`ServerProxy`.
- Exposes `IsAsleep` (bool) and `NormalizedPosition` (`Vector2?`) for any late-joining observer, plus the two notifications described in Data Flow above.

### `BannerMarkerView` (new)
- File: `Code/AlienOgKo.Unity/Assets/Scripts/Map/BannerMarkerView.cs`
- Default assembly, `MonoBehaviour`, lives as a child GameObject under the existing "MapImage" GameObject — inherits `MapView`'s pan/zoom transform for free, no extra code needed for that.
- `[SerializeField] private Image icon` — placeholder colored circle sprite for now (no dedicated art exists yet); swap the sprite reference once real art is provided.
- `public void ShowAt(Vector2 normalized)` — converts the normalized position to `anchoredPosition` using the parent `RectTransform`'s `rect.size`, activates the GameObject.
- `public void Hide()` — deactivates the GameObject (used when asleep — there is no valid position to show).

### `MapMediator` (modify)
- File: `Code/AlienOgKo.Unity/Assets/Bootstrap.cs` (registration) and `Code/AlienOgKo.Unity/Assets/Scripts/Map/MapMediator.cs`.
- Constructor gains a `BannerMarkerView` parameter alongside the existing `MapView` one: `MapMediator(MapView view, BannerMarkerView bannerMarker)`.
- `ListNotificationInterests()` returns `BannerProxy.Notifications.BannerLocationUpdated`, `BannerProxy.Notifications.BannerAsleep`. `HandleNotification` dispatches to `bannerMarker.ShowAt(...)` / `bannerMarker.Hide()` accordingly.

### `BottomBarView` / `BottomBarMediator` (modify)
- Files: `Code/AlienOgKo.Unity/Assets/Scripts/UI/BottomBarView.cs`, `BottomBarMediator.cs`.
- `BottomBarView` gains `[SerializeField] private RectTransform messagePanel`, `[SerializeField] private Text messageText`, `public void ShowMessage(string text)` (sets text, tweens `messagePanel`'s anchored Y from off-screen-below to its resting position via PrimeTween), `public void HideMessage()` (tweens back down).
- `BottomBarMediator.ListNotificationInterests()` returns the same two `BannerProxy.Notifications.*` constants; dispatches to `ShowMessage("Alien er gået på pause 😴")` / `HideMessage()`.

### `MapSceneSetup.cs` (modify)
- File: `Code/AlienOgKo.Unity/Assets/Editor/MapSceneSetup.cs`.
- Extend the existing `Setup()` method to also:
  - Create a `BannerMarker` child GameObject under `MapImage` with `BannerMarkerView` + a placeholder circle `Image`, initially inactive.
  - Create a `BottomBar` GameObject (Canvas child, anchored to the bottom of the screen) with `BottomBarView` + a child `Text` element (`messagePanel`/`messageText`), initially positioned off-screen below.

### `Bootstrap.cs` (modify)
- Register `BannerProxy` (constructed with the same `mapSettings` already loaded for `MapDataProxy`).
- Update the `MapMediator` construction call to also look up `BannerMarkerView` via `Object.FindAnyObjectByType<BannerMarkerView>()` and pass it through.
- The existing `Object.FindAnyObjectByType<BottomBarView>()` + `RegisterMediator(new BottomBarMediator(...))` call already exists in `Bootstrap.cs` from prior scaffolding — once `MapSceneSetup.cs` actually places a `BottomBarView` in the scene, this registration will start firing for the first time.

## Error handling

- `BannerProxy`'s poll loop follows `MapDataProxy`'s existing pattern: on HTTP failure or null/failed deserialization, log the error and skip that cycle (retry on the next poll ~45s later) rather than crashing the loop. No new notification type for "fetch failed" — the marker/message simply stay in their last-known state until the next successful poll.
- `MapProjection.LatLonToNormalized`'s clamping to `[0,1]` means a position east/west/north of the configured box pins to that edge rather than rendering off-screen; only south-of-box is treated as the special "asleep" case, per the explicit camping-privacy requirement.

## Testing

- `MapProjection.LatLonToNormalized`: unit-tested in `AlienOgKo.Map.Tests` (EditMode), following the same TDD pattern as `MapBoundsTests` — real coordinates (e.g. Apollo's position, the configured box) with known expected normalized output, plus a case confirming the NE/SW longitude-inversion quirk is handled.
- `BannerLocation` deserialization: unit-tested against the real `/trails/latest` response shape (captured during this session), same pattern as `PlaceTests`.
- `BannerProxy`, `BannerMarkerView`, `BottomBarView`/`Mediator`, and the `MapSceneSetup.cs` changes: not unit tested, consistent with the rest of the PureMVC proxy/view/scene-setup layer in this codebase. Verified via the same manual Play-mode smoke test pattern used for `MapDataProxy` (and, this time, achievable headlessly via the `unity-mcp` bridge now available in this project).

## Out of scope (explicitly deferred)

- Tap-to-show-tooltip on the marker (mentioned as optional in issue #4) — fast follow-up once the marker itself works.
- A real marker icon/sprite — placeholder colored circle for now; swap in once art is provided.
- Precise on-the-ground recalibration of the NE/SW box specifically for `map-venue.png` — deferred to when the team is physically at the festival.
- Issue #3 (user's own GPS location dot) — a separate issue, though it will reuse `MapProjection` from this work.
