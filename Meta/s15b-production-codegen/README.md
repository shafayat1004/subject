# s15b-production-codegen (spike, throwaway)

Per spike-driven skill. Catalog doc:
`AppEggShellGallery/public-dev/docs/modernization/spikes/s15b-production-codegen.md`.

## Layout

- `Types/` — F# types with `[<GenerateSerializer]` + `[<Id(n)>]`.
- `Codegen/` — C# helper project that triggers the Orleans source generator on the F# types.
- `Host/` — F# console that boots a 2-silo `TestCluster` and asserts round-trip.

## Build

```sh
dotnet build Meta/s15b-production-codegen/Host/S15b-production-codegen.fsproj -c Debug
dotnet Meta/s15b-production-codegen/Host/bin/Debug/net10.0/S15b-production-codegen.dll
```
