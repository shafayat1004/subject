namespace LibClient.Components.Nav.Top

open LibClient

module Base =
    type Theme = {
        DesktopHeight:   int
        HandheldHeight:  int
        BackgroundColor: Color
        HideShadow:      bool
    }

namespace LibClient.Components

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Responsive

open Rn.Components
open Rn.Styles

open Nav.Top.Base

[<AutoOpen>]
module Nav_Top_Base =

    [<RequireQualifiedAccess>]
    module private Styles =
        let view (theme: Theme) (screenSize: ScreenSize) =
            makeViewStyles {
                FlexDirection.Row
                AlignContent.Center
                AlignItems.Center
                Overflow.Visible
                backgroundColor theme.BackgroundColor
                match screenSize with
                | ScreenSize.Desktop ->
                    height theme.DesktopHeight
                    paddingHorizontal 16
                | ScreenSize.Handheld ->
                    height theme.HandheldHeight
                if not theme.HideShadow then
                    shadow (Color.BlackAlpha 0.2) 3 (0, 2)
                    border 1 (Color.Grey "cc")
            }

        // Grow the bar by the top safe-area inset and pad its content down by the same amount, so
        // the (coloured) bar background fills the status-bar strip while the content stays centred
        // below it. Web / no-notch devices report insetTop = 0, so this is a no-op there.
        // Keyed on primitives (two ints) per the style-leak rules.
        let topSafeInset =
            ViewStyles.Memoize (fun (barHeight: int) (insetTop: int) ->
                makeViewStyles {
                    paddingTop insetTop
                    height     (barHeight + insetTop)
                }
            )

    type LibClient.Components.Constructors.LC.Nav.Top with
        [<Component>]
        static member Base(
                handheld:       unit -> ReactElement,
                desktop:        unit -> ReactElement,
                ?styles:        array<ViewStyles>,
                ?theme:         Theme -> Theme,
                ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>,
                ?testId:        string,
                ?key:           string
            ) : ReactElement =
            key |> ignore

            let theTheme = Themes.GetMaybeUpdatedWith theme
            let testId = defaultArg testId "eggshell-nav-top"

            // Device top inset (status bar / notch). Zero on web. The bar background extends up into
            // it and content is padded down (see Styles.topSafeInset).
            let insets = SafeArea.useInsets ()

            let legacyViewStyles : array<ViewStyles> =
                match xLegacyStyles with
                | Some legacyStyles ->
                    match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                    | []     -> [||]
                    | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
                | None -> [||]

            let wrap (content: ReactElement) (screenSize: ScreenSize) =
                if isNoElementOrEmptyFragmentOrEmptyArray content then
                    noElement
                else
                    let barHeight =
                        match screenSize with
                        | ScreenSize.Desktop  -> theTheme.DesktopHeight
                        | ScreenSize.Handheld -> theTheme.HandheldHeight
                    Rn.View(
                        styles =
                            [|
                                Styles.view theTheme screenSize
                                if insets.Top > 0 then
                                    Styles.topSafeInset barHeight insets.Top
                                yield! legacyViewStyles
                                yield! defaultArg styles [||]
                            |],
                        testId = testId,
                        accessibilityRole = AccessibilityRole.Header,
                        accessibilityLabel = "Top navigation",
                        children = [| content |]
                    )

            LC.Responsive(
                desktop =
                    (fun screenSize ->
                        wrap (desktop ()) screenSize),
                handheld =
                    (fun screenSize ->
                        wrap (handheld ()) screenSize)
            )
