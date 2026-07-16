namespace LibUiSubject.Services.SubjectService

#if DEBUG

open System
open Fable.Core
open LibClient
open LibClient.Services.Subscription
open LibUiSubject.Types
open LibLifeCycleTypes.Api.V1

[<AutoOpen>]
module private FakeSubjectServiceHelpers =
    let log message = Browser.Dom.console.log message

type private Subscriber<'Projection> = AsyncData<'Projection> -> unit

type private SubscriberMany<'Projection, 'Id when 'Projection :> SubjectProjection<'Id> and 'Id :> SubjectId and 'Id: comparison> =
    AsyncData<Subjects<'Id, 'Projection>> -> unit

type private SubscriberManyWithCount<'Projection, 'Id when 'Projection :> SubjectProjection<'Id> and 'Id :> SubjectId and 'Id: comparison> =
    AsyncData<SubjectsWithTotalCount<'Id, 'Projection>> -> unit

[<RequireQualifiedAccess>]
type FakeDelay =
    | NoDelay
    | Fixed of DelayMs: int
    | Random of Random: (unit -> int)
    member this.Wait() =
        let delayMs =
            match this with
            | FakeDelay.NoDelay       -> 0
            | FakeDelay.Fixed delayMs -> delayMs
            | FakeDelay.Random fn     -> fn ()

        async { do! Async.Sleep delayMs }

