# Backend: Hosting & Persistence

## Silo configuration

Configured in `LibLifeCycleHost/src/OrleansEx/SiloBuilder.fs`. Orleans **3.7.2**, clustering via
**ADO.NET on SQL Server** (`.UseAdoNetClustering(...)`, invariant hardcoded to `"Microsoft.Data.SqlClient"`,
v5.2.3). There are several silo "shapes": `Proper` (prod), `ProperSiloDev`, `Test` (in-memory reminders),
`TestDataSeeding`.

Startup tasks run in order (`HostExtensions.fs`): `SqlServerSetupStartupTask` (schema/migrations) →
`CustomStorageInitStartupTask` → `LifeCycleHostStartupTask` (runs the ecosystem `_Startup` grain).

Host entry points exist for **Kubernetes** (`Host/K8S/...`), **Service Fabric** (`Host/Fabric/...`), and
**Development** (single in-memory node).

## Storage abstraction

The clean seam is `IGrainStorageHandler<...>` in `LibLifeCycleHost/src/GrainStorageHandler.fs`:

```fsharp
abstract member TryReadState      : pKey -> Task<Option<ReadStateResult<...>>>
abstract member InitializeSubject : pKey -> dedup -> data -> Task<Result<...>>
abstract member UpdateSubject     : pKey -> dedup -> updateData -> Task<Result<...>>
abstract member PrepareInitializeSubject / CommitInitializeSubject  // 2-phase commit
```

Storage type per lifecycle (`LifeCycle.fs`): `Persistent` (with history retention policy) | `Volatile` |
`Custom`.

The SQL Server implementation lives in `LibLifeCycleHost/src/Storage/SqlServer/`:

- `SqlServerGrainStorageHandler.fs` — state as **gzip-compressed JSON**, optimistic concurrency via
  ETags/rowversion, dedup cache, 2-phase commit.
- `SqlServerSubjectRepo.fs`, `SqlServerTimeSeriesRepo.fs` — query/index reads.
- `SqlServerSetup.fs` — idempotent schema management: embedded `.sql` resources + a hash-tracked upgrade
  folder (`Upgrades_Auto/*.sql`) applied via `__ApplyScriptIfNewOrChanged`.
- `SqlServerDataProtectionXmlRepository.fs` — ASP.NET Data Protection key store.
- `SqlServerTransientErrorDetection.fs` — SQL-Server-specific retry classification.

Per lifecycle the schema generates: main table, `_History`, `_Index`, `_Subscription`, `_SideEffect`,
`_Prepared` tables, plus a `_Version` sequence.

## Adding PostgreSQL — the concrete seams

This is feasible but non-trivial. The abstraction (`IGrainStorageHandler`) is already clean; the work is
dialect + provider + Orleans clustering. Exact touch points:

1. **New storage handlers** implementing the existing interfaces: `PostgresGrainStorageHandler.fs`,
   `PostgresSubjectRepo.fs`, `PostgresTimeSeriesRepo.fs`, `PostgresTransferBlobHandler.fs`,
   `PostgresDataProtectionXmlRepository.fs`.
2. **Provider selection** in `HostExtensions.fs` — today it directly instantiates the SQL Server handlers.
   Introduce a DB-dialect switch on the existing `EcosystemStorageSetup` union.
3. **Connection factory** — `SqlConnection`/`Microsoft.Data.SqlClient` is hardcoded in
   `SqlServerConfiguration.fs` and the handlers. Abstract behind a factory; Postgres uses `Npgsql`.
4. **SQL dialect** — the DDL/stored-proc `.sql` files are T-SQL. Postgres equivalents needed for:
   `ROWVERSION` → `xmin`/`bigserial`; `BIT` → `boolean`; `NVARCHAR ... COLLATE` → `text`/collation;
   full-text catalog → `tsvector`/`tsquery`; geography indices → PostGIS; stored procs → PL/pgSQL.
5. **Orleans clustering** — Orleans 3.7 ADO.NET clustering can target Postgres, but membership SQL
   (`SetupOrleans.sql`) must be ported and the invariant changed to the Npgsql provider.
6. **Transient-error detection** — Postgres error codes differ from SQL Server's.

Rough estimate: the F# handler layer is a moderate rewrite (~40%); the bulk of the effort is porting and
testing the SQL/stored-proc surface and the membership tables. The core lifecycle/Orleans layers are
untouched.

> **Sequencing note.** A `.NET 7` → current-LTS upgrade and the Postgres effort interact. If an Orleans
> 7.x upgrade is also on the table, do the storage rewrite **once**, against the target Orleans version,
> to avoid doing the membership/clustering port twice. See
> [Goals & Roadmap → G](./modernization/goals-and-roadmap.md).
