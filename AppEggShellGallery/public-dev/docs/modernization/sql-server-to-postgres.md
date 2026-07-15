# SQL Server to PostgreSQL Migration Plan

The EggShell backend persists to SQL Server through Microsoft Orleans. This page is the plan for moving
that storage backend to PostgreSQL, coupled with the Orleans 3.7 to current-major upgrade, as the single
[Goal G](./modernization/goals-and-roadmap.md) workstream ([Phase 6](./modernization/phased-plan.md)).
It maps the current storage layer, the SQL-Server-specific constructs that must be ported, the
Orleans-on-Postgres specifics, and a phased execution order. The concrete seam list also appears in
[Hosting & Persistence](./architecture/backend-hosting-persistence.md); this page is the detailed version.

**Workstream status: not started.**

Version numbers and upstream script paths carry source URLs in [References](#references).

## The decision that shapes everything: couple the Orleans upgrade with the DB swap

Orleans is pinned at **3.7.2** across `LibLifeCycleCore` and `LibLifeCycleHost` (the `.fsproj` comments
warn against bumping it because "biosphere communication and reminders" depend on that exact version).
The current Orleans major line is **9.x/10.x**; the ADO.NET providers are at **10.2.1**.

Going 3.7 to current is a **hard, non-rolling** migration, not a version bump:

- The **wire protocol changed incompatibly** at 7.0. A cluster cannot mix pre-7.0 and 7.0+ silos, so
  there is no rolling upgrade. Cut-over is blue/green: stand up a new cluster, decommission the old.
- **Grain and stream identity changed** to `type/key` strings, replacing the old numeric type-code +
  category + generic-info bytes. Persisted grain state, reminders, and streams do not map straight
  across.
- The **ADO.NET membership, reminder, and persistence table schemas differ** between 3.x and current;
  the current-branch setup scripts are not schema-compatible with a 3.x database.

The PostgreSQL rewrite touches the *same* seams: membership/clustering configuration, the reminder
table, and grain-state storage. Doing the storage rewrite against Orleans 3.7 and then redoing the
membership and reminder port again for the target Orleans is wasted work. **The target Orleans (and .NET
LTS) version is decided first, then the storage rewrite is done once against that target.**

A secondary consequence: because grain identity and the persistence schema change with Orleans 7+, this
is a **fresh-cluster, re-seed-state** exercise, not an in-place migration of the Orleans system tables.
Application subject data (the per-lifecycle tables) can be bulk-moved with pgloader; the Orleans
control-plane tables are recreated from the upstream scripts, not migrated.

## Current storage layer

The state of `LibLifeCycleHost/src/Storage/` and the hosting layer, as they stand.

### What exists

- **One backend: SQL Server.** `Storage/SqlServer/` is the sole DB implementation. There is a
  `Storage/Test/` (in-memory fakes) and a `Storage/Volatile/` (non-persistent), but no `Postgres/`
  directory and no `Npgsql` dependency.
- **The data client is `Microsoft.Data.SqlClient` 5.2.3** (`LibLifeCycleHost.fsproj`).
- **A partial abstraction seam exists, at the handler level:**
  - `IGrainStorageHandler` (non-generic and generic) in `LibLifeCycleHost/src/GrainStorageHandler.fs`.
  - `ITransferBlobHandler` in `LibLifeCycleHost/src/TransferBlobHandler.fs`.
  - `ITimeSeriesStorageHandler<...>` in `LibLifeCycleHost/src/TimeSeriesStorageHandler.fs`.
  - `ISubjectRepo<...>` and `ITimeSeriesRepo<...>` in `LibLifeCycle/src/DefaultServices.fs`.
  - A parallel `Storage/Postgres/` set of implementations can slot in behind these interfaces.
- **The SQL Server implementation files** (all in `LibLifeCycleHost/src/Storage/SqlServer/`):
  `SqlServerGrainStorageHandler.fs` (~176 KB, core state persistence, TVP-heavy), `SqlServerSubjectRepo.fs`
  (~81 KB, index/full-text/geography queries), `SqlServerIndexQueryOptimizer.fs` (~30 KB, spatial query
  building), `SqlServerSetup.fs` (~23 KB, schema bootstrap + Orleans setup), `SqlServerTimeSeriesRepo.fs`,
  `SqlServerTimeSeriesStorageHandler.fs`, `SqlServerTransferBlobHandler.fs`,
  `SqlServerDataProtectionXmlRepository.fs`, `SqlServerConfiguration.fs` (holds both
  `SqlServerConfiguration` and `SqlServerConnectionStrings`), `SqlServerTransientErrorDetection.fs`,
  `SqlServerUtils.fs`.
- **Schema is driven by ~56 embedded `.sql` resources** (declared `EmbeddedResource` in
  `LibLifeCycleHost.fsproj`), applied idempotently by `SqlServerSetup.fs` via a hash-tracked upgrade
  folder (`Storage/SqlServer/Upgrades_Auto/*.sql`). Per lifecycle the schema generates a main table plus
  `_History`, `_Index`, `_Subscription`, `_SideEffect`, `_Prepared` tables and a `_Version` sequence.
- **Orleans 3.7.2** clustering via `.UseAdoNetClustering(...)` in
  `LibLifeCycleHost/src/OrleansEx/SiloBuilder.fs`, with the ADO.NET **invariant hardcoded to
  `"Microsoft.Data.SqlClient"`**. Membership DDL is the embedded `SetupOrleans.sql`, run by
  `SqlServerSetup.runOrleansSetup`.
- **Reminders are custom:** `LibLifeCycleHost/src/SubjectReminderTable.fs` implements Orleans
  `IReminderTable` directly against the subject tables (it `open`s `Microsoft.Data.SqlClient` and issues
  raw commands). This is SQL-Server-coupled and sits outside the handler interfaces.
- **ASP.NET Data Protection keys** are stored via `SqlServerDataProtectionXmlRepository.fs`.

### What does not exist (so is not a porting concern)

- **No stored procedures.** Persistence logic is dynamic SQL generated in F# and executed via
  `sp_executesql` (with `WITH RESULT SETS NONE` for the generated DDL). There is no PL/pgSQL proc
  rewrite; the dynamic-SQL builders in F# must emit PostgreSQL dialect.
- **No temporal / system-versioned tables.** History is a manually managed `*_History` table, not
  `SYSTEM_VERSIONING`.
- **No `SqlDbType.Xml` columns.** The only XML is inside the Data Protection repository payload, not a
  column type.
- **No `IDbConnectionProvider` / `IDbSetup` seam.** The abstraction stops at the handler interfaces;
  connection creation, the ADO.NET invariant, the reminder table, the Data Protection store, and the
  Orleans setup are each bound directly to SQL Server. Closing this gap is the first work item.

### Migration hotspots, ranked by effort

1. **Table-Valued Parameters / UDT table types** (`SqlDbType.Structured`). ~32 call sites; 13 UDTs in
   `CreateTypes.sql` (`SideEffectList_V2`, `SubscriptionNameAndLifeEventList`, `SubscriptionNameList`,
   `IndexingList_V3`, `RebuildIndexList_V4`, `PromotedIndexingList`, `IdList`, `GuidList`,
   `BlobActionList_V2`, `CallDedupList_V2`, `ReEncodeSubjectsList`, `ReEncodeSubscriptionsList`,
   `TimeSeriesPointsList`). This is the single biggest blocker: PostgreSQL has no TVP.
2. **ROWVERSION optimistic concurrency.** `ConcurrencyToken ROWVERSION` and
   `PreparedInitializeConcurrencyToken ROWVERSION` in `CreateTables.sql`, used as the ETag for the
   2-phase-commit grain storage.
3. **Full-text search.** `CreateFullTextCatalog.sql` (`CREATE FULLTEXT CATALOG`), `CreateSearchIndexTable.sql`
   (`CREATE FULLTEXT INDEX`), with the query-side predicate built in `SqlServerSubjectRepo.fs`.
4. **GEOGRAPHY / spatial.** `CreateGeographyIndexTable.sql` (`GEOGRAPHY` column),
   `UpsertStateShared_UpsertGeographyIndex.sql`, `RebuildIndices_GeographyIndex.sql`,
   `ClearState_DeleteGeographyIndex.sql`, and `SqlServerIndexQueryOptimizer.fs`.
5. **`VARBINARY(MAX)` payloads** (serialized state/history/side-effects/timeseries, gzip-compressed JSON).
6. **Orleans invariant + membership scripts + custom reminder table.**
7. **Connection-string dialect + MARS.** Connection strings carry `MultipleActiveResultSets=True`, which
   is SQL-Server-only and must be removed for Npgsql.
8. **Transient-error classification** (`SqlServerTransientErrorDetection.fs`) uses SQL Server error
   numbers; PostgreSQL uses SQLSTATE strings.

## Target versions and toolchain

Confirm exact point releases on nuget.org before pinning (see the Npgsql caveat in
[References](#references)); the target shape is:

| Component | Target | Notes |
|---|---|---|
| Orleans | 9.x/10.x (ADO.NET providers 10.2.1) | Decide the exact major with the .NET LTS decision. Non-rolling upgrade from 3.7.2. |
| .NET | The chosen LTS (interacts with the repo-wide TFM bump) | Backend projects are still `net7.0`; the [TFM bump](./modernization/goals-and-roadmap.md) is a prerequisite. |
| Npgsql | 10.x (9.0.x is the maintained prior line) | Targets net10.0. **Npgsql 10 deprecates synchronous APIs** — write async DB access only. |
| PostgreSQL | 17 | Use the `postgis/postgis:17-3.4` image where spatial is needed. |
| PostGIS | 3.4+ | Extension, installed per-database (`CREATE EXTENSION postgis;`). |
| Orleans ADO.NET invariant | `Npgsql` | Replaces `Microsoft.Data.SqlClient` in `SiloBuilder.fs`. |
| Orleans packages | `Microsoft.Orleans.{Clustering,Persistence,Reminders}.AdoNet` | Apply `PostgreSQL-Main.sql` first, then the per-feature scripts. |
| Schema migration | DbUp (`dbup-postgresql`) for versioned scripts; pgloader for the one-time data lift | See [Tooling](#tooling). |

## The abstraction gap to close first

The repo lacks a connection/setup seam below the handler interfaces. Introducing one lets the SQL Server
path keep working while the Postgres path is built in parallel behind the same interfaces.

1. **`IDbConnectionProvider`** — a single seam that yields an open `DbConnection` (base type, not
   `SqlConnection`) per ecosystem and purpose. The SQL Server implementation wraps the existing
   `SqlServerConnectionStrings`; the Postgres implementation builds `NpgsqlConnection`. Every
   `Storage/SqlServer/*.fs` that news up `SqlConnection` today routes through this instead.
2. **`IDbSetup`** — abstracts `SqlServerSetup`'s "apply embedded schema + upgrades" and "run Orleans
   membership setup" so a `PostgresDbSetup` can apply the PostgreSQL DDL and the upstream Orleans
   PostgreSQL scripts.
3. **A dialect switch on the storage-setup union.** `HostExtensions.fs` today directly instantiates the
   SQL Server handlers. A `DatabaseProvider` config key (`SqlServer` | `Postgres`) selects the handler
   set, connection provider, setup, reminder table, Data Protection store, transient-error detector, and
   the Orleans ADO.NET invariant.
4. **A provider-specific transient-error classifier** so retry logic is correct on each backend
   (`SqlServerTransientErrorDetection` vs a Postgres SQLSTATE-based one).
5. **Connection-string handling.** Strip `MultipleActiveResultSets=True` for Postgres; MARS is
   SQL-Server-only. Npgsql uses `@name` placeholders in command text (rewritten to positional
   internally) but raw `unnest`/function calls need `$1` positional — this matters when porting the
   dynamic-SQL builders.

The Postgres implementations are written only after this seam is in place and the SQL Server path is
verified unchanged.

## Feature-by-feature port

The reference for porting each SQL construct. Sources for every mapping are in [References](#references).

| SQL Server construct (where) | PostgreSQL approach | Notes |
|---|---|---|
| **TVP / UDT table types** (`SqlDbType.Structured`; 13 types in `CreateTypes.sql`) | Pass a .NET array as **one parameter + `unnest()`** to expand to rows; for bulk load use **binary COPY** (`NpgsqlBinaryImporter`); for complex rows use **JSONB + `jsonb_to_recordset`** or a registered **composite-type array**. | Biggest single item. Cast array params to resolve type ambiguity. No custom table types needed. Prefer `unnest` for the index/subscription/side-effect lists; consider COPY only for the highest-volume paths. |
| **`ROWVERSION` concurrency token** (`CreateTables.sql`) | Either map the hidden **`xmin`** system column as the token, or add an **explicit version column** bumped on update. | `xmin` = zero extra columns, auto-updates, but it is a transaction id (wraps around long-term) and fights code-first tooling. An explicit `bigint` version column is clearer and migration-friendly at the cost of app code. Given the 2-phase-commit ETag logic, an **explicit version column** is the safer choice. |
| **Full-text: `CREATE FULLTEXT CATALOG/INDEX`, `CONTAINS`/`FREETEXT`** (`CreateFullTextCatalog.sql`, `CreateSearchIndexTable.sql`, `SqlServerSubjectRepo.fs`) | **`tsvector`/`tsquery`** + **GIN index**; rank with **`ts_rank()`**. `CONTAINS` (precise/prefix/boolean) to `to_tsquery`; `FREETEXT` (natural language) to `plainto_tsquery`/`websearch_to_tsquery`. | Full rewrite of the FTS predicate builder. Prefer a stored/generated `tsvector` column + GIN for speed. PG FTS is less feature-rich than SQL Server's but adequate for the index-search use here. |
| **`GEOGRAPHY` + `STDistance`** (`CreateGeographyIndexTable.sql`, `SqlServerIndexQueryOptimizer.fs`) | **PostGIS** `geography` type, **`ST_Distance`** (geodesic meters), **`ST_DWithin`** for radius filters. `CREATE EXTENSION postgis;` per DB. | `ST_Distance` is ~1:1 with `STDistance()`. **Use `ST_DWithin` for range queries** — it uses the GiST spatial index; `ST_Distance` in a `WHERE` does not. |
| **`VARBINARY(MAX)`** (serialized payloads) | **`bytea`** (`NpgsqlDbType.Bytea`, .NET `byte[]`). | Validate byte-lengths after any bulk migration; some automated migrators truncate large `bytea`. |
| **`DATETIMEOFFSET`** | **`timestamptz`** (`TIMESTAMP WITH TIME ZONE`). | `timestamptz` normalizes to UTC on write and does **not** store the original offset. Npgsql maps `DateTime`/`DateTimeOffset` with UTC rules; `DateTime.Kind` matters. If an original offset must survive, store it separately. |
| **`MERGE` / upsert** (`UpsertStateShared*.sql`) | **`INSERT ... ON CONFLICT (cols) DO UPDATE SET x = EXCLUDED.x`**. | `EXCLUDED` is the would-be-inserted row. PG 15+ also has real `MERGE`, but `ON CONFLICT` is idiomatic. |
| **`SCOPE_IDENTITY()` / `OUTPUT INSERTED`** | **`RETURNING`** on the DML statement. | Cleaner and multi-row capable. |
| **Sequences** (`SequenceCurrentValue.sql`, `SequenceNextValue.sql`, `_Version` sequence) | PG sequences (`nextval`/`currval`), or `GENERATED ... AS IDENTITY`. | Syntax differs; behavior maps closely. |
| **Error numbers** (`SqlServerTransientErrorDetection.fs`, catch/retry logic) | **`PostgresException.SqlState`** (SQLSTATE), constants in `Npgsql.PostgresErrorCodes`: unique **`23505`**, deadlock **`40P01`**, serialization failure **`40001`**. Also check `IsTransient`. | Rewrite from int codes to SQLSTATE strings. Retry `40001`/`40P01`. |
| **Identifier case** (all DDL, all quoted identifiers) | PG folds unquoted identifiers to **lowercase**; quoted `"Name"` is case-sensitive. | Biggest silent-bug source. Standardize on a single convention and never mix quoted/unquoted. Decide early whether to keep the current PascalCase (quote everywhere) or move to `lower_snake_case`. |
| **`sp_executesql` dynamic DDL** (`CreateTables.sql`, etc.) | `EXECUTE format(...)` in a `DO $$ ... $$` block, or build and execute the SQL from F# directly. | Since the SQL is generated in F#, prefer emitting the final PostgreSQL text from F# rather than porting the `sp_executesql` indirection. |
| **MARS** (`MultipleActiveResultSets=True` in connection strings) | Remove; not supported by Npgsql. Use separate connections or read fully before the next command. | Audit `SqlServerSubjectRepo` batch/streaming reads (`CommandBehavior.CloseConnection`, `AsyncSeq`) for MARS assumptions. |

## Orleans on PostgreSQL

- **Packages:** `Microsoft.Orleans.Clustering.AdoNet`, `Microsoft.Orleans.Persistence.AdoNet`,
  `Microsoft.Orleans.Reminders.AdoNet` (10.2.1). Provider package: `Npgsql`.
- **Invariant:** set `Invariant = "Npgsql"` and the `ConnectionString`; for grain persistence set
  `UseJsonFormat = true`.
- **Setup scripts** live in the `dotnet/orleans` repo and are applied **Main first**, then per feature:
  `PostgreSQL-Main.sql`, then `PostgreSQL-Clustering.sql`, `PostgreSQL-Persistence.sql`,
  `PostgreSQL-Reminders.sql`. Every silo must reach the database before start. They are applied via the
  `IDbSetup` Postgres implementation (bundle the upstream scripts as embedded resources, the same
  pattern as today's `SetupOrleans.sql`).
- **The custom `SubjectReminderTable`** is the awkward part. It bypasses the Orleans reminder table and
  piggybacks on subject tables with raw SQL Server commands. Two options: (a) port it to Npgsql (keep the
  piggyback design), or (b) switch to the standard `Microsoft.Orleans.Reminders.AdoNet` PostgreSQL
  reminder table and drop the custom implementation. Option (b) is cleaner and aligns with the upstream
  schema, but changes reminder semantics — evaluate against the "biosphere communication and reminders"
  dependency the `.fsproj` comments flag.
- **Cut-over:** because grain identity and the persistence schema change at Orleans 7+, this is a
  fresh-cluster, re-seed exercise, not an in-place system-table migration. Application (subject) data
  moves with pgloader; the Orleans control-plane tables are created fresh from the upstream scripts.

## Phased execution plan

Each phase ends with a building, verified backend (SQL Server unaffected) before the next begins. Every
refactor commit stays green: no half-stabilized state is committed.

**Phase 0 — Decisions and spike.**
- Pick the target Orleans major and .NET LTS together. Confirm the
  [repo-wide TFM bump](./modernization/goals-and-roadmap.md) is sequenced ahead of or with this.
- Spike Orleans-on-Postgres in a throwaway project: apply the upstream scripts, run a trivial silo with
  ADO.NET clustering + persistence + reminders on `postgres:17`, confirm the invariant and JSON format
  work on the target Orleans version.

**Phase 1 — Close the abstraction gap (SQL Server still the only backend).**
- Introduce `IDbConnectionProvider`, `IDbSetup`, the `DatabaseProvider` switch, and the provider-specific
  transient-error classifier (see [The abstraction gap](#the-abstraction-gap-to-close-first)).
- Refactor `Storage/SqlServer/*` to obtain connections and run setup through the new seams. No behavior
  change. The existing simulation/test suites stay green.

**Phase 2 — Postgres schema and DDL.**
- Port the ~56 embedded `.sql` resources to a Postgres variant (or make the F# builders dialect-aware).
  Work bottom-up: types/tables first (`CreateTypes.sql`, `CreateTables.sql`, `CreateSchema.sql`), then
  the index/search/geography tables, then the upsert/state/tick/timeseries/subscription scripts.
- Apply the [feature port table](#feature-by-feature-port) construct by construct. Decide the identifier
  case convention up front.
- Add PostGIS and the full-text (`tsvector`/GIN) objects.

**Phase 3 — Postgres handlers.**
- Implement `PostgresGrainStorageHandler`, `PostgresSubjectRepo`, `PostgresTimeSeriesRepo`,
  `PostgresTimeSeriesStorageHandler`, `PostgresTransferBlobHandler`, `PostgresDataProtectionXmlRepository`
  behind the existing handler interfaces.
- Replace TVP call sites with `unnest`/COPY/JSONB per the table. Rewrite the concurrency token to the
  chosen version-column strategy. Rewrite the FTS and geography query builders.

**Phase 4 — Orleans on Postgres.**
- Bring in the AdoNet packages, set the `Npgsql` invariant in `SiloBuilder.fs` behind the dialect switch,
  apply the upstream PostgreSQL scripts via `IDbSetup`.
- Resolve the reminder-table decision (port vs adopt the standard AdoNet reminder table).

**Phase 5 — Verification and dual-DB CI.**
- Run the full `TestDataSeeding` simulation suite against Postgres.
- Stand up dual-DB CI (SQL Server + Postgres containers) so both backends stay green.
- Performance pass (EXPLAIN ANALYZE the index/search/geography queries; tune GIN/GiST indexes and
  `work_mem`).

**Phase 6 — Data lift and cut-over (per deployment).**
- pgloader the application/subject data from SQL Server to Postgres; verify counts and byte-lengths.
- Create the Orleans control-plane tables fresh from the upstream scripts. Blue/green the cluster.

## Tooling

- **Versioned schema (owned by the app):** **DbUp** with `dbup-postgresql` (7.0.1, depends on Npgsql).
  Runs ordered idempotent SQL scripts and tracks a journal table — the natural replacement for the
  current hash-tracked `Upgrades_Auto` mechanism. It deploys schema, not data.
- **One-time data lift:** **pgloader**. It reads **directly from SQL Server** (via FreeTDS:
  `pgloader mssql://user@host/db postgresql://...`), auto-maps types including `varbinary` to `bytea`
  (validate the truncation caveat). For ongoing/zero-downtime, use a CDC platform instead.
- **Dev stack:** a single `docker compose` file running both engines, folded into the planned
  `./dev-stack up` ([Phase 6 of the phased plan](./modernization/phased-plan.md)):

  ```yaml
  services:
    mssql:
      image: mcr.microsoft.com/mssql/server:2022-latest
      environment: { ACCEPT_EULA: "Y", MSSQL_SA_PASSWORD: "Your_strong_P@ss1" }
      ports: ["1433:1433"]
    postgres:
      image: postgis/postgis:17-3.4    # postgres:17 if spatial is not needed
      environment: { POSTGRES_PASSWORD: "postgres", POSTGRES_DB: "app" }
      ports: ["5432:5432"]
      volumes: ["pgdata:/var/lib/postgresql/data"]
  volumes: { pgdata: {} }
  ```

  Mount the Postgres volume at `/var/lib/postgresql/data` exactly (the parent will not persist).

## Testing strategy

- **The simulation suite is the primary safety net.** The deterministic simulation tests (see
  [Testing framework](./architecture/testing-framework.md)) exercise lifecycles through the storage
  layer. Running them in `TestDataSeeding` mode against Postgres is the acceptance bar for each handler.
- **Dual-DB CI matrix.** Run the suite against SQL Server and Postgres containers so neither backend
  regresses while both exist. The `DatabaseProvider` switch is the only difference between the two CI
  legs.
- **Result-diffing during the port.** For the query-heavy paths (subject repo, FTS, geography), diff
  Postgres results against SQL Server for the same inputs before removing the SQL Server path.
- **Concurrency tests.** The 2-phase-commit + concurrency-token logic is the highest-risk area; add
  targeted tests for optimistic-concurrency conflicts under the chosen version-column strategy.

## Risks and open questions

- **Reminder table.** Port the custom `SubjectReminderTable` to Npgsql vs adopt the standard AdoNet
  reminder table. The `.fsproj` comments tie "biosphere communication and reminders" to Orleans 3.7.2, so
  this needs a careful semantics review on the target Orleans version.
- **Concurrency token strategy.** `xmin` vs explicit version column. The explicit column is preferred for
  clarity with the 2-phase commit, but it needs validation against the actual ETag round-trip in
  `SqlServerGrainStorageHandler.fs`.
- **Identifier case convention.** Keep PascalCase (quote everywhere) or move to `lower_snake_case`. Cheap
  to decide, expensive to change later. Decide in Phase 2.
- **TVP-to-`unnest` performance.** For the highest-volume indexing/side-effect writes, `unnest` of a
  large array may underperform SQL Server TVPs; binary COPY may be needed for those specific paths.
  Measure in Phase 5.
- **Full-text parity.** PostgreSQL FTS is less feature-rich than SQL Server's; confirm the actual search
  features used (`SqlServerSubjectRepo.fs`) are all expressible in `tsquery`.
- **Orleans state re-seed.** Because grain identity changes at Orleans 7+, confirm the re-seed / state
  reprojection approach for existing production data is acceptable to operations.
- **Npgsql sync-API deprecation.** Any synchronous DB access must be async by Npgsql 10. Audit the
  ported handlers.
- **Exact upstream point releases.** Confirm Orleans and Npgsql point versions on nuget.org before
  pinning (see the Npgsql date caveat below).

## References

Internal:
- [Goal G — PostgreSQL + Orleans upgrade](./modernization/goals-and-roadmap.md)
- [Phased Plan — Phase 6](./modernization/phased-plan.md)
- [Hosting & Persistence](./architecture/backend-hosting-persistence.md)
- [Testing framework](./architecture/testing-framework.md)

Upstream (confirmed 2026-07):
- Orleans ADO.NET configuration and invariants: https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/adonet-configuration
- Orleans migration guidance: https://learn.microsoft.com/en-us/dotnet/orleans/migration-guide
- Orleans 7.0 breaking changes (identity/wire): https://www.infoq.com/news/2022/12/orleans-dotnet-7/ , https://github.com/dotnet/orleans/discussions/8425
- Orleans PostgreSQL setup scripts (`dotnet/orleans` main): `src/AdoNet/Shared/PostgreSQL-Main.sql` and the per-feature `PostgreSQL-{Clustering,Persistence,Reminders}.sql`
- `Microsoft.Orleans.Persistence.AdoNet` (10.2.1): https://www.nuget.org/packages/Microsoft.Orleans.Persistence.AdoNet
- Npgsql COPY / `NpgsqlBinaryImporter`: https://www.npgsql.org/doc/copy.html
- Npgsql release notes / .NET 10: https://github.com/npgsql/npgsql/releases , https://www.npgsql.org/doc/release-notes/9.0.html
- PostgreSQL full-text search: https://www.postgresql.org/docs/current/textsearch.html
- PostGIS `ST_Distance` / `ST_DWithin`: https://postgis.net/docs/ST_Distance.html , https://postgis.net/docs/ST_DWithin.html
- Npgsql concurrency (`xmin`): https://www.npgsql.org/efcore/modeling/concurrency.html
- `Npgsql.PostgresErrorCodes`: https://github.com/npgsql/npgsql/blob/main/src/Npgsql/PostgresErrorCodes.cs
- `timestamptz` mapping pitfalls: https://www.roji.org/postgresql-dotnet-timestamp-mapping
- PG identifier case folding: https://www.postgresql.org/docs/current/sql-syntax-lexical.html
- DbUp PostgreSQL: https://www.nuget.org/packages/dbup-postgresql
- pgloader from SQL Server: https://pgloader.readthedocs.io/en/latest/ref/mssql.html

**Sourcing caveat:** version *ordering* (Npgsql 10.x current, 9.0.x maintained) is corroborated by the
release-notes pages, but confirm the exact Npgsql 10.x point release on nuget.org before pinning.
