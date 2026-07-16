[<AutoOpen>]
module LibUiAdmin.Components.GridCell

open Fable.React
open Fable.React.Props
open LibClient
open Rn.Components
open Rn.Styles

module dom = Fable.React.Standard

// Legacy `col-w-*` convention: width units × 20px (see git history for reference).
let private columnBaseWidth = 20

let private defaultColWidthUnits (columnIndex: int) : int =
    match columnIndex with
    | 0 -> 14
    | 1 -> 7
    | 2 -> 9
    | _ -> 6

let private defaultColumnTotalUnits = 14 + 7 + 9

let private resolveWidthUnits (columnIndex: int) (widthUnits: int option) : int =
    widthUnits |> Option.defaultValue (defaultColWidthUnits columnIndex)

let private columnWidthPercent (widthUnits: int) (totalUnits: int) : float =
    100.0 * float widthUnits / float totalUnits

[<RequireQualifiedAccess>]
module GridCellLayout =
    let columnBaseWidth = columnBaseWidth
    let defaultColumnTotalUnits = defaultColumnTotalUnits

    let totalUnits (columnWidthUnits: seq<int>) : int =
        columnWidthUnits |> Seq.sum

    let tableWidthFromUnits (units: seq<int>) : int =
        units |> Seq.sumBy (fun u -> u * columnBaseWidth)

[<RequireQualifiedAccess>]
module GridCellStyles =
    /// Matches `table.la-table td { color: #666 }` on web.
    let text =
        makeTextStyles {
            color (Color.Grey "66")
            #if !EGGSHELL_PLATFORM_IS_WEB
            flexShrink 1
            #endif
        }

[<RequireQualifiedAccess>]
module private Styles =
    let private cellStylesCache = System.Collections.Generic.Dictionary<string, ViewStyles>()

    /// Fixed column width as a row-percentage (same on every row, like HTML table columns).
    let cell (widthUnits: int) (totalUnits: int) (isFirstColumn: bool) : ViewStyles =
        let key = sprintf "%i-%i-%b" widthUnits totalUnits isFirstColumn
        match cellStylesCache.TryGetValue key with
        | true, styles -> styles
        | false, _ ->
            let styles =
                makeViewStyles {
                    widthPercent (columnWidthPercent widthUnits totalUnits)
                    flexGrow 0
                    flexShrink 0
                    FlexDirection.Row
                    AlignItems.Center
                    JustifyContent.FlexStart
                    Overflow.Hidden
                    paddingHorizontal 10
                    paddingVertical 20
                    if isFirstColumn then paddingLeft 30
                }
            cellStylesCache.[key] <- styles
            styles

    let cellContent =
        makeViewStyles {
            flex 1
            AlignSelf.Stretch
            JustifyContent.Center
            AlignItems.FlexStart
        }

type UiAdmin with
    /// Cross-platform table cell: `dom.td` on web (styled by `table.la-table`), flex column on native.
    [<Component>]
    static member GridCell (
            children:          ReactElements,
            ?columnIndex:      int,
            ?widthUnits:       int,
            ?columnTotalUnits: int,
            ?className:        string,
            ?isFirstColumn:    bool,
            ?key:              string
        ) : ReactElement =
        let columnIndex = defaultArg columnIndex 0
        let widthUnits = resolveWidthUnits columnIndex widthUnits
        let totalUnits = defaultArg columnTotalUnits defaultColumnTotalUnits
        let isFirstColumn = defaultArg isFirstColumn (columnIndex = 0)
        let cellKey = key |> Option.defaultValue (string columnIndex)

        #if EGGSHELL_PLATFORM_IS_WEB
        ignore widthUnits
        ignore totalUnits
        ignore isFirstColumn
        let classes =
            match className with
            | Some c when c <> "" -> c
            | _                   -> null

        dom.td [
            Key cellKey
            if not (isNull classes) then ClassName classes
        ] children
        #else
        ignore className
        Rn.View(
            key      = cellKey,
            styles   = [| Styles.cell widthUnits totalUnits isFirstColumn |],
            children = [|
                Rn.View(
                    styles   = [| Styles.cellContent |],
                    children = children
                )
            |]
        )
        #endif
