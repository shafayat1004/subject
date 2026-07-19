namespace S15BPRODUCTIONCODEGEN_Host

open System
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Orleans
open Orleans.Concurrency
open Orleans.Hosting
open Orleans.Serialization
open Orleans.Serialization.Cloning
open Orleans.Serialization.Codecs
open Orleans.Serialization.Buffers
open Orleans.Serialization.Configuration
open Orleans.Serialization.Serializers
open Orleans.Serialization.WireProtocol
open Orleans.TestingHost
open S15BPRODUCTIONCODEGEN
open System.Text.Json
open System.Text.Json.Serialization
open FSharp.Reflection
open Microsoft.FSharp.Core

// === Wire the C# codegen project's generated metadata + the F# Grains project into the F# host.
// Without [<assembly: Orleans.ApplicationPartAttribute("...")>], the F# host can't see the
// C# source-generated invokers for the F#-defined grain interfaces. Upstream: dotnet/orleans
// issue #8235 (F# host workaround by fwaris).
[<assembly: Orleans.ApplicationPartAttribute("S15b-production-codegenCodegen")>]
[<assembly: Orleans.ApplicationPartAttribute("S15b-production-codegenGrains")>]
do ()

// === F# grain impls live in the sibling F# Grains project (Meta/s15b-production-codegen/Grains/).
// The C# Codegen project scans that Grains assembly (via GenerateCodeForDeclaringAssembly) and
// emits grain-class + invoker metadata; this Host wires it via ApplicationPartAttribute above.

