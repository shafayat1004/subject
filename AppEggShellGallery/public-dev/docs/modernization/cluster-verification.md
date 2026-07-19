# Orleans 10 Cluster Verification

The Orleans 3.7 to 10.x upgrade ([Goal G](./goals-and-roadmap.md)) must be proven as good or
better than the prior stack on two axes that a single in-process silo cannot exercise:

1. **Cross-silo reliability** across a real multi-silo cluster: membership, grain placement and
   single-activation, reminders across silos, failover, and membership hygiene.
2. **Throughput and latency**, including the Orleans 7+ wire protocol, measured on the actual
   payload path rather than trusted from published numbers.

This page is the reference for that verification. It complements
[SQL Server to Postgres](./sql-server-to-postgres.md) (the storage workstream) and
[Hosting & Persistence](../architecture/backend-hosting-persistence.md) (the silo/storage model).
It is registered in the spike catalog as **S16** in
[SQL Server to Postgres](./sql-server-to-postgres.md#spike-driven-execution).

## What single-dev validation already covers

The current stack is validated only on a single laptop: one in-process silo
(`DevelopmentHost`), SQL Server storage, backend plus frontend. The simulation suite boots a
2-silo in-process `TestCluster` (`LibLifeCycleTest/TestCluster.fs`), which validates in-process
multi-silo correctness but not SQL Server clustering, real inter-process networking, or
failover. No throughput or load harness exists.

## Why published wire-protocol numbers do not transfer

The payload path uses a custom codec (`EggShellSubjectGrainsCodec`, registered as
`IGeneralizedCodec` / `IGeneralizedCopier` / `IDeepCopier` / `IFieldCodec` in
`LibLifeCycleCore/src/OrleansEx/Serialization.fs`), not `[<GenerateSerializer>]`. Production F#
types are deliberately not annotated for source-generated serialization. Orleans' published
wire-protocol gains are measured on the default serializer, so the actual codec is measured
directly on the wire.

## Method: head-to-head Docker Compose A/B

Two structurally identical Docker Compose stacks run sequentially on the same host under the
same CPU and memory limits:

- **old**: net7 + Orleans 3.7 (checked out from the pre-upgrade commit; the old build is
  recoverable from git history).
- **new**: net10 + Orleans 10.2.1.

Each stack is 3 silo containers plus 1 SQL Server plus 1 load client. Container networking
reports the relative A/B honestly; absolute numbers understate real-network cost, which the
report states explicitly.

### Harness components (`Meta/ClusterVerify/`)

- **Grains**: a synthetic `EchoGrain` (framework and wire ceiling) and `ComputeGrain`
  (placement and scheduling under CPU load), plus a caller into the real subject grains for the
  codec, SQL persistence, and reminder path. Two builds share one op-mix definition because the
  Orleans 3.7 and 10 grain and client APIs differ. The echo payload carries a
  representative subject-shaped payload so the custom codec is exercised.
- **Load client**: an async generator with a concurrency ladder, warmup, and a fixed
  steady-state window per step, writing HdrHistogram percentiles to `results.json` with an
  identical schema across stacks.
- **Reliability runner**: drives `docker kill`, `docker network disconnect`, and restart,
  asserting cluster truth via `IManagementGrain` (`GetDetailedHosts`,
  `GetSimpleGrainStatistics`), the SQL membership table, and log scrape.
- **Comparison**: `compare.fsx` diffs old versus new `results.json` and emits the verdict
  table.

Observability is identical on both stacks: Orleans statistics plus OpenTelemetry/Prometheus
counters (throughput, request-latency histograms, silo membership gauge, activation counts).

## Reliability scenarios

1. **Cluster formation**: 3 silos join the SQL clustering table and all reach ACTIVE.
2. **Placement and distribution**: activations spread across silos; the single-activation
   guarantee holds under concurrent calls to one key.
3. **Reminders across silos**: a reminder fires; killing the owning silo migrates the reminder
   and it keeps firing. This regression-guards the `IReminderRegistry` service registration on
   the `SiloBuilder` Proper branch (`LibLifeCycleHost/src/OrleansEx/SiloBuilder.fs`).
4. **Failover under load**: killing one silo mid-load retries in-flight calls, recovers
   activations on survivors, loses no data, and the cluster re-stabilizes within a measured
   time.
5. **Membership hygiene**: a dead silo is evicted from the table and defunct entries cleaned.
6. **Partition (optional)**: a network-disconnected silo is detected and recovers.

## Throughput and latency

A concurrency ladder (for example 1, 8, 32, 128, 512 outstanding requests) runs three op mixes
per step, each with warmup then a fixed measurement window:

- **echo-only**: framework and wire ceiling with a representative payload.
- **compute-only**: placement and scheduling under CPU load.
- **real subject ops**: end-to-end cost through the custom codec, SQL, and reminders.

Each combination reports req/s and p50/p99/p999. The synthetic mixes isolate Orleans overhead;
the real mix isolates the codec-and-storage delta.

## Acceptance

On identical hardware limits and op mix:

- Throughput on the new stack is at or above the old stack at every concurrency step, treating
  ±5% as parity.
- p99 and p999 latency on the new stack are at or below the old stack at every step.
- All reliability scenarios pass, and failover recovery time is at or below the old stack.

The deliverable is a single markdown verdict report: a side-by-side table (metric by
concurrency by stack), reliability pass/fail with recovery times, and a per-dimension verdict
line.

## Relationship to other spikes

- **[S14](./sql-server-to-postgres.md#tier-5--validation--perf-acceptance)** validates Kubernetes
  membership on Postgres clustering. S16 validates the Orleans jump itself on the current SQL
  Server clustering, so the two are complementary: S16 proves upgrade parity before the storage
  cut-over, S14 proves the target production membership topology.
- **[S2](./sql-server-to-postgres.md#spike-driven-execution)** benchmarks the Postgres TVP
  strategy. S16 reuses the same benchmark discipline (concurrency ladder, warmup, percentile
  reporting) for the cluster-wide throughput comparison.
