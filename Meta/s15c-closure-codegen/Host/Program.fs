namespace S15C_Host

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
open S15C
open S15C_Grains

// Wire the C# codegen assembly + the F# Grains assembly into the F# host runtime.
// Upstream: dotnet/orleans issue #8235.
[<assembly: Orleans.ApplicationPartAttribute("s15c-closure-codegenCodegen")>]
[<assembly: Orleans.ApplicationPartAttribute("s15c-closure-codegenGrains")>]
do ()

// === Custom IGeneralizedCodec for the PingPayload record (mirrors the production Fleece-based
// wire serializer). System.TextJson handles F# records natively (parameterized ctor + matching
// property names), so no custom JsonConverter is needed for this spike's single record type. ===
module private Registry =
    let supported : Type list = [ typeof<PingPayload> ]
    let isSupported (t: Type) : bool = supported |> List.contains t

type PingCodec() =
    let jsonOpts = System.Text.Json.JsonSerializerOptions()

    interface IFieldCodec with
        member _.WriteField<'W when 'W :> System.Buffers.IBufferWriter<byte>>
            (writer: byref<Writer<'W>>, fieldIdDelta: uint32, expectedType: Type, value: obj) : unit =
            if isNull value then () else
                let runtimeType = value.GetType()
                writer.WriteFieldHeader(fieldIdDelta, expectedType, runtimeType, WireType.LengthPrefixed)
                let json = System.Text.Json.JsonSerializer.Serialize(value, runtimeType, jsonOpts)
                let bytes = System.Text.Encoding.UTF8.GetBytes(json)
                writer.WriteVarUInt32(uint32 bytes.Length)
                writer.Write(System.ReadOnlySpan<byte>(bytes))

        member _.ReadValue<'R>(reader: byref<Reader<'R>>, field: Field) : obj =
            let _ = field
            let jsonLength = reader.ReadVarUInt32() |> int
            let jsonBytes : byte[] = Array.zeroCreate jsonLength
            reader.ReadBytes(System.Span<byte>(jsonBytes)) |> ignore
            let json = System.Text.Encoding.UTF8.GetString(jsonBytes)
            System.Text.Json.JsonSerializer.Deserialize(json, typeof<PingPayload>, jsonOpts)

    interface IGeneralizedCodec with
        member _.IsSupportedType(t: Type) : bool = Registry.isSupported t

    interface IGeneralizedCopier with
        member _.IsSupportedType(t: Type) : bool = Registry.isSupported t

    interface IDeepCopier with
        member _.DeepCopy(input: obj, _context: CopyContext) : obj = input

module private Registration =
    let register (services: IServiceCollection) : unit =
        services.AddSingleton<PingCodec>() |> ignore
        services.AddSingleton<IGeneralizedCodec>(fun sp -> sp.GetRequiredService<PingCodec>() :> IGeneralizedCodec) |> ignore
        services.AddSingleton<IGeneralizedCopier>(fun sp -> sp.GetRequiredService<PingCodec>() :> IGeneralizedCopier) |> ignore

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

        // === Shape: grain-observer round-trip via an F# object-expression observer inside a
        // backgroundTask CE (mirrors GrainConnector.RunAndWait). Exercises CreateObjectReference +
        // the source-gen invoker path -- the exact path S15b's spike never tested. ===
        try
            let grain = cluster.GrainFactory.GetGrain<IPingGrain>(Guid.NewGuid())
            let count = 5
            let lastSeq = (Subscriber.subscribeViaNamedClass cluster.GrainFactory grain count).Result
            check "grain-observer round-trip (named-class observer, 5 pings)" (lastSeq = count)
        with ex ->
            check "grain-observer round-trip (F# object-expression observer)" false
            printfn "    exception: %A" ex

        printfn "SPIKE PASS: %d / FAIL: %d" passCount failCount
        if failCount = 0 then 0 else 1

    [<EntryPoint>]
    let main _argv =
        printfn "SPIKE S15c: F# object-expression grain-observer closure through Orleans 10 source generator"
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
