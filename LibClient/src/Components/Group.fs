[<AutoOpen>]
module LibClient.Components.Group

open Fable.React
open LibClient
open LibClient.Accessibility
open Rn.Components

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Group(
            ?label:              string,
            ?accessibilityLabel: string,
            ?testId:             string,
            ?children:           ReactChildrenProp,
            ?key:                string
        ) : ReactElement =
        key |> ignore
        let a11yLabel = accessibilityLabel |> Option.orElse label

        Rn.View(
            ?testId             = testId,
            ?accessibilityLabel = a11yLabel,
            accessibilityRole   = AccessibilityRole.Group,
            children            = (defaultArg children [||])
        )

    [<Component>]
    static member RadioGroup(
            label:     string,
            ?testId:   string,
            ?children: ReactChildrenProp,
            ?key:      string
        ) : ReactElement =
        key |> ignore

        Rn.View(
            ?testId            = testId,
            accessibilityLabel = label,
            accessibilityRole  = AccessibilityRole.RadioGroup,
            children           = (defaultArg children [||])
        )
