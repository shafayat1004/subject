#!/usr/bin/env bash
# new-spike.sh — scaffold a throwaway spike project under Meta/<spike-name>/.
# Usage: new-spike.sh <spike-name>
# Example: new-spike.sh s16-result-codec
#
# Part of the spike-driven skill. Creates the 3-project layout (Types / Codegen / Host).
# Drop subdirectories you don't need; do NOT share obj/ across sibling projects (MSBuild MSB3540).
#
# The .fsproj/.csproj files use:
#   <PackageReference Update="FSharp.Core" VersionOverride="10.0.103" />
# because Directory.Build.props pins FSharp.Core 9.0.201 and Orleans packages demand >= 10.0.103.
# VersionOverride (not Version) is translated by Directory.Build.targets into a Version that wins.

set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <spike-name>" >&2
    echo "Example: $0 s16-result-codec" >&2
    exit 2
fi

name="$1"
root="Meta/$name"

if [[ -d "$root" ]]; then
    echo "error: $root already exists" >&2
    exit 1
fi

# Project names (PascalCase, capitalize first char of spike name).
types_pname="$(printf '%s' "$name" | cut -c1 | tr '[:lower:]' '[:upper:]')$(printf '%s' "$name" | cut -c2-)Types"
codegen_pname="$(printf '%s' "$name" | cut -c1 | tr '[:lower:]' '[:upper:]')$(printf '%s' "$name" | cut -c2-)Codegen"
host_pname="$(printf '%s' "$name" | cut -c1 | tr '[:lower:]' '[:upper:]')$(printf '%s' "$name" | cut -c2-)"
ns="$(printf '%s' "$name" | tr '[:lower:]' '[:upper:]' | tr -d '-')"

mkdir -p "$root/Types" "$root/Codegen" "$root/Host"

# Types/<Types>.fsproj
cat > "$root/Types/${types_pname}.fsproj" <<FSPROJ
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <LangVersion>8.0</LangVersion>
        <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
        <Configurations>Debug;Release</Configurations>
        <EggShellFmtSeverity>none</EggShellFmtSeverity>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Update="FSharp.Core" VersionOverride="10.0.103" />
        <PackageReference Include="Microsoft.Orleans.Core" Version="10.2.1" />
        <PackageReference Include="Microsoft.Orleans.Serialization.FSharp" Version="10.2.1" />
    </ItemGroup>

    <ItemGroup>
        <Compile Include="Shapes.fs" />
    </ItemGroup>

</Project>
FSPROJ

# Types/Shapes.fs (placeholder F# types)
cat > "$root/Types/Shapes.fs" <<FSHARP
namespace ${ns}

open System
open Orleans

// TODO: define representative F# types with [<GenerateSerializer>] + [<Id(n)>] per field/case.
// Mirror the real subject shapes (DUs with nullary, single-field, multi-field cases; records;
// Option; nested collections) so the spike result extrapolates to production.
// Note: F# nullary union cases (e.g. | Foo) emit private nested classes the C# source generator
// cannot access (CS0122). Workaround: | Foo of unit. Cite dotnet/orleans issue #8717.
FSHARP

# Codegen/<Codegen>.csproj
cat > "$root/Codegen/${codegen_pname}.csproj" <<CSPROJ
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <LangVersion>13.0</LangVersion>
        <Nullable>enable</Nullable>
        <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
        <Configurations>Debug;Release</Configurations>
        <EggShellFmtSeverity>none</EggShellFmtSeverity>
        <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
        <CompilerGeneratedFilesOutputPath>\$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="../Types/${types_pname}.fsproj" />
        <PackageReference Include="Microsoft.Orleans.Sdk" Version="10.2.1" />
        <PackageReference Include="Microsoft.Orleans.Core.Abstractions" Version="10.2.1" />
        <PackageReference Include="FSharp.Core" VersionOverride="10.0.103" />
    </ItemGroup>

</Project>
CSPROJ

# Codegen/CodegenAssemblyInfo.cs
cat > "$root/Codegen/CodegenAssemblyInfo.cs" <<CS
using Orleans;

