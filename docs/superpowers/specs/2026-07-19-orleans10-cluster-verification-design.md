# Orleans 10 / net10 Cluster Verification — Design

**Date:** 2026-07-19
**Branch context:** `shafayat/pgsql-initial-spikes`
**Status:** Approved design, ready for implementation plan.

## Problem

The repo has migrated from net7 / Orleans 3.7 to net10 / Orleans 10. Only the single-dev
stack has been validated: one laptop running SQL Server plus the backend and frontend in a
single in-process silo (`DevelopmentHost`). Two claims remain unverified:

1. **Cross-silo / multi-server reliability** is as good or better than the old stack
   (cluster membership, grain placement and single-activation, reminders across silos,
   failover, membership hygiene).
2. **Throughput and latency** are as good or better, including the Orleans 7+ new wire
   protocol gains.

Neither can be observed on a single in-process silo. There is currently **no multi-process
cluster path exercised outside production** and **no performance harness of any kind** in the
repo.

### Why the throughput claim needs measurement, not published numbers

The payload path uses a **custom codec** (`EggShellSubjectGrainsCodec`, registered as
`IGeneralizedCodec`/`IGeneralizedCopier`/`IDeepCopier`/`IFieldCodec` in
`LibLifeCycleCore/src/OrleansEx/Serialization.fs`), not `[<GenerateSerializer>]`. Production
F# types are deliberately not annotated for source-generated serialization. Orleans' published
wire-protocol improvements are measured on the default serializer, so they do not transfer
directly. The actual codec must be measured on the wire.

## Goal & acceptance criteria

Prove Orleans 10 / net10 is **as good or better** than Orleans 3.7 / net7 on two dimensions via
a head-to-head Docker Compose A/B, with the old stack revived and run on the same hardware.

Accept when, on identical hardware limits and op mix:

- **Throughput:** new stack req/s ≥ old at every concurrency step, within a stated noise band
  (±5% treated as parity).
- **Latency:** new stack p99 and p999 ≤ old at every step.
- **Reliability:** all catalog scenarios (below) pass, and failover/recovery time ≤ old.

The deliverable is a single markdown verdict report plus the reusable harness.

## Existing facts (scouted)

- `LibLifeCycleTest/TestCluster.fs:485` already boots a **2-silo in-process TestCluster**
  (`Orleans.TestingHost`, in-memory membership, loopback) for normal test runs; 1 silo for
  data-seeding. This validates in-process multi-silo correctness but **not** SQL-Server
  clustering, real inter-process networking, or failover.
- Dev host (`LibLifeCycleHost/src/Host/Development/DevelopmentHost.fs`) = single silo, SQL
  ADO.NET clustering (`Microsoft.Data.SqlClient`), membership table reset on start. Prod host
  (`LibLifeCycleHost/src/Host/K8S/SubjectHost/SubjectHost.fs`) = multi-node with advertised IP.
- Clustering + reminders storage is 100% SQL Server. `Npgsql` 10.0.3 is referenced in
  `LibLifeCycleHost/src/LibLifeCycleHost.fsproj` but not yet wired to any code.
- Reminders in the Proper branch use `.AddReminders()` + a custom read-only `IReminderTable`
  (`SubjectReminderTable`). The `IReminderRegistry` regression fixed in commit `3eaac2f`
  (codemem 1912) lives on exactly this path and must be regression-guarded.
- No BenchmarkDotNet / NBomber / load-gen harness exists anywhere.
- `SetupOrleans.sql` / `SqlServerSetup.fs` (`LibLifeCycleHost/src/Storage/SqlServer/`)
  provision the Orleans SQL schema and are reusable by the harness.

## Approach (chosen: custom two-stack compose harness)

