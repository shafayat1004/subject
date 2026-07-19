# S15c -- F# closure misclassification by the Orleans 10 source generator

Branch `shafayat/pgsql-initial-spikes`. Throwaway spike at `Meta/s15c-closure-codegen/`. Closes the
HARD BLOCKER deferred from S15b-production-port (session 42): the Orleans 10 C# source generator emits
broken C# when scanning production-shape F# assemblies, so both `GenerateCodeForDeclaringAssembly`
attributes were commented out and runtime grain dispatch was unwired. Master plan: the **S15c** entry
in `modernization/sql-server-to-postgres.md`. S15c gates S1 (PG18 baseline) -- S1 could not boot a real
silo until grain method dispatch was proven.

Two questions:
1. **(primary, gates runtime)** How do we re-enable `GenerateCodeForDeclaringAssembly` when the
   generator misclassifies F# compiler-generated closures (from `backgroundTask { }` CEs and object
   expressions implementing grain-observer interfaces) as `InvokableInterfaceImplementations`?
2. **(secondary)** How do we resolve the F# 10 FS0909 that unwired `ConnectorGrain`'s
   `interface IConnectorGrain<...> with` block?

## Setup (4-project layout)

Mirrors the S15b 4-project pattern (`Types` / `Grains` / `Codegen` / `Host`), but the grain impls now
exercise the **production F# construct shapes** S15b's trivial impls omitted:

- `Types/Shapes.fs` -- `IPingObserver<'Payload when 'Payload : equality>` (inherits `IGrainObserver`,
  generic + constrained -- mirrors `ILifeEventAwaiter<'Subject,'LifeEvent,'SubjectId>` /
  `ISubjectGrainObserver<'Subject,'SubjectId>`) + `IPingGrain`. `[<assembly: InternalsVisibleTo>]`.
- `Grains/Grains.fs` -- `PingGrain` + a `Subscriber` helper using a `backgroundTask { }` CE that
  hands a grain-observer to `CreateObjectReference` (the exact production `GrainConnector.RunAndWait`
  shape).
- `Codegen/CodegenAssemblyInfo.cs` -- two `GenerateCodeForDeclaringAssembly` attributes (Types + Grains).
- `Host/Program.fs` -- 2-silo `TestCluster`, custom `IGeneralizedCodec` for `PingPayload`, and a
  **real grain-observer round-trip** (invokes `IPingGrain.PingObserver` via the cluster client and
  awaits the observer callback) -- the invoker path S15b never exercised.

Package refs + FSharp.Core `VersionOverride="10.0.103"` per S15b (F# projects use `Update`, the C#
Codegen uses `Include`).

## Critical findings

### Finding 1 (primary, GENUINELY NOVEL) -- F# object-expression grain observers break the generator

**Symptom (verbatim, Phase A repro -- object expression in the scanned assembly):** building the
Codegen project emits `s15c-closure-codegenCodegen.orleans.g.cs` containing:

```csharp
config.InterfaceImplementations.Add(typeof(global::S15C_Grains.Subscriber.observer @ 33));
```

which fails with:

```
error CS1646: Keyword, identifier, or string expected after verbatim specifier: @
error CS1026: ) expected
error CS0426: The type name 'observer' does not exist in the type 'Subscriber'
error CS1003: Syntax error, ',' expected
error CS1002: ; expected
error CS1513: } expected
```

`observer@33` is the F# object expression `let observer = { new IPingObserver<PingPayload> with ... }`
at `Grains.fs:33`. F# names the compiler-emitted closure class after the enclosing binding + source
line; the `@` is not a valid C# identifier char.

**Root cause (verified in `~/Code/orleans` v10.2.1, `CodeGenerator.cs:224-259`):** the generator adds
to `InvokableInterfaceImplementations` **any** non-abstract public/internal class implementing an
interface annotated (inherited) with `[GenerateMethodSerializers]`:

```csharp
if ((symbol.TypeKind == TypeKind.Class || symbol.TypeKind == TypeKind.Struct)
    && !symbol.IsAbstract
    && (symbol.DeclaredAccessibility == Accessibility.Public || symbol.DeclaredAccessibility == Accessibility.Internal))
{
    foreach (var iface in symbol.AllInterfaces)
    {
        var attribute = iface.GetAttribute(LibraryTypes.GenerateMethodSerializersAttribute, inherited: true);
        if (attribute != null) { MetadataModel.InvokableInterfaceImplementations.Add(symbol); break; }
    }
}
```

There is **no `IsCompilerGenerated()` filter** on this path (lines 170 and 194 in the same file DO
filter compiler-generated symbols; this path does not). `IGrainObserver : IAddressable`, and
`IAddressable` carries `[GenerateMethodSerializers]`, so every F# type implementing an
`IGrainObserver`-derived interface -- including F#'s compiler-emitted object-expression closure
classes -- gets flagged. C# never produces a closure that implements a grain interface, so the C#
compiler never hits this; F# object expressions do, exposing the gap.

