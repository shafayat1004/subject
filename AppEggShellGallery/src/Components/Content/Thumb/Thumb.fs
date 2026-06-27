[<AutoOpen>]
module AppEggShellGallery.Components.Content_Thumb

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery.LocalImages

type private Fruit = {
    Name:     string
    ImageUrl: string
}

let private banana = {
    Name     = "banana"
    ImageUrl = "/images/yuumei1.jpg"
}

type Ui.Content with
    [<Component>]
    static member Thumb () : ReactElement =
        Ui.ComponentContent(
            displayName = "Thumb",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Thumb",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = LC.Thumb(``for`` = LC.Thumb.For.Of (localImage "/images/yuumei1.jpg")),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """LC.Thumb(``for`` = LC.Thumb.For.Of (localImage "/images/yuumei1.jpg"))"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.Thumb(
                                ``for`` = LC.Thumb.For.Of (localImage "/images/yuumei1.jpg"),
                                isSelected = true
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Thumb(
    ``for`` = LC.Thumb.For.Of (localImage "/images/yuumei1.jpg"),
    isSelected = true
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.Thumb(
                                ``for`` = LC.Thumb.For.Of (banana, fun fruit -> localImage fruit.ImageUrl)
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
type Fruit = { Name: string; ImageUrl: string }
let banana = { Name = "banana"; ImageUrl = "/images/yuumei1.jpg" }

LC.Thumb(``for`` = LC.Thumb.For.Of (banana, fun fruit -> localImage fruit.ImageUrl))
"""
                            )
                    )
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.Thumb(
                                ``for`` = LC.Thumb.For.Of (localImage "/images/yuumei2.jpg"),
                                theme = fun theme -> { theme with Size = 120 }
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Thumb(
    ``for`` = LC.Thumb.For.Of (localImage "/images/yuumei2.jpg"),
    theme = fun theme -> { theme with Size = 120 }
)"""
                            )
                    )
                }
        )
