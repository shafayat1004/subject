using Orleans;
using S15C;
using S15C_Grains;

// Tell the Orleans C# source generator to scan:
// (1) the F# Types assembly (declares IPingObserver<>, IPingGrain grain interfaces).
// (2) the F# Grains assembly (declares PingGrain + the Subscriber module's object-expression
//     closure -- this is what triggers the S15c misclassification bug).
// Each GenerateCodeForDeclaringAssembly scans the DECLARING assembly of the given type only.
// Upstream: dotnet/orleans issue #8520.
[assembly: GenerateCodeForDeclaringAssembly(typeof(IPingGrain))]
[assembly: GenerateCodeForDeclaringAssembly(typeof(PingGrain))]
