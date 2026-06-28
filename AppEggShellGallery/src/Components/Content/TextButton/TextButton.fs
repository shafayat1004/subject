[<AutoOpen>]
module AppEggShellGallery.Components.Content_TextButton

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.TextButton
open AppEggShellGallery

module private SampleThemes =
    let special (theme: LC.TextButton.Theme) : LC.TextButton.Theme =
        { theme with
            Primary =
                { theme.Primary with
                    Actionable =
                        { theme.Primary.Actionable with
                            TextColor = Color.DevOrange
                        }
                }
        }

type Ui.Content with
    [<Component>]
    static member TextButton() : ReactElement =
        Ui.ComponentContent(
            displayName = "TextButton",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.TextButton",
            notes =
                LC.Text "Actionable text buttons auto-slug testId from the label (text-button-add-to-cart). Pass ?testId to override.",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.TextButton(
                                label = "Add to Cart",
                                state = PropStateFactory.MakeLowLevel (Actionable Actions.demoPointerEventAction)
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.TextButton(
    label = "Add to Cart",
    state = PropStateFactory.MakeLowLevel (Actionable Actions.demoPointerEventAction)
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.TextButton(
                                label = "Add to Cart",
                                state = PropStateFactory.MakeLowLevel InProgress
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.TextButton(
    label = "Add to Cart",
    state = PropStateFactory.MakeLowLevel InProgress
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.TextButton(
                                label = "Add to Cart",
                                state = PropStateFactory.MakeDisabled
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.TextButton(
    label = "Add to Cart",
    state = PropStateFactory.MakeDisabled
)"""
                            )
                    )
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.TextButton(
                                label = "Special Add to Cart",
                                state = PropStateFactory.MakeLowLevel (Actionable Actions.demoPointerEventAction),
                                theme = SampleThemes.special
                            ),
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        children =
                                            [| LC.Text """
LC.TextButton(
    label = "Special Add to Cart",
    state = PropStateFactory.MakeLowLevel (Actionable Actions.demoPointerEventAction),
    theme = SampleThemes.special
)""" |]
                                    )

                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        heading = "Theme",
                                        children =
                                            [| LC.Text """
let special (theme: LC.TextButton.Theme) =
    { theme with
        Primary =
            { theme.Primary with
                Actionable =
                    { theme.Primary.Actionable with TextColor = Color.DevOrange }
            }
    }
""" |]
                                    )
                                }
                            )
                    )
                }
        )
