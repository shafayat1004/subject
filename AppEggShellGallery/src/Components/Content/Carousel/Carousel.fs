[<AutoOpen>]
module AppEggShellGallery.Components.Content_Carousel

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery.LocalImages
open Rn.Components
open Rn.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let carousel = makeViewStyles { height 200 }
    let image    = makeViewStyles { height 200 }

module private Helpers =
    let carouselSlide (urls: string[]) (index: int) : ReactElement =
        LC.With.Layout(
            initialOnly = true,
            ``with`` = fun (onLayoutOption, maybeLayout) ->
                Rn.View(
                    ?onLayout = onLayoutOption,
                    children = [|
                        Rn.Image(
                            source = localImage urls.[index],
                            resizeMode = Image.Cover,
                            size = Image.FromParentLayout maybeLayout,
                            styles = [| Styles.image |]
                        )
                    |]
                )
        )

type Ui.Content with
    [<Component>]
    static member Carousel () : ReactElement =
        Ui.ComponentContent(
            displayName = "Carousel",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Carousel",
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.Carousel",
                    role = "none (scroll container)",
                    namePattern = "Child slide content provides names; carousel itself is unlabeled",
                    stateNotes = "Swipe/scroll navigation; ensure non-gesture alternatives for slide content",
                    scalesWithFont = true,
                    contrastNotes = "Slide content inherits child contrast"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.Carousel(
                                (4 |> PositiveInteger.ofLiteral),
                                Helpers.carouselSlide [| "/images/wlop1.jpg"; "/images/wlop2.jpg"; "/images/wlop3.jpg"; "/images/wlop4.jpg" |],
                                styles = [| Styles.carousel |],
                                requestFocusOnMount = true
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Carousel(
    styles = [| makeViewStyles { height 200 } |],
    count = 4 |> PositiveInteger.ofLiteral,
    requestFocusOnMount = true,
    slide = fun index ->
        LC.With.Layout(
            initialOnly = true,
            ``with`` = fun (onLayoutOption, maybeLayout) ->
                Rn.View(
                    ?onLayout = onLayoutOption,
                    children = [|
                        Rn.Image(
                            source = localImage (sprintf "/images/wlop%i.jpg" (index + 1)),
                            resizeMode = Image.Cover,
                            size = Image.FromParentLayout maybeLayout,
                            styles = [| makeViewStyles { height 200 } |]
                        )
                    |]
                )
        )
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            element {
                                let urls =
                                    [|
                                        "/images/wlop1.jpg"
                                        "/images/wlop2.jpg"
                                        "/images/wlop3.jpg"
                                        "/images/wlop4.jpg"
                                        "/images/yuumei1.jpg"
                                    |]

                                LC.Carousel(
                                    (urls.Length |> PositiveInteger.ofIntUnsafe),
                                    Helpers.carouselSlide urls,
                                    styles = [| Styles.carousel |],
                                    requestFocusOnMount = true
                                )
                            },
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
let urls = [|
    "/images/wlop1.jpg"
    "/images/wlop2.jpg"
    "/images/wlop3.jpg"
    "/images/wlop4.jpg"
    "/images/yuumei1.jpg"
|]

LC.Carousel(
    styles = [| makeViewStyles { height 200 } |],
    count = urls.Length |> PositiveInteger.ofIntUnsafe,
    requestFocusOnMount = true,
    slide = fun index ->
        LC.With.Layout(
            initialOnly = true,
            ``with`` = fun (onLayoutOption, maybeLayout) ->
                Rn.View(
                    ?onLayout = onLayoutOption,
                    children = [|
                        Rn.Image(
                            source = localImage urls.[index],
                            resizeMode = Image.Cover,
                            size = Image.FromParentLayout maybeLayout,
                            styles = [| makeViewStyles { height 200 } |]
                        )
                    |]
                )
        )
)"""
                            )
                    )
                }
        )
