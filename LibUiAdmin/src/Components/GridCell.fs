[<AutoOpen>]
module LibUiAdmin.Components.GridCell

open Fable.React
open Fable.React.Props
open LibClient
open ReactXP.Components
open ReactXP.Styles

module dom = Fable.React.Standard

[<RequireQualifiedAccess>]
module private Styles =
    let cell =
        makeViewStyles {
            flex 1
            minWidth 100
            paddingHorizontal 10
            paddingVertical 20
            borderBottom 1 (Color.Grey "cc")
        }

    let firstCell =
        makeViewStyles {
            paddingLeft 30
        }

type UiAdmin with
    /// Cross-platform table cell: `dom.td` on web, `RX.View` on native.
    [<Component>]
    static member GridCell (children: ReactElements, ?className: string, ?isFirstColumn: bool, ?key: string) : ReactElement =
        key |> ignore

        #if EGGSHELL_PLATFORM_IS_WEB
        isFirstColumn |> ignore
        dom.td (
            match className with
            | Some c -> [| ClassName c |]
            | None   -> [||]
        ) children
        #else
        ignore className
        RX.View(
            styles =
                [|
                    Styles.cell
                    if defaultArg isFirstColumn false then Styles.firstCell
                |],
            children = children
        )
        #endif
