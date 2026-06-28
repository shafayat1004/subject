[<AutoOpen>]
module LibClient.Components.IconWithBadge

open Fable.React

open LibClient

open ReactXP.Components
open ReactXP.Styles

type Badge = LibClient.Output.Badge
let Text  = Badge.Text
let Count = Badge.Count

module LC =
    module IconWithBadge =
        type Theme = {
            IconColor:       Color
            IconSize:        int
            BadgeMarginLeft: int
            Badge:           LC.Badge.Theme
        }

open LC.IconWithBadge

[<RequireQualifiedAccess>]
module private Styles =
    let row =
        makeViewStyles {
            FlexDirection.Row
            AlignItems.Center
        }

    let badgeWrap =
        ViewStyles.Memoize (fun (badgeMarginLeft: int) ->
            makeViewStyles {
                marginLeft badgeMarginLeft
            }
        )

    let iconText =
        TextStyles.Memoize (fun (iconSize: int) (iconColor: Color) ->
            makeTextStyles {
                fontSize iconSize
                color    iconColor
            }
        )

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member IconWithBadge(
            icon:           LibClient.Icons.IconConstructor,
            badge:          Badge,
            ?theme:         Theme -> Theme,
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

        RX.View(
            styles = [| Styles.row; yield! legacyViewStyles |],
            children =
                elements {
                    LC.Icon(icon = icon, styles = [| Styles.iconText theTheme.IconSize theTheme.IconColor |])
                    RX.View(
                        styles = [| Styles.badgeWrap theTheme.BadgeMarginLeft |],
                        children =
                            elements {
                                LC.Badge(
                                    badge = badge,
                                    theme = fun _ -> theTheme.Badge
                                )
                            }
                    )
                }
        )
