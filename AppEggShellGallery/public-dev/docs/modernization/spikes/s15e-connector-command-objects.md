# S15e -- Connector command objects (Option B)

Branch `shafayat/pgsql-initial-spikes`. Production port replacing F# `FSharpFunc` arguments on
`IConnectorGrain` with serializable command-object interfaces, eliminating the
`CodecNotFoundException: Could not find a copier for type LibLifeCycle.Services+buildRequest@282-2`
closure-copier failure that caused the `SuiteJobs` "Stasis not reached" stalls. Master plan: the
**S15e** entry in `modernization/sql-server-to-postgres.md`. S15e gates S1 (PG18 baseline) -- the
Orleans 10 cluster can now dispatch connector requests end-to-end.

## Question

`IConnectorGrain<'Request,'Env>` took the request builder and response mapper as F# function
arguments (`ResponseChannel<'Reply> -> 'Request` and `'Reply -> LifeAction`). The Orleans 10
source-generated invoker deep-copies every grain method argument before dispatch. F# compiler-generated
closure classes (e.g. `LibLifeCycle.Services+buildRequest@282-2`) are not serializable/copiable by the
built-in `Microsoft.Orleans.Serialization.FSharp` package, so `CopyContext.DeepCopy` threw
`CodecNotFoundException`, the transient connector side effect never completed, and the 15-second
stasis wait timed out.

Can we keep the same connector programming model while passing **values** (command objects) instead of
**closures** across the grain boundary?

## Setup / scope inventory

- One connector definition uses the grain dispatch path: `JobRunnerConnector`
  (`SuiteJobs/Ecosystem/LifeCycles/Connectors/JobRunnerConnector.fs`).
- One production call site: `SuiteJobs/Ecosystem/LifeCycles/JobLifeCycle.fs:138-149`.
- The 16 `SuiteJobs` test sites that reference `JobRunnerRequest.RunJob (data, responseChannel)` use
  `Ecosystem.useConnector` (in-process test mocks in `LibLifeCycleTest/Ecosystem.fs:1028`) -- a
  separate code path, NOT affected by this change.
- `ViewConnector` uses `Service.Query` (in-process), not `IConnectorGrain` dispatch.

## Critical findings

### Finding 1 -- closures on grain methods are not Orleans-10 copiable

Orleans 10 source-generated proxies call `CopyContext.DeepCopy<T>` for every method argument. F#
compiler-emitted closures implement `FSharpFunc`2` and may capture arbitrary state; they are neither
annotated with `[<GenerateSerializer>]` nor handled by `Microsoft.Orleans.Serialization.FSharp`. The
resulting `CodecNotFoundException` is fatal for a transient side effect because the failure is caught
inside the dispatch loop and the side effect is never marked processed, which causes the stasis
tracker to time out after 15 seconds.

### Finding 2 -- command objects replace the two closure roles

The captured state is trivially serializable:

- `buildRequest` closure at `Services.fs:282` captures the DU constructor `JobRunnerRequest.RunJob`
  (static, no state) and a `RunJobRequestData` record (fully serializable Guid/JobId/bool/JobPayload).
- `buildAction` closure at `JobLifeCycle.fs:144` captures nothing; it is a pure pattern-match from
  `Guid * ProcessingJobUpdate` to `JobAction`.

Two named F# classes implement generic interfaces and carry the captured state as fields:

```fsharp
[<AllowNullLiteral>]
type IConnectorRequestBuilder<'Request, 'Reply when 'Request :> Request> =
    abstract Build: MultiResponseChannel<'Reply> -> 'Request

[<AllowNullLiteral>]
type IConnectorRequestBuilderSingleReply<'Request, 'Reply when 'Request :> Request> =
    abstract Build: ResponseChannel<'Reply> -> 'Request

[<AllowNullLiteral>]
type IConnectorResponseMapper<'Reply, 'Action when 'Action :> LifeAction> =
    abstract Map: 'Reply -> 'Action
```

Per-connector named classes implement these interfaces (e.g. `JobRunnerRequestBuilder` and
`JobRunnerResponseMapper`). The method bodies on the grain side are unchanged -- they call
`requestBuilder.Build(channel)` and `responseMapper.Map(response)`.

### Finding 3 -- the codec must recognize the command objects for deep-copy, not necessarily serialize them

`LibLifeCycleCore/src/OrleansEx/Serializer.fs` cannot reference app-specific builder classes (it sits
below `LibLifeCycle` and `SuiteJobs`). Instead, `EggShellSubjectGrainsCodec`'s
`IGeneralizedCopier.IsSupportedType` is extended to recognize any type implementing one of the three
command-object interfaces by generic definition. `IDeepCopier.DeepCopy` returns the same instance --
these objects are immutable value-like carriers and are only being deep-copied by the invoker for
process-isolation semantics. This unblocks the in-process `TestCluster` path where the actual failure
occurred.

### Finding 4 -- tupled grain method form is required for Orleans dispatch

F# curried method signatures compile to `FSharpFunc` even when the arguments themselves are not
functions. `IConnectorGrain.SendRequest` / `SendRequestMultiResponse` are rewritten in tupled form
with explicit generic method parameters `'Reply` and `'Action`, per the S15b lesson that grain
interface methods must be tupled for Orleans interop (codemem 1895).

## Per-file change list

1. `LibLifeCycle/src/Services.fs` -- add the three command-object interfaces; rewrite
   `Connector.Request` and `Connector.RequestMultiResponse` to accept builder/mapper instances; remove
   the unused `ServiceQueryExtensions` connector `Request` / `BlockingRequest` overloads (they
   produce closures incompatible with grain dispatch).
2. `LibLifeCycle/src/LifeCycle.fs` -- update `ExternalOperation` DU cases to carry `obj` builder /
   mapper values instead of `obj` functions.
3. `LibLifeCycleHost/src/GrainHostInterface.fs` -- rewrite `IConnectorGrain` methods in tupled form
   taking the typed builder/mapper interfaces.
4. `LibLifeCycleHost/src/ConnectorGrain.fs` -- implement the new `IConnectorGrain` signatures;
   invoke `.Build(channel)` and `.Map(response)` directly.
5. `LibLifeCycleHost/src/ConnectorAdapter.fs` -- pass builder/mapper objects into the grain; use a
   reflected helper to recover the response `'Action` type from the mapper's generic interface.