The list is consumed by `MetadataGenerator.cs:79-84`, which emits
`config.InterfaceImplementations.Add(typeof(<symbol>))` -- and `typeof(<F# closure>)` is invalid C#.
The interface **invokers** (`Invokable_IPingObserver_GrainReference_...`) generate fine from the
interface itself (`VisitInterface`); only the per-implementation registration of the closure breaks.

**Fix (verified PASS):** lift the object expression into a **named top-level F# class**. A named class
emits a valid `typeof(global::S15C_Grains.PingObserver)`, so the generated `.g.cs` compiles. The named
class captures the same state (`TaskCompletionSource` + last-seen seq) via its constructor + a member
instead of a closure capture; behaviorally identical. After the fix, line 362 of the generated file
reads `config.InterfaceImplementations.Add(typeof(global::S15C_Grains.PingObserver));` and the observer
round-trips through a real 2-silo cluster. The extra `InterfaceImplementations` registration of a
non-grain observer class is **benign at runtime** (the round-trip PASSES).

**Upstream:** no issue reports this. Searched `dotnet/orleans` for "F# object expression grain observer
source generator", "F# closure InvokableInterfaceImplementation", "F# CreateObjectReference codegen",
etc. The closest (#5772 F# support discussion, #8977, #8502) concern the known
`GenerateCodeForDeclaringAssembly` requirement, not closure misclassification. **Genuinely novel**
(cf. S15 finding #7). Worth filing upstream with this spike as the minimal repro; the upstream fix is a
one-line `&& !symbol.IsCompilerGenerated()` guard at `CodeGenerator.cs:224`.

### Finding 2 (secondary) -- the "FS0909" ConnectorGrain blocker was a MISDIAGNOSIS

The S15b-production-port session (42) commented out `ConnectorGrain.fs`'s
`interface IConnectorGrain<'Request, 'Env> with` block, attributing the failure to F# 10 FS0909 on a
constraint-bearing generic interface, and deferred a "drop the `when 'Request :> Request` constraint"
cascade to S15c.

**This was wrong.** Reproductions of the exact shape (constraint-bearing generic grain interface with a
generic method, implemented via a late `interface ... with` block, on a `Grain()`-derived generic class
implementing two interfaces) all compile **green** under F# 10 / FSharp.Core 10.0.103 -- no FS0909.
Uncommenting the real production block and building `LibLifeCycleHost` surfaced the actual error:

```
ConnectorGrain.fs(133,58): error FS0010: Unexpected infix operator in lambda expression. Expected '->' or other token.
```

a stray `=>` (C#-style lambda arrow) in `(fun hook => hook.OnSideEffectProcessed sideEffectId)` at line
133 -- **inside the commented block**, so it was never type-checked while commented. Fixing `=>` to
`->` and uncommenting the block builds `LibLifeCycleHost` with **0 errors** and the `IConnectorGrain`
interface fully wired. No constraint drop, no API cascade, no FS0909 workaround needed.

**Fix:** in the follow-up production port, uncomment the block and change `=>` to `->` on that one line.
Trivial. (Production left pristine by this spike per throwaway discipline.)

**Lesson:** a compiler error reported from inside a large `(* ... *)` block is not evidence about the
block's real diagnostics -- the block was never compiled. Uncomment + build to get ground truth before
theorizing a language-level cause.

## Per-shape result table

| Shape | Construct | Phase A (object expr) | Phase B (named class) |
|---|---|---|---|
| grain-observer round-trip (5 pings) | F# object expression `{ new IPingObserver<_> with ... }` in `backgroundTask` CE, via `CreateObjectReference` | **FAIL** -- `.g.cs` won't compile (`observer@33`, CS1646/CS1026/CS0426) | **PASS** -- named `PingObserver` class; 2-silo cluster; observer fires 5x, returns last seq |
| Codegen `.g.cs` compile | interface invokers | n/a (blocked by impl line) | **PASS** -- `typeof(global::S15C_Grains.PingObserver)` valid |
| ConnectorGrain interface | constraint-bearing generic interface via late `interface ... with` | n/a | **PASS (green)** -- no FS0909; blocker was a `=>` typo |

Host build green, run exit 0: `SPIKE PASS: 1 / FAIL: 0`.

## Decision

- **Re-enable both `GenerateCodeForDeclaringAssembly` attributes** in production. The blocker is
  removable by **lifting the two production grain-observer object expressions to named top-level F#
  classes**:
  - `GrainConnector.fs:117` -- `{ new ILifeEventAwaiter<'Subject,'LifeEvent,'SubjectId> with ... }`
    -> a named class capturing `taskCompletionSource` (+ the awaiter-alive workaround).
  - `Web/RealTime.fs:103` -- `{ new ISubjectGrainObserver<'Subject,'SubjectId> with ... }` -> a named
    class capturing `maybeCurrentVersion` + `observer`.
  This is contained, behavior-preserving, and does not touch the `backgroundTask { }` CEs themselves
  (their state-machine closures implement `IAsyncStateMachine`, not a grain interface, so they are not
  flagged). Grep production for `{ new I.*Observer` / `{ new I.*Awaiter` to find any others.
- **Uncomment `ConnectorGrain.fs`'s interface block and fix the `=>` -> `->` typo.** No FS0909
  workaround, no constraint drop, no LibLifeCycle API cascade.

Both are follow-up **production-port** edits (not this throwaway spike).

## Open questions / surprises

- The generated `.g.cs` still registers the named observer class in `InterfaceImplementations` even
  though it is a client-side observer, not a hosted grain. Empirically benign (round-trip PASSES), but
  it means every named grain-observer impl in a scanned assembly gets a (harmless) manifest entry.
- Process win: mirroring production's F# constructs in the spike (the S15b-production-port lesson now
  baked into the spike-driven skill) surfaced the closure bug in ~5 minutes of Codegen build, exactly
  as the skill predicted. The FS0909 misdiagnosis also validates the skill's "uncomment + build for
  ground truth" instinct.

## S15c-production-port -- LANDED

Applied all three decisions to production and re-enabled codegen:

- `GrainConnector.fs` -- the `ILifeEventAwaiter` object expression is now a named top-level
  `LifeEventAwaiter<..>` class.
- `Web/RealTime.fs` -- the `ISubjectGrainObserver` object expression is now a named top-level
  `SubjectGrainObserver<..>` class.
- `ConnectorGrain.fs` -- interface block uncommented, `=>` -> `->` typo fixed; `IConnectorGrain` wired.
- `LibLifeCycleCodeGenHost/CodegenAssemblyInfo.cs` -- both `GenerateCodeForDeclaringAssembly` attributes
  re-enabled.
- `LibLifeCycleHost/src/AssemblyInfo.fs` -- added `[<assembly: InternalsVisibleTo("LibLifeCycleCodeGenHost")>]`
  (the generated code registers the named `SubjectGrainObserver` in the internal `Web.RealTime` module).

Result: `LibLifeCycleCodeGenHost` builds green with valid invokers (`typeof(global::LibLifeCycleCore.LifeEventAwaiter<..>)`,
`typeof(global::LibLifeCycleHost.Web.RealTime.SubjectGrainObserver<..>)`, `ConnectorGrain<,>` registered).
The 93-test `LibLifeCycleTest` unit suite passes.

Booting the real silo in the simulation harness then surfaced two further Orleans-10 runtime blockers
that the codegen gate had been hiding:

1. **Autofac keyed services** -- `Autofac.Extensions.DependencyInjection` 6.0.0 predates .NET 8 keyed
   services (which Orleans 10 registers), crashing silo boot with `RegisterInstance(null) ->
   ArgumentNullException`. Fixed by bumping to ADI 10.0.0 + Autofac 8.3.0 across the test projects
   (autofac/Autofac.Extensions.DependencyInjection #112/#116). `TestCluster.fs` needed an explicit
   `Register<T>` type arg (Autofac 8 added an overload).
2. **F# wrapper serialization** -- Orleans 10 decomposes `Option`/`ValueOption`/`Choice`/`Result`
   natively and needs bare-leaf codecs. Split out as spike **S15d**
   ([s15d-fsharp-wrapper-codecs.md](./s15d-fsharp-wrapper-codecs.md)).

## Next work item

- **S15d-production-port -- LANDED (session 45).** See the S15d catalog's "Production-port outcome"
  section. `Serializer.fs` reworked to bare leaves (TypeIds 84-103 + view 4/5); both validation guards
  made decomposition-aware; plus a real bug-fix -- `SiloBuilder.ConfigureSiloClientForEcosystem`'s
  TestCluster branch now calls `configureSiloClientSerializers` (Orleans 3.x lazy codec resolution had
  masked the missing client codec for years; Orleans 10's eager `AnalyzeSerializerAvailability` makes
  it fatal at `ClusterClient..ctor`). **LibLifeCycleTest 93/93 + SuiteTodo/Ecosystem 5/5 green.**
  SuiteJobs tests runnable for the first time under .NET 10 (stale Test SDK 16.8.3 -> 17.12.0); 18/47
  PASS, 29 FAIL with `Stasis not reached` -- separate follow-up, NOT a regression.
- **S1 (PG18 baseline)** -- unblocked.
- **File upstream** dotnet/orleans issue: source generator should skip compiler-generated types when
  collecting `InvokableInterfaceImplementations` (one-line `!symbol.IsCompilerGenerated()` guard at
  `CodeGenerator.cs:224`). Attach `Meta/s15c-closure-codegen/` as the minimal repro.
