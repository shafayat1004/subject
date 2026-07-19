using Orleans;
using S15D;
using S15D_Grains;

// Scan the F# Types assembly (grain interface IPingGrain) and the F# Grains assembly (PingGrain impl).
// Each GenerateCodeForDeclaringAssembly scans only the DECLARING assembly of the given type.
// Upstream: dotnet/orleans issue #8520.
[assembly: GenerateCodeForDeclaringAssembly(typeof(IPingGrain))]
[assembly: GenerateCodeForDeclaringAssembly(typeof(PingGrain))]
