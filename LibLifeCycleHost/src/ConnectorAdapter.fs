[<AutoOpen>]
module LibLifeCycleHost.ConnectorAdapter

open LibLifeCycle
open LibLifeCycleCore
open Orleans
open System
open System.Reflection

type IConnectorAdapter =
    abstract member RequestOnGrain:              hostEcosystemGrainFactory: IGrainFactory -> responseType: Type -> requestBuilder: obj (* IConnectorRequestBuilderSingleReply<'Request, 'Response>  *) -> responseMapper: obj (* IConnectorResponseMapper<'Response, 'Action> *) -> grainPartition: GrainPartition -> requestor: SubjectPKeyReference -> sideEffectId: GrainSideEffectId -> unit
    abstract member RequestMultiResponseOnGrain: hostEcosystemGrainFactory: IGrainFactory -> responseType: Type -> requestBuilder: obj (* IConnectorRequestBuilder<'Request, 'Response>  *) -> responseMapper: obj (* IConnectorResponseMapper<'Response, 'Action> *) -> grainPartition: GrainPartition -> requestor: SubjectPKeyReference -> sideEffectId: GrainSideEffectId -> unit
    abstract member ShouldSendTelemetry:         bool

// Modules can't be typeof<>'ed. But we can get their type via an actual type contained within in
type private AnchorTypeForModule = private AnchorTypeForModule of unit

let private tryGetMapperActionType (responseMapper: obj) : Type =
    let mapperInterface =
        responseMapper.GetType().GetInterfaces()
        |> Array.find (fun i -> i.IsGenericType && i.GetGenericTypeDefinition() = typedefof<IConnectorResponseMapper<_, _>>)
    mapperInterface.GetGenericArguments().[1]

let private dispatchConnectorRequestTyped<'Request, 'Env, 'Reply, 'Action when 'Request :> Request and 'Env :> Env and 'Action :> LifeAction>
        (hostEcosystemGrainFactory: IGrainFactory)
        (requestBuilder: obj)
        (responseMapper: obj)
        (GrainPartition grainPartitionGuid)
        (requestor: SubjectPKeyReference)
        (sideEffectId: GrainSideEffectId) : unit =
    let typedBuilder   = requestBuilder :?> IConnectorRequestBuilderSingleReply<'Request, 'Reply>
    let typedMapper    = responseMapper  :?> IConnectorResponseMapper<'Reply, 'Action>
    let connectorGrain = hostEcosystemGrainFactory.GetGrain<IConnectorGrain<'Request, 'Env>> grainPartitionGuid
    // Connector grains by their very nature are interacted with a fire-and-forget mechanism, so we don't need to wait for a response
    connectorGrain.SendRequest(typedBuilder, typedMapper, requestor, sideEffectId) |> ignore

