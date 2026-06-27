[<AutoOpen>]
module LibClient.Components.Button

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Responsive

open ReactXP.Components
open ReactXP.Styles

type Level =
| Primary
| Secondary
| PrimaryB
| SecondaryB
| Tertiary
| Cautionary

type ButtonLowLevelState = LibClient.Input.ButtonLowLevelState
let Actionable = ButtonLowLevelState.Actionable
let InProgress = ButtonLowLevelState.InProgress
let Disabled   = ButtonLowLevelState.Disabled

type Icon =
| No
| Left  of LibClient.Icons.IconConstructor
| Right of LibClient.Icons.IconConstructor
with
    member this.LeftOption : Option<LibClient.Icons.IconConstructor> =
        match this with
        | Left icon -> Some icon
        | _         -> None

    member this.RightOption : Option<LibClient.Icons.IconConstructor> =
        match this with
        | Right icon -> Some icon
        | _          -> None

type Badge = LibClient.Output.Badge
let Text  = Badge.Text
let Count = Badge.Count

type PropStateFactory = ButtonHighLevelStateFactory

module LC =
    module Button =
        type Appearance = {
            TextColor:       Color
            BorderColor:     Color
            BackgroundColor: Color
            FontWeight:      ReactXP.Styles.RulesRestricted.FontWeight
        }

        type StateAppearance = {
            Actionable: Appearance
            Disabled:   Appearance
            InProgress: Appearance
        }

        type Theme = {
            Primary:    StateAppearance
            Secondary:  StateAppearance
            PrimaryB:   StateAppearance
            SecondaryB: StateAppearance
            Tertiary:   StateAppearance
            Cautionary: StateAppearance
            IconSize:           int
            DesktopLabelFontSize:    int
            HandheldLabelFontSize:   int
            DesktopHeight:      int
            HandheldHeight:     int
        }

open LC.Button

let private levelAppearance (theme: Theme) (level: Level) =
    match level with
    | Primary    -> theme.Primary
    | Secondary  -> theme.Secondary
    | PrimaryB   -> theme.PrimaryB
    | SecondaryB -> theme.SecondaryB
    | Tertiary   -> theme.Tertiary
    | Cautionary -> theme.Cautionary

let private stateAppearance (levelApp: StateAppearance) (state: ButtonLowLevelState) =
    match state with
    | Actionable _ -> levelApp.Actionable
    | Disabled     -> levelApp.Disabled
    | InProgress   -> levelApp.InProgress

let private isTertiary (level: Level) =
    level = Tertiary

