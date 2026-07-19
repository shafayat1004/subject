# S10 -- Orleans 3.7.2 -> 10.2.1 bump catalog

Branch: `shafayat/pgsql-initial-spikes`. Spike per `modernization/sql-server-to-postgres.md` S10.
Goal: catalog the surface area of the Orleans 3.7 -> 10 jump. NOT fixing breaks here.

## Package changes

| Project | Change |
|---|---|
| LibLifeCycleCore | `Microsoft.Orleans.Core` 3.7.2 -> 10.2.1; `Microsoft.Orleans.Clustering.AdoNet` 3.7.2 -> 10.2.1; `Microsoft.Orleans.Connections.Security` 3.7.2 -> 10.2.1; ADD `Microsoft.Orleans.Serialization.FSharp` 10.2.1; `TreatWarningsAsErrors` true -> false |
| LibLifeCycleHost | DROP `Microsoft.Orleans.Core`, `Microsoft.Orleans.OrleansRuntime`, `Microsoft.Orleans.OrleansProviders`, `Microsoft.Orleans.OrleansCodeGenerator`, `Microsoft.Orleans.OrleansTelemetryConsumers.AI`, `OrleansDashboard`; ADD `Microsoft.Orleans.Server` 10.2.1, `Microsoft.Orleans.Clustering.AdoNet` 10.2.1, `Microsoft.Orleans.Connections.Security` 10.2.1, `Microsoft.Orleans.Persistence.AdoNet` 10.2.1, `Microsoft.Orleans.Reminders.AdoNet` 10.2.1, `Microsoft.Orleans.Reminders` 10.2.1, `Npgsql` 10.0.3; `TreatWarningsAsErrors` true -> false |
| LibLifeCycleTest | DROP `Microsoft.Orleans.Core`, `Microsoft.Orleans.OrleansRuntime`, `Microsoft.Orleans.OrleansProviders`, `Microsoft.Orleans.OrleansCodeGenerator`; KEEP `Microsoft.Orleans.TestingHost` 3.7.2 -> 10.2.1; ADD `Microsoft.Orleans.Sdk` 10.2.1; `TreatWarningsAsErrors` true -> false |
| LibLifeCycleHostBuild | DROP `Microsoft.Orleans.Core`, `Microsoft.Orleans.OrleansRuntime`, `Microsoft.Orleans.OrleansProviders`, `Microsoft.Orleans.OrleansCodeGenerator`, `Microsoft.Orleans.CodeGenerator.MSBuild`; ADD `Microsoft.Orleans.Server` 10.2.1, `Microsoft.Orleans.Sdk` 10.2.1, `Microsoft.Orleans.Clustering.AdoNet` 10.2.1, `Microsoft.Orleans.Connections.Security` 10.2.1; ADD `TreatWarningsAsErrors` false and `NoWarn` NU1605 |

Dropped packages: `OrleansDashboard` 3.1.0, `Microsoft.Orleans.OrleansTelemetryConsumers.AI` 3.7.2, `Microsoft.Orleans.OrleansProviders` 3.7.2, `Microsoft.Orleans.OrleansCodeGenerator` 3.7.2, `Microsoft.Orleans.CodeGenerator.MSBuild` 3.7.2.

Added packages: `Npgsql` 10.0.3, `Microsoft.Orleans.Persistence.AdoNet` 10.2.1, `Microsoft.Orleans.Reminders.AdoNet` 10.2.1, `Microsoft.Orleans.Reminders` 10.2.1, `Microsoft.Orleans.Serialization.FSharp` 10.2.1, `Microsoft.Orleans.Sdk` 10.2.1.

Also added per-project `PackageReference Update="FSharp.Core" Version="10.0.103"` to all four edited projects because `Directory.Build.props` pins FSharp.Core 9.0.201 and `Microsoft.Orleans.Serialization.FSharp` 10.2.1 requires >= 10.0.103. Without this override restore fails with NU1605.

## Build results

| Project | Result | Errors reported by compiler | Distinct errors (deduped by file:line:code) | Notes |
|---|---|---|---|---|
| LibLifeCycleCore | FAIL | 63 | 50 | Core sources do not compile against Orleans 10 |
| LibLifeCycleHost | FAIL | 63 | 0 own errors | Build stops because Core project fails |
| LibLifeCycleHostBuild | FAIL | 63 | 0 own errors | Build stops because Core project fails |
| LibLifeCycleTest | FAIL | 63 | 0 own errors | Build stops because Core project fails |

All downstream failures are the same Core diagnostics repeated. Host, HostBuild, and Test never reach their own source files until Core is green.

## Error catalog

Grouped by error code, then file. Each entry: file:line, message. Cascade errors (inference/type-test/no-override) cluster around the missing serialization and client-builder APIs.

### FS0039 -- namespace/type/constructor not defined

