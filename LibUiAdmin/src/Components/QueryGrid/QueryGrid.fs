[<AutoOpen>]
module LibUiAdmin.Components.QueryGrid

open Fable.React

open LibClient
open LibClient.Components
open LibClient.Components.Form.Base
open LibClient.Components.Form.Base.Types
open LibClient.Services.Subscription

open ReactXP.Components
open ReactXP.Styles

open LibUiAdmin
open LibUiAdmin.Components.Grid


type PaginatedGridData<'Item> = LibUiAdmin.Components.Grid.PaginatedGridData<'Item>

type Order =
| Ascending
| Descending

type QueryPage<'Query> = {
    Query:      'Query
    PageSize:   PositiveInteger
    PageNumber: PositiveInteger
    Order:      Order
}

type Page<'Query> =
| BlankPage of PageSize: PositiveInteger
| QueryPage of QueryPage<'Query>
with
    member this.PageSize : PositiveInteger =
        match this with
        | BlankPage pageSize  -> pageSize
        | QueryPage queryPage -> queryPage.PageSize

    member this.PageNumber : PositiveInteger =
        match this with
        | BlankPage _         -> PositiveInteger.One
        | QueryPage queryPage -> queryPage.PageNumber

    member this.MaybeQuery : Option<'Query> =
        match this with
        | BlankPage _         -> None
        | QueryPage queryPage -> Some queryPage.Query

