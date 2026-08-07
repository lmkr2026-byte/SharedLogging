# Shared Logging Architecture for the LMKR Microservices Platform

This document lays out how to move request/response and correlation-id
logging out of each of the 29 individual APIs and into one shared,
centrally-versioned class library, backed by a single stored procedure and
a configurable Serilog viewer. It accompanies the code in `/src`, the SQL
in `/sql`, the pipeline in `/pipeline`, and the sample config in `/samples`.

## 1. What changes and why

Today, `CorrelationIdMiddleware`, `RequestResponseLoggingMiddleware`, and
`RequestResponseLoggingRepository` are duplicated (or will drift toward
being duplicated) across all 29 services. A bug fix or a new field means
touching 29 repos. The new design collapses that into one NuGet package,
`LMKR.Shared.Logging`, that every service references. A change is made
once in that repo, published as a new package version, and each service
picks it up by bumping a version number in its own `.csproj` — no source
copy/paste, ever.

Three requirements drove the specific choices below:

1. **One class library, referenced everywhere, updated centrally.** Solved
   with a dedicated Azure Repos project for `LMKR.Shared.Logging`, built
   and published to an internal Azure Artifacts NuGet feed on every merge
   to `main` (see `/pipeline/azure-pipelines.yml`). This is the standard
   pattern for "change once, everyone updates" in a multi-repo/microservice
   estate — a Git submodule or shared-source approach was considered and
   rejected because it has no versioning: a breaking change in the shared
   code would silently affect every service the next time anyone pulled,
   with no way for a service to pin an older version while it catches up.
2. **Serilog with a configurable viewer.** Solved by having the shared
   library bundle the Seq, Elasticsearch, Application Insights, File and
   Console sink packages, and by building the Serilog pipeline with
   `ReadFrom.Configuration(...)`. Which viewer is active is entirely a
   property of each service's `appsettings.{Environment}.json` — see
   `/samples/appsettings.sample.json`. You can run Seq in Dev, Elasticsearch
   in Prod, or even write to two sinks at once, without touching code or
   even redeploying the shared package.
3. **One stored procedure, which decides the table.** Solved by
   `usp_ApiLogs_Save` (`/sql/02_usp_ApiLogs_Save.sql`), which looks up the
   target table for a given `ServiceName`/`LogCategory` in a small
   `dbo.LogRoutingConfig` table and defaults to one unified `dbo.ApiLogs`
   table when there's no override. This is the assumption worth flagging
   explicitly: a single shared table is the simplest starting point (one
   schema, trivial cross-service correlation-id lookups, one index
   strategy to maintain), and the routing table lets you carve out a
   dedicated table for a specific noisy or sensitive service later without
   touching the stored procedure, the class library, or any of the 29
   services. If you'd rather start with one table per service instead
   (matching today's layout more closely), that's a config-only change —
   add 29 rows to `LogRoutingConfig` up front instead of zero.

## 2. Repo and package layout

```
LMKR.Shared.Logging (new Azure Repos repo)
├── src/LMKR.Shared.Logging/
│   ├── Configuration/SharedLoggingOptions.cs   - options bound from appsettings
│   ├── Extensions/ServiceCollectionExtensions.cs - AddSharedLogging(...)
│   ├── Extensions/ApplicationBuilderExtensions.cs - UseSharedLogging()
│   ├── Extensions/SerilogBootstrapper.cs        - Serilog config-driven builder
│   ├── Middleware/CorrelationIdMiddleware.cs
│   ├── Middleware/RequestResponseLoggingMiddleware.cs
│   ├── Models/ApiLogModel.cs
│   └── Repositories/ApiLoggingRepository.cs      - calls usp_ApiLogs_Save only
├── sql/01_CreateTables.sql
├── sql/02_usp_ApiLogs_Save.sql
└── azure-pipelines.yml
```

The SQL objects live in the same repo for change tracking, but they are
deployed to the shared `ApisLogsManagement` database independently (via
your existing DB deployment process/DACPAC/Flyway — whatever the platform
already uses), not as part of the NuGet package.

## 3. What each of the 29 services does

Per service, the integration is three steps and stays that way for the
life of the service — future improvements to logging arrive purely as a
version bump:

1. Add a `nuget.config` (if not already present) pointing at the
   `LMKR-Shared-Packages` Azure Artifacts feed, and add
   `<PackageReference Include="LMKR.Shared.Logging" Version="x.y.z" />`.
2. Delete the service's own copies of `CorrelationIdMiddleware`,
   `RequestResponseLoggingMiddleware`, and
   `RequestResponseLoggingRepository` — they're replaced by the package.
3. Wire it up in `Program.cs` as shown in `/samples/Program.sample.cs`:
   `builder.Host.UseSerilog(...)`, `builder.Services.AddSharedLogging(...)`,
   `app.UseSharedLogging()`, and set `SharedLogging:ServiceName` in
   `appsettings.json` to that service's name.

## 4. Rollout plan across 29 services

Doing all 29 at once is unnecessary risk. A staged rollout:

**Stage 0 — build the shared library and pipeline.** Stand up the
`LMKR.Shared.Logging` repo and Azure Artifacts feed, run `/sql/*.sql`
against `ApisLogsManagement`, get `azure-pipelines.yml` publishing package
version `1.0.0`.

**Stage 1 — pilot on 1–2 low-risk services.** Pick services with modest
traffic. Confirm logs land in `dbo.ApiLogs` correctly, correlation IDs
propagate end-to-end, and the chosen viewer (Seq, to start) shows the data
as expected. Compare volume/behavior against the old per-service logging
for a few days before removing the old code from those services.

**Stage 2 — roll through the remaining services in batches.** Migrate in
small batches (5–6 at a time) rather than all 27 remaining services in one
PR wave, so a problem in the shared package is caught against a small
blast radius. Bump the package version and remove old logging code in the
same PR per service, so there's never a period where a service has both.

**Stage 3 — decommission the old stored procedures.** Once every service
is off `usp_ParcelManagementLogs_Save`/`usp_ParcelManagementLogs_Update`
(and any per-service equivalents), retire them and the per-service
`ApiLogsModel`/`RequestResponseLoggingRepository` classes for good.

## 5. Operational notes

- **Logging must never break the request pipeline.** Both the middleware
  and the repository swallow their own exceptions and log to Serilog's
  other sinks (console/file keep working even if the DB or a remote sink
  is down) rather than letting a logging failure surface as a 500 to the
  caller.
- **Body capture is bounded per service.** `SharedLoggingOptions` exposes
  `LogBody` and `MaxBodyLength` — a service handling large file uploads or
  sensitive payloads can turn body logging off, or truncate more
  aggressively, without a code change. The library does not currently log
  request headers at all (only `User-Agent` and the resolved client IP), so
  there's no header-redaction concern today; if you later add full header
  capture, redact sensitive headers (`Authorization`, `Cookie`, API keys)
  before they're written anywhere.
- **gRPC stays lightweight.** Full body capture is skipped for gRPC calls
  (protobuf isn't useful as logged text); only the correlation event and
  standard Serilog request logging apply.
- **CommandTimeout is short (5s) on the logging stored procedure calls.**
  A slow or contended logging DB should never visibly slow down the
  actual API response.

## 6. Open decision to confirm

Everything above assumes the unified-table default described in section 1,
item 3. If you'd rather keep logs partitioned by service or by log
category from day one instead of defaulting to one shared table, say so
and the routing config (`/sql/01_CreateTables.sql`) can be pre-seeded with
one row per service before rollout — the stored procedure and class
library need no changes either way.
