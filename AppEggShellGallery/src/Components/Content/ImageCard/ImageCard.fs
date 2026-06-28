[<AutoOpen>]
module AppEggShellGallery.Components.Content_ImageCard

open Fable.React
open LibClient
open LibClient.Accessibility
open LibClient.Components
open AppEggShellGallery.LocalImages
open AppEggShellGallery
open ReactXP.Components
open ReactXP.Styles

[<RequireQualifiedAccess>]
module private MetadataStyles =
    let metadata = makeViewStyles { FlexDirection.Row; JustifyContent.SpaceBetween; paddingHV 20 10 }
    let title    = makeTextStyles { FontWeight.Bold; fontSize 18; color Color.White }
    let author   = makeTextStyles { fontSize 14; color Color.White }

type Ui.Content with
    [<Component>]
    static member ImageCard () : ReactElement =
        Ui.ComponentContent (
            displayName = "ImageCard",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.ImageCard",
            notes = LC.Text "ImageCard displays a background image with an optional label overlay. Use ImageCard.Text for simple captions or ImageCard.Children for custom metadata.",
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Pressable",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = LC.ImageCard(
                                        source = localImage "/images/wlop4.jpg",
                                        label  = ImageCard.Text ("Painting", ImageCard.UseScrim.Yes),
                                        onPress = (fun _ -> Action.alert "Image opened"),
                                        testId = A11ySlug.testId "image-card" "Painting",
                                        styles = [| sampleImageCardStyles |]
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.ImageCard(
    source = localImage "/images/wlop4.jpg",
    label  = ImageCard.Text ("Painting", ImageCard.UseScrim.Yes),
    onPress = (fun _ -> Action.alert "Image opened"),
    testId = A11ySlug.testId "image-card" "Painting",
    styles = [| sampleImageCardStyles |]
)""")
                                )
                            }
                        )
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    heading = "Image only",
                                    visuals = LC.ImageCard(
                                        source = localImage "/images/wlop4.jpg",
                                        styles = [| sampleImageCardStyles |]
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.ImageCard(
    source = localImage "/images/wlop4.jpg",
    styles = [| sampleImageCardStyles |]
)""")
                                )

                                Ui.ComponentSample(
                                    heading = "Text label, no scrim",
                                    visuals = LC.ImageCard(
                                        source = localImage "/images/wlop4.jpg",
                                        label  = ImageCard.Text ("Painting", ImageCard.UseScrim.No),
                                        styles = [| sampleImageCardStyles |]
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.ImageCard(
    source = localImage "/images/wlop4.jpg",
    label  = ImageCard.Text ("Painting", ImageCard.UseScrim.No),
    styles = [| sampleImageCardStyles |]
)""")
                                )

                                Ui.ComponentSample(
                                    heading = "Text label with scrim",
                                    visuals = LC.ImageCard(
                                        source = localImage "/images/wlop4.jpg",
                                        label  = ImageCard.Text ("Painting", ImageCard.UseScrim.Yes),
                                        styles = [| sampleImageCardStyles |]
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.ImageCard(
    source = localImage "/images/wlop4.jpg",
    label  = ImageCard.Text ("Painting", ImageCard.UseScrim.Yes),
    styles = [| sampleImageCardStyles |]
)""")
                                )

                                Ui.ComponentSample(
                                    heading = "Custom children overlay",
                                    visuals = LC.ImageCard(
                                        source   = localImage "/images/wlop4.jpg",
                                        label    = ImageCard.Children ImageCard.UseScrim.Yes,
                                        styles   = [| sampleImageCardStyles |],
                                        children = [|
                                            RX.View(
                                                styles   = [| MetadataStyles.metadata |],
                                                children = [|
                                                    LC.UiText("Painting", styles = [| MetadataStyles.title  |])
                                                    LC.UiText("by WLOP",  styles = [| MetadataStyles.author |])
                                                |]
                                            )
                                        |]
                                    ),
                                    code = ComponentSample.Children (
                                        element {
                                            Ui.Code(
                                                language = Code.Fsharp,
                                                children = [| LC.Text """
LC.ImageCard(
    source   = localImage "/images/wlop4.jpg",
    label    = ImageCard.Children ImageCard.UseScrim.Yes,
    styles   = [| sampleImageCardStyles |],
    children = [|
        RX.View(
            styles   = [| MetadataStyles.metadata |],
            children = [|
                LC.UiText("Painting", styles = [| MetadataStyles.title  |])
                LC.UiText("by WLOP",  styles = [| MetadataStyles.author |])
            |]
        )
    |]
)""" |]
                                            )
                                            Ui.Code(
                                                heading  = "Styles",
                                                language = Code.Fsharp,
                                                children = [| LC.Text """
let metadata = makeViewStyles { FlexDirection.Row; JustifyContent.SpaceBetween; paddingHV 20 10 }
let title    = makeTextStyles { FontWeight.Bold; fontSize 18; color Color.White }
let author   = makeTextStyles { fontSize 14; color Color.White }""" |]
                                            )
                                        }
                                    )
                                )
                            }
                        )
                    )
                }
            )
        )