- `LibLifeCycleCore/src/GrainConnectorProvider.fs:66` -- The value or constructor 'ClientBuilder' is not defined.
- `LibLifeCycleCore/src/GrainConnectorProvider.fs:66` -- The type 'IClientBuilder' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serialization.fs:6` -- The namespace 'ApplicationParts' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serialization.fs:35` -- The type 'SerializationProviderOptions' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serialization.fs:49` -- The type 'SerializationManager' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serialization.fs:119` -- The type 'IApplicationPartManager' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:635` -- The type 'ICopyContext' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:639` -- The type 'IDeserializationContext' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:655` -- The type 'ISerializationContext' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:692` -- The type 'IExternalSerializer' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:693` -- The type 'ICopyContext' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:696` -- The type 'IDeserializationContext' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:702` -- The type 'ISerializationContext' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:878` -- The type 'IExternalSerializer' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:880` -- The type 'ICopyContext' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:884` -- The type 'IDeserializationContext' is not defined.
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:900` -- The type 'ISerializationContext' is not defined.

Interpretation:
- `Orleans.ApplicationParts` namespace and `IApplicationPartManager` removed in Orleans 7+.
- `SerializationProviderOptions`, `SerializationManager` removed; wire serializer bootstrapping now goes through `Microsoft.Orleans.Sdk` / source generators.
- `IExternalSerializer`, `ICopyContext`, `IDeserializationContext`, `ISerializationContext` removed; custom wire serializers must move to the new source-generator friendly model.
- `ClientBuilder` / `IClientBuilder` no longer in `Microsoft.Orleans.Core`; client builder API moved to `Microsoft.Orleans.Client` / `UseOrleansClient` generic-host integration.

### FS0072 -- type of expression could not be inferred before member access

- `LibLifeCycleCore/src/GrainConnectorProvider.fs:66`
- `LibLifeCycleCore/src/GrainConnectorProvider.fs:96`
- `LibLifeCycleCore/src/OrleansEx/Serialization.fs:205`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:640`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:641`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:642`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:668`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:669`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:673`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:885`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:886`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:887`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:913`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:914`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:918`

Interpretation: cascade from FS0039; once the missing types in `OrleansEx/Serializer.fs` and `GrainConnectorProvider.fs` are fixed, these should collapse.

### FS0855 -- no abstract or interface member for override

- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:693`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:696`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:699`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:702`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:880`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:884`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:895`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:900`

Interpretation: serializer classes inherit/overrides the removed `IExternalSerializer` and context interfaces. These classes will likely be deleted during S15 if `Microsoft.Orleans.Serialization.FSharp` fully covers F# DU/record wire serialization.

### FS0008 -- runtime coercion/type test on indeterminate type

- `LibLifeCycleCore/src/OrleansEx/Serialization.fs:49`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:640`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:641`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:642`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:885`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:886`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:887`

Interpretation: same cascade as FS0072; type annotations cannot be resolved because the target types are missing.

### FS0887 -- type is not an interface type

- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:692`
- `LibLifeCycleCore/src/OrleansEx/Serializer.fs:878`

Interpretation: `IExternalSerializer` is missing, so `obj` falls out as the erased type in inheritance position.

### FS0041 -- no overload match for method Bind

- `LibLifeCycleCore/src/GrainConnector.fs:126` -- No overloads match for method 'Bind' on `ILifeEventAwaiter<...>` inside a task CE.

Interpretation: either the awaitable shape of `ILifeEventAwaiter` no longer satisfies the task builder's `Bind` constraint, or `task` CE overload resolution changed with FSharp.Core 10 / .NET 10. Likely needs explicit `GetAwaiter`/result-type adjustment, not just Orleans API changes.

## Observations

- Total distinct errors: 50 across 4 files in Core (build reported 63; duplicates from MSBuild output sections).
- Distinct files touched: `LibLifeCycleCore/src/GrainConnector.fs`, `LibLifeCycleCore/src/GrainConnectorProvider.fs`, `LibLifeCycleCore/src/OrleansEx/Serializer.fs`, `LibLifeCycleCore/src/OrleansEx/Serialization.fs`.
- Top namespaces/APIs missing: `Orleans.ApplicationParts`, `Orleans.Runtime.Serialization` (`IExternalSerializer`, `ICopyContext`, `ISerializationContext`, `IDeserializationContext`), old client builder (`ClientBuilder`/`IClientBuilder`).
- Highest-impact file: `LibLifeCycleCore/src/OrleansEx/Serializer.fs` (custom wire serializer) and `LibLifeCycleCore/src/OrleansEx/Serialization.fs` (bootstrap config).
- `Microsoft.Orleans.Connections.Security` 10.2.1 exists: NuGet restore succeeds for all projects that reference it.
- `TreatWarningsAsErrors` set to false in the three fsproj files; `LibLifeCycleHostBuild.csproj` also got `TreatWarningsAsErrors` false and `NoWarn` NU1605. The downgrade NU1605 is a package-graph issue, not an Orleans API break.
- FSharp.Core 10.0.103 is required because of `Microsoft.Orleans.Serialization.FSharp`. Added per-project override because `Directory.Build.props` pins 9.0.201.
- No explicit `<LangVersion>` error surfaced yet; source-generator diagnostics are expected after the serializer API surface is fixed.
- Codegen package change: runtime codegen packages (`Microsoft.Orleans.OrleansCodeGenerator`, `Microsoft.Orleans.CodeGenerator.MSBuild`) replaced by `Microsoft.Orleans.Sdk` source generators. No `[GenerateSerializer]` analyzer warnings yet because Core does not compile.

## Next-spike worklist

- **S15** -- F# DU/record wire serializer round-trip (gates the Orleans jump). Files: `LibLifeCycleCore/src/OrleansEx/Serializer.fs`, `LibLifeCycleCore/src/OrleansEx/Serialization.fs`. Decide whether to delete the custom wire serializer and rely entirely on `Microsoft.Orleans.Serialization.FSharp`, or keep a thin adapter.
- **S1** -- Orleans-on-PG18 baseline. Depends on: Core compiling after S15. Switch AdoNet invariant to `Nppgsql`, wire up `PostgreSQL-Main.sql`, `PostgreSQL-Persistence.sql`, `PostgreSQL-Reminders.sql` (and `PostgreSQL-Clustering.sql` for the non-K8s fallback).
- **S4** -- `PostgresGrainStorageHandler`. Files: `LibLifeCycleHost/src/Storage/Postgres/`. Depends on: S1 silo boot path.
