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
open LibClient.Components.Legacy
open ReactXP.Components
open ReactXP.Styles

[<AutoOpen>]
module Legacy_Card =

    [<RequireQualifiedAccess>]
    module private Styles =
        let shadowed = makeViewStyles {
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
                fun (theme: Card.Theme) ->
                    makeViewStyles {
                        margin          8
                        padding         8
                        backgroundColor Color.White
                        borderWidth     1
                        borderRadius    6
                        borderColor     theme.FlatBorderColor
                    }
            )

        let view (style: Card.Style) (theme: Card.Theme) =
            match style with
            | Card.Shadowed -> shadowed
            | Card.Flat     -> flat theme

    type LibClient.Components.Constructors.LC.Legacy with
        [<Component>]
        static member Card(
                children:       array<ReactElement>,
                ?style:         Card.Style,
                ?theme:         Card.Theme -> Card.Theme,
                ?onPress:       (ReactEvent.Action -> unit),
                ?styles:        array<ViewStyles>,
                ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
                ?key:           string
            ) : ReactElement =
            key           |> ignore
            xLegacyStyles |> ignore
            let theStyle  = defaultArg style Card.Shadowed
            let theTheme  = Themes.GetMaybeUpdatedWith theme
            RX.View(
                styles = [| Styles.view theStyle theTheme; yield! defaultArg styles [||] |],
                children = [|
                    yield! children
                    match onPress with
                    | Some f -> LC.TapCapture(onPress = f)
                    | None   -> ()
                |]
            )
