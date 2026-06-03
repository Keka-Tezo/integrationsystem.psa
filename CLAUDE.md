# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A **.NET 10 Worker Service** that synchronizes data between **ConnectWise** (PSA) and **Keka** (HR platform). It runs as an AWS ECS Fargate container, triggered hourly by EventBridge Scheduler. Data flows one-way: ConnectWise → Keka for companies, projects, project statuses, and time entries. Sync state and entity mappings are persisted in DynamoDB.

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

## Deploy to Production

```powershell
# Push image to ECR (ap-south-1, account 272403792718)
docker tag oculusit-integration-service:latest 272403792718.dkr.ecr.ap-south-1.amazonaws.com/oculusit-integration-service:latest
aws ecr get-login-password --region ap-south-1 | docker login --username AWS --password-stdin 272403792718.dkr.ecr.ap-south-1.amazonaws.com
docker push 272403792718.dkr.ecr.ap-south-1.amazonaws.com/oculusit-integration-service:latest
```

## Project Structure

| Project | Role |
|---------|------|
| `oculusit.sync` | Entry point: Worker service, DI bootstrap (`Program.cs`), `Worker.cs` (7 partial classes) |
| `oculusit.sync.core` | Domain models, `ISyncStateService` (DynamoDB read/write), config models |
| `oculusit.sync.connectwise` | ConnectWise REST API client (companies, projects, time entries) |
| `oculusit.sync.keka` | Keka REST API client (clients, projects, allocations, timesheets, employees) |
| `oculusit.sync.orchestration` | Sync orchestration: maps CW entities to Keka entities, drives full/incremental sync |

Each project registers its services via a `*ServiceExtensions.cs` file called from `Program.cs`.

## Architecture

### Sync Flow

`Worker.cs` is split into 7 partial classes, each handling a sync phase:
1. **Full sync** (first run): Fetch all CW entities, create Keka counterparts, store mappings in DynamoDB
2. **Incremental sync**: Use `LastUpdatedAt` from DynamoDB state to fetch only changed records
3. **Retry**: Re-process failed entities stored in `FailedCompanies`/`FailedProjects` lists in DynamoDB
4. **Time entries**: Per-employee deduplication using `TimeEntries#{EmployeeId}` DynamoDB partition keys

### DynamoDB State Model

Single table, partition key = `SyncType`. Key sync types: `"Company"`, `"Project"`, `"InitialCompany"`, `"ProjectStatus"`. Each record holds entity ID mappings (CW ID → Keka ID), failure lists for retry, and a `LastUpdatedAt` checkpoint.

### Resilience

- ConnectWise/Keka HTTP clients use `AddStandardResilienceHandler()` (30s attempt, exponential backoff, circuit breaker)
- Keka project/client API calls override to 2-minute attempt timeout and 10-minute total timeout (slow API)

### Configuration

- **Local**: `appsettings.json` + `appsettings.Development.json` + .NET User Secrets
- **Production**: 17 secrets from AWS SSM Parameter Store (injected at ECS task startup)

## Key Patterns

- **No ORM**: DynamoDB access is via `IAmazonDynamoDB` with manual attribute mapping — no Entity Framework
- **All singletons**: All services registered as singletons (stateless by design)
- **DTO pattern**: Separate request/response models per API (`ConnectWiseCompany`, `KekaClientRequest`, etc.)
- **Nullable reference types** enabled; use `?` annotations and null checks consistently
- **File-scoped namespaces** and modern C# 10+ syntax throughout
