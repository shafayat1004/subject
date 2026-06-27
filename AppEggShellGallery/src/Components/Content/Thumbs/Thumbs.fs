[<AutoOpen>]
module AppEggShellGallery.Components.Content_Thumbs

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery.LocalImages
open AppEggShellGallery.Navigation

type private Helpers =
    [<Component>]
    static member SelectableSample () : ReactElement =
        let selected = Hooks.useState (Set.ofList [2; 4])
        let items = [4; 3; 2; 1]

        LC.Thumbs(
            ``for`` = LC.Thumbs.For.Of (items, fun i -> localImage (sprintf "/images/yuumei%i.jpg" i)),
            selected = selected.current,
            onPress = fun i _ _ -> selected.update (selected.current.Toggle i)
        )

    [<Component>]
    static member ImageViewerSample () : ReactElement =
        let items = [4; 3; 2; 1]
        let itemToImage i = localImage (sprintf "/images/yuumei%i.jpg" i)

        LC.Thumbs(
            ``for`` = LC.Thumbs.For.Of (items, itemToImage),
            onPress = fun _ index -> nav.Go (ImageViewer (items |> List.map itemToImage, index))
        )

type Ui.Content with
    [<Component>]
    static member Thumbs () : ReactElement =
        Ui.ComponentContent(
            displayName = "Thumbs",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Thumbs",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = LC.Thumbs(``for`` = LC.Thumbs.For.Of []),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """LC.Thumbs(``for`` = LC.Thumbs.For.Of [])"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.Thumbs(
                                ``for`` =
                                    LC.Thumbs.For.Of (
                                        ["/images/yuumei1.jpg"; "/images/yuumei2.jpg"; "/images/yuumei3.jpg"; "/images/yuumei4.jpg"]
                                        |> List.map localImage
                                    )
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Thumbs(
    ``for`` = LC.Thumbs.For.Of (
        ["/images/yuumei1.jpg"; "/images/yuumei2.jpg"; "/images/yuumei3.jpg"; "/images/yuumei4.jpg"]
        |> List.map localImage
    )
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.Thumbs(
                                ``for`` =
                                    LC.Thumbs.For.Of (
                                        [4; 3; 2; 1],
                                        fun i -> localImage (sprintf "/images/yuumei%i.jpg" i)
                                    )
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Thumbs(
    ``for`` = LC.Thumbs.For.Of (
        [4; 3; 2; 1],
        fun i -> localImage (sprintf "/images/yuumei%i.jpg" i)
    )
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals = Helpers.SelectableSample(),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
let selected = Hooks.useState (Set.ofList [2; 4])
let items = [4; 3; 2; 1]

LC.Thumbs(
    ``for`` = LC.Thumbs.For.Of (items, fun i -> localImage (sprintf "/images/yuumei%i.jpg" i)),
    selected = selected.current,
    onPress = fun i _ _ -> selected.update (selected.current.Toggle i)
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals = Helpers.ImageViewerSample(),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
let items = [4; 3; 2; 1]
let itemToImage i = localImage (sprintf "/images/yuumei%i.jpg" i)

LC.Thumbs(
    ``for`` = LC.Thumbs.For.Of (items, itemToImage),
    onPress = fun _ index -> nav.Go (ImageViewer (items |> List.map itemToImage, index))
)"""
                            )
                    )
                }
        )
