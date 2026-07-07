[<AutoOpen>]
module LibAutoUi.Components.InputFieldString

open Fable.React

open LibClient
open LibClient.Components
open LibAutoUi.Types

open Rn.Components
open Rn.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let textInput =
        makeViewStyles {
            border 1 (Color.Hex "#cccccc")
        }

type UIAuto with
    [<Component>]
    static member InputFieldString(
            onChange:         InputValue -> unit,
            maybeValue:       Option<InputValue>,
            ?xLegacyStyles:   List<Rn.LegacyStyles.RuntimeStyles>,
            ?key:             string
        ) : ReactElement =
        key |> ignore
        xLegacyStyles |> ignore

        match maybeValue with
        | Some (StringValue value) ->
            Rn.TextInput(
                value = value,
                onChangeText = (fun v -> onChange (StringValue v)),
                styles = [| Styles.textInput |]
            )
        | None ->
            Rn.TextInput(
                onChangeText = (fun v -> onChange (StringValue v)),
                styles = [| Styles.textInput |]
            )
        | other ->
            LC.UiText(value = sprintf "XXX not a string value! %A" other)
