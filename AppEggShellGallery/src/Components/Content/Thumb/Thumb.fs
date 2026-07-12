[<AutoOpen>]
module AppEggShellGallery.Components.Content_Thumb

open Fable.React
open LibClient
open LibClient.Accessibility
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
            props       = ComponentContent.ForFullyQualifiedName "LibClient.Components.Thumb",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.Thumb",
                    role           = "image or button when pressable",
                    namePattern    = "?accessibilityLabel describing the thumbnail content",
                    stateNotes     = "Pressable thumbs expose button role",
                    scalesWithFont = false,
                    contrastNotes  = "N/A — image content"
                ),
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
                                ``for``    = LC.Thumb.For.Of (localImage "/images/yuumei1.jpg"),
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
                                ``for`` = LC.Thumb.For.Of (localImage "/images/yuumei1.jpg"),
                                onPress = (fun _ -> Action.alert "Thumbnail selected"),
                                testId  = A11ySlug.testId "thumb" "select"
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Thumb(
    ``for`` = LC.Thumb.For.Of (localImage "/images/yuumei1.jpg"),
    onPress = (fun _ -> Action.alert "Thumbnail selected"),
    testId = A11ySlug.testId "thumb" "select"
)"""
                            )
                    )
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.Thumb(
                                ``for`` = LC.Thumb.For.Of (localImage "/images/yuumei2.jpg"),
                                theme   = fun theme -> { theme with Size = 120 }
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
