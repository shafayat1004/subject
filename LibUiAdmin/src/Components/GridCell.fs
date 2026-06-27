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
            minWidth 80
            paddingHorizontal 8
            paddingVertical 4
        }

type UiAdmin with
    /// Cross-platform table cell: `dom.td` on web, `RX.View` on native.
    [<Component>]
    static member GridCell (children: ReactElements, ?className: string, ?key: string) : ReactElement =
        key |> ignore

        #if EGGSHELL_PLATFORM_IS_WEB
        dom.td (
            match className with
            | Some c -> [| ClassName c |]
            | None   -> [||]
        ) children
        #else
        ignore className
        RX.View(
            styles = [| Styles.cell |],
            children = children
        )
        #endif
