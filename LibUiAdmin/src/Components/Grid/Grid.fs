[<AutoOpen>]
module LibUiAdmin.Components.Grid

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props

open LibClient
open LibClient.Components
open LibClient.ColorScheme
open LibClient.RenderHelpers

open ReactXP.Components
open ReactXP.Styles

// NOTE: do NOT `open ReactXP.LegacyStyles` here. Its rule functions shadow the new-dialect
// ones and break make*Styles computation expressions.

module dom = Fable.React.Standard

type PaginatedGridData<'T> = {
    PageNumber:          PositiveInteger
    PageSize:            PositiveInteger
    MaybePageCount:      Option<UnsignedInteger>
    Items:               AsyncData<seq<'T>>
    MaybeTotalItemCount: Option<UnsignedInteger>
    GoToPage:       (* pageSize *) PositiveInteger -> (* pageNumber *) PositiveInteger -> Option<ReactEvent.Action> -> unit
} with
    member this.MakeFetching : PaginatedGridData<'T> =
        { this with Items = this.Items |> AsyncData.makeFetching }

type Input<'T> =
| Static     of Rows: ReactElement * MaybeHandheldRows: Option<ReactElement>
| Everything of AsyncData<seq<'T>> * MakeDesktopRow: ('T -> ReactElement) * MakeHandheldRow: (Option<'T -> ReactElement>)
| Paginated  of PaginatedGridData<'T> * MakeDesktopRow: ('T -> ReactElement) * MakeHandheldRow: (Option<'T -> ReactElement>)

// TODO at some point we should probably have GoToPage return the proper result type in the first place,
// not silently swollow errors/asyncness
let goToPageAdapter (goToPage: (* pageSize *) PositiveInteger -> (* pageNumber *) PositiveInteger -> Option<ReactEvent.Action> -> unit) (pageSize: PositiveInteger) (pageNumber: PositiveInteger) : Async<Result<unit, string>> =
    async {
        goToPage pageSize pageNumber None
        return Ok ()
    }

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        makeViewStyles {
            marginTop    20
            marginBottom 40
        }

    let fullHeight =
        makeViewStyles {
            flex 1
        }

    let scrollViewHorizontal =
        makeScrollViewStyles {
            paddingVertical 2
            minHeight       160
        }

    let scrollViewVertical =
        makeScrollViewStyles {
            paddingHorizontal 2
            minWidth          160
            flex              1
        }

#if !EGGSHELL_PLATFORM_IS_WEB
    let nativeGridRoot =
        makeViewStyles {
            AlignSelf.Stretch
            minWidth        280
        }
#endif

    let headers =
        makeViewStyles {
            FlexDirection.Row
            AlignItems.Stretch
            AlignSelf.Stretch
            Overflow.Visible
            widthPercent 100
        }

    let nativeTableBody =
        makeViewStyles {
            FlexDirection.Column
            AlignSelf.Stretch
            widthPercent 100
        }

    let nativeTableContainer =
        makeViewStyles {
            AlignSelf.Stretch
            widthPercent 100
            Overflow.Visible
        }

    let row =
        makeViewStyles {
            FlexDirection.Row
            AlignItems.Stretch
            AlignSelf.Stretch
            Overflow.Visible
            widthPercent 100
        }

    let rowDivider =
        makeViewStyles {
            borderBottom 1 (Color.Grey "cc")
        }

    let rowAlt =
        makeViewStyles {
            backgroundColor (Color.Grey "f0")
        }

    let rows =
        makeViewStyles {
            Overflow.Visible
            minHeight 100
            AlignSelf.Stretch
            widthPercent 100
        }

    let emptyMessage =
        makeViewStyles {
            margin 42
        }

    let emptyMessageText =
        makeTextStyles {
            TextAlign.Center
        }

    let pagination =
        makeViewStyles {
            paddingHorizontal 10
            backgroundColor   (Color.Grey "f0")
            FlexDirection.Row
            AlignItems.Stretch
            JustifyContent.SpaceBetween
            AlignSelf.Stretch
            widthPercent 100
        }

    let paginationHandheld =
        makeViewStyles {
            paddingHorizontal 10
            AlignItems.Center
            AlignSelf.Stretch
            widthPercent 100
        }

    let navigation =
        makeViewStyles {
            FlexDirection.Row
            AlignItems.Center
            flex 0
        }

    let pageSize =
        makeViewStyles {
            flex 0
            AlignItems.Center
            FlexDirection.Row
            JustifyContent.FlexEnd
        }

    let pageSizeText =
        makeTextStyles {
            marginHorizontal 10
        }

    let picker =
        makeViewStyles {
            padding         0
            margin          10
            backgroundColor Color.White
        }

    let pageInfoContainer =
        makeViewStyles {
            FlexDirection.Row
            AlignItems.Center
        }

    let gotoPageBtn =
        makeViewStyles {
            height 40
        }

    let gotoPageInput =
        makeViewStyles {
            marginTop       0
            width           80
            backgroundColor Color.White
        }

    let currPageInfo =
        makeTextStyles {
            marginHorizontal 10
        }

    let navPageInternalDot =
        makeTextStyles {
            marginHorizontal 10
            color            MaterialDesignColors.Indigo.Main
        }

    let resultCount =
        makeTextStyles {
            marginLeft 10
        }

    let navPageNumber =
        makeViewStyles {
            marginHorizontal 10
        }

    let navCurrentPage =
        makeViewStyles {
            borderWidth 1
            borderRadius 2
            backgroundColor Color.White
            paddingHorizontal 5
            paddingVertical 2
        }

    let navButton =
        makeViewStyles {
            marginHorizontal 5
        }

    let navButtonTheme (theme: LC.IconButton.Theme) : LC.IconButton.Theme =
        { theme with
            Actionable =
                { theme.Actionable with
                    IconSize = 14
                }
            Disabled =
                { theme.Disabled with
                    IconSize = 14
                }
            InProgress =
                { theme.InProgress with
                    IconSize = 14
                }
        }

module private FragmentHelpers =
    /// `element { ... }` produces a React fragment; table rows need direct cell/row children.
    [<Emit("(function (el) { var c = el && el.props && el.props.children; if (c == null) return [el]; if (Array.isArray(c)) return c; return [c]; })($0)")>]
    let unwrapFragmentChildren (content: ReactElement) : ReactElements = jsNative

#if !EGGSHELL_PLATFORM_IS_WEB
module NativeGrid =
    /// `element { ... }` produces a React fragment; RN flex rows need direct cell children.
    let unwrapFragmentChildren = FragmentHelpers.unwrapFragmentChildren

    let headers (headers: ReactElement option) (headersRaw: ReactElement option) : ReactElement =
        match headersRaw, headers with
        | _, Some raw ->
            let cells = unwrapFragmentChildren raw
            RX.View (styles = [| Styles.headers; Styles.rowDivider |], children = cells)
        | Some h, None ->
            let cells = unwrapFragmentChildren h
            RX.View (styles = [| Styles.headers; Styles.rowDivider |], children = cells)
        | None, None   -> noElement

    let staticBody (headers: ReactElement) (rows: ReactElement) : ReactElement =
        RX.View (
            styles = [| Styles.nativeTableBody |],
            children = [|
                if headers <> noElement then headers
                RX.View (styles = [| Styles.rows |], children = [| rows |])
            |]
        )

    let desktopRows<'T> (items: seq<'T>) (makeDesktopRow: 'T -> ReactElement) (itemKey: Option<'T -> string>) : ReactElement =
        let itemsList = items |> Seq.toList
        let lastIndex = itemsList.Length - 1

        RX.View (
            styles = [| Styles.rows |],
            children =
                (itemsList
                |> List.mapi (fun index item ->
                    let rowStyles =
                        [|
                            if index % 2 = 0 then Styles.rowAlt
                            Styles.row
                            if index < lastIndex then Styles.rowDivider
                        |]

                    let rowContent = makeDesktopRow item
                    let cells = unwrapFragmentChildren rowContent

                    RX.View (
                        key = (itemKey |> Option.map (fun f -> f item) |> Option.getOrElse (string index)),
                        styles = rowStyles,
                        children = cells
                    )
                )
                |> List.toArray)
        )

    let tableBody (headerRow: ReactElement) (bodyRows: ReactElement) : ReactElement =
        RX.View (
            styles = [| Styles.nativeTableBody |],
            children =
                [|
                    if headerRow <> noElement then headerRow
                    bodyRows
                |]
        )