// Tell the Orleans C# source generator to scan the referenced F# assembly
// for [GenerateSerializer] types and emit codecs/serializers into THIS project.
// Without this, the generator only processes types in the current C# compilation.
// Replace ${ns}.SomeType with a real type from YOUR Shapes.fs.
[assembly: GenerateCodeForDeclaringAssembly(typeof(${ns}.SomeType))]
CS

# Host/<Host>.fsproj
cat > "$root/Host/${host_pname}.fsproj" <<FSPROJ
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <LangVersion>8.0</LangVersion>
        <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
        <Configurations>Debug;Release</Configurations>
        <EggShellFmtSeverity>none</EggShellFmtSeverity>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Update="FSharp.Core" VersionOverride="10.0.103" />
        <PackageReference Include="Microsoft.Orleans.Server" Version="10.2.1" />
        <PackageReference Include="Microsoft.Orleans.Serialization.FSharp" Version="10.2.1" />
        <PackageReference Include="Microsoft.Orleans.TestingHost" Version="10.2.1" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="../Types/${types_pname}.fsproj" />
        <ProjectReference Include="../Codegen/${codegen_pname}.csproj" />
    </ItemGroup>

    <ItemGroup>
        <Compile Include="Program.fs" />
    </ItemGroup>

</Project>
FSPROJ

# Host/Program.fs (placeholder)
cat > "$root/Host/Program.fs" <<FSHARP
namespace ${ns}_Host

open System
open System.Threading.Tasks
open Orleans
open Orleans.Hosting
open Orleans.TestingHost
open Orleans.Serialization.Configuration
open Microsoft.Extensions.Configuration

module Program =

    type SiloConfigurator() =
        interface ISiloConfigurator with
            member _.Configure(siloBuilder: ISiloBuilder) =
                siloBuilder.Configure<TypeManifestOptions>(fun (options: TypeManifestOptions) ->
                    options.AllowAllTypes <- true)
                |> ignore

    type ClientConfigurator() =
        interface IClientBuilderConfigurator with
            member _.Configure(_configuration: IConfiguration, clientBuilder: IClientBuilder) =
                clientBuilder.Configure<TypeManifestOptions>(fun (options: TypeManifestOptions) ->
                    options.AllowAllTypes <- true)
                |> ignore

    [<EntryPoint>]
    let main _argv =
        printfn "SPIKE: <one-line spike goal>"
        printfn "Booting 2-silo in-process test cluster..."

        let builder = TestClusterBuilder(2s)
        builder.AddSiloBuilderConfigurator<SiloConfigurator>() |> ignore
        builder.AddClientBuilderConfigurator<ClientConfigurator>() |> ignore
        use cluster = builder.Build()

        cluster.DeployAsync().Wait()
        printfn "Cluster deployed."

        // TODO: call grain methods, assert round-trip, capture per-shape PASS/FAIL.

        printfn "SPIKE PASS/FAIL: <result>"
        0
FSHARP

# README
cat > "$root/README.md" <<README
# $name (spike, throwaway)

Per spike-driven skill. Catalog doc:
\`AppEggShellGallery/public-dev/docs/modernization/spikes/$name.md\`.

## Layout

- \`Types/\` — F# types with \`[<GenerateSerializer]\` + \`[<Id(n)>]\`.
- \`Codegen/\` — C# helper project that triggers the Orleans source generator on the F# types.
- \`Host/\` — F# console that boots a 2-silo \`TestCluster\` and asserts round-trip.

## Build

\`\`\`sh
dotnet build $root/Host/${host_pname}.fsproj -c Debug
dotnet $root/Host/bin/Debug/net10.0/${host_pname}.dll
\`\`\`
README

echo "scaffolded $root"
echo "NEXT: edit Codegen/CodegenAssemblyInfo.cs to point at a real type from Shapes.fs"
echo "NEXT: run scripts/upstream-research.sh BEFORE writing the .fs/.cs bodies"