let private dispatchConnectorRequest<'Request, 'Env, 'Reply when 'Request :> Request and 'Env :> Env>
        (hostEcosystemGrainFactory: IGrainFactory)
        (requestBuilder: obj)
        (responseMapper: obj)
        (grainPartition: GrainPartition)
        (requestor: SubjectPKeyReference)
        (sideEffectId: GrainSideEffectId) : unit =
    let actionType = tryGetMapperActionType responseMapper
    typeof<AnchorTypeForModule>
        .DeclaringType // The Module
        .GetMethod((nameof dispatchConnectorRequestTyped), BindingFlags.NonPublic ||| BindingFlags.Static)
        .MakeGenericMethod([| typeof<'Request>; typeof<'Env>; typeof<'Reply>; actionType |])
        .Invoke(null, [| hostEcosystemGrainFactory; requestBuilder; responseMapper; grainPartition; requestor; sideEffectId |])
        :?> unit

let private dispatchConnectorMultiResponseTyped<'Request, 'Env, 'Reply, 'Action when 'Request :> Request and 'Env :> Env and 'Action :> LifeAction>
        (hostEcosystemGrainFactory: IGrainFactory)
        (requestBuilder: obj)
        (responseMapper: obj)
        (GrainPartition grainPartitionGuid)
        (requestor: SubjectPKeyReference)
        (sideEffectId: GrainSideEffectId) : unit =
    let typedBuilder   = requestBuilder :?> IConnectorRequestBuilder<'Request, 'Reply>
    let typedMapper    = responseMapper  :?> IConnectorResponseMapper<'Reply, 'Action>
    let connectorGrain = hostEcosystemGrainFactory.GetGrain<IConnectorGrain<'Request, 'Env>> grainPartitionGuid
    connectorGrain.SendRequestMultiResponse(typedBuilder, typedMapper, requestor, sideEffectId) |> ignore

let private dispatchConnectorMultiResponse<'Request, 'Env, 'Reply when 'Request :> Request and 'Env :> Env>
        (hostEcosystemGrainFactory: IGrainFactory)
        (requestBuilder: obj)
        (responseMapper: obj)
        (grainPartition: GrainPartition)
        (requestor: SubjectPKeyReference)
        (sideEffectId: GrainSideEffectId) : unit =
    let actionType = tryGetMapperActionType responseMapper
    typeof<AnchorTypeForModule>
        .DeclaringType
        .GetMethod((nameof dispatchConnectorMultiResponseTyped), BindingFlags.NonPublic ||| BindingFlags.Static)
        .MakeGenericMethod([| typeof<'Request>; typeof<'Env>; typeof<'Reply>; actionType |])
        .Invoke(null, [| hostEcosystemGrainFactory; requestBuilder; responseMapper; grainPartition; requestor; sideEffectId |])
        :?> unit

type ConnectorAdapter<'Request, 'Env when 'Request :> Request and 'Env :> Env> = {
    Connector: Connector<'Request, 'Env>
}
with
    interface IConnectorAdapter with
        member _.RequestOnGrain
                (hostEcosystemGrainFactory: IGrainFactory)
                (responseType: Type)
                (requestBuilder: obj)
                (responseMapper: obj)
                (grainPartition: GrainPartition)
                (requestor: SubjectPKeyReference)
                (sideEffectId: GrainSideEffectId) : unit =
            typeof<AnchorTypeForModule>
                .DeclaringType // The Module
                .GetMethod((nameof dispatchConnectorRequest), BindingFlags.NonPublic ||| BindingFlags.Static)
                .MakeGenericMethod([| typeof<'Request>; typeof<'Env>; responseType |])
                .Invoke(null, [| hostEcosystemGrainFactory; requestBuilder; responseMapper; grainPartition; requestor; sideEffectId |])
                :?> unit

        member _.RequestMultiResponseOnGrain
                (hostEcosystemGrainFactory: IGrainFactory)
                (responseType: Type)
                (requestBuilder: obj)
                (responseMapper: obj)
                (grainPartition: GrainPartition)
                (requestor: SubjectPKeyReference)
                (sideEffectId: GrainSideEffectId) : unit =
            typeof<AnchorTypeForModule>
                .DeclaringType
                .GetMethod((nameof dispatchConnectorMultiResponse), BindingFlags.NonPublic ||| BindingFlags.Static)
                .MakeGenericMethod([| typeof<'Request>; typeof<'Env>; responseType |])
                .Invoke(null, [| hostEcosystemGrainFactory; requestBuilder; responseMapper; grainPartition; requestor; sideEffectId |])
                :?> unit

        member this.ShouldSendTelemetry = this.Connector.ShouldSendTelemetry

type ConnectorAdapterCollection = ConnectorAdapterCollection of Map<string, IConnectorAdapter>
with
    interface System.Collections.Generic.IEnumerable<IConnectorAdapter> with
        member this.GetEnumerator(): Collections.Generic.IEnumerator<IConnectorAdapter> =
            let (ConnectorAdapterCollection dictionary) = this
            dictionary.Values.GetEnumerator()

        member this.GetEnumerator(): Collections.IEnumerator =
            let (ConnectorAdapterCollection dictionary) = this
            dictionary.Values.GetEnumerator() :> Collections.IEnumerator

    member this.GetConnectorAdapterByName name : Option<IConnectorAdapter> =
        match this with
        | ConnectorAdapterCollection dictionary ->
            match dictionary.TryGetValue name with
            | true, adapter -> Some adapter
            | false, _      -> None

let makeConnectorAdapter (connector: Connector) =
    connector.Invoke
        { new FullyTypedConnectorFunction<_> with
            member _.Invoke (connector: Connector<'Request, 'Env>) = { Connector = connector } :> IConnectorAdapter }
