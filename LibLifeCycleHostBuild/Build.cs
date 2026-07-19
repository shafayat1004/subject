// TODO S15c: Orleans 10 dropped KnownAssembly + Orleans.CodeGeneration namespace. Codegen now
// flows through GenerateCodeForDeclaringAssembly in LibLifeCycleCodeGenHost (currently disabled
// due to the F# closure misclassification HARD BLOCKER -- see
// LibLifeCycleHost/LibLifeCycleCodeGenHost/CodegenAssemblyInfo.cs for details). Once S15c
// re-enables source-gen, this Build.cs may be obsolete or may need to reference the CodegenHost
// project for transitive loading. For now, no codegen attributes here.
//
// using Orleans.CodeGeneration;
// [assembly: KnownAssembly(typeof(LibLifeCycleHost.Config.AnchorTypeForProject))]
// [assembly: KnownAssembly(typeof(LibLifeCycleCore.Anchor.AnchorTypeForProject))]
// [assembly: KnownAssembly(typeof(LibLifeCycleTypes.SubjectTypes.SubjectReference))]

namespace LibLifeCycleHostBuild
{
    public static class Build
    {
        // NO-OP, just for passing around assembly reference
    }
}