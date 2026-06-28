[<AutoOpen>]
module AppEggShellGallery.Components.Content_FloatingActionButton

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.FloatingActionButton
open LibClient.Icons
open AppEggShellGallery.Actions

module private Styles =
    let specialTheme (theme: LC.FloatingActionButton.Theme) : LC.FloatingActionButton.Theme =
        { theme with
            Actionable =
                { theme.Actionable with
                    BackgroundColor = Color.White
                    IconColor       = Color.DevBlue
                }
        }

type Ui.Content with
    [<Component>]
    static member FloatingActionButton() : ReactElement =
        Ui.ComponentContent(
            displayName = "FloatingActionButton",
            props =
                ComponentContent.ForFullyQualifiedName
                    "LibClient.Components.FloatingActionButton",
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.FloatingActionButton",
                    role = "button",
                    namePattern = "label prop when set; otherwise derived from icon (IconA11y.labelFromIcon)",
                    stateNotes = "disabled when MakeDisabled; busy when InProgress",
                    scalesWithFont = true,
                    contrastNotes = "Themed icon/background pairs meet WCAG AA"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.FloatingActionButton(
                                icon = Icon.Home,
                                state = PropStateFactory.MakeLowLevel (Actionable greet),
                                testId = "fab-home"
                            ),
                        code =
                            ComponentSample.SingleBlock (
                                ComponentSample.Fsharp,
                                LC.Text """
LC.FloatingActionButton(
    icon = Icon.Home,
    state = PropStateFactory.MakeLowLevel (Actionable greet),
    testId = "fab-home"
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.FloatingActionButton(
                                icon = Icon.Plus,
                                label = "Add Items",
                                state = PropStateFactory.MakeLowLevel (Actionable ignore),
                                testId = "fab-add-items"
                            ),
                        code =
                            ComponentSample.SingleBlock (
                                ComponentSample.Fsharp,
                                LC.Text """
LC.FloatingActionButton(
    icon = Icon.Plus,
    label = "Add Items",
    state = PropStateFactory.MakeLowLevel (Actionable ignore),
    testId = "fab-add-items"
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.FloatingActionButton(
                                icon = Icon.Home,
                                state = PropStateFactory.MakeLowLevel InProgress,
                                testId = "fab-home-in-progress"
                            ),
                        code =
                            ComponentSample.SingleBlock (
                                ComponentSample.Fsharp,
                                LC.Text """
LC.FloatingActionButton(
    icon = Icon.Home,
    state = PropStateFactory.MakeLowLevel InProgress,
    testId = "fab-home-in-progress"
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.FloatingActionButton(
                                icon = Icon.Plus,
                                label = "Add Items",
                                state = PropStateFactory.MakeLowLevel InProgress,
                                testId = "fab-add-items-in-progress"
                            ),
                        code =
                            ComponentSample.SingleBlock (
                                ComponentSample.Fsharp,
                                LC.Text """
LC.FloatingActionButton(
    icon = Icon.Plus,
    label = "Add Items",
    state = PropStateFactory.MakeLowLevel InProgress,
    testId = "fab-add-items-in-progress"
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.FloatingActionButton(
                                icon = Icon.Home,
                                state = PropStateFactory.MakeDisabled,
                                testId = "fab-home-disabled"
                            ),
                        code =
                            ComponentSample.SingleBlock (
                                ComponentSample.Fsharp,
                                LC.Text """
LC.FloatingActionButton(
    icon = Icon.Home,
    state = PropStateFactory.MakeDisabled,
    testId = "fab-home-disabled"
)"""
                            )
                    )
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.FloatingActionButton(
                                icon = Icon.Report,
                                state = PropStateFactory.MakeLowLevel (Actionable greet),
                                theme = Styles.specialTheme,
                                testId = "fab-report-themed"
                            ),
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        children = [|
                                            LC.Text """
LC.FloatingActionButton(
    icon = Icon.Report,
    state = PropStateFactory.MakeLowLevel (Actionable greet),
    theme = fun theme ->
        { theme with
            Actionable =
                { theme.Actionable with
                    BackgroundColor = Color.White
                    IconColor = Color.DevBlue
                }
        },
    testId = "fab-report-themed"
)"""
                                        |]
                                    )
                                }
                            )
                    )
                }
        )