[<RequireQualifiedAccess>]
module private Styles =
    let viewBase (screenSize: ScreenSize) (theme: Theme) (level: Level) (state: ButtonLowLevelState) (appearance: Appearance) =
        makeViewStyles {
            Position.Relative
            FlexDirection.Column
            JustifyContent.Center
            AlignItems.Center
            borderWidth  1
            borderRadius 4
            margin       4
            borderColor     appearance.BorderColor
            backgroundColor appearance.BackgroundColor

            if not (isTertiary level) then
                paddingHV 12 4
                shadow    (Color.BlackAlpha 0.2) 5 (0, 2)

            match state with
            | Disabled -> opacity 0.5
            | Actionable _ -> Cursor.Pointer
            | _ -> Noop

            match screenSize with
            | ScreenSize.Desktop ->
                height theme.DesktopHeight
                paddingBottom 5
            | ScreenSize.Handheld ->
                height theme.HandheldHeight
        }

    let viewPointer (level: Level) (pointerState: LC.Pointer.State.PointerState) =
        if isTertiary level then
            makeViewStyles { Noop }
        elif pointerState.IsDepressed then
            makeViewStyles {
                shadow (Color.BlackAlpha 0.2) 3 (0, 0)
                top    1
            }
        elif pointerState.IsHovered then
            makeViewStyles {
                shadow (Color.BlackAlpha 0.2) 5 (0, 3)
                top    -1
            }
        else
            makeViewStyles { Noop }

    let labelBlock =
        makeViewStyles {
            AlignItems.Center
            FlexDirection.Row
            JustifyContent.Center
        }

    let labelText (screenSize: ScreenSize) (theme: Theme) (appearance: Appearance) =
        makeTextStyles {
            TextAlign.Center
            flex 1
            color      appearance.TextColor
            RulesRestricted.fontWeight appearance.FontWeight

            match screenSize with
            | ScreenSize.Desktop  -> fontSize theme.DesktopLabelFontSize
            | ScreenSize.Handheld -> fontSize theme.HandheldLabelFontSize
        }

    let leftIcon =
        makeViewStyles { marginRight 5 }

    let rightIcon =
        makeViewStyles { marginLeft 5 }

    let icon (theme: Theme) (appearance: Appearance) =
        makeTextStyles {
            color    appearance.TextColor
            fontSize theme.IconSize
        }

    let badge =
        makeViewStyles { marginLeft 5 }

    let spinnerBlock =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
            backgroundColor (Color.WhiteAlpha 0.5)
            AlignItems.Center
            JustifyContent.Center
        }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Button(
            label: string,
            state: ButtonHighLevelState,
            ?children: ReactChildrenProp,
            ?level: Level,
            ?icon: Icon,
            ?badge: Badge,
            ?styles: array<ViewStyles>,
            ?contentContainerStyles: array<ViewStyles>,
            ?testId: string,
            ?key: string,
            ?theme: Theme -> Theme,
            ?badgeTheme: LC.Badge.Theme -> LC.Badge.Theme,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore
        children |> ignore

        let level = defaultArg level Primary
        let icon = defaultArg icon No
        let theTheme = Themes.GetMaybeUpdatedWith theme
        let theBadgeTheme = Themes.GetMaybeUpdatedWith badgeTheme
        let lowLevelState = state.ToLowLevel
        let appearance = stateAppearance (levelAppearance theTheme level) lowLevelState

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
                            RX.View(
                                styles =
                                    [|
                                        Styles.viewBase screenSize theTheme level lowLevelState appearance
                                        Styles.viewPointer level pointerState
                                        yield! legacyViewStyles
                                        yield! (styles |> Option.defaultValue [||])
                                    |],
                                children =
                                    elements {
                                        RX.View(
                                            styles =
                                                [|
                                                    Styles.labelBlock
                                                    yield! (contentContainerStyles |> Option.defaultValue [||])
                                                |],
                                            children =
                                                elements {
                                                    match icon.LeftOption with
                                                    | Some leftIcon ->
                                                        RX.View(
                                                            styles = [| Styles.leftIcon |],
                                                            children =
                                                                elements {
                                                                    LC.Icon(
                                                                        icon = leftIcon,
                                                                        styles = [| Styles.icon theTheme appearance |]
                                                                    )
                                                                }
                                                        )
                                                    | None ->
                                                        noElement

                                                    LC.UiText(
                                                        value = label,
                                                        numberOfLines = 1,
                                                        ellipsizeMode = EllipsizeMode.Tail,
                                                        styles = [| Styles.labelText screenSize theTheme appearance |]
                                                    )

                                                    match icon.RightOption with
                                                    | Some rightIcon ->
                                                        RX.View(
                                                            styles = [| Styles.rightIcon |],
                                                            children =
                                                                elements {
                                                                    LC.Icon(
                                                                        icon = rightIcon,
                                                                        styles = [| Styles.icon theTheme appearance |]
                                                                    )
                                                                }
                                                        )
                                                    | None ->
                                                        noElement

                                                    match badge with
                                                    | Some badge ->
                                                        RX.View(
                                                            styles = [| Styles.badge |],
                                                            children =
                                                                elements {
                                                                    LC.Badge(
                                                                        badge = badge,
                                                                        theme = fun _ -> theBadgeTheme
                                                                    )
                                                                }
                                                        )
                                                    | None ->
                                                        noElement
                                                }
                                        )

                                        match lowLevelState with
                                        | InProgress ->
                                            RX.View(
                                                styles = [| Styles.spinnerBlock |],
                                                children =
                                                    elements {
                                                        RX.ActivityIndicator(
                                                            color = "#aaaaaa",
                                                            size = Size.Tiny
                                                        )
                                                    }
                                            )
                                        | _ ->
                                            noElement

                                        match lowLevelState with
                                        | Actionable onPress ->
                                            LC.Pressable(
                                                onPress = onPress,
                                                label = label,
                                                role = AccessibilityRole.Button,
                                                ?testId = testId,
                                                overlay = true,
                                                pointerState = pointerState,
                                                componentName = "LC.Button"
                                            )
                                        | _ ->
                                            noElement
                                    }
                            )
                    )
        )
