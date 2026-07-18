namespace S15D_Host

open System
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Orleans
open Orleans.Hosting
open Orleans.Serialization.Cloning
open Orleans.Serialization.Codecs
open Orleans.Serialization.Buffers
open Orleans.Serialization.Configuration
open Orleans.Serialization.Serializers
open Orleans.Serialization.WireProtocol
open Orleans.TestingHost
open S15D
open S15D_Grains

// Wire the C# codegen assembly + the F# Grains assembly into the F# host runtime (dotnet/orleans #8235).
[<assembly: Orleans.ApplicationPartAttribute("s15d-fsharp-wrapper-codecsCodegen")>]
[<assembly: Orleans.ApplicationPartAttribute("s15d-fsharp-wrapper-codecsGrains")>]
do ()

// === Custom IGeneralizedCodec.
//
// THE FIX (Phase B): claim the bare LEAF types (PingPayload, PingError). Orleans 10 resolves
// Option<PingPayload> via FSharpOptionCodec and Result<PingPayload, PingError> via FSharpResultCodec;
// each decomposes the wrapper and asks for the inner leaf's codec -- which is this one.
//
// Phase A (REPRO -- see README): claim the WRAPPERS instead
//     [ typeof<Option<PingPayload>>; typeof<Result<PingPayload, PingError>> ]
// and the silo startup validator throws
//     Orleans.Serialization.CodecNotFoundException : Could not find a codec for type S15D.PingPayload
// because Orleans never asks our codec for the wrapper (its own FSharp codec owns it) -- only for the
// inner leaf, which we then failed to register.
module private Registry =
    let supported : Type list = [ typeof<PingPayload>; typeof<PingError> ]
    let isSupported (t: Type) : bool = supported |> List.contains t
    let byName = supported |> List.map (fun t -> t.FullName, t) |> dict

type LeafCodec() =
    let jsonOpts = System.Text.Json.JsonSerializerOptions()

    interface IFieldCodec with
        member _.WriteField<'W when 'W :> System.Buffers.IBufferWriter<byte>>
            (writer: byref<Writer<'W>>, fieldIdDelta: uint32, expectedType: Type, value: obj) : unit =
            if isNull value then () else
                let runtimeType = value.GetType()
                writer.WriteFieldHeader(fieldIdDelta, expectedType, runtimeType, WireType.LengthPrefixed)
                // Self-describing payload: "<FullName>\n<json>" so ReadValue can resolve the leaf type.
                let json = System.Text.Json.JsonSerializer.Serialize(value, runtimeType, jsonOpts)
                let framed = runtimeType.FullName + "\n" + json
                let bytes = System.Text.Encoding.UTF8.GetBytes(framed)
                writer.WriteVarUInt32(uint32 bytes.Length)
                writer.Write(System.ReadOnlySpan<byte>(bytes))

        member _.ReadValue<'R>(reader: byref<Reader<'R>>, field: Field) : obj =
            let _ = field
            let len = reader.ReadVarUInt32() |> int
            let bytes : byte[] = Array.zeroCreate len
            reader.ReadBytes(System.Span<byte>(bytes)) |> ignore
            let framed = System.Text.Encoding.UTF8.GetString(bytes)
            let sep = framed.IndexOf('\n')
            let typeName = framed.Substring(0, sep)
            let json = framed.Substring(sep + 1)
            System.Text.Json.JsonSerializer.Deserialize(json, Registry.byName.[typeName], jsonOpts)

    interface IGeneralizedCodec with
        member _.IsSupportedType(t: Type) : bool = Registry.isSupported t

    interface IGeneralizedCopier with
        member _.IsSupportedType(t: Type) : bool = Registry.isSupported t

    interface IDeepCopier with
        member _.DeepCopy(input: obj, _context: CopyContext) : obj = input

module private Registration =
    let register (services: IServiceCollection) : unit =
        services.AddSingleton<LeafCodec>() |> ignore
        services.AddSingleton<IGeneralizedCodec>(fun sp -> sp.GetRequiredService<LeafCodec>() :> IGeneralizedCodec) |> ignore
        services.AddSingleton<IGeneralizedCopier>(fun sp -> sp.GetRequiredService<LeafCodec>() :> IGeneralizedCopier) |> ignore

module Program =

    type SiloConfigurator() =
        interface ISiloConfigurator with
            member _.Configure(siloBuilder: ISiloBuilder) =
                siloBuilder.Configure<TypeManifestOptions>(fun (o: TypeManifestOptions) -> o.AllowAllTypes <- true) |> ignore
                siloBuilder.Services |> Registration.register

    type ClientConfigurator() =
        interface IClientBuilderConfigurator with
            member _.Configure(_configuration: IConfiguration, clientBuilder: IClientBuilder) =
                clientBuilder.Configure<TypeManifestOptions>(fun (o: TypeManifestOptions) -> o.AllowAllTypes <- true) |> ignore
                clientBuilder.Services |> Registration.register

    let runSpike (cluster: TestCluster) : int =
        let mutable passCount = 0
        let mutable failCount = 0
        let check (label: string) (ok: bool) =
            if ok then passCount <- passCount + 1; printfn "  PASS: %s" label
            else failCount <- failCount + 1; printfn "  FAIL: %s" label

        let grain = cluster.GrainFactory.GetGrain<IPingGrain>(Guid.NewGuid())

        // Option<PingPayload>: Orleans decomposes Option, our codec supplies bare PingPayload.
        try
            let maybe = grain.GetMaybe().Result
            check "Option<PingPayload> round-trips" (maybe = Some { Seq = 7; Message = "hello" })
        with ex ->
            check "Option<PingPayload> round-trips" false
            printfn "    exception: %A" ex

        // Result Ok<PingPayload>: Orleans decomposes Result, our codec supplies bare PingPayload.
        try
            let okRes = grain.TryPing(1).Result
            check "Result Ok<PingPayload> round-trips" (match okRes with | Ok p -> p.Seq = 1 | Error _ -> false)
        with ex ->
            check "Result Ok<PingPayload> round-trips" false
            printfn "    exception: %A" ex

        // Result Error<PingError>: Orleans decomposes Result, our codec supplies bare PingError.
        try
            let errRes = grain.TryPing(-1).Result
            check "Result Error<PingError> round-trips" (match errRes with | Error e -> e.Code = 400 | Ok _ -> false)
        with ex ->
            check "Result Error<PingError> round-trips" false
            printfn "    exception: %A" ex

        printfn "SPIKE PASS: %d / FAIL: %d" passCount failCount
        if failCount = 0 then 0 else 1

    [<EntryPoint>]
    let main _argv =
        printfn "SPIKE S15d: F# Option/Result wrapper decomposition by Orleans 10 (bare-leaf codec required)"
        printfn "Booting 2-silo in-process test cluster..."
        let builder = TestClusterBuilder(2s)
        builder.AddSiloBuilderConfigurator<SiloConfigurator>() |> ignore
        builder.AddClientBuilderConfigurator<ClientConfigurator>() |> ignore
        use cluster = builder.Build()
        cluster.DeployAsync().Wait()
        printfn "Cluster deployed."
        let rc = runSpike cluster
        cluster.StopAllSilosAsync().Wait()
        rc
