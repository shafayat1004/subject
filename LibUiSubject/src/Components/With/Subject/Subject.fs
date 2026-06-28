[<AutoOpen>]
module LibUiSubject.Components.With.Subject

open Fable.React
open LibClient
open LibClient.Components
open LibLifeCycleTypes.SubjectTypes
open LibUiSubject
open LibUiSubject.Components.Constructors

let private getSubject
    (store: Subscribe.SubscriptionsDataStore)
    (service: LibUiSubject.Services.SubjectService.ISubjectService<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError>)
    (useCache: UseCache)
    (id: 'Id)
    : AsyncData<'Projection> =
    store.Subscribe
        $"subject-{service.LifeCycleKey.LocalLifeCycleName}-{id.IdString}"
        (fun subscriber ->
            service.SubscribeOne id useCache subscriber
        )

type LibUiSubject.Components.Constructors.UiSubject.With with
    [<Component>]
    static member Subject (
        service:                       LibUiSubject.Services.SubjectService.ISubjectService<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError>,
        id:                            'Id,
        whenAvailable:                 'Projection -> ReactElement,
        ?whenUninitialized:            unit -> ReactElement,
        ?whenFetching:                 Option<'Projection> -> ReactElement,
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
            let subjectAD = getSubject store service (defaultArg useCache UseCache.IfReasonablyFresh) id
            let maybeAdjustedSubjectAD =
                match shouldTreatFetchingSomeAsAvailable with
                | false -> subjectAD
                | true  -> AsyncData.treatFetchingSomeAsAvailable subjectAD

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
    static member Subject (
        service:   LibUiSubject.Services.SubjectService.ISubjectService<'Subject, 'Projection, 'Id, 'Index, 'Constructor, 'Action, 'Event, 'OpError>,
        id:        'Id,
        content:   AsyncData<'Projection> -> ReactElement,
        ?useCache: UseCache,
        ?key:      string)
        : ReactElement =
            ignore key
            let store = Subscribe.useSubscriptions ()
            let subjectAD = getSubject store service (defaultArg useCache UseCache.IfReasonablyFresh) id

            content subjectAD
