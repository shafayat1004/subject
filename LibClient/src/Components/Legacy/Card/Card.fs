namespace LibClient.Components.Legacy

open LibClient

// Keep the public types at the canonical path `LibClient.Components.Legacy.Card.*`
// (matching the pre-conversion typext module) so existing callers and the render
// DSL — which emits `LibClient.Components.Legacy.Card.Shadowed` etc. — keep resolving.
module Card =
    type Style =
    | Shadowed
    | Flat

    type Theme = {
        FlatBorderColor: Color
    }


namespace LibClient.Components

open Fable.React
open LibClient
open LibClient.Accessibility
open LibClient.Components.Legacy
open Rn.Components
open Rn.Styles

[<AutoOpen>]
module Legacy_Card =

    [<RequireQualifiedAccess>]
    module private Styles =
        let shadowed = makeViewStyles {
            Position.Relative
            margin          8
            padding         8
            backgroundColor Color.White
            borderRadius    2
            elevation       10
            shadow          (Color.BlackAlpha 0.3) 4 (0, 1)
            Overflow.Hidden
        }

        let flat =
            ViewStyles.Memoize(
                fun (flatBorderColor: Color) ->
                    makeViewStyles {
                        Position.Relative
                        margin          8
                        padding         8
                        backgroundColor Color.White
                        borderWidth     1
                        borderRadius    6
                        borderColor     flatBorderColor
                    }
            )

        let view (style: Card.Style) (theme: Card.Theme) =
            match style with
            | Card.Shadowed -> shadowed
            | Card.Flat     -> flat theme.FlatBorderColor

    type LibClient.Components.Constructors.LC.Legacy with
        [<Component>]
        static member Card(
                children:       array<ReactElement>,
                ?style:         Card.Style,
                ?theme:         Card.Theme -> Card.Theme,
                ?onPress:       (ReactEvent.Action -> unit),
                ?label:         string,
                ?testId:        string,
                ?styles:        array<ViewStyles>,
                ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>,
                ?key:           string
            ) : ReactElement =
            key           |> ignore
            xLegacyStyles |> ignore
            let theStyle = defaultArg style Card.Shadowed
            let theTheme = Themes.GetMaybeUpdatedWith theme
            Rn.View(
                styles   = [| Styles.view theStyle theTheme; yield! defaultArg styles [||] |],
                children = [|
                    yield! children
                    match onPress with
                    | Some f ->
                        let a11yLabel = defaultArg label "Open"
                        let resolvedTestId =
                            testId |> Option.orElse (Some (A11ySlug.testId "legacy-card" a11yLabel))
                        LC.Pressable(
                            onPress       = f,
                            label         = a11yLabel,
                            testId        = resolvedTestId.Value,
                            role          = AccessibilityRole.Button,
                            overlay       = true,
                            componentName = "LC.Legacy.Card"
                        )
                    | None -> ()
                |]
            )
