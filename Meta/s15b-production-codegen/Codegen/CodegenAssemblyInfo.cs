using Orleans;
using S15BPRODUCTIONCODEGEN;
using S15BPRODUCTIONCODEGEN_Grains;

// Tell the Orleans C# source generator to scan:
// (1) the F# Types assembly (declares the grain interfaces IBlobSpikeGrain, IViewSpikeGrain<,>) --
//     without this, the generator sees the interfaces only as opaque references and emits no invokers.
// (2) the F# Grains assembly (declares the grain classes BlobSpikeGrain, ViewSpikeGrain) --
//     without this, the generator cannot emit grain class metadata + activators.
// Each GenerateCodeForDeclaringAssembly attribute scans the DECLARING assembly of the given type.
// Upstream: dotnet/orleans issue #8520.
[assembly: GenerateCodeForDeclaringAssembly(typeof(IBlobSpikeGrain))]
[assembly: GenerateCodeForDeclaringAssembly(typeof(BlobSpikeGrain))]
