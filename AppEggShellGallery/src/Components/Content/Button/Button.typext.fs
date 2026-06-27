module AppEggShellGallery.Components.Content.Button

open LibClient
open LibClient.Components
open ReactXP.LegacyStyles
open ReactXP.Styles
open LC.Button

module SampleThemes =
    let private appearance textColor borderColor backgroundColor : Appearance =
        {
            TextColor       = textColor
            BorderColor     = borderColor
            BackgroundColor = backgroundColor
            FontWeight      = ReactXP.Styles.RulesRestricted.FontWeight.Normal
        }

    let private stateAppearance textColor borderColor backgroundColor : StateAppearance =
        {
            Actionable = appearance textColor borderColor backgroundColor
            Disabled   = appearance textColor borderColor backgroundColor
            InProgress = appearance textColor borderColor backgroundColor
        }

    let caution (theme: LC.Button.Theme) : LC.Button.Theme =
        { theme with Cautionary = stateAppearance Color.Black Color.White Color.DevOrange }

    let small (theme: LC.Button.Theme) : LC.Button.Theme =
        { theme with IconSize = 15 }

    let badgeGreenLegacy : List<ReactXP.LegacyStyles.RuntimeStyles> =
        let blocks : List<ISheetBuildingBlock> =
            [
                "badge" ==> LibClient.Components.BadgeStyles.Theme.One (14, ReactXP.LegacyStyles.RulesRestricted.FontWeight.Bold, Color.White, Color.DevGreen)
            ]
        blocks
        |> ReactXP.LegacyStyles.Designtime.makeSheet
        |> legacyTheme

type Props = (* GenerateMakeFunction *) {
    key: string option // defaultWithAutoWrap JsUndefined
}

type Button(_initialProps) =
    inherit PureStatelessComponent<Props, Actions, Button>("AppEggShellGallery.Components.Content.Button", _initialProps, Actions, hasStyles = false)

and Actions(_this: Button) =
    class end

let Make = makeConstructor<Button, _, _>

type Estate = NoEstate
type Pstate = NoPstate
