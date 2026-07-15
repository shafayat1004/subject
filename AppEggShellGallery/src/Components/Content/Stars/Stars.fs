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
            props       = ComponentContent.ForFullyQualifiedName "LibClient.Components.Stars",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.Stars",
                    role           = "text (rating display)",
                    namePattern    = "count prop rendered as star icons plus implicit rating value",
                    stateNotes     = "Static rating indicator; pair with visible numeric rating for color-blind users",
                    scalesWithFont = true,
                    contrastNotes  = "Star fill color is paired with count; not the sole signal"
                ),
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
                                            OnColor  = Color.DevGreen
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
