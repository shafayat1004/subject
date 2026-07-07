[<AutoOpen>]
module LibClient.Components.Dialog.Base

open Fable.Core
open Browser.Types
open Fable.React

open LibClient

open Rn.Components
open Rn.Styles

type ContentPosition =
| Free
| Center
| CenterTop

type CloseAction =
| OnEscape
| OnBackground
| OnCloseButton

type CanClose =
| When of List<CloseAction> * (ReactEvent.Action -> unit)
| Never
with
    member this.ShouldShowCloseButton : bool =
        match this with
        | When (value, _) -> value |> List.contains OnCloseButton
        | Never           -> false

    member this.OnClose (e: ReactEvent.Action) =
        match this with
        | When (_, action) ->
            e.MaybeEvent |> Option.sideEffect (fun event -> event.stopPropagation())
            action e
        | Never -> Noop

[<Emit("undefined")>]
let private undefinedOnPress: PointerEvent -> unit = jsNative

[<RequireQualifiedAccess>]
module private Styles =
    let background =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
            backgroundColor (Color.BlackAlpha 0.7)
        }

    let dialog =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
        }

    let positionWrapper =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
            FlexDirection.Column
            AlignItems.Center
        }

    let positionCenterTop =
        makeViewStyles {
            JustifyContent.FlexStart
        }

    let positionCenter =
        makeViewStyles {
            JustifyContent.Center
        }

type LibClient.Components.Constructors.LC.Dialog with
    [<Component>]
    static member Base(
            canClose: CanClose,
            contentPosition: ContentPosition,
            ?children: ReactChildrenProp,
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>,
            ?key: string
        ) : ReactElement =
        key |> ignore
        xLegacyStyles |> ignore

        let onBackgroundPress (e: PointerEvent) : unit =
            match canClose with
            | When (value, action) when List.contains OnBackground value ->
                action (ReactEvent.Action.OfBrowserEvent e)
            | _ -> Noop

        let onKeyPress (e: KeyboardEvent) : unit =
            match (canClose, e.key) with
            | (When (value, action), KeyboardEvent.Key.Escape) when List.contains OnEscape value ->
                action (ReactEvent.Action.OfBrowserEvent e)
            | _ -> Noop

        let onPress =
            if Rn.Runtime.isNative() then undefinedOnPress else onBackgroundPress

        Rn.View(
            styles = [| Styles.background |],
            children =
                [|
                    Rn.View(
                        onKeyPress = onKeyPress,
                        onPress = onPress,
                        styles = [| Styles.dialog |],
                        children =
                            [|
                                match contentPosition with
                                | Free -> defaultArg children [||] |> castAsElement
                                | CenterTop ->
                                    Rn.View(
                                        styles = [| Styles.positionWrapper; Styles.positionCenterTop |],
                                        children = defaultArg children [||]
                                    )
                                | Center ->
                                    Rn.View(
                                        styles = [| Styles.positionWrapper; Styles.positionCenter |],
                                        children = defaultArg children [||]
                                    )
                            |]
                    )
                |]
        )
