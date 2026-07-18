# S15d -- F# Option/ValueOption/Choice/Result wrapper decomposition by Orleans 10

Branch `shafayat/pgsql-initial-spikes`. Throwaway spike at `Meta/s15d-fsharp-wrapper-codecs/`. Surfaced
by the S15c-production-port follow-up: once the codegen blocker was cleared and the production silo could
actually boot in the `LibLifeCycleTest` / `SuiteTodo` simulation harness, grain dispatch failed with
`CodecNotFoundException` for bare F# leaf types (`BlobData`, `GrainRefreshTimersAndSubsError`, ...). This
spike isolates and answers the underlying serialization question. Master plan: the **S15d** entry in
`modernization/sql-server-to-postgres.md`. S15d gates S1 (PG18 baseline) -- a real silo cannot round-trip
grain calls until the custom serializer is reworked.

## Question

Does Orleans 10 serialize F# `Option` / `ValueOption` / `Choice` / `Result` WHOLESALE -- so the S15b
custom `IGeneralizedCodec` can claim `Option<X>` / `Result<X, E>` as one blob -- or does it DECOMPOSE
them natively and delegate each generic arg to that arg's own codec, forcing the custom codec to claim
the bare INNER leaf types (`X`, `E`) instead?

## Setup (4-project layout)

Mirrors the S15b/S15c pattern (`Types` / `Grains` / `Codegen` / `Host`):

- `Types/Shapes.fs` -- two F# record leaves (`PingPayload`, `PingError`, no `[<GenerateSerializer>]`) +
  `IPingGrain` whose methods return `Task<Option<PingPayload>>` and
  `Task<Result<PingPayload, PingError>>` (the exact production shape: `Task<Option<BlobData>>`,
  `Task<Result<unit, GrainRefreshTimersAndSubsError>>`).
- `Grains/Grains.fs` -- `PingGrain` returning `Some` / `Ok` / `Error` values.
- `Codegen/CodegenAssemblyInfo.cs` -- two `GenerateCodeForDeclaringAssembly` attributes (Types + Grains).
- `Host/Program.fs` -- a custom `IGeneralizedCodec` (`LeafCodec`) with a self-describing JSON payload, a
  2-silo `TestCluster`, and a real client round-trip of all three shapes. `Registry.supported` is the
  single toggle between Phase A (wrappers) and Phase B (bare leaves).

## Critical findings

### Finding 1 -- Orleans 10 DECOMPOSES F# Option/ValueOption/Choice/Result; register bare leaves

**Package inventory (verified, `Microsoft.Orleans.Serialization.FSharp` 10.2.1):** the package ships
`FSharpUnitCodec`, `FSharpOptionCodec\`1`, `FSharpValueOptionCodec\`1`, `FSharpChoiceCodec\`2..6`, and --
**new vs the S15 finding #2** -- `FSharpResultCodec\`2` (+ matching copiers). So Result IS natively
handled now. (Confirmed via `strings` over the package DLL and its XML doc.)

**Phase A (register wrappers) -- verbatim failure:** with
`Registry.supported = [ typeof<Option<PingPayload>>; typeof<Result<PingPayload, PingError>> ]` the silo
fails to boot:

```
Orleans.Serialization.CodecNotFoundException : Could not find a codec for type S15D.PingPayload.
  at Orleans.Serialization.Serializers.CodecProvider.ThrowCodecNotFound(Type fieldType)
```

Orleans resolves `Option<PingPayload>` via `FSharpOptionCodec` and `Result<PingPayload, PingError>` via
`FSharpResultCodec`. A generic/specific codec always wins over an `IGeneralizedCodec` (which is a
fallback), so Orleans NEVER asks our codec for the wrapper -- it asks only for the decomposed inner leaf
(`PingPayload`, `PingError`), which the wrapper-only registration never supplied.

**Phase B (register bare leaves) -- PASS:** with
`Registry.supported = [ typeof<PingPayload>; typeof<PingError> ]` all three shapes round-trip through a
real 2-silo cluster:

```
PASS: Option<PingPayload> round-trips
PASS: Result Ok<PingPayload> round-trips
PASS: Result Error<PingError> round-trips
SPIKE PASS: 3 / FAIL: 0
```

