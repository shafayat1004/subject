[<AutoOpen>]
module AppEggShellGallery.Components.Content_Button

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Button
open AppEggShellGallery
open ReactXP.Styles

module private SampleThemes =
    let private appearance textColor borderColor backgroundColor : LC.Button.Appearance =
        {
            TextColor       = textColor
            BorderColor     = borderColor
            BackgroundColor = backgroundColor
            FontWeight      = RulesRestricted.FontWeight.Normal
        }

    let private stateAppearance textColor borderColor backgroundColor : LC.Button.StateAppearance =
        {
            Actionable = appearance textColor borderColor backgroundColor
            Disabled   = appearance textColor borderColor backgroundColor
            InProgress = appearance textColor borderColor backgroundColor
        }

    let caution (theme: LC.Button.Theme) : LC.Button.Theme =
        { theme with Cautionary = stateAppearance Color.Black Color.White Color.DevOrange }

    let small (theme: LC.Button.Theme) : LC.Button.Theme =
        { theme with IconSize = 15 }

    let badgeGreen (theme: LC.Badge.Theme) : LC.Badge.Theme =
        { theme with
            FontWeight      = RulesRestricted.FontWeight.Bold
            FontColor       = Color.White
            BackgroundColor = Color.DevGreen
        }

module private Helpers =
    let wrap (child: ReactElement) =
        LC.Buttons(children = [| child |])

    let levelGroup (heading: string) (level: Level) (levelCode: string) =
        Ui.ComponentSampleGroup(
            heading = heading,
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            wrap (
                                LC.Button(
                                    label = "Submit",
                                    level = level,
                                    state = PropStateFactory.MakeLowLevel (Actionable Actions.greet)
                                )
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text $"""
LC.Button(
    label = "Submit",
    level = {levelCode},
    state = PropStateFactory.MakeLowLevel (Actionable Actions.greet)
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            wrap (
                                LC.Button(
                                    label = "Submit",
                                    level = level,
                                    state = PropStateFactory.MakeLowLevel InProgress
                                )
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text $"""
LC.Button(
    label = "Submit",
    level = {levelCode},
    state = PropStateFactory.MakeLowLevel InProgress
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            wrap (
                                LC.Button(
                                    label = "Submit",
                                    level = level,
                                    state = PropStateFactory.MakeDisabled
                                )
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text $"""
LC.Button(
    label = "Submit",
    level = {levelCode},
    state = PropStateFactory.MakeDisabled
)"""
                            )
                    )
                }
        )

type Ui.Content with
    [<Component>]
    static member Button() : ReactElement =
        Ui.ComponentContent(
            displayName = "Button",
            isResponsive = true,
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Button",
            notes =
                LC.Text "Every LC.Button component below is wrapped with LC.Buttons to prevent it from expanding to full width. This is not shown in code samples for simplicity.",
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Icons",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        Helpers.wrap (
                                            LC.Button(
                                                icon = Icon.Left Icon.Home,
                                                label = "Submit",
                                                state = PropStateFactory.MakeLowLevel (Actionable Actions.greet)
                                            )
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Button(
    icon = Icon.Left Icon.Home,
    label = "Submit",
    state = PropStateFactory.MakeLowLevel (Actionable Actions.greet)
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Badge",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        Helpers.wrap (
                                            LC.Button(
                                                badgeTheme = SampleThemes.badgeGreen,
                                                icon = Icon.Left Icon.ShoppingCart,
                                                label = "Cart",
                                                badge = Count 3,
                                                state = PropStateFactory.MakeLowLevel (Actionable Actions.greet)
                                            )
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Button(
    icon = Icon.Left Icon.Cart,
    label = "Cart",
    badge = Count 3,
    state = PropStateFactory.MakeLowLevel (Actionable Actions.greet)
)"""
                                        )
                                )
                            }
                    )

                    Helpers.levelGroup "Primary" Primary "Primary"
                    Helpers.levelGroup "Secondary" Secondary "Secondary"
                    Helpers.levelGroup "Tertiary" Tertiary "Tertiary"
                    Helpers.levelGroup "PrimaryB" PrimaryB "PrimaryB"
                    Helpers.levelGroup "SecondaryB" SecondaryB "SecondaryB"
                    Helpers.levelGroup "Cautionary" Cautionary "Cautionary"
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            Helpers.wrap (
                                LC.Button(
                                    theme = SampleThemes.caution,
                                    label = "Submit",
                                    level = Cautionary,
                                    state = PropStateFactory.MakeLowLevel (Actionable Actions.greet)
                                )
                            ),
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        children =
                                            [| LC.Text """
LC.Button(
    theme = SampleThemes.caution,
    label = "Submit",
    level = Cautionary,
    state = PropStateFactory.MakeLowLevel (Actionable Actions.greet)
)""" |]
                                    )

                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        heading = "Theme",
                                        children =
                                            [| LC.Text """
let caution (theme: LC.Button.Theme) =
    { theme with
        Cautionary = {
            Actionable = { TextColor = Color.Black; BorderColor = Color.White; BackgroundColor = Color.DevOrange; FontWeight = RulesRestricted.FontWeight.Normal }
            Disabled   = { TextColor = Color.Black; BorderColor = Color.White; BackgroundColor = Color.DevOrange; FontWeight = RulesRestricted.FontWeight.Normal }
            InProgress = { TextColor = Color.Black; BorderColor = Color.White; BackgroundColor = Color.DevOrange; FontWeight = RulesRestricted.FontWeight.Normal }
        }
    }
""" |]
                                    )
                                }
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            Helpers.wrap (
                                LC.Button(
                                    theme = SampleThemes.small,
                                    icon = Icon.Right Icon.Home,
                                    label = "Submit",
                                    state = PropStateFactory.MakeLowLevel (Actionable Actions.greet)
                                )
                            ),
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        children =
                                            [| LC.Text """
LC.Button(
    theme = SampleThemes.small,
    icon = Icon.Right Icon.Home,
    label = "Submit",
    state = PropStateFactory.MakeLowLevel (Actionable Actions.greet)
)""" |]
                                    )

                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        heading = "Theme",
                                        children =
                                            [| LC.Text """
let small (theme: LC.Button.Theme) =
    { theme with IconSize = 15 }
""" |]
                                    )
                                }
                            )
                    )
                }
        )
