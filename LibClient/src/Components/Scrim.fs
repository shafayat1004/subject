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

    // Opacity is driven by a Reanimated animated style (see below), never set here.
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
            isVisible:        bool,
            ?onPress:         ReactEvent.Action -> unit,
            ?onPanVertical:   Rn.Components.GestureView.PanGestureState -> unit,
            ?onPanHorizontal: Rn.Components.GestureView.PanGestureState -> unit,
            ?testId:          string,
            ?styles:          array<ViewStyles>,
            ?key:             string) : ReactElement =
        key |> ignore

        let isMountedHook = Hooks.useState isVisible
        let opacity = Reanimated.useSharedValue (if isVisible then 1.0 else 0.0)
        let animatedStyle = Reanimated.useAnimatedOpacity opacity
        // A token guards against a stale fade-out unmounting the overlay after a quick re-show.
        let animTokenRef = Hooks.useRef 0

        Hooks.useEffect(
            (fun () ->
                animTokenRef.current <- animTokenRef.current + 1
                let token = animTokenRef.current
                if isVisible then
                    isMountedHook.update true
                    // runLater avoids the sporadically-botched animation seen without the deferral.
                    LibClient.JsInterop.runLater (System.TimeSpan.FromMilliseconds 10.) (fun () ->
                        if animTokenRef.current = token then
                            opacity.AnimateTiming(1.0, durationMs = 500.0))
                else
                    LibClient.JsInterop.runLater (System.TimeSpan.FromMilliseconds 10.) (fun () ->
                        if animTokenRef.current = token then
                            opacity.AnimateTiming(
                                0.0,
                                durationMs = 500.0,
                                onComplete =
                                    (fun () ->
                                        if animTokenRef.current = token then
                                            isMountedHook.update false)))
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
                        Rn.ReanimatedView(
                            styles        = [| Styles.scrimInner |],
                            animatedStyle = animatedStyle,
                            children      = [||]
                        )

                        let child =
                            match onPress with
                            | Some onPress ->
                                LC.Pressable(
                                    onPress       = onPress,
                                    label         = "Dismiss",
                                    role          = AccessibilityRole.Button,
                                    testId        = (testId |> Option.defaultValue "scrim-dismiss"),
                                    overlay       = true,
                                    componentName = "LC.Scrim"
                                )
                            | None ->
                                noElement

                        let isPanEnabled = onPanHorizontal.IsSome || onPanVertical.IsSome

                        if isPanEnabled then
                            Rn.GestureView(
                                styles           = [| Styles.gestureView |],
                                ?onPanHorizontal = onPanHorizontal,
                                ?onPanVertical   = onPanVertical,
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
