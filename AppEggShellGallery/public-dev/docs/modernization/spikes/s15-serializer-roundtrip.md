# S15 -- Orleans 10 wire serializer x F# records/DUs round-trip

Branch: `shafayat/pgsql-initial-spikes`. Spike per `modernization/sql-server-to-postgres.md` S15
(Tier 1). Gated: the master plan assumed `Microsoft.Orleans.Serialization.FSharp` 10.2.1 covers
F# DU/record wire serialization. **This spike disproves that assumption** and discovers the actual
shape of the F# + Orleans 10 interop, plus the workaround pattern that DOES emit serializers for
F# types. Throwaway project under `Meta/S15SerializerRoundtrip/` -- not part of production build.

## Setup (3-project layout)

Pure F# is impossible. The Orleans C# source generator (only in `analyzers/dotnet/cs/`) does not
run on F# compilations, so a pure-F# Orleans project emits **zero** generated invokers/serializers
and fails at runtime. The spike uses three projects:

| Project | Lang | Role |
|---|---|---|
| `Meta/S15SerializerRoundtrip/Types/S15Types.fsproj` | F# | F# types with `[<GenerateSerializer>]` + `[<Id(n)>]`. References `Microsoft.Orleans.Core` 10.2.1, `Microsoft.Orleans.Serialization.FSharp` 10.2.1. |
| `Meta/S15SerializerRoundtrip/Codegen/S15CodegenHost.csproj` | C# | Grain interfaces + grain impls in C# (so the source generator runs). Carries `[assembly: GenerateCodeForDeclaringAssembly(typeof(S15SerializerRoundtrip.Annotated.Todo))]` in `CodegenAssemblyInfo.cs` -- this is what makes the C# generator scan the referenced F# assembly for `[GenerateSerializer]` types. |
| `Meta/S15SerializerRoundtrip/Host/S15SerializerRoundtrip.fsproj` | F# | Console host: boots 2-silo in-process `TestCluster`, calls each grain method, asserts round-trip. References `Types` + `Codegen` + `Microsoft.Orleans.Server` + `Microsoft.Orleans.TestingHost`. |

FSharp.Core is pinned to 10.0.103 (the version `Microsoft.Orleans.Serialization.FSharp` 10.2.1
requires) via `<PackageReference Include="FSharp.Core" VersionOverride="10.0.103" />` per project.
The repo's `Directory.Build.targets` translates `VersionOverride` into a `Version` that beats the
pinned 9.0.201 in `Directory.Build.props`. Without the override, NU1605 downgrade warning fires and
`Microsoft.Orleans.Serialization.FSharp` may not bind correctly.

Both silo and client configure `TypeManifestOptions.AllowAllTypes = true` via an `ISiloConfigurator`
and `IClientBuilderConfigurator`; without this, Orleans 10 rejects every user type with "Type is
not allowed."

## Critical findings

Most of these were discoverable by websearch *before* building the spike -- the spike ended up
being mostly empirical confirmation of upstream-documented behavior. That websearch was not done
first is a process miss; the citations are included below so the next person does not have to
re-derive them. Finding #7 (per-case `[<Id(n)>]` not honored on multi-case DUs) appears to be
genuinely novel -- no upstream report found.

