# Tracked upstream Orleans issues (affecting Goal G)

Tracked here so we notice when upstream resolves them. A resolution may let us simplify the
codebase (drop a workaround, delete a spike-only helper, relax a constraint). Each entry
records: status, our affected files, the workaround in our code, and the simplification path
if the upstream issue is resolved. Checked against `dotnet/orleans` on the date noted.

The master plan: [SQL Server to Postgres](./sql-server-to-postgres.md). Spike catalogs in
[spikes/](./spikes/). Engineering log: [knowledge-base/engineering-log.md](../knowledge-base/engineering-log.md).

| # | Title | State | Affects | Our workaround | Simplification if resolved | Last checked |
|---|---|---|---|---|---|---|
| [#8520](https://github.com/dotnet/orleans/issues/8520) | `GenerateCodeForDeclaringAssembly` is critical and undocumented | OPEN | `LibLifeCycleCodeGenHost.csproj` (to be added in S15b-production-port) | Carry TWO `[assembly: GenerateCodeForDeclaringAssembly(...)]` attributes (one for F# Types, one for F# Grains) — empirically confirmed by S15b (23-line stub with one attribute → 666-line full codegen with two). | If upstream adds transitive scan (scan referenced assemblies of the given type's assembly), we could drop the second attribute. If upstream documents the multi-assembly case, we could drop the comment explaining it. | 2026-07-18 |
| [#8717](https://github.com/dotnet/orleans/issues/8717) | F# discriminated unions, where some cases have no associated values, fails to compile with error CS0122 | OPEN | `LibLifeCycleCore` F# types with nullary cases (`TodoAction.ToggleDone`, `TodoAction.Archive`, `TodoOpError.EmptyTitle`, etc.) | `[<assembly: InternalsVisibleTo("LibLifeCycleCodeGenHost")>]` on `LibLifeCycleCore` (confirmed by gfix's reply on #8717 + S15b spike). NO caller cascade tax — keeps `TodoAction.ToggleDone` (not `TodoAction.ToggleDone ()`). | If upstream fixes the source generator to handle nullary cases without the InternalsVisibleTo (e.g., by emitting `new()` calls instead of referencing the private nested classes), we can drop the InternalsVisibleTo attribute entirely. | 2026-07-18 |
| [#8235](https://github.com/dotnet/orleans/issues/8235) | F# host: Could not find an implementation for interface ... — C# host ok | OPEN | F# Host (`SuiteTodo/Launchers/Dev/DevelopmentHost/src/DevelopmentHost.fs`) | `[<assembly: Orleans.ApplicationPartAttribute("LibLifeCycleCodeGenHost")>]` + `[<assembly: Orleans.ApplicationPartAttribute("LibLifeCycleHost")>]` on the F# Host (fwaris's workaround, confirmed by S15b spike). | If upstream fixes F# host auto-discovery of codegen-host-generated metadata (e.g., by making the SDK auto-add ApplicationPartAttribute for referenced assemblies), we can drop both lines. | 2026-07-18 |
| [#6703](https://github.com/dotnet/orleans/issues/6703) | Remove `IExternalSerializer` interface from `OrleansJsonSerializer` | CLOSED (fixed in #7070) | `LibLifeCycleCore/src/OrleansEx/Serializer.fs` (the existing custom wire serializer) | Rewrite against `IGeneralizedCodec`+`IGeneralizedCopier`+`IFieldCodec`+`IDeepCopier` (S15b-production-port). The new pattern is mandatory; there is no path back to `IExternalSerializer`. | N/A — already resolved upstream; our migration to the new pattern is the resolution. | 2026-07-18 |
| [#10227](https://github.com/dotnet/orleans/issues/10227) | 10.2.0 tightened type allow-listing | OPEN (informational) | `TypeManifestOptions.AllowAllTypes = true` on both silo and client (`ISiloConfigurator` + `IClientBuilderConfigurator`) | Set `AllowAllTypes = true` globally (per official API docs). | If upstream relaxes the allow-listing (or auto-allows types in the same assembly as grain interfaces), we could remove the `ISiloConfigurator` + `IClientBuilderConfigurator` configurators. Low-priority — the configurators are 6 lines total. | 2026-07-18 |
| [#8255](https://github.com/dotnet/orleans/issues/8255) | F# DU serialization broken in early Orleans 7.x (fixed via PR #9095) | CLOSED (fixed) | N/A — informational; the fix shipped in `Microsoft.Orleans.Serialization.FSharp`. | None — the package is used as-is. | N/A — already resolved. The S15 spike empirically confirmed the fix works for built-in shapes (Unit/Option/ValueOption/Choice); user-defined F# records/DUs/Result are NOT covered (the package ships only the built-in codecs). | 2026-07-18 |
| **S15 finding #7 (genuinely novel — NO upstream report located)** | Per-case `[<Id(n)>]` attribute on F# union cases is accepted by the F# compiler but the emitted C# codec does NOT honor it — multi-case DUs round-trip with structural mismatch; `Set<Category>` throws "Unable to cast 'Personal' to 'Work'". | UNFILED | Affects the source-gen path — which we are NOT using (S15b bypasses it via the custom `IGeneralizedCodec`). | Bypass source-gen entirely for F# types; use the custom `IGeneralizedCodec` wrapping Fleece/STJ. The F# types carry NO `[<GenerateSerializer>]`/`[<Id(n)>]` annotations. | If upstream fixes the per-case `[Id(n)]` honoring in the C# source generator's `FSharpUtilities`, we could reconsider using source-gen for F# types — BUT this is not a simplification we want; the custom codec gives us version-tolerant wire format + control over the Fleece JSON body that source-gen cannot match. Filing the issue is still worthwhile (codemem 1891 notes this). | 2026-07-18 |

## How to use this doc

- Before each Orleans-related work item, re-check the OPEN issues via `gh issue view N --repo dotnet/orleans --comments`.
- If an issue is resolved CLOSED, evaluate the simplification path: does the workaround file's complexity drop?
  If yes, file a follow-up work item to remove the workaround (after confirming the resolution is in the
  Orleans 10.2.x line we use; we do NOT upgrade Orleans versions mid-port).
- New issues that affect us should be appended here with the same columns. Cite the codemem entry that captures
  the symptom-to-fix detail.
- When a spike catalog doc cites an issue, link the issue back from this table.

## Process

To re-check all OPEN issues in one shot:

```sh
for n in 8520 8717 8235 10227; do
  echo "=== #$n ==="
  gh issue view $n --repo dotnet/orleans --json state,title,updatedAt | jq '{state, title, updatedAt}'
done
```

(For the UNFILED S15 finding #7, file a new issue with the S15 spike as repro before re-checking here.)