6. `LibLifeCycleHost/src/SideEffectProcessor.fs` -- remove the `buildAction >> (fun action -> action
   :> LifeAction)` wrapper; pass the stored mapper object through unchanged.
7. `LibLifeCycleHost/src/SubjectGrainModel.fs` -- change `GrainTransientSideEffect.ConnectorRequest`
   and `ConnectorRequestMultiResponse` to carry builder/mapper `obj` values.
8. `LibLifeCycleCore/src/OrleansEx/Serializer.fs` -- extend `IGeneralizedCopier.IsSupportedType` to
   recognize command-object interface implementations and deep-copy them by reference.
9. `SuiteJobs/Ecosystem/LifeCycles/Connectors/JobRunnerConnector.fs` -- add `JobRunnerRequestBuilder`
   and `JobRunnerResponseMapper` named classes.
10. `SuiteJobs/Ecosystem/LifeCycles/JobLifeCycle.fs:138-149` -- switch the call site from
    `jobRunnerConnector.Request JobRunnerRequest.RunJob { ... } (fun ...)` to
    `jobRunnerConnector.RequestMultiResponse (JobRunnerRequestBuilder { ... }) (JobRunnerResponseMapper())`.

## Validation targets

- `dotnet build` 0 errors / 0 warnings across `LibLifeCycleCore`, `LibLifeCycleHost`,
  `LibLifeCycleCodeGenHost`, `LibLifeCycleTest`, `SuiteTodo/Ecosystem/Tests`,
  `SuiteJobs/Ecosystem/Tests`.
- `dotnet test LibLifeCycleTest/LibLifeCycleTest.fsproj`: **93/93 PASS**.
- `dotnet test SuiteTodo/Ecosystem/Tests/Tests.fsproj`: **5/5 PASS**.
- `dotnet test SuiteJobs/Ecosystem/Tests/Tests.fsproj`: **47/47 PASS**, zero occurrences of
  "Stasis not reached", "CodecNotFoundException", or "Could not find a copier".

## Results

Executed on `shafayat/pgsql-initial-spikes` after S15d-production-port.

| Suite | Target | Result |
|---|---|---|
| `dotnet build LibLifeCycleCore/src/LibLifeCycleCore.fsproj` | 0 errors | PASS |
| `dotnet build LibLifeCycleHost/src/LibLifeCycleHost.fsproj` | 0 errors | PASS |
| `dotnet build LibLifeCycleHost/LibLifeCycleCodeGenHost/LibLifeCycleCodeGenHost.csproj` | 0 errors | PASS |
| `dotnet build LibLifeCycleTest/LibLifeCycleTest.fsproj` | 0 errors | PASS |
| `dotnet build SuiteTodo/Ecosystem/Tests/Tests.fsproj` | 0 errors | PASS |
| `dotnet build SuiteJobs/Ecosystem/Tests/Tests.fsproj` | 0 errors | PASS |
| `LibLifeCycleTest` (`dotnet test`) | 93/93 PASS | PASS |
| `SuiteTodo/Ecosystem/Tests` | 5/5 PASS | PASS |
| `SuiteJobs/Ecosystem/Tests` | 47/47 PASS, 0 "Stasis not reached", 0 `CodecNotFoundException`, 0 "Could not find a copier" | PASS |
| `typeof(global::...@...)` count in `LibLifeCycleCodeGenHost.orleans.g.cs` | 0 | PASS |

The command-object rewrite is the production fix for the Orleans 10 closure-copier gap and unblocks S1.

## Risks and follow-up work

- The `IGeneralizedCopier` extension returns the same instance rather than serializing state. This is
  correct for the in-process `TestCluster` where the failure reproduced, but a real cross-silo
  deployment will need a proper serializer for concrete command-object types or a change to the grain
  contract that transports the raw request payload instead of the builder object.
- The unused connector-side `ServiceQueryExtensions.Request` / `BlockingRequest` overloads were
  removed. If any external consumer relies on the curried fluent form, it will need to construct a
  builder class instead; a repo-wide search showed no callers.
- F# named classes implementing non-grain-observer interfaces are benign to the Orleans source generator,
  but the generated `LibLifeCycleCodeGenHost.orleans.g.cs` must still be inspected for any stray
  `typeof(global::...@...)` patterns after the build.
