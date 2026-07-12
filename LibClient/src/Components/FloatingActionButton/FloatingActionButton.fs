[<AutoOpen>]
module LibClient.Components.FloatingActionButton

open System.Text.RegularExpressions

open Fable.Core.JsInterop
open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Icons

open Rn.Components
open Rn.Styles

module LC =
    module FloatingActionButton =
        type StateTheme = {
            BackgroundColor: Color
            IconColor:       Color
            IconSize:        int
        }

        type Theme = {
            Size:       int
            Actionable: StateTheme
            Disabled:   StateTheme
            InProgress: StateTheme
        }
        with
            member this.StateTheme (state: ButtonLowLevelState) =
                match state with
                | ButtonLowLevelState.Actionable _ -> this.Actionable
                | ButtonLowLevelState.Disabled     -> this.Disabled
                | ButtonLowLevelState.InProgress   -> this.InProgress

        type PropStateFactory = ButtonHighLevelStateFactory

        let Actionable = ButtonLowLevelState.Actionable
        let InProgress = ButtonLowLevelState.InProgress
        let Disabled   = ButtonLowLevelState.Disabled

open LC.FloatingActionButton

type PropStateFactory = ButtonHighLevelStateFactory
let Actionable = ButtonLowLevelState.Actionable
let InProgress = ButtonLowLevelState.InProgress
let Disabled   = ButtonLowLevelState.Disabled

module private IconA11y =
    let private humanizePascalCase (name: string) =
        Regex.Replace(name, "([a-z0-9])([A-Z])", "$1 $2")

    let labelFromIcon (icon: IconConstructor) : string =
        let jsName =
            try
                match (box icon)?name with
                | null -> ""
                | n    -> unbox<string> n
            with _ ->
                ""
        if System.String.IsNullOrWhiteSpace jsName then "Floating action button"
        else humanizePascalCase jsName

[<RequireQualifiedAccess>]
module private Styles =
    let viewTheme =
        ViewStyles.Memoize(
            fun (fabSize: int) (fillColorCss: string) (hasLabel: bool) (stateName: string) (isDepressed: bool) (isHovered: bool) ->
                makeViewStyles {
                    Overflow.VisibleForTapCapture
                    FlexDirection.Row
                    AlignItems.Center
                    JustifyContent.SpaceAround
                    AlignSelf.FlexStart

                    shadow (Color.BlackAlpha 0.3) 5 (0, 1)

                    height          fabSize
                    minWidth        fabSize
                    borderRadius    (fabSize / 2)
                    backgroundColor (Color.InternalString fillColorCss)

                    if hasLabel then
                        paddingHorizontal (fabSize / 4)

                    match stateName with
                    | "Disabled"   -> opacity 0.5
                    | "Actionable" -> Cursor.Pointer
                    | _            -> Noop

                    if isDepressed then
                        shadow (Color.BlackAlpha 0.2) 3 (0, 0)
                        opacity 0.5
                    elif isHovered && not isDepressed then
                        shadow (Color.BlackAlpha 0.3) 6 (0, 3)
                }
        )

    let viewThemeFor (theme: Theme) (state: ButtonLowLevelState) (hasLabel: bool) (isDepressed: bool) (isHovered: bool) =
        let stateTheme = theme.StateTheme state
        viewTheme theme.Size stateTheme.BackgroundColor.ToCssString hasLabel state.GetName isDepressed isHovered

    let iconTheme =
        TextStyles.Memoize(
            fun (iconColorCss: string) (iconFontSize: int) ->
                makeTextStyles {
                    color    (Color.InternalString iconColorCss)
                    fontSize iconFontSize
                }
        )

    let iconThemeFor (theme: Theme) (state: ButtonLowLevelState) =
        let stateTheme = theme.StateTheme state
        iconTheme stateTheme.IconColor.ToCssString stateTheme.IconSize

    let labelBlock =
        makeViewStyles { marginLeft 8 }

    let labelTextTheme =
        TextStyles.Memoize(
            fun (labelColorCss: string) ->
                makeTextStyles {
                    fontSize 16
                    color    (Color.InternalString labelColorCss)
                }
        )

    let labelTextThemeFor (theme: Theme) (state: ButtonLowLevelState) =
        let stateTheme = theme.StateTheme state
        labelTextTheme stateTheme.IconColor.ToCssString

    let spinnerBlock =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
            AlignItems.Center
            JustifyContent.Center
            backgroundColor (Color.WhiteAlpha 0.5)
        }

    let pressableTapTarget =
        makeViewStyles {
            trbl -4 -12 -4 -12
        }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member FloatingActionButton(
            icon:           IconConstructor,
            state:          ButtonHighLevelState,
            ?label:         string,
            ?testId:        string,
            ?styles:        array<ViewStyles>,
            ?theme:         Theme -> Theme,
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>,
            ?key:           string
        ) : ReactElement =
        key |> ignore

        let theTheme = Themes.GetMaybeUpdatedWith theme
        let lowLevelState = state.ToLowLevel
        let hasLabel = label.IsSome

        let a11yLabel =
            match label with
            | Some text -> text
            | None      -> IconA11y.labelFromIcon icon

        let resolvedTestId =
            testId |> Option.orElse (Some (A11ySlug.testId "fab" a11yLabel))

        let legacyViewStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
            | None -> [||]

        LC.Pointer.State(
            fun pointerState ->
                let isDepressed = pointerState.IsDepressed
                let isHovered = pointerState.IsHovered && not isDepressed

                Rn.View(
                    styles =
                        [|
                            Styles.viewThemeFor theTheme lowLevelState hasLabel isDepressed isHovered
                            yield! legacyViewStyles
                            yield! (styles |> Option.defaultValue [||])
                        |],
                    children =
                        elements {
                            LC.Icon(
                                icon   = icon,
                                styles = [| Styles.iconThemeFor theTheme lowLevelState |]
                            )

                            match label with
                            | Some labelText ->
                                Rn.View(
                                    styles = [| Styles.labelBlock |],
                                    children =
                                        elements {
                                            LC.UiText(
                                                value  = labelText,
                                                styles = [| Styles.labelTextThemeFor theTheme lowLevelState |]
                                            )
                                        }
                                )
                            | None ->
                                noElement

                            match lowLevelState with
                            | InProgress ->
                                Rn.View(
                                    styles = [| Styles.spinnerBlock |],
                                    children =
                                        elements {
                                            Rn.ActivityIndicator(
                                                color = "#ffffff",
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
                                    label         = a11yLabel,
                                    ?testId       = resolvedTestId,
                                    role          = AccessibilityRole.Button,
                                    overlay       = true,
                                    pointerState  = pointerState,
                                    styles        = [| Styles.pressableTapTarget |],
                                    componentName = "LC.FloatingActionButton"
                                )
                            | _ ->
                                noElement
                        }
                )
        )
