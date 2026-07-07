[<AutoOpen>]
module LibClient.Components.TriStateful_Simple

open LibClient
open Fable.React
open Rn.Components
open Rn.Styles
open LC.TriStateful.Abstract

module private Styles =
    let shield = makeViewStyles {
        Position.Absolute; FlexDirection.Row; JustifyContent.Center; AlignItems.Center
        trbl 0 0 0 0; backgroundColor (Color.WhiteAlpha 0.8)
    }
    let error   = makeViewStyles { JustifyContent.FlexStart; paddingLeft 24 }
    let message = makeTextStyles { color Color.DevRed; marginRight 24 }

type LibClient.Components.Constructors.LC.TriStateful with
    [<Component>]
    static member Simple(content: (Async<Result<unit, string>> -> unit) -> ReactElement, ?key: string) : ReactElement =
        key |> ignore
        LC.TriStateful.Abstract(fun (mode, runAction, reset) ->
            castAsElement [|
                content runAction
                match mode with
                | Mode.Initial -> noElement
                | Mode.InProgress ->
                    Rn.View(styles = [| Styles.shield |], children = [|
                        Rn.ActivityIndicator(size = Size.Small, color = "#aaaaaa")
                    |])
                | Mode.Error message ->
                    Rn.View(styles = [| Styles.shield; Styles.error |], children = [|
                        LC.Text(message, styles = [| Styles.message |])
                        LC.Button(label = "Ok", state = ButtonHighLevelState.LowLevel (ButtonLowLevelState.Actionable reset))
                    |])
            |]
        )
