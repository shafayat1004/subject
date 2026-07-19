[<AutoOpen>]
module internal LibLifeCycleCore.OrleansEx.Serializer

// couldn't make LibLifeCycleCore.Orleans namespace because it makes "open Orleans" ambiguous in a few places

open System
open System.Buffers
open System.Collections.Generic
open System.Text
open System.Threading.Tasks
open Orleans.Serialization
open Orleans.Serialization.Buffers
open Orleans.Serialization.Cloning
open Orleans.Serialization.Codecs
open Orleans.Serialization.Serializers
open Orleans.Serialization.WireProtocol
open LibLifeCycleCore
open FSharpPlus
open CodecLib.StjCodecs
open Microsoft.FSharp.Reflection

#nowarn "0686"  // Allow exceptionally here to have explicit parameters for codec related functions

// Orleans 10 natively serializes these leaf types (primitives + well-known BCL types) AND natively
// DECOMPOSES Option/ValueOption/Choice/Result/tuples, delegating each generic arg to that arg's own
// codec (a generic codec always beats our IGeneralizedCodec fallback). So the custom codec registers
// bare LEAF types (user records/DUs), never the native wrappers, and the coverage guards must peel the
// native wrappers to leaves before checking. See spikes/s15d-fsharp-wrapper-codecs.md.
let private orleansNativeLeafTypes : HashSet<Type> =
    HashSet<Type> [
        typeof<string>; typeof<bool>; typeof<char>; typeof<Guid>
        typeof<byte>; typeof<int16>; typeof<uint16>; typeof<int>; typeof<uint32>
        typeof<int64>; typeof<uint64>; typeof<decimal>; typeof<double>; typeof<single>
        typeof<DateTime>; typeof<DateTimeOffset>; typeof<DateOnly>; typeof<TimeOnly>; typeof<TimeSpan>
        typeof<unit>; typeof<Void>; typeof<Task>
    ]

let rec private decomposeToNativeLeaves (t: Type) : Type list =
    if FSharpType.IsTuple t then
        FSharpType.GetTupleElements t |> Array.toList |> List.collect decomposeToNativeLeaves
    elif t.IsGenericType then
        let gtd = t.GetGenericTypeDefinition()
        if gtd = typedefof<option<_>> || gtd = typedefof<ValueOption<_>> then
            decomposeToNativeLeaves (t.GetGenericArguments().[0])
        elif gtd = typedefof<Result<_, _>> then
            t.GetGenericArguments() |> Array.toList |> List.collect decomposeToNativeLeaves
        elif gtd.FullName.StartsWith "Microsoft.FSharp.Core.FSharpChoice`" then
            t.GetGenericArguments() |> Array.toList |> List.collect decomposeToNativeLeaves
        else
            [ t ]
    else
        [ t ]

let inline private summarizerForType (toCodecFriendly: 'T -> 'CodecFriendly) : (Type * (obj -> string)) =
    typeof<'T>, (fun (t: obj) -> t :?> 'T |> toCodecFriendly |> ValueSummaryCodecs.toSummaryValue |> ValueSummaryCodecs.getSummaryString 6)


