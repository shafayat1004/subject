[<AutoOpen>]
module LibClient.Components.Badge

open Fable.React

open LibClient

open ReactXP.Components
open ReactXP.Styles

type Badge = LibClient.Output.Badge
let Text      = Badge.Text
let Count     = Badge.Count
let NoContent = Badge.NoContent

module LC =
    module Badge =
        type Theme = {
            FontSize:        int
            FontWeight:      ReactXP.Styles.RulesRestricted.FontWeight
            FontColor:       Color
            BackgroundColor: Color
        }

open LC.Badge

[<RequireQualifiedAccess>]
module private Styles =
    let view (theme: Theme) =
        makeViewStyles {
            flex              0
            minHeight         22
            minWidth          22
            paddingHorizontal 6
            borderRadius      11
            JustifyContent.Center
            backgroundColor   theme.BackgroundColor
        }

    let noContent (theme: Theme) =
        makeViewStyles {
            minHeight         10
            minWidth          10
            borderRadius      5
            JustifyContent.Center
            backgroundColor   theme.BackgroundColor
        }

    let text (theme: Theme) =
        makeTextStyles {
            TextAlign.Center
            fontSize                   theme.FontSize
            RulesRestricted.fontWeight theme.FontWeight
            color                      theme.FontColor
        }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Badge(
            badge:          Badge,
            ?theme:         Theme -> Theme,
            ?styles:        array<ViewStyles>,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key:           string
        ) : ReactElement =
        key |> ignore

        let theTheme = Themes.GetMaybeUpdatedWith theme

        let legacyViewStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
            | None -> [||]

        let extraStyles = styles |> Option.defaultValue [||]

        match badge with
        | Badge.Count count ->
            RX.View(
                styles = [| Styles.view theTheme; yield! legacyViewStyles; yield! extraStyles |],
                children =
                    elements {
                        LC.UiText(value = string count, styles = [| Styles.text theTheme |])
                    }
            )
        | Badge.Text text ->
            RX.View(
                styles = [| Styles.view theTheme; yield! legacyViewStyles; yield! extraStyles |],
                children =
                    elements {
                        LC.UiText(
                            value = text,
                            numberOfLines = 1,
                            styles = [| Styles.text theTheme |]
                        )
                    }
            )
        | Badge.NoContent ->
            RX.View(
                styles = [| Styles.noContent theTheme; yield! legacyViewStyles; yield! extraStyles |]
            )
