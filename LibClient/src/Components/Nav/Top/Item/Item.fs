namespace LibClient.Components.Nav.Top

open LibClient

module Item =
    type Badge = LibClient.Output.Badge
    let Text  = Badge.Text
    let Count = Badge.Count

    type Style =
    | Internal of MaybeLabel: Option<string> * MaybeIcon: Option<LibClient.Icons.IconConstructor> * MaybeBadge: Option<Badge>
    with
        static member With (?label: string, ?icon: LibClient.Icons.IconConstructor, ?badge: Badge) : Style =
            Internal (label, icon, badge)

    type State =
    | Actionable of OnPress: (ReactEvent.Action -> unit)
    | InProgress
    | Disabled
    | Selected
    | SelectedActionable of OnPress: (ReactEvent.Action -> unit)
    with
        member this.Name : string =
            unionCaseName this

    let labelOnly (label: string)                          : Style = Style.With(label = label)
    let iconOnly  (icon:  LibClient.Icons.IconConstructor) : Style = Style.With(icon  = icon)

namespace LibClient.Components

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Responsive

open ReactXP.Components
open ReactXP.Styles

open Nav.Top.Item

[<AutoOpen>]
module Nav_Top_Item =

    module LC =
        module Nav =
            module Top =
                module Item =
                    type AppearanceColors = {
                        Label:                Color
                        LabelWeight:          ReactXP.Styles.RulesRestricted.FontWeight
                        Background:           Color
                        Border:               Color
                        Icon:                 Color
                        BadgeFontColor:       Color
                        BadgeFontWeight:      ReactXP.Styles.RulesRestricted.FontWeight
                        BadgeBackgroundColor: Color
                    }

                    type InteractionColors = {
                        Base:      AppearanceColors
                        Hovered:   AppearanceColors
                        Depressed: AppearanceColors
                    }

                    type ScreenSizes = {
                        IconFontSize:  int
                        LabelFontSize: int
                        Height:        int
                        BadgeFontSize: int
                        BadgeTop:      int
                        BadgeLeft:     int
                    }

                    type Theme = {
                        Desktop:              ScreenSizes
                        Handheld:             ScreenSizes
                        IconVerticalAdjust:   int
                        Actionable:           InteractionColors
                        Selected:             InteractionColors
                        SelectedActionable:   InteractionColors
                        Disabled:             InteractionColors
                        InProgress:           InteractionColors
                    }

    open LC.Nav.Top.Item

    let private styleParts (style: Style) =
        match style with
        | Style.Internal (label, icon, badge) -> (label, icon, badge)

    let private pressLabel (style: Style) =
        match style with
        | Style.Internal (Some label, _, _) -> label
        | Style.Internal (None, Some _, _)  -> "Menu"
        | _                                 -> "Menu item"

    let private onPressForState (state: State) =
        match state with
        | Actionable onPress | SelectedActionable onPress -> Some onPress
        | _ -> None

    let private isSelectedState (state: State) =
        match state with
        | Selected | SelectedActionable _ -> true
        | _ -> false

    let private interactionForState (theme: Theme) (state: State) =
        match state with
        | Actionable _         -> theme.Actionable
        | Selected             -> theme.Selected
        | SelectedActionable _ -> theme.SelectedActionable
        | Disabled             -> theme.Disabled
        | InProgress           -> theme.InProgress

    let private pickColors (interaction: InteractionColors) (pointerState: LC.Pointer.State.PointerState) =
        if pointerState.IsDepressed then
            interaction.Depressed
        elif pointerState.IsHovered then
            interaction.Hovered
        else
            interaction.Base

    let private screenSizes (theme: Theme) (screenSize: ScreenSize) =
        match screenSize with
        | ScreenSize.Desktop  -> theme.Desktop
        | ScreenSize.Handheld -> theme.Handheld

    let private bottomBorderColor (state: State) (colors: AppearanceColors) =
        match state with
        | Selected | SelectedActionable _ -> colors.Border
        | _                               -> colors.Background

    [<RequireQualifiedAccess>]
    module private Styles =
        let item =
            ViewStyles.Memoize (fun (itemHeight: int) (border: Color) (background: Color) (bottomBorder: Color) ->
                makeViewStyles {
                    Position.Relative
                    paddingHorizontal 10
                    JustifyContent.SpaceAround
                    AlignItems.Center
                    height (itemHeight - 2)
                    borderColor     border
                    backgroundColor background
                    borderBottom    3 bottomBorder
                }
            )

        let contentRow =
            makeViewStyles {
                FlexDirection.Row
                AlignItems.Center
                Overflow.Visible
            }

        let contentRowWithBadge =
            makeViewStyles {
                FlexDirection.Row
                AlignItems.Center
                Overflow.Visible
                left 5
            }

        let contentRowIconLabel =
            makeViewStyles {
                FlexDirection.Row
                AlignItems.Center
                Overflow.Visible
                gap 5
            }

        let iconAdjust =
            ViewStyles.Memoize (fun (verticalAdjust: int) ->
                makeViewStyles {
                    bottom verticalAdjust
                }
            )

        let labelContent =
            makeViewStyles {
                FlexDirection.Row
                marginHorizontal 2
                Overflow.Visible
            }

        let labelContentIconBadge =
            makeViewStyles {
                FlexDirection.Row
                marginHorizontal 2
                Overflow.Visible
                left -10
            }

        let labelSentinel =
            TextStyles.Memoize (fun (labelFontSize: int) (sentinelColor: Color) ->
                makeTextStyles {
                    fontSize labelFontSize
                    color sentinelColor
                    FontWeight.W900
                }
            )

        let labelVisible =
            TextStyles.Memoize (fun (labelFontSize: int) (labelColor: Color) (weight: RulesRestricted.FontWeight) ->
                makeTextStyles {
                    Position.Absolute
                    trbl 0 0 0 0
                    fontSize labelFontSize
                    color labelColor
                    RulesRestricted.fontWeight weight
                }
            )

        let iconText =
            TextStyles.Memoize (fun (iconFontSize: int) (iconColor: Color) ->
                makeTextStyles {
                    fontSize iconFontSize
                    color iconColor
                }
            )

        let badgeHandheld =
            makeViewStyles {
                minHeight         16
                minWidth          16
                borderRadius      8
                paddingHorizontal 4
            }

        let badgePosition =
            ViewStyles.Memoize (fun (badgeTop: int) (badgeLeft: int) ->
                makeViewStyles {
                    top  badgeTop
                    left badgeLeft
                }
            )

    let private renderLabelBlock (sizes: ScreenSizes) (colors: AppearanceColors) (label: string) (withIconBadgeOffset: bool) =
        RX.View(
            styles = [| if withIconBadgeOffset then Styles.labelContentIconBadge else Styles.labelContent |],
            children =
                elements {
                    LC.UiText(value = label, styles = [| Styles.labelSentinel sizes.LabelFontSize colors.Background |])
                    LC.UiText(value = label, styles = [| Styles.labelVisible sizes.LabelFontSize colors.Label colors.LabelWeight |])
                }
        )

    let private renderIcon (sizes: ScreenSizes) (colors: AppearanceColors) (theme: Theme) (icon: LibClient.Icons.IconConstructor) =
        RX.View(
            styles = [| Styles.iconAdjust theme.IconVerticalAdjust |],
            children =
                elements {
                    LC.Icon(icon = icon, styles = [| Styles.iconText sizes.IconFontSize colors.Icon |])
                }
        )

    let private badgeTheme (sizes: ScreenSizes) (colors: AppearanceColors) : LC.Badge.Theme =
        {
            FontSize        = sizes.BadgeFontSize
            FontWeight      = colors.BadgeFontWeight
            FontColor       = colors.BadgeFontColor
            BackgroundColor = colors.BadgeBackgroundColor
        }

    let private renderBadge (badge: Badge) (sizes: ScreenSizes) (screenSize: ScreenSize) (colors: AppearanceColors) =
        RX.View(
            styles =
                [|
                    Styles.badgePosition sizes.BadgeTop sizes.BadgeLeft
                    if screenSize = ScreenSize.Handheld then Styles.badgeHandheld
                |],
            children =
                elements {
                    LC.Badge(
                        badge = badge,
                        theme = fun _ -> badgeTheme sizes colors
                    )
                }
        )

    let private renderContent
            (theme: Theme)
            (screenSize: ScreenSize)
            (colors: AppearanceColors)
            (style: Style)
            : ReactElement =
        let sizes = screenSizes theme screenSize
        let (maybeLabel, maybeIcon, maybeBadge) = styleParts style

        match maybeLabel, maybeIcon, maybeBadge with
        | Some label, Some icon, Some badge ->
            RX.View(
                styles = [| Styles.contentRowWithBadge |],
                children =
                    elements {
                        renderIcon sizes colors theme icon
                        renderBadge badge sizes screenSize colors
                        renderLabelBlock sizes colors label true
                    }
            )
        | Some label, Some icon, None ->
            RX.View(
                styles = [| Styles.contentRowIconLabel |],
                children =
                    elements {
                        renderIcon sizes colors theme icon
                        renderLabelBlock sizes colors label false
                    }
            )
        | None, Some icon, Some badge ->
            RX.View(
                styles = [| Styles.contentRowWithBadge |],
                children =
                    elements {
                        renderIcon sizes colors theme icon
                        renderBadge badge sizes screenSize colors
                    }
            )
        | Some label, None, Some badge ->
            RX.View(
                styles = [| Styles.contentRowWithBadge |],
                children =
                    elements {
                        renderLabelBlock sizes colors label false
                        renderBadge badge sizes screenSize colors
                    }
            )
        | Some label, None, None ->
            RX.View(
                styles = [| Styles.contentRow |],
                children =
                    elements {
                        renderLabelBlock sizes colors label false
                    }
            )
        | None, Some icon, None ->
            RX.View(
                styles = [| Styles.contentRow |],
                children =
                    elements {
                        renderIcon sizes colors theme icon
                    }
            )
        | _ ->
            LC.UiText(value = "combination not supported")

    type Constructors.LC.Nav.Top with
        [<Component>]
        static member Item(
                state: State,
                style: Style,
                ?testId: string,
                ?children: ReactChildrenProp,
                ?styles: array<ViewStyles>,
                ?theme: Theme -> Theme,
                ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
                ?key: string
            ) : ReactElement =
            key |> ignore
            children |> ignore

            let theTheme = Themes.GetMaybeUpdatedWith theme
            let pressLabelValue = pressLabel style
            let itemTestId =
                testId |> Option.defaultValue (A11ySlug.testId "nav-top-item" pressLabelValue)

            let legacyViewStyles : array<ViewStyles> =
                match xLegacyStyles with
                | Some legacyStyles ->
                    match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                    | []     -> [||]
                    | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
                | None -> [||]

            LC.With.ScreenSize(
                ``with`` =
                    fun screenSize ->
                        LC.Pointer.State(
                            fun pointerState ->
                                let sizes = screenSizes theTheme screenSize
                                let colors =
                                    pickColors (interactionForState theTheme state) pointerState

                                RX.View(
                                    styles =
                                        [|
                                            Styles.item sizes.Height colors.Border colors.Background (bottomBorderColor state colors)
                                            yield! legacyViewStyles
                                            yield! (styles |> Option.defaultValue [||])
                                        |],
                                    children =
                                        elements {
                                            renderContent theTheme screenSize colors style

                                            match onPressForState state with
                                            | Some onPress ->
                                                LC.Pressable(
                                                    onPress = onPress,
                                                    label = pressLabelValue,
                                                    role = AccessibilityRole.Button,
                                                    testId = itemTestId,
                                                    state =
                                                        { AccessibilityStateRecord.empty with
                                                            Selected = Some (isSelectedState state)
                                                            Disabled = match state with | Disabled -> Some true | _ -> None
                                                            Busy = match state with | InProgress -> Some true | _ -> None
                                                        },
                                                    overlay = true,
                                                    pointerState = pointerState,
                                                    componentName = "LC.Nav.Top.Item"
                                                )
                                            | None ->
                                                noElement
                                        }
                                )
                        )
            )