#endif

#if EGGSHELL_PLATFORM_IS_WEB
do
    // Legacy Grid.styles pulled LibUiAdmin.Styles in via FixmeCrappyStyleSharing.
    // Modern Grid must import it so table.la-table CSS is injected on web.
    LibUiAdmin.Styles.styles.Force() |> ignore

    ReactXP.LegacyStyles.Css.addCss """
.row {
    position: relative
}

.row-alt {
    background-color: #f0f0f0;
}
"""
#endif

module private Helpers =
    let generatePagesToShow (currentPage: int) (totalPageCount: int) : List<int> =
        set [1; currentPage - 1; currentPage; currentPage + 1; totalPageCount]
        |> Set.filter (fun page -> page > 0 && page <= totalPageCount)
        |> Set.toList

    let onJumpToPageNumberKeyPress (maybeSave: Option<ReactEvent.Action -> unit>) (cancel: ReactEvent.Action -> unit) (e: Browser.Types.KeyboardEvent) : unit =
        let actionEvent = ReactEvent.Keyboard.OfBrowserEvent e |> ReactEvent.Action.Make
        match e.key with
        | KeyboardEvent.Key.Enter  -> maybeSave |> Option.sideEffect (fun save -> save actionEvent)
        | KeyboardEvent.Key.Escape -> cancel actionEvent
        | _                        -> Noop

    let onJumpToPageInitialize (jumpToPage: LibClient.Components.Legacy.Input.PositiveInteger.InputPositiveIntegerRef) : unit =
        jumpToPage.SelectAll()
        jumpToPage.RequestFocus()

    let maybeHeaders (headers: ReactElement option) (headersRaw: ReactElement option) : ReactElement =
        #if EGGSHELL_PLATFORM_IS_WEB
        let headerRow (cells: ReactElement) : ReactElement =
            dom.tr [] (FragmentHelpers.unwrapFragmentChildren cells)

        match headersRaw, headers with
        | _, Some raw ->
            dom.thead [ ClassName "headers" ] [| headerRow raw |]
        | Some h, None ->
            dom.thead [ ClassName "headers" ] [| headerRow h |]
        | None, None ->
            noElement
        #else
        NativeGrid.headers headers headersRaw
        #endif

