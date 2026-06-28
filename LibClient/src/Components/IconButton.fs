[<AutoOpen>]
module LibClient.Components.IconButton

open Fable.React

open LibClient
open LibClient.Icons
open LibClient.Accessibility

open ReactXP.Components
open ReactXP.Styles

module LC =
    module IconButton =
        type PropStateFactory = ButtonHighLevelStateFactory
        let Actionable = ButtonLowLevelState.Actionable
        let InProgress = ButtonLowLevelState.InProgress
        let Disabled   = ButtonLowLevelState.Disabled

        type StateTheme = {
            IconColor: Color
            IconSize: int
            TapTargetMargin: int * int * int * int
        }

        type Theme = {
            Actionable: StateTheme
            Disabled: StateTheme
            InProgress: StateTheme
        }
        with
            member this.StateTheme (lowLevelState: ButtonLowLevelState) =
                match lowLevelState with
                | Actionable _ -> this.Actionable
                | InProgress -> this.InProgress
                | Disabled -> this.Disabled

open LC.IconButton

// TODO: delete after RenderDSL migration
type PropStateFactory = LC.IconButton.PropStateFactory

[<RequireQualifiedAccess>]
module private Styles =
    let spinnerBlock =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
            AlignItems.Center
            JustifyContent.Center
            backgroundColor (Color.WhiteAlpha 0.5)
        }

    let viewTheme =
        ViewStyles.Memoize (fun (btnIconSize: int) (lowLevelState: ButtonLowLevelState) (isDepressed: bool) ->
            makeViewStyles {
                width btnIconSize
                height btnIconSize

                FlexDirection.Row
                JustifyContent.Center
                AlignItems.Center
                Overflow.VisibleForTapCapture

                match lowLevelState with
                | ButtonLowLevelState.Disabled ->
                    opacity 0.5
                | ButtonLowLevelState.Actionable _ ->
                    Cursor.Pointer
                | ButtonLowLevelState.InProgress ->
                    Noop

                if isDepressed then
                    opacity 0.5
            })

    let viewThemeFor (theme: Theme) (lowLevelState: ButtonLowLevelState) (isDepressed: bool) =
        let stateTheme = theme.StateTheme lowLevelState
        viewTheme stateTheme.IconSize lowLevelState isDepressed

    let iconTheme =
        TextStyles.Memoize (fun (iconColor: Color) (btnIconSize: int) ->
            makeTextStyles {
                color iconColor
                fontSize btnIconSize
            })

    let iconThemeFor (theme: Theme) (lowLevelState: ButtonLowLevelState) =
        let stateTheme = theme.StateTheme lowLevelState
        iconTheme stateTheme.IconColor stateTheme.IconSize

    let tapCaptureTheme =
        ViewStyles.Memoize (fun (marginTop: int) (marginRight: int) (marginBottom: int) (marginLeft: int) ->
            makeViewStyles {
                trbl marginTop marginRight marginBottom marginLeft
            })

    let tapCaptureThemeFor (theme: Theme) (lowLevelState: ButtonLowLevelState) =
        let (t, r, b, l) = (theme.StateTheme lowLevelState).TapTargetMargin
        tapCaptureTheme t r b l

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member IconButton(
            state: ButtonHighLevelState,
            icon: IconConstructor,
            ?label: string,
            ?testId: string,
            ?styles: array<ViewStyles>,
            ?theme: Theme -> Theme,
            ?key: string
        ) : ReactElement =
        key |> ignore

        let theTheme = Themes.GetMaybeUpdatedWith theme
        let lowLevelState = state.ToLowLevel
        let a11yLabel = defaultArg label "Icon button"

        LC.Pointer.State(
            fun pointerState ->
                let isDepressed = pointerState.IsDepressed

                element {
                    RX.View(
                        styles =
                            [|
                                Styles.viewThemeFor theTheme lowLevelState isDepressed
                                yield! (styles |> Option.defaultValue [||])
                            |],
                        children =
                            elements {
                                LC.Icon(
                                    styles = [| Styles.iconThemeFor theTheme lowLevelState |],
                                    icon = icon
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
                                        label = a11yLabel,
                                        ?testId = testId,
                                        role = AccessibilityRole.Button,
                                        overlay = true,
                                        pointerState = pointerState,
                                        ?styles = (Some [| Styles.tapCaptureThemeFor theTheme lowLevelState |]),
                                        componentName = "LC.IconButton"
                                    )
                                | _ ->
                                    noElement
                            }
                    )
                }
        )