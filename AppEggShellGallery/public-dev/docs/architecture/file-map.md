# Key File Map

Where each concern lives in the source tree.

| Concern | Entry points |
|---------|--------------|
| Lifecycle core | `LibLifeCycle/src/LifeCycle.fs`, `LibLifeCycleTypes/src/SubjectTypes.fs`, builders in `LibLifeCycle/src/*Builder.fs` |
| Views / TimeSeries / Connectors / Ecosystem | `LibLifeCycle/src/{View,TimeSeries,Services,Ecosystem,DefaultServices}.fs` |
| Orleans grains | `LibLifeCycleHost/src/SubjectGrain.fs`, `SubjectGrainModel.fs`, `LibLifeCycleCore/src/GrainClientInterface.fs` |
| Silo / hosting | `LibLifeCycleHost/src/OrleansEx/SiloBuilder.fs`, `HostExtensions.fs`, `Host/{K8S,Fabric,Development}` |
| SQL storage | `LibLifeCycleHost/src/Storage/SqlServer/*` |
| Codecs | `LibLangFsharp/src/CodecLib.fs`, `LibCodecGen/src/CodecGen.Common.fs`, `LibCodecValidation/*`, `validate-codec.sh` |
| HTTP / realtime | `LibLifeCycleHost/src/Web/Api/V1/{GenericHttpHandler,JsonEncoding}.fs`, `Web/RealTime.fs`, `LibClient/src/Services/{HttpService,EntityService}` |
| Tests | `LibLifeCycleTest/{SimulationBuilder,ClockSimulation,TestCluster,Ecosystem,SideEffectTracking}.fs` |
| Frontend core | `LibClient/src/ChaldalReact.fs`, `ReactXP/*`, `LibRouter/src/*` |
| Toolchain | `Meta/AppEggshellCli`, `Meta/AppRenderDslCompiler`, `Meta/Lib{Eggshell,Scaffolding,RtCompilerFileSystemBindings,FablePlus}` |

## External context

- Orleans: [3.x → 7.0 migration guide](https://learn.microsoft.com/en-us/dotnet/orleans/migration-guide) ·
  [What's new in Orleans 7](https://devblogs.microsoft.com/dotnet/whats-new-in-orleans-7/)
- ReactXP: [archived on GitHub](https://github.com/microsoft/reactxp) ·
  [React Native for Web compatibility](https://necolas.github.io/react-native-web/docs/react-native-compatibility/) ·
  [ReactXP FAQ (RN-Web positioning)](https://microsoft.github.io/reactxp/docs/faq.html)
- Fable: [Fable 5 release](https://fable.io/blog/2026/2026-02-27-Fable_5_release_candidate.html) ·
  [releases](https://github.com/fable-compiler/Fable/releases) ·
  [.NET compatibility](https://fable.io/docs/dotnet/compatibility.html)
- F# / .NET: [What's new in F# 10](https://learn.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-10) ·
  [Performance improvements in .NET 10](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/)
