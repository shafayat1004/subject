using Orleans;
using LibLifeCycleCore;
using LibLifeCycleHost;

// The Orleans 10 C# source generator only runs in C# projects. This project carries the attributes that
// tell the generator to scan the F# assemblies that contain grain interfaces (LibLifeCycleCore) and
// grain implementations (LibLifeCycleHost) so that invokers, proxies and activators are emitted.
//
// GenerateCodeForDeclaringAssembly scans the DECLARING assembly of the type given to it, so any public
// type from each F# assembly is sufficient.
//
// LibLifeCycleCore exposes its internal nullary-case DU helpers to this generator via
// [<assembly: InternalsVisibleTo("LibLifeCycleCodeGenHost")>].

// TODO S15c (HARD BLOCKER): both GenerateCodeForDeclaringAssembly attributes are DISABLED.
// The Orleans 10 source generator misclassifies F# compiler-generated closures as grain impls
// (InterfaceImplementations), emitting broken C# referencing F# mangled names like
// `GrainConnector.RunAndWait@117-1<...>` and `Web.RealTime.grainObserver@103<...>` (the `@` and
// `$` chars aren't valid C# identifiers). The generator's logic (CodeGenerator.cs:248-259)
// correctly identifies these closures as implementing grain-observer interfaces (because they DO
// implement ISubjectGrainObserver via F# object expressions), but its C# emission can't handle
// the F# mangled names. The S15b spike didn't surface this because its grain impls were trivial
// (no Task CEs, no object expressions near grain interfaces). File an upstream issue with the
// production Web/RealTime.fs:103 + GrainConnector.fs:117 closures as repro. Workaround options
// for S15c: (a) rewrite the F# closures as explicit classes-based grain impls (massive refactor),
// (b) move grain observer interfaces to a separate assembly with no F# closures, (c) wait for
// upstream fix. Until S15c resolves this, NO source-gen invokers exist -- runtime grain dispatch
// will fail. S1 (PG18 baseline) will catch it immediately. The custom EggShellSubjectGrainsCodec
// (Serializer.fs) handles serialization; only the invoker (method-dispatch) side is missing.
//
// [assembly: GenerateCodeForDeclaringAssembly(typeof(IBlobRepoGrain))]
// [assembly: GenerateCodeForDeclaringAssembly(typeof(SubjectBlobRepoGrain))]

