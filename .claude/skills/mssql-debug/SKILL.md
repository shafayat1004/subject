---
name: mssql-debug
description: Debug the EggShell framework's MSSQL storage layer (subjects, sideffects, prepared tx, cluster) using read-only queries against the dev DB. Use for "subject stuck", "sideffect not progressing", "silo down", "what did this subject do over time", "is the cluster healthy", or converting UTC columns to local time. Read runbooks/troubleshooting.md before improvising DB queries.
user-invocable: true
argument-hint: "<subcommand> [args]"
---

# mssql-debug

Debug EggShell MSSQL storage. Read-only. No production DBs.

## When to use

- Subject stuck / not progressing.
- Sideffect not progressing.
- Silo down or cluster split-brain.
- "What did this subject do over time?"
- "Is the ecosystem healthy?"
- Converting UTC `DATETIMEOFFSET` columns to local time.

## Pre-reqs

- `dotnet` present (verified baseline).
- Network reach to `192.168.2.231:1433`.
- `Microsoft.Data.SqlClient` pulled by `dotnet fsi` via NuGet in each script.

If DB unreachable, scripts fail fast with connection timeout (`Connect Timeout=5`).

## Conn resolution order

1. `MSSQL_CONN` env var.
2. `appsettings.Development.json` key `Storage.SqlServer:ConnectionString`.
3. `appsettings.json` same key.
4. Fail with hint.

Default path for (2): `SuiteTodo/Launchers/Dev/DevelopmentHost/src/appsettings.Development.json`
relative to repo root. Override with `scripts/lib/conn.sh <path>`.

## Ecosystem / lifecycle discovery

- Ecosystems: scan `[eco].[__SchemaUpgrade]` rows in `Todo_Dev`. Each distinct `eco` = one ecosystem schema.
- Lifecycles: tables matching `[eco].[%_History]`; strip `_History` suffix.
- Defaults: ecosystem `Todo_Dev`, lifecycle `Todo`.

## Subcommands

| Subcommand | Args | Does |
|---|---|---|
| `decode-subject` | `<eco> <lc> <subjectId> [--version N]` | Current state + construction row, decoded JSON. |
| `subject-history` | `<eco> <lc> <subjectId> [--from <iso>] [--to <iso>]` | Version timeline, local times, truncated JSON. |
| `stalled-summary` | `<eco>` | 5-resultset healthcheck (prepared/failed/sideffect/timer). |
| `stalled-list` | `<eco> [lc] --kind <sideffect\|prepared\|timer\|failed>` | Full rows of one diag view, decoded blobs. |
| `cluster-health` | `[deploymentId]` | Silo status; no arg lists deployment IDs. |
| `failed-sideffects` | `<eco> [lc] [--subject <id>] [--severity 1\|2]` | Failed sideffects, decoded, ack state. |
| `to-local` | `<utc-iso>...` | Convert UTC timestamps to local TZ. |

All scripts live under `scripts/`. Call via `dotnet fsi <script.fsx>` or directly if executable.

## Safety -- read-only hard rule

- All scripts use `NOLOCK` + `SELECT` / views only.
- No `UPDATE`, `DELETE`, `MERGE`, or mutation proc `EXEC`.
- Skill refuses ad-hoc queries -- use only bundled scripts. If insight needs new query, add script,
  review, then run.

## Gotchas

- `Subject` blob is GZip+UTF8 JSON. Rely on server-side `[eco].[decode](@binary)`. Collation matters:
  `Latin1_General_100_CS_AS_KS_WS_SC_UTF8`.
- DB times are `DATETIMEOFFSET` UTC. Printing without local conversion hides "5min overdue" signal in
  user's TZ. Scripts print both.
- `AllStalledTimers` view needs `_Meta` subject (`RebuildingTimersAndSubs` flag in
  `[eco].[_Meta_Index]`). TestDataSeeding co-hosted ecosystems without `_Meta` show EMPTY view -- not
  "no stalled timers". Script warns when key missing.
- History `Tombstone=1` rows are soft-deleted audit. `ClearExpiredHistoryBatch` prunes them later.
  Recent deleted history may still be present.
- `FailureAckedUntil` is UTC. "Acked" means `FailureAckedUntil > GETUTCDATE()`.
- `OrleansMembershipTable.Status` is `int`. Scripts map to name:
  0=Created,1=Joining,2=Active,3=ShuttingDown,4=Stopping,5=Stopped,6=Dead.
- Cluster tables are `dbo` schema -- NOT `[eco]`. Common confusion.

## Doc refs

- runbooks/troubleshooting.md
- knowledge-base/engineering-log.md
- modernization/sql-server-to-postgres.md (skill doubles as diff-oracle inspector during pgsql port)

(All under `AppEggShellGallery/public-dev/docs/`.)
