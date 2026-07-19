# SQL Server to PostgreSQL + Orleans Upgrade Plan

The EggShell backend persists to SQL Server through Microsoft Orleans. This page is the plan for moving that
storage backend to PostgreSQL **and** upgrading Orleans 3.7 to current-major together, as the single
[Goal G](./goals-and-roadmap.md) workstream ([Phase 6](./phased-plan.md)). It maps the current storage layer,
the SQL-Server-specific constructs that must be ported, the Orleans-on-Postgres specifics, the
modern-Orleans and PostgreSQL-18 features worth adopting, and a spike-driven execution order. The concrete
seam list also appears in [Hosting & Persistence](../architecture/backend-hosting-persistence.md); this page
is the detailed version.

**Workstream status: S15 spike FAILED (1/10); S15b spike PASSED (7/7); S15b-production-port COMPILE GREEN; S15c spike PASSED + S15c-production-port LANDED (session 44); S15d spike PASSED (3/3, session 44) + S15d-production-port LANDED (session 45 -- bare-leaf serializer rework + TestCluster client codec-registration fix; LibLifeCycleTest 93/93 + SuiteTodo 5/5 green); S15e-production-port LANDED (session 46 -- IConnectorGrain command-object rewrite; SuiteJobs 47/47 PASS, zero Stasis/CodecNotFoundException). Next: S1 (PG18 baseline throwaway silo).**

