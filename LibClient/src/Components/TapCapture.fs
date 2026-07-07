[<AutoOpen>]
module LibClient.Components.TapCapture

open Fable.React
open LibClient
open LibClient.Accessibility
open Rn.Styles

type LibClient.Components.Constructors.LC with
    static member TapCapture(
            onPress: ReactEvent.Action -> unit,
            ?label: string,
            ?testId: string,
            ?role: AccessibilityRole,
            ?state: AccessibilityStateRecord,
            ?accessibilityId: string,
            ?onHoverStart: Browser.Types.PointerEvent -> unit,
            ?onHoverEnd: Browser.Types.PointerEvent -> unit,
            ?onPressIn: Browser.Types.PointerEvent -> unit,
            ?onPressOut: Browser.Types.PointerEvent -> unit,
            ?pointerState: LC.Pointer.State.PointerState,
            ?styles: array<ViewStyles>,
            ?key: string
        ) : ReactElement =
        key |> ignore
        onHoverStart |> ignore
        onHoverEnd |> ignore
        onPressIn |> ignore
        onPressOut |> ignore

        let resolvedTestId =
            testId
            |> Option.orElse (label |> Option.map (A11ySlug.testId "pressable"))

        LC.Pressable(
            onPress = onPress,
            ?label = label,
            ?testId = resolvedTestId,
            ?role = role,
            ?state = state,
            ?accessibilityId = accessibilityId,
            ?pointerState = pointerState,
            ?styles = styles,
            overlay = true,
            componentName = "LC.TapCapture",
            ?key = key
        )
