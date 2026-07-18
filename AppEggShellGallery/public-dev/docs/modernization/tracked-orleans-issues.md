# Tracked upstream Orleans issues (affecting Goal G)

Tracked here so we notice when upstream resolves them. A resolution may let us simplify the
codebase (drop a workaround, delete a spike-only helper, relax a constraint). Each entry
records: status, our affected files, the workaround in our code, and the simplification path
if the upstream issue is resolved. Checked against `dotnet/orleans` on the date noted.

The master plan: [SQL Server to Postgres](./sql-server-to-postgres.md). Spike catalogs in
[spikes/](./spikes/). Engineering log: [knowledge-base/engineering-log.md](../knowledge-base/engineering-log.md).

| # | Title | State | Affects | Our workaround | Simplification if resolved | Last checked |
|---|---|---|---|---|---|---|
| [#8520](https://github.com/dotnet/orleans/issues/8520) | `GenerateCodeForDeclaringAssembly` is critical and undocumented | OPEN | `LibLifeCycleCodeGenHost.csproj` (to be added in S15b-production-port) | Carry TWO `[assembly: GenerateCodeForDeclaringAssembly(...)]` attributes (one for F# Types, one for F# Grains) — empirically confirmed by S15b (23-line stub with one attribute → 666-line full codegen with two). | If upstream adds transitive scan (scan referenced assemblies of the given type's assembly), we could drop the second attribute. If upstream documents the multi-assembly case, we could drop the comment explaining it. | 2026-07-18 |
| [#8717](https://github.com/dotnet/orleans/issues/8717) | F# discriminated unions, where some cases have no associated values, fails to compile with error CS0122 | OPEN | `LibLifeCycleCore` F# types with nullary cases (`TodoAction.ToggleDone`, `TodoAction.Archive`, `TodoOpError.EmptyTitle`, etc.) | `[<assembly: InternalsVisibleTo("LibLifeCycleCodeGenHost")>]` on `LibLifeCycleCore` (confirmed by gfix's reply on #8717 + S15b spike). NO caller cascade tax — keeps `TodoAction.ToggleDone` (not `TodoAction.ToggleDone ()`). | If upstream fixes the source generator to handle nullary cases without the InternalsVisibleTo (e.g., by emitting `new()` calls instead of referencing the private nested classes), we can drop the InternalsVisibleTo attribute entirely. | 2026-07-18 |
| [#8235](https://github.com/dotnet/orleans/issues/8235) | F# host: Could not find an implementation for interface ... — C# host ok | OPEN | F# Host (`SuiteTodo/Launchers/Dev/DevelopmentHost/src/DevelopmentHost.fs`) | `[<assembly: Orleans.ApplicationPartAttribute("LibLifeCycleCodeGenHost")>]` + `[<assembly: Orleans.ApplicationPartAttribute("LibLifeCycleHost")>]` on the F# Host (fwaris's workaround, confirmed by S15b spike). | If upstream fixes F# host auto-discovery of codegen-host-generated metadata (e.g., by making the SDK auto-add ApplicationPartAttribute for referenced assemblies), we can drop both lines. | 2026-07-18 |
| [#6703](https://github.com/dotnet/orleans/issues/6703) | Remove `IExternalSerializer` interface from `OrleansJsonSerializer` | CLOSED (fixed in #7070) | `LibLifeCycleCore/src/OrleansEx/Serializer.fs` (the existing custom wire serializer) | Rewrite against `IGeneralizedCodec`+`IGeneralizedCopier`+`IFieldCodec`+`IDeepCopier` (S15b-production-port). The new pattern is mandatory; there is no path back to `IExternalSerializer`. | N/A — already resolved upstream; our migration to the new pattern is the resolution. | 2026-07-18 |
| [#10227](https://github.com/dotnet/orleans/issues/10227) | 10.2.0 tightened type allow-listing | OPEN (informational) | `TypeManifestOptions.AllowAllTypes = true` on both silo and client (`ISiloConfigurator` + `IClientBuilderConfigurator`) | Set `AllowAllTypes = true` globally (per official API docs). | If upstream relaxes the allow-listing (or auto-allows types in the same assembly as grain interfaces), we could remove the `ISiloConfigurator` + `IClientBuilderConfigurator` configurators. Low-priority — the configurators are 6 lines total. | 2026-07-18 |
| [#8255](https://github.com/dotnet/orleans/issues/8255) | F# DU serialization broken in early Orleans 7.x (fixed via PR #9095) | CLOSED (fixed) | N/A — informational; the fix shipped in `Microsoft.Orleans.Serialization.FSharp`. | None — the package is used as-is. | N/A — already resolved. The S15 spike empirically confirmed the fix works for built-in shapes (Unit/Option/ValueOption/Choice); user-defined F# records/DUs/Result are NOT covered (the package ships only the built-in codecs). | 2026-07-18 |
| **S15 finding #7 (genuinely novel -- NO upstream report located)** | Per-case `[<Id(n)>]` attribute on F# union cases is accepted by the F# compiler but the emitted C# codec does NOT honor it -- multi-case DUs round-trip with structural mismatch; `Set<Category>` throws "Unable to cast 'Personal' to 'Work'". | UNFILED | Affects the source-gen path -- which we are NOT using (S15b bypasses it via the custom `IGeneralizedCodec`). | Bypass source-gen entirely for F# types; use the custom `IGeneralizedCodec` wrapping Fleece/STJ. The F# types carry NO `[<GenerateSerializer>]`/`[<Id(n)>]` annotations. | If upstream fixes the per-case `[Id(n)]` honoring in the C# source generator's `FSharpUtilities`, we could reconsider using source-gen for F# types -- BUT this is not a simplification we want; the custom codec gives us version-tolerant wire format + control over the Fleece JSON body that source-gen cannot match. Filing the issue is still worthwhile (codemem 1891 notes this). | 2026-07-18 |
| [ADI #112](https://github.com/autofac/Autofac.Extensions.DependencyInjection/issues/112) / [#116](https://github.com/autofac/Autofac.Extensions.DependencyInjection/issues/116) | `Autofac.Extensions.DependencyInjection` < 9.0.0 does not support .NET 8 keyed services; Orleans 10 registers keyed services, so silo boot crashes with `RegisterInstance(null) -> ArgumentNullException (Parameter 'instance')` | CLOSED (fixed in ADI 9.0.0) | Test harness only: `LibLifeCycleTest.fsproj` + `TestCluster.fs` (uses `AutofacServiceProviderFactory` for child-scope registration override) + the 3 Ecosystem test projects (SuiteTodo, SuiteJobs, Meta template) | Bumped `Autofac.Extensions.DependencyInjection` 6.0.0 -> 10.0.0 and `Autofac` 6.1.0 -> 8.3.0. `TestCluster.fs` needed an explicit `Register<T>` type arg (Autofac 8 added a `Register<'TDependency1,'TComponent>` overload that made a bare `fun _ ->` ambiguous). | N/A -- keyed-services support is the resolution; keep ADI >= 9.0.0 for any Orleans 10 host. | 2026-07-19 |
| **S15d: Orleans 10 decomposes F# Option/ValueOption/Choice/Result natively (genuinely novel finding; SOLVED by S15d spike, session 44)** | The `Microsoft.Orleans.Serialization.FSharp` 10.2.1 package ships `FSharpOptionCodec`, `FSharpValueOptionCodec`, `FSharpChoiceCodec\`2..6`, and (new vs S15 finding #2) `FSharpResultCodec\`2`. These DECOMPOSE the wrapper and delegate each generic arg to that arg's own codec. An `IGeneralizedCodec` is a fallback and never wins over these, so registering the WHOLE wrapper (`Option<X>` / `Result<X, E>`) leaves the bare inner leaf uncovered -> `CodecNotFoundException` for the leaf at silo boot. | OPEN (informational -- not a bug; Orleans working as designed) | `LibLifeCycleCore/src/OrleansEx/Serializer.fs` (S15b custom codec registers ~30 whole wrappers: `Option<BlobData>`, `Result<unit, GrainRefreshTimersAndSubsError>`, etc.) | **S15d-production-port (pending):** register the bare LEAF types (user F# records/DUs); make the two in-file validation guards decomposition-aware (peel Option/ValueOption/Choice/Result/tuples to leaves; scalars are native); drop wrappers whose only inner is native (`Option<DateTimeOffset>`) or already bare-registered (`Option<SideEffectDedupInfo>`). Keep `FSharpList`/`Map`/`Set` wholesale (NOT native). Never reuse typeIds. | If upstream ever drops the built-in F# codecs (won't happen) the wrappers could be reclaimed wholesale. Not expected. | 2026-07-19 |
| **S15c: F# closure misclassification (genuinely novel -- NO upstream report; SOLVED by S15c spike, session 43; PRODUCTION-PORT LANDED session 44)** | Orleans 10 source generator misclassifies F# compiler-generated **object-expression** closures (`{ new ISubjectGrainObserver<...> with ... }` / `{ new ILifeEventAwaiter<...> with ... }`) as `InterfaceImplementations`, emitting invalid C# `config.InterfaceImplementations.Add(typeof(...observer @ 33))` (`@` not a valid identifier -> CS1646/CS1026/CS0426). Root cause (verified in ~/Code/orleans v10.2.1): `CodeGenerator.cs:224-259` adds any non-abstract public/internal class implementing a `[GenerateMethodSerializers]`-annotated (inherited) interface to `InvokableInterfaceImplementations` with **no `IsCompilerGenerated()` filter**; `IGrainObserver : IAddressable` carries the attribute, so F# object-expression closures get flagged. (The `backgroundTask { }` CE state machines are NOT flagged -- they implement `IAsyncStateMachine`, not a grain interface.) | UNFILED (file with `Meta/s15c-closure-codegen/` as minimal repro; upstream fix = one-line `!symbol.IsCompilerGenerated()` guard at `CodeGenerator.cs:224`) | Production `GrainConnector.fs:117` (`ILifeEventAwaiter` awaiter) + `Web/RealTime.fs:103` (`ISubjectGrainObserver`) object expressions. **The FS0909 `ConnectorGrain` sub-blocker was a MISDIAGNOSIS** -- the real error was a `=>` typo at `ConnectorGrain.fs:133` hidden inside the commented block. | **SOLVED: lift the grain-observer object expressions to named top-level F# classes** (valid `typeof`; extra manifest registration benign -- proven by a real 2-silo grain-observer round-trip in the S15c spike). For ConnectorGrain: uncomment the block + fix `=>` -> `->` (no FS0909, no constraint drop). Applied in the S15c-production-port follow-up work item, then re-enable both `GenerateCodeForDeclaringAssembly` attributes. | If upstream adds the `IsCompilerGenerated()` guard, F# object expressions on grain-observer interfaces would work without the named-class lift. | 2026-07-19 |

## How to use this doc

- Before each Orleans-related work item, re-check the OPEN issues via `gh issue view N --repo dotnet/orleans --comments`.
- If an issue is resolved CLOSED, evaluate the simplification path: does the workaround file's complexity drop?
  If yes, file a follow-up work item to remove the workaround (after confirming the resolution is in the
  Orleans 10.2.x line we use; we do NOT upgrade Orleans versions mid-port).
- New issues that affect us should be appended here with the same columns. Cite the codemem entry that captures
  the symptom-to-fix detail.
- When a spike catalog doc cites an issue, link the issue back from this table.

## Process

To re-check all OPEN issues in one shot:

```sh
for n in 8520 8717 8235 10227; do
  echo "=== #$n ==="
  gh issue view $n --repo dotnet/orleans --json state,title,updatedAt | jq '{state, title, updatedAt}'
done
```

(For the UNFILED S15 finding #7, file a new issue with the S15 spike as repro before re-checking here. For the S15c closure-misclassification blocker (now SOLVED, session 43), file a new issue with `Meta/s15c-closure-codegen/` as the minimal repro; the upstream fix is a one-line `!symbol.IsCompilerGenerated()` guard at `CodeGenerator.cs:224`.)

## Orleans source clone (for the production-port phase)

The `dotnet/orleans` repo is cloned shallow at the `v10.2.1` tag (matches our package pin) at
`~/Code/orleans` (sibling to `~/Code/subject`). 34M. Use it for the S15b-production-port work
item (the actual LibLifeCycleCore rewrite). Key files to reference:

- `src/Orleans.Serialization/Codecs/IFieldCodec.cs` -- the IFieldCodec interface (WriteField / ReadValue).
- `src/Orleans.Serialization/Serializers/IGeneralizedCodec.cs` -- IGeneralizedCodec : IFieldCodec (+ IsSupportedType).
- `src/Orleans.Serialization/Cloning/IDeepCopier.cs` -- IDeepCopier + IGeneralizedCopier + IBaseCopier + CopyContext (all in one file).
- `src/Orleans.Serialization.FSharp/FSharpCodecs.cs` -- the built-in F# codecs (FSharpUnitCodec, FSharpOptionCodec<T>, FSharpValueOptionCodec<T>, FSharpChoiceCodec<T1,T2>). Confirms S15 finding #2: only those 4 are built-in, no user-defined F# DU/record codecs.
- `src/Orleans.CodeGenerator/ApplicationPartAttributeGenerator.cs` -- emits the `[assembly: Orleans.ApplicationPartAttribute("...")]` lines into generated code.
- `src/Orleans.CodeGenerator/Diagnostics/GenerateCodeForDeclaringAssemblyAttribute_NoDeclaringAssembly_Diagnostic.cs` -- the source generator's diagnostic for when `GenerateCodeForDeclaringAssembly` is given a type with no declaring assembly.

The clone is shallow (depth 1) at the v10.2.1 tag. To refresh or upgrade to a newer tag:

```sh
cd ~/Code/orleans
git fetch --depth 1 origin tag v10.2.2-rc.2
git checkout v10.2.2-rc.2
```

The `dotnet/samples` repo (for the official F# HelloWorld sample) is NOT cloned locally; it is
~3GB. Fetch specific files on-demand via raw.githubusercontent.com URLs (e.g. the README was
read via `https://raw.githubusercontent.com/dotnet/samples/main/orleans/FSharpHelloWorld/README.md`).