Version numbers and upstream script paths carry source URLs in [References](#references). All versions below
were confirmed against nuget.org / upstream release pages on **2026-07-15**.

## The decisions that shape everything

Four decisions frame the whole effort. They are settled first because every later choice depends on them.

### 1. Couple the Orleans upgrade with the DB swap

Orleans is pinned at **3.7.2** across `LibLifeCycleCore` and `LibLifeCycleHost` (the `.fsproj` comments warn
against bumping it because "biosphere communication and reminders" depend on that exact version). The current
Orleans major line is **10.x**; the ADO.NET providers are at **10.2.1**.

Going 3.7 to current is a **hard, non-rolling** migration, not a version bump:

- The **wire protocol changed incompatibly** at 7.0. A cluster cannot mix pre-7.0 and 7.0+ silos, so there is
  no rolling upgrade. Cut-over is blue/green: stand up a new cluster, decommission the old.
- **Grain and stream identity changed** to `type/key` strings, replacing the old numeric type-code + category +
  generic-info bytes. Persisted grain state, reminders, and streams do not map straight across.
- The **ADO.NET membership, reminder, and persistence table schemas differ** between 3.x and current; the
  current-branch setup scripts are not schema-compatible with a 3.x database.

The PostgreSQL rewrite touches the *same* seams: membership/clustering configuration, the reminder table, and
grain-state storage. Doing the storage rewrite against Orleans 3.7 and then redoing the membership and reminder
port again for the target Orleans is wasted work. **The target Orleans (and .NET LTS) version is decided first,
then the storage rewrite is done once against that target.**

A secondary consequence: because grain identity and the persistence schema change with Orleans 7+, this is a
**fresh-cluster, re-seed-state** exercise, not an in-place migration of the Orleans system tables. Application
subject data is seeded fresh (see decision 2).

### 2. PostgreSQL is the only end-state backend (no permanent dual-DB)

There are **no live applications and no running cluster** in this repo. That removes the only real reason to
maintain two backends forever. The end state is **PostgreSQL-only**: SQL Server code (`Storage/SqlServer/`,
`SubjectReminderTable.fs`, `SqlServerDataProtectionXmlRepository.fs`, `SqlServerTransientErrorDetection.fs`,
`SetupOrleans.sql`) is deleted once the Postgres path is proven.

This reshapes the original six-phase plan:

- **No permanent `DatabaseProvider` switch.** The abstraction seam (`IDbConnectionProvider` / `IDbSetup` /
  dialect switch) is **scaffolding built to de-risk the port and to let SQL Server act as a throwaway diff
  oracle**, not a permanent feature. It is deleted with the SQL Server code at the end.
- **No production data lift.** Phase 6's `pgloader` cut-over disappears — there is nothing live to lift. Fresh
  seed only. (`pgloader` stays in the toolbox purely in case a one-time bulk import from an external SQL Server
  is ever wanted.)
- **CI is PostgreSQL-only** once the port is confident (see [Testing strategy](#testing-strategy)). SQL Server
  runs only as a *transient* diff oracle during the port: query-result diffs for the same inputs validate the
  Postgres handlers before the SQL Server path is deleted. The in-memory `Storage/Test` fakes stay for fast
  unit tests.

Keeping SQL Server as a temporary diff oracle (not a maintained backend) is the key distinction: it preserves
correctness validation without paying a permanent abstraction tax.

### 3. Decide the target stack before writing storage code

The target Orleans major, .NET LTS, Npgsql line, and PostgreSQL major are picked together
([Target versions](#target-versions-and-toolchain)), because the storage rewrite must happen once against the
target. The repo-wide TFM bump off `net7.0` is a prerequisite (the `global.json` SDK is already `10.0.301`, so
the build host is ready; the backend projects are not).

### 4. Kubernetes is the *preferred* deployment target, not a hard requirement

Kubernetes is not a fixed premise; the design stays **substrate-flexible**. The rule is: *when* the cluster
runs on K8s, use the best-native membership approach; when it does not, fall back to ADO.NET-on-PostgreSQL
clustering. Both implement the same Orleans `IMembershipTable` seam, so switching is a **per-environment config
switch** (`UseKubernetesClustering()` vs `UseAdoNetClustering()` vs `UseLocalhostClustering()`), never a code fork.

On K8s, membership uses **`Orleans.Clustering.Kubernetes` 10.0.1** (community-contributed, Orleans 10-compatible,
targets net8+net10, depends on `KubernetesClient` 18.0.5). It is more reliable than DB-polling membership and
removes a class of split-brain and stale-membership bugs the ADO.NET provider carries.

**How K8s membership actually works** (correcting the shorthand "K8s replaces Orleans membership"): Orleans always
runs its **own** membership protocol regardless of provider: peer probing, periodic `IAmAliveTime`, and
suspect-and-vote eviction. The K8s provider does not replace that; it **feeds it better signal**. Two distinct
mechanisms are in play:

- **Kubelet liveness/readiness probes** are the thing literally named a "liveness probe." Kubelet hits an
  HTTP/TCP/exec endpoint on the container; on failure it *restarts the pod*. This is process-level health, not
  Orleans membership, but you point it at a silo health endpoint so a wedged silo gets recycled.
- **Pod-status watch via the API server** is what the Orleans provider consumes. It watches pods in the
  silo's label selector; when a pod is deleted/evicted/`Failed` (or its node stops renewing its lease), the
  provider marks that silo `Dead` in the roster directly, **short-circuiting** the probe-timeout-and-vote path.
  This is the "direct liveness" advantage: an authoritative event from the orchestrator that killed the
  process, versus inferring death from missed DB heartbeats.

> Not yet verified against the 10.0.1 source: the exact roster backing the provider uses (CRD / ConfigMap /
> pods-as-truth). Confirm in [S14](#tier-5--validation--perf-acceptance) before relying on the detail.

**PostgreSQL clustering stays a real, runnable fallback.** Because K8s is not mandated, the upstream
`PostgreSQL-Clustering.sql` script is **ported alongside** `-Main`/`-Persistence`/`-Reminders`, not skipped. That
keeps the non-K8s substrate genuinely deployable (a Postgres box with no orchestrator) and preserves an escape
hatch if [S14](#tier-5--validation--perf-acceptance) finds the K8s provider unsuitable. The only cost is porting
one more SQL script; the payoff is substrate flexibility, which the project explicitly wants.

Local development requires **no** K8s cluster: dev uses **localhost clustering** (`UseLocalhostClustering`), the
pattern the existing silo already falls back to. CI runs against a PG container with localhost clustering for
unit/integration; K8s membership is validated in [S14](#tier-5--validation--perf-acceptance) against a throwaway
minikube/kind cluster.

### The no-regression bar

The governing constraint for every remaining decision: **no regressions versus what SQL Server supports today,
and no regression in the dev/DBA experience.** Concretely:

- Every SQL Server feature in use has a PostgreSQL equivalent of equal-or-better capability (the
  [feature port table](#feature-by-feature-port) is the evidence; each entry is validated by its spike).
- The DBA experience stays ergonomic: a single PostgreSQL database (no cross-DB), idiomatic `lower_snake_case`
  identifiers (no quoting friction in `psql`/`pg_dump`/editors), inspectable JSON payloads (not opaque binary),
  and standard PG tooling (`pg_stat_statements`, `pg_stat_io`, `EXPLAIN ANALYZE`).
- The dev experience stays frictionless and matches or beats today: a developer runs the app and database
  locally with no K8s cluster. `docker compose up` gives a working local silo (`UseLocalhostClustering` + a
  local PG 18 container); the deterministic simulation and unit tests use the in-memory `Test`/`Volatile`
  handlers and need no database at all. K8s is a production deployment *option* (decision 4), never a local-dev
  requirement, and never the only path to production.

This bar is why some decisions are "adopt if the spike proves parity" rather than "adopt unconditionally" — the
spikes exist to prove no regression before a deletion.

### Platform portability (ARM and x86)

A concrete win of dropping SQL Server: the entire target stack runs **natively on both arm64 (Apple Silicon,
including M-series) and x86-64**, with no emulation.

- **SQL Server is the ARM problem being removed.** SQL Server has no native arm64 build for macOS; it runs only
  under x86 emulation on Apple Silicon, and its Full-Text Search component does not work in that configuration.
  FTS is a hard dependency here (the `_SearchIndex` catalog), so full-fidelity local development on an M-series
  Mac is effectively impossible today.
- **PostgreSQL closes the gap.** PostgreSQL 18 and the official `postgres` and `postgis/postgis` Docker images are
  multi-arch with native arm64 (Apple Silicon, AWS Graviton) and amd64 builds. PostgreSQL full-text search
  (`tsvector`/`tsquery`) is **in-core**, not a separate installable component, so there is no FTS-on-ARM gap at
  all; PostGIS 3.5 ships in the official arm64 image.
- **The rest of the stack is architecture-neutral.** .NET 10 is arm64-native (macOS-arm64, linux-arm64); Orleans
  and Npgsql are pure managed code with no native dependencies. The same build runs on an M-series Mac dev
  machine and an x86 CI/prod host.
- **Verified in [S1](#tier-1--foundation-de-risk-the-core-combination)** on the actual arm64 dev machine: confirm
  the `postgis/postgis:18-3.5` image pulls the native arm64 variant (not emulated amd64) and that FTS + PostGIS
  queries run. The official PostGIS arm64 manifest is relatively recent (the image was amd64-only historically),
  so this is the one architecture item S1 checks on-device; `imresamu/postgis` is the documented fallback if a
  specific tag lags.

## Current storage layer

The state of `LibLifeCycleHost/src/Storage/` and the hosting layer, as they stand. Confirmed against the repo
on 2026-07-15.

### What exists

- **One backend: SQL Server.** `Storage/SqlServer/` is the sole DB implementation. There is a `Storage/Test/`
  (in-memory fakes) and a `Storage/Volatile/` (non-persistent), but no `Postgres/` directory and no `Npgsql`
  dependency.
- **The data client is `Microsoft.Data.SqlClient` 5.2.3** (`LibLifeCycleHost.fsproj`).
- **Backend projects target `net7.0`** (`LibLifeCycleCore`, `LibLifeCycleHost`, `LibLifeCycle`,
  `LibLifeCycleTypes`, `LibLifeCycleUi`, `LibLifeCycleTest`). The `global.json` pins SDK `10.0.301`
  (`rollForward: latestFeature`), so the build host is already .NET 10; the *projects* are not.
- **No Central Package Management.** There is no `Directory.Packages.props`; package versions are pinned
  per-project. This upgrade touches ~12 packages across six `LibLifeCycle*` projects, so adopting CPM is part
  of the TFM spike (see [S0](#tier-0--prerequisites-unblock-all)).
- **Orleans 3.7.2** packages: `Microsoft.Orleans.Core`, `Microsoft.Orleans.Clustering.AdoNet`,
  `Microsoft.Orleans.Connections.Security` (Core); `Microsoft.Orleans.Core`, `Microsoft.Orleans.OrleansRuntime`,
  `Microsoft.Orleans.OrleansProviders`, `Microsoft.Orleans.OrleansCodeGenerator`,
  `Microsoft.Orleans.OrleansTelemetryConsumers.AI`, `Microsoft.Orleans.Clustering.AdoNet`,
  `Microsoft.Orleans.Connections.Security` (Host).
- **`OrleansDashboard` 3.1.0** (community package) and **`Microsoft.Orleans.OrleansTelemetryConsumers.AI`
  3.7.2** (legacy Application Insights sink) are referenced in Host. Orleans 10 ships its **own dashboard**
  and **native metrics / DiagnosticSource / OpenTelemetry**, so both are replaced, not ported
  (see [Orleans 10 modernization](#orleans-10-modernization-adopt-dont-reimplement)).
- **A partial abstraction seam exists, at the handler level:**
  - `IGrainStorageHandler` (non-generic and generic) in `LibLifeCycleHost/src/GrainStorageHandler.fs`.
  - `ITransferBlobHandler` in `LibLifeCycleHost/src/TransferBlobHandler.fs`.
  - `ITimeSeriesStorageHandler<...>` in `LibLifeCycleHost/src/TimeSeriesStorageHandler.fs`.
  - `ISubjectRepo<...>` and `ITimeSeriesRepo<...>` in `LibLifeCycle/src/DefaultServices.fs`.
  - A parallel `Storage/Postgres/` set of implementations slots in behind these interfaces — this is where the
    new handlers live.
- **The SQL Server implementation files** (all in `LibLifeCycleHost/src/Storage/SqlServer/`):
  `SqlServerGrainStorageHandler.fs` (~176 KB, core state persistence, TVP-heavy), `SqlServerSubjectRepo.fs`
  (~81 KB, index/full-text/geography queries), `SqlServerIndexQueryOptimizer.fs` (~30 KB, spatial query
  building), `SqlServerSetup.fs` (~23 KB, schema bootstrap + Orleans setup), `SqlServerTimeSeriesRepo.fs`,
  `SqlServerTimeSeriesStorageHandler.fs`, `SqlServerTransferBlobHandler.fs`,
  `SqlServerDataProtectionXmlRepository.fs`, `SqlServerConfiguration.fs` (holds both
  `SqlServerConfiguration` and `SqlServerConnectionStrings`), `SqlServerTransientErrorDetection.fs`,
  `SqlServerUtils.fs`.
- **Schema is driven by ~58 embedded `.sql` resources** (declared `EmbeddedResource` in
  `LibLifeCycleHost.fsproj`), applied idempotently by `SqlServerSetup.fs` via a hash-tracked upgrade folder
  (`Storage/SqlServer/Upgrades_Auto/*.sql`). Per lifecycle the schema generates a main table plus `_History`,
  `_Index`, `_Subscription`, `_SideEffect`, `_Prepared` tables and a `_Version` sequence.
- **Orleans 3.7.2 clustering** via `.UseAdoNetClustering(...)` in
  `LibLifeCycleHost/src/OrleansEx/SiloBuilder.fs`, with the ADO.NET **invariant hardcoded to
  `"Microsoft.Data.SqlClient"`**. Membership DDL is the embedded `SetupOrleans.sql`, run by
  `SqlServerSetup.runOrleansSetup`.
- **Reminders are custom:** `LibLifeCycleHost/src/SubjectReminderTable.fs` implements Orleans
  `IReminderTable` directly against the subject tables (it `open`s `Microsoft.Data.SqlClient` and issues raw
  commands). This is SQL-Server-coupled and sits outside the handler interfaces.
- **ASP.NET Data Protection keys** are stored via `SqlServerDataProtectionXmlRepository.fs`.
- **A custom durable side-effect / action queue** lives in the subject tables: `EnqueueAction.sql`,
  `RetryPermanentFailures.sql`, and the diagnostics scripts `StalledTimers.sql`, `StalledPrepared.sql`,
  `StalledSideEffects.sql`, `FailedSideEffects.sql`, plus the 2-phase-commit `PrepareStateShared.sql` /
  `PrepareUpdate.sql` / `PrepareInitialize.sql` / `UpdateState.sql` / `RollbackStateShared.sql` /
  `RollbackPreparedUpdate.sql` / `RollbackPreparedInitialize.sql`. This is a hand-rolled durable-job +
  journaling layer. Orleans 10 introduced **native Durable Jobs + a journaling catalog**, so this subsystem is
  a candidate for partial or full replacement — the single biggest simplification opportunity
  (see [Durable Jobs evaluation](#durable-jobs-evaluation)).

### What does not exist (so is not a porting concern)

- **No stored procedures.** Persistence logic is dynamic SQL generated in F# and executed via `sp_executesql`
  (with `WITH RESULT SETS NONE` for the generated DDL). There is no PL/pgSQL proc rewrite; the dynamic-SQL
  builders in F# emit PostgreSQL dialect directly.
- **No temporal / system-versioned tables.** History is a manually managed `*_History` table, not
  `SYSTEM_VERSIONING`.
- **No `SqlDbType.Xml` columns.** The only XML is inside the Data Protection repository payload, not a column
  type.
- **No `IDbConnectionProvider` / `IDbSetup` seam.** The abstraction stops at the handler interfaces; connection
  creation, the ADO.NET invariant, the reminder table, the Data Protection store, and the Orleans setup are
  each bound directly to SQL Server. Closing this gap is the first work item — as scaffolding, per decision 2.

### Migration hotspots, ranked by effort

1. **Table-Valued Parameters / UDT table types** (`SqlDbType.Structured`). ~32 call sites; 13 UDTs in
   `CreateTypes.sql` (`SideEffectList_V2`, `SubscriptionNameAndLifeEventList`, `SubscriptionNameList`,
   `IndexingList_V3`, `RebuildIndexList_V4`, `PromotedIndexingList`, `IdList`, `GuidList`, `BlobActionList_V2`,
   `CallDedupList_V2`, `ReEncodeSubjectsList`, `ReEncodeSubscriptionsList`, `TimeSeriesPointsList`). This is
   the single biggest blocker: PostgreSQL has no TVP. See the [TVP strategy](#tvp-strategy) note — `unnest` vs
   `COPY` vs JSONB is a per-volume decision, not a blanket one.
2. **Custom durable side-effect / action queue + 2-phase commit.** The `EnqueueAction` / `RetryPermanentFailures`
   / `Stalled*` / `Prepare*` / `Rollback*` machinery is a hand-rolled durable-job system. Orleans 10 Durable
   Jobs may replace part of it (see [Durable Jobs evaluation](#durable-jobs-evaluation)). This is the highest
   *simplification* payoff, even though the 2-phase-commit grain-state persistence itself stays.
3. **ROWVERSION optimistic concurrency.** `ConcurrencyToken ROWVERSION` and
   `PreparedInitializeConcurrencyToken ROWVERSION` in `CreateTables.sql`, used as the ETag for the
   2-phase-commit grain storage. Map to PostgreSQL `xmin` (preferred) or an explicit version column — see
   [Concurrency token](#concurrency-token).
4. **Full-text search.** `CreateFullTextCatalog.sql` (`CREATE FULLTEXT CATALOG`), `CreateSearchIndexTable.sql`
   (`CREATE FULLTEXT INDEX`), with the query-side predicate built in `SqlServerSubjectRepo.fs`.
5. **GEOGRAPHY / spatial.** `CreateGeographyIndexTable.sql` (`GEOGRAPHY` column),
   `UpsertStateShared_UpsertGeographyIndex.sql`, `RebuildIndices_GeographyIndex.sql`,
   `ClearState_DeleteGeographyIndex.sql`, and `SqlServerIndexQueryOptimizer.fs`.
6. **`VARBINARY(MAX)` payloads** (serialized state/history/side-effects/timeseries, gzip-compressed JSON).
7. **Orleans invariant + membership scripts + custom reminder table.**
8. **Connection-string dialect + MARS.** Connection strings carry `MultipleActiveResultSets=True`, which is
   SQL-Server-only and must be removed for Npgsql.
9. **Transient-error classification** (`SqlServerTransientErrorDetection.fs`) uses SQL Server error numbers;
   PostgreSQL uses SQLSTATE strings.
10. **Observability replacement.** `OrleansTelemetryConsumers.AI` and `OrleansDashboard` 3.1.0 are dropped in
    favor of Orleans 10 native metrics + DiagnosticSource + OTel + the built-in dashboard. The custom
    `Diagnostics/*.sql` scripts (decode/Stalled*) are superseded by native observability + `pg_stat_statements`.

## Target versions and toolchain

Confirm exact point releases on nuget.org before pinning. The target shape, confirmed 2026-07-15:

| Component | Target | Notes |
|---|---|---|
| Orleans | **10.2.1** (→ 10.2.2 when green; 10.2.2-rc.1 out 2026-07-10) | Non-rolling upgrade from 3.7.2. 10.2.0 is the big reliability release (grain-directory races, rolling-upgrade compat, `ValidateInitialConnectivity`); 10.0 multi-targets net8+net10. |
| .NET | **net10.0** (backend TFM) | SDK already `10.0.301` in `global.json`. Backend projects still `net7.0`; the TFM bump is the prerequisite spike. |
| Npgsql | **10.0.3** (May 2026) | Targets net8/9/10. **Npgsql 10 deprecates synchronous APIs** — write async DB access only. |
| PostgreSQL | **18** (current stable since 2025-09; 19 in beta) | Bumped from 17. Use `postgis/postgis:18-3.5`. Data checksums now default-on. |
| PostGIS | **3.5+** | Matches PG 18. `CREATE EXTENSION postgis;` per database. |
| Orleans ADO.NET invariant | `Npgsql` | Replaces `Microsoft.Data.SqlClient` for persistence + reminders (+ clustering on the non-K8s substrate). |
| Orleans packages | `Microsoft.Orleans.{Clustering,Persistence,Reminders}.AdoNet` 10.2.1 | Apply `PostgreSQL-Main.sql` then per-feature scripts, **including `PostgreSQL-Clustering.sql`** (it backs the PG-membership fallback, decision 4). |
| Membership (prod, on K8s) | **`Orleans.Clustering.Kubernetes` 10.0.1** | Preferred when the substrate is K8s: pod-status watch, more reliable than DB-polling. Orleans 10-compatible (net8+net10). |
| Membership (prod, non-K8s) | `UseAdoNetClustering` on Postgres | Real fallback: keeps the app deployable on a plain Postgres box with no orchestrator. |
| Membership (dev/CI) | `UseLocalhostClustering` | No K8s requirement for the inner loop; per-environment config switch. |
| Dashboard | **Built-in `Microsoft.Orleans.Dashboard`** (part of Server) | Drop community `OrleansDashboard` 3.1.0. |
| Telemetry | **Native metrics (`IMeterFactory`) + DiagnosticSource + OTel + `Npgsql.OpenTelemetry`** | Drop `Microsoft.Orleans.OrleansTelemetryConsumers.AI` 3.7.2. |
| Serializer | **JSON** (`UseJsonFormat = true`) | Keep — debuggable/DBA-inspectable. MemoryPack (perf) deferred; revisit only in the perf pass if a hot path demands it. |
| Package management | **Central Package Management** (`Directory.Packages.props`) | Adopt with the TFM bump; ~12 packages across 6 projects. |
| Schema migration | **DbUp** (`dbup-postgresql`) for versioned scripts | Replaces the hash-tracked `Upgrades_Auto` mechanism. Applies schema, not data. |
| Data lift | (none required — fresh seed; `pgloader` kept in toolbox only) | No live data to lift (decision 2). |

### PostgreSQL 18 native features to adopt

PG 18 is current stable and carries features that directly serve the "max native PG + reliability + perf" goal.
Each is validated by [S12](#tier-4--modernize-maximize-features-do-these-in-the-port-not-after); the ones that
earn a place get baked into the schema during the port, not bolted on later.

- **Asynchronous I/O (`io_method=worker`).** Queues multiple read requests → faster sequential scans, bitmap
  heap scans, vacuum. Free perf; set `io_combine_limit` / `io_max_combine_limit`.
- **Temporal constraints (`WITHOUT OVERLAPS` for PK/UNIQUE, `PERIOD` for FK).** Native non-overlapping range
  constraints for subscription validity ranges and history validity windows — stronger than app-enforced
  checks, enforced by the database.
- **`uuidv7()`.** Timestamp-ordered, B-tree-locality-friendly UUIDs. If any subject/grain IDs use UUID,
  switching to v7 improves insert/index locality. Evaluate against the existing key scheme.
- **`OLD` / `NEW` in `RETURNING`.** Lets `InitializeState` / `UpdateState` upserts return both the prior and
  new row in one statement — fewer round-trips for the read-back paths.
- **Data checksums default-on.** Silent-corruption reliability; no action beyond using the default.
- **Virtual generated columns.** Derived columns computed on read (no storage). Use only where a stored value
  isn't needed; FTS stays a **stored** generated `tsvector` + GIN (faster than virtual).
- **Parallel GIN build.** Faster (re)build of FTS and spatial GiST/GIN indexes.
- **`pg_stat_statements` + per-backend I/O/WAL stats.** Evidence source for the perf pass; supersede the
  custom `Diagnostics/*.sql` scripts.

## The abstraction gap (scaffolding, not permanent)

The repo lacks a connection/setup seam below the handler interfaces. Per [decision 2](#2-postgresql-is-the-only-end-state-backend-no-permanent-dual-db),
this seam is **built to de-risk the port and to let SQL Server serve as a throwaway diff oracle**, then deleted
with the SQL Server code. It is not a maintained feature.

1. **`IDbConnectionProvider`** — yields an open `DbConnection` (base type, not `SqlConnection`) per ecosystem
   and purpose. The SQL Server implementation wraps the existing `SqlServerConnectionStrings`; the Postgres
   implementation builds `NpgsqlConnection`. Every `Storage/SqlServer/*.fs` that news up `SqlConnection` today
   routes through this instead.
2. **`IDbSetup`** — abstracts `SqlServerSetup`'s "apply embedded schema + upgrades" and "run Orleans membership
   setup" so a `PostgresDbSetup` applies the PostgreSQL DDL and the upstream Orleans PostgreSQL scripts.
3. **A dialect switch on the storage-setup union.** `HostExtensions.fs` today directly instantiates the SQL
   Server handlers. A `DatabaseProvider` config key (`SqlServer` | `Postgres`) selects the handler set,
   connection provider, setup, reminder table, Data Protection store, transient-error detector, and the Orleans
   ADO.NET invariant. **Deleted at port completion.**
4. **A provider-specific transient-error classifier** so retry logic is correct on each backend
   (`SqlServerTransientErrorDetection` vs a Postgres SQLSTATE-based one). The Postgres classifier stays; the
   SQL Server one is deleted.
5. **Connection-string handling.** Strip `MultipleActiveResultSets=True` for Postgres; MARS is SQL-Server-only.
   Npgsql uses `@name` placeholders in command text (rewritten to positional internally) but raw `unnest` /
   function calls need `$1` positional — this matters when porting the dynamic-SQL builders.

The Postgres implementations are written only after this seam is in place and the SQL Server path is verified
unchanged. Then the Postgres path is proven by diffing against SQL Server, and the SQL Server path is deleted.

## Feature-by-feature port

The reference for porting each SQL construct. Sources for every mapping are in [References](#references).

| SQL Server construct (where) | PostgreSQL approach | Notes |
|---|---|---|
| **TVP / UDT table types** (13 in `CreateTypes.sql`) | Pass a .NET array as **one parameter + `unnest()`** to expand to rows; for bulk load use **binary COPY** (`NpgsqlBinaryImporter`); for complex rows use **JSONB + `jsonb_to_recordset`** or a registered **composite-type array**. | See [TVP strategy](#tvp-strategy). No custom table types needed. Prefer `unnest` for the index/subscription/side-effect lists; consider COPY only for the highest-volume paths; evaluate JSONB for the list-type TVPs (see below). |
| **`ROWVERSION` concurrency token** (`CreateTables.sql`) | Map the hidden **`xmin`** system column as the token (preferred), or add an **explicit `bigint` version column** bumped on update. | See [Concurrency token](#concurrency-token). `xmin` = zero columns, auto-updates, ideal for an ETag (only needs to differ post-update, not be globally monotonic-forever; wraparound is a non-issue). An explicit column is the fallback if the 2-phase-commit ETag round-trip breaks. |
| **Full-text: `CREATE FULLTEXT CATALOG/INDEX`, `CONTAINS`/`FREETEXT`** | **`tsvector`/`tsquery`** + **GIN index**; rank with **`ts_rank()`**. `CONTAINS` (precise/prefix/boolean) → `to_tsquery`; `FREETEXT` (natural language) → `plainto_tsquery`/`websearch_to_tsquery`. | Full rewrite of the FTS predicate builder. Prefer a **stored generated** `tsvector` column + GIN for speed (PG 18 builds GIN in parallel). PG FTS is less feature-rich than SQL Server's but adequate for the index-search use here. PG 18 also improved FTS collation handling — reindex FTS indexes after any `pg_upgrade`. |
| **`GEOGRAPHY` + `STDistance`** | **PostGIS** `geography` type, **`ST_Distance`** (geodesic meters), **`ST_DWithin`** for radius filters. `CREATE EXTENSION postgis;` per DB. | `ST_Distance` is ~1:1 with `STDistance()`. **Use `ST_DWithin` for range queries** — it uses the GiST spatial index; `ST_Distance` in a `WHERE` does not. |
| **`VARBINARY(MAX)`** (serialized payloads) | **`bytea`** (`NpgsqlDbType.Bytea`, .NET `byte[]`). | Validate byte-lengths after any bulk migration; some automated migrators truncate large `bytea`. |
| **`DATETIMEOFFSET`** | **`timestamptz`** (`TIMESTAMP WITH TIME ZONE`). | `timestamptz` normalizes to UTC on write and does **not** store the original offset. Npgsql maps `DateTime`/`DateTimeOffset` with UTC rules; `DateTime.Kind` matters. If an original offset must survive, store it separately. |
| **`MERGE` / upsert** (`UpsertStateShared*.sql`) | **`INSERT ... ON CONFLICT (cols) DO UPDATE SET x = EXCLUDED.x`**. PG 15+ also has real `MERGE`, but `ON CONFLICT` is idiomatic. | PG 18 adds `OLD`/`NEW` in `RETURNING` — use it to read back prior + new state in one statement for `InitializeState`/`UpdateState`. |
| **`SCOPE_IDENTITY()` / `OUTPUT INSERTED`** | **`RETURNING`** on the DML statement. | Cleaner and multi-row capable. |
| **Sequences** (`SequenceCurrentValue.sql`, `SequenceNextValue.sql`, `_Version` sequence) | PG sequences (`nextval`/`currval`), or `GENERATED ... AS IDENTITY`. | Syntax differs; behavior maps closely. |
| **Subscription / history validity ranges** (app-enforced today) | **PG 18 temporal constraints** (`WITHOUT OVERLAPS` on PK/UNIQUE, `PERIOD` on FK) where a non-overlapping range is the actual invariant. | Native > app-enforced. Evaluate per table; not every range is a temporal PK. |
| **UUID keys** (if any) | **`uuidv7()`** (PG 18) for time-sortable, B-tree-locality-friendly IDs. | Only if the existing key scheme uses UUID; evaluate against current key generation. |
| **Error numbers** (`SqlServerTransientErrorDetection.fs`, catch/retry logic) | **`PostgresException.SqlState`** (SQLSTATE), constants in `Npgsql.PostgresErrorCodes`: unique **`23505`**, deadlock **`40P01`**, serialization failure **`40001`**. Also check `IsTransient`. | Rewrite from int codes to SQLSTATE strings. Retry `40001`/`40P01`. |
| **Identifier case** (all DDL, all quoted identifiers) | PG folds unquoted identifiers to **lowercase**; quoted `"Name"` is case-sensitive. | Biggest silent-bug source. **Recommendation: `lower_snake_case` for the new PG schema** (idiomatic, no quoting tax) — since the SQL Server code is being deleted anyway, there is no mixed-case legacy to preserve. Decide in [S2](#tier-2--core-storage-the-hard-part). |
| **`sp_executesql` dynamic DDL** (`CreateTables.sql`, etc.) | `EXECUTE format(...)` in a `DO $$ ... $$` block, or build and execute the SQL from F# directly. | Since the SQL is generated in F#, prefer emitting the final PostgreSQL text from F# rather than porting the `sp_executesql` indirection. |
| **MARS** (`MultipleActiveResultSets=True` in connection strings) | Remove; not supported by Npgsql. Use separate connections or read fully before the next command. | Audit `SqlServerSubjectRepo` batch/streaming reads (`CommandBehavior.CloseConnection`, `AsyncSeq`) for MARS assumptions. |

### TVP strategy

The 13 TVPs split three ways (decided per TVP by [S2](#tier-2--core-storage-the-hard-part)):

- **`unnest(array)`** — default for the row-list TVPs that stay relational (`IndexingList_V3`,
  `RebuildIndexList_V4`, `PromotedIndexingList`, `SubscriptionNameAndLifeEventList`, `SubscriptionNameList`,
  `TimeSeriesPointsList`). Cast array params to resolve type ambiguity.
- **Binary `COPY`** — only for the highest-volume *insert-only* write paths if `unnest` underperforms (measure
  in S2 / S13). Note the PostgreSQL COPY protocol supports inserts only, not upserts, so the many upsert-shaped
  index/side-effect paths cannot use COPY directly; they use `unnest(array)` + `INSERT ... ON CONFLICT` (the
  composite-array-and-unnest bulk-upsert pattern benchmarks around 20s for 1M rows vs ~81s for single-row
  inserts). COPY-then-merge-from-staging is an option only if a specific path proves hot enough to justify it.
- **JSONB + `jsonb_to_recordset`** — candidate for the *list-type* TVPs that are really opaque payloads
  (`SideEffectList_V2`, `BlobActionList_V2`, `CallDedupList_V2`, `IdList`, `GuidList`,
  `ReEncodeSubjectsList`, `ReEncodeSubscriptionsList`). JSONB = fewer tables, fewer joins, simpler schema, and
  a GIN index covers membership queries. Trade-off: less relational querying. The dedicated `_SideEffect` /
  `_Subscription` tables stay for index/search/geography (those are genuinely relational); the JSONB option is
  only for the list-payload TVPs.

### Concurrency token

The `ROWVERSION` ETag maps to either:

- **`xmin`** (preferred) — the hidden system column, zero extra schema, auto-updates on every update. For an
  ETag (detect concurrent modification) it only needs to differ after an update, not be globally monotonic
  forever, so `xmin` wraparound is a non-issue. [S3](#tier-2--core-storage-the-hard-part) ports the
  2-phase-commit ETag round-trip both ways and confirms.
- **Explicit `bigint` version column** bumped on update — the fallback if the 2-phase-commit round-trip proves
  `xmin`-incompatible. Clearer for code-first tooling at the cost of app-side bump logic.

## Orleans on PostgreSQL

- **Membership: K8s preferred, PG fallback kept real (decision 4).** On K8s, `Orleans.Clustering.Kubernetes`
  10.0.1 feeds Orleans' own membership protocol from pod-status watches (more reliable than DB polling and immune
  to its split-brain/stale-entry failure modes). Because K8s is *not* a hard requirement, the upstream
  **`PostgreSQL-Clustering.sql` script is ported too** (not skipped), backing an `UseAdoNetClustering` fallback so
  the app stays deployable on a plain Postgres box. All of `PostgreSQL-Main.sql` + `-Clustering` + `-Persistence`
  + `-Reminders` are applied. Dev/CI uses `UseLocalhostClustering` (no K8s needed for the inner loop).
- **Persistence + Reminders: ADO.NET on PostgreSQL.** `Microsoft.Orleans.Persistence.AdoNet` +
  `Microsoft.Orleans.Reminders.AdoNet` (10.2.1). Provider package: `Npgsql`. Set `Invariant = "Npgsql"` and the
  `ConnectionString`; for grain persistence set `UseJsonFormat = true` (debuggable, DBA-inspectable — see the
  [no-regression bar](#the-no-regression-bar)).
- **Two serializers, kept distinct (do not conflate).** `UseJsonFormat = true` governs only grain *storage*
  serialization in the stock AdoNet provider (its default `IGrainStorageSerializer` is `Newtonsoft.Json`, and it
  is pluggable). It does **not** govern the *cluster/wire* serializer that Orleans 7+ made mandatory: the
  version-tolerant `[GenerateSerializer]`/`[Id]` model (up to 170% higher end-to-end throughput than the pre-7
  serializer, per the migration guide). Grain-call arguments and returns, and any `[Immutable]` types, cross the
  wire through that serializer, so every F# type on a grain interface must round-trip through it (the F#
  interop risk, closed by [S15](#tier-1--foundation-de-risk-the-core-combination)). EggShell's subject *state* is
  serialized by the custom storage handler (its own codec), not by `UseJsonFormat`, so the storage-serializer
  decision and the wire-serializer work are independent.
- **Stock AdoNet grain persistence is not adopted for subject state.** Orleans' AdoNet persistence stores an
  opaque blob per grain and expects its Version/ETag to be a *signed 32-bit integer*, and it offers no secondary
  indexes, no full-text, no geospatial query, and no 2-phase commit. EggShell's custom `IGrainStorageHandler`
  provides all of those and uses a `ROWVERSION`/`xmin` ETag that does not fit a 32-bit `Version`, so the custom
  handler stays. Stock AdoNet persistence is used only where a plain blob-per-grain suffices (if anywhere); the
  stock reminder table is a separate evaluation ([S5](#tier-3--feature-ports)).
- **Setup scripts** live in the `dotnet/orleans` repo and are applied **Main first**, then per feature:
  `PostgreSQL-Main.sql`, then `PostgreSQL-Clustering.sql` (backs the non-K8s membership fallback, decision 4),
  `PostgreSQL-Persistence.sql`, `PostgreSQL-Reminders.sql`. Every silo must reach the database before start. They are applied via the
  `IDbSetup` Postgres implementation (bundle the upstream scripts as embedded resources, the same pattern as
  today's `SetupOrleans.sql`).
- **The custom `SubjectReminderTable`** is the awkward part. It bypasses the Orleans reminder table and
  piggybacks on subject tables with raw SQL Server commands. Two options: (a) port it to Npgsql (keep the
  piggyback design), or (b) switch to the standard `Microsoft.Orleans.Reminders.AdoNet` PostgreSQL reminder
  table and drop the custom implementation. Option (b) is cleaner and aligns with the upstream schema, but
  changes reminder semantics — [S5](#tier-3--feature-ports) evaluates against the "biosphere communication and
  reminders" dependency the `.fsproj` comments flag. Concrete contract to match if the custom table is kept: the
  stock `OrleansRemindersTable` keys on `(ServiceId, GrainId, ReminderName)`, carries an explicit integer
  `Version` incremented on upsert (`ON CONFLICT DO UPDATE ... RETURNING`, not `xmin`), and serves
  `IReminderTable`'s range reads by partitioning on `GrainHash` (the `ReadRangeRows1Key`/`ReadRangeRows2Key`
  queries). Any custom Postgres reminder table must replicate that `GrainHash`-range-read contract.
- **Cut-over:** because grain identity and the persistence schema change at Orleans 7+, this is a fresh-cluster,
  re-seed exercise, not an in-place system-table migration. The Orleans control-plane tables are created fresh
  from the upstream scripts.

### Orleans 10 modernization (adopt, don't reimplement)

Orleans 10 carries features that directly serve "max native Orleans + reliability + perf." These replace custom
machinery and are adopted *during* the port, not after.

- **Durable Jobs + journaling catalog.** Orleans 10 added native Durable Jobs (with polling, poisoned-shard
  handling, observable scheduling). The repo's custom `EnqueueAction` / `RetryPermanentFailures` /
  `StalledSideEffects` / `StalledPrepared` / `FailedSideEffects` machinery is a hand-rolled durable-job system.
  **[S6](#tier-4--modernize-maximize-features-do-these-in-the-port-not-after) maps the custom semantics to
  Durable Jobs and decides: delete / partial-replace / keep.** The 2-phase-commit grain-state persistence itself
  stays (that is grain storage, not a job), but the side-effect/action queue is the candidate for replacement —
  the single biggest simplification payoff.
- **Native observability.** Orleans 10 moved metrics to `IMeterFactory`, added DiagnosticSource events for
  activation/persistence/migration, and ships distributed-tracing spans. Combined with `Npgsql.OpenTelemetry`
  and PG `pg_stat_statements` / per-backend I/O stats, this supersedes both
  `Microsoft.Orleans.OrleansTelemetryConsumers.AI` (deleted) and the custom `Diagnostics/*.sql` scripts
  (`decode.sql`, `encode.sql`, `StalledTimers.sql`, `StalledPrepared.sql`, `StalledSideEffects.sql`,
  `FailedSideEffects.sql` — replaced by runtime metrics + PG stats views). [S11](#tier-4--modernize-maximize-features-do-these-in-the-port-not-after)
  wires this into the dev silo.
- **`TimeProvider`.** Orleans 10 uses `TimeProvider` in Reminders, AsyncTimer, and Streams — injectable,
  deterministic test time. This fits the repo's deterministic-simulation ethos; adopt it in the simulation suite
  (S11) for deterministic reminder/timer behavior.
- **Built-in dashboard.** `Microsoft.Orleans.Dashboard` (part of `Microsoft.Orleans.Server`) replaces the
  community `OrleansDashboard` 3.1.0. The 10.2.0 release added a lifecycle dependency graph to the dashboard.
- **Membership provider.** Decision 4 settled this: **`Orleans.Clustering.Kubernetes` 10.0.1** preferred when the
  substrate is K8s, `UseAdoNetClustering` on Postgres as the real non-K8s fallback, `UseLocalhostClustering` for
  dev/CI, all one `IMembershipTable` seam, a per-environment config switch. The upstream clustering script is
  ported (not skipped) so the fallback is runnable. K8s path validated in [S14](#tier-5--validation--perf-acceptance).
- **Runtime placement, directory, and failure detection (free wins from the jump).** Orleans 9.x/10 default to
  a strong-consistency grain directory, `ResourceOptimizedPlacement` as the default placement (9.2+), and
  memory-based activation shedding; failure detection dropped from ~10 minutes (3.x) to ~90 seconds. Orleans 8.2+
  added experimental Activation Repartitioning (automatic grain rebalancing). Most of these come simply by
  upgrading and directly serve the reliability + performance goal; `ResourceOptimizedPlacement` and
  repartitioning are worth an explicit review for the lifecycle workload during S1/S14.
- **Rolling-upgrade compatibility.** 10.2.0 hardened distributed grain-directory compatibility for rolling
  upgrades. Not directly relevant (we are fresh-cluster, not rolling), but it means the target line is stable.

## Durable Jobs evaluation

The custom durable machinery (ranked #2 by simplification payoff) deserves its own callout because it is the
biggest potential deletion. The existing pieces:

- **Queue/enqueue:** `EnqueueAction.sql`.
- **Retry:** `RetryPermanentFailures.sql`.
- **Stalled detection:** `StalledTimers.sql`, `StalledPrepared.sql`, `StalledSideEffects.sql`,
  `FailedSideEffects.sql` (diagnostics).
- **2-phase commit (grain-state persistence, stays regardless):** `PrepareStateShared.sql`, `PrepareUpdate.sql`,
  `PrepareInitialize.sql`, `UpdateState.sql`, `RollbackStateShared.sql`, `RollbackPreparedUpdate.sql`,
  `RollbackPreparedInitialize.sql`, `SetTickState.sql`, `SetTickStateAndSubscriptions.sql`.

[S6](#tier-4--modernize-maximize-features-do-these-in-the-port-not-after) prototypes mapping the enqueue/retry/
stalled/failed semantics onto Orleans 10 Durable Jobs + the journaling catalog. Likely outcome: **partial
replace** — the 2-phase-commit grain-state path stays (it is storage, not a job), but the side-effect/action
queue and its stalled/failed diagnostics move to Durable Jobs. If Durable Jobs covers the full semantics, the
custom queue and its `Diagnostics/*.sql` are deleted; otherwise the subset that Durable Jobs does not cover
stays as a thin Postgres handler. Gated *after* [S4](#tier-2--core-storage-the-hard-part) so the existing 2PC
semantics are understood first.

## Spike-driven execution

The work is organized as **spikes that test the full upgrade end-to-end**, tiered so the foundation is proven
before the expensive storage work begins. Each spike is time-boxed, produces green evidence + a decision, and
maps to the original phase arc (P0–P6) for continuity. **Every refactor commit stays green** — no half-stabilized
state is committed (lesson from an earlier Postgres spike branch: duplicate members left in
`SqlServerGrainStorageHandler.fs`, resource leak from a removed `finally` in `SqlServerSetup.fs`).

Recommended order: **S0 → S10 → S15 → S15b → S15b-production-port → S15c → S15c-production-port → S15d → S15d-production-port → S15e → S15e-production-port → S1 → S9 → (S3 ‖ S2) → S4 → (S5 ‖ S7 ‖ S8) → S6 → S11 → S12 → S13 → S14.**
S0+S10+S15+S15b+S15b-production-port+S15c+S15c-production-port+S1 first (~7–9 days) prove the foundation before the big storage work. S15 ran right after the package
bump (S10) because the Orleans 7+ serializer change is discovered there and F# interop gates everything. S15b confirmed the
production codegen-host pattern; **S15b-production-port** is the actual LibLifeCycleCore rewrite (not a spike — applies the
S15b pattern to the real codebase, ~1–2d). **S1 is gated by S15b-production-port** — LibLifeCycleCore must compile under
Orleans 10 before the throwaway PG18 console can boot a real silo. S2/S3 are independent and parallel; S4 is
gated after S4. S14 (K8s membership) is last — it depends on the silo being otherwise complete, and the localhost path (S1) already
covers dev/CI.

### Tier 0 — prerequisites (unblock all)

- **S0 · net10 TFM + Central Package Management.** *(revised — NOT a ~0.5d backend-only task; maps to P1 prereq.)*
  **Correction (verified 2026-07-16):** the original premise — "bump `LibLifeCycle{Core,Host,Types,Ui,Test}` +
  `LibLifeCycle` to `net10.0`" as an isolated backend step — is **wrong**. Those are not backend-only projects:
  the **shared type libs (`LibLangFsharp`, `LibLifeCycleTypes`, `Todo.Types`/`Jobs.Types`, `LibLifeCycle`) are
  `ProjectReference`d directly by the Fable frontend** (`LibClient`, every `LibUi*`, `App*`, `ThirdParty/*`, the
  gallery — ~45 projects hang off `LibLangFsharp` alone). A net7 project **cannot** consume a net10 project, so
  bumping any shared lib **breaks the entire frontend build**. Only two closure projects (`LibLifeCycleTest`'s
  `Tests`, `LibLifeCycleHost`) are truly backend-only. **Consequence:** the net10 TFM bump is **atomic and
  repo-wide** — frontend + backend move together, or not at all (net10 *can* consume net7, so the one-way blocker
  is only net7-consuming-net10). This is why CLAUDE.md defers the repo-wide TFM migration; it is not a small PR.
  - **What IS separable and is already done** (PR #21, branch `s0/net10-backend-build-fixes`): making the *net7*
    code compile + run the simulation suite under the **net10 SDK** (SRTP explicit-type-arg fixes in
    `LibLifeCycleCore`; codec-generator emit-order forward-reference in `Todo.fs`; `LibLifeCycleTest`
    `PlatformTarget` conditional for arm64; `Microsoft.NET.Test.Sdk` bump). Verified: `SuiteTodo` sims 5/5 green
    on aarch64 (requires the `eggshell-signalr` sibling checked out + `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`
    where `libicu` is absent). This proved "does the net10 SDK build the backend as-is" → **no**, and fixed it.
  - **What remains for the actual TFM bump** (do as one repo-wide change, with the frontend Fable 5 toolchain):
    move every `net7.0` TFM to `net10.0` together, then add `Directory.Packages.props` (CPM) pinning the *current*
    package versions first (keep Orleans 3.7.2 etc.) to isolate the TFM bump from the package bumps.
- **S10 · net10 + Orleans 10.2 + Npgsql 10 package bump (isolated branch).** *(~1–2d, maps to P1.)*
  On top of S0, swap all Orleans packages → 10.2.1; add `Npgsql` 10.0.3; add
  `Microsoft.Orleans.{Clustering,Persistence,Reminders}.AdoNet` 10.2.1; drop `OrleansDashboard` and
  `Microsoft.Orleans.OrleansTelemetryConsumers.AI` (replaced by built-in dashboard + native metrics). Keep
  `Microsoft.Data.SqlClient` temporarily. Expect compile breaks (Orleans 7.0 wire/identity/API changes).
  **Catalog every break** — this is the surface area of the Orleans jump. **Proves:** the size of the
  Orleans upgrade.

### Tier 1 — foundation (de-risk the core combination)

- **S1 · Orleans-on-PG18 baseline (throwaway console).** *(~1d, maps to P0 spike.)*
  Apply upstream `PostgreSQL-Main` + `-Persistence` + `-Reminders` to `postgis/postgis:18-3.5` (the S1 baseline
  uses localhost clustering, so `-Clustering` is exercised later in the PG-fallback path, decision 4). Run a
  trivial silo: `UseLocalhostClustering` + ADO.NET persistence
  (`UseJsonFormat`) + reminders, invariant `Npgsql`, on net10 + Orleans 10.2.1. Confirm: silo boots, grain state
  survives restart, reminder fires. Run on the **arm64 dev machine** and confirm the `postgis/postgis:18-3.5`
  image is the native arm64 variant (not emulated amd64) with FTS + PostGIS queries working. **Proves:** the
  Orleans + Npgsql + PG18 + net10 combo works before touching real storage, on both arm64 and x86. (This is the
  original Phase-0 spike, with versions bumped and clustering de-scoped to S14.)
- **S9 · Schema-migration tooling.** *(~0.5d, maps to P2 tooling.)*
  Prototype **DbUp** (`dbup-postgresql`) journal vs porting the F# `ApplyScriptIfNewOrChanged` hash-tracked
  `Upgrades_Auto` mechanism to PG. Pick DbUp (versioned, idempotent, journal table) unless the spike shows a
  blocker. **Proves:** the tooling decision.
- **S15 · Orleans 10 wire serializer x F# records/DUs.** *(~1d, maps to P0 spike; gates the Orleans jump.)*
  Define a representative F# grain interface whose arguments and returns mirror real subject shapes (`Action` /
  `LifeEvent` / `Constructor` discriminated unions with nullary, single-field, and multi-field cases; records;
  `Option`; nested collections). Add `Microsoft.Orleans.Serialization.FSharp`; annotate per the
  `[GenerateSerializer]`/`[Id]` model (or confirm the F# package's automatic handling). Call the grain across two
  real silos and assert exact round-trip of every shape. **Proves:** F# type interop with the Orleans 7+
  serializer, the single highest-risk part of the Orleans jump. F# DU serialization was broken in early Orleans
  7.x (issue #8255) and fixed via PR #9095 in `Microsoft.Orleans.Serialization.FSharp`; this spike confirms it
  holds on 10.2.1 for the repo's actual shapes. Pass/fail: every representative F# type round-trips across a real
  grain call; a failure escalates before any storage work begins.

  **STATUS (2026-07-18, session 40): S15 FAILED the no-regression bar — 1 of 10 shapes round-tripped.**
  Catalog: `spikes/s15-serializer-roundtrip.md`. 7 critical findings: source-gen is C#-only;
  `Microsoft.Orleans.Serialization.FSharp` ships only built-in codecs (Unit/Option/ValueOption/Choice —
  NOT user records/DUs/Result); source-gen does NOT scan referenced assemblies by default; the seam is
  `[assembly: GenerateCodeForDeclaringAssembly(typeof(SomeTypeInFsharpAssembly))]` in a C# project;
  F# nullary union cases compile to private nested classes the C# generator can't access (CS0122); the
  per-case `[<Id(n)>]` attribute is NOT honored by the emitted C# codec (genuinely novel finding, no
  upstream report). 6 of 7 findings were upstream-documented (dotnet/orleans #8520, #8717, official API
  docs). Process miss: upstream research was not done BEFORE writing the .fsproj — added as CLAUDE.md rule 14
  + codemem 1893, codemem 1894 (spike-driven skill). The custom wire serializer in
  `LibLifeCycleCore/src/OrleansEx/Serializer.fs` CANNOT simply be deleted — must be rewritten.

- **S15b · Production codegen-host pattern for F# grain interfaces + custom IGeneralizedCodec.** *(~1d,
  maps to P0 spike; supersedes S15's pessimistic verdict.)*
  Throwaway 4-project spike (Types / Grains / Codegen / Host) validating the production porting pattern:
  C# Codegen project carries two `GenerateCodeForDeclaringAssembly` attributes (one per F# assembly —
  interfaces in Types, grain impls in Grains); `[<assembly: InternalsVisibleTo("Codegen")>]` on the F#
  Types project exposes nullary-case internal classes (sidesteps S15 finding #5's `of unit` cascade tax —
  NO caller changes); custom `IGeneralizedCodec`+`IGeneralizedCopier`+`IFieldCodec`+`IDeepCopier`
  (registered via DI) wraps the existing Fleece/STJ wire serializer (sidesteps S15 finding #7's source-gen
  DU mis-routing); `[<assembly: Orleans.ApplicationPartAttribute("...")>]` on the F# Host wires in the
  C#-generated invokers + the F# grain impls (dotnet/orleans #8235 workaround).

  **STATUS (2026-07-18, session 41): S15b PASSED — 7 of 7 representative shapes round-trip through the
  production-shape codegen-host pattern via a 2-silo in-process TestCluster.** Catalog:
  `spikes/s15b-production-codegen.md`. Codemem 1895. 7 critical findings (5 of 7 upstream-documented:
  #8717 InternalsVisibleTo, #8520 GenerateCodeForDeclaringAssembly, official IGeneralizedCodec docs, #8235
  F# host ApplicationPartAttribute). Decision: the custom wire serializer MUST be rewritten as
  IGeneralizedCodec+IGeneralizedCopier+IFieldCodec+IDeepCopier; a new C#
  `LibLifeCycleCodeGenHost.csproj` sibling project must be added to LibLifeCycleCore; `[InternalsVisibleTo]`
  on LibLifeCycleCore; `[Orleans.ApplicationPartAttribute(...)]` on the F# Host.

- **S15b-production-port · (NOT a spike — production work item).** Apply the S15b spike pattern to the
  real `LibLifeCycleCore`. Files: rewrite `LibLifeCycleCore/src/OrleansEx/Serializer.fs` against the 4
  new interfaces (port the 83 TypeId entries from the existing `UntypedSerializer.ForIsomorphicType`
  table — the byte TypeId scheme stays, JSON body serialization reuses Fleece+gzip); rewrite
  `OrleansEx/Serialization.fs` to register the new codec via DI (replace removed
  `SerializationProviderOptions`/`IApplicationPartManager`/`SerializerFeature`); rewrite
  `GrainConnectorProvider.fs` for the new `UseOrleansClient` generic-host pattern (replaces removed
  `ClientBuilder`/`IClientBuilder`); fix the `GrainConnector.fs:126` Task CE Bind overload mismatch
  (FSharp.Core 10 / .NET 10 effect); add a new C# `LibLifeCycleCodeGenHost/LibLifeCycleCodeGenHost.csproj`
  sibling project carrying the two `GenerateCodeForDeclaringAssembly` attributes; add
  `[<assembly: InternalsVisibleTo("LibLifeCycleCodeGenHost")>]` to `LibLifeCycleCore`; add
  `[<assembly: Orleans.ApplicationPartAttribute("LibLifeCycleCodeGenHost")>]` +
  `[<assembly: Orleans.ApplicationPartAttribute("LibLifeCycleHost")>]` to the F# Host's startup
  (`DevelopmentHost.fs` or equivalent). The 50 distinct S10 compiler errors all resolve through this
  work item. GATES S1 (the throwaway PG18 console can't boot a real Orleans 10 silo until
  `LibLifeCycleCore` compiles).

  **STATUS (2026-07-19, session 42): COMPILE GREEN (0 errors 0 warnings on all 5 affected projects:
  LibLifeCycleCore + LibLifeCycleHost + LibLifeCycleTest + LibLifeCycleHostBuild + new C#
  LibLifeCycleCodeGenHost).** The serializer rewrite, client-builder ports, grain lifecycle port
  (`IGrainActivationContext` → `IGrainContext`, `OnActivateAsync`/`OnDeactivateAsync` signatures,
  `ReminderEntry.GrainId`, `IInvokable.GetArgument`), `UseDashboard` deferral (TODO S11),
  `ApplicationPartAttribute` + `InternalsVisibleTo` wiring, and the new C# codegen-host project are
  all in place. BUT: the Orleans 10 source generator emits BROKEN C# when scanning production-shape
  F# assemblies (misclassifies F# Task-CE/object-expression closures as `InterfaceImplementations`,
  emitting invalid C# referencing F# mangled names like `GrainConnector.RunAndWait@117-1<...>`).
  Both `GenerateCodeForDeclaringAssembly` attributes are COMMENTED OUT -> no source-gen invokers ->
  runtime grain dispatch will fail. The `ConnectorGrain.fs` `interface IConnectorGrain<...> with`
  block is also commented out (F# 10 FS0909 on constraint-bearing generic interfaces). Both deferred
  to **S15c spike** (find workaround). Engineering log session 42. S1 cannot proceed until S15c
  unblocks runtime dispatch.

- **S15c · Orleans 10 source-generator F# closure misclassification. SPIKE PASSED (session 43).**
  *(gated S1; now resolved.)* Throwaway spike `Meta/s15c-closure-codegen/`; catalog
  [`spikes/s15c-closure-codegen.md`](spikes/s15c-closure-codegen.md). Root cause (verified in
  `~/Code/orleans` v10.2.1, `CodeGenerator.cs:224-259`): the generator adds any non-abstract
  public/internal class implementing a `[GenerateMethodSerializers]`-annotated (inherited) interface to
  `InvokableInterfaceImplementations` with **no `IsCompilerGenerated()` filter**. `IGrainObserver :
  IAddressable` (carries the attribute), so F#'s compiler-emitted object-expression closures get flagged,
  and `MetadataGenerator.cs:79-84` emits an invalid `config.InterfaceImplementations.Add(typeof(...observer @ 33))`
  (`@` is not a valid C# identifier -> CS1646/CS1026/CS0426). **Fix (verified PASS, real 2-silo
  grain-observer round-trip): lift the grain-observer object expressions to named top-level F# classes**
  (valid `typeof`); the extra manifest registration is benign at runtime. Genuinely novel (no upstream
  issue). **The "FS0909" ConnectorGrain sub-blocker was a MISDIAGNOSIS** -- uncommenting the real block +
  building surfaced `FS0010` on a stray `=>` typo at `ConnectorGrain.fs:133` *inside the commented block*
  (never type-checked). Fixing `=>` -> `->` compiles green; no FS0909, no constraint drop, no LibLifeCycle
  cascade. **Follow-up production-port worklist (S15c-production-port):** (a) lift `GrainConnector.fs:117`
  (`ILifeEventAwaiter` awaiter) + `Web/RealTime.fs:103` (`ISubjectGrainObserver`) object expressions to
  named F# classes (grep production for `{ new I.*Observer` / `{ new I.*Awaiter`); (b) re-enable both
  `GenerateCodeForDeclaringAssembly` attributes in `LibLifeCycleCodeGenHost/CodegenAssemblyInfo.cs`;
  (c) uncomment `ConnectorGrain.fs`'s interface block + fix the `=>` typo; (d) rebuild all 5 backend
  projects green + run a real grain round-trip; (e) file the upstream orleans issue (one-line
  `!symbol.IsCompilerGenerated()` guard) with `Meta/s15c-closure-codegen/` as the minimal repro.

- **S15c-production-port · LANDED (session 44).** Applied (a)-(c) above: `GrainConnector.fs` +
  `Web/RealTime.fs` object expressions lifted to named classes; `ConnectorGrain.fs` typo fixed + wired;
  both `GenerateCodeForDeclaringAssembly` attributes re-enabled; `InternalsVisibleTo` added to
  LibLifeCycleHost. `LibLifeCycleCodeGenHost` builds green with valid invokers; the 93-test
  `LibLifeCycleTest` unit suite passes. Booting the real silo then surfaced two further Orleans-10 runtime
  blockers the codegen gate had hidden: **(1) Autofac keyed services** (ADI 6.0.0 predates .NET 8 keyed
  services -> `RegisterInstance(null)`; fixed by bumping ADI 6.0.0 -> 10.0.0 + Autofac 6.1.0 -> 8.3.0 in
  the test projects, autofac ADI #112/#116); **(2) F# wrapper serialization** -> spike **S15d**.

- **S15d · Orleans 10 decomposes F# Option/ValueOption/Choice/Result natively. SPIKE PASSED (session 44).**
  *(gates S1.)* Throwaway spike `Meta/s15d-fsharp-wrapper-codecs/`; catalog
  [`spikes/s15d-fsharp-wrapper-codecs.md`](spikes/s15d-fsharp-wrapper-codecs.md). The
  `Microsoft.Orleans.Serialization.FSharp` 10.2.1 package ships `FSharpOption`/`ValueOption`/`Choice`/`Result`
  codecs (Result is NEW vs S15 finding #2) that decompose the wrapper and delegate to inner leaf codecs; an
  `IGeneralizedCodec` (fallback) never wins over them, so the S15b custom codec's whole wrapper
  registrations (`Option<BlobData>`, `Result<unit, GrainRefreshTimersAndSubsError>`, ...) leave the bare
  inner leaf uncovered -> `CodecNotFoundException` at silo boot. **Fix (verified PASS, 3/3 real 2-silo
  round-trip): register the bare LEAF types, not the wrappers.**

- **S15d-production-port · LANDED (session 45).** Applied the spike decision to production:
  - `Serializer.fs`: new `decomposeToNativeLeaves` + `orleansNativeLeafTypes`; retired 23 whole-wrapper
    TypeIds (8/15/17-19/21-22/32/36-39/45-46/53-55/59/62-66/68/71/73/77/79-83) with leaf-migration comments;
    added 20 new bare-leaf registrations at TypeIds 84-103 (subject) + 4/5 (view, retiring old TypeId 3).
  - Both in-file coverage guards (redundancy check + "must define serializers" check) made
    decomposition-aware -- peel declared grain param/return types via `decomposeToNativeLeaves` before
    testing; `isOrleansHandledLeaf` predicate covers native primitives + grain-observer interfaces.
  - **Second bug (NOT from the spike):** `SiloBuilder.ConfigureSiloClientForEcosystem`'s
    `TestCluster`/`TestClusterDataSeeding` branch never called `configureSiloClientSerializers`, so the
    test-client DI never registered `EggShellSubjectGrainsCodec`. Orleans 3.x's lazy codec resolution
    masked this for years; Orleans 10's eager `AnalyzeSerializerAvailability` at `ClusterClient..ctor`
    makes it fatal. Fixed: added the call. **Lesson:** an Orleans 3.x -> 10 migration must audit EVERY
    `IClientBuilder` path for codec registration, not just the silo -- the in-process TestCluster does NOT
    inherit the silo's DI container.
  - **Verification:** build 0 err 0 warn on all 5 affected + 2 Ecosystem test projects; `LibLifeCycleTest`
    **93/93 PASS**; `SuiteTodo/Ecosystem/Tests` **5/5 PASS** (real 2-silo grain round-trips).
  - **SuiteJobs follow-up (resolved by S15e-production-port, session 46; see below):** at the time of
    S15d, SuiteJobs tests ran for the first time under .NET 10 / Orleans 10 and showed **18/47 PASS,
    29 FAIL** with a uniform `Stasis not reached` failure pattern. Root cause identified and fixed in
    S15e; final result **47/47 PASS**.

- **S15e · `IConnectorGrain` command objects: replace F# closures with serializable value-like
  builder/mapper interfaces. LANDED (session 46).** *(closed the SuiteJobs Stasis blocker; gates S1.)*
  Catalog [`spikes/s15e-connector-command-objects.md`](spikes/s15e-connector-command-objects.md).
  The Orleans 10 source-generated invoker deep-copies every grain method argument before dispatch.
  `IConnectorGrain` previously accepted request-builder and response-mapper arguments as F# closures
  (`ResponseChannel<'Reply> -> 'Request` and `'Reply -> 'Action`), which compile to F#-compiler-generated
  closure classes not supported by `Microsoft.Orleans.Serialization.FSharp`. `CopyContext.DeepCopy` threw
  `CodecNotFoundException: Could not find a copier`, the connector transient side effect never completed,
  and the 15-second stasis wait timed out.

  **Fix:** add generic command-object interfaces in `LibLifeCycle.Services`
  (`IConnectorRequestBuilder`, `IConnectorRequestBuilderSingleReply`, `IConnectorResponseMapper`), change
  `IConnectorGrain` to tupled methods that take these interfaces, and implement named carrier classes
  per connector (`JobRunnerRequestBuilder`, `JobRunnerResponseMapper`). The grain implementation calls
  `.Build(channel)` and `.Map(response)`; the codec's `IGeneralizedCopier.IsSupportedType` recognizes any
  concrete implementation of the interfaces and returns the same instance during deep-copy. Builds on the
  S15b tupled-method lesson and the S15c named-class lesson.

  **Is the identity-copier production-grade? Yes.** `IConnectorGrain` is a `StatelessWorker` and always
  activates on the calling silo (`ConnectorGrain.fs:15`; the serializer file notes "no need to cover
  IConnectorGrain because it's always executed locally"), so the builder/mapper carriers are never
  serialized over the wire -- the only copy that runs is local-call isolation, where identity-copy of an
  immutable read-only carrier is correct. The catalog's follow-up is a **conditional invariant to guard,
  not a pending deficiency**: it applies only if connector grains ever stop being local (remote
  activation) or a carrier gains mutable state. See catalog Finding 5.

  **Verification:** build 0 errors across `LibLifeCycleCore`, `LibLifeCycleHost`,
  `LibLifeCycleCodeGenHost`, `LibLifeCycleTest`, `SuiteTodo/Ecosystem/Tests`, `SuiteJobs/Ecosystem/Tests`;
  `LibLifeCycleTest` **93/93 PASS**; `SuiteTodo/Ecosystem/Tests` **5/5 PASS**; `SuiteJobs/Ecosystem/Tests`
  **47/47 PASS** with zero "Stasis not reached", zero `CodecNotFoundException`, and zero
  "Could not find a copier". Generated `LibLifeCycleCodeGenHost.orleans.g.cs`: zero
  `typeof(global::...@...)` patterns.

### Tier 2 — core storage (the hard part)

- **S3 · Concurrency token: `xmin` vs explicit version column.** *(~1d, maps to P3.)*
  Port the 2-phase-commit ETag round-trip (`PrepareUpdate` → `UpdateState` concurrency check) to Npgsql both
  ways. Run concurrency-conflict tests. **Proves:** optimistic-concurrency works; decide `xmin` (preferred) vs
  explicit column. See [Concurrency token](#concurrency-token).
- **S4 · PostgresGrainStorageHandler (the pivotal spike).** *(~3–5d, maps to P3 — this IS most of the work.)*
  Implement `PostgresGrainStorageHandler` behind `IGrainStorageHandler` for the core path: `InitializeState`,
  `PrepareUpdate`, `UpdateState`, `ClearState`, `SetTickState`, `SetTickStateAndSubscriptions`. Run the
  deterministic **simulation suite (`TestDataSeeding`)** against PG, **diff results vs SQL Server** (the
  throwaway diff oracle). **Proves:** the real storage port. Largest spike.
- **S2 · TVP → unnest / COPY / JSONB benchmark.** *(~1.5d, maps to P2.)*
  One representative TVP call site (`SideEffectList_V2` + `IndexingList_V3`). Port three ways: (a)
  `unnest(array)`, (b) binary `COPY`, (c) JSONB + `jsonb_to_recordset`. Benchmark vs PG18 at realistic row
  counts. **Proves:** the [TVP strategy](#tvp-strategy) + the perf open question; pick the per-volume winner.
  The JSONB arm doubles as the list-type TVP reconsideration. Also settles the identifier-case convention
  (`lower_snake_case` recommended).

### Tier 3 — feature ports

- **S5 · Reminders decision.** *(~1.5d, maps to P4.)*
  (a) Port `SubjectReminderTable` → Npgsql keeping the piggyback design; (b) adopt the standard
  `Microsoft.Orleans.Reminders.AdoNet` PostgreSQL table. Test register/tick/cancel both. Audit the "biosphere
  communication + reminders" coupling the `.fsproj` warns about. **Proves:** port-vs-adopt decision.
- **S7 · Full-text search parity.** *(~1.5d, maps to P2/P3.)*
  Port the `SqlServerSubjectRepo` CONTAINS/FREETEXT builder → `tsvector`/`tsquery` + GIN (stored generated
  `tsvector` column). Confirm every search feature used is expressible. Diff query results vs SQL Server.
  **Proves:** FTS parity open question.
- **S8 · Geography / PostGIS parity.** *(~1d, maps to P2/P3.)*
  Port `SqlServerIndexQueryOptimizer` GEOGRAPHY/STDistance → PostGIS `geography`/`ST_Distance`/`ST_DWithin`.
  Verify `ST_DWithin` uses GiST (range queries), not `ST_Distance` in `WHERE`. Diff results. **Proves:** the
  spatial port.

### Tier 4 — modernize / maximize features (do these IN the port, not after)

- **S6 · Durable Jobs vs custom side-effect queue.** *(~2d, maps to P3 simplification — gated after S4.)*
  Map `EnqueueAction` / `RetryPermanentFailures` / `StalledSideEffects` / `StalledPrepared` semantics to
  Orleans 10 Durable Jobs + the journaling catalog. Prototype replacing the custom durable queue. **Decision:**
  delete the custom machinery / partial replace / keep. See [Durable Jobs evaluation](#durable-jobs-evaluation).
  **Highest simplification payoff.**
- **S11 · Observability + TimeProvider.** *(~1d, maps to P5.)*
  Wire Orleans 10 metrics (`IMeterFactory`) + DiagnosticSource + OTel spans (persistence/activation/migration)
  + `Npgsql.OpenTelemetry` + PG `pg_stat_statements` / per-backend I/O stats into the dev silo. Adopt
  `TimeProvider` in the simulation suite for deterministic time. **Proves:** the observability surface for the
  perf + reliability pass; supersedes the custom `Diagnostics/*.sql` scripts.
- **S12 · PostgreSQL 18 native features adoption.** *(~1d, maps to P2/P3.)*
  Prototype: temporal `WITHOUT OVERLAPS` on subscription validity, `uuidv7()` for a representative ID column,
  `OLD`/`NEW` in `RETURNING` for an upsert path, `io_method=worker` AIO. **Proves:** which [PG 18
  features](#postgresql-18-native-features-to-adopt) earn a place in the schema.

### Tier 5 — validation + perf (acceptance)

- **S13 · Simulation suite on PG-only + perf pass.** *(~2–3d, maps to P5/P6.)*
  Full `TestDataSeeding` against PG18. Drop the SQL Server diff (now confident); **delete the SQL Server code
  and the scaffolding seam** (decision 2). `EXPLAIN ANALYZE` the index/search/geography/hot-write queries; tune
  GIN/GiST, `work_mem`, AIO, eager-freeze. **Proves:** the acceptance bar. There is no P6 cut-over — fresh seed
  only.
- **S14 · Kubernetes membership validation (gated last).** *(~1d, maps to P4.)*
  Gated last on purpose: membership sits on top of a complete silo (it needs a booting silo that already persists
  state + runs reminders to cluster at all), and it needs infra the inner loop lacks (a cluster + RBAC + a
  pod-status watch). The localhost path (S1) already covers dev/CI, so S14's absence blocks nothing earlier.
  Stand up a throwaway cluster (minikube or kind), deploy the silo with `Orleans.Clustering.Kubernetes` 10.0.1,
  and confirm: the silo registers, a second silo joins and membership converges, and a killed pod's membership
  entry is reaped from the **pod-status watch** (short-circuiting Orleans' own probe-timeout-and-vote), not from
  DB polling. Confirm persistence + reminders still reach the PG container. Also confirm the exact roster backing
  the provider uses (CRD/ConfigMap/pods), unverified at plan time. **Proves:** the K8s membership decision
  (decision 4) holds in the target environment. The PG `UseAdoNetClustering` fallback (decision 4) is the escape
  hatch if this spike fails; the localhost path (S1) is the dev/CI equivalent.

### Phase correspondence (for continuity)

| Original phase | Spikes | End-state change |
|---|---|---|
| P0 Decisions + spike | S1 (+ decisions 1–4, this revision) | done |
| P1 Close abstraction gap | S0, S10 | seam is scaffolding, deleted at S13 |
| P2 Postgres schema/DDL | S9, S2, S7, S8, S12 | + PG 18 features baked in |
| P3 Postgres handlers | S3, S4, S6 | + Durable Jobs simplification |
| P4 Orleans on Postgres | S5, S14 | reminder port-vs-adopt; K8s membership (decision 4) |
| P5 Dual-DB CI + simulation | S11, S13 | **PG-only** CI (no permanent dual-DB) |
| P6 Data lift + cut-over | (none) | **removed** — fresh seed, no live data |

## Tooling

- **Versioned schema (owned by the app):** **DbUp** with `dbup-postgresql` (depends on Npgsql). Runs ordered
  idempotent SQL scripts and tracks a journal table — the natural replacement for the current hash-tracked
  `Upgrades_Auto` mechanism. It deploys schema, not data. Decided by [S9](#tier-1--foundation-de-risk-the-core-combination).
- **Package management:** **Central Package Management** (`Directory.Packages.props`) adopted with the TFM
  bump (S0); ~12 packages across six `LibLifeCycle*` projects pinned in one place.
- **One-time data lift:** **not required** (decision 2 — no live data). `pgloader` (reads SQL Server via
  FreeTDS, auto-maps types incl. `varbinary` → `bytea`, validate the truncation caveat) stays in the toolbox
  only in case a one-time bulk import from an external SQL Server is ever wanted.
- **Dev stack:** a single `docker compose` file running the engine, folded into the planned `./dev-stack up`
  ([Phase 6 of the phased plan](./phased-plan.md)). SQL Server is **not** in the permanent dev stack (only
  during the port, as the diff oracle):

  ```yaml
  services:
    postgres:
      image: postgis/postgis:18-3.5    # PG 18 + PostGIS 3.5
      environment: { POSTGRES_PASSWORD: "postgres", POSTGRES_DB: "app" }
      ports: ["5432:5432"]
      volumes: ["pgdata:/var/lib/postgresql/data"]
  volumes: { pgdata: {} }
  ```

  Mount the Postgres volume at `/var/lib/postgresql/data` exactly (the parent will not persist). Add a transient
  `mssql:` service (2022-latest) only while the diff oracle is needed.

## Testing strategy

- **The simulation suite is the primary safety net.** The deterministic simulation tests (see
  [Testing framework](../architecture/testing-framework.md)) exercise lifecycles through the storage layer.
  Running them in `TestDataSeeding` mode against Postgres is the acceptance bar for each handler.
- **SQL Server as a transient diff oracle (during the port only).** For the query-heavy paths (subject repo,
  FTS, geography), diff Postgres results against SQL Server for the same inputs *before* removing the SQL
  Server path. This is transient validation, not a permanent dual-DB CI leg.
- **PG-only CI (after confidence).** Once S4/S7/S8 diffs are clean, CI runs the simulation suite against
  Postgres containers only. The in-memory `Storage/Test` fakes stay for fast unit tests. The
  `DatabaseProvider` switch and the SQL Server code are deleted (S13).
- **Concurrency tests.** The 2-phase-commit + concurrency-token logic is the highest-risk area; [S3](#tier-2--core-storage-the-hard-part)
  adds targeted tests for optimistic-concurrency conflicts under the chosen `xmin`/version-column strategy.
- **Perf pass evidence.** [S13](#tier-5--validation--perf-acceptance) uses `EXPLAIN ANALYZE`, `pg_stat_statements`,
  and PG 18 per-backend I/O/WAL stats (not the custom `Diagnostics/*.sql` scripts) to tune GIN/GiST indexes,
  `work_mem`, AIO, and eager-freeze.

## Risks and residual open questions

**Decisions settled this revision** (governed by the [no-regression bar](#the-no-regression-bar)):

- **Deployment target / membership provider, DECIDED: K8s preferred, not mandated.** K8s is not a hard given;
  the design stays substrate-flexible. `Orleans.Clustering.Kubernetes` 10.0.1 when running on K8s (best-native,
  pod-status watch feeding Orleans' own protocol), `UseAdoNetClustering` on Postgres as a real non-K8s fallback,
  `UseLocalhostClustering` for dev/CI (decision 4). No regression: K8s liveness is more reliable than DB-polling
  membership, the PG fallback keeps orchestrator-free deployment open, and the dev loop needs no cluster.
  Validated by [S14](#tier-5--validation--perf-acceptance).
- **Durable Jobs appetite — DECIDED: adopt if S6 proves parity.** Delete the custom durable side-effect/action
  queue only if [S6](#tier-4--modernize-maximize-features-do-these-in-the-port-not-after) shows Orleans 10
  Durable Jobs covers the enqueue/retry/stalled/failed semantics with no regression; otherwise keep the
  uncovered subset as a thin Postgres handler. The 2-phase-commit grain-state persistence stays regardless.
- **Identifier case convention — DECIDED: `lower_snake_case`.** No regression (no mixed-case legacy to preserve
  since the SQL Server code is deleted); improves the DBA experience (no quoting friction in `psql`/`pg_dump`).
  Applied during [S2](#tier-2--core-storage-the-hard-part).
- **Serializer — DECIDED: keep JSON.** `UseJsonFormat = true`. No regression on debuggability/DBA
  inspectability — JSON payloads stay inspectable, matching the current gzip-JSON-in-`VARBINARY` approach.
  MemoryPack (perf) is deferred to the perf pass; revisit only if a hot path demands it and the DBA-inspectability
  loss is acceptable for that path alone.

**Residual open questions** (each closed by its spike):

- **Reminder table.** Port the custom `SubjectReminderTable` to Npgsql vs adopt the standard AdoNet reminder
  table. The `.fsproj` comments tie "biosphere communication and reminders" to Orleans 3.7.2, so this needs a
  careful semantics review on the target Orleans version. Decided by [S5](#tier-3--feature-ports).
- **Concurrency token strategy.** `xmin` (preferred) vs explicit version column. [S3](#tier-2--core-storage-the-hard-part)
  validates against the actual ETag round-trip in `SqlServerGrainStorageHandler.fs`. Fallback to the explicit
  column only if `xmin` proves incompatible — the [no-regression bar](#the-no-regression-bar) requires the
  ETag semantics to be preserved exactly.
- **TVP-to-`unnest` performance.** For the highest-volume indexing/side-effect writes, `unnest` of a large
  array may underperform SQL Server TVPs; binary COPY or JSONB may be needed for those specific paths (this is
  the one place a *performance* regression risk exists). Measure in [S2](#tier-2--core-storage-the-hard-part)
  and [S13](#tier-5--validation--perf-acceptance); the per-volume strategy ensures no blanket regression.
- **Full-text parity.** PostgreSQL FTS is less feature-rich than SQL Server's; [S7](#tier-3--feature-ports)
  confirms the actual search features used (`SqlServerSubjectRepo.fs`) are all expressible in `tsquery` — this
  is the feature-parity check against the no-regression bar. Note PG 18 changed FTS collation handling — reindex
  FTS indexes after any `pg_upgrade`.
- **F# type serialization under the Orleans 10 wire serializer.** Orleans 7+ mandates the
  `[GenerateSerializer]`/`[Id]` serializer for all cross-grain types. F# DU serialization was broken in early
  Orleans 7.x (issue #8255) and fixed via PR #9095 in `Microsoft.Orleans.Serialization.FSharp`; confirm it holds
  on 10.2.1 for the repo's actual DU/record shapes. Closed by [S15](#tier-1--foundation-de-risk-the-core-combination).
  This is separate from the storage `UseJsonFormat` decision and is the highest-risk item in the Orleans jump.
- **Npgsql sync-API deprecation.** Any synchronous DB access must be async by Npgsql 10. Audit the ported
  handlers (S4) — the current SQL Server code may use sync APIs that have no async Npgsql equivalent without
  rewriting the call sites.
- **Exact upstream point releases.** Confirm Orleans (10.2.1 → 10.2.2 when green), Npgsql (10.0.3), and
  `Orleans.Clustering.Kubernetes` (10.0.1) point versions on nuget.org before pinning.

## References

Internal:
- [Goal G — PostgreSQL + Orleans upgrade](./goals-and-roadmap.md)
- [Phased Plan — Phase 6](./phased-plan.md)
- [Hosting & Persistence](../architecture/backend-hosting-persistence.md)
- [Testing framework](../architecture/testing-framework.md)
- [Tracked upstream Orleans issues](./tracked-orleans-issues.md) — re-check before each Orleans work item; resolution may let us simplify the codebase.
- Spike catalogs: [S10 (pkg-bump break)](./spikes/s10-orleans-bump-catalog.md), [S15 (FAILED — wire serializer)](./spikes/s15-serializer-roundtrip.md), [S15b (PASSED — production codegen-host pattern)](./spikes/s15b-production-codegen.md), [S15e (LANDED — connector command objects)](./spikes/s15e-connector-command-objects.md)

Upstream (confirmed 2026-07-15):
- Orleans releases: https://github.com/dotnet/orleans/releases (10.2.1 stable 2026-06-24; 10.2.2-rc.1 2026-07-10; 10.0 multi-targets net8+net10)
- `Microsoft.Orleans.Server` 10.2.1: https://www.nuget.org/packages/Microsoft.Orleans.Server
- Orleans 10.2.0 highlights (grain-directory reliability, Durable Jobs, journaling, `ValidateInitialConnectivity`, observability, rolling-upgrade compat): https://github.com/dotnet/orleans/releases/tag/v10.2.0
- Orleans ADO.NET configuration and invariants: https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/adonet-configuration
- Orleans migration guidance: https://learn.microsoft.com/en-us/dotnet/orleans/migration-guide
- Orleans 7.0 breaking changes (identity/wire): https://www.infoq.com/news/2022/12/orleans-dotnet-7/ , https://github.com/dotnet/orleans/discussions/8425
- Orleans serializer (`[GenerateSerializer]`/`[Id]`, version tolerance): https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/serialization
- F# DU serialization issue (broken in 7.x, fixed via PR #9095): https://github.com/dotnet/orleans/issues/8255
- `Microsoft.Orleans.Serialization.FSharp`: https://www.nuget.org/packages/Microsoft.Orleans.Serialization.FSharp
- Orleans ADO.NET grain persistence (`IGrainStorageSerializer`, default Newtonsoft.Json, signed-32-bit Version/ETag): https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence/relational-storage
- Orleans grain persistence overview (opaque ETag, `InconsistentStateException` optimistic concurrency): https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence/
- Orleans PostgreSQL setup scripts (`dotnet/orleans` main): `src/AdoNet/Shared/PostgreSQL-Main.sql` and the per-feature `PostgreSQL-{Clustering,Persistence,Reminders}.sql` (all applied; `-Clustering` backs the non-K8s membership fallback, decision 4)
- `Microsoft.Orleans.Persistence.AdoNet` (10.2.1): https://www.nuget.org/packages/Microsoft.Orleans.Persistence.AdoNet
- `Orleans.Clustering.Kubernetes` (10.0.1, Orleans 10-compatible, net8+net10, depends on `KubernetesClient` 18.0.5): https://www.nuget.org/packages/Orleans.Clustering.Kubernetes
- Npgsql 10.0.3: https://www.nuget.org/packages/Npgsql (targets net8/9/10; 10.x deprecates sync APIs)
- Npgsql release notes: https://github.com/npgsql/npgsql/releases
- Orleans 9.x/10 runtime features (grain directory, `ResourceOptimizedPlacement`, activation shedding/repartitioning, 90s failure detection): https://learn.microsoft.com/en-us/dotnet/orleans/migration-guide
- Orleans PostgreSQL reminders schema (`OrleansRemindersTable`, integer `Version`, `GrainHash` range reads): https://github.com/dotnet/orleans/blob/main/src/AdoNet/Orleans.Reminders.AdoNet/PostgreSQL-Reminders.sql
- Npgsql COPY / `NpgsqlBinaryImporter` (insert-only): https://www.npgsql.org/doc/copy.html
- Composite-array + `unnest` + `ON CONFLICT` bulk upsert (perf): https://www.bytefish.de/blog/bulk_updates_postgres.html
- `Npgsql.OpenTelemetry`: https://www.nuget.org/packages/Npgsql.OpenTelemetry
- PostgreSQL 18 release notes (AIO, temporal constraints, `uuidv7()`, `OLD`/`NEW` `RETURNING`, virtual generated columns, data checksums default-on, parallel GIN, per-backend I/O/WAL stats): https://www.postgresql.org/docs/18/release-18.html
- PostgreSQL full-text search: https://www.postgresql.org/docs/current/textsearch.html
- PostGIS `ST_Distance` / `ST_DWithin`: https://postgis.net/docs/ST_Distance.html , https://postgis.net/docs/ST_DWithin.html
- PostGIS Docker image arm64 (Apple Silicon) support: https://hub.docker.com/r/postgis/postgis (fallback: https://hub.docker.com/r/imresamu/postgis)
- Npgsql concurrency (`xmin`): https://www.npgsql.org/efcore/modeling/concurrency.html
- `Npgsql.PostgresErrorCodes`: https://github.com/npgsql/npgsql/blob/main/src/Npgsql/PostgresErrorCodes.cs
- `timestamptz` mapping pitfalls: https://www.roji.org/postgresql-dotnet-timestamp-mapping
- PG identifier case folding: https://www.postgresql.org/docs/current/sql-syntax-lexical.html
- DbUp PostgreSQL: https://www.nuget.org/packages/dbup-postgresql
- pgloader from SQL Server (toolbox only): https://pgloader.readthedocs.io/en/latest/ref/mssql.html
