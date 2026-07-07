[<AutoOpen>]
module LibClient.Components.TextButton

open Fable.React

open LibClient
open LibClient.Accessibility

open Rn.Components
open Rn.Styles

module LC =
    module TextButton =
        type PropStateFactory = ButtonHighLevelStateFactory
        let Actionable = ButtonLowLevelState.Actionable
        let InProgress = ButtonLowLevelState.InProgress
        let Disabled   = ButtonLowLevelState.Disabled

        type Level =
        | Primary
        | Secondary

        type StateTheme = {
            TextColor: Color
            FontSize: int
            Opacity: float
        }

        type StatesTheme = {
            Actionable: StateTheme
            Disabled: StateTheme
            InProgress: StateTheme
        }

        type Theme = {
            Primary: StatesTheme
            Secondary: StatesTheme
        }
        with
            member this.StateTheme (level: Level) (state: ButtonLowLevelState) =
                let statesTheme =
                    match level with
                    | Primary -> this.Primary
                    | Secondary -> this.Secondary

                match state with
                | Actionable _ -> statesTheme.Actionable
                | InProgress -> statesTheme.InProgress
                | Disabled -> statesTheme.Disabled

open LC.TextButton

// TODO: delete after RenderDSL migration
type PropStateFactory = LC.TextButton.PropStateFactory

[<RequireQualifiedAccess>]
module private Styles =
    let tapCapture =
        makeViewStyles {
            trbl -12 -12 -12 -12
        }

    let view =
        ViewStyles.Memoize(
            fun (stateName: string) ->
                makeViewStyles {
                    Position.Relative
                    Overflow.VisibleForTapCapture
                    minHeight 44
                    JustifyContent.Center

                    match stateName with
                    | "Actionable" ->
                        Cursor.Pointer
                    | _ ->
                        Noop
                }
        )

    let textTheme =
        TextStyles.Memoize (fun (textColorCss: string) (labelFontSize: int) (labelOpacity: float) ->
            makeTextStyles {
                color (Color.InternalString textColorCss)
                fontSize labelFontSize
                opacity labelOpacity
            })

    let textThemeFor (theme: Theme) (level: Level) (state: ButtonLowLevelState) =
        let stateTheme = theme.StateTheme level state
        textTheme stateTheme.TextColor.ToCssString stateTheme.FontSize stateTheme.Opacity

    let spinnerBlock =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
            AlignItems.Center
            JustifyContent.Center
            backgroundColor (Color.WhiteAlpha 0.5)
        }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member TextButton(
            state: ButtonHighLevelState,
            label: string,
            ?level: Level,
            ?numberOfLines: int,
            ?testId: string,
            ?role: AccessibilityRole,
            ?accessibilityState: AccessibilityStateRecord,
            ?styles: array<TextStyles>,
            ?theme:   Theme -> Theme,
            ?key: string
        ) : ReactElement =
        key |> ignore

        let level = defaultArg level Primary
        let theTheme = Themes.GetMaybeUpdatedWith theme
        let lowLevelState = state.ToLowLevel
        let theTestId = testId |> Option.defaultValue (A11ySlug.testId "text-button" label)
        let theRole = defaultArg role AccessibilityRole.Button
        let theA11yState = defaultArg accessibilityState AccessibilityStateRecord.empty

        Rn.View(
            styles = [| Styles.view lowLevelState.GetName |],
            children =
                elements {
                    LC.Text(
                        value = label,
                        styles =
                            [|
                                Styles.textThemeFor theTheme level lowLevelState
                                yield! (styles |> Option.defaultValue [||])
                            |],
                        ?numberOfLines = numberOfLines
                    )

                    match lowLevelState with
                    | InProgress ->
                        Rn.View(
                            styles = [| Styles.spinnerBlock |],
                            children =
                                elements {
                                    Rn.ActivityIndicator(
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
                            role = theRole,
                            state = theA11yState,
                            testId = theTestId,
                            overlay = true,
                            styles = [| Styles.tapCapture |],
                            componentName = "LC.TextButton"
                        )
                    | _ ->
                        noElement
                }
        )