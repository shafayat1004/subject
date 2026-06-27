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

let private colWidthPx (units: int) : int =
    units * columnBaseWidth

[<RequireQualifiedAccess>]
module GridCellLayout =
    let columnBaseWidth = columnBaseWidth
    let defaultThreeColumnTableWidth = colWidthPx 14 + colWidthPx 7 + colWidthPx 9

    let tableWidthFromUnits (units: seq<int>) : int =
        units |> Seq.sumBy colWidthPx

[<RequireQualifiedAccess>]
module GridCellStyles =
    /// Matches `table.la-table td { color: #666 }` on web.
    let text =
        makeTextStyles {
            color (Color.Grey "66")
        }

[<RequireQualifiedAccess>]
module private Styles =
    let cell (widthUnits: int) (isFirstColumn: bool) =
        makeViewStyles {
            width (colWidthPx widthUnits)
            flexGrow 0
            flexShrink 0
            paddingHorizontal 10
            paddingVertical 20
            JustifyContent.Center
            if isFirstColumn then paddingLeft 30
        }

type UiAdmin with
    /// Cross-platform table cell: `dom.td` on web (styled by `table.la-table` + `col-w-*`), flex column on native.
    [<Component>]
    static member GridCell (
            children:           ReactElements,
            ?columnIndex:        int,
            ?widthUnits:         int,
            ?className:          string,
            ?isFirstColumn:      bool,
            ?key:                string
        ) : ReactElement =
        key |> ignore

        let columnIndex = defaultArg columnIndex 0
        let widthUnits = resolveWidthUnits columnIndex widthUnits
        let isFirstColumn = defaultArg isFirstColumn (columnIndex = 0)

        #if EGGSHELL_PLATFORM_IS_WEB
        let classes =
            match className with
            | Some c when c <> "" -> c
            | _                   -> null

        dom.td [ if not (isNull classes) then ClassName classes ] children
        #else
        ignore className
        RX.View(
            styles = [| Styles.cell widthUnits isFirstColumn |],
            children = children
        )
        #endif
