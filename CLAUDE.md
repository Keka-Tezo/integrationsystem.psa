# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A **.NET 10 Worker Service** that synchronizes data between **ConnectWise** (PSA) and **Keka** (HR platform). It is cloud-agnostic — no AWS or other cloud SDK dependency — and runs as a plain Docker container. Data flows one-way: ConnectWise → Keka for companies, projects, project statuses, and time entries. Sync state and entity mappings are persisted as JSON files on a mounted data volume.

## Build & Run

```bash
# Build
dotnet build oculusit.sync.slnx

# Run locally (requires appsettings.Development.json or user secrets)
dotnet run --project oculusit.sync/oculusit.sync.csproj

# Docker build (run from solution root)
docker build -f oculusit.sync/Dockerfile -t oculusit-integration-service:latest .
```

**No test suite exists** in this codebase.

## Project Structure

| Project | Role |
|---------|------|
| `oculusit.sync` | Entry point: Worker service, DI bootstrap (`Program.cs`), `Worker.cs` (7 partial classes) |
| `oculusit.sync.core` | Domain models, `ISyncStateService` (file-based JSON read/write), config models |
| `oculusit.sync.connectwise` | ConnectWise REST API client (companies, projects, time entries) |
| `oculusit.sync.keka` | Keka REST API client (clients, projects, allocations, timesheets, employees) |
| `oculusit.sync.orchestration` | Sync orchestration: maps CW entities to Keka entities, drives full/incremental sync |

Each project registers its services via a `*ServiceExtensions.cs` file called from `Program.cs`.

## Architecture

### Sync Flow

`Worker.cs` is split into 7 partial classes, each handling a sync phase:
1. **Full sync** (first run): Fetch all CW entities, create Keka counterparts, store mappings in the sync state store
2. **Incremental sync**: Use `LastUpdatedAt` from the sync state to fetch only changed records
3. **Retry**: Re-process failed entities stored in `FailedCompanies`/`FailedProjects` lists in the sync state store
4. **Time entries**: Per-employee deduplication using `TimeEntries#{EmployeeId}` sync state keys

### Sync State Model

One JSON file per `SyncType` under `SyncState:DataDirectory` (default `/data/sync-state`, mounted as a Docker volume for persistence across container restarts). Key sync types: `"Company"`, `"Project"`, `"InitialCompany"`, `"ProjectStatus"`. Each record holds entity ID mappings (CW ID → Keka ID), failure lists for retry, and a `LastUpdatedAt` checkpoint.

### Resilience

- ConnectWise/Keka HTTP clients use `AddStandardResilienceHandler()` (30s attempt, exponential backoff, circuit breaker)
- Keka project/client API calls override to 2-minute attempt timeout and 10-minute total timeout (slow API)

### Configuration

- `appsettings.json` + `appsettings.Development.json` + .NET User Secrets locally; any setting can be overridden via environment variables using the `Section__Key` convention (e.g. `Keka__ClientSecret`, `SyncState__DataDirectory`)

## Key Patterns

- **No ORM/database**: sync state is plain JSON files on disk, read/written via `System.Text.Json` — no Entity Framework, no external database
- **All singletons**: All services registered as singletons (stateless by design)
- **DTO pattern**: Separate request/response models per API (`ConnectWiseCompany`, `KekaClientRequest`, etc.)
- **Nullable reference types** enabled; use `?` annotations and null checks consistently
- **File-scoped namespaces** and modern C# 10+ syntax throughout
