namespace LibClient.Components.Nav.Bottom

open LibClient
open LibClient.Components.Button

module Button =
    type Badge = LibClient.Output.Badge
    let Text  = Badge.Text
    let Count = Badge.Count

    let Actionable = ButtonLowLevelState.Actionable
    let InProgress = ButtonLowLevelState.InProgress
    let Disabled   = ButtonLowLevelState.Disabled

    let Primary    = Level.Primary
    let Secondary  = Level.Secondary
    let PrimaryB   = Level.PrimaryB
    let SecondaryB = Level.SecondaryB
    let Tertiary   = Level.Tertiary
    let Cautionary = Level.Cautionary

    type Icon = LibClient.Components.Button.Icon

    type PropStateFactory = ButtonHighLevelStateFactory

namespace LibClient.Components

open Fable.React

open LibClient

open ReactXP.Components
open ReactXP.Styles

open Nav.Bottom.Button

[<AutoOpen>]
module Nav_Bottom_Button =

    type Constructors.LC.Nav.Bottom with
        [<Component>]
        static member Button(
                label: string,
                state: ButtonHighLevelState,
                ?level: Level,
                ?icon: Icon,
                ?badge: Badge,
                ?styles: array<ViewStyles>,
                ?contentContainerStyles: array<ViewStyles>,
                ?badgeTheme: LC.Badge.Theme -> LC.Badge.Theme,
                ?key: string
            ) : ReactElement =
            key |> ignore

            LC.Button(
                label = label,
                state = state,
                ?level = level,
                ?icon = icon,
                ?badge = badge,
                ?styles = styles,
                ?contentContainerStyles = contentContainerStyles,
                ?badgeTheme = badgeTheme
            )