module Page =
    let ofQuery (query: 'Query) : Page<'Query> =
        {
            Query      = query
            PageSize   = PositiveInteger.ofLiteral 50
            PageNumber = PositiveInteger.One
            Order      = Ascending
        }
        |> QueryPage

    let maybeQuery (value: Page<'Query>) : Option<'Query> =
        value.MaybeQuery

    let withSize (size: PositiveInteger) (value: Page<'Query>) : Page<'Query> =
        match value with
        | BlankPage _         -> BlankPage size
        | QueryPage queryPage -> QueryPage { queryPage with PageSize = size }

    let withNumber (number: PositiveInteger) (value: Page<'Query>) : Page<'Query> =
        match value with
        | BlankPage size       -> BlankPage size
        | QueryPage queryPage  -> QueryPage { queryPage with PageNumber = number }

type Mode<'Item, 'Query> =
| OneTime                    of Execute: (QueryPage<'Query> -> Async<AsyncData<seq<'Item>>>)
| SubscribeWithoutTotalCount of MakeSubscription: (QueryPage<'Query> -> (LibClient.AsyncDataModule.AsyncData<seq<'Item>> -> unit) -> SubscribeResult)
| SubscribeWithTotalCount    of MakeSubscription: (QueryPage<'Query> -> (LibClient.AsyncDataModule.AsyncData<ItemsMaybeWithTotalCount<'Item>> -> unit) -> SubscribeResult)


module private Styles =
    let queryBlock = makeViewStyles {
        width 300
    }

    let buttonsBlock = makeViewStyles {
        width 300
    }


type UiAdmin with
    [<Component>]
    static member QueryGrid<'Item, 'QueryFormField, 'QueryAcc, 'Query when 'QueryFormField: comparison and 'Query : equality and 'QueryAcc :> AbstractAcc<'QueryFormField, 'Query>> (
        mode:            Mode<'Item, 'Query>,
        page:            Page<'Query>,
        onPageChange:    Page<'Query> -> unit,
        initialQueryAcc: 'QueryAcc,
        headers:         ReactElement,
        row:             'Item * Option<QueryPage<'Query>> * (unit -> unit) -> ReactElement,
        ?handheldRow:    'Item * Option<QueryPage<'Query>> * (unit -> unit) -> ReactElement,
        ?queryForm:      FormHandle<'QueryFormField, 'QueryAcc, 'Query> -> ReactElement,
        ?customRender:   ReactElement * ReactElement -> ReactElement,
        ?children:       ReactChildrenProp,
        ?key:            string,
        ?xLegacyStyles:  List<ReactXP.LegacyStyles.RuntimeStyles>
    ) : ReactElement =
        ignore (children, key)

        let subscriptionOffRef = Hooks.useRef None

        let initialItems =
            match page with
            | QueryPage _ -> WillStartFetchingSoonHack
            | BlankPage _ -> Available Seq.empty

        let gridDataHook =
            Hooks.useState {
                PageNumber          = page.PageNumber
                PageSize            = page.PageSize
                MaybePageCount      = None
                Items               = initialItems
                GoToPage            = (fun _ _ _ -> ())
                MaybeTotalItemCount = None
            }

        let maybeCurrentQueryPageHook = Hooks.useState None
        let refreshNonceHook          = Hooks.useState 0

        let pageRef = Hooks.useRef page
        pageRef.current <- page

        let goToPage (pageSize: PositiveInteger) (pageNumber: PositiveInteger) (_e: Option<ReactEvent.Action>) : unit =
            page
            |> Page.withSize pageSize
            |> Page.withNumber pageNumber
            |> onPageChange

        let paginatedGridDataForInitialPageSize (size: PositiveInteger) : PaginatedGridData<'Item> = {
            PageNumber          = PositiveInteger.One
            PageSize            = size
            MaybePageCount      = None
            Items               = Available Seq.empty
            GoToPage            = goToPage
            MaybeTotalItemCount = None
        }

        let setLoadedPageIfQueryPageStillMatches (queryPage: QueryPage<'Query>) (itemsMaybeWithTotalCountAD: AsyncData<ItemsMaybeWithTotalCount<'Item>>) : unit =
            LibClient.JsInterop.runOnNextTick (fun () ->
                if pageRef.current = QueryPage queryPage then
                    let (maybePageCount, maybeTotalItemCount) =
                        itemsMaybeWithTotalCountAD
                        |> AsyncData.toOption
                        |> Option.flatMap (fun itemsMaybeWithTotalCount ->
                            itemsMaybeWithTotalCount.MaybeTotalCount
                            |> Option.map (fun itemTotalCount ->
                                let maybeTotalItemCount = itemTotalCount |> UnsignedInteger.ofUint64 |> Some
                                let maybePageCount =
                                    (double itemTotalCount) / (double queryPage.PageSize.Value)
                                    |> ceil
                                    |> uint64
                                    |> UnsignedInteger.ofUint64
                                    |> Some

                                (maybePageCount, maybeTotalItemCount)
                            )
                        )
                        |> Option.mapOrElse (None, None) id

                    gridDataHook.update (fun estateGridData ->
                        { estateGridData with
                            Items               = itemsMaybeWithTotalCountAD |> AsyncData.map ItemsMaybeWithTotalCount.items
                            PageNumber          = queryPage.PageNumber
                            PageSize            = queryPage.PageSize
                            MaybePageCount      = maybePageCount |> Option.orElse estateGridData.MaybePageCount
                            MaybeTotalItemCount = maybeTotalItemCount
                        }
                    )

                    maybeCurrentQueryPageHook.update (Some queryPage)
            )

        Hooks.useEffectDisposable (
            (fun () ->
                subscriptionOffRef.current |> Option.iter (fun currSubscriptionOff ->
                    currSubscriptionOff ()
                    subscriptionOffRef.current <- None
                )

                gridDataHook.update (fun estateGridData -> { estateGridData with Items = estateGridData.Items |> AsyncData.makeFetching })

                match page with
                | BlankPage pageSize ->
                    gridDataHook.update (fun _ -> paginatedGridDataForInitialPageSize pageSize)
                    maybeCurrentQueryPageHook.update None

                    { new System.IDisposable with
                        member _.Dispose() = ()
                    }

                | QueryPage queryPage ->
                    match mode with
                    | OneTime executeQuery ->
                        async {
                            let! itemsAsyncData = executeQuery queryPage
                            setLoadedPageIfQueryPageStillMatches queryPage (itemsAsyncData |> AsyncData.map ItemsMaybeWithTotalCount.withoutTotalCount)
                        }
                        |> AsyncHelpers.startSafely

                        { new System.IDisposable with
                            member _.Dispose() = ()
                        }

                    | SubscribeWithoutTotalCount makeSubscription ->
                        let subscribeResult =
                            makeSubscription queryPage (fun itemsAsyncData ->
                                setLoadedPageIfQueryPageStillMatches queryPage (itemsAsyncData |> AsyncData.map ItemsMaybeWithTotalCount.withoutTotalCount)
                            )

                        subscriptionOffRef.current <- Some subscribeResult.Off

                        { new System.IDisposable with
                            member _.Dispose() =
                                subscribeResult.Off ()
                                subscriptionOffRef.current <- None
                        }

                    | SubscribeWithTotalCount makeSubscription ->
                        let subscribeResult =
                            makeSubscription queryPage (fun itemsAsyncData ->
                                setLoadedPageIfQueryPageStillMatches queryPage itemsAsyncData
                            )

                        subscriptionOffRef.current <- Some subscribeResult.Off

                        { new System.IDisposable with
                            member _.Dispose() =
                                subscribeResult.Off ()
                                subscriptionOffRef.current <- None
                        }
            ),
            [| page :> obj; box mode; refreshNonceHook.current :> obj |]
        )

        let refresh () : unit =
            refreshNonceHook.update (fun n -> n + 1)

        let submit (query: 'Query) (_e: ReactEvent.Action) () : UDActionResult =
            {
                Query      = query
                PageSize   = page.PageSize
                PageNumber = PositiveInteger.One
                Order      = Ascending
            }
            |> QueryPage
            |> onPageChange

            Ok () |> Async.Of

        let gridData              = { gridDataHook.current with GoToPage = goToPage }
        let maybeCurrentQueryPage = maybeCurrentQueryPageHook.current

        let legacyViewStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
            | None -> [||]

        let form =
            match queryForm with
            | None -> noElement
            | Some queryFormFn ->
                LC.Form.Base (
                    submit      = submit,
                    accumulator = ManageInternallyInitializingWith initialQueryAcc,
                    content     =
                        fun (form: FormHandle<'QueryFormField, 'QueryAcc, 'Query>) ->
                            element {
                                RX.View (
                                    styles   = [| Styles.queryBlock |],
                                    children = [| queryFormFn form |]
                                )
                                LC.Buttons (
                                    align    = LibClient.Components.Buttons.Left,
                                    styles   = [| Styles.buttonsBlock |],
                                    children = [|
                                        LC.Button (
                                            state = Button.PropStateFactory.Make form.TrySubmit,
                                            label = "Submit"
                                        )
                                    |]
                                )
                            }
                )

        let grid =
            RX.View (
                children =
                    [|
                        match handheldRow with
                        | None ->
                            UiAdmin.Grid (
                                headers = headers,
                                input   =
                                    Paginated (
                                        gridData,
                                        (fun item -> row (item, maybeCurrentQueryPage, refresh)),
                                        None
                                    )
                            )
                        | Some handheldRowFn ->
                            UiAdmin.Grid (
                                headers = headers,
                                input   =
                                    Paginated (
                                        gridData,
                                        (fun item -> row (item, maybeCurrentQueryPage, refresh)),
                                        Some (fun item -> handheldRowFn (item, maybeCurrentQueryPage, refresh))
                                    )
                            )
                    |]
            )

        RX.View (
            styles   = legacyViewStyles,
            children =
                match customRender with
                | None        -> [| form; grid |]
                | Some render -> [| render (form, grid) |]
        )
