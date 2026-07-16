[<AutoOpen>]
module AppEggShellGallery.Components.Content_IconButton

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.IconButton
open AppEggShellGallery

module private Styles =
    let specialTheme (theme: LC.IconButton.Theme) : LC.IconButton.Theme =
        { theme with
            Actionable =
                { theme.Actionable with
                    IconColor = Color.DevBlue
                    IconSize  = 42
                }
        }

type Ui.Content with
    [<Component>]
    static member IconButton() : ReactElement =
        Ui.ComponentContent(
            displayName = "IconButton",
            props       = ComponentContent.ForFullyQualifiedName "LibClient.Components.IconButton",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.IconButton",
                    role           = "button",
                    namePattern    = "?label prop (defaults to \"Icon button\"); icon is decorative",
                    stateNotes     = "disabled when MakeDisabled; busy when InProgress",
                    scalesWithFont = false,
                    contrastNotes  = "Themed icon color meets WCAG AA on typical backgrounds"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.IconButton(
                                icon  = Icon.Megaphone,
                                state = PropStateFactory.MakeLowLevel (Actionable Actions.greet)
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.IconButton(
    icon = Icon.Megaphone,
    state = PropStateFactory.MakeLowLevel (Actionable Actions.greet)
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.IconButton(
                                icon  = Icon.Megaphone,
                                state = PropStateFactory.MakeLowLevel InProgress
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.IconButton(
    icon = Icon.Megaphone,
    state = PropStateFactory.MakeLowLevel InProgress
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.IconButton(
                                icon  = Icon.Home,
                                state = PropStateFactory.MakeDisabled
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.IconButton(
    icon = Icon.Home,
    state = PropStateFactory.MakeDisabled
)"""
                            )
                    )
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.IconButton(
                                icon  = Icon.Send,
                                state = PropStateFactory.MakeLowLevel (Actionable Actions.greet),
                                theme = Styles.specialTheme
                            ),
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        children =
                                            [| LC.Text """
LC.IconButton(
    icon = Icon.Send,
    state = PropStateFactory.MakeLowLevel (Actionable Actions.greet),
    theme = Styles.specialTheme
)""" |]
                                    )

                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        heading  = "Theme",
                                        children =
                                            [| LC.Text """
let specialTheme (theme: LC.IconButton.Theme) =
    { theme with
        Actionable =
            { theme.Actionable with
                IconColor = Color.DevBlue
                IconSize  = 42
            }
    }
""" |]
                                    )
                                }
                            )
                    )
                }
        )