type UiAdmin with
    /// Cross-platform table row: `dom.tr` on web, horizontal flex `RX.View` on native.
    [<Component>]
    static member GridRow (index: int, children: ReactElement, ?showBottomBorder: bool, ?key: string, ?itemKey: string) : ReactElement =
        let rowKey = key |> Option.orElse (itemKey |> Option.map id) |> Option.defaultValue (string index)
        let showBottomBorder = defaultArg showBottomBorder true

        #if EGGSHELL_PLATFORM_IS_WEB
        dom.tr
            [|
                unbox ("key", rowKey)
                ClassName ("row" + (if index % 2 = 0 then " row-alt" else ""))
            |]
            (FragmentHelpers.unwrapFragmentChildren children)
        #else
        let cells = NativeGrid.unwrapFragmentChildren children
        let rowStyles =
            [|
                if index % 2 = 0 then Styles.rowAlt
                Styles.row
                if showBottomBorder then Styles.rowDivider
            |]

        RX.View(
            key = rowKey,
            styles = rowStyles,
            children = cells
        )
        #endif

    [<Component>]
    static member Grid<'T> (
            input:                   Input<'T>,
            ?children:               ReactChildrenProp,
            ?pageSizeChoices:        List<PositiveInteger>,
            ?handleVerticalScrolling: bool,
            ?headers:                ReactElement,
            ?headersRaw:             ReactElement,
            ?itemKey:                ('T -> string),
            ?key:                    string,
            ?xLegacyStyles:          List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore
        children |> ignore

        let pageSizeChoices =
            pageSizeChoices
            |> Option.defaultValue ([10; 20; 50; 100] |> List.map PositiveInteger.ofLiteral)

        let handleVerticalScrolling = defaultArg handleVerticalScrolling false

        let headers = headers |> Option.orElse None
        let headersRaw = headersRaw |> Option.orElse None
        let itemKey = itemKey |> Option.orElse None

        let jumpToPageState = Hooks.useState LibClient.Components.Input.PositiveInteger.empty

        let legacyViewStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
            | None -> [||]

        let renderQuadState (data: PaginatedGridData<'T>) (isHandheldMode: bool) : ReactElement =
            LC.QuadStateful (
                act = goToPageAdapter data.GoToPage data.PageSize,
                validate =
                    (match data.MaybePageCount with
                     | None -> id
                     | Some pageCount -> Option.flatMap (fun v -> if v.Value > pageCount.Value then None else Some v)),
                initialInputAcc = LC.QuadStateful.Sync (Some data.PageNumber),
                initial =
                    (fun _edit ->
                        element {
                            LC.IconButton (
                                state  = IconButton.PropStateFactory.MakeLowLevel (if data.PageNumber = PositiveInteger.One then LC.IconButton.Disabled else LC.IconButton.Actionable (Some >> data.GoToPage data.PageSize PositiveInteger.One)),
                                icon   = LibClient.Icons.Icon.First,
                                styles = [| Styles.navButton |],
                                theme  = Styles.navButtonTheme
                            )
                            LC.IconButton (
                                state  = IconButton.PropStateFactory.MakeLowLevel (match PositiveInteger.ofInt (data.PageNumber.Value - 1) with | None -> LC.IconButton.Disabled | Some newPageNumber -> LC.IconButton.Actionable (Some >> data.GoToPage data.PageSize newPageNumber)),
                                icon   = LibClient.Icons.Icon.Previous,
                                styles = [| Styles.navButton |],
                                theme  = Styles.navButtonTheme
                            )
                            match data.MaybePageCount with
                            | None ->
                                match data.Items with
                                | Available items when items |> Seq.length = 0 ->
                                    LC.IconButton (
                                        state  = IconButton.PropStateFactory.MakeLowLevel LC.IconButton.Disabled,
                                        icon   = LibClient.Icons.Icon.Next,
                                        styles = [| Styles.navButton |],
                                        theme  = Styles.navButtonTheme
                                    )
                                    LC.IconButton (
                                        state  = IconButton.PropStateFactory.MakeLowLevel LC.IconButton.Disabled,
                                        icon   = LibClient.Icons.Icon.Last,
                                        styles = [| Styles.navButton |],
                                        theme  = Styles.navButtonTheme
                                    )
                                | _ ->
                                    LC.IconButton (
                                        state  = IconButton.PropStateFactory.MakeLowLevel (LC.IconButton.Actionable (Some >> data.GoToPage data.PageSize (data.PageNumber + PositiveInteger.One))),
                                        icon   = LibClient.Icons.Icon.Next,
                                        styles = [| Styles.navButton |],
                                        theme  = Styles.navButtonTheme
                                    )
                                    if isHandheldMode then
                                        LC.IconButton (
                                            state  = IconButton.PropStateFactory.MakeLowLevel LC.IconButton.Disabled,
                                            icon   = LibClient.Icons.Icon.Last,
                                            styles = [| Styles.navButton |],
                                            theme  = Styles.navButtonTheme
                                        )
                                    else
                                        LC.Text "Unknown Pages"
                            | Some pageCount ->
                                let pagesToShow = Helpers.generatePagesToShow data.PageNumber.Value pageCount.Value
                                let pagesToShowCount = pagesToShow |> List.length

                                pagesToShow
                                |> Seq.mapi (fun index pageNumber ->
                                    let isCurrentPage = pageNumber = data.PageNumber.Value
                                    element {
                                        RX.View (
                                            styles = [|
                                                Styles.navPageNumber
                                                if isCurrentPage then Styles.navCurrentPage
                                            |],
                                            children = [|
                                                LC.TextButton (
                                                    label = $"{pageNumber}",
                                                    state = TextButton.PropStateFactory.MakeLowLevel (match isCurrentPage with | false -> LC.TextButton.Actionable (Some >> data.GoToPage data.PageSize (PositiveInteger.ofLiteral pageNumber)) | true -> LC.TextButton.Disabled)
                                                )
                                            |]
                                        )
                                        if pagesToShowCount > 1 && ((index = 0 && pagesToShow.[0] + 1 <> pagesToShow.[1]) || (index = pagesToShowCount - 2 && (pagesToShow.[pagesToShowCount - 2] + 1 <> pagesToShow.[pagesToShowCount - 1]))) then
                                            LC.Text (
                                                value  = "•••",
                                                styles = [| Styles.navPageInternalDot |]
                                            )
                                    }
                                )

                                LC.IconButton (
                                    state  = IconButton.PropStateFactory.MakeLowLevel (if data.PageNumber.Value >= pageCount.Value then LC.IconButton.Disabled else LC.IconButton.Actionable (Some >> data.GoToPage data.PageSize (data.PageNumber + PositiveInteger.One))),
                                    icon   = LibClient.Icons.Icon.Next,
                                    styles = [| Styles.navButton |],
                                    theme  = Styles.navButtonTheme
                                )
                                match PositiveInteger.ofUnsignedInteger pageCount with
                                | None -> ()
                                | Some lastPage ->
                                    LC.IconButton (
                                        state  = IconButton.PropStateFactory.MakeLowLevel (if data.PageNumber.Value >= pageCount.Value then LC.IconButton.Disabled else LC.IconButton.Actionable (Some >> data.GoToPage data.PageSize lastPage)),
                                        icon   = LibClient.Icons.Icon.Last,
                                        styles = [| Styles.navButton |],
                                        theme  = Styles.navButtonTheme
                                    )

                            match data.MaybeTotalItemCount, isHandheldMode with
                            | Some totalItemCount, false ->
                                let resultCountText =
                                    sprintf "Total %s" (pluralize (uint32 totalItemCount.Value) "result" "results")

                                LC.Text (
                                    value  = resultCountText,
                                    styles = [| Styles.resultCount |]
                                )
                            | _ -> ()
                        }
                    ),
                input =
                    (fun (_, setInput, maybeSave, cancel) ->
                        LC.With.Ref (
                            onInitialize = Helpers.onJumpToPageInitialize,
                            ``with`` =
                                (fun (bindJumpToPageRef, _) ->
                                    LibClient.Components.Constructors.LC.Legacy.Input.PositiveInteger (
                                        Result.toOption >> setInput,
                                        initialValue = data.PageNumber,
                                        onKeyPress   = Helpers.onJumpToPageNumberKeyPress maybeSave cancel,
                                        ref          = bindJumpToPageRef
                                    )
                                )
                        )
                    )
            )

        let renderNavRow (isHandheldMode: bool) : ReactElement =
            match input with
            | Paginated (data, _, _) ->
                let quadState = renderQuadState data isHandheldMode

                if isHandheldMode then
                    RX.View (
                        styles = [| Styles.paginationHandheld |],
                        children = [|
                            RX.View (
                                styles = [| Styles.navigation |],
                                children = [| quadState |]
                            )
                        |]
                    )
                else
                    RX.View (
                        styles = [| Styles.pagination |],
                        children = [|
                            RX.View (
                                styles = [| Styles.navigation |],
                                children = [| quadState |]
                            )
                            RX.View (
                                styles = [| Styles.pageInfoContainer |],
                                children = [|
                                    RX.View (
                                        styles = [| Styles.pageSize |],
                                        children = [|
                                            LC.Text (
                                                value  = "Page Size",
                                                styles = [| Styles.pageSizeText |]
                                            )
                                            LibClient.Components.Constructors.LC.Legacy.Input.Picker (
                                                pageSizeChoices |> List.map (fun size -> { Label = size.Value.ToString(); Item = size }),
                                                LibClient.Components.Legacy.Input.Picker.ByItem data.PageSize |> Some,
                                                LibClient.Components.Legacy.Input.Picker.CannotUnselect (fun (index, _) -> data.GoToPage (pageSizeChoices.Item index) PositiveInteger.One None),
                                                InputValidity.Valid,
                                                styles = [| Styles.picker |]
                                            )
                                        |]
                                    )
                                    LC.Text (
                                        styles = [| Styles.currPageInfo |],
                                        value  =
                                            match data.Items with
                                            | Available items when items |> Seq.length = 0 -> ""
                                            | _ ->
                                                let pageCountText =
                                                    data.MaybePageCount
                                                    |> Option.mapOrElse "unknown" (fun p -> string p.Value)

                                                sprintf "Showing %i of %s pages" data.PageNumber.Value pageCountText
                                    )
                                    LC.Input.PositiveInteger (
                                        validity    = InputValidity.Valid,
                                        onChange      = jumpToPageState.update,
                                        value         = jumpToPageState.current,
                                        placeholder   = "Page no",
                                        styles        = [| Styles.gotoPageInput |]
                                    )
                                    LC.Button (
                                        state  = Button.PropStateFactory.MakeLowLevel (match jumpToPageState.current.Result with | Ok (Some page) -> Button.Actionable (Some >> data.GoToPage data.PageSize page) | _ -> Button.Disabled),
                                        label  = "Go",
                                        styles = [| Styles.gotoPageBtn |]
                                    )
                                |]
                            )
                        |]
                    )
            | _ ->
                noElement

        let renderGridBody (isHandheldMode: bool) : ReactElement =
            let maybeHeaderElement = Helpers.maybeHeaders headers headersRaw

            match input with
            | Static (rows, maybeHandheldRows) ->
                match isHandheldMode, maybeHandheldRows with
                | true, Some handheldRows -> handheldRows
                | _ ->
                    #if EGGSHELL_PLATFORM_IS_WEB
                    dom.table [ ClassName "la-table" ] [|
                        maybeHeaderElement
                        dom.tbody [ ClassName "rows" ] (FragmentHelpers.unwrapFragmentChildren rows)
                    |]
                    #else
                    NativeGrid.staticBody maybeHeaderElement rows
                    #endif

            | Everything (asyncItems, makeDesktopRow, maybeMakeHandheldRow)
            | Paginated ({ Items = asyncItems }, makeDesktopRow, maybeMakeHandheldRow) ->
                LC.AsyncData (
                    data = asyncItems,
                    whenAvailable =
                        (fun (items: seq<'T>) ->
                            if items |> Seq.isEmpty then
                                RX.View (
                                    styles = [| Styles.emptyMessage |],
                                    children = [|
                                        LC.Text (
                                            value  = "No Rows",
                                            styles = [| Styles.emptyMessageText |]
                                        )
                                    |]
                                )
                            else
                                match isHandheldMode, maybeMakeHandheldRow with
                                | true, Some makeHandheldRows ->
                                    LC.ItemList (
                                        items        = items,
                                        whenNonempty =
                                            (fun items ->
                                                items
                                                |> Seq.mapi (fun index item ->
                                                    RX.View (
                                                        key = (itemKey |> Option.map (fun f -> f item) |> Option.getOrElse (string index)),
                                                        children = [| makeHandheldRows item |]
                                                    )
                                                )
                                                |> Seq.toArray
                                                |> castAsElement
                                            ),
                                        style = LibClient.Components.ItemList.Style.Raw
                                    )
                                | _ ->
                                    #if EGGSHELL_PLATFORM_IS_WEB
                                    dom.table [ ClassName "la-table" ] [|
                                        maybeHeaderElement
                                        dom.tbody [ ClassName "rows" ] (
                                            items
                                            |> Seq.mapi (fun index item ->
                                                dom.tr
                                                    [|
                                                        unbox ("key", itemKey |> Option.map (fun f -> f item) |> Option.getOrElse (string index))
                                                        ClassName ("row" + (if index % 2 = 0 then " row-alt" else ""))
                                                    |]
                                                    (FragmentHelpers.unwrapFragmentChildren (makeDesktopRow item))
                                            )
                                            |> Seq.toArray
                                        )
                                    |]
                                    #else
                                    NativeGrid.tableBody
                                        maybeHeaderElement
                                        (NativeGrid.desktopRows items makeDesktopRow itemKey)
                                    #endif
                        ),
                    whenFailed =
                        (fun error ->
                            LC.InfoMessage (
                                level   = Level.Caution,
                                message = error.ToString()
                            )
                        )
                )

        #if EGGSHELL_PLATFORM_IS_WEB
        let gridView (isHandheldMode: bool) : ReactElement =
            let navRow = renderNavRow isHandheldMode

            RX.View (
                styles =
                    [|
                        Styles.view
                        if handleVerticalScrolling then Styles.fullHeight
                        yield! legacyViewStyles
                    |],
                children = [|
                    match isHandheldMode with
                    | true  -> noElement
                    | false -> navRow

                    RX.ScrollView (
                        scrollEnabled = handleVerticalScrolling,
                        styles        = [| Styles.scrollViewVertical |],
                        children      = [|
                            RX.ScrollView (
                                horizontal = true,
                                styles     = [| Styles.scrollViewHorizontal |],
                                children   = [| renderGridBody isHandheldMode |]
                            )
                        |]
                    )

                    navRow
                |]
            )

        gridView false
        #else
        let navRow = renderNavRow false

        RX.View (
            styles = [| Styles.view; Styles.nativeGridRoot; yield! legacyViewStyles |],
            children = [|
                navRow
                RX.View (
                    styles = [| Styles.nativeTableContainer |],
                    children = [| renderGridBody false |]
                )
                navRow
            |]
        )
        #endif