type internal UntypedSerializer = {
    To:     obj -> byte[]
    Fro:    byte[] -> obj
    Type:   Type
    TypeId: byte

    // TODO: can we move it away? it's incidental that we need logging summarizers and custom Orleans serializers for the same types
    //  (in fact we may need more type for summarizers e.g. error types (not just errors inside Result<_, _>)
    ToSummary: obj -> string
}
with
    static member inline ForIsomorphicType
        (typeId: byte)
        (toCodecFriendly: 'T -> 'CodecFriendly)
        (fromCodecFriendly: 'CodecFriendly -> 'T)
        // since F# doesn't support higher ranks we need to supply two copies of the same 'CodecFriendly type in order to be generalized with different generics
        (codecFriendlyId: 'CodecFriendly -> 'CodecFriendly2) : UntypedSerializer =
        // 'CodecFriendly can be encoded or decoded in a type-safe way
        // 'T is the type isomorphic to 'CodecFriendly (in unsafe way) that grain actually wants to expose
        let toJsonText (x: 'CodecFriendly) =
            x
#if DEBUG // TODO:  instead of if-def, pass config from upstream to check json in Dev Host and Tests but it is very tedious
            |> toJsonTextChecked<'CodecFriendly>
#else
            |> toJson<'CodecFriendly> |> (fun v -> v.ToString(unindentedOptions))
#endif
        {
            To  = fun (t: obj) -> t :?> 'T |> toCodecFriendly |> toJsonText |> gzipCompressUtf8String
            Fro = fun (bytes: byte[]) ->
                let jsonText = gzipDecompressToUtf8String bytes
                match ofJsonText<'CodecFriendly> jsonText with
                | Ok x ->
                    fromCodecFriendly x |> box
                | Error err ->
                    InvalidOperationException (sprintf "Orleans deserializer is unable to decode. Type Id: %d. Error: %A. Json value: %s" typeId err jsonText)
                    |> raise
            ToSummary = summarizerForType (toCodecFriendly >> codecFriendlyId) |> snd
            Type      = typeof<'T>
            TypeId    = typeId
        }

    static member inline ForConcreteTypeWithExplicitCodec
        (typeId: byte)
        (codec: Fleece.Codec<StjEncoding, 'T>) : UntypedSerializer =
        let toJsonText (x: 'T) =
            x
#if DEBUG // TODO:  instead of if-def, pass config from upstream to check json in Dev Host and Tests but it is very tedious
            |> toJsonTextCheckedWithCodec<'T> codec
#else
            |> CodecLib.Codec.encode codec |> (fun v -> v.ToString(unindentedOptions))
#endif
        {
            To  = fun (t: obj) -> t :?> 'T |> toJsonText |> gzipCompressUtf8String
            Fro = fun (bytes: byte[]) ->
                let jsonText = gzipDecompressToUtf8String bytes
                let enc = StjEncoding.Parse jsonText
                match CodecLib.Codec.decode codec enc with
                | Ok x ->
                    x |> box
                | Error err ->
                    InvalidOperationException (sprintf "Orleans deserializer is unable to decode. Type Id: %d. Error: %A. Json value: %s" typeId err jsonText)
                    |> raise
            ToSummary = fun _ ->
                NotImplementedException (sprintf "Orleans deserializer ForConcreteTypeWithExplicitCodec does not implement ToSummary. Type Id: %d" typeId)
                |> raise
            Type   = typeof<'T>
            TypeId = typeId
        }

// Command-object interfaces from LibLifeCycle.Services are used in place of F# closures on
// IConnectorGrain grain methods. Their concrete implementations carry only immutable captured
// state (e.g. RunJobRequestData), so returning the same instance during a deep-copy is safe.
// This unblocks the Orleans 10 invoker's CopyContext.DeepCopy of builder/mapper arguments.
let rec private isConnectorCommandType (itemType: Type) =
    if isNull itemType then false
    else
        let interfaces = if itemType.IsInterface then [| itemType |] else itemType.GetInterfaces()
        interfaces
        |> Array.exists (fun i ->
            i.IsGenericType &&
            let gtd = i.GetGenericTypeDefinition()
            let fullName =
                match gtd.FullName with
                | null -> ""
                | name -> name.Replace('+', '.')
            fullName.StartsWith "LibLifeCycle.Services.IConnectorRequestBuilder`"
            || fullName.StartsWith "LibLifeCycle.Services.IConnectorRequestBuilderSingleReply`"
            || fullName.StartsWith "LibLifeCycle.Services.IConnectorResponseMapper`")

type EggShellSubjectGrainsCodec (untypedSerializersByTypeId: IReadOnlyDictionary<byte, UntypedSerializer>,
                                  untypedSerializersByType: IReadOnlyDictionary<Type, UntypedSerializer>) =

    static let payloadVersion = 0uy

    let tryGetSerializerForType (itemType: Type) =
        match IReadOnlyDictionary.tryGetValue itemType untypedSerializersByType with
        | Some serializer -> Some serializer
        | None ->
            if not (isNull itemType.BaseType) then
                match IReadOnlyDictionary.tryGetValue itemType.BaseType untypedSerializersByType with
                | Some serializer -> Some serializer
                | None ->
                    itemType.GetInterfaces()
                    |> Array.tryPick (fun i -> IReadOnlyDictionary.tryGetValue i untypedSerializersByType)
            else
                itemType.GetInterfaces()
                |> Array.tryPick (fun i -> IReadOnlyDictionary.tryGetValue i untypedSerializersByType)

    interface IFieldCodec with
        member _.WriteField<'W when 'W :> IBufferWriter<byte>>
            (writer: byref<Writer<'W>>, fieldIdDelta: uint32, expectedType: Type, value: obj) : unit =
            if isNull value then
                failwith "OrleansSerializer.WriteField got null; that's not expected"
            else
                let runtimeType = value.GetType()
                writer.WriteFieldHeader(fieldIdDelta, expectedType, runtimeType, WireType.LengthPrefixed)
                match tryGetSerializerForType runtimeType with
                | None -> failwithf "Invalid type for serialization %s, item: %O" runtimeType.FullName value
                | Some serializer ->
                    writer.WriteVarUInt32(1u)
                    writer.Write(System.ReadOnlySpan<byte>([| payloadVersion |]))
                    writer.WriteVarUInt32(1u)
                    writer.Write(System.ReadOnlySpan<byte>([| serializer.TypeId |]))
                    let payload = serializer.To value
                    writer.WriteVarUInt32(uint32 payload.Length)
                    writer.Write(System.ReadOnlySpan<byte>(payload))

        member _.ReadValue<'R>(reader: byref<Reader<'R>>, field: Field) : obj =
            let _ = field
            let versionLength = reader.ReadVarUInt32() |> int
            let versionBytes = Array.zeroCreate<byte> versionLength
            reader.ReadBytes(System.Span<byte>(versionBytes)) |> ignore
            let _version = versionBytes.[0]
            let typeIdLength = reader.ReadVarUInt32() |> int
            let typeIdBytes = Array.zeroCreate<byte> typeIdLength
            reader.ReadBytes(System.Span<byte>(typeIdBytes)) |> ignore
            let serializedTypeId = typeIdBytes.[0]
            let payloadLength = reader.ReadVarUInt32() |> int
            let payload = Array.zeroCreate<byte> payloadLength
            reader.ReadBytes(System.Span<byte>(payload)) |> ignore
            match untypedSerializersByTypeId.TryGetValue serializedTypeId with
            | true, serializer -> serializer.Fro payload
            | false, _         -> failwithf "Invalid type header for deserialization %A" serializedTypeId

    interface IGeneralizedCodec with
        member _.IsSupportedType(itemType: Type) : bool =
            tryGetSerializerForType itemType |> Option.isSome

    interface IGeneralizedCopier with
        member _.IsSupportedType(itemType: Type) : bool =
            tryGetSerializerForType itemType |> Option.isSome ||
            isConnectorCommandType itemType

    interface IDeepCopier with
        member _.DeepCopy(source: obj, _context: CopyContext) : obj =
            source

let private getUntypedSubjectSerializers<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent, 'SubjectIndex, 'SubjectId
                                     when 'Subject              :> Subject<'SubjectId>
                                     and  'LifeAction           :> LifeAction
                                     and  'OpError              :> OpError
                                     and  'Constructor          :> Constructor
                                     and  'LifeEvent            :> LifeEvent
                                     and  'LifeEvent            : comparison
                                     and  'SubjectIndex         :> SubjectIndex<'OpError>
                                     and  'SubjectId            :> SubjectId
                                     and  'SubjectId            : comparison> () =

    // List out all ISubjectGrain, ISubjectIdGenerationGrain, ISubjectRepoGrain, IBlobRepoGrain, ISubjectReflectionGrain parameters and response types
    // NOTES:
    // * no need to cover IConnectorGrain because it's always executed locally
    // * the type ID mapping can *never* change. Do not re-use. If you ever remove a serializer
    //   simply comment out that line, so others don't accidentally re-use
    // * Orleans 10 natively DECOMPOSES Option/ValueOption/Choice/Result/tuples and delegates each
    //   generic arg to that arg's own codec, so we register bare LEAF types, never those wrappers.
    //   TypeIds 8/15/17-19/21-22/32/36-39/45-46/53-55/59/62-66/68/71/73/77/79-83 were native wrappers
    //   under Orleans 3.7; they are retired here and their non-native leaves re-registered at 84+.
    //   See spikes/s15d-fsharp-wrapper-codecs.md.

    [
        UntypedSerializer.ForIsomorphicType   1uy
            (fun x -> x :> Subject<'SubjectId>)
            (fun x -> x :?> 'Subject)
            id<_>

        UntypedSerializer.ForIsomorphicType   2uy
            (fun x -> x :> SubjectId)
            (fun x -> x :?> 'SubjectId)
            id<_>

        UntypedSerializer.ForIsomorphicType   3uy
            (fun x -> x :> LifeAction)
            (fun x -> x :?> 'LifeAction)
            id<_>

//        UntypedSerializer.ForIsomorphicType   4uy

        UntypedSerializer.ForIsomorphicType   5uy
            (fun x -> x :> Constructor)
            (fun x -> x :?> 'Constructor)
            id<_>

        UntypedSerializer.ForIsomorphicType   6uy
            (fun x -> x :> LifeEvent)
            (fun x -> x :?> 'LifeEvent)
            id<_>

//        UntypedSerializer.ForIsomorphicType   7uy

//        UntypedSerializer.ForIsomorphicType   8uy  // was Result<Subject, GrainConstructionError<OpError>>; leaves: Subject (1) + GrainConstructionError<OpError> (84)

//        UntypedSerializer.ForIsomorphicType  9uy

//        UntypedSerializer.ForIsomorphicType  10uy

//        UntypedSerializer.ForIsomorphicType  11uy

//        UntypedSerializer.ForIsomorphicType  12uy

        UntypedSerializer.ForIsomorphicType  13uy
            id<Map<SubscriptionName, LifeEvent>>
            id<Map<SubscriptionName, LifeEvent>>
            id<_>

//        UntypedSerializer.ForIsomorphicType  14uy

//        UntypedSerializer.ForIsomorphicType  15uy  // was Result<unit, SubjectFailure<GrainSubscriptionError>>; leaf: SubjectFailure<GrainSubscriptionError> (95)

        UntypedSerializer.ForIsomorphicType  16uy
            id<SubscriptionTriggerType>
            id<SubscriptionTriggerType>
            id<_>

//        UntypedSerializer.ForIsomorphicType  17uy  // was Result<unit, SubjectFailure<GrainTriggerSubscriptionError<OpError>>>; leaf: (96)

//        UntypedSerializer.ForIsomorphicType  18uy  // was Option<ConstructSubscriptions>; leaf: ConstructSubscriptions (92)

//        UntypedSerializer.ForIsomorphicType  19uy  // was Result<SubjectId, GrainIdGenerationError<OpError>>; leaves: SubjectId (2) + GrainIdGenerationError<OpError> (85)

//        UntypedSerializer.ForIsomorphicType  20uy

//        UntypedSerializer.ForIsomorphicType  21uy  // was Result<unit, SubjectFailure<GrainPrepareConstructionError<OpError>>>; leaf: (97)

//        UntypedSerializer.ForIsomorphicType  22uy  // was Result<unit, SubjectFailure<GrainPrepareTransitionError<OpError>>>; leaf: (98)

//        UntypedSerializer.ForIsomorphicType  23uy

//        UntypedSerializer.ForIsomorphicType  24uy

        UntypedSerializer.ForIsomorphicType  25uy
            (Set.map (fun v -> v :> SubjectId))
            (Set.map (fun v -> v :?> 'SubjectId))
            id<_>

//        UntypedSerializer.ForIsomorphicType  26uy

        UntypedSerializer.ForIsomorphicType  27uy
            id<IndexQuery<'SubjectIndex>>
            id<IndexQuery<'SubjectIndex>>
            id<_>

        UntypedSerializer.ForIsomorphicType  28uy
            id<Set<string>>
            id<Set<string>>
            id<_>

        UntypedSerializer.ForIsomorphicType  29uy
            id<ResultSetOptions<'SubjectIndex>>
            id<ResultSetOptions<'SubjectIndex>>
            id<_>

        UntypedSerializer.ForIsomorphicType  30uy
            (List.map (fun v -> v :> Subject<'SubjectId>))
            (List.map (fun v -> v :?> 'Subject))
            id<_>

        UntypedSerializer.ForIsomorphicType  31uy
            (List.map TemporalSnapshot<Subject<'SubjectId>, LifeAction, Constructor, 'SubjectId>.CastUnsafe)
            (List.map TemporalSnapshot<'Subject, 'LifeAction, 'Constructor, 'SubjectId>.CastUnsafe)
            id<_>

//        UntypedSerializer.ForIsomorphicType  32uy  // was Option<TemporalSnapshot<..>>; leaf: TemporalSnapshot<..> (93)

        UntypedSerializer.ForIsomorphicType  33uy
            id<PreparedIndexPredicate<'SubjectIndex>>
            id<PreparedIndexPredicate<'SubjectIndex>>
            id<_>

//        UntypedSerializer.ForIsomorphicType  34uy

//        UntypedSerializer.ForIsomorphicType  35uy

//        UntypedSerializer.ForIsomorphicType  36uy  // was Result<Option<Subject>, GrainGetError>; leaves: Subject (1) + GrainGetError (89)

//        UntypedSerializer.ForIsomorphicType  37uy  // was Result<Option<Subject>, SubjectFailure<GrainConstructionError<OpError>>>; leaf: (99)

//        UntypedSerializer.ForIsomorphicType  38uy  // was Result<Option<Subject>, SubjectFailure<GrainTransitionError<OpError>>>; leaf: (100)

//        UntypedSerializer.ForIsomorphicType  39uy  // was Result<Option<Subject>, SubjectFailure<GrainOperationError<OpError>>>; leaf: (101)

        // UntypedSerializer.ForIsomorphicType  40uy

        UntypedSerializer.ForIsomorphicType  41uy
            id<ResultPage>
            id<ResultPage>
            id<_>

        UntypedSerializer.ForIsomorphicType  42uy
            id<LifeEvent>
            id<LifeEvent>
            id<_>

        UntypedSerializer.ForIsomorphicType  43uy
            id<SubjectTransactionId>
            id<SubjectTransactionId>
            id<_>

        UntypedSerializer.ForIsomorphicType  44uy
            id<SideEffectDedupInfo>
            id<SideEffectDedupInfo>
            id<_>

//        UntypedSerializer.ForIsomorphicType  45uy  // was Option<SideEffectDedupInfo>; leaf already bare at typeId 44

//        UntypedSerializer.ForIsomorphicType  46uy  // was Result<unit, GrainRefreshTimersAndSubsError>; leaf: GrainRefreshTimersAndSubsError (90)

        UntypedSerializer.ForIsomorphicType  47uy
            id<SubjectPKeyReference>
            id<SubjectPKeyReference>
            id<_>

        //UntypedSerializer.ForIsomorphicType  48uy

        //UntypedSerializer.ForIsomorphicType  49uy

        //UntypedSerializer.ForIsomorphicType  50uy

        UntypedSerializer.ForIsomorphicType  51uy
            id<NonemptySet<Guid>> // GrainSideEffectId
            id<NonemptySet<Guid>>
            id<_>

        //UntypedSerializer.ForIsomorphicType  52uy

//        UntypedSerializer.ForIsomorphicType  53uy  // was Option<DateTimeOffset>; inner is Orleans-native, no registration needed

//        UntypedSerializer.ForIsomorphicType  54uy  // was Result<unit, SubjectFailure<GrainEnqueueActionError>>; leaf: (102)

//        UntypedSerializer.ForIsomorphicType  55uy  // was (List<Subject>, uint64) tuple; leaves: List<Subject> (30) + uint64 (native)

        UntypedSerializer.ForIsomorphicType  56uy
            id<GetSnapshotOfVersion>
            id<GetSnapshotOfVersion>
            id<_>

        // UntypedSerializer.ForIsomorphicType  57uy

        UntypedSerializer.ForIsomorphicType  58uy
            id<LocalSubjectPKeyReference>
            id<LocalSubjectPKeyReference>
            id<_>

//        UntypedSerializer.ForIsomorphicType  59uy  // was Result<unit, SubjectFailure<GrainTriggerDynamicSubscriptionError>>; leaf: (103)

        UntypedSerializer.ForIsomorphicType  60uy
            id<ClientGrainCallContext>
            id<ClientGrainCallContext>
            id<_>

        UntypedSerializer.ForIsomorphicType  61uy
            VersionedSubject<Subject<'SubjectId>, 'SubjectId>.CastUnsafe
            VersionedSubject<'Subject, 'SubjectId>.CastUnsafe
            id<_>

//        UntypedSerializer.ForIsomorphicType  62uy  // was Result<VersionedSubject, GrainConstructionError<OpError>>; leaves: VersionedSubject (61) + GrainConstructionError<OpError> (84)

//        UntypedSerializer.ForIsomorphicType  63uy  // was Result<VersionedSubject, GrainTransitionError<OpError>>; leaves: VersionedSubject (61) + GrainTransitionError<OpError> (86)

//        UntypedSerializer.ForIsomorphicType  64uy  // was Result<VersionedSubject, GrainOperationError<OpError>>; leaves: VersionedSubject (61) + GrainOperationError<OpError> (87)

//        UntypedSerializer.ForIsomorphicType  65uy  // was Result<VersionedSubject, GrainMaybeConstructionError<OpError>>; leaves: VersionedSubject (61) + GrainMaybeConstructionError<OpError> (88)

//        UntypedSerializer.ForIsomorphicType  66uy  // was Result<Option<VersionedSubject>, GrainGetError>; leaves: VersionedSubject (61) + GrainGetError (89)

        UntypedSerializer.ForIsomorphicType   67uy
            LibLifeCycleCore.SubjectChange<Subject<'SubjectId>, 'SubjectId>.CastUnsafe
            LibLifeCycleCore.SubjectChange<'Subject, 'SubjectId>.CastUnsafe
            id<_>

//        UntypedSerializer.ForIsomorphicType  68uy  // was Option<Tuple<int64, uint64>>; inner is Orleans-native, no registration needed

        UntypedSerializer.ForIsomorphicType  69uy
            id<list<string>>
            id<list<string>>
            id<_>

        UntypedSerializer.ForIsomorphicType  70uy
            (List.map VersionedSubject<Subject<'SubjectId>, 'SubjectId>.CastUnsafe)
            (List.map VersionedSubject<'Subject, 'SubjectId>.CastUnsafe)
            id<_>

//        UntypedSerializer.ForIsomorphicType  71uy  // was (List<VersionedSubject>, uint64) tuple; leaves: List<VersionedSubject> (70) + uint64 (native)

        UntypedSerializer.ForIsomorphicType 72uy
            id<CallOrigin>
            id<CallOrigin>
            id<_>

//        UntypedSerializer.ForIsomorphicType 73uy  // was Option<BlobData>; leaf: BlobData (94)

        UntypedSerializer.ForIsomorphicType  74uy
            (List.map (fun (id: 'SubjectId) -> id :> SubjectId))
            (List.map (fun (id: SubjectId) -> id :?> 'SubjectId))
            id<_>

        UntypedSerializer.ForIsomorphicType  75uy
            (List.map SubjectAuditData<LifeAction, Constructor>.CastUnsafe)
            (List.map SubjectAuditData<'LifeAction, 'Constructor>.CastUnsafe)
            id<_>

        // UntypedSerializer.ForIsomorphicType  76uy

//        UntypedSerializer.ForIsomorphicType  77uy  // was Result<unit, SubjectFailure<GrainTransitionError<OpError>>>; leaf: (100)

        UntypedSerializer.ForIsomorphicType  78uy
            (NonemptyList.map (fun (id: 'LifeAction) -> id :> LifeAction))
            (NonemptyList.map (fun (id: LifeAction) -> id :?> 'LifeAction))
            id<_>

//        UntypedSerializer.ForIsomorphicType  79uy  // was Result<Option<'LifeAction>, GrainTriggerTimerError<'OpError, 'LifeAction>>; leaves: 'LifeAction (3) + GrainTriggerTimerError<OpError, LifeAction> (91)

//        UntypedSerializer.ForIsomorphicType  80uy  // was Result<unit, GrainConstructionError<OpError>>; leaf: GrainConstructionError<OpError> (84)

//        UntypedSerializer.ForIsomorphicType  81uy  // was Result<unit, GrainTransitionError<OpError>>; leaf: GrainTransitionError<OpError> (86)

//        UntypedSerializer.ForIsomorphicType  82uy  // was Result<unit, GrainOperationError<OpError>>; leaf: GrainOperationError<OpError> (87)

//        UntypedSerializer.ForIsomorphicType  83uy  // was Result<unit, GrainMaybeConstructionError<OpError>>; leaf: GrainMaybeConstructionError<OpError> (88)

        // --- bare leaves for Orleans-10-decomposed wrappers (S15d-production-port) ---

        UntypedSerializer.ForIsomorphicType  84uy // error leaf of retired 8, 62, 80
            GrainConstructionError<OpError>.CastUnsafe
            GrainConstructionError<'OpError>.CastUnsafe
            id<_>

        UntypedSerializer.ForIsomorphicType  85uy // error leaf of retired 19
            GrainIdGenerationError<OpError>.CastUnsafe
            GrainIdGenerationError<'OpError>.CastUnsafe
            id<_>

        UntypedSerializer.ForIsomorphicType  86uy // error leaf of retired 63, 81
            GrainTransitionError<OpError>.CastUnsafe
            GrainTransitionError<'OpError>.CastUnsafe
            id<_>

        UntypedSerializer.ForIsomorphicType  87uy // error leaf of retired 64, 82
            GrainOperationError<OpError>.CastUnsafe
            GrainOperationError<'OpError>.CastUnsafe
            id<_>

        UntypedSerializer.ForIsomorphicType  88uy // error leaf of retired 65, 83
            GrainMaybeConstructionError<OpError>.CastUnsafe
            GrainMaybeConstructionError<'OpError>.CastUnsafe
            id<_>

        UntypedSerializer.ForIsomorphicType  89uy // error leaf of retired 36, 66
            id<GrainGetError>
            id<GrainGetError>
            id<_>

        UntypedSerializer.ForIsomorphicType  90uy // error leaf of retired 46
            id<GrainRefreshTimersAndSubsError>
            id<GrainRefreshTimersAndSubsError>
            id<_>

        UntypedSerializer.ForIsomorphicType  91uy // error leaf of retired 79
            GrainTriggerTimerError<OpError, LifeAction>.CastUnsafe
            GrainTriggerTimerError<'OpError, 'LifeAction>.CastUnsafe
            id<_>

        UntypedSerializer.ForIsomorphicType  92uy // ok leaf of retired 18
            id<ConstructSubscriptions>
            id<ConstructSubscriptions>
            id<_>

        UntypedSerializer.ForIsomorphicType  93uy // ok leaf of retired 32
            TemporalSnapshot<Subject<'SubjectId>, LifeAction, Constructor, 'SubjectId>.CastUnsafe
            TemporalSnapshot<'Subject, 'LifeAction, 'Constructor, 'SubjectId>.CastUnsafe
            id<_>

        UntypedSerializer.ForIsomorphicType  94uy // ok leaf of retired 73
            id<BlobData>
            id<BlobData>
            id<_>

        UntypedSerializer.ForIsomorphicType  95uy // error leaf of retired 15
            id<SubjectFailure<GrainSubscriptionError>>
            id<SubjectFailure<GrainSubscriptionError>>
            id<_>

        UntypedSerializer.ForIsomorphicType  96uy // error leaf of retired 17
            (SubjectFailure.CastUnsafe GrainTriggerSubscriptionError<OpError>.CastUnsafe)
            (SubjectFailure.CastUnsafe GrainTriggerSubscriptionError<'OpError>.CastUnsafe)
            id<_>

        UntypedSerializer.ForIsomorphicType  97uy // error leaf of retired 21
            (SubjectFailure.CastUnsafe GrainPrepareConstructionError<OpError>.CastUnsafe)
            (SubjectFailure.CastUnsafe GrainPrepareConstructionError<'OpError>.CastUnsafe)
            id<_>

        UntypedSerializer.ForIsomorphicType  98uy // error leaf of retired 22
            (SubjectFailure.CastUnsafe GrainPrepareTransitionError<OpError>.CastUnsafe)
            (SubjectFailure.CastUnsafe GrainPrepareTransitionError<'OpError>.CastUnsafe)
            id<_>

        UntypedSerializer.ForIsomorphicType  99uy // error leaf of retired 37
            (SubjectFailure.CastUnsafe GrainConstructionError<OpError>.CastUnsafe)
            (SubjectFailure.CastUnsafe GrainConstructionError<'OpError>.CastUnsafe)
            id<_>

        UntypedSerializer.ForIsomorphicType 100uy // error leaf of retired 38, 77
            (SubjectFailure.CastUnsafe GrainTransitionError<OpError>.CastUnsafe)
            (SubjectFailure.CastUnsafe GrainTransitionError<'OpError>.CastUnsafe)
            id<_>

        UntypedSerializer.ForIsomorphicType 101uy // error leaf of retired 39
            (SubjectFailure.CastUnsafe GrainOperationError<OpError>.CastUnsafe)
            (SubjectFailure.CastUnsafe GrainOperationError<'OpError>.CastUnsafe)
            id<_>

        UntypedSerializer.ForIsomorphicType 102uy // error leaf of retired 54
            id<SubjectFailure<GrainEnqueueActionError>>
            id<SubjectFailure<GrainEnqueueActionError>>
            id<_>

        UntypedSerializer.ForIsomorphicType 103uy // error leaf of retired 59
            id<SubjectFailure<GrainTriggerDynamicSubscriptionError>>
            id<SubjectFailure<GrainTriggerDynamicSubscriptionError>>
            id<_>
    ]
    |> fun untypedSerializers ->
        // assert no duplicate Ids before return
        match untypedSerializers |> Seq.groupBy (fun s -> s.TypeId) |> Seq.filter (fun (_, g) -> (Seq.length g) > 1) |> Seq.map fst |> Seq.tryHead with
        | None ->
            untypedSerializers
        | Some duplicateTypeId ->
            failwithf "Duplicate typeId in subject serializers: %d" duplicateTypeId

let private buildSubjectCodec<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent, 'SubjectIndex, 'SubjectId
                         when 'Subject              :> Subject<'SubjectId>
                         and  'LifeAction           :> LifeAction
                         and  'OpError              :> OpError
                         and  'Constructor          :> Constructor
                         and  'LifeEvent            :> LifeEvent
                         and  'LifeEvent            : comparison
                         and  'SubjectIndex         :> SubjectIndex<'OpError>
                         and  'SubjectId            :> SubjectId
                         and  'SubjectId            : comparison> () : EggShellSubjectGrainsCodec =

    // assert all grains params and return value types are covered by serializers

    // FIXME - awful hack to reflect ISubjectGrain & other types defined in LibLifeCycleHost (will be skipped on client side)
    let maybeLifeCycleHostAssembly =
        AppDomain.CurrentDomain.GetAssemblies()
        |> Seq.tryFind (fun a -> a.GetName().Name = "LibLifeCycleHost")

    let maybeISubjectGrainTypeDef =
        maybeLifeCycleHostAssembly
        |> Option.map (fun a -> a.GetType("LibLifeCycleHost.ISubjectGrain`6")) // 6 is number of params

    let maybeISubjectGrainObserverTypeDef =
        maybeLifeCycleHostAssembly
        |> Option.map (fun a -> a.GetType("LibLifeCycleHost.ISubjectGrainObserver`2")) // 2 is number of params

    let maybeIDynamicSubscriptionDispatcherGrainType =
        maybeLifeCycleHostAssembly
        |> Option.map (fun a -> a.GetType("LibLifeCycleHost.IDynamicSubscriptionDispatcherGrain"))

    let untypedSerializers =
        getUntypedSubjectSerializers<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent, 'SubjectIndex, 'SubjectId>()
        |> fun serializers ->

            let supportedTypes =
                serializers
                |> Seq.map (fun x -> x.Type, x.TypeId)
                |> readOnlyDict

            let declaredGrainParamAndRetValTypes =
                [
                    yield typeof<ISubjectClientGrain<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent, 'SubjectId>>
                    yield typeof<ISubjectRepoGrain<'Subject, 'LifeAction, 'Constructor, 'SubjectId, 'SubjectIndex, 'OpError>>
                    yield typeof<IBlobRepoGrain>
                    yield typeof<ISubjectIdGenerationGrain<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent, 'SubjectId>>
                    yield typeof<ISubjectReflectionGrain<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent, 'SubjectId>>
                    yield typeof<ILifeEventAwaiter<'Subject, 'LifeEvent, 'SubjectId>>

                    match maybeISubjectGrainTypeDef with
                    | None -> ()
                    | Some iSubjectGrainTypeDef ->
                        yield iSubjectGrainTypeDef.MakeGenericType(
                            [| typeof<'Subject>; typeof<'LifeAction>; typeof<'OpError>; typeof<'Constructor>; typeof<'LifeEvent>; typeof<'SubjectId> |])

                    match maybeISubjectGrainObserverTypeDef with
                    | None -> ()
                    | Some iSubjectGrainObserverTypeDef ->
                        yield iSubjectGrainObserverTypeDef.MakeGenericType(
                            [| typeof<'Subject>; typeof<'SubjectId> |])

                    match maybeIDynamicSubscriptionDispatcherGrainType with
                    | None -> ()
                    | Some iDynamicSubscriptionDispatcherGrainType ->
                        yield iDynamicSubscriptionDispatcherGrainType
                ]
                |> Seq.collect (fun typ -> typ.GetMethods ())
                |> Seq.collect (
                    fun method ->
                        seq {
                            yield! method.GetParameters() |> Seq.map (fun p -> p.ParameterType)
                            let retType = method.ReturnType
                            yield
                                if retType.IsGenericType && retType.GetGenericTypeDefinition() = typedefof<Task<_>> then
                                    retType.GetGenericArguments().[0]
                                else
                                    retType
                        })
                |> Seq.map (fun t -> t, ())
                |> readOnlyDict

            // Orleans 10 decomposes Option/ValueOption/Choice/Result/tuples natively, so the declared
            // grain param/return types must be peeled to their bare leaves before checking coverage.
            let declaredLeafTypes =
                declaredGrainParamAndRetValTypes.Keys
                |> Seq.collect decomposeToNativeLeaves
                |> Seq.map (fun t -> t, ())
                |> readOnlyDict

            // types Orleans serializes without our help: native leaf types + the grain-observer
            // interfaces (which implement Orleans.IGrainObserver)
            let isOrleansHandledLeaf (typ: Type) =
                orleansNativeLeafTypes.Contains typ
                || typ = typeof<ILifeEventAwaiter<'Subject, 'LifeEvent, 'SubjectId>>
                || (match maybeISubjectGrainObserverTypeDef with
                    | None -> false
                    | Some iSubjectGrainObserverTypeDef ->
                        typ = iSubjectGrainObserverTypeDef.MakeGenericType([| typeof<'Subject>; typeof<'SubjectId> |]))

            // also assert that no redundant serializers (only inside host, because client will have a few)
            match maybeISubjectGrainTypeDef with
            | None -> ()
            | Some _ ->
                supportedTypes
                |> Seq.filter (fun kvp -> kvp.Key |> declaredLeafTypes.ContainsKey |> not)
                |> List.ofSeq
                |>
                    function
                    | [] -> ()
                    | redundantTypesWithTypeIds ->
                        let typesMessage =
                            redundantTypesWithTypeIds
                            |> List.fold
                                (fun (sb: StringBuilder) kvp ->
                                    sb
                                        .Append(" - ")
                                        .Append("type ID ")
                                        .Append(kvp.Value)
                                        .Append(": ")
                                        .Append(kvp.Key.FullName)
                                        .AppendLine()
                                )
                                (StringBuilder())
                            |> (fun sb -> sb.ToString())

                        failwithf "Remove redundant subject serializers for following types:\n%s" typesMessage

            declaredLeafTypes
            |> fun dict -> dict.Keys
            |> Seq.filter (not << supportedTypes.ContainsKey)
            |> Seq.filter (not << isOrleansHandledLeaf)
            |> List.ofSeq
            |>
                function
                | [] -> serializers
                | notSupportedTypes ->
                    let typesMessage =
                        notSupportedTypes
                        |> List.fold
                            (fun (sb: StringBuilder) t ->
                                sb
                                    .Append(" - ")
                                    .Append(t.FullName)
                                    .AppendLine()
                            )
                            (StringBuilder())
                        |> (fun sb -> sb.ToString())

                    failwithf "Need to define serializers for following types on subject grain interfaces (if Orleans can serialize this type without the help of a custom serializer, add an exception to the above filter):\n%s" typesMessage

    EggShellSubjectGrainsCodec(
        untypedSerializers |> Seq.map (fun untypedSer -> (untypedSer.TypeId, untypedSer)) |> readOnlyDict,
        untypedSerializers |> Seq.map (fun untypedSer -> (untypedSer.Type,   untypedSer)) |> readOnlyDict)

let buildSubjectCodecs (lifeCycleDefs: LifeCycleDef list) : EggShellSubjectGrainsCodec list =
    lifeCycleDefs
    |> List.map (fun def ->
        def.Invoke
            { new FullyTypedLifeCycleDefFunction<_> with
                member _.Invoke (_def: LifeCycleDef<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent, 'SubjectIndex, 'SubjectId>) =
                    buildSubjectCodec<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent, 'SubjectIndex, 'SubjectId>() })

let getSummaryEncoders<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent, 'SubjectIndex, 'SubjectId
                             when 'Subject              :> Subject<'SubjectId>
                             and  'LifeAction           :> LifeAction
                             and  'OpError              :> OpError
                             and  'Constructor          :> Constructor
                             and  'LifeEvent            :> LifeEvent
                             and  'LifeEvent            : comparison
                             and  'SubjectIndex         :> SubjectIndex<'OpError>
                             and  'SubjectId            :> SubjectId
                             and  'SubjectId            : comparison> () : seq<Type * (obj -> string)> =

    getUntypedSubjectSerializers<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent, 'SubjectIndex, 'SubjectId>()
    |> Seq.map (fun x -> (x.Type, x.ToSummary))
    // append summarizers for types not present in grain contract but still useful for logging
    |> Seq.append (
        seq {
            yield summarizerForType (fun (e: 'OpError) -> e :> OpError)
            yield summarizerForType (fun (e: GrainTriggerSubscriptionError<'OpError>) -> GrainTriggerSubscriptionError.CastUnsafe e)
            yield summarizerForType id<GrainTriggerDynamicSubscriptionError>
            yield summarizerForType id<GrainEnqueueActionError>
            yield summarizerForType (fun (e: GrainIdGenerationError<'OpError>) -> GrainIdGenerationError.CastUnsafe e)
            yield summarizerForType (fun (e: GrainMaybeConstructionError<'OpError>) -> GrainMaybeConstructionError.CastUnsafe e)
            yield summarizerForType (fun (e: GrainPrepareConstructionError<'OpError>) -> GrainPrepareConstructionError.CastUnsafe e)
            yield summarizerForType (fun (e: GrainPrepareTransitionError<'OpError>) -> GrainPrepareTransitionError.CastUnsafe e)
            yield summarizerForType id<GrainGetError>
            yield summarizerForType id<LocalSubjectPKeyReference>
            yield summarizerForType (fun (actions: NonemptyList<'LifeAction>) -> actions.ToList |> List.map (fun action -> action :> LifeAction))
        })

let private getUntypedViewSerializers<'Input, 'Output, 'OpError
                                        when 'Input :> ViewInput<'Input>
                                        and  'Output :> ViewOutput<'Output>
                                        and  'OpError :> ViewOpError<'OpError>
                                        and  'OpError :> OpError> () =

    // List out all IViewGrain parameters and response types
    // NOTES:
    // * the type ID mapping can *never* change. Do not re-use. If you ever remove a serializer
    //   simply comment out that line, so others don't accidentally re-use

    [
        UntypedSerializer.ForIsomorphicType 1uy
            id<ClientGrainCallContext>
            id<ClientGrainCallContext>
            id<_>

        UntypedSerializer.ForConcreteTypeWithExplicitCodec 2uy
            ('Input.Codec())

//      UntypedSerializer.ForConcreteTypeWithExplicitCodec 3uy  // was Result<'Output, GrainExecutionError<'OpError>>; Orleans 10 decomposes Result, leaves re-registered at 4 + 5

        UntypedSerializer.ForConcreteTypeWithExplicitCodec 4uy
            ('Output.Codec())

        UntypedSerializer.ForConcreteTypeWithExplicitCodec 5uy
            (GrainExecutionError<'OpError>.CodecWithExplicitOpError ('OpError.Codec()))
    ]
    |> fun untypedSerializers ->
        // assert no duplicate Ids before return
        match untypedSerializers |> Seq.groupBy (fun s -> s.TypeId) |> Seq.filter (fun (_, g) -> (Seq.length g) > 1) |> Seq.map fst |> Seq.tryHead with
        | None ->
            untypedSerializers
        | Some duplicateTypeId ->
            failwithf "Duplicate typeId in view serializers: %d" duplicateTypeId

let private buildViewCodec<'Input, 'Output, 'OpError
                         when 'Input :> ViewInput<'Input>
                         and  'Output :> ViewOutput<'Output>
                         and  'OpError :> ViewOpError<'OpError>
                         and  'OpError :> OpError> () : EggShellSubjectGrainsCodec =

    // assert all grains params and return value types are covered by serializers

    // FIXME - awful hack to reflect IViewGrain & other types defined in LibLifeCycleHost (will be skipped on client side)
    let maybeIViewGrainTypeDef =
        AppDomain.CurrentDomain.GetAssemblies()
        |> Seq.tryFind (fun a -> a.GetName().Name = "LibLifeCycleHost")
        |> Option.map (fun a -> a.GetType("LibLifeCycleHost.IViewGrain`3")) // 3 is number of params

    let untypedSerializers =
        getUntypedViewSerializers<'Input, 'Output, 'OpError>()
        |> fun serializers ->

            let supportedTypes =
                serializers
                |> Seq.map (fun x -> x.Type, x.TypeId)
                |> readOnlyDict

            let declaredGrainParamAndRetValTypes =
                [
                    yield typeof<IViewClientGrain<'Input, 'Output, 'OpError>>
                    match maybeIViewGrainTypeDef with
                    | None -> ()
                    | Some iViewGrainTypeDef ->
                        yield iViewGrainTypeDef.MakeGenericType(
                            [| typeof<'Input>; typeof<'Output>; typeof<'OpError> |])
                ]
                |> Seq.collect (fun typ -> typ.GetMethods ())
                |> Seq.collect (
                    fun method ->
                        seq {
                            yield! method.GetParameters() |> Seq.map (fun p -> p.ParameterType)
                            let retType = method.ReturnType
                            yield
                                if retType.IsGenericType && retType.GetGenericTypeDefinition() = typedefof<Task<_>> then
                                    retType.GetGenericArguments().[0]
                                else
                                    retType
                        })
                |> Seq.map (fun t -> t, ())
                |> readOnlyDict

            // Orleans 10 decomposes Option/ValueOption/Choice/Result/tuples natively, so peel the
            // declared grain param/return types to their bare leaves before checking coverage.
            let declaredLeafTypes =
                declaredGrainParamAndRetValTypes.Keys
                |> Seq.collect decomposeToNativeLeaves
                |> Seq.map (fun t -> t, ())
                |> readOnlyDict

            // also assert that no redundant serializers (only inside host, because client will have a few)
            match maybeIViewGrainTypeDef with
            | None -> ()
            | Some _ ->
                supportedTypes
                |> Seq.filter (fun kvp -> kvp.Key |> declaredLeafTypes.ContainsKey |> not)
                |> List.ofSeq
                |>
                    function
                    | [] -> ()
                    | redundantTypesWithTypeIds ->
                        let typesMessage =
                            redundantTypesWithTypeIds
                            |> List.fold
                                (fun (sb: StringBuilder) kvp ->
                                    sb
                                        .Append(" - ")
                                        .Append("type ID ")
                                        .Append(kvp.Value)
                                        .Append(": ")
                                        .Append(kvp.Key.FullName)
                                        .AppendLine()
                                )
                                (StringBuilder())
                            |> (fun sb -> sb.ToString())

                        failwithf "Remove redundant view serializers for following types:\n%s" typesMessage

            declaredLeafTypes
            |> fun dict -> dict.Keys
            |> Seq.filter (not << supportedTypes.ContainsKey)
            |> Seq.filter (not << orleansNativeLeafTypes.Contains)
            |> List.ofSeq
            |>
                function
                | [] -> serializers
                | notSupportedTypes ->
                    let typesMessage =
                        notSupportedTypes
                        |> List.fold
                            (fun (sb: StringBuilder) t ->
                                sb
                                    .Append(" - ")
                                    .Append(t.FullName)
                                    .AppendLine()
                            )
                            (StringBuilder())
                        |> (fun sb -> sb.ToString())

                    failwithf "Need to define serializers for following types on view grain interfaces (if Orleans can serialize this type without the help of a custom serializer, add an exception to the above filter):\n%s" typesMessage

    EggShellSubjectGrainsCodec(
        untypedSerializers |> Seq.map (fun untypedSer -> (untypedSer.TypeId, untypedSer)) |> readOnlyDict,
        untypedSerializers |> Seq.map (fun untypedSer -> (untypedSer.Type,   untypedSer)) |> readOnlyDict)

type private ViewCodecBuilder =
    static member Build<'Input, 'Output, 'OpError
        when 'Input :> ViewInput<'Input>
        and  'Output :> ViewOutput<'Output>
        and  'OpError :> ViewOpError<'OpError>
        and  'OpError :> OpError>
        () : EggShellSubjectGrainsCodec =
        buildViewCodec<'Input, 'Output, 'OpError>()

let buildViewCodecs (viewDefs: IViewDef list) : EggShellSubjectGrainsCodec list =
    let viewInputDef = typedefof<ViewInput<NoInput>>
    let viewOutputDef = typedefof<ViewOutput<NoOutput>>
    let buildMethod =
        typeof<ViewCodecBuilder>.GetMethod(
            "Build",
            System.Reflection.BindingFlags.Static ||| System.Reflection.BindingFlags.Public ||| System.Reflection.BindingFlags.NonPublic)
    viewDefs
    |> List.choose (fun def ->
        let viewDefType = def.GetType()
        let typeArgs = viewDefType.GenericTypeArguments
        if typeArgs.Length = 3 then
            let inputType = typeArgs.[0]
            let outputType = typeArgs.[1]
            let _opErrorType = typeArgs.[2]
            let inputOk =
                inputType.GetInterfaces()
                |> Seq.exists (fun it -> it.IsGenericType && it.GetGenericTypeDefinition() = viewInputDef && it.GenericTypeArguments.[0] = inputType)
            let outputOk =
                outputType.GetInterfaces()
                |> Seq.exists (fun it -> it.IsGenericType && it.GetGenericTypeDefinition() = viewOutputDef && it.GenericTypeArguments.[0] = outputType)
            if inputOk && outputOk then
                buildMethod.MakeGenericMethod(typeArgs)
                    .Invoke(null, [| |])
                    :?> EggShellSubjectGrainsCodec
                |> Some
            else
                None
        else
            None)
