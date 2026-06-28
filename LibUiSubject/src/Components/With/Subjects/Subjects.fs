[<AutoOpen>]
module LibUiSubject.Components.With.Subjects

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Subscribe
open LibLifeCycleTypes.SubjectTypes
open LibUiSubject
open LibUiSubject.Components.Constructors

type By<'Subject, 'Projection, 'Id, 'Index, 'Action, 'Event, 'OpError
                        when 'Subject      :> Subject<'Id>
                        and  'Projection   :> SubjectProjection<'Id>
                        and  'Id           :> SubjectId
                        and  'Id           :  comparison
                        and  'Action       :> LifeAction
                        and  'Event        :> LifeEvent
                        and  'OpError      :> OpError
                        and  'Index        :> SubjectIndex<'OpError>> =
| All     of ResultSetOptions<'Index>
| Ids     of Set<'Id>
| Indexed of Query: IndexQuery<'Index>
with
    member this.MakeSubscription
        (service: LibUiSubject.Services.SubjectService.ISubjectService<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError>)
        (useCache: UseCache)
        : ((AsyncData<Subjects<'Id, 'Projection>> -> unit) -> LibClient.Services.Subscription.SubscribeResult) =
        match this with
        | By.All resultSetOptions -> fun subscriber -> service.SubscribeAll     resultSetOptions useCache subscriber
        | By.Ids ids              -> fun subscriber -> service.SubscribeMany    ids              useCache subscriber
        | By.Indexed query        -> fun subscriber -> service.SubscribeIndexed query            useCache subscriber

let private getSubjects
    (store: SubscriptionsDataStore)
    (service: LibUiSubject.Services.SubjectService.ISubjectService<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError>)
    (useCache: UseCache)
    (by: By<'Subject, 'Projection, 'Id, 'Index, 'Action, 'Event, 'OpError>)
    : AsyncData<Subjects<'Id, 'Projection>> =
    store.Subscribe
        $"subjects-{service.LifeCycleKey.LocalLifeCycleName}-{by.ToString()}"
        (by.MakeSubscription service useCache)

type LibUiSubject.Components.Constructors.UiSubject.With with
    [<Component>]
    static member Subjects (
        service:                       LibUiSubject.Services.SubjectService.ISubjectService<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError>,
        by:                            By<'Subject, 'Projection, 'Id, 'Index, 'Action, 'Event, 'OpError>,
        whenAvailable:                 Subjects<'Id, 'Projection> -> ReactElement,
        ?whenUninitialized:            unit -> ReactElement,
        ?whenFetching:                 Option<Subjects<'Id, 'Projection>> -> ReactElement,
        ?whenFailed:                   AsyncDataFailure -> ReactElement,
        ?whenUnavailable:              unit -> ReactElement,
        ?whenAccessDenied:             unit -> ReactElement,
        ?whenElse:                     unit -> ReactElement,
        ?useCache:                     UseCache,
        ?treatFetchingSomeAsAvailable: bool,
        ?key:                          string)
        : ReactElement =
            ignore key

            let shouldTreatFetchingSomeAsAvailable = defaultArg treatFetchingSomeAsAvailable false
            let store = Subscribe.useSubscriptions ()
            let subjectsAD = getSubjects store service (defaultArg useCache UseCache.IfReasonablyFresh) by
            let maybeAdjustedSubjectAD =
                match shouldTreatFetchingSomeAsAvailable with
                | false -> subjectsAD
                | true  -> AsyncData.treatFetchingSomeAsAvailable subjectsAD

            LC.AsyncData (
                data               = maybeAdjustedSubjectAD,
                whenAvailable      = whenAvailable,
                ?whenUninitialized = whenUninitialized,
                ?whenFetching      = whenFetching,
                ?whenFailed        = whenFailed,
                ?whenUnavailable   = whenUnavailable,
                ?whenAccessDenied  = whenAccessDenied,
                ?whenElse          = whenElse
            )

    [<Component>]
    static member Subjects (
        service:   LibUiSubject.Services.SubjectService.ISubjectService<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError>,
        by:        By<'Subject, 'Projection, 'Id, 'Index, 'Action, 'Event, 'OpError>,
        content:   AsyncData<Subjects<'Id, 'Projection>> -> ReactElement,
        ?useCache: UseCache,
        ?key:      string)
        : ReactElement =
            ignore key
            let store = Subscribe.useSubscriptions ()
            let subjectsAD = getSubjects store service (defaultArg useCache UseCache.IfReasonablyFresh) by

            content subjectsAD
