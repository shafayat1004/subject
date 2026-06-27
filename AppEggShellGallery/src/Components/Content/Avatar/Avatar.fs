[<AutoOpen>]
module AppEggShellGallery.Components.Content_Avatar

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery.LocalImages

type Ui.Content with
    [<Component>]
    static member Avatar() : ReactElement =
        Ui.ComponentContent(
            displayName = "Avatar",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Avatar",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = LC.Avatar(source = localImage "/images/avatar.png"),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Avatar(source = localImage "/images/avatar.png")
"""
                            )
                    )
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.Avatar(
                                source = localImage "/images/avatar2.png",
                                theme = fun theme -> { theme with Size = 120 }
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Avatar(
    source = localImage "/images/avatar2.png",
    theme = fun theme -> { theme with Size = 120 }
)
"""
                            )
                    )
                }
        )
