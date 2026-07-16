[<AutoOpen>]
module LibClient.Components.Button

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Responsive

open Rn.Components
open Rn.Styles

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
            FontWeight:      Rn.Styles.RulesRestricted.FontWeight
        }

        type StateAppearance = {
            Actionable: Appearance
            Disabled:   Appearance
            InProgress: Appearance
        }

        type Theme = {
            Primary:               StateAppearance
            Secondary:             StateAppearance
            PrimaryB:              StateAppearance
            SecondaryB:            StateAppearance
            Tertiary:              StateAppearance
            Cautionary:            StateAppearance
            IconSize:              int
            DesktopLabelFontSize:  int
            HandheldLabelFontSize: int
            DesktopHeight:         int
            HandheldHeight:        int
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


let private levelTag (level: Level) =
    match level with
    | Primary    -> 0
    | Secondary  -> 1
    | PrimaryB   -> 2
    | SecondaryB -> 3
    | Tertiary   -> 4
    | Cautionary -> 5

let private fontWeightTag (weight: Rn.Styles.RulesRestricted.FontWeight) =
    match weight with
    | Rn.Styles.RulesRestricted.FontWeight.Normal -> 0
    | Rn.Styles.RulesRestricted.FontWeight.Bold   -> 1
    | Rn.Styles.RulesRestricted.FontWeight.W100   -> 2
    | Rn.Styles.RulesRestricted.FontWeight.W200   -> 3
    | Rn.Styles.RulesRestricted.FontWeight.W300   -> 4
    | Rn.Styles.RulesRestricted.FontWeight.W400   -> 5
    | Rn.Styles.RulesRestricted.FontWeight.W500   -> 6
    | Rn.Styles.RulesRestricted.FontWeight.W600   -> 7
    | Rn.Styles.RulesRestricted.FontWeight.W700   -> 8
    | Rn.Styles.RulesRestricted.FontWeight.W800   -> 9
    | Rn.Styles.RulesRestricted.FontWeight.W900   -> 10

let private fontWeightForTag (tag: int) =
    match tag with
    | 1  -> Rn.Styles.RulesRestricted.FontWeight.Bold
    | 2  -> Rn.Styles.RulesRestricted.FontWeight.W100
    | 3  -> Rn.Styles.RulesRestricted.FontWeight.W200
    | 4  -> Rn.Styles.RulesRestricted.FontWeight.W300
    | 5  -> Rn.Styles.RulesRestricted.FontWeight.W400
    | 6  -> Rn.Styles.RulesRestricted.FontWeight.W500
    | 7  -> Rn.Styles.RulesRestricted.FontWeight.W600
    | 8  -> Rn.Styles.RulesRestricted.FontWeight.W700
    | 9  -> Rn.Styles.RulesRestricted.FontWeight.W800
    | 10 -> Rn.Styles.RulesRestricted.FontWeight.W900
    | _  -> Rn.Styles.RulesRestricted.FontWeight.Normal

[<RequireQualifiedAccess>]
module private Styles =
    let viewBase =
        ViewStyles.Memoize (fun (levelTag: int) (stateName: string) (borderCss: string) (backgroundCss: string) (itemHeight: int) (isDesktop: bool) ->
            makeViewStyles {
                Position.Relative
                FlexDirection.Column
                JustifyContent.Center
                AlignItems.Center
                borderWidth  1
                borderRadius 4
                margin       4
                borderColor     (Color.InternalString borderCss)
                backgroundColor (Color.InternalString backgroundCss)

                if levelTag <> 4 then
                    paddingHV 12 4
                    shadow    (Color.BlackAlpha 0.2) 5 (0, 2)

                match stateName with
                | "Disabled"   -> opacity 0.5
                | "Actionable" -> Cursor.Pointer
                | _            -> Noop

                height itemHeight
                if isDesktop then
                    paddingBottom 5
            })

    let viewPointerNoop = makeViewStyles { Noop }

    let viewPointer =
        ViewStyles.Memoize (fun (levelTag: int) (isDepressed: bool) (isHovered: bool) ->
            if levelTag = 4 then
                viewPointerNoop
            elif isDepressed then
                makeViewStyles {
                    shadow (Color.BlackAlpha 0.2) 3 (0, 0)
                    top    1
                }
            elif isHovered then
                makeViewStyles {
                    shadow (Color.BlackAlpha 0.2) 5 (0, 3)
                    top    -1
                }
            else
                viewPointerNoop
            )

    let labelBlock =
        makeViewStyles {
            AlignItems.Center
            FlexDirection.Row
            JustifyContent.Center
        }

    let labelText =
        TextStyles.Memoize (fun (isDesktop: bool) (textColorCss: string) (fontWeightTag: int) (desktopFontSize: int) (handheldFontSize: int) ->
            makeTextStyles {
                TextAlign.Center
                flex 1
                color      (Color.InternalString textColorCss)
                RulesRestricted.fontWeight (fontWeightForTag fontWeightTag)

                if isDesktop then
                    fontSize desktopFontSize
                else
                    fontSize handheldFontSize
            })

    let labelTextFor (screenSize: ScreenSize) (theme: Theme) (appearance: Appearance) =
        labelText
            (screenSize = ScreenSize.Desktop)
            appearance.TextColor.ToCssString
            (fontWeightTag appearance.FontWeight)
            theme.DesktopLabelFontSize
            theme.HandheldLabelFontSize

    let leftIcon =
        makeViewStyles { marginRight 5 }

    let rightIcon =
        makeViewStyles { marginLeft 5 }

    let icon =
        TextStyles.Memoize (fun (iconSize: int) (textColorCss: string) ->
            makeTextStyles {
                color    (Color.InternalString textColorCss)
                fontSize iconSize
            })

    let iconFor (theme: Theme) (appearance: Appearance) =
        icon theme.IconSize appearance.TextColor.ToCssString

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
            label:                   string,
            state:                   ButtonHighLevelState,
            ?children:               ReactChildrenProp,
            ?level:                  Level,
            ?icon:                   Icon,
            ?badge:                  Badge,
            ?styles:                 array<ViewStyles>,
            ?contentContainerStyles: array<ViewStyles>,
            ?testId:                 string,
            ?key:                    string,
            ?theme:                  Theme -> Theme,
            ?badgeTheme:             LC.Badge.Theme -> LC.Badge.Theme,
            ?xLegacyStyles:          List<Rn.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key      |> ignore
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
                match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
            | None -> [||]

        LC.With.ScreenSize(
            ``with`` =
                fun screenSize ->
                    LC.Pointer.State(
                        fun pointerState ->
                            let itemHeight =
                                match screenSize with
                                | ScreenSize.Desktop  -> theTheme.DesktopHeight
                                | ScreenSize.Handheld -> theTheme.HandheldHeight
                            let isDesktop = screenSize = ScreenSize.Desktop

                            let maybeContainerRole, maybeContainerLabel, maybeContainerState, maybeContainerTestId =
                                match lowLevelState with
                                | Actionable _ -> None, None, None, None
                                | Disabled ->
                                    Some AccessibilityRole.Button,
                                    Some label,
                                    Some (AccessibilityStateRecord.toJs (AccessibilityStateRecord.disabled true)),
                                    testId
                                | InProgress ->
                                    Some AccessibilityRole.Button,
                                    Some label,
                                    Some (AccessibilityStateRecord.toJs (AccessibilityStateRecord.busy true)),
                                    testId

                            Rn.View(
                                styles =
                                    [|
                                        Styles.viewBase
                                            (levelTag level)
                                            lowLevelState.GetName
                                            appearance.BorderColor.ToCssString
                                            appearance.BackgroundColor.ToCssString
                                            itemHeight
                                            isDesktop
                                        Styles.viewPointer (levelTag level) pointerState.IsDepressed pointerState.IsHovered
                                        yield! legacyViewStyles
                                        yield! (styles |> Option.defaultValue [||])
                                    |],
                                ?accessibilityRole  = maybeContainerRole,
                                ?accessibilityLabel = maybeContainerLabel,
                                ?accessibilityState = maybeContainerState,
                                ?testId             = maybeContainerTestId,
                                children =
                                    elements {
                                        Rn.View(
                                            styles =
                                                [|
                                                    Styles.labelBlock
                                                    yield! (contentContainerStyles |> Option.defaultValue [||])
                                                |],
                                            children =
                                                elements {
                                                    match icon.LeftOption with
                                                    | Some leftIcon ->
                                                        Rn.View(
                                                            styles = [| Styles.leftIcon |],
                                                            children =
                                                                elements {
                                                                    LC.Icon(
                                                                        icon       = leftIcon,
                                                                        decorative = true,
                                                                        styles     = [| Styles.iconFor theTheme appearance |]
                                                                    )
                                                                }
                                                        )
                                                    | None ->
                                                        noElement

                                                    LC.UiText(
                                                        value         = label,
                                                        numberOfLines = 1,
                                                        ellipsizeMode = EllipsizeMode.Tail,
                                                        styles        = [| Styles.labelTextFor screenSize theTheme appearance |]
                                                    )

                                                    match icon.RightOption with
                                                    | Some rightIcon ->
                                                        Rn.View(
                                                            styles = [| Styles.rightIcon |],
                                                            children =
                                                                elements {
                                                                    LC.Icon(
                                                                        icon       = rightIcon,
                                                                        decorative = true,
                                                                        styles     = [| Styles.iconFor theTheme appearance |]
                                                                    )
                                                                }
                                                        )
                                                    | None ->
                                                        noElement

                                                    match badge with
                                                    | Some badge ->
                                                        Rn.View(
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
                                            Rn.View(
                                                styles = [| Styles.spinnerBlock |],
                                                children =
                                                    elements {
                                                        Rn.ActivityIndicator(
                                                            color = "#aaaaaa",
                                                            size  = Size.Tiny
                                                        )
                                                    }
                                            )
                                        | _ ->
                                            noElement

                                        match lowLevelState with
                                        | Actionable onPress ->
                                            LC.Pressable(
                                                onPress       = onPress,
                                                label         = label,
                                                role          = AccessibilityRole.Button,
                                                ?testId       = testId,
                                                overlay       = true,
                                                pointerState  = pointerState,
                                                componentName = "LC.Button"
                                            )
                                        | _ ->
                                            noElement
                                    }
                            )
                    )
        )
