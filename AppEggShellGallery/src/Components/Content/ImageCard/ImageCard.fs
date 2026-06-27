[<AutoOpen>]
module AppEggShellGallery.Components.Content_ImageCard

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery.LocalImages
open ReactXP.Components
open ReactXP.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let metadata = makeViewStyles { FlexDirection.Row; JustifyContent.SpaceBetween; paddingHV 20 10 }
    let title    = makeTextStyles { FontWeight.Bold; fontSize 18; color Color.White }
    let author   = makeTextStyles { fontSize 14; color Color.White }

type Ui.Content with
    [<Component>]
    static member ImageCard () : ReactElement =
        Ui.ComponentContent (
            displayName = "ImageCard",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.ImageCard",
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = LC.ImageCard(
                                        source = localImage "/images/wlop4.jpg"
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.ImageCard(
    source = localImage "/images/wlop4.jpg"
)""")
                                )

                                Ui.ComponentSample(
                                    visuals = LC.ImageCard(
                                        source = localImage "/images/wlop4.jpg",
                                        label  = ImageCard.Text ("Painting", ImageCard.UseScrim.No)
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.ImageCard(
    source = localImage "/images/wlop4.jpg",
    label  = ImageCard.Text ("Painting", ImageCard.UseScrim.No)
)""")
                                )

                                Ui.ComponentSample(
                                    visuals = LC.ImageCard(
                                        source = localImage "/images/wlop4.jpg",
                                        label  = ImageCard.Text ("Painting", ImageCard.UseScrim.Yes)
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.ImageCard(
    source = localImage "/images/wlop4.jpg",
    label  = ImageCard.Text ("Painting", ImageCard.UseScrim.Yes)
)""")
                                )

                                Ui.ComponentSample(
                                    visuals = LC.ImageCard(
                                        source   = localImage "/images/wlop4.jpg",
                                        label    = ImageCard.Children ImageCard.UseScrim.Yes,
                                        children = [|
                                            RX.View(
                                                styles   = [| Styles.metadata |],
                                                children = [|
                                                    LC.UiText("Painting", styles = [| Styles.title  |])
                                                    LC.UiText("by WLOP",  styles = [| Styles.author |])
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
    children = [|
        RX.View(
            styles   = [| Styles.metadata |],
            children = [|
                LC.UiText("Painting", styles = [| Styles.title  |])
                LC.UiText("by WLOP",  styles = [| Styles.author |])
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