1. **Orleans 10 source generator is C#-only.** The `Microsoft.Orleans.CodeGenerator` package
   exposes `analyzers/dotnet/cs/` and nothing else. There is no F# analyzer. A pure F# project
   referencing `Microsoft.Orleans.Server` + `Microsoft.Orleans.Serialization.FSharp` builds cleanly
   but emits zero source-generated code. Runtime: `Could not find an implementation for interface
   S15SerializerRoundtrip.IAnnotatedSpikeGrain` on the first `GetGrain<...>` call.
   **Upstream**: the official Orleans F# sample
   (https://learn.microsoft.com/en-us/samples/dotnet/samples/orleans-fsharp-sample/) README
   states verbatim: "The Microsoft.Orleans.Sdk package does not support emitting F# code,
   however, it supports analyzing F# assemblies and emitting C# code."
2. **`Microsoft.Orleans.Serialization.FSharp` provides only built-in F# codecs.** Its public API
   (from the XML doc list) is `FSharpUnitCodec`, `FSharpOptionCodec<'T>`, `FSharpValueOptionCodec<'T>`,
   `FSharpChoiceCodec<'T1,'T2>` and matching copiers. **There is no codec for user-defined F#
   records or DUs.** The package does not register any F#-specific source generator.
3. **C# source generator does not scan referenced assemblies by default.** A C# project that
   references an F# types assembly gets invoker codecs emitted for its own C# grain interfaces, but
   the F# types referenced as method parameters get `Could not find a codec for type
   S15SerializerRoundtrip.Annotated+Todo` at silo boot. `[assembly: ApplicationPartAttribute
   ("S15Types")]` is a runtime-only marker; it does not trigger compile-time codegen.
   **Upstream**: dotnet/orleans issue #8520 "GenerateCodeForDeclaringAssembly is critical and
   undocumented" (https://github.com/dotnet/orleans/issues/8520) -- a C# dev with .NET since 1.1
   also could not find this in the docs.
4. **The fix that works: `[assembly: GenerateCodeForDeclaringAssembly(typeof(SomeTypeInTargetAssembly))]`**
   in the C# project. This attribute tells the Orleans source generator to scan the declaring
   assembly of the given type for `[GenerateSerializer]` types and emit codecs/copiers/activators
   into the current C# compilation. After adding
   `[assembly: GenerateCodeForDeclaringAssembly(typeof(S15SerializerRoundtrip.Annotated.Todo))]`
   to `CodegenAssemblyInfo.cs`, the generated file grew from 21 lines (empty TypeManifest) to 5034
   lines including codecs/copiers/activators for every `[<GenerateSerializer>]` F# type in
   `S15Types.dll`.
   **Upstream**: documented in the Orleans F# sample + dotnet/orleans issue #8520.
5. **F# nullary union cases are inaccessible to the C# source generator.** A nullary case like
   `| EmptyTitle` compiles to a private nested class `Annotated.TodoError._EmptyTitle`. The source
   generator emits references to that nested type (`global::S15SerializerRoundtrip.Annotated.TodoError._EmptyTitle`)
   but it is `internal` in F#, so the C# generator's output fails with `error CS0122: 'Annotated.TodoError._EmptyTitle'
   is inaccessible due to its protection level`. The spike worked around this by changing nullary
   cases to `of unit` (e.g. `| [<Id(0u)>] EmptyTitle of unit`). This is ergonomically worse at the
   call site (`TodoError.EmptyTitle ()` instead of `TodoError.EmptyTitle`) and is a **production
   concern**, not a spike-only detail: every nullary union case in the real subject types
   (`TodoAction.ToggleDone`, `TodoAction.Archive`, `TodoAction.Delete`, `TodoLifeEvent.Created`,
   `TodoLifeEvent.Archived`, `TodoOpError.EmptyTitle`, etc.) would have to change, and the cascade
   touches every caller. Worth a separate evaluation in S15b.
   **Upstream**: dotnet/orleans issue #8717 "F# discriminated unions, where some cases has no
   associated values, fails to compile with error CS0122"
   (https://github.com/dotnet/orleans/issues/8717) -- exact same symptom and same workaround
   proposed by an outside F# team migrating 3.x -> 7.0. SO Q77159202 "What is required to make F#
   discriminated unions work with Orleans 7" -- answer: "DU cases must carry some data."
6. **`TypeManifestOptions.AllowAllTypes = true` is mandatory** for both silo and client. Without
   it, Orleans 10's type-allow-list rejects every user type with "Type is not allowed. To allow
   it, add it to TypeManifestOptions.AllowedTypes or register an ITypeNameFilter or ITypeFilter
   instance which allows it." Set via `ISiloConfigurator` and `IClientBuilderConfigurator` in
   `Program.fs`.
   **Upstream**: official API docs
   (https://learn.microsoft.com/en-us/dotnet/api/orleans.serialization.configuration.typemanifestoptions.allowalltypes)
   -- "Gets or sets a value indicating whether to allow all types by default. Default: false."
   Also discussed in dotnet/orleans issue #10227 (10.2.0 tightened type allow-listing).
7. **The `[<Id(n)>]` per-case attribute is honored by the source generator.** Without it, the
   generator refuses to emit codecs for F# union types (silent skip -- the codec simply does not
   appear, leading to a runtime "no codec" error). With it, codecs are emitted.
   **Genuinely novel finding (no upstream report located)**: even WITH `[<Id(n)>]` per case,
   multi-case-shape F# DUs fail round-trip with "structural mismatch" (see per-shape table below).
   The `CollectionsRecord` round-trip throws `Unable to cast object of type 'Personal' to type
   'Work'` -- the generated codec emits a single codec for the base `Category` type and mis-routes
   cases. Possibly an interaction with the `of unit` workaround (finding #5), or a real bug in
   `Microsoft.Orleans.CodeGenerator` 10.2.1's `FSharpUtilities`. Worth filing against
   dotnet/orleans with the spike as the repro. This is the only finding the websearch did not
   surface -- every other finding was upstream-documented.

## Per-shape round-trip result (Annotated module)

The `Unannotated` module (F# types without `[<GenerateSerializer>]`) was removed during the spike
because, per finding #2 + #3, unannotated F# types have no path to a codec and are guaranteed
to fail. Only the `Annotated` module was tested.

| Shape | Annotation | Round-trip | Note |
|---|---|---|---|
| `Priority` (DU, 3 cases all `of unit`) | `[<GenerateSerializer>]` + `[<Id(n)>]` per case | **PASS** | Basic DU round-trips. |
| `TodoAction` (mixed DU: `SetTitle of string`, `ToggleDone of unit`, `SetPriority of Priority`, `SetCategory of Option<Category>`, `SetDueOn of Option<DateTimeOffset>`) | annotated | **FAIL** | "structural mismatch" -- the round-tripped value differs from the input. Likely the source generator does not preserve the `[<Id(n)>]` per-case ordinal for F# unions and falls back to positional case indices, so multi-case-shape unions desync. |
| `TodoConstructor` (single-case DU with multi-field payload) | annotated | **FAIL** | "structural mismatch" -- same suspected cause. |
| `Todo` (record with 9 fields, including `Option<DateTimeOffset>`, `Option<Category>`, nested `TodoId of Guid`) | annotated | **FAIL** | "structural mismatch" -- the `Option` fields round-trip but the inner DU values may shift case index. |
| `TodoLifeEvent` (DU, 2 nullary `of unit` + 2 single-field) | annotated | **FAIL** | "structural mismatch" |
| `TodoError` (DU, 1 nullary `of unit` + 1 `Conflict of Guid`) | annotated | **FAIL** | "structural mismatch" |
| `ResultWrapper` (record wrapping `Result<Todo, TodoError>`) | annotated | **FAIL** | "structural mismatch" -- `Result` is not a built-in `FSharpChoice` and is not covered by `Microsoft.Orleans.Serialization.FSharp`. |
| `CollectionsRecord` (record with `string list`, `int array`, `Set<Category>`, `Map<string, int>`, `Option<Option<int>>`) | annotated | **FAIL** | "Unable to cast object of type 'Personal' to type 'Work'" -- the source-generated codec for `Set<Category>` mixes up the union-case instances. Confirms the per-case `[<Id(n)>]` is not honored for F# union types; the generator emits a single codec for the `Category` base type and mis-routes cases. |
| `NestedResultWrapper` (record wrapping `Result<Result<Todo, TodoError> list, TodoError>`) | annotated | **FAIL** | "structural mismatch" |
| `StoreRetrieve` (Store + Retrieve on the same grain instance) | annotated | **FAIL** | "retrieved mismatch" -- the stored `Todo` does not round-trip back through the wire. Same root cause as `Todo`. |

Final spike verdict: **S15 FAILS the no-regression bar.** Only 1 of 10 representative shapes
round-trips. The `[<GenerateSerializer>]` + `[<Id(n)>]` + C# `GenerateCodeForDeclaringAssembly`
pattern emits codecs but those codecs do not correctly serialize F# DUs with more than one case.
The F# record + plain `Option` + `Priority` (a single-payload-shape DU) work; everything more
complex breaks.

## Decision (preliminary)

The master plan's S15 "Proves: F# type interop with the Orleans 7+ serializer, the single highest-
risk part of the Orleans jump" is **not proven**. The spike instead proves:

- **The custom wire serializer in `LibLifeCycleCore/src/OrleansEx/Serializer.fs` CANNOT simply be
  deleted.** `Microsoft.Orleans.Serialization.FSharp` 10.2.1 does not cover the repo's F# DU/record
  shapes. The existing custom serializer must either (a) be ported to Orleans 10's new
  `IFieldCodec`/`IBaseCopier` interfaces (replacing the removed `IExternalSerializer`/
  `ICopyContext`/`IDeserializationContext`/`ISerializationContext`), or (b) be replaced by another
  hand-written layer that produces codecs the new wire protocol understands. This is a substantial
  rewrite, not a deletion.
- **The bigger blocker is grain-interface codegen, not the serializer.** `LibLifeCycleCore/src/
  GrainClientInterface.fs` defines `IViewClientGrain`, `ISubjectClientGrain`, `ISubjectIdGenerationGrain`,
  `ISubjectRepoGrain`, `IBlobRepoGrain`, `ISubjectReflectionGrain` in F# (currently inheriting
  `IGrainWithGuidCompoundKey`). Under Orleans 10 those F# interfaces need a C# codegen host
  project carrying `[assembly: GenerateCodeForDeclaringAssembly(typeof(...))]` for the silo and
  client to load invokers. The grain impls (in `LibLifeCycleHost`) similarly need C#-side codegen
  registration metadata. The spike pattern is a workable template; the production port needs a
  new spike **S15b** to confirm it applies to the real grain interfaces and the real subject type
  hierarchy (which is far deeper than the spike's `Todo` shape).
- **The nullary-case `of unit` workaround cascades** through every caller of every nullary DU case
  in `LibLifeCycleTypes` / `SuiteTodo/Ecosystem/Todo.Types` / `SuiteJobs`. That is a project-scale
  ergonomics break and should be avoided. Either find a different F#-nullary-case workaround in
  S15b, or move the affected types to C# records (which conflicts with the FSharpPlus/Fleece
  ecosystem in `LibLangFsharp`), or accept the ergonomics tax.

## Open questions / surprises

- **Process miss: websearch was not done before building the spike.** 6 of 7 findings were
  upstream-documented (Orleans F# sample, dotnet/orleans issues #8520 and #8717, official
  TypeManifestOptions API docs, SO Q77159202). The spike still contributed empirical confirmation
  that the documented behavior holds on 10.2.1 for the repo's actual shapes, plus the genuinely
  novel finding #7, plus a per-shape round-trip table the upstream issues do not provide. But the
  4-5 hours of build/iterate work could have been compressed to ~1 hour if the upstream issues had
  been read first. Lesson for future spikes: websearch upstream issues + the official sample's
  README *before* writing code, especially for an Orleans/Microsoft-ecosystem question -- the
  Orleans team and outside F# teams have hit and documented most of these.
- The `[<Id(n)>]` attribute on F# union cases is accepted by the F# compiler but the emitted C#
  codec does not appear to honor it: the `CollectionsRecord` round-trip fails with a cast error
  that points to a single codec for the base `Category` type routing all cases through one
  branch. This may be a bug in `Microsoft.Orleans.CodeGenerator` 10.2.1's F# union handling
  (the generator contains `FSharpUtilities`, `FSharpUnionCaseTypeDescription`,
  `FSharpUnionCaseFieldDescription` -- it tries to handle F# unions, but the per-case Id
  mapping is not wired through). Worth filing against dotnet/orleans with the spike as the
  repro.
- `Result<'T, 'E>` is NOT one of `Microsoft.Orleans.Serialization.FSharp`'s built-in codecs
  (the package only ships `Unit`, `Option`, `ValueOption`, `Choice`). The repo uses `Result`
  extensively on grain interfaces. A custom `Result` codec or a wrapper-record pattern is needed.
- The Orleans README example for `Microsoft.Orleans.Serialization.FSharp` shows pure-F# grain
  interfaces and grain impls and silently omits that they only work if a separate C# project
  carries `[GenerateCodeForDeclaringAssembly]`. The README is misleading; raise a doc PR
  against dotnet/orleans referencing issue #8520.

## Next spikes (worklist update)

- **S15b (NEW, gates S1 + the whole Orleans jump)** -- Production codegen-host pattern for
  `LibLifeCycleCore`. Stand up the C# codegen-host project for the real grain interfaces in
  `GrainClientInterface.fs`, confirm invokers + serializers emit, and resolve the nullary-case
  accessibility problem (either via a non-`of unit` workaround, or by accepting the cascade, or
  by moving the affected types to C#). Also resolve `Result<'T,'E>` and the custom wire serializer
  port (`OrleansEx/Serializer.fs` rewritten against `IFieldCodec`/`IBaseCopier`). Files:
  `LibLifeCycleCore/src/GrainClientInterface.fs`, `LibLifeCycleCore/src/OrleansEx/Serializer.fs`,
  `LibLifeCycleCore/src/OrleansEx/Serialization.fs`, `LibLifeCycleCore/src/GrainConnectorProvider.fs`,
  plus a new C# `LibLifeCycleCodeGenHost.csproj` sibling.
- **S1 (Orleans-on-PG18 baseline)** -- UNBLOCKED in storage-script terms but **gated by S15b**
  in code-compilation terms. The throwaway console cannot even boot a real Orleans 10 silo
  against PG18 until `LibLifeCycleCore` compiles, which now requires the S15b work.
- **S4 (PostgresGrainStorageHandler)** -- unchanged; gated by S1.