Rejected alternatives: NBomber + xUnit TestCluster (HTTP-oriented, in-process TestCluster
can't test SQL clustering/real-network failover — half-measure); adapting Orleans'
`test/Benchmarks` (built for the default serializer + generic grains, heavy surgery to fit the
custom codec and the old stack).

### Components — `Meta/ClusterVerify/`

- **`old/` and `new/`** — parallel Docker Compose stacks: 3 silo containers + 1 SQL Server +
  1 load client each. Identical silo count, ports, and resource limits (CPU/mem caps) so the
  comparison is fair. `old/` targets net7 + Orleans 3.7; `new/` targets net10 + Orleans 10.
  Run sequentially on the same host, never concurrently.
- **`grains/`** — synthetic `EchoGrain` (identity call, raw framework+wire ceiling) and
  `ComputeGrain` (fixed CPU work, placement/scheduling), plus a thin caller into the **real
  subject grains** for the codec + SQL persistence + reminders path. Two builds because the
  O3.7 and O10 grain/client APIs differ; both share a single op-mix definition file.
- **`loadclient/`** — async load generator: concurrency ladder, warmup then fixed-duration
  steady-state per step, HdrHistogram percentiles written to `results.json` with an identical
  schema across both stacks. One build per stack (client APIs differ).
- **`reliability/`** — scenario runner driving `docker kill` / `docker network disconnect` /
  restart, asserting via `IManagementGrain` (`GetDetailedHosts`, `GetSimpleGrainStatistics`),
  the SQL membership table, and log scrape.
- **`compare.fsx`** — diffs old vs new `results.json` and emits the verdict table (markdown).

### Metrics & observability

Enable Orleans statistics and OpenTelemetry/Prometheus counters (throughput, request-latency
histograms, silo membership gauge, activation counts) on both stacks identically. The load
client owns the authoritative client-side latency via HdrHistogram. Cluster truth (silo
statuses, activation distribution) comes from `IManagementGrain`.

### Reliability scenario catalog (track A)

1. **Cluster formation** — 3 silos join the SQL clustering table and all reach ACTIVE.
2. **Placement / distribution** — activations spread across silos; single-activation guarantee
   holds under concurrent calls to the same key.
3. **Reminders cross-silo** — a reminder fires; killing the owning silo migrates the reminder
   and it keeps firing. Directly guards the `IReminderRegistry` regression (codemem 1912).
4. **Failover under load** — kill one silo mid-load: in-flight calls retry, activations recover
   on survivors, zero data loss, and cluster re-stabilization time is measured.
5. **Membership hygiene** — a dead silo is evicted from the table and defunct entries cleaned.
6. **Partition (optional)** — `network disconnect` one silo; verify detection and recovery.

### Throughput benchmark (track B)

Concurrency ladder (e.g. 1, 8, 32, 128, 512 outstanding requests). Per step: warmup, then a
fixed steady-state measurement window. Op mix:

- **echo-only** — framework + wire ceiling. The echo payload must carry a *representative
  subject-shaped payload* so the custom codec is actually exercised; an empty/primitive echo
  would make the wire numbers meaningless.
- **compute-only** — placement and scheduling under CPU load.
- **real subject ops** — the true end-to-end cost through `EggShellSubjectGrainsCodec` + SQL +
  reminders.

Report req/s and p50/p99/p999 per op per step. Synthetic isolates Orleans overhead; real
isolates the codec+storage delta (the synthetic+real split chosen during design).

### Verdict artifact

A single markdown report: side-by-side table (metric × concurrency × stack), reliability
pass/fail with recovery times, and a plain verdict line (better / parity / regression) per
dimension.

## Risks & nuances

- **Custom codec representativeness** — echo payload must mirror real subject shape or wire
  numbers are invalid.
- **Old-stack revival is the long pole** — net7 / Orleans 3.7 build may need dependency
  pinning and toolchain juggling; budget time for it up front.
- **Container network ≠ real NIC** — compose reports the relative A/B honestly, but absolute
  numbers understate real-network cost. State this caveat in the report.
- **Fair comparison** — identical CPU/mem caps on both stacks, same host, sequential runs.
- **SQL clustering setup** — reuse `SetupOrleans.sql` / `SqlServerSetup.fs`; give each stack
  its own database.

## Pre-build reading (project rule 14)

Before writing any spike code:

- Orleans `test/Benchmarks` load-client pattern and `TestCluster` / chaos patterns.
- ADO.NET clustering sample README and clustering/failover docs.
- `gh issue` search on `dotnet/orleans` for Orleans 10 clustering, failover, and serialization
  gotchas; cite issue numbers in the spike catalog doc.

## Out of scope

- Postgres/Npgsql clustering (later workstream; clustering is still SQL Server).
- Multi-machine / real-network topology (compose one-host is the chosen topology; note as a
  possible follow-up for absolute numbers).
- .NET 10 TFM migration and net10-specific feature adoption.
