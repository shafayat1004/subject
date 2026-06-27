[<AutoOpen>]
module LibRouter.Components.Nav_Top_BackButton

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Icons
open LibClient.Components.Nav.Top.Item
open ReactXP.Styles
open ReactXP.LegacyStyles

type LegacyFontWeight = RulesRestricted.FontWeight

module LR =
    module Nav =
        module Top =
            module BackButtonTypes =
                type Colors = {
                    Label: Color
                    LabelWeight: LegacyFontWeight
                    Background: Color
                    Border: Color
                    Icon: Color
                    BadgeFontWeight: LegacyFontWeight
                    BadgeFontColor: Color
                    BadgeBackgroundColor: Color
                }

                type StateTheme = {
                    BaseColors: Colors
                    HoveredColors: Colors
                    DepressedColors: Colors
                }

                type Theme = {
                    Actionable: StateTheme
                    InProgress: StateTheme
                    Disabled: StateTheme
                    Selected: StateTheme
                    SelectedActionable: StateTheme
                }

open LR.Nav.Top.BackButtonTypes

let private mapFontWeight (weight: LegacyFontWeight) : ReactXP.Styles.RulesRestricted.FontWeight =
    match weight with
    | LegacyFontWeight.Normal -> ReactXP.Styles.RulesRestricted.FontWeight.Normal
    | LegacyFontWeight.Bold  -> ReactXP.Styles.RulesRestricted.FontWeight.Bold
    | _ -> ReactXP.Styles.RulesRestricted.FontWeight.Normal

let private mapColors (colors: Colors) : LC.Nav.Top.Item.AppearanceColors =
    {
        Label                = colors.Label
        LabelWeight          = mapFontWeight colors.LabelWeight
        Background           = colors.Background
        Border               = colors.Border
        Icon                 = colors.Icon
        BadgeFontColor       = colors.BadgeFontColor
        BadgeFontWeight      = mapFontWeight colors.BadgeFontWeight
        BadgeBackgroundColor = colors.BadgeBackgroundColor
    }

let private mapInteraction (stateTheme: StateTheme) : LC.Nav.Top.Item.InteractionColors =
    {
        Base      = mapColors stateTheme.BaseColors
        Hovered   = mapColors stateTheme.HoveredColors
        Depressed = mapColors stateTheme.DepressedColors
    }

let private toItemTheme (backTheme: Theme) (itemTheme: LC.Nav.Top.Item.Theme) : LC.Nav.Top.Item.Theme =
    {
        itemTheme with
            Actionable         = mapInteraction backTheme.Actionable
            InProgress         = mapInteraction backTheme.InProgress
            Disabled           = mapInteraction backTheme.Disabled
            Selected           = mapInteraction backTheme.Selected
            SelectedActionable = mapInteraction backTheme.SelectedActionable
    }

type LibRouter.Components.Constructors.LR.Nav.Top with
    [<Component>]
    static member BackButton(
            ?theme: Theme -> Theme,
            ?styles: array<ViewStyles>,
            ?key: string) : ReactElement =
        key |> ignore

        let theTheme = Themes.GetMaybeUpdatedWith theme
        let navigate = LibRouter.Components.Router.useNavigate()

        let goBack (_: ReactEvent.Action) =
            navigate.GoBack()

        LC.Nav.Top.Item(
            styles = (styles |> Option.defaultValue [||]),
            style = Nav.Top.Item.iconOnly Icon.Back,
            state = Nav.Top.Item.State.Actionable goBack,
            theme = toItemTheme theTheme
        )
