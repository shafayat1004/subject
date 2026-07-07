[<AutoOpen>]
module LibClient.Components.AppShell_NetworkFailureMessage

open Fable.React
open LibClient
open Rn.Components
open Rn.Styles

module private Styles =
    let view       = makeViewStyles { margin 20; AlignItems.Center }
    let secondLine = makeViewStyles { marginVertical 20 }

type LibClient.Components.Constructors.LC.AppShell with
    [<Component>]
    static member NetworkFailureMessage(?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>, ?key: string) : ReactElement =
        key |> ignore
        let legacyStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                | [] -> [||]
                | s  -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" s |]
            | None -> [||]
        Rn.View(
            styles = [| Styles.view; yield! legacyStyles |],
            children = [|
                LC.Heading(children = [| LC.UiText "Network Unreachable!" |])
                Rn.View(
                    styles = [| Styles.secondLine |],
                    children = [| LC.Text "It seems like you may have lost network connectivity, please check and try again." |]
                )
            |]
        )
