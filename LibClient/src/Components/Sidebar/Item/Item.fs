namespace LibClient.Components.Sidebar

open LibClient

module Item =
    type State =
    | Actionable of OnPress: (ReactEvent.Action -> unit)
    | InProgress
    | Disabled
    | Selected
    with
        member this.Name : string =
            unionCaseName this

    type [<RequireQualifiedAccess>] Right =
    | Badge of PositiveInteger
    | Icon  of Icons.IconConstructor
    | NoElement

namespace LibClient.Components

open Fable.React

open LibClient
open LibClient.Accessibility

open Rn.Components
open Rn.Styles

open Sidebar.Item

[<AutoOpen>]
module Sidebar_Item =

    module LC =
        module Sidebar =
            module Item =
                type Colors = {
                    Label:           Color
                    LabelWeight:     Rn.Styles.RulesRestricted.FontWeight
                    Background:      Color
                    Border:          Color
                    LeftIcon:        Color
                    RightIcon:       Color
                    BadgeBackground: Color
                    BadgeText:       Color
                }

                type InteractionColors = {
                    Base:      Colors
                    Hovered:   Colors
                    Depressed: Colors
                }

                type Theme = {
                    IconFontSize:  int
                    LabelFontSize: int
                    BadgeFontSize: int
                    ItemHeight:    int
                    Actionable:    InteractionColors
                    Selected:      InteractionColors
                    Disabled:      InteractionColors
                    InProgress:    InteractionColors
                }

    open LC.Sidebar.Item

    let private pickColors (interaction: InteractionColors) (pointerState: LC.Pointer.State.PointerState) =
        if pointerState.IsDepressed then
            interaction.Depressed
        elif pointerState.IsHovered then
            interaction.Hovered
        else
            interaction.Base

    let private interactionForState (theme: Theme) (state: State) =
        match state with
        | Actionable _ -> theme.Actionable
        | Selected     -> theme.Selected
        | Disabled     -> theme.Disabled
        | InProgress   -> theme.InProgress

    let private itemA11yState (state: State) (isSelected: bool) =
        match state with
        | Selected   -> AccessibilityStateRecord.selected true
        | Disabled   -> AccessibilityStateRecord.disabled true
        | InProgress -> AccessibilityStateRecord.busy true
        | Actionable _ ->
            { AccessibilityStateRecord.empty with Selected = Some isSelected }

    let private itemTestId (label: string) (testId: string option) =
        testId |> Option.orElse (Some (A11ySlug.testId "sidebar-item" label))

    let private badgeText (count: PositiveInteger) =
        if count <= PositiveInteger.ofLiteral 99 then
            string count
        else
            "99+"

    [<RequireQualifiedAccess>]
    module private Styles =
        let item =
            ViewStyles.Memoize (fun (itemHeight: int) (border: Color) (background: Color) ->
                makeViewStyles {
                    FlexDirection.Row
                    flex              1
                    height            itemHeight
                    paddingHorizontal 18
                    borderColor       border
                    backgroundColor   background
                }
            )

        let left =
            makeViewStyles {
                AlignItems.Center
                JustifyContent.Center
                flex        0
                width       32
                marginRight 10
            }

        let middle =
            makeViewStyles {
                flex 1
                FlexDirection.Row
                AlignItems.Center
            }

        let right =
            makeViewStyles {
                AlignItems.Center
                JustifyContent.Center
                flex 0
            }

        let iconText =
            TextStyles.Memoize (fun (iconFontSize: int) (iconColor: Color) ->
                makeTextStyles {
                    fontSize iconFontSize
                    color    iconColor
                }
            )

        let labelText =
            TextStyles.Memoize (fun (labelFontSize: int) (labelColor: Color) (weight: RulesRestricted.FontWeight) ->
                makeTextStyles {
                    fontSize labelFontSize
                    color    labelColor
                    RulesRestricted.fontWeight weight
                }
            )

        let badgeContainer =
            ViewStyles.Memoize (fun (background: Color) ->
                makeViewStyles {
                    AlignItems.Center
                    JustifyContent.Center
                    flex              0
                    height            28
                    minWidth          28
                    paddingHorizontal 8
                    borderRadius      14
                    backgroundColor   background
                }
            )

        let badgeTextStyle =
            TextStyles.Memoize (fun (badgeFontSize: int) (badgeColor: Color) ->
                makeTextStyles {
                    fontSize badgeFontSize
                    color badgeColor
                }
            )

    type Constructors.LC.Sidebar with
        [<Component>]
        static member Item(
                label: string,
                state: State,
                ?children: ReactChildrenProp,
                ?leftIcon: Icons.IconConstructor,
                ?right: Right,
                ?styles: array<ViewStyles>,
                ?testId: string,
                ?theme: Theme -> Theme,
                ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>,
                ?key: string
            ) : ReactElement =
            key |> ignore
            children |> ignore

            let theTheme = Themes.GetMaybeUpdatedWith theme
            let isSelected = match state with | Selected -> true | _ -> false

            let legacyViewStyles : array<ViewStyles> =
                match xLegacyStyles with
                | Some legacyStyles ->
                    match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                    | []     -> [||]
                    | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
                | None -> [||]

            let onPress =
                match state with
                | Actionable onPress -> Some onPress
                | _                  -> None

            LC.Pointer.State(
                fun pointerState ->
                    let colors =
                        pickColors (interactionForState theTheme state) pointerState

                    Rn.View(
                        styles =
                            [|
                                Styles.item theTheme.ItemHeight colors.Border colors.Background
                                yield! legacyViewStyles
                                yield! (styles |> Option.defaultValue [||])
                            |],
                        children =
                            elements {
                                match leftIcon with
                                | Some icon ->
                                    Rn.View(
                                        styles = [| Styles.left |],
                                        children =
                                            elements {
                                                LC.Icon(
                                                    icon = icon,
                                                    styles = [| Styles.iconText theTheme.IconFontSize colors.LeftIcon |]
                                                )
                                            }
                                    )
                                | None ->
                                    noElement

                                Rn.View(
                                    styles = [| Styles.middle |],
                                    children =
                                        elements {
                                            LC.UiText(
                                                value = label,
                                                selectable = true,
                                                styles = [| Styles.labelText theTheme.LabelFontSize colors.Label colors.LabelWeight |]
                                            )
                                        }
                                )

                                match state with
                                | InProgress ->
                                    Rn.View(
                                        styles = [| Styles.right |],
                                        children =
                                            elements {
                                                Rn.ActivityIndicator(color = "#aaaaaa", size = Size.Small)
                                            }
                                    )
                                | _ ->
                                    match right with
                                    | Some (Right.Badge count) ->
                                        Rn.View(
                                            styles = [| Styles.right |],
                                            children =
                                                elements {
                                                    Rn.View(
                                                        styles = [| Styles.badgeContainer colors.BadgeBackground |],
                                                        children =
                                                            elements {
                                                                LC.UiText(
                                                                    value = badgeText count,
                                                                    styles = [| Styles.badgeTextStyle theTheme.BadgeFontSize colors.BadgeText |]
                                                                )
                                                            }
                                                    )
                                                }
                                        )
                                    | Some (Right.Icon icon) ->
                                        Rn.View(
                                            styles = [| Styles.right |],
                                            children =
                                                elements {
                                                    LC.Icon(
                                                        icon = icon,
                                                        styles = [| Styles.iconText theTheme.IconFontSize colors.RightIcon |]
                                                    )
                                                }
                                        )
                                    | Some Right.NoElement | None ->
                                        noElement

                                match onPress with
                                | Some onPress ->
                                    LC.Pressable(
                                        onPress = onPress,
                                        label = label,
                                        role = AccessibilityRole.MenuItem,
                                        state = itemA11yState state isSelected,
                                        testId = (itemTestId label testId |> Option.defaultValue (A11ySlug.testId "sidebar-item" label)),
                                        overlay = true,
                                        pointerState = pointerState,
                                        componentName = "LC.Sidebar.Item"
                                    )
                                | None ->
                                    noElement
                            }
                    )
            )
