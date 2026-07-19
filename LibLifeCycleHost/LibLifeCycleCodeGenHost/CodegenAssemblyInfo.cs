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
//
// S15c: the two production grain-observer object expressions (GrainConnector.fs's ILifeEventAwaiter
// and Web/RealTime.fs's ISubjectGrainObserver) were lifted to named top-level F# classes
// (LibLifeCycleCore.LifeEventAwaiter, LibLifeCycleHost.Web.RealTime.SubjectGrainObserver). The Orleans
// 10 source generator flags every type implementing an IGrainObserver-derived interface as an
// InterfaceImplementation and emits `typeof(...)` for it; a named class emits a valid C# name whereas
// an object-expression closure (`awaiter@117`) emits a broken `@`-bearing name. See
// modernization/spikes/s15c-closure-codegen.md for the root cause and the 2-silo round-trip proof.

// One attribute per F# assembly that holds grain interfaces or impls -- the generator scans only the
// DECLARING assembly of the given type, with no transitive scan (dotnet/orleans #8520):
[assembly: GenerateCodeForDeclaringAssembly(typeof(IBlobRepoGrain))]        // LibLifeCycleCore
[assembly: GenerateCodeForDeclaringAssembly(typeof(SubjectBlobRepoGrain))]  // LibLifeCycleHost

