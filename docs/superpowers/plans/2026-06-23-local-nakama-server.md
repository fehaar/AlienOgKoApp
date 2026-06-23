# Local Nakama Server Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up a self-hosted local Nakama instance via Docker Compose so the Unity client's existing `NakamaAuthBackend` (already implemented and configured to point at `localhost:7350`) has something real to authenticate against — closing out the remainder of issue #11.

**Architecture:** A `docker-compose.yml` under `Backend/` runs two services: `cockroachdb` (Nakama's database) and `nakama` (the game server itself), matching Heroic Labs' own current official example, trimmed of the Prometheus/metrics service this project doesn't need yet. No Unity-side code changes — the Unity client already has the SDK and the matching default config.

**Tech Stack:** Docker Compose, Nakama server (`registry.heroiclabs.com/heroiclabs/nakama:3.37.0`), CockroachDB (`cockroachdb/cockroach:latest-v24.1`).

## Global Constraints

- The Nakama Unity SDK is already added and resolved — confirmed in `Code/AlienOgKo.Unity/Packages/packages-lock.json` (`com.heroiclabs.nakama-unity`) and genuinely cached under `Code/AlienOgKo.Unity/Library/PackageCache/`. Do not touch `Packages/manifest.json` in this plan.
- `Code/AlienOgKo.Unity/Assets/Scripts/Settings/ServerSettings.cs` already defaults to `authBackend = Nakama`, `nakamaScheme = "http"`, `nakamaHost = "localhost"`, `nakamaPort = 7350`, `nakamaServerKey = "defaultkey"`. These match this plan's compose file's exposed defaults exactly — do not change any C# in this plan.
- New infra files go under `Backend/` at the repo root. This matches a pre-existing (never-yet-used) `.gitignore` entry for `Backend/.env`, confirming this is the project's intended location for backend/server-adjacent config — `Backend/` does not exist yet, this plan creates it.
- Base the compose file on Heroic Labs' own current official example (fetched directly from `github.com/heroiclabs/nakama`'s repo root `docker-compose.yml` on 2026-06-23): CockroachDB + `nakama:3.37.0`. Drop the Prometheus/metrics service from their example — not needed for local auth verification (YAGNI).
- Set an explicit Compose project `name: alienogko-nakama` so this stack's containers/volumes don't collide with any other local Nakama-based project on the same machine — the user confirmed self-hosted Nakama means **one instance per game** (e.g. CanVerse may want its own local instance later; confirmed it doesn't have one yet).
- Nakama's custom runtime modules load from `/nakama/data/modules` inside the container — bind-mount `Backend/data` there (instead of mirroring the upstream example's whole-directory `./:/nakama/data` mount) so future custom server-side modules can be dropped in directly, without exposing `docker-compose.yml` itself into the container.
- Verifying "authentication works in-editor" (issue #11's third checklist item) needs a human pressing Play and watching the Console — same constraint as the MapDataProxy plan's manual smoke test. This plan's automated verification stops at "Nakama is reachable and healthy over Docker"; the in-editor check is a deferred manual step.

---

### Task 1: Local Nakama Docker Compose stack

**Files:**
- Create: `Backend/docker-compose.yml`
- Create: `Backend/data/.gitkeep`

**Interfaces:**
- Produces: a Nakama server reachable at `http://localhost:7350` (client API/socket, matches `ServerSettings.NakamaScheme`/`NakamaHost`/`NakamaPort` exactly) and a web console at `http://localhost:7351`, authenticated with server key `defaultkey` (matches `ServerSettings.NakamaServerKey`).

- [ ] **Step 1: Create the compose file**

Create `Backend/docker-compose.yml`:

```yaml
name: alienogko-nakama

services:
  cockroachdb:
    image: cockroachdb/cockroach:latest-v24.1
    command: start-single-node --insecure --store=attrs=ssd,path=/var/lib/cockroach/
    restart: "no"
    volumes:
      - cockroach-data:/var/lib/cockroach
    expose:
      - "8080"
      - "26257"
    ports:
      - "26257:26257"
      - "8080:8080"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health?ready=1"]
      interval: 3s
      timeout: 3s
      retries: 5

  nakama:
    image: registry.heroiclabs.com/heroiclabs/nakama:3.37.0
    entrypoint:
      - "/bin/sh"
      - "-ecx"
      - >
          /nakama/nakama migrate up --database.address root@cockroachdb:26257 &&
          exec /nakama/nakama --name nakama1 --database.address root@cockroachdb:26257 --logger.level DEBUG --session.token_expiry_sec 7200
    restart: "no"
    links:
      - "cockroachdb:db"
    depends_on:
      cockroachdb:
        condition: service_healthy
    volumes:
      - ./data:/nakama/data/modules
    expose:
      - "7349"
      - "7350"
      - "7351"
    ports:
      - "7349:7349"
      - "7350:7350"
      - "7351:7351"
    healthcheck:
      test: ["CMD", "/nakama/nakama", "healthcheck"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  cockroach-data:
```

- [ ] **Step 2: Create the bind-mounted modules directory placeholder**

Create `Backend/data/.gitkeep` (empty file — keeps the directory tracked in git since it's otherwise empty; this is where future custom Nakama runtime modules go):

```
```

- [ ] **Step 3: Bring the stack up and verify it's healthy**

Run:
```bash
cd /home/fehaar/devdrive/AlienOgKoApp/Backend && docker compose up -d
```
Expected: both `cockroachdb` and `nakama` containers created and started.

Poll until both report healthy (this can take 10-30s for migrations to run on first boot):
```bash
cd /home/fehaar/devdrive/AlienOgKoApp/Backend && docker compose ps
```
Expected: both services show `(healthy)` in the `STATUS` column. If `nakama` shows `(unhealthy)` or keeps restarting, run `docker compose logs nakama` and report what you find rather than guessing at a fix.

- [ ] **Step 4: Verify the Nakama API and console are actually reachable**

```bash
curl -sS -o /dev/null -w "console_status=%{http_code}\n" http://localhost:7351
curl -sS -o /dev/null -w "api_status=%{http_code}\n" http://localhost:7350/
```
Expected: `console_status=200` (or a redirect like `302` to a login page — either confirms the console is serving) and `api_status=404` (Nakama's root API path has no handler, but a `404` confirms the HTTP server itself is up and responding — this is expected, not an error).

- [ ] **Step 5: Commit**

```bash
git add Backend/docker-compose.yml Backend/data/.gitkeep
git commit -m "Add local Nakama Docker Compose stack for issue #11"
```

- [ ] **Step 6: Manual follow-up (deferred to a human with the Unity Editor open)**

With the stack still running (`docker compose ps` showing both healthy), open the Unity project in the Editor, press Play on a scene that runs `Bootstrap` (any scene that loads at startup), and confirm the Console shows `Authenticated with identity backend` (logged by `PlayerAuthenticatedCommand`, added in the previous "remove ServerProxy" work) with no errors. This closes out issue #11's third checklist item ("Verify connection and authentication works in-editor").

---

## Self-Review Notes

- **Spec coverage:** issue #11's three checklist items — "Add the Nakama Unity SDK package" (already done, confirmed, no task needed), "Configure the SDK to connect to the local Nakama instance for dev" (already done in `ServerSettings.cs`, confirmed, no task needed), "Verify connection and authentication works in-editor" (Task 1 stands up the instance; Step 6 is the deferred manual verification since it needs a human, same pattern as the MapDataProxy plan's API-key smoke test).
- **Out of scope, confirmed explicitly by the user:** cloud-hosted Nakama deployment (separate future issue).
- **No code changes in this plan** — purely new infra files (`Backend/docker-compose.yml`, `Backend/data/.gitkeep`). No EditMode test run is needed/applicable since nothing under `Code/AlienOgKo.Unity/Assets/` changes; Task 1's own verification (Steps 3-4) is the infra-level equivalent of a test cycle for this kind of deliverable.
