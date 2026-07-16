[<AutoOpen>]
module LibClient.Components.Sidebar_Divider

open Fable.React
open LibClient
open Rn.Components
open Rn.Styles

module private Styles =
    let view = makeViewStyles { marginVertical 20; borderBottom 1 (Color.Grey "cc") }

type LibClient.Components.Constructors.LC.Sidebar with
    [<Component>]
    static member Divider(?styles: array<ViewStyles>, ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>, ?key: string) : ReactElement =
        key |> ignore
        let legacyStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                | [] -> [||]
                | s  -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" s |]
            | None -> [||]
        Rn.View(styles = [| Styles.view; yield! legacyStyles; yield! defaultArg styles [||] |], children = [||])