// === Minimal F#-aware JsonConverter for the spike. Handles F# unions (incl. nullary cases),
// Option<'T>, and Result<'T,'E>. Production uses Fleece+STJ (CodecLib.StjCodecs) which already
// covers all F# shapes; this hand-rolled converter is the spike's drop-in for STJ.
type private FSharpJsonConverter() =
    inherit JsonConverter<obj>()

    static member CanConvertFor (t: Type) : bool =
        FSharpType.IsUnion(t)

    override _.CanConvert(t: Type) : bool =
        FSharpJsonConverter.CanConvertFor t

    override _.Write(writer: Utf8JsonWriter, value: obj, options: JsonSerializerOptions) : unit =
        if isNull value then writer.WriteNullValue() else
        let t = value.GetType()
        // Special-case FSharpOption: Some(x) -> write x directly; None -> null.
        // FSharpOption is a union with cases Some and None; the F# compiler emits it specially
        // and STJ's default handling treats null as None. Writing Some as the inner value lets
        // STJ's standard Option/Nullable handling work for nested options inside records.
        if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Option<_>> then
            // value is Some(inner); get inner via reflection
            let inner = t.GetProperty("Value").GetValue(value)
            if isNull inner then writer.WriteNullValue() else
                JsonSerializer.Serialize(writer, inner, inner.GetType(), options)
        else if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<list<_>> then
            // FSharpList: write as a JSON array (STJ can deserialize arrays, but FSharpList needs
            // special handling because it's a union [Empty; Cons(head, tail)])
            let enumerable = value :?> System.Collections.IEnumerable
            writer.WriteStartArray()
            for item in enumerable do
                if isNull item then writer.WriteNullValue() else
                    JsonSerializer.Serialize(writer, item, item.GetType(), options)
            writer.WriteEndArray()
        else
            let case, fields = FSharpValue.GetUnionFields(value, t)
            if fields.Length = 0 then
                // nullary case: write as just the case name string
                writer.WriteStringValue(case.Name)
            else if fields.Length = 1 then
                // single-payload case: write the payload directly with a wrapper tag for round-trip
                writer.WriteStartObject()
                writer.WriteString("__case", case.Name)
                writer.WritePropertyName("__value")
                JsonSerializer.Serialize(writer, fields.[0], fields.[0].GetType(), options)
                writer.WriteEndObject()
            else
                // multi-payload case: write object { "__case": name, "__fields": [...] }
                writer.WriteStartObject()
                writer.WriteString("__case", case.Name)
                writer.WriteStartArray("__fields")
                for f in fields do
                    JsonSerializer.Serialize(writer, f, f.GetType(), options)
                writer.WriteEndArray()
                writer.WriteEndObject()

    override _.Read(reader: byref<Utf8JsonReader>, t: Type, options: JsonSerializerOptions) : obj =
        // Special-case FSharpOption: null -> None; non-null -> Some(deserialized inner).
        if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Option<_>> then
            if reader.TokenType = JsonTokenType.Null then
                reader.Read() |> ignore
                null  // FSharpOption.None is represented as null in .NET
            else
                let innerType = t.GetGenericArguments().[0]
                let inner = JsonSerializer.Deserialize(&reader, innerType, options)
                let someCase = FSharpType.GetUnionCases(t) |> Array.find (fun c -> c.Name = "Some")
                FSharpValue.MakeUnion(someCase, [| inner |])
        else if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<list<_>> then
            // FSharpList: read JSON array -> FSharpList via Cons.
            if reader.TokenType <> JsonTokenType.StartArray then
                failwithf "FSharpJsonConverter.Read: expected StartArray for FSharpList, got %A" reader.TokenType
            reader.Read() |> ignore
            let elementType = t.GetGenericArguments().[0]
            let items = System.Collections.Generic.List<obj>()
            while reader.TokenType <> JsonTokenType.EndArray do
                let item = JsonSerializer.Deserialize(&reader, elementType, options)
                items.Add(item)
                // STJ convention: after Deserialize, reader is at the END of the value (e.g. at the
                // Todo's EndObject token), not past it. Advance to the next token (comma or EndArray).
                reader.Read() |> ignore
            // STJ convention: leave the reader at the EndArray token, do NOT advance past it.
            // Build FSharpList from the items: fold Cons from the end
            let consCase = FSharpType.GetUnionCases(t) |> Array.find (fun c -> c.Name = "Cons")
            let emptyCase = FSharpType.GetUnionCases(t) |> Array.find (fun c -> c.Name = "Empty")
            let rec buildList (items: obj list) =
                match items with
                | [] -> FSharpValue.MakeUnion(emptyCase, [||])
                | h :: rest ->
                    let tail = buildList rest
                    FSharpValue.MakeUnion(consCase, [| h; tail |])
            buildList (List.ofSeq items)
        else
            let cases = FSharpType.GetUnionCases(t)
            match reader.TokenType with
            | JsonTokenType.String ->
                let name = reader.GetString()
                // STJ convention: leave the reader at the END of the value (at the closing quote),
                // not past it. Do NOT call reader.Read() here -- STJ will advance after the converter
                // returns. Calling Read() here causes "read too much or not enough" errors.
                let case = cases |> Array.find (fun c -> c.Name = name)
                FSharpValue.MakeUnion(case, [||])
            | JsonTokenType.Null ->
                reader.Read() |> ignore
                null
            | JsonTokenType.StartObject ->
                reader.Read() |> ignore
                let mutable caseName = Unchecked.defaultof<string>
                let mutable valuePayload : JsonElement = Unchecked.defaultof<JsonElement>
                let mutable fieldsPayload : JsonElement[] = [||]
                let mutable isFieldsArray = false
                while reader.TokenType <> JsonTokenType.EndObject do
                    let prop = reader.GetString()
                    reader.Read() |> ignore
                    match prop with
                    | "__case" ->
                        caseName <- reader.GetString()
                        reader.Read() |> ignore
                    | "__value" ->
                        use doc = JsonDocument.ParseValue(&reader)
                        valuePayload <- doc.RootElement.Clone()
                        reader.Read() |> ignore
                    | "__fields" ->
                        isFieldsArray <- true
                        if reader.TokenType = JsonTokenType.StartArray then
                            reader.Read() |> ignore
                            let items = System.Collections.Generic.List<JsonElement>()
                            while reader.TokenType <> JsonTokenType.EndArray do
                                use doc = JsonDocument.ParseValue(&reader)
                                items.Add(doc.RootElement.Clone())
                                reader.Read() |> ignore
                            fieldsPayload <- items.ToArray()
                            reader.Read() |> ignore
                    | _ ->
                        reader.Skip()
                        reader.Read() |> ignore
                reader.Read() |> ignore  // consume EndObject
                let case = cases |> Array.find (fun c -> c.Name = caseName)
                if isFieldsArray then
                    let fieldTypes = case.GetFields()
                    let typedFields =
                        fieldsPayload
                        |> Array.mapi (fun i (p: JsonElement) ->
                            let ft = fieldTypes.[i].PropertyType
                            p.Deserialize(ft, options))
                    FSharpValue.MakeUnion(case, typedFields)
                else
                    // single-payload
                    let ft = case.GetFields().[0].PropertyType
                    let typedPayload = valuePayload.Deserialize(ft, options)
                    FSharpValue.MakeUnion(case, [| typedPayload |])
            | _ ->
                failwithf "FSharpJsonConverter.Read: unexpected token %A" reader.TokenType

