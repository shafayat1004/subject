[<AutoOpen>]
module LibClient.Components.AppShell_NetworkFailureMessage

open Fable.React
open LibClient
open ReactXP.Components
open ReactXP.Styles

module private Styles =
    let view       = makeViewStyles { margin 20; AlignItems.Center }
    let secondLine = makeViewStyles { marginVertical 20 }

type LibClient.Components.Constructors.LC.AppShell with
    [<Component>]
    static member NetworkFailureMessage(?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>, ?key: string) : ReactElement =
        key |> ignore
        let legacyStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                | [] -> [||]
                | s  -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" s |]
            | None -> [||]
        RX.View(
            styles = [| Styles.view; yield! legacyStyles |],
            children = [|
                LC.Heading(children = [| LC.UiText "Network Unreachable!" |])
                RX.View(
                    styles = [| Styles.secondLine |],
                    children = [| LC.Text "It seems like you may have lost network connectivity, please check and try again." |]
                )
            |]
        )