**Decision:** the custom generalized codec must register the bare LEAF types (user F# records/DUs), never
the F# native wrappers. Orleans natively serializes: primitives, string, Guid, DateTime(Offset),
TimeSpan, decimal, unit, tuples/value-tuples, and (decomposing) `Option`/`ValueOption`/`Choice`/`Result`.
F# collections (`FSharpList`, `Map`, `Set`) are NOT native -- the custom codec still claims those
wholesale.

**Upstream:** not a bug -- this is Orleans working as designed (generalized codec is a fallback). No issue
to file. The S15b design predated `FSharpResultCodec` shipping and assumed wrappers could be claimed
wholesale; that assumption is simply wrong for the 5 native F# wrappers.

## Per-shape result table

| Declared grain shape | Orleans path | Register wrapper (Phase A) | Register bare leaf (Phase B) |
|---|---|---|---|
| `Option<PingPayload>` | FSharpOptionCodec -> inner | **FAIL** CodecNotFound PingPayload | **PASS** |
| `Result<PingPayload, PingError>` (Ok) | FSharpResultCodec -> inner | **FAIL** | **PASS** |
| `Result<PingPayload, PingError>` (Error) | FSharpResultCodec -> inner | **FAIL** | **PASS** |

## Impact on production (`LibLifeCycleCore/src/OrleansEx/Serializer.fs`)

The S15b custom serializer (`EggShellSubjectGrainsCodec` + `getUntypedSubjectSerializers`) registers
whole wrappers in ~30 of its ~67 entries -- e.g. `Option<BlobData>` (typeId 73),
`Option<ConstructSubscriptions>` (18), `Option<TemporalSnapshot<..>>` (32), and every
`Result<X, E>` / `Result<Option<X>, E>`. Under Orleans 10 each of those must be replaced by bare-leaf
registrations of the inner user F# type(s), and the two in-file validation guards
(`declaredGrainParamAndRetValTypes` redundancy check + the "must define serializers" check) must be made
decomposition-aware (peel Option/ValueOption/Choice/Result/tuples to their leaves; treat scalars as
native). Types whose only inner is native (`Option<DateTimeOffset>` (53), `Option<Tuple<int64,uint64>>`
(68)) drop entirely; types whose inner is already registered bare (`Option<SideEffectDedupInfo>` (45), the
bare `SideEffectDedupInfo` is typeId 44) drop the wrapper. TypeIds are never reused -- retire in place and
append new ones.

An in-progress runtime enumeration (during the S15c-production-port session) confirmed the top-level
`Option<_>` declared leaves as: `ConstructSubscriptions`, `SideEffectDedupInfo` (already bare-registered),
`BlobData`, `TemporalSnapshot<..>` (per lifecycle), plus the native inners `DateTimeOffset` and
`Tuple<int64,uint64>`. The `Result<_,_>` leaves (e.g. `GrainRefreshTimersAndSubsError`) surface the same
way and need the same bare-leaf treatment; a complete list must be gathered from a clean run since the
framework's reflection-based declared-type scan is assembly-load-order sensitive.

## Production-port outcome (session 45, LANDED)

**S15d-production-port landed.** Reworked `LibLifeCycleCore/src/OrleansEx/Serializer.fs` per the spike
decision: 23 whole-wrapper entries retired (TypeIds 8/15/17-19/21-22/32/36-39/45-46/53-55/59/62-66/68/
71/73/77/79-83), 20 new bare-leaf registrations added at TypeIds 84-103 (subject) + 4/5 (view), and both
in-file coverage guards made decomposition-aware via `decomposeToNativeLeaves`. Catalog's "must be gathered
from a clean run" caveat turned out to be unnecessary -- the guards' reflection-based declared-type scan
finds the leaves deterministically once `AllowAllTypes = true` is set.

**Second bug found and fixed (not part of the spike).** `SiloBuilder.ConfigureSiloClientForEcosystem`'s
`TestCluster`/`TestClusterDataSeeding` branch never called `configureSiloClientSerializers`, so the test
client DI never registered `EggShellSubjectGrainsCodec` at all. Orleans 3.x's lazy codec resolution masked
this for years; Orleans 10's eager `AnalyzeSerializerAvailability` makes it fatal at
`ClusterClient..ctor`. Fix: added the call. Same audit applies to ANY `IClientBuilder` path -- every
client configurator must register the codec, not just the silo.

**Verification.**
- `dotnet build` 0 errors / 0 warnings across `LibLifeCycleCore` + `LibLifeCycleHost` +
  `LibLifeCycleCodeGenHost` + `LibLifeCycleTest` + `SuiteTodo/Ecosystem/Tests` + `SuiteJobs/Ecosystem/Tests`.
- `dotnet test LibLifeCycleTest/LibLifeCycleTest.fsproj`: **93/93 PASS**.
- `dotnet test SuiteTodo/Ecosystem/Tests/Tests.fsproj`: **5/5 PASS** (real 2-silo grain round-trips:
  construct/act/get/snapshot/subscribe + blob + reminders).
- `dotnet test SuiteJobs/Ecosystem/Tests/Tests.fsproj`: **18/47 PASS, 29 FAIL** with a uniform
  `Stasis not reached, 1 side effects not processed within 00:00:15 : [Transient ...` failure. This is a
  pre-existing issue surfaced for the first time because the SuiteJobs test project had a stale Test SDK
  (16.8.3) that prevented any test from running under .NET 10. Bumped Test.Sdk to 17.12.0 + added
  `xunit.runner.visualstudio` (needed because `SimulationTestFramework` extends `XunitTestFramework`).
  Tracked as a separate follow-up work item; does NOT block S15d-production-port or S1.

**Next.** S1 (Orleans 10 silo against Postgres 18, throwaway) is unblocked. SuiteJobs `Stasis not reached`
follow-up is its own work item.

## Next work item

- **S1 (PG18 baseline)** -- no longer gated. Throwaway console booting a real Orleans 10 silo against
  Postgres 18 with `UseLocalhostClustering` + ADO.NET persistence + reminders (Npgsql invariant).
- **SuiteJobs `Stasis not reached` follow-up** (separate, NOT gating S1). 29/47 SuiteJobs simulation
  tests fail with a uniform 1-`GrainSideEffect.Transient` unprocessed within the 15s
  `defaultStasisWaitFor`. Likely an Orleans 3.7 -> 10 timing/dispatch difference in the SuiteJobs-specific
  job lifecycle (scheduling + retries + connector heartbeat + reminders). The basic side-effect tracking
  path works (LibLifeCycleTest's `SideEffectTracking.fs` + `ClockSimulation.fs` pass 93/93); the stall is
  in SuiteJobs's higher-level job-state machine.

