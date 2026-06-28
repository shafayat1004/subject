[<AutoOpen>]
module LibUiSubjectAdmin.Components.Audit.Generic

open System
open Fable.React
open LibClient
open LibClient.Components
open LibClient.Services.HttpService.ThothEncodedHttpService
open LibUiAdmin.Components
open LibUiAdmin.Components.Grid
open LibUiSubjectAdmin.Components.Audit.Types
open LibUiSubjectAdmin.Components.Constructors
open ReactXP.Components

#if EGGSHELL_PLATFORM_IS_WEB
do
    ReactXP.LegacyStyles.Css.addCss """
td.audit-operation-str div div {
    white-space: pre !important;
}
"""
#endif

type LibUiSubjectAdmin.Components.Constructors.UiSubjectAdmin.Audit with
    [<Component>]
    static member Generic<'EndpointParams, 'Entry> (
        endpoint:             ApiEndpoint<'EndpointParams, unit, List<'Entry>>,
        pageToEndpointParams: {| Size: PositiveInteger; Number: PositiveInteger |} -> 'EndpointParams,
        auditSubjectId:       AuditSubjectId,
        headersAndFields:     ReactElement * ('Entry -> ReactElement),
        ?handheldRow:         'Entry -> ReactElement,
        ?filter:              'Entry -> bool,
        ?key:                 string)
        : ReactElement =
            ignore (auditSubjectId, key)

            let goToPageRef =
                Hooks.useRef (fun (_: PositiveInteger) (_: PositiveInteger) (_: Option<ReactEvent.Action>) -> ())

            let gridDataHook =
                Hooks.useState {
                    PageNumber          = PositiveInteger.One
                    PageSize            = PositiveInteger.ofLiteral 10
                    MaybePageCount      = None
                    Items               = WillStartFetchingSoonHack
                    GoToPage            = (fun pageSize pageNumber e -> goToPageRef.current pageSize pageNumber e)
                    MaybeTotalItemCount = None
                }

            let goToPage (pageSize: PositiveInteger) (pageNumber: PositiveInteger) (_e: Option<ReactEvent.Action>) : unit =
                async {
                    let parameters = pageToEndpointParams {| Size = pageSize; Number = pageNumber |}

                    let! data = LibClient.ServiceInstances.services().ThothEncodedHttp.Request endpoint parameters ()
                    let itemsAD =
                        match data with
                        | Ok entries   -> AsyncData.Available (Seq.ofList entries)
                        | Error requestError ->
                            match requestError with
                            | RequestError.DecodingError (message, _, _) -> AsyncData.Failed (UnknownFailure message)
                            | RequestError.Non200Code (0,         _)     -> AsyncData.Failed NetworkFailure
                            | RequestError.Non200Code (errorCode, _)     -> AsyncData.Failed (UnknownFailure $"Error {errorCode}")
                        |>
                            match filter with
                            | None -> identity
                            | Some filterFn -> AsyncData.map (Seq.filter filterFn)

                    gridDataHook.update (fun estate ->
                        { estate with
                            PageNumber = pageNumber
                            PageSize   = pageSize
                            Items      = itemsAD
                        }
                    )
                } |> startSafely

            goToPageRef.current <- goToPage

            Hooks.useEffect (
                (fun () -> goToPage gridDataHook.current.PageSize gridDataHook.current.PageNumber None),
                [| |]
            )

            let headers, makeDesktopRow = headersAndFields

            RX.View (
                children =
                    [|
                        UiAdmin.Grid (
                            headers = headers,
                            input   =
                                Paginated (
                                    gridDataHook.current,
                                    makeDesktopRow,
                                    handheldRow
                                )
                        )
                    |]
            )
