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

open ReactXP.Components
open ReactXP.Styles

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

    type LibClient.Components.Constructors.LC.Nav.Top with
        [<Component>]
        static member Base(
                handheld:       unit -> ReactElement,
                desktop:        unit -> ReactElement,
                ?styles:        array<ViewStyles>,
                ?theme:         Theme -> Theme,
                ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
                ?testId:        string,
                ?key:           string
            ) : ReactElement =
            key |> ignore

            let theTheme = Themes.GetMaybeUpdatedWith theme
            let testId = defaultArg testId "eggshell-nav-top"

            let legacyViewStyles : array<ViewStyles> =
                match xLegacyStyles with
                | Some legacyStyles ->
                    match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                    | []     -> [||]
                    | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
                | None -> [||]

            let wrap (content: ReactElement) (screenSize: ScreenSize) =
                if isNoElementOrEmptyFragmentOrEmptyArray content then
                    noElement
                else
                    RX.View(
                        styles =
                            [|
                                Styles.view theTheme screenSize
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
