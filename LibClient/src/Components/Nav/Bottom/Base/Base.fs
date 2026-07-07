namespace LibClient.Components.Nav.Bottom

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
open LibClient.Responsive

open Rn.Components
open Rn.Styles

open Nav.Bottom.Base

[<AutoOpen>]
module Nav_Bottom_Base =

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
                | ScreenSize.Desktop  -> height theme.DesktopHeight
                | ScreenSize.Handheld -> height theme.HandheldHeight
                if not theme.HideShadow then
                    shadow (Color.BlackAlpha 0.2) 3 (0, -2)
                    borderTop 1 (Color.Grey "cc")
            }

    type LibClient.Components.Constructors.LC.Nav.Bottom with
        [<Component>]
        static member Base(
                handheld:       unit -> ReactElement,
                desktop:        unit -> ReactElement,
                ?styles:        array<ViewStyles>,
                ?theme:         Theme -> Theme,
                ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>,
                ?key:           string
            ) : ReactElement =
            key |> ignore

            let theTheme = Themes.GetMaybeUpdatedWith theme

            let legacyViewStyles : array<ViewStyles> =
                match xLegacyStyles with
                | Some legacyStyles ->
                    match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                    | []     -> [||]
                    | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
                | None -> [||]

            let wrap (content: ReactElement) (screenSize: ScreenSize) (includeExtraStyles: bool) =
                if isNoElementOrEmptyFragmentOrEmptyArray content then
                    noElement
                else
                    Rn.View(
                        styles =
                            [|
                                Styles.view theTheme screenSize
                                yield! legacyViewStyles
                                if includeExtraStyles then
                                    yield! defaultArg styles [||]
                            |],
                        children = [| content |]
                    )

            LC.Responsive(
                desktop =
                    (fun screenSize ->
                        wrap (desktop ()) screenSize true),
                handheld =
                    (fun screenSize ->
                        wrap (handheld ()) screenSize false)
            )
