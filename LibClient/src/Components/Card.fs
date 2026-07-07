[<AutoOpen>]
module LibClient.Components.Card

open Fable.React
open Rn.Components
open Rn.Styles
open LibClient.Components
open LibClient
open LibClient.Accessibility

module LC =
    module Card =
        type ShadowedCardProperties = {
            ShadowColor:  Color
            Elevation:    int
            ShadowRadius: int
            ShadowOffset: int * int
        }

        type FlatCardProperties = {
            BorderColor: Color
            BorderWidth: int
        }

        type CardType =
        | Flat of FlatCardProperties
        | Shadowed of ShadowedCardProperties

        type Theme = {
            CardType:     CardType
            BorderRadius: int
            Padding:      int
        }
        with
            static member ShadowedCard =
                {
                    BorderRadius = 2
                    Padding      = 8
                    CardType     = Shadowed {
                        ShadowColor  = Color.BlackAlpha 0.3
                        Elevation    = 10
                        ShadowRadius = 4
                        ShadowOffset = (0, 1)
                    }
                }

            static member FlatCard =
                {
                    BorderRadius = 6
                    Padding      = 8
                    CardType     = Flat {
                        BorderColor = MaterialDesignColors.grey.B300
                        BorderWidth = 1
                    }
                }

open LC.Card

[<RequireQualifiedAccess>]
module private Styles =
    let outerContainerDefaults = makeViewStyles {
        Position.Relative
        backgroundColor Color.White
        margin 8
    }

    let outerContainerFlat = ViewStyles.Memoize (
        fun (cornerRadius: int) (borderWidth: int) (outlineColor: Color) -> makeViewStyles {
            padding 0
            Overflow.Visible
            borderRadius cornerRadius
            border borderWidth outlineColor
        }
    )

    let outerContainerShadowed = ViewStyles.Memoize (
        fun (cornerRadius: int) (shadowColor: Color) (cardElevation: int) (shadowRadius: int) (offsetX: int) (offsetY: int) ->
            makeViewStyles {
                padding 0
                Overflow.Visible
                borderRadius cornerRadius
                shadow shadowColor shadowRadius (offsetX, offsetY)
                elevation cardElevation
            }
    )

    let contentContainer = ViewStyles.Memoize (
        fun (cornerRadius: int) (contentPadding: int) -> makeViewStyles {
            borderRadius cornerRadius
            Overflow.Hidden
            padding contentPadding
        }
    )

    let outerContainerFor (theme: Theme) =
        match theme.CardType with
        | Flat flatTheme ->
            outerContainerFlat theme.BorderRadius flatTheme.BorderWidth flatTheme.BorderColor
        | Shadowed shadowedTheme ->
            let (offsetX, offsetY) = shadowedTheme.ShadowOffset
            outerContainerShadowed
                theme.BorderRadius
                shadowedTheme.ShadowColor
                shadowedTheme.Elevation
                shadowedTheme.ShadowRadius
                offsetX
                offsetY

    let outerContainerWithOnPressIdle = makeViewStyles { Noop }

    let outerContainerWithOnPressHovered =
        makeViewStyles {
            backgroundColor MaterialDesignColors.grey.B050
        }

    let outerContainerWithOnPressActive =
        makeViewStyles {
            opacity 0.4
            backgroundColor MaterialDesignColors.grey.B100
        }

    let outerContainerWithOnPressFor (isHovered: bool) (isActive: bool) =
        if isActive then
            outerContainerWithOnPressActive
        elif isHovered then
            outerContainerWithOnPressHovered
        else
            outerContainerWithOnPressIdle

type LC with
    [<Component>]
    (*
        A layered card is required because iOS shadows get clipped when overflow is hidden.
        Having a layered card with proper overflow built in provides a bug free shadow.
        Control over the outer container with the shadow is limited to avoid accidentaly issues with shadow.
        Card content layout can be controlled granularly using a wrapper around the content.
    *)
    static member Card (
        children:     array<ReactElement>,
        ?onPress:     (ReactEvent.Action -> unit),
        ?label:        string,
        ?testId:       string,
        ?theme:       Theme -> Theme,
        ?outerStyles: array<ViewStyles>,
        ?key:         string
    ) : ReactElement =
        ignore key
        // noElement
        let theTheme    = Themes.GetMaybeUpdatedWith theme
        let outerStyles = defaultArg outerStyles Array.empty

        match onPress with
        | None ->
            Rn.View (
                styles = [|
                    Styles.outerContainerDefaults
                    yield! outerStyles
                    Styles.outerContainerFor theTheme
                |],
                children = [|
                    Rn.View (
                        children = children,
                        styles   = [| Styles.contentContainer theTheme.BorderRadius theTheme.Padding |]
                    )
                |]
            )
        | Some onPress ->
            let a11yLabel = defaultArg label "Open"
            let resolvedTestId =
                testId |> Option.orElse (Some (A11ySlug.testId "card" a11yLabel))

            LC.Pointer.State (fun pointerState ->
                Rn.View (
                    styles = [|
                        Styles.outerContainerDefaults
                        yield! outerStyles
                        Styles.outerContainerFor theTheme
                        Styles.outerContainerWithOnPressFor pointerState.IsHovered pointerState.IsDepressed
                    |],
                    children = [|
                        Rn.View (
                            children = children,
                            styles   = [| Styles.contentContainer theTheme.BorderRadius theTheme.Padding |]
                        )
                        LC.Pressable (
                            onPress      = onPress,
                            label        = a11yLabel,
                            testId       = resolvedTestId.Value,
                            role         = AccessibilityRole.Button,
                            overlay      = true,
                            pointerState = pointerState,
                            componentName = "LC.Card"
                        )
                    |]
                )
            )
