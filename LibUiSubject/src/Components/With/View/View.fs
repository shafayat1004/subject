[<AutoOpen>]
module LibUiSubject.Components.With.View

open Fable.React
open LibClient
open LibClient.Components
open LibUiSubject
open LibUiSubject.Components.Constructors

let private getView
    (service: LibUiSubject.Services.ViewService.IViewService<'Input, 'Output, 'OpError>)
    (maybeUseCache: Option<UseCache>)
    (input: 'Input)
    : Async<AsyncData<'Output>> =
    let useCache = defaultArg maybeUseCache UseCache.IfReasonablyFresh
    service.GetOne useCache input

type LibUiSubject.Components.Constructors.UiSubject.With with
    [<Component>]
    static member View (
        service: LibUiSubject.Services.ViewService.IViewService<'Input, 'Output, 'OpError>,
        input: 'Input,
        whenAvailable: 'Output -> ReactElement,
        ?whenUninitialized: unit -> ReactElement,
        ?whenFetching: Option<'Output> -> ReactElement,
        ?whenFailed: AsyncDataFailure -> ReactElement,
        ?whenUnavailable: unit -> ReactElement,
        ?whenAccessDenied: unit -> ReactElement,
        ?whenElse: unit -> ReactElement,
        ?useCache: UseCache,
        ?key: string)
        : ReactElement =
            ignore key

            let valueState = Hooks.useState WillStartFetchingSoonHack

            Hooks.useEffect(
                dependencies = [| input |],
                effect =
                    fun () ->
                        async {
                            let! outputAD = getView service useCache input
                            valueState.update outputAD
                        } |> startSafely
            )

            LC.AsyncData (
                data = valueState.current,
                whenAvailable = whenAvailable,
                ?whenUninitialized = whenUninitialized,
                ?whenFetching = whenFetching,
                ?whenFailed = whenFailed,
                ?whenUnavailable = whenUnavailable,
                ?whenAccessDenied = whenAccessDenied,
                ?whenElse = whenElse
            )

    [<Component>]
    static member View (
        service: LibUiSubject.Services.ViewService.IViewService<'Input, 'Output, 'OpError>,
        input: 'Input,
        content: AsyncData<'Output> -> ReactElement,
        ?useCache: UseCache,
        ?key: string)
        : ReactElement =
            ignore key

            let valueState = Hooks.useState WillStartFetchingSoonHack

            Hooks.useEffect(
                dependencies = [| input |],
                effect =
                    fun () ->
                        async {
                            let! outputAD = getView service useCache input
                            valueState.update outputAD
                        } |> startSafely
            )

            content valueState.current