// === Custom IGeneralizedCodec + IGeneralizedCopier -- emulates porting the existing
// Fleece-based wire serializer (LibLifeCycleCore/src/OrleansEx/Serializer.fs) to Orleans 10.
// Strategy: serialize F# type bodies via System.TextJson (production: Fleece+gzip), bridge into
// the Orleans 10 wire protocol via IFieldCodec's WriteField/ReadValue. Bypasses source-gen
// entirely for F# types (sidesteps S15 finding #7: source-generated F# DU codecs mis-route cases).

module private FSharpTypeRegistry =

    let supportedClosedTypes : Type list =
        [
            typeof<Priority>
            typeof<Category>
            typeof<TodoAction>
            typeof<Todo>
            typeof<TodoOpError>
            typeof<BlobData>
            typeof<Option<BlobData>>
            typeof<Option<Category>>
            typeof<Option<DateTimeOffset>>
            typeof<Result<Todo, TodoOpError>>
            typeof<Result<GetTodoOutput, TodoOpError>>
            typeof<GetTodoInput>
            typeof<GetTodoOutput>
        ]

    let supportedOpenTypeDefs : Type list =
        [
            typedefof<Option<_>>
            typedefof<Result<_, _>>
        ]

    let isSupported (t: Type) : bool =
        if supportedClosedTypes |> List.contains t then true
        else
            if t.IsGenericType then
                let def = t.GetGenericTypeDefinition()
                supportedOpenTypeDefs |> List.contains def
            else false

