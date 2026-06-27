[<AutoOpen>]
module AppEggShellGallery.Components.Content_TimeSpan

open System
open Fable.React
open LibClient
open LibClient.Components

type Ui.Content with
    [<Component>]
    static member TimeSpan() : ReactElement =
        let sampleValue = TimeSpan.FromMilliseconds(123581321.)

        Ui.ComponentContent(
            displayName = "TimeSpan",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.TimeSpan",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = LC.TimeSpan(value = sampleValue),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.TimeSpan(value = System.TimeSpan.FromMilliseconds(123581321.))
"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.TimeSpan(
                                value = sampleValue,
                                shouldTruncateMillis = false
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.TimeSpan(
    value = System.TimeSpan.FromMilliseconds(123581321.),
    shouldTruncateMillis = false
)
"""
                            )
                    )
                }
        )
