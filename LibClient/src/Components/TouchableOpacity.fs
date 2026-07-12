[<AutoOpen>]
module LibClient.Components.TouchableOpacity

open Fable.React

open LibClient
open LibClient.Accessibility

open Rn.Styles
open Rn.Components

[<RequireQualifiedAccess>]
module private Styles =
    let container =
        makeViewStyles {
            Position.Relative
            padding 15
            margin -15
        }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member TouchableOpacity (
            children:      ReactElements,
            onPress:       ReactEvent.Action -> unit,
            ?onHoverStart: Browser.Types.PointerEvent -> unit,
            ?onHoverEnd:   Browser.Types.PointerEvent -> unit,
            ?onPressIn:    Browser.Types.PointerEvent -> unit,
            ?onPressOut:   Browser.Types.PointerEvent -> unit,
            ?pointerState: LC.Pointer.State.PointerState,
            ?label:        string,
            ?testId:       string,
            ?styles:       array<ViewStyles>,
            ?key:          string) : ReactElement =
        onHoverStart |> ignore
        onHoverEnd   |> ignore
        onPressIn    |> ignore
        onPressOut   |> ignore

        let a11yLabel = defaultArg label "Button"
        let theTestId = testId |> Option.defaultValue (A11ySlug.testId "touchable-opacity" a11yLabel)

        Rn.View(
            styles   = [| Styles.container |],
            children = elements {
                children
                LC.Pressable (
                    onPress       = onPress,
                    label         = a11yLabel,
                    role          = AccessibilityRole.Button,
                    testId        = theTestId,
                    overlay       = true,
                    ?pointerState = pointerState,
                    ?styles       = styles,
                    componentName = "LC.TouchableOpacity",
                    ?key          = key
                )
            }
        )