// Custom codec: serializes F# type bodies via System.TextJson, bridges via IFieldCodec.
// Production will use Fleece+gzip instead of System.TextJson (drop-in).
type FSharpJsonCodec() =
    let jsonOpts = System.Text.Json.JsonSerializerOptions()
    do jsonOpts.Converters.Add(FSharpJsonConverter())

    // IFieldCodec (from Orleans.Serialization.Codecs)
    // Wire format: [header: LengthPrefixed]
    //              [varuint32 typeFullNameBytes.Length][typeFullNameBytes]
    //              [varuint32 jsonBytes.Length][jsonBytes]
    // The type-name prefix lets ReadValue know the exact runtime type (mirrors the existing
    // Serializer.fs TypeId byte scheme). Without it, the "try each registered type" heuristic
    // mis-routes inner payloads (e.g. BlobData vs Todo) when called by typed codecs like
    // FSharpResultCodec.
    interface IFieldCodec with
        member _.WriteField<'W when 'W :> System.Buffers.IBufferWriter<byte>>
            (writer: byref<Writer<'W>>, fieldIdDelta: uint32, expectedType: Type, value: obj) : unit =
            if isNull value then () else
                let runtimeType = value.GetType()
                writer.WriteFieldHeader(fieldIdDelta, expectedType, runtimeType, WireType.LengthPrefixed)
                let typeFullName = System.Text.Encoding.UTF8.GetBytes(runtimeType.FullName)
                writer.WriteVarUInt32(uint32 typeFullName.Length)
                writer.Write(System.ReadOnlySpan<byte>(typeFullName))
                let json = System.Text.Json.JsonSerializer.Serialize(value, runtimeType, jsonOpts)
                let bytes = System.Text.Encoding.UTF8.GetBytes(json)
                writer.WriteVarUInt32(uint32 bytes.Length)
                writer.Write(System.ReadOnlySpan<byte>(bytes))

        member _.ReadValue<'R>(reader: byref<Reader<'R>>, field: Field) : obj =
            let _ = field
            // Read the type-name prefix.
            let typeNameLength = reader.ReadVarUInt32() |> int
            let typeNameBytes : byte[] = Array.zeroCreate typeNameLength
            reader.ReadBytes(System.Span<byte>(typeNameBytes)) |> ignore
            let typeFullName = System.Text.Encoding.UTF8.GetString(typeNameBytes)
            // Resolve the .NET Type by FullName across loaded assemblies.
            let runtimeType =
                AppDomain.CurrentDomain.GetAssemblies()
                |> Seq.pick (fun a ->
                    try
                        match a.GetType(typeFullName) with
                        | null -> None
                        | t -> Some t
                    with _ -> None)
                |> (fun t ->
                    if isNull t then
                        failwithf "FSharpJsonCodec.ReadValue: could not resolve type %s" typeFullName
                    else t)
            // Read the JSON bytes.
            let jsonLength = reader.ReadVarUInt32() |> int
            let jsonBytes : byte[] = Array.zeroCreate jsonLength
            reader.ReadBytes(System.Span<byte>(jsonBytes)) |> ignore
            let json = System.Text.Encoding.UTF8.GetString(jsonBytes)
            System.Text.Json.JsonSerializer.Deserialize(json, runtimeType, jsonOpts)

    // IGeneralizedCodec (from Orleans.Serialization.Serializers)
    interface IGeneralizedCodec with
        member _.IsSupportedType(t: Type) : bool =
            FSharpTypeRegistry.isSupported t

    // IGeneralizedCopier (from Orleans.Serialization.Cloning)
    interface IGeneralizedCopier with
        member _.IsSupportedType(t: Type) : bool =
            FSharpTypeRegistry.isSupported t

    interface IDeepCopier with
        member _.DeepCopy(input: obj, _context: CopyContext) : obj =
            // All F# types here are immutable; return as-is (matches the existing Serializer.fs DeepCopy).
            input

module private FSharpTypeRegistration =

    let registerFSharpCodecs (services: IServiceCollection) : unit =
        services.AddSingleton<FSharpJsonCodec>() |> ignore
        services.AddSingleton<IGeneralizedCodec, FSharpJsonCodec>() |> ignore
        services.AddSingleton<IGeneralizedCopier, FSharpJsonCodec>() |> ignore
        services.AddSingleton<IDeepCopier, FSharpJsonCodec>() |> ignore