/// A subject service that serves fake subjects.
///
/// Limitations:
///   - no support for ordering indexed queries (can be done, just not worth the effort right now)
type FakeSubjectService<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError
    when 'Subject :> Subject<'Id>
    and 'Projection :> SubjectProjection<'Id>
    and 'Projection: equality
    and 'Id :> SubjectId
    and 'Id: comparison
    and 'Constructor :> Constructor
    and 'Action :> LifeAction
    and 'Event :> LifeEvent
    and 'OpError :> OpError
    and 'Index :> SubjectIndex<'OpError>>(
        lifeCycleKey:    LifeCycleKey,
        fakeProjections: List<'Projection>,
        fakeDelay:       FakeDelay) as this =

    static let mutable eventQueueId = 0

    let eventQueue: EventBus.Queue<SubjectServiceEvent<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError>> =
        let result = EventBus.Queue $"fakeSubjectServiceEventQueue-{eventQueueId}"
        eventQueueId <- eventQueueId + 1
        result

    let mutable projectionsById =
        fakeProjections
        |> List.map (fun p -> this.ExtractProjectionId(p), p)
        |> Map.ofList

    let mutable subscriptionsById: Map<'Id, Map<Guid, Subscriber<'Projection>>> =
        Map.empty

    let notifySubscriberOfInitialValue id subscriberId =
        async {
            do! fakeDelay.Wait()

            match subscriptionsById |> Map.tryFind id with
            | Some subscriptions ->
                match subscriptions |> Map.tryFind subscriberId with
                | Some subscriber ->
                    match projectionsById |> Map.tryFind id with
                    | Some projection -> projection |> Available |> subscriber
                    | None            -> Unavailable |> subscriber
                | None -> ()
            | None -> ()
        }

    let addSubscriber id subscriberId subscriber =
        let updatedSubscribers =
            match subscriptionsById |> Map.tryFind id with
            | Some subscribers -> subscribers |> Map.add subscriberId subscriber
            | None             -> Map.empty |> Map.add subscriberId subscriber

        subscriptionsById <- subscriptionsById |> Map.remove id |> Map.add id updatedSubscribers

        notifySubscriberOfInitialValue id subscriberId |> Async.StartAsPromise |> ignore

    let removeSubscriber id subscriberId =
        match subscriptionsById |> Map.tryFind id with
        | Some subscribers ->
            let updatedSubscribers = subscribers |> Map.remove subscriberId

            subscriptionsById <- subscriptionsById |> Map.remove id |> Map.add id updatedSubscribers
        | None -> ()

    let notifySubscribers (projection: 'Projection) =
        match subscriptionsById |> Map.tryFind (this.ExtractProjectionId projection) with
        | Some subscribers -> subscribers |> Map.iter (fun _ s -> s (Available projection))
        | None             -> ()

    let addOrReplaceProjection (projection: 'Projection) =
        projectionsById <-
            projectionsById
            |> Map.remove (this.ExtractProjectionId projection)
            |> Map.add (this.ExtractProjectionId projection) projection

        notifySubscribers projection

    let subscribeMany (ids: OrderedSet<'Id>) (notifySubscriber: List<'Id * AsyncData<'Projection>> -> unit) =
        let subscriberId = Guid.NewGuid()

        let maybeProjections: ('Id * Option<AsyncData<'Projection>>)[] =
            ids |> OrderedSet.toArray |> Array.map (fun id -> id, None)

        let idIndexes =
            ids
            |> OrderedSet.toArray
            |> Array.mapi (fun index id -> (id, index))
            |> Map.ofArray

        let innerSubscriber id (asyncData: AsyncData<'Projection>) =
            match idIndexes |> Map.tryFind id with
            | Some index -> maybeProjections[index] <- (id, Some asyncData)
            | None       -> ()

            let projections =
                maybeProjections
                |> Array.choose (fun (id, maybeAD) ->
                    match maybeAD with
                    | Some ad -> Some(id, ad)
                    | None    -> None
                )

            if (maybeProjections.Length = projections.Length) then
                notifySubscriber (projections |> List.ofArray)

        ids
        |> OrderedSet.toSeq
        |> Seq.iter (fun id -> addSubscriber id subscriberId (innerSubscriber id))

        { Off = (fun () -> ids |> OrderedSet.toSeq |> Seq.iter (fun id -> removeSubscriber id subscriberId)) }

    let failWithMissingMethodImplementation methodName maybeExtraInformation =
        // TODO: would be nice to include projection type in the message (typeof<'Projection>.Name), but Fable type erasure is making it painful
        let extraInformation =
            maybeExtraInformation
            |> Option.map (fun extraInformation -> $" {extraInformation}")
            |> Option.defaultValue ""
        $"A fake subject service for life cycle {lifeCycleKey.LocalLifeCycleName} is being used in a way that necessitates an implementation of the {methodName} method. You will need to override the default implementation.{extraInformation}"
        |> failwith

    member _.FakeProjections = fakeProjections

    abstract member ExtractProjectionId: 'Projection -> 'Id
    abstract member ConstructCore:       'Constructor -> Result<'Projection, ActionOrConstructionError<'OpError>>
    abstract member GetOneCore:          'Id -> AsyncData<'Projection>
    abstract member GetAllCore:          ResultSetOptions<'SubjectIndex> -> AsyncData<SubjectsWithTotalCount<'Id, 'Projection>>
    abstract member GetIndexedCore:      IndexQuery<'Index> -> AsyncData<SubjectsWithTotalCount<'Id, 'Projection>>
    abstract member ActCore:             'Projection -> 'Action -> Async<Result<'Projection, ActionOrConstructionError<'OpError>>>
    // TODO: should this return JS.Promise<'Event> instead?
    abstract member WaitCore:                       'Event -> Async<ApiActOrConstructAndWaitOnLifeEventResult<'Projection, 'Event>>
    abstract member GetIndexQueryResults:           seq<'Projection> -> UntypedPredicate -> seq<'Projection>
    abstract member SortProjectionsBy:              seq<'Projection> -> string -> OrderDirection -> seq<'Projection>
    abstract member ShouldRemoveProjectionAfterAct: 'Action -> 'Projection -> bool

    default _.ExtractProjectionId projection = projection.SubjectId

    default _.ShouldRemoveProjectionAfterAct _ _ = false

    default this.ConstructCore ctor =
        failWithMissingMethodImplementation (nameof this.ConstructCore) (Some $"Constructor: {ctor}")

    default _.GetOneCore id =
        projectionsById
        |> Map.tryFind id
        |> Option.mapOrElse Unavailable (fun s -> Available s)

    default this.GetAllCore (resultSetOptions: ResultSetOptions<'SubjectIndex>) =
        let projections = projectionsById.Values
        let sortedProjections = this.SortProjections projections resultSetOptions.Options.OrderBy
        let page = resultSetOptions.Options.Page
        let pagedProjections =
            sortedProjections
            |> Seq.skip (int page.Offset)
            |> Seq.truncate (int page.Size)

        {
            Subjects =
                pagedProjections
                |> Seq.map (fun projection -> (projection.SubjectId, this.GetOneCore projection.SubjectId))
            TotalCount =
                projections
                |> Seq.length
                |> uint64
        }
        |> Available

    default this.GetIndexedCore (query: IndexQuery<'Index>) =
        let projections = this.GetIndexQueryResults projectionsById.Values query.Predicate
        let sortedProjections = this.SortProjections projections query.ResultSetOptions.OrderBy
        let page = query.ResultSetOptions.Page
        let pagedProjections =
            sortedProjections
            |> Seq.skip (int page.Offset)
            |> Seq.truncate (int page.Size)

        {
            Subjects =
                pagedProjections
                |> Seq.map (fun projection -> (projection.SubjectId, this.GetOneCore projection.SubjectId))
            TotalCount =
                projections
                |> Seq.length
                |> uint64
        }
        |> Available

    default this.ActCore _ action =
        failWithMissingMethodImplementation (nameof this.ActCore) (Some $"Action: {action}")

    default this.WaitCore event =
        failWithMissingMethodImplementation (nameof this.WaitCore) (Some $"Event: {event}")

    default this.GetIndexQueryResults _ untypedPredicate =
        failWithMissingMethodImplementation (nameof this.GetIndexQueryResults) (Some $"Untyped predicate: {untypedPredicate}")

    default this.SortProjectionsBy _ key direction =
        failWithMissingMethodImplementation (nameof this.SortProjectionsBy) (Some $"Key: {key}, direction: {direction}")

    member this.SortProjections (projections: seq<'Projection>) (orderBy: UntypedOrderBy): seq<'Projection> =
        match orderBy with
        | UntypedOrderBy.FastestOrSingleSearchScoreIfAvailable ->
            projections
        | UntypedOrderBy.SubjectId direction ->
            match direction with
            | OrderDirection.Ascending ->
                projections
                |> Seq.sortBy (fun projection -> projection.SubjectId)
            | OrderDirection.Descending ->
                projections
                |> Seq.sortByDescending (fun projection -> projection.SubjectId)
        | UntypedOrderBy.Random ->
            let rnd = Random()
            projections
            |> Seq.map (fun projection -> projection, (rnd.Next()))
            |> Seq.sortBy snd
            |> Seq.map fst
        | UntypedOrderBy.NumericIndexEntry (key, direction)
        | UntypedOrderBy.StringIndexEntry (key, direction) ->
            this.SortProjectionsBy projections key direction

    member this.RemoveProjection (id: 'Id) =
        projectionsById <- projectionsById |> Map.remove id

        match subscriptionsById |> Map.tryFind id with
        | Some subscribers -> subscribers |> Map.iter (fun _ subscriber -> Unavailable |> subscriber)
        | None             -> ()

    member this.ConstructAndUpdateFakeProjections ctor =
        let result = this.ConstructCore ctor

        match result with
        | Ok projection -> addOrReplaceProjection projection
        | Error _       -> ()

        result

    member this.GetMaybeConstructAndUpdateFakeProjections id ctor =
        let exists = projectionsById |> Map.containsKey id

        match exists with
        | true -> this.GetOneCore id
        | false ->
            match this.ConstructAndUpdateFakeProjections ctor with
            | Ok projection -> Available projection
            | Error opError -> $"Failed to construct: {opError}" |> UnknownFailure |> Failed

    member private this.ActAndUpdateFakeProjections id action =
        async {
            // We have to assume the projection is in the map at this point because we don't currently mock HTTP errors, which is what the server
            // would send when e.g. acting on a non-existant projection.
            match projectionsById |> Map.tryFind id with
            | Some projection ->
                let! result = this.ActCore projection action

                match result with
                | Ok projection ->
                    if this.ShouldRemoveProjectionAfterAct action projection then
                        this.RemoveProjection id
                    else
                        addOrReplaceProjection projection
                | Error _ -> ()

                return result |> Result.map (fun _ -> ())
            | None ->
                let message = $"No fake projection with ID %A{id} found"
                log message
                return failwith message
        }

    member private this.ActMaybeConstructAndUpdateFakeProjections id action ctor =
        async {
            let exists = projectionsById |> Map.containsKey id

            return!
                match exists with
                | true -> this.ActAndUpdateFakeProjections id action
                | false ->
                    this.ConstructAndUpdateFakeProjections ctor
                    |> Result.map (fun _ -> ())
                    |> Async.Of
        }

    member private this.ActWaitAndUpdateFakeProjections id action event =
        async {
            let! result = this.ActAndUpdateFakeProjections id action

            match result with
            | Ok _ ->
                let! waitResult = this.WaitCore event
                return Ok waitResult
            | Error e -> return Error e
        }

    member this.ConstructAndWait ctor event =
        async {
            let result = this.ConstructAndUpdateFakeProjections ctor

            match result with
            | Ok _ ->
                let! waitResult = this.WaitCore event
                return Ok waitResult
            | Error e -> return Error e
        }

    member this.ActMaybeConstructAndWaitAndUpdateFakeProjections id action ctor event =
        async {
            let! result = this.ActMaybeConstructAndUpdateFakeProjections id action ctor

            match result with
            | Ok _ ->
                let! waitResult = this.WaitCore event
                return Ok waitResult
            | Error e -> return Error e
        }

    member _.SubscribeOne id subscriber =
        let subscriberId = Guid.NewGuid()
        addSubscriber id subscriberId subscriber
        { Off = (fun () -> removeSubscriber id subscriberId) }

    member _.SubscribeMany (ids: OrderedSet<'Id>) (subscriber: SubscriberMany<'Projection, 'Id>) =
        if ids.IsEmpty then
            Seq.empty<'Id * AsyncData<'Projection>>
            |> Available
            |> subscriber

            { Off = id }
        else
            subscribeMany
                ids
                (fun projections ->
                    projections
                    |> List.toSeq
                    |> Available
                    |> subscriber
                )

    member _.SubscribeManyWithCount (ids: OrderedSet<'Id>) (totalCount: uint64) (subscriber: SubscriberManyWithCount<'Projection, 'Id>) =
        if ids.IsEmpty then
            {
                Subjects   = Seq.empty<'Id * AsyncData<'Projection>>
                TotalCount = 0UL
            }
            |> Available
            |> subscriber

            { Off = id }
        else
            subscribeMany
                ids
                (fun projections ->
                    {
                        Subjects   = projections |> List.toSeq
                        TotalCount = totalCount
                    }
                    |> Available
                    |> subscriber
                )


    interface ISubjectService<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError> with
        member _.LifeCycleKey = lifeCycleKey

        member _.EventQueue = eventQueue

        override this.Construct(ctor: 'Constructor) : Async<Result<'Projection, ActionOrConstructionError<'OpError>>> =
            async {
                do! fakeDelay.Wait()
                return this.ConstructAndUpdateFakeProjections ctor
            }

        override this.GetMaybeConstruct (_: UseCache) (id: 'Id) (ctor: 'Constructor) : Async<AsyncData<'Projection>> =
            async {
                do! fakeDelay.Wait()
                return this.GetMaybeConstructAndUpdateFakeProjections id ctor
            }

        override this.GetOne (_: UseCache) (id: 'Id) : Async<AsyncData<'Projection>> =
            async {
                do! fakeDelay.Wait()
                return this.GetOneCore id
            }

        override _.GetAll (_: UseCache) (resultSetOptions: ResultSetOptions<'Index>) : Async<AsyncData<Subjects<'Id, 'Projection>>> =
            async {
                do! fakeDelay.Wait()
                return
                    this.GetAllCore resultSetOptions
                    |> AsyncData.map SubjectsWithTotalCount.subjects
            }

        override _.GetAllWithTotalCount (_: UseCache) (resultSetOptions: ResultSetOptions<'Index>) : Async<AsyncData<SubjectsWithTotalCount<'Id, 'Projection>>> =
            async {
                do! fakeDelay.Wait()
                return this.GetAllCore resultSetOptions
            }

        override _.GetAllCount (_: UseCache) : Async<AsyncData<uint64>> =
            async {
                do! fakeDelay.Wait()
                return
                    this.GetAllCore (ResultSetOptions<'Index>.OrderByFastestWithPage { Size = 0us; Offset = 0UL })
                    |> AsyncData.map SubjectsWithTotalCount.totalCount
            }

        override _.GetMany (_useCache: UseCache) (_maybeEmptyIds: Set<'Id>) : Async<AsyncData<Subjects<'Id, 'Projection>>> =
            failwith "Not implemented"

        override this.GetIndexed (_: UseCache) (query: IndexQuery<'Index>) : Async<AsyncData<Subjects<'Id, 'Projection>>> =
            async {
                do! fakeDelay.Wait()
                return
                    this.GetIndexedCore query
                    |> AsyncData.map SubjectsWithTotalCount.subjects
            }

        override this.GetIndexedWithTotalCount (_: UseCache) (query: IndexQuery<'Index>) : Async<AsyncData<SubjectsWithTotalCount<'Id, 'Projection>>> =
            async {
                do! fakeDelay.Wait()
                return
                    this.GetIndexedCore query
            }

        override this.GetIndexedCount (_: UseCache) (predicate: PreparedIndexPredicate<'Index>) : Async<AsyncData<uint64>> =
            async {
                do! fakeDelay.Wait()

                let count =
                    this.GetIndexQueryResults projectionsById.Values predicate.Predicate
                    |> Seq.length
                    |> uint64

                return Available count
            }

        override this.PauseSubscriptions () : IDisposable =
            { new IDisposable with
                member _.Dispose() =
                    Noop
            }

        override this.Act (id: 'Id) (action: 'Action) : Async<Result<unit, ActionOrConstructionError<'OpError>>> =
            async {
                do! fakeDelay.Wait()
                return! this.ActAndUpdateFakeProjections id action
            }

        override this.ActMaybeConstruct (id: 'Id) (action: 'Action) (ctor: 'Constructor) : Async<Result<unit, ActionOrConstructionError<'OpError>>> =
            async {
                do! fakeDelay.Wait()
                return! this.ActMaybeConstructAndUpdateFakeProjections id action ctor
            }

        override this.ConstructAndWait
            (ctor: 'Constructor)
            (event: 'Event)
            : Async<Result<ApiActOrConstructAndWaitOnLifeEventResult<'Projection, 'Event>, ActionOrConstructionError<'OpError>>> =
            async {
                do! fakeDelay.Wait()
                return! this.ConstructAndWait ctor event
            }

        override this.ActWait
            (id: 'Id)
            (action: 'Action)
            (event: 'Event)
            : Async<Result<ApiActOrConstructAndWaitOnLifeEventResult<'Projection, 'Event>, ActionOrConstructionError<'OpError>>> =
            async {
                do! fakeDelay.Wait()
                return! this.ActWaitAndUpdateFakeProjections id action event
            }

        member this.ActWaitWithTimeout
            (id: 'Id)
            (action: 'Action)
            (event: 'Event)
            (_timeout: TimeSpan) // ignore timeout in fake service
            : Async<Result<ApiActOrConstructAndWaitOnLifeEventResult<'Projection, 'Event>, ActionOrConstructionError<'OpError>>> =
            async {
                do! fakeDelay.Wait()
                return! this.ActWaitAndUpdateFakeProjections id action event
            }


        override this.ActMaybeConstructAndWait
            (id: 'Id)
            (action: 'Action)
            (ctor: 'Constructor)
            (event: 'Event)
            : Async<Result<ApiActOrConstructAndWaitOnLifeEventResult<'Projection, 'Event>, ActionOrConstructionError<'OpError>>> =
            async {
                do! fakeDelay.Wait()
                return! this.ActMaybeConstructAndWaitAndUpdateFakeProjections id action ctor event
            }

        override this.SubscribeOne (id: 'Id) (_: UseCache) (subscriber: Subscriber<'Projection>) : SubscribeResult = this.SubscribeOne id subscriber

        override this.SubscribeMany (ids: Set<'Id>) (_: UseCache) (subscriber: SubscriberMany<'Projection, 'Id>) : SubscribeResult =
            this.SubscribeMany (ids |> OrderedSet.ofSeq) subscriber

        override this.SubscribeAll (resultSetOptions: ResultSetOptions<'Index>) (_: UseCache) (subscriber: SubscriberMany<'Projection, 'Id>) : SubscribeResult =
            let projections = projectionsById |> Map.values
            let sortedProjections = this.SortProjections projections resultSetOptions.Options.OrderBy
            let ids =
                sortedProjections
                |> Seq.map (fun projection -> projection.SubjectId)
            let page = resultSetOptions.Options.Page

            let matchingIds =
                ids
                |> Seq.skip (int page.Offset)
                |> Seq.truncate (int page.Size)
                |> OrderedSet.ofSeq

            this.SubscribeMany matchingIds subscriber

        override this.SubscribeAllWithTotalCount
            (resultSetOptions: ResultSetOptions<'Index>)
            (_: UseCache)
            (subscriber: SubscriberManyWithCount<'Projection, 'Id>)
            : SubscribeResult =
            let projections = projectionsById |> Map.values
            let sortedProjections = this.SortProjections projections resultSetOptions.Options.OrderBy
            let ids =
                sortedProjections
                |> Seq.map (fun projection -> projection.SubjectId)
            let page = resultSetOptions.Options.Page

            let matchingIds =
                ids
                |> Seq.skip (int page.Offset)
                |> Seq.truncate (int page.Size)
                |> OrderedSet.ofSeq

            this.SubscribeManyWithCount matchingIds (uint64 matchingIds.Count) subscriber

        override this.SubscribeIndexed (query: IndexQuery<'Index>) (_: UseCache) (subscriber: SubscriberMany<'Projection, 'Id>) : SubscribeResult =
            let projections = this.GetIndexQueryResults projectionsById.Values query.Predicate
            let sortedProjections = this.SortProjections projections query.ResultSetOptions.OrderBy
            let page = query.ResultSetOptions.Page

            let matchingIds =
                sortedProjections
                |> Seq.skip (int page.Offset)
                |> Seq.truncate (int page.Size)
                |> Seq.map (fun projection -> projection.SubjectId)
                |> OrderedSet.ofSeq

            this.SubscribeMany matchingIds subscriber

        override this.SubscribeIndexedWithTotalCount
            (query: IndexQuery<'Index>)
            (_: UseCache)
            (subscriber: SubscriberManyWithCount<'Projection, 'Id>)
            : SubscribeResult =
            let projections = this.GetIndexQueryResults projectionsById.Values query.Predicate
            let sortedProjections = this.SortProjections projections query.ResultSetOptions.OrderBy
            let page = query.ResultSetOptions.Page

            let matchingIds =
                sortedProjections
                |> Seq.skip (int page.Offset)
                |> Seq.truncate (int page.Size)
                |> Seq.map (fun projection -> projection.SubjectId)
                |> OrderedSet.ofSeq

            this.SubscribeManyWithCount matchingIds (matchingIds.Count |> uint64) subscriber

        member this.SubscribeQuery (query: Query<'Id, 'Index, 'OpError>) (useCache: UseCache) (subscriber: SubscriberMany<'Projection, 'Id>) : SubscribeResult =
            let castThis =
                (this :> ISubjectService<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError>)

            let result =
                match query with
                | Query.All resultSetOptions -> castThis.SubscribeAll resultSetOptions useCache subscriber
                | Query.Indexed indexQuery -> castThis.SubscribeIndexed indexQuery useCache subscriber
                | Query.One id ->
                    // The ugly lambda is to translate from the Subscriber<'Projection> required by SubscribeOne into the SubscriberMany<'Projection, 'Id>
                    // we actually have.
                    castThis.SubscribeOne
                        id
                        useCache
                        (fun projectionAd ->
                            projectionAd
                            |> AsyncData.map (fun projection -> seq { (projection.SubjectId, AsyncData.Available projection) })
                            |> subscriber
                        )

            result

        member this.SubscribeQueryWithTotalCount
            (query: Query<'Id, 'Index, 'OpError>)
            (useCache: UseCache)
            (subscriber: SubscriberManyWithCount<'Projection, 'Id>)
            : SubscribeResult =
            let castThis =
                (this :> ISubjectService<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError>)

            let result =
                match query with
                | Query.All resultSetOptions -> castThis.SubscribeAllWithTotalCount resultSetOptions useCache subscriber
                | Query.Indexed indexQuery -> castThis.SubscribeIndexedWithTotalCount indexQuery useCache subscriber
                | Query.One id ->
                    // The ugly lambda is to translate from the Subscriber<'Projection> required by SubscribeOne into the SubscriberManyWithCount<'Projection, 'Id>
                    // we actually have.
                    castThis.SubscribeOne
                        id
                        useCache
                        (fun projectionAd ->
                            projectionAd
                            |> AsyncData.map (fun projection ->
                                {
                                    Subjects   = seq { (projection.SubjectId, AsyncData.Available projection) }
                                    TotalCount = 1UL
                                }
                            )
                            |> subscriber
                        )

            result

        member this.GetAuditSnapshot (_idString: string) (_version: uint64) : Async<AsyncData<TemporalSnapshot<'Subject, 'Action, 'Constructor, 'Id>>> =
            failwith "Not implemented"

#endif
