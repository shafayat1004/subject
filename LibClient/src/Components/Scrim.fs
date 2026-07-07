[<AutoOpen>]
module LibClient.Components.Scrim

open Fable.Core.JsInterop
open Fable.React

open LibClient
open LibClient.Accessibility

open Rn.Styles
open Rn.Components

[<RequireQualifiedAccess>]
module private Styles =
    let root =
        makeViewStyles {
            Position.Relative
        }

    let gestureView =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
        }

    // Opacity is driven by Moti's `animate` prop (see below), never set here.
    let scrimInner =
        makeViewStyles {
            Position.Absolute
            top 0
            right 0
            bottom 0
            left 0
            backgroundColor (Color.Rgba(0, 0, 0, 0.5))
        }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Scrim(
            isVisible: bool,
            ?onPress: ReactEvent.Action -> unit,
            ?onPanVertical: Rn.Components.GestureView.PanGestureState -> unit,
            ?onPanHorizontal: Rn.Components.GestureView.PanGestureState -> unit,
            ?testId: string,
            ?styles: array<ViewStyles>,
            ?key: string) : ReactElement =
        key |> ignore

        let isMountedHook = Hooks.useState isVisible

        // Keep the overlay mounted while shown; when hidden, let the fade-out finish (Moti's
        // onDidAnimate fires on the JS thread) before unmounting.
        Hooks.useEffect(
            (fun () ->
                if isVisible then
                    isMountedHook.update true
            ),
            [| isVisible |]
        )

        if isMountedHook.current then
            Rn.View(
                styles =
                    [|
                        Styles.root
                        yield! (styles |> Option.defaultValue [||])
                    |],
                children =
                    elements {
                        Rn.MotiView(
                            from        = createObj [ "opacity" ==> 0.0 ],
                            animate     = createObj [ "opacity" ==> (if isVisible then 1.0 else 0.0) ],
                            transition  = createObj [ "type" ==> "timing"; "duration" ==> 500 ],
                            onDidAnimate =
                                (fun _styleProp _finished ->
                                    if not isVisible then
                                        isMountedHook.update false),
                            styles = [| Styles.scrimInner |],
                            children = [||]
                        )

                        let child =
                            match onPress with
                            | Some onPress ->
                                LC.Pressable(
                                    onPress = onPress,
                                    label = "Dismiss",
                                    role = AccessibilityRole.Button,
                                    testId = (testId |> Option.defaultValue "scrim-dismiss"),
                                    overlay = true,
                                    componentName = "LC.Scrim"
                                )
                            | None ->
                                noElement

                        let isPanEnabled = onPanHorizontal.IsSome || onPanVertical.IsSome

                        if isPanEnabled then
                            Rn.GestureView(
                                styles = [| Styles.gestureView |],
                                ?onPanHorizontal = onPanHorizontal,
                                ?onPanVertical = onPanVertical,
                                children =
                                    elements {
                                        child
                                    }
                            )
                        else
                            child
                    }
            )
        else
            noElement
