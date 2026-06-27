[<AutoOpen>]
module LibClient.Components.Sidebar_Divider

open Fable.React
open LibClient
open ReactXP.Components
open ReactXP.Styles

module private Styles =
    let view = makeViewStyles { marginVertical 20; borderBottom 1 (Color.Grey "cc") }

type LibClient.Components.Constructors.LC.Sidebar with
    [<Component>]
    static member Divider(?styles: array<ViewStyles>, ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>, ?key: string) : ReactElement =
        key |> ignore
        let legacyStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                | [] -> [||]
                | s  -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" s |]
            | None -> [||]
        RX.View(styles = [| Styles.view; yield! legacyStyles; yield! defaultArg styles [||] |], children = [||])
