[<AutoOpen>]
module LibClient.Components.LabelledValue

open Fable.React
open LibClient
open ReactXP.Components
open ReactXP.Styles

module private Styles =
    let root      = makeViewStyles { FlexDirection.Row; JustifyContent.SpaceBetween; marginVertical 2 }
    let labelText = makeTextStyles { FontWeight.Bold }
    let valueView = makeViewStyles { FlexDirection.Column }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member LabelledValue(label: string, ?children: array<ReactElement>, ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>, ?key: string) : ReactElement =
        key |> ignore
        let legacyStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                | [] -> [||]
                | s  -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" s |]
            | None -> [||]
        RX.View(
            styles = [| Styles.root; yield! legacyStyles |],
            children = [|
                RX.View(children = [| LC.Text(label, styles = [| Styles.labelText |]) |])
                RX.View(styles = [| Styles.valueView |], children = defaultArg children [||])
            |]
        )
