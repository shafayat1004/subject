[<AutoOpen>]
module LibUiAdmin.Components.GridCell

open Fable.React
open Fable.React.Props
open LibClient
open ReactXP.Components
open ReactXP.Styles

module dom = Fable.React.Standard

// Legacy `col-w-*` convention: width units × 20px (see git history for reference).
let private columnBaseWidth = 20

let private defaultColWidthUnits (columnIndex: int) : int =
    match columnIndex with
    | 0 -> 14
    | 1 -> 7
    | 2 -> 9
    | _ -> 6

let private resolveWidthUnits (columnIndex: int) (widthUnits: int option) : int =
    widthUnits |> Option.defaultValue (defaultColWidthUnits columnIndex)

[<RequireQualifiedAccess>]
module GridCellLayout =
    let columnBaseWidth = columnBaseWidth

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
    /// Row-direction cell: proportional width via flexGrow, vertical middle via AlignItems.Center
    /// (matches web `vertical-align: middle` on `table.la-table td`).
    let cell =
        ViewStyles.Memoize (fun (widthUnits: int) (isFirstColumn: bool) ->
            makeViewStyles {
                FlexDirection.Row
                AlignItems.Center
                flexGrow widthUnits
                flexShrink 1
                flexBasis 0
                minWidth 0
                Overflow.Visible
                paddingHorizontal 10
                paddingVertical 20
                if isFirstColumn then paddingLeft 30
            })

type UiAdmin with
    /// Cross-platform table cell: `dom.td` on web (styled by `table.la-table`), flex column on native.
    [<Component>]
    static member GridCell (
            children:           ReactElements,
            ?columnIndex:        int,
            ?widthUnits:         int,
            ?className:          string,
            ?isFirstColumn:      bool,
            ?key:                string
        ) : ReactElement =
        let columnIndex = defaultArg columnIndex 0
        let widthUnits = resolveWidthUnits columnIndex widthUnits
        let isFirstColumn = defaultArg isFirstColumn (columnIndex = 0)
        let cellKey = key |> Option.defaultValue (string columnIndex)

        #if EGGSHELL_PLATFORM_IS_WEB
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
        RX.View(
            key = cellKey,
            styles = [| Styles.cell widthUnits isFirstColumn |],
            children = children
        )
        #endif
