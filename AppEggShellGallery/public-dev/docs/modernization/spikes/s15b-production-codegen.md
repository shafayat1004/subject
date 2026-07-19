# S15b -- Production codegen-host pattern for F# grain interfaces (Orleans 10)

Branch: `shafayat/pgsql-initial-spikes`. Spike per `modernization/sql-server-to-postgres.md`
S15b (Tier 1). Gated S1 + the whole Orleans jump. S15 found that the custom wire serializer
in `LibLifeCycleCore/src/OrleansEx/Serializer.fs` cannot simply be deleted -- 1 of 10 F#
shapes round-tripped through Orleans 10's source-generated codecs. S15b proves the production
pattern: a 4-project layout (Types / Grains / Codegen / Host), `InternalsVisibleTo` to
sidestep the nullary-case CS0122 cascade tax, and a custom `IGeneralizedCodec` wrapping the
existing Fleece/STJ wire serializer. Throwaway project under `Meta/s15b-production-codegen/`
-- not part of production build.

## Setup (4-project layout)

S15's 3-project layout (Types / Codegen / Host) was insufficient: the C# source generator
could not emit grain-class metadata because the grain impls lived in the F# Host project,
which the source generator (C#-only) does not scan. The production-shape 4-project layout:

| Project | Lang | Role |
|---|---|---|
| `Meta/s15b-production-codegen/Types/S15b-production-codegenTypes.fsproj` | F# | F# types (NO `[<GenerateSerializer>]` -- handled by custom IGeneralizedCodec) + F# grain interfaces (generic + non-generic with `when` constraints) + `[<assembly: InternalsVisibleTo("S15b-production-codegenCodegen")>]` to expose nullary-case internal classes. |
| `Meta/s15b-production-codegen/Grains/S15b-production-codegenGrains.fsproj` | F# | F# grain impls (e.g. `BlobSpikeGrain`, `ViewSpikeGrain`). References Types. |
| `Meta/s15b-production-codegen/Codegen/S15b-production-codegenCodegen.csproj` | C# | Carries TWO `[assembly: GenerateCodeForDeclaringAssembly(...)]` attributes -- one pointing at a type from Types (for interface invoker codegen), one pointing at a type from Grains (for grain-class metadata + activator codegen). References Types + Grains. |
| `Meta/s15b-production-codegen/Host/S15b-production-codegen.fsproj` | F# | Console host: 2-silo TestCluster + custom `IGeneralizedCodec`/`IGeneralizedCopier` + `[<assembly: Orleans.ApplicationPartAttribute("S15b-production-codegenCodegen")>]` + `[<assembly: Orleans.ApplicationPartAttribute("S15b-production-codegenGrains")>]`. References Types + Grains + Codegen. |

FSharp.Core pinned to 10.0.103 (the version `Microsoft.Orleans.Serialization.FSharp` 10.2.1
requires) via `<PackageReference Include="FSharp.Core" VersionOverride="10.0.103" />` per
project (C# projects use `Include` + `VersionOverride`; F# projects use `Update` +
`VersionOverride`; the repo's `Directory.Build.targets` translates `VersionOverride` to a
winning `Version` that beats the pinned 9.0.201 in `Directory.Build.props`). Lesson from
S10 (codemem 1889).

Both silo and client configure `TypeManifestOptions.AllowAllTypes = true` via
`ISiloConfigurator` + `IClientBuilderConfigurator` (Lesson from S15 finding #6).

## Critical findings

1. **`[<assembly: InternalsVisibleTo("Codegen")>]` on the F# Types project IS the workaround
   for the nullary-case CS0122 issue** (S15 finding #5), with NO caller cascade tax. F# nullary
   union cases (e.g. `| EmptyTitle`, `| ToggleDone`, `| Archive`) compile to private nested
   classes the C# source generator references when emitting invoker code. Adding
   `[<assembly: System.Runtime.CompilerServices.InternalsVisibleTo("S15b-production-codegenCodegen")>]`
   to the F# Types assembly exposes those nested classes to the C# generated code -- no `of
   unit` change required. Callers keep writing `TodoAction.ToggleDone` (not
   `TodoAction.ToggleDone ()`).
   **Upstream**: dotnet/orleans issue #8717 (gfix confirmed the same workaround verbatim --
   "Adding `[<InternalsVisibleTo("YourCodeGenProject")>]` does indeed work. Thank you for the
   swift reply!" -- and ReubenBond's reply indicates it's the canonical path). This OVERRIDES
   S15's "cascade tax is unavoidable" decision: the production port can keep nullary F# DU
   cases as-is and just add the InternalsVisibleTo attribute.
2. **The C# source generator needs TWO `GenerateCodeForDeclaringAssembly` attributes when the
   grain interfaces and grain classes live in separate assemblies.** With a single
   `[assembly: GenerateCodeForDeclaringAssembly(typeof(BlobSpikeGrain))]` (pointing at the
   F# Grains assembly only), the generated file was 23 lines -- just a TypeManifestProviderBase
   stub with `InterfaceImplementations.Add(...)` for the two grain classes, but NO invokers and
   NO grain-class metadata. Runtime: "Unable to find an IGrainReferenceActivatorProvider for
   grain type ...". Adding a second attribute
   `[assembly: GenerateCodeForDeclaringAssembly(typeof(IBlobSpikeGrain))]` (pointing at the F#
   Types assembly where the interfaces live) grew the generated file to 666 lines with full
   invoker + activator + proxy code. Each `GenerateCodeForDeclaringAssembly` scans the
   DECLARING assembly of the given type -- it does not transitively scan referenced
   assemblies. Lesson: when interfaces and impls are split, point at BOTH.
   **Upstream**: dotnet/orleans issue #8520 ("GenerateCodeForDeclaringAssembly is critical
   and undocumented"). #8520 describes the single-assembly case; the multi-assembly case
   (interfaces + impls split) is a natural extension the issue does not cover.
3. **The custom `IGeneralizedCodec`/`IGeneralizedCopier` pattern (Orleans 10) replaces the
   removed `IExternalSerializer`** (Orleans 3.x) cleanly. The implementation:
   - Implement `IGeneralizedCodec` (with `IsSupportedType`) + `IGeneralizedCopier` (with
     `IsSupportedType`) + `IFieldCodec` (with `WriteField`/`ReadValue`) + `IDeepCopier` (with
     `DeepCopy`).
   - Register via `services.AddSingleton<IGeneralizedCodec, FSharpJsonCodec>()` (and same
     for the other 3 interfaces) on both silo and client.
   - The codec claims all F# types via `IsSupportedType`, bypassing source-generated codecs
     entirely. This sidesteps S15 finding #7 (source-generated F# DU codecs mis-route
     multi-case DUs).
   - `IDeepCopier.DeepCopy` returns `input` as-is for immutable F# types (matches the
     existing Serializer.fs behavior).
   **Upstream**: official docs `learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/serialization-customization`
   ("Custom serializer implementation" section) show the exact pattern: implement
   `IGeneralizedCodec, IGeneralizedCopier, ITypeFilter` and register via DI.
4. **The custom codec's wire format MUST include a type-name prefix** (or a TypeId int/byte)
   so the reader knows the runtime type. The naive "try each registered type" heuristic
   mis-routes inner payloads when called by typed codecs like `FSharpResultCodec` (from
   `Microsoft.Orleans.Serialization.FSharp`): e.g. when the Result wrapper calls into my
   codec for the Ok payload, my heuristic returned the first type that deserialized
   (BlobData instead of Todo), and the FSharpResultCodec threw
   `InvalidCastException: Unable to cast 'BlobData' to 'Todo'`. The production
   `Serializer.fs` already has this design -- a TypeId byte prefix in the wire format -- so
   the port is straightforward.
5. **F# grain impls CAN live in an F# project** (not in the C# Codegen project) -- they just
   need a sibling C# project to scan them. The 4-project layout (Types/Grains/Codegen/Host)
   has no project-reference cycle: Codegen (C#) references Types + Grains (F#); Host (F#)
   references Types + Grains + Codegen. The F# Grains project does NOT reference the C#
   Codegen project (the runtime wiring is via `ApplicationPartAttribute` string-name, not a
   project ref). The official F# sample (C# host scanning F# Grains via
   `GenerateCodeForDeclaringAssembly`) inverts this; our production shape (F# host loading
   C#-generated metadata via ApplicationPartAttribute) is the inverse and is what the
   `dotnet/orleans` issue #8235 workaround documented.
   **Upstream**: dotnet/orleans issue #8235 ("F# host: Could not find an implementation for
   interface OrleansDashboard.IDashboardGrain - C# host ok") -- fwaris's workaround was
   adding `[<assembly: Orleans.ApplicationPartAttribute("GrainsCodeGen")>]` (and others) to
   the F# host's `Program.fs`. This spike reproduced that workaround + extended it to the
   case where the grain impls are ALSO F# (in a separate F# Grains project).
6. **System.TextJson does NOT natively handle F# DUs, Options, or FSharpList**. The
   spike's hand-rolled `JsonConverter` (using `FSharp.Reflection`) handles them, with
   special cases for `FSharpOption` (Some -> write inner value, None -> null) and
   `FSharpList` (write as JSON array, read by folding Cons). Production uses Fleece+STJ
   (`CodecLib.StjCodecs`) which already covers all F# shapes; the spike's converter is a
   drop-in for the Fleece dependency. STJ convention gotcha: `JsonConverter.Read` must
   leave the reader at the END of the value (at the closing quote/brace/bracket), NOT past
   it -- calling `reader.Read()` after `reader.GetString()` causes
   "The converter read too much or not enough" errors. After
   `JsonSerializer.Deserialize<T>(ref reader, ...)` returns, the reader is at the END of the
   value (not past it) -- the caller must explicitly advance via `reader.Read()` to get to
   the next token.

## Per-shape round-trip result

All shapes pass through a 2-silo in-process TestCluster (Orleans 10.2.1) with the custom
`IGeneralizedCodec` wrapping a hand-rolled FSharp.Reflection-based STJ converter.

| Shape | Annotation | Round-trip | Note |
|---|---|---|---|
| `BlobData` (record with `Guid` + `byte[]`) via `Option<BlobData>` | custom IGeneralizedCodec (no `[<GenerateSerializer>]`) | **PASS** | First end-to-end round-trip via `IBlobSpikeGrain.SetBlob`/`GetBlob`. |
| `TodoAction.SetTitle of string` (DU single-payload case) | custom codec | **PASS** | |
| `TodoAction.ToggleDone` (DU nullary case) | custom codec | **PASS** | InternalsVisibleTo workaround empirically confirmed -- no caller change needed. |
| `TodoAction.Archive` (DU nullary case) | custom codec | **PASS** | Same as above. |
| `TodoOpError.EmptyTitle` (DU nullary error case) returned as `Error` of `Result<Todo, TodoOpError>` | custom codec + `FSharpResultCodec` from `Microsoft.Orleans.Serialization.FSharp` | **PASS** | The `FSharpResultCodec` (Orleans-provided) handles the Result wrapper; the inner `TodoOpError` payload uses my custom codec via the type-name prefix dispatch. |
| `Result<GetTodoOutput, TodoOpError>` with `GetTodoOutput` containing `Todo list` | custom codec + FSharpResultCodec | **PASS** | Nested FSharpList + F# record round-trips through the typed Result codec into the custom codec. |
| `Priority.High` (DU all-nullary cases) inside `Todo` | custom codec | **PASS** | |

Final spike verdict: **S15b PASSES the no-regression bar.** 7 of 7 representative shapes
round-trip through the production-shape codegen-host pattern. The custom `IGeneralizedCodec`
pattern is THE production porting path for `LibLifeCycleCore/src/OrleansEx/Serializer.fs`.

## Decision

The master plan's S15b "production codegen-host pattern" question is **proven**:

- **The custom wire serializer in `LibLifeCycleCore/src/OrleansEx/Serializer.fs` must be
  REWRITTEN** as a custom `IGeneralizedCodec` + `IGeneralizedCopier` (+ `IFieldCodec` +
  `IDeepCopier`) registered via DI on both silo and client. The existing 83-entry
  `UntypedSerializer.ForIsomorphicType(...)` table maps directly to the new pattern: each
  entry becomes a TypeId in the wire format, the JSON body serialization reuses the existing
  Fleece+gzip path (drop-in for the spike's hand-rolled STJ converter), and the
  `IDeepCopier.DeepCopy` returns `input` as-is for immutable F# types (matches existing
  behavior). The new file replaces `IExternalSerializer`/`ICopyContext`/`IDeserializationContext`/
  `ISerializationContext` (all removed in Orleans 10) with the 4 new interfaces.
- **A new C# `LibLifeCycleCodeGenHost.csproj` sibling project must be added** to LibLifeCycleCore,
  carrying `[assembly: GenerateCodeForDeclaringAssembly(typeof(IViewClientGrain<,>))]` (or
  similar) pointing at the F# Types assembly AND `[assembly: GenerateCodeForDeclaringAssembly(typeof(SomeConcreteGrain))]` pointing at the F# Grains assembly (where the
  real grain impls live, currently in `LibLifeCycleHost`). The 4-project layout is mandatory
  because the source generator (C#-only) cannot scan the F# Host project directly.
- **`[<assembly: InternalsVisibleTo("LibLifeCycleCodeGenHost")>]` must be added to
  `LibLifeCycleCore`** (the F# Types project) to expose nullary-case internal classes to the
  C# source generator. This replaces the S15 finding #5 "of unit cascade tax" -- no caller
  changes needed. Every nullary F# DU case (`TodoAction.ToggleDone`, `TodoAction.Archive`,
  `TodoOpError.EmptyTitle`, etc.) keeps its current call-site syntax.
- **`[<assembly: Orleans.ApplicationPartAttribute("LibLifeCycleCodeGenHost")>]` and
  `[<assembly: Orleans.ApplicationPartAttribute("LibLifeCycleHost")>]`** must be added to
  the F# Host's startup file (`DevelopmentHost.fs` or equivalent) to wire the C#-generated
  invokers + the F# grain impls into the silo's runtime.
- **`TypeManifestOptions.AllowAllTypes = true`** on both silo and client (carried over from
  S15 finding #6).

## Open questions / surprises

- **Production's `IViewClientGrain<'Input, 'Output, 'OpError>` uses self-referential
  interface constraints** (`'Input :> ViewInput<'Input>`, etc.). The spike simplified to
  plain interface constraints (`'Input :> ISpikeViewInput`). The codegen question is the
  same -- `GenerateCodeForDeclaringAssembly` scans the assembly and emits invokers for any
  generic interface it finds, regardless of constraint shape. The production constraints
  should work; if they don't, that's a follow-up S15c spike.
- **The 4-project layout is the spike's biggest structural contribution.** The official
  F# sample's 3-project layout (C# Interfaces / F# Grains / C# Host) inverts our
  production shape (F# Interfaces / F# Grains / F# Host). Production needs a 4th C#
  codegen-host project sitting between the F# Types/Grains and the F# Host. This is not
  documented upstream; the spike is the first place this 4-project pattern was needed.
- **The custom codec's wire format type-name prefix** is a string (the runtime type's
  `FullName`). Production's `Serializer.fs` uses a single byte TypeId (1uy, 2uy, ..., 83uy)
  -- more compact but requires a static type-id registry. The porting choice: keep the
  byte TypeId scheme (matches existing wire format, faster, requires careful
  forward-only compatibility) OR switch to type FullName (slower, self-describing, no
  registry needed). Spike uses FullName for simplicity; production should keep byte TypeId
  for wire-format compatibility.
- **STJ's `JsonConverter.Read` convention** (leave reader at END of value, not past it) is
  not obvious from the docs; the spike discovered it empirically via
  "The converter read too much or not enough" errors. Production uses Fleece, which
  manages reader position itself, so this is spike-only detail -- but worth recording for
  future spikes that use STJ directly.
- **Process win this time**: upstream research via `gh issue` BEFORE writing the .fsproj
  surfaced the `InternalsVisibleTo` workaround (dotnet/orleans #8717), the
  `IGeneralizedCodec` pattern (official docs), and the F# host
  `ApplicationPartAttribute` workaround (dotnet/orleans #8235) -- all before any code was
  written. Total spike time: ~2.5h including iteration. S15 (which skipped upstream
  research) took 4-5h and missed 6 of 7 documented findings. The spike-driven skill's
  step-1 mandate paid off.

## Next spikes (worklist update)

- **S1 (Orleans-on-PG18 baseline)** -- **UNBLOCKED**. The S15b pattern proves
  `LibLifeCycleCore` can compile under Orleans 10 (the codegen-host + custom codec rewrite
  is the path). S1 is a throwaway console that boots a real Orleans 10 silo against PG18
  with `UseLocalhostClustering` + ADO.NET persistence + reminders (Npgsql invariant).
  Still gated on the actual LibLifeCycleCore rewrite (S15b-production-port -- a follow-up
  work item, NOT a spike; the spike has proven the pattern).
- **S4 (PostgresGrainStorageHandler)** -- unchanged; gated by S1.
- **S15b-production-port (NEW work item, NOT a spike)** -- Apply the spike pattern to the
  real `LibLifeCycleCore`: add `LibLifeCycleCodeGenHost.csproj` sibling, add
  `[<assembly: InternalsVisibleTo("LibLifeCycleCodeGenHost")>]` to LibLifeCycleCore, rewrite
  `OrleansEx/Serializer.fs` against `IGeneralizedCodec`/`IGeneralizedCopier` (port the 83
  TypeId entries), rewrite `OrleansEx/Serialization.fs` to register the new codec via DI
  (replace removed `SerializationProviderOptions`/`IApplicationPartManager`), rewrite
  `GrainConnectorProvider.fs` for the new `UseOrleansClient` generic-host pattern, resolve
  the `GrainConnector.fs:126` Task CE Bind overload mismatch (FSharp.Core 10 / .NET 10
  effect). Files: `LibLifeCycleCore/src/GrainClientInterface.fs` (no change -- the F# types
  keep their `get_Codec` Fleece extensions; they're called by the new IGeneralizedCodec),
  `LibLifeCycleCore/src/OrleansEx/Serializer.fs` (rewrite),
  `LibLifeCycleCore/src/OrleansEx/Serialization.fs` (rewrite),
  `LibLifeCycleCore/src/GrainConnectorProvider.fs` (rewrite),
  `LibLifeCycleCore/src/GrainConnector.fs` (fix Task CE), plus a new
  `LibLifeCycleCodeGenHost/LibLifeCycleCodeGenHost.csproj` (C#).
