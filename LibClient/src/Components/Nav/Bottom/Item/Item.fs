namespace LibClient.Components.Nav.Bottom

open LibClient
open LibClient.Input

module Item =
    type Badge = LibClient.Output.Badge
    let Text  = Badge.Text
    let Count = Badge.Count

    type LabelPosition =
    | SideLabel
    | BottomLabel

    type Style =
    | Internal of MaybeLabel: Option<string> * MaybeIcon: Option<LibClient.Icons.IconConstructor> * LabelPosition * MaybeBadge: Option<Badge>
    with
        static member With (?label: string, ?icon: LibClient.Icons.IconConstructor, ?labelPosition: LabelPosition, ?badge: Badge) : Style =
            let labelPosition = defaultArg labelPosition LabelPosition.SideLabel
            Internal (label, icon, labelPosition, badge)

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

open Nav.Bottom.Item

[<AutoOpen>]
module Nav_Bottom_Item =

    module LC =
        module Nav =
            module Bottom =
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
                        Desktop:            ScreenSizes
                        Handheld:           ScreenSizes
                        IconVerticalAdjust: int
                        Actionable:         InteractionColors
                        Selected:           InteractionColors
                        SelectedActionable: InteractionColors
                        Disabled:           InteractionColors
                        InProgress:         InteractionColors
                    }

    open LC.Nav.Bottom.Item

    let private styleParts (style: Style) =
        match style with
        | Style.Internal (label, icon, labelPosition, badge) -> (label, icon, labelPosition, badge)

    let private pressLabel (style: Style) =
        match style with
        | Style.Internal (Some label, _, _, _) -> label
        | Style.Internal (None, Some _, _, _)  -> "Menu"
        | _                                    -> "Menu item"

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

    let private badgeTheme (sizes: ScreenSizes) (colors: AppearanceColors) : LC.Badge.Theme =
        {
            FontSize        = sizes.BadgeFontSize
            FontWeight      = colors.BadgeFontWeight
            FontColor       = colors.BadgeFontColor
            BackgroundColor = colors.BadgeBackgroundColor
        }

    [<RequireQualifiedAccess>]
    module private Styles =
        let item (sizes: ScreenSizes) (colors: AppearanceColors) =
            makeViewStyles {
                AlignItems.Center
                JustifyContent.SpaceAround
                marginHorizontal 5
                borderRadius 4
                height (sizes.Height - 2)
                borderColor     colors.Border
                backgroundColor colors.Background
            }

        let contentContainer (labelPosition: LabelPosition) =
            makeViewStyles {
                Overflow.Visible
                AlignItems.Center
                match labelPosition with
                | LabelPosition.SideLabel   -> FlexDirection.Row
                | LabelPosition.BottomLabel -> FlexDirection.Column
            }

        let contentContainerWithBadge (labelPosition: LabelPosition) =
            makeViewStyles {
                Overflow.Visible
                AlignItems.Center
                left 5
                match labelPosition with
                | LabelPosition.SideLabel   -> FlexDirection.Row
                | LabelPosition.BottomLabel -> FlexDirection.Column
            }

        let labelContent =
            makeViewStyles {
                Overflow.Visible
                FlexDirection.Row
                paddingRight 5
                marginLeft 5
            }

        let labelContentWithIconBadge =
            makeViewStyles {
                Overflow.Visible
                FlexDirection.Row
                paddingRight 5
                marginLeft 5
                left -10
            }

        let label (sizes: ScreenSizes) (colors: AppearanceColors) =
            makeTextStyles {
                fontSize                   sizes.LabelFontSize
                RulesRestricted.fontWeight colors.LabelWeight
                color                      colors.Label
            }

        let iconAdjust (theme: Theme) =
            makeViewStyles {
                bottom theme.IconVerticalAdjust
            }

        let iconText (sizes: ScreenSizes) (colors: AppearanceColors) =
            makeTextStyles {
                fontSize sizes.IconFontSize
                color    colors.Icon
            }

        let badgeHandheld =
            makeViewStyles {
                minHeight         16
                minWidth          16
                borderRadius      8
                paddingHorizontal 4
            }

        let badgePosition (sizes: ScreenSizes) =
            makeViewStyles {
                top  sizes.BadgeTop
                left sizes.BadgeLeft
            }

    let private renderLabel (sizes: ScreenSizes) (colors: AppearanceColors) (label: string) (withIconBadgeOffset: bool) =
        RX.View(
            styles = [| if withIconBadgeOffset then Styles.labelContentWithIconBadge else Styles.labelContent |],
            children =
                elements {
                    LC.UiText(value = label, styles = [| Styles.label sizes colors |])
                }
        )

    let private renderIcon (sizes: ScreenSizes) (colors: AppearanceColors) (theme: Theme) (icon: LibClient.Icons.IconConstructor) =
        RX.View(
            styles = [| Styles.iconAdjust theme |],
            children =
                elements {
                    LC.Icon(icon = icon, styles = [| Styles.iconText sizes colors |])
                }
        )

    let private renderBadge (badge: Badge) (sizes: ScreenSizes) (screenSize: ScreenSize) (colors: AppearanceColors) =
        RX.View(
            styles =
                [|
                    Styles.badgePosition sizes
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
        let (maybeLabel, maybeIcon, labelPosition, maybeBadge) = styleParts style

        match maybeLabel, maybeIcon, maybeBadge with
        | Some label, Some icon, Some badge ->
            RX.View(
                styles = [| Styles.contentContainerWithBadge labelPosition |],
                children =
                    elements {
                        renderIcon sizes colors theme icon
                        renderBadge badge sizes screenSize colors
                        renderLabel sizes colors label true
                    }
            )
        | Some label, Some icon, None ->
            RX.View(
                styles = [| Styles.contentContainer labelPosition |],
                children =
                    elements {
                        renderIcon sizes colors theme icon
                        renderLabel sizes colors label false
                    }
            )
        | None, Some icon, Some badge ->
            RX.View(
                styles = [| Styles.contentContainerWithBadge LabelPosition.SideLabel |],
                children =
                    elements {
                        renderIcon sizes colors theme icon
                        renderBadge badge sizes screenSize colors
                    }
            )
        | Some label, None, Some badge ->
            RX.View(
                styles = [| Styles.contentContainerWithBadge LabelPosition.SideLabel |],
                children =
                    elements {
                        renderLabel sizes colors label false
                        renderBadge badge sizes screenSize colors
                    }
            )
        | Some label, None, None ->
            RX.View(
                styles = [| Styles.contentContainer LabelPosition.SideLabel |],
                children =
                    elements {
                        renderLabel sizes colors label false
                    }
            )
        | None, Some icon, None ->
            RX.View(
                styles = [| Styles.contentContainer LabelPosition.SideLabel |],
                children =
                    elements {
                        renderIcon sizes colors theme icon
                    }
            )
        | _ ->
            LC.UiText(value = "combination not supported")

    type Constructors.LC.Nav.Bottom with
        [<Component>]
        static member Item(
                state: State,
                style: Style,
                ?children: ReactChildrenProp,
                ?styles: array<ViewStyles>,
                ?theme: Theme -> Theme,
                ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
                ?key: string
            ) : ReactElement =
            key |> ignore
            children |> ignore

            let theTheme = Themes.GetMaybeUpdatedWith theme

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
                                            Styles.item sizes colors
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
                                                    label = pressLabel style,
                                                    role = AccessibilityRole.Button,
                                                    state =
                                                        { AccessibilityStateRecord.empty with
                                                            Selected = Some (isSelectedState state)
                                                            Disabled = match state with | Disabled -> Some true | _ -> None
                                                            Busy = match state with | InProgress -> Some true | _ -> None
                                                        },
                                                    overlay = true,
                                                    pointerState = pointerState,
                                                    componentName = "LC.Nav.Bottom.Item"
                                                )
                                            | None ->
                                                noElement
                                        }
                                )
                        )
            )