module Program =

    type SiloConfigurator() =
        interface ISiloConfigurator with
            member _.Configure(siloBuilder: ISiloBuilder) =
                siloBuilder.Configure<TypeManifestOptions>(fun (options: TypeManifestOptions) ->
                    options.AllowAllTypes <- true)
                |> ignore
                siloBuilder.Services |> FSharpTypeRegistration.registerFSharpCodecs

    type ClientConfigurator() =
        interface IClientBuilderConfigurator with
            member _.Configure(_configuration: IConfiguration, clientBuilder: IClientBuilder) =
                clientBuilder.Configure<TypeManifestOptions>(fun (options: TypeManifestOptions) ->
                    options.AllowAllTypes <- true)
                |> ignore
                clientBuilder.Services |> FSharpTypeRegistration.registerFSharpCodecs

    let runSpike (cluster: TestCluster) : int =
        let mutable passCount = 0
        let mutable failCount = 0

        let check (label: string) (ok: bool) =
            if ok then passCount <- passCount + 1; printfn "  PASS: %s" label
            else failCount <- failCount + 1; printfn "  FAIL: %s" label

        // === Shape 1: IBlobSpikeGrain round-trip with BlobData (Option<BlobData>) ===
        try
            let grain = cluster.GrainFactory.GetGrain<IBlobSpikeGrain>(Guid.NewGuid())
            let blob = { BlobId = Guid.NewGuid(); Bytes = [| 1uy; 2uy; 3uy; 4uy; 5uy |] }
            grain.SetBlob(blob.BlobId, blob).Wait()
            let retrieved = grain.GetBlob(blob.BlobId).Result
            check "BlobData round-trip via Option<BlobData>"
                (retrieved.IsSome && retrieved.Value.BlobId = blob.BlobId && retrieved.Value.Bytes = blob.Bytes)
        with ex ->
            check "BlobData round-trip via Option<BlobData>" false
            printfn "    exception: %A" ex

        // === Shape 2: IViewSpikeGrain.Mutate nullary action (ToggleDone, Archive) -- exercises InternalsVisibleTo ===
        try
            let grain = cluster.GrainFactory.GetGrain<IViewSpikeGrain<GetTodoInput, GetTodoOutput>>(Guid.NewGuid())
            let initial = grain.Mutate(TodoAction.SetTitle "first").Result
            check "SetTitle initial"
                (match initial with Ok _ -> true | _ -> false)
            let toggled = grain.Mutate(TodoAction.ToggleDone).Result
            check "ToggleDone (nullary case) round-trip"
                (match toggled with Ok t -> t.Done | _ -> false)
            let archived = grain.Mutate(TodoAction.Archive).Result
            check "Archive (nullary case) round-trip"
                (match archived with Ok t -> t.Category.IsNone | _ -> false)
        with ex ->
            check "Mutate nullary cases (ToggleDone/Archive)" false
            printfn "    exception: %A" ex

        // === Shape 3: IViewSpikeGrain.Mutate SetTitle empty (EmptyTitle nullary error) ===
        try
            let grain = cluster.GrainFactory.GetGrain<IViewSpikeGrain<GetTodoInput, GetTodoOutput>>(Guid.NewGuid())
            let _ = grain.Mutate(TodoAction.SetTitle "x").Result
            let err = grain.Mutate(TodoAction.SetTitle "").Result
            check "EmptyTitle (nullary error case) round-trip"
                (match err with Error TodoOpError.EmptyTitle -> true | _ -> false)
        with ex ->
            check "EmptyTitle (nullary error case)" false
            printfn "    exception: %A" ex

        // === Shape 4: IViewSpikeGrain.Read returns Result<GetTodoOutput, TodoOpError> with Todo list ===
        try
            let grain = cluster.GrainFactory.GetGrain<IViewSpikeGrain<GetTodoInput, GetTodoOutput>>(Guid.NewGuid())
            let _ = grain.Mutate(TodoAction.SetTitle "read-test").Result
            let result = grain.Read({ QueryId = Guid.NewGuid() }).Result
            check "Read returns Result<Output, Error> with Todo list"
                (match result with
                 | Ok out -> out.Todos.Length > 0 && out.Todos.[0].Title = "read-test"
                 | _ -> false)
        with ex ->
            check "Read returns Result<Output, Error>" false
            printfn "    exception: %A" ex

        // === Shape 5: Priority / Category round-trip within Todo ===
        try
            let grain = cluster.GrainFactory.GetGrain<IViewSpikeGrain<GetTodoInput, GetTodoOutput>>(Guid.NewGuid())
            let result = grain.Mutate(TodoAction.SetPriority Priority.High).Result
            check "Priority.High round-trip"
                (match result with Ok t -> t.Priority = Priority.High | _ -> false)
        with ex ->
            check "Priority round-trip" false
            printfn "    exception: %A" ex

        printfn "SPIKE PASS: %d / FAIL: %d" passCount failCount
        if failCount = 0 then 0 else 1

    [<EntryPoint>]
    let main _argv =
        printfn "SPIKE: S15b production codegen-host pattern (generic F# grain interfaces + custom IGeneralizedCodec + InternalsVisibleTo)"
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
