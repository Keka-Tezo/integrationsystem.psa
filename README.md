# oculusit.sync

A .NET 10 Worker Service that synchronises ConnectWise companies, projects, project statuses, and time entries to the Keka HR platform. The service is cloud-agnostic — no AWS or other cloud SDK dependency — and persists sync state as JSON files on a local/mounted data directory.

> Syncs PSA / CoreHR data: ConnectWise → Keka for the OculusIT customer.

---

## Prerequisites

| Tool | Purpose |
|------|---------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Build & run locally |
| [Docker](https://docs.docker.com/get-docker/) | Build & run the container image |

---

## Configuration

Application settings come from `appsettings.json`, with `appsettings.Development.json` and .NET User Secrets layered on top for local development. Any setting can be overridden via environment variables using the `Section__Key` convention (e.g. `Keka__ClientSecret`, `SyncState__DataDirectory`).

Key sections:

| Section | Purpose |
|---------|---------|
| `Keka` | Keka API base URL, identity/token endpoint, client credentials |
| `ConnectWise` | ConnectWise base URL, company ID, API keys |
| `SyncState` | `DataDirectory` — where sync-state JSON files are written (default `/data/sync-state`) |

---

## Run locally (no Docker)

```bash
dotnet run --project oculusit.sync/oculusit.sync.csproj
```

Uses `appsettings.Development.json`, which points `SyncState:DataDirectory` at a relative `./data/sync-state` folder.

---

## Run in Docker

All commands below must be run from the **solution root directory** (the folder containing `oculusit.sync/`, `oculusit.sync.core/`, etc.) — the build context needs every project folder as a sibling of `oculusit.sync/`.

### Build the image

```sh
docker build -f oculusit.sync/Dockerfile -t oculusit-integration-service:latest .
```

### Run the container

Sync state is persisted to `/data/sync-state` inside the container — mount a host directory there so state survives restarts, and override the Keka/ConnectWise credentials via an env file:

```sh
docker run --rm \
  --env-file ./oculusit-sync.env \
  -v "$(pwd)/data:/data/sync-state" \
  oculusit-integration-service:latest
```

Each run performs one full sync pass and exits — schedule repeat runs however suits your environment (cron, a container orchestrator, a scheduled task, etc.).

---

## Project Structure

```
oculusit.sync/               # Worker Service entry point (BackgroundService)
oculusit.sync.core/          # Domain models, interfaces, file-based sync state service
oculusit.sync.orchestration/ # Company & project sync orchestration logic
oculusit.sync.connectwise/   # ConnectWise API client
oculusit.sync.keka/          # Keka HR API client
```
