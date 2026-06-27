[<AutoOpen>]
module AppEggShellGallery.Components.Content_Stars

open Fable.React
open LibClient
open LibClient.Components

type Ui.Content with
    [<Component>]
    static member Stars() : ReactElement =
        Ui.ComponentContent(
            displayName = "Stars",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Stars",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = LC.Stars(count = 4),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Stars(count = 4)
"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals = LC.Stars(count = 1, total = 3),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Stars(count = 1, total = 3)
"""
                            )
                    )
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.Stars(
                                count = 4,
                                theme =
                                    fun theme ->
                                        { theme with
                                            OnColor = Color.DevGreen
                                            OffColor = Color.Grey "77"
                                            IconSize = 32
                                        }
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Stars(
    count = 4,
    theme = fun theme ->
        { theme with
            OnColor = Color.DevGreen
            OffColor = Color.Grey "77"
            IconSize = 32
        }
)
"""
                            )
                    )
                }
        )
