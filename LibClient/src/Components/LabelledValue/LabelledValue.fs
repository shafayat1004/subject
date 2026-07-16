[<AutoOpen>]
module LibClient.Components.LabelledValue

open Fable.React
open LibClient
open Rn.Components
open Rn.Styles

module private Styles =
    let root      = makeViewStyles { FlexDirection.Row; JustifyContent.SpaceBetween; marginVertical 2 }
    let labelText = makeTextStyles { FontWeight.Bold }
    let valueView = makeViewStyles { FlexDirection.Column }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member LabelledValue(label: string, ?children: array<ReactElement>, ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>, ?key: string) : ReactElement =
        key |> ignore
        let legacyStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                | [] -> [||]
                | s  -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" s |]
            | None -> [||]
        Rn.View(
            styles   = [| Styles.root; yield! legacyStyles |],
            children = [|
                Rn.View(children = [| LC.Text(label, styles = [| Styles.labelText |]) |])
                Rn.View(styles   = [| Styles.valueView |], children = defaultArg children [||])
            |]
        )
