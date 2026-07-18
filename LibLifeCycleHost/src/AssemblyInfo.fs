namespace LibLifeCycleHost.AssemblyInfo

open Orleans
open System.Runtime.CompilerServices

[<assembly: Orleans.ApplicationPartAttribute("LibLifeCycleCodeGenHost")>]
[<assembly: Orleans.ApplicationPartAttribute("LibLifeCycleHost")>]
// S15c: the Orleans source generator registers the named SubjectGrainObserver (in the internal
// Web.RealTime module) in InterfaceImplementations, so the generated code in LibLifeCycleCodeGenHost
// must be able to see internals of this assembly (mirrors LibLifeCycleCore's AssemblyInfo).
[<assembly: InternalsVisibleTo("LibLifeCycleCodeGenHost")>]
do ()
